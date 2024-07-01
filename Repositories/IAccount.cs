using JwtApi.DTOs;
using JwtApi.Models;
using static JwtApi.Responses.CustomResponses;

namespace JwtApi.Repositories
{
    public interface IAccount
    {
        Task<RegistrationResponse> RegisterAsync(RegisterDTO model);
        Task<LoginResponse> LoginAsync(LoginDTO model);
        Task<RegistrationResponse> LogoutAsync(string expiredAccessToken);
        Task<LoginResponse> ObtainNewAccessToken(string expiredAccessToken, string refreshToken);
        Task<RegistrationResponse> ConfirmEmail(string email, string confirmToken);
        Task<RegistrationResponse> ChangePassword(ChangePwdDTO model);
        Task<ApplicationUser?> GetUser(string email);
    }
}