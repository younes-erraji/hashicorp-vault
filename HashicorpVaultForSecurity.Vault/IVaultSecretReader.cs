namespace HashicorpVaultForSecurity.Vault;

/// <summary>
/// Reads secrets from HashiCorp Vault.
/// </summary>
public interface IVaultSecretReader
{
    /// <summary>
    /// Reads all key/value pairs stored at the configured secret path.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A dictionary of every key and value found in the secret.</returns>
    Task<IReadOnlyDictionary<string, object>> ReadSecretAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a single value from the secret and returns it as a string.
    /// </summary>
    /// <param name="key">The name of the value to read.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The value stored under <paramref name="key"/>.</returns>
    Task<string> ReadSecretValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a single value from the secret and converts it to the type you ask for.
    /// </summary>
    /// <typeparam name="T">The type to convert the value into.</typeparam>
    /// <param name="key">The name of the value to read.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The value stored under <paramref name="key"/>, converted to <typeparamref name="T"/>.</returns>
    Task<T> ReadSecretValueAsync<T>(string key, CancellationToken cancellationToken = default);
}
