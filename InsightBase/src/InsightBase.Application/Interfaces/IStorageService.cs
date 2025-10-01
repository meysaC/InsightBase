using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsightBase.Application.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadAsync(string fileName, string fileType, byte[] content); //dosyayı byte array olarak alacak
        Task<bool> RemoveObjectAsync(string fileName, string? versionId);
    }
}