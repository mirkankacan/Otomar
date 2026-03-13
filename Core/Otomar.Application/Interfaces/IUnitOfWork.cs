using System.Data;

namespace Otomar.Application.Interfaces
{
    /// <summary>
    /// Transaction yönetimi için Unit of Work pattern.
    /// Repository'ler Connection ve Transaction üzerinden DB işlemlerini gerçekleştirir.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction? Transaction { get; }
        IDbTransaction BeginTransaction();
        void Commit();
        void Rollback();
    }
}
