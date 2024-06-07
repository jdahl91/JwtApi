using JwtApi.Models;

namespace JwtApi.Responses
{
    public class CustomResponses
    {
        public record ApiResponse(bool Flag = false, string Message = null!, object payload = null!);
        public record RegistrationResponse(bool Flag = false, string Message = null!);
        public record LoginResponse(bool Flag = false, string Message = null!, string JwtToken = null!, string RefreshToken = null!);
        public record GetAllPasswordsApiResonse(bool Flag = false, string Message = null!, List<PasswordEntry> PasswordEntries = null!);
    }
}
