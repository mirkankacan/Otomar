using Otomar.Domain.Entities;

namespace Otomar.Application.Contracts.Persistence.Repositories
{
    public interface IPanelUserRepository
    {
        Task<PanelKullanici?> GetByUsernameAsync(string username);
    }
}
