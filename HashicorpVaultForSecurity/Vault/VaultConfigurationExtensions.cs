using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace HashicorpVaultForSecurity.Vault;

public static class VaultConfigurationExtensions
{
    public static async Task AddVaultConfigurationAsync(this WebApplicationBuilder builder)
    {
        VaultSettings settings = builder.Configuration
            .GetSection(VaultSettings.SectionName)
            .Get<VaultSettings>()
            ?? throw new InvalidOperationException(
                $"Missing '{VaultSettings.SectionName}' configuration section.");

        IVaultSecretReader reader = new VaultSecretReader(Options.Create(settings));

        IReadOnlyDictionary<string, object> secret = await reader.ReadSecretAsync();

        var asConfig = new Dictionary<string, string?>();
        foreach (var kvp in secret)
        {
            Flatten(kvp.Key, kvp.Value, asConfig);
        }

        builder.Configuration.AddInMemoryCollection(asConfig);
    }

    private static void Flatten(string prefix, object? value, IDictionary<string, string?> target)
    {
        switch (value)
        {
            case JObject obj:
                foreach (var property in obj.Properties())
                {
                    Flatten($"{prefix}:{property.Name}", property.Value, target);
                }
                return;

            case JArray array:
                for (int i = 0; i < array.Count; i++)
                {
                    Flatten($"{prefix}:{i}", array[i], target);
                }
                return;

            case JValue jValue:
                // Scalar wrapped in a JToken (e.g. a nested string/number/bool).
                target[prefix] = jValue.Value?.ToString();
                return;

            default:
                // Already a plain CLR scalar (string/number/bool) or null.
                target[prefix] = value?.ToString();
                return;
        }
    }
}
