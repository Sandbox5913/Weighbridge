using System.Data;

namespace Weighbridge.Services
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}