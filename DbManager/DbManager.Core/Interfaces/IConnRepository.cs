using DbManager.Core.Interfaces;
using DbManager.Core.Models;

namespace DbManager.Core.Interfaces;

public interface IConnRepository
{
    Task InitializeDatabaseAsync();
    Task<List<DbConnectionModel>> GetAllConnectionsAsync();
    Task<DbConnectionModel?> GetConnectionByIdAsync(int id);
    Task<DbConnectionModel?> GetConnectionByNameAsync(string name);
    Task<int> AddConnectionAsync(DbConnectionModel connection);
    Task<int> UpdateConnectionAsync(DbConnectionModel connection);
    Task<int> DeleteConnectionAsync(int id);
    Task<int> DeleteConnectionsAsync(List<int> ids);
    Task<List<DbConnectionGroupModel>> GetAllGroupsAsync();
    Task<int> AddGroupAsync(DbConnectionGroupModel group);
    Task<int> UpdateGroupAsync(DbConnectionGroupModel group);
    Task<int> DeleteGroupAsync(int id);
}
