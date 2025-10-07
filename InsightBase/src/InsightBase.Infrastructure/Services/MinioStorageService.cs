using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.DTOs;
using InsightBase.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace InsightBase.Infrastructure.Services
{
    public class MinioStorageService : IStorageService
    {
        private readonly IMinioClient _client;
        private readonly string _bucketName;
        private readonly ILogger<MinioStorageService> _logger;
        public MinioStorageService(IConfiguration configuration, ILogger<MinioStorageService> logger)//string endpoint, string accessKey, string secretKey
        {
            _logger = logger;
            var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
            var accessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
            var secretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");
            _bucketName = Environment.GetEnvironmentVariable("MINIO_BUCKET") ?? "documents";
            _client = new MinioClient()
                            .WithEndpoint(endpoint)
                            .WithCredentials(accessKey, secretKey)
                            .Build();
        }


        public async Task<string> UploadAsync(string? fileName, string fileType, byte[] content)
        {
            try
            {
                // Check if the bucket exists, if not create it
                bool found = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
                if (!found) await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));

                // URL encode the filename to make it ASCII-safe
                var encodedFileName = fileName != null
                                    ? Uri.EscapeDataString(fileName)
                                    : null;
                //to decode:
                //var decodedFileName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedFileName));

                var contentType = GetMimeType(fileType);

                // await _client.PutObjectAsync(new PutObjectArgs()
                var putObjectArgs = new PutObjectArgs()
                                .WithBucket(_bucketName)
                                .WithObject(fileType) // object name (dosyanın bucket içindeki adı (key))
                                .WithStreamData(new MemoryStream(content))
                                .WithObjectSize(content.Length)
                                .WithContentType(contentType); // mime type fileType


                // User olduğu zaman user bilgilerini de custom metadata ya ekle!!! -> metadata’ları daha sonra StatObjectAsync çağrısıyla okuyabilirsin.
                if (encodedFileName != null)
                {
                    putObjectArgs = putObjectArgs.WithHeaders(new Dictionary<string, string>
                    {
                        {"x-amz-meta-user-filename", encodedFileName} //fileName
                        // {"x-amz-meta-uploaded-by", user}
                    });
                }

                await _client.PutObjectAsync(putObjectArgs);

                return $"{_bucketName}/{fileType}"; //fileName ?? 
            }
            catch (Exception ex)
            {
                throw new Exception($"The document could not be upload to the minio, document name: {fileName}", ex);
            }
        }

        public async Task<RemoveResult> RemoveAsync(params string[] fileNames)
        {
            // var list = fileNames?.ToList() ?? new();
            if (fileNames.Length == 0 || fileNames == null)
            {
                _logger.LogWarning("RemoveAsync called with empty list.");
                return new RemoveResult();
            }

            return fileNames.Length == 1
                    ? await RemoveSingleAsync(fileNames[0], null)
                    : await RemoveBatchAsync(fileNames.ToList(), null);
        }

        private async Task<RemoveResult> RemoveSingleAsync(string fileName, string? versionId)
        {
            var result = new RemoveResult();
            try
            {
                var args = new RemoveObjectArgs()
                                .WithBucket(_bucketName)
                                .WithObject(fileName);
                if (!string.IsNullOrEmpty(versionId)) args = args.WithVersionId(versionId);
                await _client.RemoveObjectAsync(args).ConfigureAwait(false); //ConfigureAwait(false) asenkron deadlock riskini kaldırır

                bool exists = await ObjectExistsAsync(fileName);
                if (exists)
                {
                    result.Failed.Add(fileName);
                    _logger.LogWarning($"Error deleting file: {fileName}");
                }
                else
                {
                    result.Successful.Add(fileName);
                    _logger.LogInformation($"File deleted: {fileName}");
                }
                return result;
            }
            catch (Exception ex)
            {
                result.Failed.Add(fileName);
                _logger.LogError(ex, "Error deleting file: {File}", fileName);
            }
            return result;
        }
        private async Task<RemoveResult> RemoveBatchAsync(List<string> fileNames, string? versionId)
        {
            var result = new RemoveResult();
            try
            {
                var args = new RemoveObjectsArgs()
                                .WithBucket(_bucketName)
                                .WithObjects(fileNames);


                var deleteErrors = await _client.RemoveObjectsAsync(args);
                foreach (var err in deleteErrors)
                {
                    result.Failed.Add(err.Key ?? "(unkown)");
                    _logger.LogWarning($"Failed to delete {err.Key}: {err.Message}");
                }


                var verificationTasks = fileNames.Select(async file =>
                {
                    bool stilExists = await ObjectExistsAsync(file);
                    if (stilExists && !result.Failed.Contains(file))
                    {
                        result.Failed.Add(file);
                        _logger.LogWarning($"Post delete check failed for {file}");
                    }
                });

                await Task.WhenAll(verificationTasks);

                result.Successful = fileNames.Except(result.Failed).ToList();
                _logger.LogInformation($"Batch delete completed. Success: {result.Successful.Count}, Failed: {result.Failed.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexcepted batch deletion error", ex);
                result.Failed.AddRange(fileNames);
            }
            return result;
        }


        private async Task<bool> ObjectExistsAsync(string fileName)
        {
            try
            {
                var args = new StatObjectArgs()
                            .WithBucket(_bucketName)
                            .WithObject(fileName);
                await _client.StatObjectAsync(args);
                return true;    
            }
            catch (Exception)
            {
                return false;
            }
        }

        // public async Task<bool> RemoveObjectAsync(string fileName, string? versionId = null) //, string versionId -> "versioning" özelliği açılırsa, her dosyanın farklı versiyonları olabilir
        // {
        //     try
        //     {
        //         var args = new RemoveObjectArgs()
        //                         .WithBucket(_bucketName)
        //                         .WithObject(fileName);
        //         if (!string.IsNullOrEmpty(versionId)) args = args.WithVersionId(versionId);
        //         await _client.RemoveObjectAsync(args).ConfigureAwait(false);
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         throw new Exception($"The file could not be removed file name: {fileName}, bucket name: {_bucketName}", ex);
        //     }
        // }

        //this returns MIME types, (for HTTP headers)
        private string GetMimeType(string fileType)
        {
            var extension = Path.GetExtension(fileType).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".txt" => "text/plain",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                _ => "application/octet-stream"
            };
        }

    }
}