using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using Serilog;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PMS.Core.Models.Common;
using PMS.Core.Services.Interfaces;

namespace PMS.Core.Database;

public class DatabaseContext : IDatabaseContext
{
    private readonly IDatabaseSettingsService _configService;
    private readonly ILogger _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly Lazy<IMongoDatabase> _lazyDatabase;
    private readonly object _enumConfigLock = new();
    private static bool _enumsConfigured;

    public IMongoDatabase Database => _lazyDatabase.Value;
        

    public DatabaseContext(
        IDatabaseSettingsService configService,
        ILogger logger,
        IEncryptionService encryptionService
    )
    {
        _configService = configService;
        _logger = logger;
        _encryptionService = encryptionService;

        if (!_enumsConfigured)
        {
            lock (_enumConfigLock)
            {
                if (!_enumsConfigured)
                {
                    MongoDbSettings.ConfigureEnums();
                    _enumsConfigured = true;
                }
            }
        }

        _lazyDatabase = new Lazy<IMongoDatabase>(InitializeDatabase);
    }

    private IMongoDatabase InitializeDatabase()
    {
        string? tempUsername = null;
        string? tempPassword = null;
        string? connectionString = null;
        DatabaseSettings? tempSettings = null;

        try
        {
            _logger.Information("Initializing MongoDB connection...");

            var dbSettings = _configService.LoadSettings();

            if (_configService.ValidateSettings(dbSettings, out var message) != ValidationResult.Valid)
            {
                _logger.Error("Database context: {ErrorMessage}", message);
                throw new InvalidOperationException($"Invalid database settings: {message}");
            }

            tempUsername = _encryptionService.Decrypt(dbSettings.Username);
            tempPassword = _encryptionService.Decrypt(dbSettings.Password);

            tempSettings = new DatabaseSettings
            {
                Host = dbSettings.Host,
                Port = dbSettings.Port,
                DatabaseName = dbSettings.DatabaseName,
                UseAuthentication = dbSettings.UseAuthentication,
                Username = tempUsername,
                Password = tempPassword,
                AuthDatabase = dbSettings.AuthDatabase,
                ConnectionTimeout = dbSettings.ConnectionTimeout
            };

            connectionString = _configService.BuildConnectionString(tempSettings);

            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ConnectTimeout = TimeSpan.FromSeconds(tempSettings.ConnectionTimeout);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            settings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<CommandStartedEvent>(e =>
                {
                    _logger.Debug("MongoDB Command: {CommandName}", e.CommandName);
                });
            };

            var client = new MongoClient(settings);
            var database = client.GetDatabase(tempSettings.DatabaseName);

            _logger.Information("Connected to MongoDB database: {DatabaseName}", tempSettings.DatabaseName);

            return database;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to connect to MongoDB");
            throw new InvalidOperationException("Database connection failed", ex);
        }
        finally
        {
            ClearSensitiveString(ref tempUsername);
            ClearSensitiveString(ref tempPassword);
            ClearSensitiveString(ref connectionString);
                
            if (tempSettings != null)
            {
                tempSettings.Username = null;
                tempSettings.Password = null;
                tempSettings = null;
            }
        }
    }

    private static void ClearSensitiveString(ref string? sensitive)
    {
        if (string.IsNullOrEmpty(sensitive)) return;
        sensitive = null;
    }

    public IMongoCollection<T> GetCollection<T>(string? name)
    {
        return Database.GetCollection<T>(name);
    }

    private IMongoCollection<T> GetCollectionForType<T>(string? collectionName = null)
    {
        return collectionName != null ? GetCollection<T>(collectionName) : GetCollection<T>(typeof(T).Name.ToLower());
    }

    public async Task<T?> FindOneAsync<T>(Expression<Func<T, bool>> filter, string? collectionName = null) where T : BaseEntity
    {
        var collection = GetCollectionForType<T>(collectionName);
        return await collection.Find(filter).FirstOrDefaultAsync();
    }
}