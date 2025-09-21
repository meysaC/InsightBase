using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsightBase.Domain.Entities
{
    public class User // Bu gereksiz SİL???? !!!!!!!!!!!!!!!!!!!!!
    {
        public string Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public ICollection<Document>? Documents { get; set; } = new List<Document>();
    }
}