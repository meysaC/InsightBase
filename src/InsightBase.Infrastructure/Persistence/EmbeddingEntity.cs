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
        [Column(TypeName = "vector(3072)")]
        public Vector? Vector { get; set; } 
        public string ModelName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}