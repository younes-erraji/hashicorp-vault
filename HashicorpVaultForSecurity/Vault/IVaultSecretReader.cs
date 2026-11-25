namespace HashicorpVaultForSecurity.Vault;

public interface IVaultSecretReader
{
    Task<IReadOnlyDictionary<string, object>> ReadSecretAsync(CancellationToken cancellationToken = default);
    Task<string> ReadSecretValueAsync(string key, CancellationToken cancellationToken = default);
    Task<T> ReadSecretValueAsync<T>(string key, CancellationToken cancellationToken = default);
}
