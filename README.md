# HashiCorp Vault for .NET

This solution loads application secrets from HashiCorp Vault into the application configuration,
so our code reads them through IConfiguration like any other setting.
without needing to change anything at all in the code so that No secrets live in the source code.

---

## The repo consist of two projects

| --- | --- |
| **`HashicorpVaultForSecurity.Vault`** | The reusable class library. This is the part that we will be using with other projects. |
| **`HashicorpVaultForSecurity`** | A small web API demo that shows how to implement the library. |

---

## What the library gives you

- **`VaultSettings`** — the connection details (where Vault is, how to authenticate, which secrets path to read).
(Note: these configs will be stored in user secrets afterwards)
- **`IVaultSecretReader` / `VaultSecretReader`** — reads secrets from Vault directly.
- **`AddVaultConfigurationAsync(...)`** — the one call that pulls your secrets into configuration.

---

## How to use it (3 steps)

### 1. Add your Vault settings to `appsettings.json`

```json
{
  "Vault": {
    "Address": "http://127.0.0.1:8200",
    "Token": "root",
    "Namespace": null,
    "MountPoint": "secret",
    "SecretPath": "myapp/config"
  }
}
```

| Setting | What it means | Example |
| --- | --- | --- |
| `Address` | The Vault server URL. | `https://vault.mycompany.com:8200` |
| `Token` | The token used to log in to Vault. | `hvs.CAESxxxx` (or `root` in dev) |
| `Namespace` | Vault namespace (Enterprise). | `blc` |
| `MountPoint` | Where the KV secrets engine is mounted. | `secret` |
| `SecretPath` | The path of your secret inside that mount. | `myapp/config` |

as for How it works

### 2. the applications Load the secrets at startup within Program.cs for example

```csharp
using HashicorpVaultForSecurity.Vault;

var builder = WebApplication.CreateBuilder(args);

// Read the "Vault" section from configuration.
var vaultSettings = builder.Configuration
    .GetSection(VaultSettings.SectionName)
    .Get<VaultSettings>();

ArgumentNullException.ThrowIfNull(vaultSettings);

// Pull the secrets from Vault into configuration.
await builder.Configuration.AddVaultConfigurationAsync(vaultSettings);

var app = builder.Build();
app.Run();
```

### 3. as for how to read these secrets, we can Read the secrets anywhere, like normal config since your secrets are now part of `IConfiguration`.

```csharp
// If your Vault secret has a key "secretKey":
string? secretKey = configuration["secretKey"];

// Nested values are flattened with ':' automatically.
// A secret like { "Smtp": { "Host": "..." } } becomes:
string? smtpHost = configuration["Smtp:Host"];
```

---
