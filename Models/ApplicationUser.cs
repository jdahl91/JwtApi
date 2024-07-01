namespace JwtApi.Models
{
    public class ApplicationUser
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        // Change this back to false when email service is fixed
        public bool IsEmailConfirmed { get; set; } = true;
    }
}