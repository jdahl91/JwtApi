using JwtApi.DTOs;
using JwtApi.Models;
//using JwtApi.Services;
//using Microsoft.AspNetCore.Components;
//using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
//using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
//using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static JwtApi.Responses.CustomResponses;

namespace JwtApi.Repositories
{
    public class Account : IAccount
    {
        private readonly NpgsqlConnection _connection;
        private readonly IConfiguration _config;
        // private readonly IEmailService emailService;

        public Account(NpgsqlConnection connection, IConfiguration config)
        {
            this._connection = connection;
            this._config = config;
        }

        public async Task<LoginResponse> LoginAsync(LoginDTO model)
        {
            var findUser = await GetUser(model.Email);
            if (findUser is null)
                return new LoginResponse(false, "Invalid credentials.");
            if (findUser.IsEmailConfirmed == false)
                return new LoginResponse(false, "Email not confirmed.");

            if (!BCrypt.Net.BCrypt.Verify(model.Password, findUser.Password))
                return new LoginResponse(false, "Invalid credentials.");

            string jwtToken = GenerateAccessToken(findUser);
            return new LoginResponse(true, "Login success.", jwtToken);
        }

        public async Task<RegistrationResponse> RegisterAsync(RegisterDTO model)
        {
            var findUser = await GetUser(model.Email);
            if (findUser is not null) return new RegistrationResponse(false, "User already exists.");

            await _connection.OpenAsync();
            var cmd = $"INSERT INTO Users (Name, Email, Role, Password, EmailConfirmationToken, IsEmailConfirmed) VALUES (@Name, @Email, @Role, @Password, @EmailConfirmationToken, @IsEmailConfirmed);";

            using (var command = new NpgsqlCommand(cmd, _connection))
            {
                command.Parameters.AddWithValue("@Name", model.Name);
                command.Parameters.AddWithValue("@Email", model.Email);
                command.Parameters.AddWithValue("@Role", "User");
                command.Parameters.AddWithValue("@Password", BCrypt.Net.BCrypt.HashPassword(model.Password));
                command.Parameters.AddWithValue("@EmailConfirmationToken", BCrypt.Net.BCrypt.HashPassword(model.Email));
                command.Parameters.AddWithValue("@IsEmailConfirmed", true); // this needs to be false once we have an email service
                await command.ExecuteNonQueryAsync();
            }
            await _connection.CloseAsync();

            // generate token and send email confirmation
            // var user = await GetUser(model.Email);
            // if (user == null) return new RegistrationResponse(false, "Unsuccessful.");
            // string confirmToken = BCrypt.Net.BCrypt.HashPassword(user.Email);
            // user.EmailConfirmationToken = confirmToken;
            // await appDbContext.SaveChangesAsync();

            // Need to fix email service in order to authenticate accounts
            // await emailService.SendConfirmationEmail(model.Email, model.Name, confirmToken);

            return new RegistrationResponse(true, "Registration success.");
        }

        // this is not implemented
        public async Task<RegistrationResponse> ConfirmEmail(string email, string confirmToken)
        {
            //var user = await GetUser(email);
            //if (user == null || user.EmailConfirmationToken != confirmToken)
            //    return new RegistrationResponse(false, "Unsuccessful.");
            //user.IsEmailConfirmed = true;

            // await appDbContext.SaveChangesAsync();
            return new RegistrationResponse(true, "Email confirmed.");
        }

        private async Task<ApplicationUser?> GetUser(string email)
        {
            await _connection.OpenAsync();
            using var cmd = new NpgsqlCommand($"SELECT * FROM users WHERE email='{email}'", _connection);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                await _connection.CloseAsync();
                return null;
            }

            var result = new ApplicationUser();
            while (reader.Read())
            {
                var user = new ApplicationUser
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3),
                    Password = reader.GetString(4),
                    IsEmailConfirmed = reader.GetBoolean(5)
                };
                result = user;
            }
            await _connection.CloseAsync();
            return result;
        }

        public void LogoutAsync()
        {
            // var customAuthenticationStateProvider = (CustomAuthenticationStateProvider)authenticationStateProvider;
            // customAuthenticationStateProvider.UpdateAuthenticationState("");
        }

        // some hashmap perhaps all refresh tokens are stored and upon logout they are deleted
        public LoginResponse ObtainNewAccessToken()
        {
            //CustomUserClaims customUserClaims = DecryptJwtService.DecryptToken(userSession.ExpiringJwtToken);
            //if (customUserClaims is null)
            //    return new LoginResponse(false, "Incorrect token.");
            //string newToken = GenerateToken(new ApplicationUser()
            //{
            //    Name = customUserClaims.Name,
            //    Email = customUserClaims.Email,
            //    Role = customUserClaims.Role
            //});
            //return new LoginResponse(true, "New token.", newToken);
            return new LoginResponse(true, "New token.", "newToken");
        }

        public async Task<RegistrationResponse> ChangePassword(ChangePwdDTO model)
        {
            var findUser = await GetUser(model.Email);
            if (findUser == null)
                return new RegistrationResponse(false, "User not found.");
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, findUser.Password))
                return new RegistrationResponse(false, "Invalid password.");
            if (model.CurrentPassword == model.NewPassword)
                return new RegistrationResponse(false, "New password cannot be the same as the old password.");
            if (model.NewPassword != model.ConfirmNewPassword)
                return new RegistrationResponse(false, "Passwords do not match.");

            var sql = $"UPDATE Users SET Password = @NewPassword WHERE Email = @Email;";

            await _connection.OpenAsync();
            using (var cmd = new NpgsqlCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@NewPassword", BCrypt.Net.BCrypt.HashPassword(model.NewPassword));
                cmd.Parameters.AddWithValue("@Email", model.Email);

                await cmd.ExecuteNonQueryAsync();
            }
            await _connection.CloseAsync();
            return new RegistrationResponse(true, "Password changed.");
        }

        private string GenerateAccessToken(ApplicationUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.Name, user.Name!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, user.Role!)
            };
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"]!,
                audience: _config["Jwt:Audience"]!,
                claims: userClaims,
                expires: DateTime.Now.AddMinutes(10),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        // should remove the email address and password from source code
        public async Task SeedAdminUser()
        {
            var findUser = await GetUser("joakimdahl@gmx.us");
            if (findUser != null) return;

            await _connection.OpenAsync();
            var cmd = $"INSERT INTO Users (Name, Email, Role, Password, EmailConfirmationToken, IsEmailConfirmed) VALUES (@Name, @Email, @Role, @Password, @EmailConfirmationToken, @IsEmailConfirmed);";

            using (var command = new NpgsqlCommand(cmd, _connection))
            {
                command.Parameters.AddWithValue("@Name", "Joakim");
                command.Parameters.AddWithValue("@Email", "joakimdahl@gmx.us");
                command.Parameters.AddWithValue("@Role", "Admin");
                command.Parameters.AddWithValue("@Password", BCrypt.Net.BCrypt.HashPassword("1234"));
                command.Parameters.AddWithValue("@EmailConfirmationToken", BCrypt.Net.BCrypt.HashPassword("joakimdahl@gmx.us"));
                command.Parameters.AddWithValue("@IsEmailConfirmed", true);
                await command.ExecuteNonQueryAsync();
            }
            await _connection.CloseAsync();
        }
    }
}