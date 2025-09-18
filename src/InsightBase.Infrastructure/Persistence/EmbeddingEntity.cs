using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Pgvector;

namespace InsightBase.Infrastructure.Persistence
{
    public class EmbeddingEntity //DB’ye insert veya sorgu yaparken burası kullanılacak.
    { 
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentChunkId { get; set; }
        [Column(TypeName = "vector(1536)")] //pgvector sütun tipi text-embedding-3-large için 3072 boyutlu vektör
        public Vector? Vector { get; set; } 
        public string ModelName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}