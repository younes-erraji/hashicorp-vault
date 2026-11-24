# HashiCorp Vault for Security — ASP.NET Core Demo

A small ASP.NET Core 9 Web API that reads secrets from **HashiCorp Vault** using the
[VaultSharp](https://github.com/rajanadar/VaultSharp) client, with a Docker Compose
setup that spins up a dev-mode Vault and seeds a demo secret so you can test end to end.

---

## Architecture at a glance

| Component | File | Responsibility |
|-----------|------|----------------|
| `VaultSettings` | [Vault/VaultSettings.cs](HashicorpVaultForSecurity/Vault/VaultSettings.cs) | Strongly-typed config bound from the `Vault` section |
| `IVaultSecretReader` | [Vault/IVaultSecretReader.cs](HashicorpVaultForSecurity/Vault/IVaultSecretReader.cs) | Abstraction for reading secrets |
| `VaultSecretReader` | [Vault/VaultSecretReader.cs](HashicorpVaultForSecurity/Vault/VaultSecretReader.cs) | VaultSharp client — token auth + KV v2 reads |
| `SecretsController` | [Controllers/SecretsController.cs](HashicorpVaultForSecurity/Controllers/SecretsController.cs) | HTTP endpoints exposing the reader |

The reader is registered as a singleton in
[Program.cs](HashicorpVaultForSecurity/Program.cs) and the `Vault` config section is
bound to `VaultSettings`.

---

## Configuration

The `Vault` section (in `appsettings.json`, or overridden via environment variables):

```json
{
  "Vault": {
    "Address": "http://127.0.0.1:8200",
    "Token": "",
    "MountPoint": "secret",
    "SecretPath": "myapp/config"
  }
}
```

| Setting | Meaning |
|---------|---------|
| `Address` | Vault server URL |
| `Token` | Token used to authenticate (dev/testing only — see security note) |
| `MountPoint` | Mount point of the KV v2 secrets engine (dev default: `secret`) |
| `SecretPath` | Path of the secret within that mount |

Environment variables override config using the `__` separator, e.g.
`Vault__Token=root`. This is how the token is injected at runtime instead of being
committed to source.

---

## How secret reading works

Vault's **KV v2** engine stores a secret as a single JSON object at a path. There is no
server-side way to fetch just one field — a read always returns the whole object — so the
"pick one key" step happens client-side. VaultSharp deserializes each value with
`System.Text.Json`, meaning every value arrives as a `JsonElement`. That lets us
deserialize structured values (arrays/objects) into real C# types.

### The three reader methods

```csharp
// 1. Read the entire secret as a dictionary of key -> value.
Task<IReadOnlyDictionary<string, object>> ReadSecretAsync(CancellationToken ct = default);

// 2. Read a single key as a string. Throws if the key is missing or blank.
Task<string> ReadSecretValueAsync(string key, CancellationToken ct = default);

// 3. Read a single key and deserialize it into T.
//    Works for scalars, arrays, and objects. Throws if the key is missing.
Task<T> ReadSecretValueAsync<T>(string key, CancellationToken ct = default);
```

### Example

Given this secret stored at `secret/myapp/config`:

```json
{
  "ApiKey": "abc123",
  "AllowedHosts": ["a.com", "b.com"],
  "Smtp": { "Host": "mail.example.com", "Port": 587 }
}
```

you can read it in any of these shapes:

```csharp
// whole secret
IReadOnlyDictionary<string, object> all = await reader.ReadSecretAsync();

// a scalar as a string
string apiKey = await reader.ReadSecretValueAsync("ApiKey");

// a scalar deserialized to a concrete type
string apiKeyTyped = await reader.ReadSecretValueAsync<string>("ApiKey");

// an array
List<string> hosts = await reader.ReadSecretValueAsync<List<string>>("AllowedHosts");

// an object -> your own POCO
SmtpOptions smtp = await reader.ReadSecretValueAsync<SmtpOptions>("Smtp");

public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
}
```

> **Note on casing:** `System.Text.Json` matches property names case-sensitively by
> default. If your JSON keys and C# property names differ in case, configure the
> deserializer with `PropertyNameCaseInsensitive = true`.

---

## HTTP endpoints

| Method | Route | Returns |
|--------|-------|---------|
| `GET` | `/secrets` | The entire secret object |
| `GET` | `/secrets/string/{key}` | A single value as a string |
| `GET` | `/secrets/object/{key}` | A single value deserialized as an object (arrays/objects included) |

All endpoints return `502 Bad Gateway` with an error message if Vault can't be reached
or the key is missing.

---

## Running with Docker Compose

The [docker-compose.yml](docker-compose.yml) starts:

- **`vault`** — HashiCorp Vault in dev mode (in-memory, auto-unsealed, KV v2 mounted at
  `secret/`), root token `root`, exposed on port `8200`.
- **`vault-init`** — a one-shot job that waits for Vault, writes a demo secret to
  `secret/myapp/config`, then exits (see [vault-init.sh](vault-init.sh)).

### Start it

```powershell
docker compose up
```

### Verify the seeded secret

```powershell
docker exec -e VAULT_ADDR=http://127.0.0.1:8200 -e VAULT_TOKEN=root vault-dev `
  vault kv get secret/myapp/config
```

Vault UI/API: <http://localhost:8200> (token: `root`).

### Tear down

```powershell
docker compose down
```

### Running the app against this Vault

When running the API from Visual Studio / `dotnet run`, point it at the dockerized Vault
and supply the token (the default `appsettings.json` token is intentionally blank):

```powershell
$env:Vault__Token = "root"
dotnet run --project HashicorpVaultForSecurity
```

Then call an endpoint, e.g.:

```powershell
curl http://localhost:<port>/secrets
```

---

## Security notes

- **Token auth is for local/dev testing only.** In production use **AppRole**,
  Kubernetes auth, or another dynamic auth method — never a static root token.
- **Never commit a real token.** The token is injected via the `Vault__Token`
  environment variable; `appsettings.json` ships with an empty value.
- **Dev-mode Vault is in-memory and auto-unsealed** — all data is lost on restart. Great
  for a test harness, unsuitable for anything real.
