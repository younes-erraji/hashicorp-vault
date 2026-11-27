using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

namespace HashicorpVaultForSecurity.Vault
{
    /// <summary>
    /// Reads secrets from a HashiCorp Vault KV (key-value) version 2 secrets engine.
    /// </summary>
    public class VaultSecretReader : IVaultSecretReader
    {
        private readonly VaultSettings _settings;
        private readonly IVaultClient _client;

        /// <summary>
        /// Creates a reader that connects to Vault using the given settings.
        /// </summary>
        /// <param name="settings">The Vault address, token, mount point and secret path.</param>
        public VaultSecretReader(VaultSettings settings)
        {
            _settings = settings;

            IAuthMethodInfo authMethod = new TokenAuthMethodInfo(_settings.Token);
            VaultClientSettings clientSettings = new VaultClientSettings(_settings.Address, authMethod)
            {
                Namespace = _settings.Namespace,
            };
            _client = new VaultClient(clientSettings);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, object>> ReadSecretAsync(CancellationToken cancellationToken = default)
        {
            Secret<SecretData> secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: _settings.SecretPath,
                mountPoint: _settings.MountPoint);

            return new Dictionary<string, object>(secret.Data.Data);
        }

        /// <inheritdoc />
        public async Task<string> ReadSecretValueAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(key, nameof(key));

            IReadOnlyDictionary<string, object> secrets = await ReadSecretAsync(cancellationToken);
            string? secretValue = secrets.TryGetValue(key, out object? value) ? value?.ToString() : null;
            ArgumentNullException.ThrowIfNullOrWhiteSpace(secretValue, key);
            return secretValue;
        }

        /// <inheritdoc />
        public async Task<T> ReadSecretValueAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(key, nameof(key));

            IReadOnlyDictionary<string, object> secrets = await ReadSecretAsync(cancellationToken);
            string? secretValue = secrets.TryGetValue(key, out object? value) ? value?.ToString() : null;
            ArgumentNullException.ThrowIfNullOrWhiteSpace(secretValue, key);

            if (value is JToken token)
            {
                return token.ToObject<T>()!;
            }

            if (value is T typed)
            {
                return typed;
            }

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value))!;
        }
    }
}
