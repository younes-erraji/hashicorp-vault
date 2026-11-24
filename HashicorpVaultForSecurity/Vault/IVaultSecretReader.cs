namespace HashicorpVaultForSecurity.Vault
{
    public interface IVaultSecretReader
    {
        /// <summary>Reads all key/value pairs stored at the configured KV v2 secret path.</summary>
        Task<IReadOnlyDictionary<string, object>> ReadSecretAsync(CancellationToken cancellationToken = default);

        /// <summary>Reads a single value by key from the configured KV v2 secret path. Returns null if the key is absent.</summary>
        Task<string> ReadSecretValueAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads a single value by key and deserializes it to <typeparamref name="T"/>.
        /// Supports scalars (string/int/bool), arrays (e.g. List&lt;string&gt;), and objects (your own POCO).
        /// Returns default(T) if the key is absent.
        /// </summary>
        Task<T> ReadSecretValueAsync<T>(string key, CancellationToken cancellationToken = default);
    }
}
