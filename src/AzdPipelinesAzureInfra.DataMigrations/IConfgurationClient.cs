using Azure;
using Azure.Data.AppConfiguration;

namespace AzdPipelinesAzureInfra.DataMigrations;

public interface IConfgurationClient
{
    IAsyncEnumerable<ConfigurationSetting> GetConfigurationSettingsAsync(
        SettingSelector selector,
        CancellationToken cancellationToken = default);

    Task<Response<ConfigurationSetting>> GetConfigurationSettingAsync(
        string key,
        string? label = null,
        CancellationToken cancellationToken = default);

    Task<Response<ConfigurationSetting>> SetConfigurationSettingAsync(
        ConfigurationSetting setting,
        bool onlyIfUnchanged = false,
        CancellationToken cancellationToken = default);

    Task<Response<ConfigurationSetting>> SetConfigurationSettingAsync(
        string key,
        string value,
        string? label = null,
        CancellationToken cancellationToken = default);

    Task<Response<ConfigurationSetting>> AddConfigurationSettingAsync(
        ConfigurationSetting setting,
        CancellationToken cancellationToken = default);

    Task<Response<ConfigurationSetting>> AddConfigurationSettingAsync(
        string key,
        string value,
        string? label = null,
        CancellationToken cancellationToken = default);

    Task<Response> DeleteConfigurationSettingAsync(
        string key,
        string? label = null,
        CancellationToken cancellationToken = default);

    Task<Response> DeleteConfigurationSettingAsync(
        ConfigurationSetting setting,
        bool onlyIfUnchanged = false,
        CancellationToken cancellationToken = default);
}
