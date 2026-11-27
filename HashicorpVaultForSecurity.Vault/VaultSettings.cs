namespace HashicorpVaultForSecurity.Vault;

/// <summary>
/// How the app authenticates to Vault.
/// </summary>
public enum VaultAuthMethod
{
    /// <summary>
    /// Use a static token supplied in <see cref="VaultSettings.Token"/>.
    /// </summary>
    Token = 0,

    /// <summary>
    /// Log in interactively through an OIDC provider. Opens the user's browser,
    /// the user signs in with their own credentials, and Vault returns a token.
    /// Suitable for CLI / desktop / local tools, not for a hosted server.
    /// </summary>
    Oidc = 1,
}

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
    /// How to authenticate to Vault. Defaults to <see cref="VaultAuthMethod.Token"/>.
    /// </summary>
    public VaultAuthMethod AuthMethod { get; set; } = VaultAuthMethod.Token;

    /// <summary>
    /// The Vault token used to authenticate the request.
    /// Only used when <see cref="AuthMethod"/> is <see cref="VaultAuthMethod.Token"/>.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// OIDC login settings. Only used when <see cref="AuthMethod"/> is <see cref="VaultAuthMethod.Oidc"/>.
    /// </summary>
    public VaultOidcSettings Oidc { get; set; } = new VaultOidcSettings();

    /// <summary>
    /// The Vault namespace (Enterprise / HCP), e.g. <c>"younes"</c>.
    /// Leave null or empty for open-source Vault, which has no namespaces.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// The path where the KV (key-value) secrets engine is mounted, e.g. <c>"secret"</c>.
    /// </summary>
    public string MountPoint { get; set; }

    /// <summary>
    /// The path of the secret within the mount, e.g. <c>"myapp/config"</c>.
    /// </summary>
    public string SecretPath { get; set; }
}

/// <summary>
/// Settings for the interactive OIDC login flow.
/// </summary>
public class VaultOidcSettings
{
    /// <summary>
    /// The path the OIDC auth method is mounted at in Vault. Defaults to <c>"oidc"</c>.
    /// </summary>
    public string MountPoint { get; set; } = "oidc";

    /// <summary>
    /// The Vault OIDC role to log in with. Leave null to use the backend's default role.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// The local address Vault redirects back to after the user signs in.
    /// Must also be listed in the role's <c>allowed_redirect_uris</c> in Vault.
    /// Defaults to <c>http://localhost:8250/oidc/callback</c> (the same default the Vault CLI uses).
    /// </summary>
    public string RedirectUri { get; set; } = "http://localhost:8250/oidc/callback";
}
