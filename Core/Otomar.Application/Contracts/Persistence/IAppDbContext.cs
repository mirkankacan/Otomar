using System.Data;

namespace Otomar.Application.Contracts.Persistence
{
    public interface IAppDbContext : IDisposable
    {
        IDbConnection Connection { get; }
    }
}
