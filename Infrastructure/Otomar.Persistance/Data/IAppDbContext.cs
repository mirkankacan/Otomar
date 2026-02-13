using System.Data;

namespace Otomar.Persistance.Data
{
    public interface IAppDbContext : IDisposable
    {
        IDbConnection Connection { get; }
    }
}