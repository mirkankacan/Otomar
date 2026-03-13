using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Otomar.Application.Interfaces;
using System.Data;

namespace Otomar.Persistence.Data
{
    /// <summary>
    /// IUnitOfWork implementasyonu.
    /// Connection lifecycle ve transaction yönetimini birlikte sağlar.
    /// Scoped lifetime ile request başına tek instance kullanılır.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnection _connection;
        private IDbTransaction? _transaction;
        private bool _disposed;

        public UnitOfWork(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SqlConnection");
            _connection = new SqlConnection(connectionString);
        }

        public IDbConnection Connection
        {
            get
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();
                return _connection;
            }
        }

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
                _connection?.Close();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }
}
