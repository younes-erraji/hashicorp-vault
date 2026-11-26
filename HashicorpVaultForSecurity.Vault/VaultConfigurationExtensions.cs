using Microsoft.Extensions.Configuration;

using Newtonsoft.Json.Linq;

namespace HashicorpVaultForSecurity.Vault;

/// <summary>
/// Helpers for pulling secrets out of HashiCorp Vault and into the app's configuration.
/// </summary>
public static class VaultConfigurationExtensions
{
    /// <summary>
    /// Reads every secret at the configured Vault path and adds them to configuration,
    /// so the rest of the app can read them through <see cref="IConfiguration"/> like any other setting.
    /// Nested values are flattened using ':' (for example <c>Smtp:Host</c>).
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to add the secrets to.</param>
    /// <param name="vaultSettings">The Vault connection settings.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    public static async Task AddVaultConfigurationAsync(
        this IConfigurationBuilder configurationBuilder,
        VaultSettings vaultSettings,
        CancellationToken cancellationToken = default)
    {
        IVaultSecretReader reader = new VaultSecretReader(vaultSettings);

        IReadOnlyDictionary<string, object> secret = await reader.ReadSecretAsync(cancellationToken);

        Dictionary<string, string?> asConfig = new Dictionary<string, string?>();
        foreach (KeyValuePair<string, object> pair in secret)
        {
            Flatten(pair.Key, pair.Value, asConfig);
        }

        configurationBuilder.AddInMemoryCollection(asConfig);
    }

    /// <summary>
    /// Turns a single secret value into one or more flat configuration entries.
    /// Handles JSON values specially; anything else is stored as its string form.
    /// </summary>
    private static void Flatten(string prefix, object? value, IDictionary<string, string?> target)
    {
        switch (value)
        {
            case JToken token:
                FlattenToken(prefix, token, target);
                return;

            default:
                // Plain CLR scalar (string/number/bool) or null.
                target[prefix] = value?.ToString();
                return;
        }
    }

    /// <summary>
    /// Walks a JSON value and writes each scalar it finds into <paramref name="target"/>.
    /// Objects and arrays are expanded recursively, building up ':'-separated keys.
    /// </summary>
    private static void FlattenToken(string prefix, JToken token, IDictionary<string, string?> target)
    {
        switch (token)
        {
            case JObject obj:
                foreach (JProperty property in obj.Properties())
                {
                    FlattenToken($"{prefix}:{property.Name}", property.Value, target);
                }
                return;

            case JArray array:
                for (int index = 0; index < array.Count; index++)
                {
                    FlattenToken($"{prefix}:{index}", array[index], target);
                }
                return;

            case JValue { Value: null }:
                target[prefix] = null;
                return;

            default:
                // Scalar value (string/number/bool).
                target[prefix] = token.ToString();
                return;
        }
    }
}
