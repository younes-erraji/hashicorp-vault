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

        // The client is built lazily because OIDC login is async and may open a browser.
        // For Token auth the login step is instant; for OIDC it runs once on first use.
        private readonly SemaphoreSlim _clientLock = new SemaphoreSlim(1, 1);
        private IVaultClient? _client;

        /// <summary>
        /// Creates a reader that connects to Vault using the given settings.
        /// </summary>
        /// <param name="settings">The Vault address, auth method, mount point and secret path.</param>
        public VaultSecretReader(VaultSettings settings)
        {
            _settings = settings;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<string, object>> ReadSecretAsync(CancellationToken cancellationToken = default)
        {
            IVaultClient client = await GetClientAsync(cancellationToken);

            Secret<SecretData> secret = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: _settings.SecretPath,
                mountPoint: _settings.MountPoint);

            return new Dictionary<string, object>(secret.Data.Data);
        }

        /// <summary>
        /// Gets the Vault client, creating it (and logging in, if using OIDC) the first time.
        /// </summary>
        private async Task<IVaultClient> GetClientAsync(CancellationToken cancellationToken)
        {
            if (_client is not null)
            {
                return _client;
            }

            await _clientLock.WaitAsync(cancellationToken);
            try
            {
                if (_client is null)
                {
                    IAuthMethodInfo authMethod = await CreateAuthMethodAsync(cancellationToken);
                    VaultClientSettings clientSettings = new VaultClientSettings(_settings.Address, authMethod)
                    {
                        Namespace = _settings.Namespace,
                    };
                    _client = new VaultClient(clientSettings);
                }
            }
            finally
            {
                _clientLock.Release();
            }

            return _client;
        }

        /// <summary>
        /// Builds the Vault auth method based on <see cref="VaultSettings.AuthMethod"/>.
        /// For OIDC this runs the interactive browser login and uses the resulting token.
        /// </summary>
        private async Task<IAuthMethodInfo> CreateAuthMethodAsync(CancellationToken cancellationToken)
        {
            switch (_settings.AuthMethod)
            {
                case VaultAuthMethod.Oidc:
                    string oidcToken = await VaultOidcLogin.LoginAsync(
                        _settings.Address, _settings.Oidc, _settings.Namespace, cancellationToken);
                    return new TokenAuthMethodInfo(oidcToken);

                case VaultAuthMethod.Token:
                default:
                    ArgumentException.ThrowIfNullOrWhiteSpace(_settings.Token, nameof(_settings.Token));
                    return new TokenAuthMethodInfo(_settings.Token);
            }
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
