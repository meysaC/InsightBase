using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace InsightBase.Api.DTOs
{
    public class UploadFileRequest
    {
        [FromForm(Name = "file"), Required]
        public IFormFile File { get; set; }
        [FromForm]
        public string? UserFileName { get; set; }
        [FromForm]
        public string? FileName { get; set; }
        [FromForm]
        public string? DocumentType { get; set; } // kanun, yonetmelik, karar
        [FromForm]
        public string? LegalArea { get; set; } // medeni_hukuk, ceza_hukuku
        [FromForm]
        public bool IsPublic { get; set; } = false; // true gömülü dosya herkes erişebilir.
    }
}