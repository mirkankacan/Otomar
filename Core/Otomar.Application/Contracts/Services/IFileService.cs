using Microsoft.AspNetCore.Http;
using Otomar.Contracts.Common;
using Otomar.Contracts.Enums;

namespace Otomar.Application.Contracts.Services
{
    public interface IFileService
    {
        Task<ServiceResult<List<string>>> UploadFileAsync(List<IFormFile> files, FileType fileType, string folderId, CancellationToken cancellationToken);
    }
}