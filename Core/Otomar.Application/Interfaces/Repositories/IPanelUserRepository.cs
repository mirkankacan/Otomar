using Otomar.Domain.Entities;

namespace Otomar.Application.Interfaces.Repositories
{
    public interface IPanelUserRepository
    {
        Task<PanelKullanici?> GetByUsernameAsync(string username);
    }
}
