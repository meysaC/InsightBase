using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightBase.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace InsightBase.Infrastructure.Services
{
    public class MinioStorageService : IStorageService
    {
        private readonly IMinioClient _client;
        private readonly string _bucketName; //= "documents";
        public MinioStorageService(IConfiguration configuration)//string endpoint, string accessKey, string secretKey
        {
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
            // Check if the bucket exists, if not create it
            bool found = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
            if(!found) await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));

            // URL encode the filename to make it ASCII-safe
            var encodedFileName = fileName != null
                                ? Uri.EscapeDataString(fileName)
                                : null;
            // Base64 encode the filename
            // var encodedFileName = fileName != null 
            //     ? Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fileName))
            //     : null;

            // await _client.PutObjectAsync(new PutObjectArgs()
            var putObjectArgs = new PutObjectArgs()
                            .WithBucket(_bucketName)
                            .WithObject(fileType) //fileName object name (dosyanın bucket içindeki adı (key))
                            .WithStreamData(new MemoryStream(content))
                            .WithObjectSize(content.Length)
                            .WithContentType(fileType);
            

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

            return $"{_bucketName}/{fileName ?? fileType}";
        }
        public async Task<bool> RemoveObjectAsync(string fileName, string? versionId = null) //, string versionId -> "versioning" özelliği açılırsa, her dosyanın farklı versiyonları olabilir
        {
            try
            {
                var args = new RemoveObjectArgs()
                                .WithBucket(_bucketName)
                                .WithObject(fileName);
                if (!string.IsNullOrEmpty(versionId)) args = args.WithVersionId(versionId);
                await _client.RemoveObjectAsync(args).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"The file could not be removed file name: {fileName}, bucket name: {_bucketName}", ex);
            }
        }
    }
}