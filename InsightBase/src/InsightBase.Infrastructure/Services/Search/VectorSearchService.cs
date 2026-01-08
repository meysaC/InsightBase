using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Pgvector;
using InsightBase.Application.Models.Enum;

namespace InsightBase.Infrastructure.Services.Search
{
    public class VectorSearchService : IVectorSearchService //pgvector ile semantic search
                                                            // HNSW (Hierarchical Navigable Small World Graph -> approximate nearest neighbor (ANN) algoritması) index kullanarak cosine similarity search
    {
        private readonly string _connectionString;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<VectorSearchService> _logger;
        private readonly IConfiguration _config;
        public VectorSearchService(IEmbeddingService embeddingService, ILogger<VectorSearchService> logger, IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _embeddingService = embeddingService;
            _logger = logger;
        }
        
        // pgvector ile vektör arama
        public async Task<List<SearchResult>> SearchAsync(string query, AccessDomain accessDomain, int topK = 20, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Şu query için vektör arama başlatılıyor: {query} topK: {topK} in access domain: {accessDomain}.");
            try
            {
                // query embedding e çevriliyor
                var queryEmbedding = _embeddingService.GenerateEmbeddingWithRetryAsync(new List<string> {query});

                // pgvector ile similarty search 
                var results = await ExecuteVectorSearchAsync(queryEmbedding, accessDomain, topK, cancellationToken);

                _logger.LogInformation($"Vektör arama tamamlandı. {results.Count} sonuç bulundu.");
                return results;
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Vektör arama sırasında hata oluştu.", ex);
                throw;
            }
        }

        // pgvector ile HNSW arama
        private async Task<List<SearchResult>> ExecuteVectorSearchAsync(Task<List<float[]>> queryEmbedding, AccessDomain accessDomain, int topK, CancellationToken cancellationToken)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var accessClause = BuildAccessControlClause(accessDomain);

            // c.embedding <=> @queryEmbedding pgvector’ın HNSW distance operator’udur (HNSW index kullanılarak nearest-neighbor search)
            var sql = $@"
                SELECT  
                    c.chunk_id,
                    c.document_id,
                    c.chunk_index,
                    c.content,
                    d.title,
                    d.document_type,
                    d.legal_area,
                    d.court,
                    d.file_number,
                    d.publish_date,
                    d.law_references,
                    d.url,
                    d.is_global,
                    d.organization_id,
                    d.is_amended,
                    d.amendment_date,
                    1 - (c.embedding <=> @queryEmbedding) as cosine_similarity
                FROM document_chunks c
                JOIN documents d ON c.document_id = d.document_id
                WHERE ({accessClause})
                  AND c.embedding IS NOT NULL
                ORDER BY c.embedding <=> @queryEmbedding
                LIMIT @topK";

            using var cmd = new NpgsqlCommand(sql, connection);

            var embeddings = await queryEmbedding; // Await the task to get the actual List<float[]>
            var embeddingVector = embeddings[0];

            cmd.Parameters.AddWithValue("queryEmbedding", new Vector(embeddingVector));
            cmd.Parameters.AddWithValue("topK", topK);

            // access control parametreleri ekle
            AddAccesscontrolParameters(cmd, accessDomain);
            
            var results = new List<SearchResult>();

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var result = Mappers.SearchResultMapper.ToSearchResultFromNpgsql(reader);
                results.Add(result);
            }
            return results;
        }
        
        // batch vektör arama
        public async Task<Dictionary<string, List<SearchResult>>> BatchSearchAsync(List<string> queries, AccessDomain accessDomain, int topK = 20, CancellationToken cancellationToken = default) 
        {
            var task = queries.Select(query => SearchAsync(query, accessDomain, topK, cancellationToken));
            var results = await Task.WhenAll(task);

            return queries.Zip(results, (q, r) => new { Query = q, Results = r })
                          .ToDictionary(x => x.Query, x => x.Results);
        }
        
        // hybrid arama ( vektör + metadata pre-filtering )
        public async Task<List<SearchResult>> SearchWithMetaDataFilterAsync(string query, AccessDomain accessDomain, Dictionary<string, object> metadataFilters, int topK = 20, CancellationToken cancellationToken = default)
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingWithRetryAsync(new List<string> {query});

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var accessClause = BuildAccessControlClause(accessDomain);
            var metadataClause = BuildMetadataFilterClause(metadataFilters);

            var sql = $@"
                SELECT 
                    c.chunk_id, c.document_id, c.chunk_index, c.content,
                    d.title, d.document_type, d.legal_area, d.court,
                    d.file_number, d.publish_date, d.law_references, d.url,
                    d.is_global, d.organization_id, d.is_amended, d.amendment_date,
                    1 - (c.embedding <=> @queryEmbedding) as cosine_similarity
                FROM document_chunks c
                JOIN documents d ON c.document_id = d.document_id
                WHERE ({accessClause})
                  AND ({metadataClause})
                  AND c.embedding IS NOT NULL
                ORDER BY c.embedding <=> @queryEmbedding
                LIMIT @topK";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("queryEmbedding", new Vector(queryEmbedding[0]));
            cmd.Parameters.AddWithValue("topK", topK);

            AddAccesscontrolParameters(cmd, accessDomain);
            AddMetadataFilterParameters(cmd, metadataFilters);

            var results = new List<SearchResult>();

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(Mappers.SearchResultMapper.ToSearchResultFromNpgsql(reader)); //ToSearchResult
            }
            return results;
        }



        private string BuildAccessControlClause(AccessDomain accessDomain) // döküman global ise kullanıcı görebilir, kullanıcının bağlı old orginazasyona aitse yine görebilir
        {
            var clauses = new List<string>() { "d.is_global = true"};
            if(accessDomain.UserOrganizationIds.Any())
            {
                clauses.Add("d.organization_id = ANY(@userOrganizationIds)"); // SQL injection güvenli çünkü parametre kullanılıyor (@userOrganizationIds)
            }
            return string.Join(" OR ", clauses);
        }
        private void AddAccesscontrolParameters(NpgsqlCommand cmd, AccessDomain accessDomain)
        {
            if(accessDomain.UserOrganizationIds.Any())
            {
                cmd.Parameters.AddWithValue("organizationIds", accessDomain.UserOrganizationIds.ToArray());
            }
        }
        private string BuildMetadataFilterClause(Dictionary<string, object> filters)
        {
            if(!filters.Any()) return "1=1"; //her zaman true

            var clauses = new List<string>();

            if(filters.ContainsKey("legal_area"))
                clauses.Add("d.legal_area = @legal_area");

            if(filters.ContainsKey("court"))
                clauses.Add("d.court ILIKE @court");
            
            if(filters.ContainsKey("start_date"))
                clauses.Add("d.publish_date >= @start_date");

            if(filters.ContainsKey("end_date"))
                clauses.Add("d.publish_date <= @end_date");

            if(filters.ContainsKey("document_type"))
                clauses.Add("d.document_type = @document_type");

            return string.Join(" AND ", clauses);
        }
        private void AddMetadataFilterParameters(NpgsqlCommand cmd, Dictionary<string, object> metadataFilters)
        {
            foreach(var filter in metadataFilters)
            {
                cmd.Parameters.AddWithValue($"@{filter.Key.Replace("_", "")}", filter.Value);
            }
        }
    }
}