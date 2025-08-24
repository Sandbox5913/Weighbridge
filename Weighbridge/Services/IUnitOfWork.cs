using System;
using System.Data; // Added for IDbConnection and IDbTransaction
using System.Threading.Tasks;

namespace Weighbridge.Services
{
    public interface IUnitOfWork : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction BeginTransaction();
        void Commit();
        void Rollback();
        // Potentially add properties for repositories here, e.g.,
        // IRepository<Vehicle> Vehicles { get; }
        // IRepository<Docket> Dockets { get; }
    }
}