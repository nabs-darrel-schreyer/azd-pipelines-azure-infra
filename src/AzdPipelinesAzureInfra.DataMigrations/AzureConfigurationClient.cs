using Azure;
using Azure.Data.AppConfiguration;
using Azure.Identity;

namespace AzdPipelinesAzureInfra.DataMigrations;

/// <summary>
/// A wrapper over the Azure SDK <see cref="ConfigurationClient"/> that implements <see cref="IConfgurationClient"/>.
/// </summary>
internal class AzureConfigurationClient : IConfgurationClient
{
    private readonly ConfigurationClient _client;

    /// <summary>
    /// Creates a new instance using a connection string.
    /// </summary>
    public AzureConfigurationClient(string connectionString)
    {
        _client = new ConfigurationClient(connectionString);
    }

    /// <summary>
    /// Creates a new instance using a URI and DefaultAzureCredential for managed identity authentication.
    /// </summary>
    public AzureConfigurationClient(Uri endpoint)
    {
        _client = new ConfigurationClient(endpoint, new DefaultAzureCredential());
    }

    public IAsyncEnumerable<ConfigurationSetting> GetConfigurationSettingsAsync(
        SettingSelector selector,
        CancellationToken cancellationToken = default)
    {
        return _client.GetConfigurationSettingsAsync(selector, cancellationToken);
    }

    public async Task<Response<ConfigurationSetting>> GetConfigurationSettingAsync(
        string key,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        return await _client.GetConfigurationSettingAsync(key, label, cancellationToken);
    }

    public async Task<Response<ConfigurationSetting>> SetConfigurationSettingAsync(
        ConfigurationSetting setting,
        bool onlyIfUnchanged = false,
        CancellationToken cancellationToken = default)
    {
        return await _client.SetConfigurationSettingAsync(setting, onlyIfUnchanged, cancellationToken);
    }

    public async Task<Response<ConfigurationSetting>> SetConfigurationSettingAsync(
        string key,
        string value,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        return await _client.SetConfigurationSettingAsync(key, value, label, cancellationToken);
    }

    public async Task<Response<ConfigurationSetting>> AddConfigurationSettingAsync(
        ConfigurationSetting setting,
        CancellationToken cancellationToken = default)
    {
        return await _client.AddConfigurationSettingAsync(setting, cancellationToken);
    }

    public async Task<Response<ConfigurationSetting>> AddConfigurationSettingAsync(
        string key,
        string value,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        return await _client.AddConfigurationSettingAsync(key, value, label, cancellationToken);
    }

    public async Task<Response> DeleteConfigurationSettingAsync(
        string key,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        return await _client.DeleteConfigurationSettingAsync(key, label, cancellationToken);
    }

    public async Task<Response> DeleteConfigurationSettingAsync(
        ConfigurationSetting setting,
        bool onlyIfUnchanged = false,
        CancellationToken cancellationToken = default)
    {
        return await _client.DeleteConfigurationSettingAsync(setting, onlyIfUnchanged, cancellationToken);
    }
}
