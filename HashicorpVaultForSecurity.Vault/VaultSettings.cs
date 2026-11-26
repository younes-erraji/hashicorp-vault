namespace HashicorpVaultForSecurity.Vault;

/// <summary>
/// The settings needed to connect to HashiCorp Vault and locate a secret.
/// Usually bound from the "Vault" section of your configuration.
/// </summary>
public class VaultSettings
{
    /// <summary>
    /// The name of the configuration section these settings are read from (<c>"Vault"</c>).
    /// </summary>
    public const string SectionName = "Vault";

    /// <summary>
    /// The base URL of the Vault server, e.g. <c>https://vault.mycompany.com:8200</c>.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// The Vault token used to authenticate the request.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// The path where the KV (key-value) secrets engine is mounted, e.g. <c>"secret"</c>.
    /// </summary>
    public string MountPoint { get; set; }

    /// <summary>
    /// The path of the secret within the mount, e.g. <c>"myapp/config"</c>.
    /// </summary>
    public string SecretPath { get; set; }
}
