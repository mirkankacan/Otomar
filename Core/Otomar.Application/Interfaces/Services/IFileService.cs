using Microsoft.AspNetCore.Http;
using Otomar.Shared.Common;
using Otomar.Shared.Enums;

namespace Otomar.Application.Interfaces.Services
{
    public interface IFileService
    {
        Task<ServiceResult<List<string>>> UploadFileAsync(List<IFormFile> files, FileType fileType, string folderId, CancellationToken cancellationToken);
    }
}