using HashicorpVaultForSecurity.Vault;
using Microsoft.AspNetCore.Mvc;

namespace HashicorpVaultForSecurity.Controllers;

[ApiController]
[Route("[controller]")]
public class SecretsController : ControllerBase
{
    private readonly IVaultSecretReader _secretReader;

    public SecretsController(IVaultSecretReader secretReader)
    {
        _secretReader = secretReader;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyDictionary<string, object> secret = await _secretReader.ReadSecretAsync(cancellationToken);
            return Ok(secret);
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = "Could not read secret from Vault.", detail = ex.Message });
        }
    }

    [HttpGet("string/{key}")]
    public async Task<IActionResult> GetString([FromRoute] string key, CancellationToken cancellationToken)
    {
        try
        {
            string secret = await _secretReader.ReadSecretValueAsync(key, cancellationToken);
            return Ok(secret);
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = "Could not read secret from Vault.", detail = ex.Message });
        }
    }

    [HttpGet("object/{key}")]
    public async Task<IActionResult> GetObject([FromRoute] string key, CancellationToken cancellationToken)
    {
        try
        {
            object secret = await _secretReader.ReadSecretValueAsync<object>(key, cancellationToken);
            return Ok(secret);
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = "Could not read secret from Vault.", detail = ex.Message });
        }
    }
}
