using JwtApi.DTOs;
using static JwtApi.Responses.CustomResponses;

namespace JwtApi.Repositories
{
    public interface IAccount
    {
        Task<RegistrationResponse> RegisterAsync(RegisterDTO model);
        Task<LoginResponse> LoginAsync(LoginDTO model);
        void LogoutAsync();
        LoginResponse ObtainNewAccessToken();
        Task<RegistrationResponse> ConfirmEmail(string email, string confirmToken);
        Task<RegistrationResponse> ChangePassword(ChangePwdDTO model);
        Task SeedAdminUser();
        // RegistrationResponse TestChangePassword();
    }
}