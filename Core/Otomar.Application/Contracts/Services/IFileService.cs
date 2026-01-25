using Microsoft.AspNetCore.Http;
using Otomar.Application.Common;
using Otomar.Domain.Enums;

namespace Otomar.Application.Contracts.Services
{
    public interface IFileService
    {
        Task<ServiceResult<List<string>>> UploadFileAsync(List<IFormFile> files, FileType fileType, string folderId, CancellationToken cancellationToken);
    }
}