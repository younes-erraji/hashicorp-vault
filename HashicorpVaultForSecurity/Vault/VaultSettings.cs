namespace HashicorpVaultForSecurity.Vault
{
    public class VaultSettings
    {
        public const string SectionName = "Vault";

        public string Address { get; set; }
        public string Token { get; set; }
        public string MountPoint { get; set; }
        public string SecretPath { get; set; }
    }
}
