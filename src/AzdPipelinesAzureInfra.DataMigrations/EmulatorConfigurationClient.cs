using Azure;
using Azure.Core;
using Azure.Data.AppConfiguration;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AzdPipelinesAzureInfra.DataMigrations;

/// <summary>
/// A client that emulates the <see cref="ConfigurationClient"/> for use with the Azure App Configuration emulator.
/// This client communicates with the emulator via HTTP REST API since the standard ConfigurationClient 
/// doesn't support anonymous authentication over HTTP.
/// </summary>
internal class EmulatorConfigurationClient : IConfgurationClient, IDisposable
{
    private const string ApiVersion = "2023-11-01";
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    public EmulatorConfigurationClient(string connectionString, HttpClient httpClient)
    {
        var emulatorUri = ParseEndpointFromConnectionString(connectionString);
        httpClient.BaseAddress = emulatorUri;
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient = httpClient;
        _ownsHttpClient = false;
    }

    /// <summary>
    /// Parses the Endpoint from an Azure App Configuration connection string.
    /// Connection string format: Endpoint=http://localhost:8483;Id=...;Secret=...
    /// </summary>
    private static Uri ParseEndpointFromConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        }

        // Parse key=value pairs from connection string
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2 &&
                keyValue[0].Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(keyValue[1], UriKind.Absolute, out var uri))
                {
                    return uri;
                }
                throw new ArgumentException($"Invalid Endpoint URI in connection string: {keyValue[1]}", nameof(connectionString));
            }
        }

        throw new ArgumentException("Connection string does not contain an Endpoint.", nameof(connectionString));
    }

    /// <summary>
    /// Gets configuration settings matching the specified selector.
    /// </summary>
    public async IAsyncEnumerable<ConfigurationSetting> GetConfigurationSettingsAsync(
        SettingSelector selector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var keyFilter = selector.KeyFilter ?? "*";
        var labelFilter = selector.LabelFilter ?? "*";

        var encodedKey = Uri.EscapeDataString(keyFilter);
        var encodedLabel = Uri.EscapeDataString(labelFilter);

        var url = $"/kv?key={encodedKey}&label={encodedLabel}&api-version={ApiVersion}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<KeyValueListResponse>(cancellationToken: cancellationToken);

        if (result?.Items is not null)
        {
            foreach (var item in result.Items)
            {
                yield return ToConfigurationSetting(item);
            }
        }
    }

    /// <summary>
    /// Gets a specific configuration setting by key and label.
    /// </summary>
    public async Task<Response<ConfigurationSetting>> GetConfigurationSettingAsync(
        string key,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        var encodedKey = Uri.EscapeDataString(key);
        var url = string.IsNullOrEmpty(label)
            ? $"/kv/{encodedKey}?api-version={ApiVersion}"
            : $"/kv/{encodedKey}?label={Uri.EscapeDataString(label)}&api-version={ApiVersion}";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new RequestFailedException((int)response.StatusCode, $"Configuration setting with key '{key}' not found.");
        }

        response.EnsureSuccessStatusCode();

        var item = await response.Content.ReadFromJsonAsync<KeyValueItem>(cancellationToken: cancellationToken);
        var setting = ToConfigurationSetting(item!);

        return Response.FromValue(setting, new EmulatorResponse(response));
    }

    /// <summary>
    /// Sets a configuration setting, creating it if it doesn't exist or updating if it does.
    /// </summary>
    public async Task<Response<ConfigurationSetting>> SetConfigurationSettingAsync(
        ConfigurationSetting setting,
        bool onlyIfUnchanged = false,
        CancellationToken cancellationToken = default)
    {
        var encodedKey = Uri.EscapeDataString(setting.Key);
        var url = string.IsNullOrEmpty(setting.Label)
            ? $"/kv/{encodedKey}?api-version={ApiVersion}"
            : $"/kv/{encodedKey}?label={Uri.EscapeDataString(setting.Label)}&api-version={ApiVersion}";

        var payload = new KeyValuePayload
        {
            Value = setting.Value,
            ContentType = setting.ContentType,
            Tags = setting.Tags?.ToDictionary(t => t.Key, t => t.Value)
        };

        var content = JsonContent.Create(payload);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.microsoft.appconfig.kv+json");

        HttpRequestMessage request = new(HttpMethod.Put, url)
        {
            Content = content
        };

        if (onlyIfUnchanged && !string.IsNullOrEmpty(setting.ETag.ToString()))
        {
            request.Headers.Add("If-Match", $"\"{setting.ETag}\"");
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var item = await response.Content.ReadFromJsonAsync<KeyValueItem>(cancellationToken: cancellationToken);
        var resultSetting = ToConfigurationSetting(item!);

        return Response.FromValue(resultSetting, new EmulatorResponse(response));
    }

    /// <summary>
    /// Sets a configuration setting by key, value, and optional label.
    /// </summary>
    public Task<Response<ConfigurationSetting>> SetConfigurationSettingAsync(
        string key,
        string value,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        var setting = new ConfigurationSetting(key, value, label);
        return SetConfigurationSettingAsync(setting, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Adds a new configuration setting. Fails if the setting already exists.
    /// </summary>
    public async Task<Response<ConfigurationSetting>> AddConfigurationSettingAsync(
        ConfigurationSetting setting,
        CancellationToken cancellationToken = default)
    {
        var encodedKey = Uri.EscapeDataString(setting.Key);
        var url = string.IsNullOrEmpty(setting.Label)
            ? $"/kv/{encodedKey}?api-version={ApiVersion}"
            : $"/kv/{encodedKey}?label={Uri.EscapeDataString(setting.Label)}&api-version={ApiVersion}";

        var payload = new KeyValuePayload
        {
            Value = setting.Value,
            ContentType = setting.ContentType,
            Tags = setting.Tags?.ToDictionary(t => t.Key, t => t.Value)
        };

        var content = JsonContent.Create(payload);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.microsoft.appconfig.kv+json");

        HttpRequestMessage request = new(HttpMethod.Put, url)
        {
            Content = content
        };

        // If-None-Match: * means only create if it doesn't exist
        request.Headers.Add("If-None-Match", "*");

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new RequestFailedException((int)response.StatusCode, $"Configuration setting with key '{setting.Key}' already exists.");
        }

        response.EnsureSuccessStatusCode();

        var item = await response.Content.ReadFromJsonAsync<KeyValueItem>(cancellationToken: cancellationToken);
        var resultSetting = ToConfigurationSetting(item!);

        return Response.FromValue(resultSetting, new EmulatorResponse(response));
    }

    /// <summary>
    /// Adds a new configuration setting by key, value, and optional label.
    /// </summary>
    public Task<Response<ConfigurationSetting>> AddConfigurationSettingAsync(
        string key,
        string value,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        var setting = new ConfigurationSetting(key, value, label);
        return AddConfigurationSettingAsync(setting, cancellationToken);
    }

    /// <summary>
    /// Deletes a configuration setting.
    /// </summary>
    public async Task<Response> DeleteConfigurationSettingAsync(
        string key,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        var encodedKey = Uri.EscapeDataString(key);
        var url = string.IsNullOrEmpty(label)
            ? $"/kv/{encodedKey}?api-version={ApiVersion}"
            : $"/kv/{encodedKey}?label={Uri.EscapeDataString(label)}&api-version={ApiVersion}";

        var response = await _httpClient.DeleteAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return new EmulatorResponse(response);
    }

    /// <summary>
    /// Deletes a configuration setting.
    /// </summary>
    public Task<Response> DeleteConfigurationSettingAsync(
        ConfigurationSetting setting,
        bool onlyIfUnchanged = false,
        CancellationToken cancellationToken = default)
    {
        return DeleteConfigurationSettingAsync(setting.Key, setting.Label, cancellationToken);
    }

    private static ConfigurationSetting ToConfigurationSetting(KeyValueItem item)
    {
        var setting = new ConfigurationSetting(item.Key, item.Value, item.Label)
        {
            ContentType = item.ContentType
        };

        if (item.Tags is not null)
        {
            foreach (var tag in item.Tags)
            {
                setting.Tags[tag.Key] = tag.Value;
            }
        }

        return setting;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }
            _disposed = true;
        }
    }

    #region JSON models for emulator REST API

    private sealed class KeyValueListResponse
    {
        [JsonPropertyName("items")]
        public List<KeyValueItem>? Items { get; set; }
    }

    private sealed class KeyValueItem
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }

        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        [JsonPropertyName("locked")]
        public bool Locked { get; set; }

        [JsonPropertyName("last_modified")]
        public DateTimeOffset? LastModified { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }

    private sealed class KeyValuePayload
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }

    #endregion

    #region EmulatorResponse wrapper

    /// <summary>
    /// A minimal Response implementation to wrap HttpResponseMessage for Azure SDK compatibility.
    /// </summary>
    private sealed class EmulatorResponse : Response
    {
        private readonly HttpResponseMessage _response;

        public EmulatorResponse(HttpResponseMessage response)
        {
            _response = response;
        }

        public override int Status => (int)_response.StatusCode;

        public override string ReasonPhrase => _response.ReasonPhrase ?? string.Empty;

        public override Stream? ContentStream
        {
            get => _response.Content.ReadAsStream();
            set => throw new NotSupportedException();
        }

        public override string ClientRequestId
        {
            get => _response.RequestMessage?.Headers.TryGetValues("x-ms-client-request-id", out var values) == true
                ? values.FirstOrDefault() ?? string.Empty
                : string.Empty;
            set => throw new NotSupportedException();
        }

        public override void Dispose() => _response.Dispose();

        protected override bool ContainsHeader(string name) =>
            _response.Headers.Contains(name) || _response.Content.Headers.Contains(name);

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            foreach (var header in _response.Headers)
            {
                yield return new HttpHeader(header.Key, string.Join(",", header.Value));
            }
            foreach (var header in _response.Content.Headers)
            {
                yield return new HttpHeader(header.Key, string.Join(",", header.Value));
            }
        }

        protected override bool TryGetHeader(string name, out string? value)
        {
            if (_response.Headers.TryGetValues(name, out var values) ||
                _response.Content.Headers.TryGetValues(name, out values))
            {
                value = string.Join(",", values);
                return true;
            }
            value = null;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string>? values)
        {
            if (_response.Headers.TryGetValues(name, out values) ||
                _response.Content.Headers.TryGetValues(name, out values))
            {
                return true;
            }
            values = null;
            return false;
        }
    }

    #endregion
}
