using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.DTOs;

namespace InsightBase.Application.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadAsync(string fileName, string? userFileName, string fileType, byte[] content); //dosyayı byte array olarak alacak
        // Task<bool> RemoveObjectAsync(string fileName, string? versionId);
        // Task<RemoveResult> RemoveAsync(IEnumerable<string> fileNames);
        Task<RemoveResult> RemoveAsync(params string[] fileNames);
        Task<string> GetPresignedUrlAsync(string fileName, int expiryInMinutes = 60);
    }
}