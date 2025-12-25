using System.Threading.Tasks;
using PMS.Core.Models.Common;

namespace PMS.Core.Services.Interfaces;


public interface IDatabaseSettingsService : ISettingsService<DatabaseSettings>
{
    Task<bool> TestConnectionAsync(DatabaseSettings config, string? encryptedUsername = "", string? encryptedPassword = "");
    string? BuildConnectionString(DatabaseSettings config);
}