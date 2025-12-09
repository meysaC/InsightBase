using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsightBase.Domain.Entities
{
    public class User
    {
        public ICollection<Document>? Documents { get; set; } = new List<Document>();
    }
}