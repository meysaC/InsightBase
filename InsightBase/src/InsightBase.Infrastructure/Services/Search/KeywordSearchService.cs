using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace InsightBase.Infrastructure.Services.Search
{
    public class KeywordSearchService : IKeywordSearchService // Postgresql full-text search BM25
                                                            // türkçe dil desteği ile
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;
        private readonly ILogger<KeywordSearchService> _logger;
        public KeywordSearchService(IConfiguration config, ILogger<KeywordSearchService> logger)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
            _logger = logger;
        }
        
        // BM25 keyword search
        public async Task<List<SearchResult>> SearchAsync(List<string> terms, AccessDomain accessDomain, int topK = 20, CancellationToken cancellationToken = default)
        {
            if(!terms.Any())
                return new List<SearchResult>();

            _logger.LogInformation("KeywordSearchService SearchAsync started with {TermCount} terms.", terms.Count);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var tsquery = BuildTsQuery(terms);
            var accessClause = BuildAccessControlClause(accessDomain);

            // bm25 sıralaması için sql ts_rank_cd
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
                    ts_rank_cd(c.search_vector, to_tsquery('turkish', @tsquery), 32) as bm25_score,
                    ts_headline('turkish', c.content, to_tsquery('turkish', @tsquery), 
                        'MaxWords=50, MinWords=25, MaxFragments=3') as highlighted_content
                FROM document_chunks c
                JOIN documents d ON c.document_id = d.document_id
                WHERE ({accessClause})
                  AND c.search_vector @@ to_tsquery('turkish', @tsquery)
                ORDER BY ts_rank_cd(c.search_vector, to_tsquery('turkish', @tsquery), 32) DESC
                LIMIT @topK";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("tsquery", tsquery);
            cmd.Parameters.AddWithValue("topK", topK);
            AddAccessControlParameters(cmd, accessDomain);

            var results = new List<SearchResult>();

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while( await reader.ReadAsync(cancellationToken))
            {
                var result = Mappers.SearchResultMapper.ToSearchResultFromNpgsql(reader);
                result.BM25Score = reader.GetDouble(16);

                // highlighted_content alanını SearchResult içindeki content alanına
                var highlightedContent = reader.GetString(17);
                result.Content = _config.GetValue<bool>("RAG:KeywordSearch:UseHighlightedContent") 
                                ? highlightedContent 
                                : result.Content;
                results.Add(result);
            }

            _logger.LogInformation("KeywordSearchService SearchAsync completed with {ResultCount} results.", results.Count);
            return results;
        }


        public async Task<List<SearchResult>> ExactMatchLawReferencesAsync(List<string> lawReferences, AccessDomain accessDomain, CancellationToken cancellationToken = default)
        {
            if(!lawReferences.Any())
                return new List<SearchResult>();
            
            _logger.LogInformation("KeywordSearchService ExactMatchLawReferenceAsync started with {LawReferenceCount} law references.", lawReferences.Count);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var accessClause = BuildAccessControlClause(accessDomain);

            // law_references alanında tam eşleşme için sql
            var sql = $@"
                SELECT 
                    c.chunk_id, c.document_id, c.chunk_index, c.content,
                    d.title, d.document_type, d.legal_area, d.court,
                    d.file_number, d.publish_date, d.law_references, d.url,
                    d.is_global, d.organization_id, d.is_amended, d.amendment_date,
                    1.0 as exact_match_score
                FROM document_chunks c
                JOIN documents d ON c.document_id = d.document_id
                WHERE ({accessClause})
                  AND d.law_references && @lawReferences
                ORDER BY 
                    CASE d.document_type 
                        WHEN 'Legislation' THEN 1
                        WHEN 'CaseLaw' THEN 2
                        ELSE 3
                    END,
                    d.publish_date DESC
                LIMIT 50";
            
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("lawReferences", lawReferences.ToArray());
            AddAccessControlParameters(cmd, accessDomain);

            var results = new List<SearchResult>();

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while( await reader.ReadAsync(cancellationToken))
            {
                var result = Mappers.SearchResultMapper.ToSearchResultFromNpgsql(reader);
                result.ExactMatchScore = 1.0;
                results.Add(result);
            }
            return results;
        }
        public async Task<List<SearchResult>> ExactMatchFileNumbersAsync(List<string> fileNumbers, AccessDomain accessDomain, CancellationToken cancellationToken = default)
        {
            if(!fileNumbers.Any())
                return new List<SearchResult>();

            _logger.LogInformation("KeywordSearchService ExactMatchFileNumberAsync started with {FileNumberCount} file numbers.", fileNumbers.Count);

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var accessClause = BuildAccessControlClause(accessDomain);

            // file_number alanında tam eşleşme için sql
            var sql = $@"
                SELECT 
                    c.chunk_id, c.document_id, c.chunk_index, c.content,
                    d.title, d.document_type, d.legal_area, d.court,
                    d.file_number, d.publish_date, d.law_references, d.url,
                    d.is_global, d.organization_id, d.is_amended, d.amendment_date,
                    1.0 as exact_match_score
                FROM document_chunks c
                JOIN documents d ON c.document_id = d.document_id
                WHERE ({accessClause})
                  AND d.file_number = ANY(@fileNumbers)
                ORDER BY d.publish_date DESC
                LIMIT 20";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("fileNumbers", fileNumbers.ToArray());
            AddAccessControlParameters(cmd, accessDomain);

            var results = new List<SearchResult>();

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while( await reader.ReadAsync(cancellationToken))
            {
                var result = Mappers.SearchResultMapper.ToSearchResultFromNpgsql(reader);
                result.ExactMatchScore = 1.0;
                results.Add(result);    
            }
            return results;
        }
        // phrase search - itirazin iptali davası - gibi tam kelime 
        public async Task<List<SearchResult>> PhraseSearchAsync(string phrase, AccessDomain accessDomain, int topK = 20, CancellationToken cancellationToken = default)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var accessClause = BuildAccessControlClause(accessDomain);

            var sql = $@"
                SELECT 
                    c.chunk_id, c.document_id, c.chunk_index, c.content,
                    d.title, d.document_type, d.legal_area, d.court,
                    d.file_number, d.publish_date, d.law_references, d.url,
                    d.is_global, d.organization_id, d.is_amended, d.amendment_date,
                    ts_rank_cd(c.search_vector, phraseto_tsquery('turkish', @phrase), 32) as phrase_score
                FROM document_chunks c
                JOIN documents d ON c.document_id = d.document_id
                WHERE ({accessClause})
                  AND c.search_vector @@ phraseto_tsquery('turkish', @phrase)
                ORDER BY ts_rank_cd(c.search_vector, phraseto_tsquery('turkish', @phrase), 32) DESC
                LIMIT @topK";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("phrase", phrase);
            cmd.Parameters.AddWithValue("topK", topK);
            AddAccessControlParameters(cmd, accessDomain);

            var results = new List<SearchResult>();

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while( await reader.ReadAsync(cancellationToken))
            {
                var result = Mappers.SearchResultMapper.ToSearchResultFromNpgsql(reader);
                result.BM25Score = reader.GetDouble(16);
                results.Add(result);
            } 
            return results;
        }


        private string BuildTsQuery(List<string> terms)
        {
            // özel hukuki terimler kouncak
            var processedTerms = terms.Select(term =>
            {
                // tck tbk gibi kısaltmalar aynen kalsın
                if(term.All(char.IsUpper) && term.Length <= 5) return $"'{term}':*";

                // sayılar aynen kalsın
                if(int.TryParse(term, out _)) return $"'{term}':*";

                return term;
            }).ToList();

            return string.Join(" | ", processedTerms);
        }
        private string BuildAccessControlClause(AccessDomain accessDomain)
        {
            var clauses = new List<string> { "d.is_global = true" };

            if(accessDomain.UserOrganizationIds.Any())
            {
                clauses.Add("d.organization_id = ANY(@organizationIds)");
            }

            return string.Join(" OR ", clauses);
        }
        private void AddAccessControlParameters(NpgsqlCommand cmd, AccessDomain accessDomain)
        {
            if(accessDomain.UserOrganizationIds.Any())
            {
                cmd.Parameters.AddWithValue("organizationIds", accessDomain.UserOrganizationIds.ToArray());
            }
        }
    }
}