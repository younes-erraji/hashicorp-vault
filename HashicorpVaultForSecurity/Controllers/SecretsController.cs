using HashicorpVaultForSecurity.Vault;
using Microsoft.AspNetCore.Mvc;

namespace HashicorpVaultForSecurity.Controllers;

[ApiController]
[Route("[controller]")]
public class SecretsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IVaultSecretReader _secretReader;

    public SecretsController(IVaultSecretReader secretReader, IConfiguration configuration)
    {
        _configuration = configuration;
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

    [HttpGet("configurations/{key}")]
    public IActionResult GetConfigurations([FromRoute] string key, CancellationToken cancellationToken)
    {
        string? secret = _configuration[key];
        string? connectionString = _configuration.GetConnectionString("Default");
        SmtpOptions? smtp = _configuration.GetSection("Smtp").Get<SmtpOptions>();
        string[]? hosts = _configuration.GetSection("CorsOrigins").Get<string[]>();

        return Ok(new { secret, connectionString, smtp, hosts });
    }
}

public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public bool UseSsl { get; set; }
}
