using System.ComponentModel.DataAnnotations;

namespace JwtApi.DTOs
{
    public class ContactFormDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required, DataType(DataType.EmailAddress), EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
