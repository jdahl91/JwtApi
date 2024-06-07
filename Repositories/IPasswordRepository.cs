using JwtApi.DTOs;
using JwtApi.Models;
using Microsoft.AspNetCore.Mvc;
using static JwtApi.Responses.CustomResponses;

namespace JwtApi.Repositories
{
    public interface IPasswordRepository
    {
        Task<ActionResult<GetAllPasswordsApiResonse>> GetPasswordEntriesAsync(string email);
        Task<ActionResult<ApiResponse>> InsertPasswordEntryAsync(NewPasswordEntryDTO entry);
        Task<ActionResult<ApiResponse>> UpdatePasswordEntryAsync(PasswordEntry entry);
    }
}
