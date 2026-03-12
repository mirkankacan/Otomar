using Otomar.Application.Contracts.Persistence;
using System.Data;

namespace Otomar.Persistence.Data
{
    /// <summary>
    /// IUnitOfWork implementasyonu. IAppDbContext üzerinden connection alır,
    /// transaction yaşam döngüsünü yönetir.
    /// </summary>
    public class UnitOfWork(IAppDbContext context) : IUnitOfWork
    {
        private IDbTransaction? _transaction;
        private bool _disposed;

        public IDbConnection Connection => context.Connection;

        public IDbTransaction? Transaction => _transaction;

        public IDbTransaction BeginTransaction()
        {
            _transaction = Connection.BeginTransaction();
            return _transaction;
        }

        public void Commit()
        {
            _transaction?.Commit();
            _transaction?.Dispose();
            _transaction = null;
        }

        public void Rollback()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _transaction = null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _transaction = null;
                _disposed = true;
            }
        }
    }
}
