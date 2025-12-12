using InsightBase.Application.Models;
using InsightBase.Application.Models.Enum;
using Npgsql;

namespace InsightBase.Infrastructure.Mappers
{
    public class SearchResultMapper
    {
        public static SearchResult ToSearchResultFromNpgsql(NpgsqlDataReader reader)
        {
            return new SearchResult
            {
                ChunkId = reader.GetString(0),
                DocumentId = reader.GetString(1),
                ChunkIndex = reader.GetInt32(2),
                Content = reader.GetString(3),
                Title = reader.GetString(4),
                DocumentType = Enum.Parse<DocumentType>(reader.GetString(5)),
                LegalArea = reader.GetString(6),
                Court = reader.IsDBNull(7) ? null : reader.GetString(7),
                FileNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                PublishDate = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                LawReferences = reader.IsDBNull(10) 
                    ? new List<string>() 
                    : reader.GetFieldValue<string[]>(10).ToList(),
                Url = reader.IsDBNull(11) ? null : reader.GetString(11),
                IsGlobal = reader.GetBoolean(12),
                OrganizationId = reader.IsDBNull(13) ? null : reader.GetString(13),
                IsAmended = reader.GetBoolean(14),
                AmendmentDate = reader.IsDBNull(15) ? null : reader.GetDateTime(15),
                VectorScore = reader.GetDouble(16),
                Relevance = reader.GetDouble(16)



                // DocumentId = reader.GetString(0),
                // ChunkId = reader.GetString(1),
                // ChunkIndex = reader.GetInt32(2),
                // Title = reader.GetString(3),
                // Content = reader.GetString(4),
                // MergedContent = reader.IsDBNull(5) ? null : reader.GetString(5),
                // IsMergedWithNext = reader.GetBoolean(6),
                // VectorScore = reader.GetDouble(7),
                // BM25Score = reader.GetDouble(8),
                // MetadataScore = reader.GetDouble(9),
                // ExactMatchScore = reader.GetDouble(10),
                // FinalScore = reader.GetDouble(11),
                // Relevance = reader.GetDouble(12),
                // DocumentType = Enum.Parse<DocumentType>(reader.GetString(13)),
                // LegalArea = reader.GetString(14),
                // Court = reader.IsDBNull(15) ? null : reader.GetString(15),
                // FileNumber = reader.IsDBNull(16) ? null : reader.GetString(16),
                // PublishDate = reader.IsDBNull(17) ? null : reader.GetDateTime(17),
                // LawReferences = reader.IsDBNull(18) ? new List<string>() : reader.GetFieldValue<List<string>>(18),
                // Url = reader.IsDBNull(19) ? null : reader.GetString(19),
                // IsGlobal = reader.GetBoolean(20),
                // OrganizationId = reader.IsDBNull(21) ? null : reader.GetString(21),
                // IsAmended = reader.GetBoolean(22),
                // AmendmentDate = reader.IsDBNull(23) ? null : reader.GetDateTime(23)
            };
        }   
        
        public static SearchResult ToSearchResult(dynamic data)
        {
            return new SearchResult
            {
                DocumentId = data.document_id,
                ChunkId = data.chunk_id,
                ChunkIndex = data.chunk_index,
                Title = data.title,
                Content = data.content,
                MergedContent = data.merged_content,
                IsMergedWithNext = data.is_merged_with_next,
                VectorScore = data.vector_score,
                BM25Score = data.bm25_score,
                MetadataScore = data.metadata_score,
                ExactMatchScore = data.exact_match_score,
                FinalScore = data.final_score,
                Relevance = data.relevance,
                DocumentType = Enum.Parse<DocumentType>(data.document_type),
                LegalArea = data.legal_area,
                Court = data.court,
                FileNumber = data.file_number,
                PublishDate = data.publish_date,
                LawReferences = data.law_references != null ? ((IEnumerable<string>)data.law_references).ToList() : new List<string>(),
                Url = data.url,
                IsGlobal = data.is_global,
                OrganizationId = data.organization_id,
                IsAmended = data.is_amended,
                AmendmentDate = data.amendment_date
            };
        }
    }
}

