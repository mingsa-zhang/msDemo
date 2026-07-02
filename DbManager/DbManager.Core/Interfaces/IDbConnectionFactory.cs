using System.Data.Common;
using DbManager.Core.Enums;
using DbManager.Core.Models;

namespace DbManager.Core.Interfaces;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection(DbConnectionModel conn);
}
