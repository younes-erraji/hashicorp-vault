using HashicorpVaultForSecurity.Vault;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    VaultSettings? vaultSettings = builder.Configuration
        .GetSection(VaultSettings.SectionName)
        .Get<VaultSettings>();

    ArgumentNullException.ThrowIfNull(vaultSettings, nameof(vaultSettings));

    await builder.Configuration.AddVaultConfigurationAsync(vaultSettings);
}

WebApplication app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
