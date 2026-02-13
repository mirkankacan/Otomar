using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Domain.Enums;
using Otomar.Persistance.Options;

namespace Otomar.Persistance.Services
{
    public class FileService(ILogger<FileService> logger, UiOptions uiOptions, IFileProvider fileProvider) : IFileService
    {
        private readonly PhysicalFileProvider _physicalFileProvider = (PhysicalFileProvider)fileProvider;

        public async Task<ServiceResult<List<string>>> UploadFileAsync(List<IFormFile> files, FileType fileType, string folderId, CancellationToken cancellationToken)
        {
            var wwwrootPath = _physicalFileProvider.Root;
            string folder = Path.Combine(wwwrootPath, fileType.ToString(), folderId);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var imagePaths = new List<string>();
            foreach (var file in files)
            {
                string originalFileName = file.FileName;
                string extension = Path.GetExtension(originalFileName);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);

                // Güvenli dosya adı oluştur (özel karakterleri temizle)
                string safeFileName = $"{SanitizeFileName(fileNameWithoutExt)}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                string filePath = Path.Combine(folder, safeFileName);

                // Dosyayı kaydet
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                // Path'i web path formatına çevir: /fileType/OTOMAR-XXX/image1.jpg
                string webPath = $"/{fileType.ToString()}/{folderId}/{safeFileName}";
                imagePaths.Add(webPath);
                logger.LogInformation($"{safeFileName} adlı dosya {webPath} yoluna kopyalandı");
            }

            return ServiceResult<List<string>>.SuccessAsOk(imagePaths);
        }

        private string SanitizeFileName(string fileName)
        {
            // Dosya adındaki geçersiz karakterleri temizle
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
    }
}