using Microsoft.AspNetCore.Mvc;
using JwtApi.Repositories;
using JwtApi.DTOs;
using static JwtApi.Responses.CustomResponses;
using Microsoft.AspNetCore.Authorization;

namespace JwtApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccount _account;

        public AccountController(IAccount account)
        {
            _account = account;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<RegistrationResponse>> RegisterAsync(RegisterDTO model)
        {
            var result = await _account.RegisterAsync(model);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> LoginAsync(LoginDTO model)
        {
            var result = await _account.LoginAsync(model);
            return Ok(result);
        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost("logout")]
        public async Task<ActionResult<RegistrationResponse>> LogoutAsync()
        {
            string? authorizationHeader = HttpContext.Request.Headers.Authorization;
            string? expiredAccessToken = null;

            if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            {
                expiredAccessToken = authorizationHeader.Substring("Bearer ".Length).Trim();
            }
            var result = await _account.LogoutAsync(expiredAccessToken!);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] string refreshToken)
        {
            string? authorizationHeader = HttpContext.Request.Headers.Authorization;
            string? expiredAccessToken = null;

            if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            {
                expiredAccessToken = authorizationHeader.Substring("Bearer ".Length).Trim();
            }
            var result = await _account.ObtainNewAccessToken(expiredAccessToken!, refreshToken);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<ActionResult<RegistrationResponse>> ConfirmEmail([FromQuery] string email, [FromQuery] string confirmToken)
        {
            var result = await _account.ConfirmEmail(email, confirmToken);
            return Ok(result);
        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost("change-password")]
        public async Task<ActionResult<RegistrationResponse>> ChangePassword(ChangePwdDTO model)
        {
            var result = await _account.ChangePassword(model);
            return Ok(result);
        }
    }
}
