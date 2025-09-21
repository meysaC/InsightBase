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
        public async Task<string> UploadAsync(string fileName, byte[] content)
        {
            // Check if the bucket exists, if not create it
            bool found = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
            if(!found) await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
            await _client.PutObjectAsync(new PutObjectArgs()
                            .WithBucket(_bucketName)
                            .WithObject(fileName)
                            .WithStreamData(new MemoryStream(content))
                            .WithObjectSize(content.Length));
            return $"{_bucketName}/{fileName}";
        }
    }
}