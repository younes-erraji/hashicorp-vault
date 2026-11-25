using System.Text.Json;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

namespace HashicorpVaultForSecurity.Vault
{
    public class VaultSecretReader : IVaultSecretReader
    {
        private readonly VaultSettings _settings;
        private readonly IVaultClient _client;

        public VaultSecretReader(IOptions<VaultSettings> settings)
        {
            _settings = settings.Value;

            IAuthMethodInfo authMethod = new TokenAuthMethodInfo(_settings.Token);
            VaultClientSettings clientSettings = new VaultClientSettings(_settings.Address, authMethod);
            _client = new VaultClient(clientSettings);
        }

        public async Task<IReadOnlyDictionary<string, object>> ReadSecretAsync(CancellationToken cancellationToken = default)
        {
            Secret<SecretData> secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: _settings.SecretPath,
                mountPoint: _settings.MountPoint);

            return new Dictionary<string, object>(secret.Data.Data);
        }

        public async Task<string> ReadSecretValueAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(key, nameof(key));

            IReadOnlyDictionary<string, object> secrets = await ReadSecretAsync(cancellationToken);
            string? secretValue = secrets.TryGetValue(key, out var value) ? value?.ToString() : null;
            ArgumentNullException.ThrowIfNullOrWhiteSpace(secretValue, key);
            return secretValue;
        }

        public async Task<T> ReadSecretValueAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(key, nameof(key));

            IReadOnlyDictionary<string, object> secrets = await ReadSecretAsync(cancellationToken);
            string? secretValue = secrets.TryGetValue(key, out object? value) ? value?.ToString() : null;
            ArgumentNullException.ThrowIfNullOrWhiteSpace(secretValue, key);

            if (value is JsonElement element)
            {
                return element.Deserialize<T>();
            }

            if (value is T typed)
            {
                return typed;
            }

            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
        }
    }
}
