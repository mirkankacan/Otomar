using System.Data;

namespace Otomar.Persistance.Data
{
    public interface IAppDbContext
    {
        IDbConnection Connection { get; }
    }
}