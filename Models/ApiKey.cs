namespace PromptForge.Api.Models
{
    public class ApiKey
    {
        public Guid Id { get; set; }
        public string? Provider { get; set; }
        public string? EncryptedKey { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
