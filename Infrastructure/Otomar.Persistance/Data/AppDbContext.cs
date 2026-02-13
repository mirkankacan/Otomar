using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Otomar.Persistance.Data
{
    public class AppDbContext : IAppDbContext, IDisposable
    {
        private readonly IDbConnection _connection;
        private bool _disposed;

        public AppDbContext(IConfiguration configuration)
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection?.Close();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }
}