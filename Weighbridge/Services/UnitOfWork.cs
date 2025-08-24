using System;
using System.Data;
using System.Threading.Tasks; // Keep for async operations if needed elsewhere, though not directly for transaction management here

namespace Weighbridge.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private IDbConnection? _connection;
        private IDbTransaction? _transaction;
        private bool _isDisposed = false;

        public IDbConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = _connectionFactory.CreateConnection(); // Assuming this is now synchronous
                    _connection.Open();
                }
                return _connection;
            }
        }

        public UnitOfWork(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IDbTransaction BeginTransaction()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }
            _transaction = Connection.BeginTransaction();
            return _transaction;
        }

        public void Commit()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit.");
            }
            try
            {
                _transaction.Commit();
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Rollback()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to rollback.");
            }
            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _connection?.Close();
                    _connection?.Dispose();
                }
                _isDisposed = true;
            }
        }

        ~UnitOfWork()
        {
            Dispose(false);
        }
    }
}