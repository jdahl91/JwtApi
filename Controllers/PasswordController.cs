using Microsoft.AspNetCore.Mvc;
using JwtApi.Repositories;
using JwtApi.DTOs;
using static JwtApi.Responses.CustomResponses;
using Microsoft.AspNetCore.Authorization;
using JwtApi.Models;

namespace JwtApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PasswordController : ControllerBase
    {
        private readonly IPasswordRepository _passwordRepository;

        public PasswordController(IPasswordRepository passwordRepository)
        {
            _passwordRepository = passwordRepository;
        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost("get-all")]
        public async Task<ActionResult<GetAllPasswordsApiResponse>> GetPasswordEntriesAsync(GetAllPasswordsDTO form)
        {
            var result = await _passwordRepository.GetPasswordEntriesAsync(form);
            return Ok(result);
        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost("insert")]
        public async Task<ActionResult<ApiResponse>> InsertPasswordEntryAsync(NewPasswordEntryDTO entry)
        {
            var result = await _passwordRepository.InsertPasswordEntryAsync(entry);
            return Ok(result);
        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost("update")]
        public async Task<ActionResult<ApiResponse>> UpdatePasswordEntryAsync(UpdatePasswordEntryDTO entry)
        {
            var result = await _passwordRepository.UpdatePasswordEntryAsync(entry);
            return Ok(result);
        }
    }
}
