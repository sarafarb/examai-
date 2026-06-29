using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamAI.Shared.Infrastructure.Storage
{
    public interface ISecureFileService
    {
        Task<string> UploadSecureFileAsync(string userId, string fileName, Stream fileStream, string contentType);
        string GeneratePresignedUrl(string userId, string fileKey);
    }

    public class SecureS3FileService : ISecureFileService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<SecureS3FileService> _logger;
        private readonly string _bucketName;

        public SecureS3FileService(IAmazonS3 s3Client, IConfiguration config, ILogger<SecureS3FileService> logger)
        {
            _s3Client = s3Client;
            _logger = logger;
            _bucketName = config["AWS:BucketName"] ?? "examai-files";
        }

        public async Task<string> UploadSecureFileAsync(string userId, string fileName, Stream fileStream, string contentType)
        {
            // IDOR Prevention: קובץ תמיד נשמר תחת התיקייה של המשתמש
            var objectKey = $"users/{userId}/uploads/{Guid.NewGuid()}_{fileName}";

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = fileStream,
                ContentType = contentType,
                // Server-Side Encryption (AES-256)
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256 
            };

            await _s3Client.PutObjectAsync(putRequest);
            _logger.LogInformation("Securely uploaded file {ObjectKey} for User {UserId}", objectKey, userId);

            return objectKey;
        }

        public string GeneratePresignedUrl(string userId, string fileKey)
        {
            // IDOR Prevention: נוודא שהקובץ המבוקש אכן שייך למשתמש שמבקש אותו
            if (!fileKey.StartsWith($"users/{userId}/"))
            {
                _logger.LogWarning("Security Alert: User {UserId} attempted to access unauthorized file {FileKey}", userId, fileKey);
                throw new UnauthorizedAccessException("You do not have permission to access this file.");
            }

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                // מקסימום 15 דקות תוקף ללינק
                Expires = DateTime.UtcNow.AddMinutes(15) 
            };

            return _s3Client.GetPreSignedURL(request);
        }
    }
}