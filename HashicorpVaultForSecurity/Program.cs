using HashicorpVaultForSecurity.Vault;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<VaultSettings>(
    builder.Configuration.GetSection(VaultSettings.SectionName));
builder.Services.AddSingleton<IVaultSecretReader, VaultSecretReader>();

WebApplication app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
