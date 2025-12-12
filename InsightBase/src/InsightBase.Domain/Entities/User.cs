using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsightBase.Domain.Entities
{
    public class User
    {
        public string Id { get; set; }
        public string? UserName { get; set; }
        public string Email { get; set; }
        public ICollection<Document>? Documents { get; set; } = new List<Document>();
    }
}