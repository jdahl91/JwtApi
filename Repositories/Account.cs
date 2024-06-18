using JwtApi.DTOs;
using JwtApi.Models;
using JwtApi.Services;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
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

        // We can set up a background service to delete expired tokens and no longer needed tokens from refreshtokens table and emailconfirmtokens table

        public async Task<LoginResponse> LoginAsync(LoginDTO model)
        {
            var findUser = await GetUser(model.Email);
            if (findUser is null)
                return new LoginResponse(false, "Invalid credentials.");
            if (findUser.IsEmailConfirmed == false)
                return new LoginResponse(false, "Email not confirmed.");

            if (!BCrypt.Net.BCrypt.Verify(model.Password, findUser.Password))
                return new LoginResponse(false, "Invalid credentials.");

            var jwtToken = GenerateAccessToken(findUser);
            var refreshToken = GenerateRefreshToken();

            await InsertRefreshTokenIntoDb(findUser.Email, refreshToken, false);
            return new LoginResponse(true, "Login success.", jwtToken, refreshToken);
        }

        public async Task<RegistrationResponse> RegisterAsync(RegisterDTO model)
        {
            var findUser = await GetUser(model.Email);
            if (findUser is not null) 
                return new RegistrationResponse(false, "User already exists.");

            try
            {
                await _connection.OpenAsync();
                var cmd = $"INSERT INTO Users (Name, Email, Role, Password, EmailConfirmationToken, IsEmailConfirmed) VALUES (@Name, @Email, @Role, @Password, @EmailConfirmationToken, @IsEmailConfirmed);";

                using (var command = new NpgsqlCommand(cmd, _connection))
                {
                    command.Parameters.AddWithValue("@Name", model.Name);
                    command.Parameters.AddWithValue("@Email", model.Email);
                    command.Parameters.AddWithValue("@Role", "User");
                    command.Parameters.AddWithValue("@Password", BCrypt.Net.BCrypt.HashPassword(model.Password));
                    command.Parameters.AddWithValue("@EmailConfirmationToken", BCrypt.Net.BCrypt.HashPassword(model.Email));
                    command.Parameters.AddWithValue("@IsEmailConfirmed", false); // until email service is set up users can request access to the site
                    await command.ExecuteNonQueryAsync();
                }
                await _connection.CloseAsync();
            }
            catch
            {
                return new RegistrationResponse(false, "Unsuccessful.");
            }
            var emailConfirmToken = GenerateEmailConfirmationToken();
            await InsertEmailConfirmTokenIntoDb(model.Email, emailConfirmToken);

            // Need to fix email service in order to authenticate accounts
            // await emailService.SendConfirmationEmail(model.Email, model.Name, confirmToken);

            return new RegistrationResponse(true, "Registration success.");
        }

        public async Task<RegistrationResponse> ConfirmEmail(string email, string confirmToken)
        {
            var emailConfirmToken = await GetEmailConfirmTokenFromDb(email);

            if (emailConfirmToken is null || emailConfirmToken?.Item2 != confirmToken)
                return new RegistrationResponse(false, "Unsuccessful.");

            var sql = $"UPDATE Users SET isemailconfirmed = @EmailConfirmed WHERE Email = @Email;";

            await _connection.OpenAsync();
            using (var cmd = new NpgsqlCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@EmailConfirmed", true);
                await cmd.ExecuteNonQueryAsync();
            }
            await _connection.CloseAsync();

            return new RegistrationResponse(true, "Email confirmed.");
        }

        public async Task<RegistrationResponse> LogoutAsync(string expiredAccessToken)
        {
            CustomUserClaims customUserClaims = DecryptJwtService.DecryptToken(expiredAccessToken);
            if (customUserClaims is null)
                return new RegistrationResponse(false, "Jwt decrypt failed."); // make less informative in production

            await _connection.OpenAsync();
            var sql = "DELETE FROM refreshtokens WHERE userid = (SELECT id FROM users WHERE email = @Email);";

            using (var command = new NpgsqlCommand(sql, _connection))
            {
                command.Parameters.AddWithValue("@Email", customUserClaims.Email);

                await command.ExecuteNonQueryAsync();
            }
            await _connection.CloseAsync();

            return new RegistrationResponse(true, "Logout success.");
        }

        // some hashmap perhaps all refresh tokens are stored and upon logout they are deleted
        public async Task<LoginResponse> ObtainNewAccessToken(string expiredAccessToken, string refreshToken)
        {
            CustomUserClaims customUserClaims = DecryptJwtService.DecryptToken(expiredAccessToken);
            if (customUserClaims is null)
                return new LoginResponse(false, "Jwt decrypt failed."); // make less informative in production

            var refreshTokenFromDb = await GetRefreshTokenFromDb(customUserClaims.Email);

            if (refreshTokenFromDb is null || refreshTokenFromDb?.Item2 != refreshToken)
                return new LoginResponse(false, "Invalid refresh token."); // make less informative in production
            if (refreshTokenFromDb?.Item3 < DateTime.Now)
                return new LoginResponse(false, "Refresh token expired."); // make less informative in production

            var newJwtToken = GenerateAccessToken(new ApplicationUser()
            {
                Name = customUserClaims.Name,
                Email = customUserClaims.Email,
                Role = customUserClaims.Role
            });
            var newRefreshToken = GenerateRefreshToken();
            await InsertRefreshTokenIntoDb(customUserClaims.Email, newRefreshToken, true);

            return new LoginResponse(true, "New tokens issued.", newJwtToken, newRefreshToken);
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

        public async Task<ApplicationUser?> GetUser(string email)
        {
            await _connection.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT * FROM users WHERE email=@Email", _connection);
            cmd.Parameters.AddWithValue("@Email", email);
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

        private async Task<(string, string)?> GetEmailConfirmTokenFromDb(string email)
        {
            await _connection.OpenAsync();
            var query = @"SELECT users.email, emailconfirmtokens.token FROM users JOIN emailconfirmtokens ON users.id=emailconfirmtokens.userid WHERE users.email = @Email";

            using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("@Email", email);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                await _connection.CloseAsync();
                return null;
            }

            (string, string)? result = null;
            while (await reader.ReadAsync())
            {
                result = (reader.GetString(0), reader.GetString(1));
            }
            await _connection.CloseAsync();
            return result;
        }

        private async Task<(string, string, DateTime)?> GetRefreshTokenFromDb(string email)
        {
            await _connection.OpenAsync();
            var query = "SELECT users.email, refreshtokens.token, refreshtokens.expirydate FROM users JOIN refreshtokens ON users.id=refreshtokens.userid WHERE users.email=@Email;";

            using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("@Email", email);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                await _connection.CloseAsync();
                return null;
            }

            (string, string, DateTime)? result = null;
            if (await reader.ReadAsync())
            {
                result = (reader.GetString(0), reader.GetString(1), reader.GetDateTime(2));
            }
            await _connection.CloseAsync();
            return result;
        }

        private async Task<bool> InsertRefreshTokenIntoDb(string email, string refreshToken, bool reinsert)
        {
            var findUser = await GetUser(email);
            if (findUser is null) return false;

            var sql1 = "INSERT INTO refreshtokens (token, userid, expirydate) VALUES (@Token, (SELECT id FROM users WHERE email=@Email), NOW() + INTERVAL '10 days');";
            var sql2 = "INSERT INTO refreshtokens (token, userid, expirydate) VALUES (@Token, (SELECT id FROM users WHERE email = @Email), NOW() + INTERVAL '10 days') ON CONFLICT (userid) DO UPDATE SET token = EXCLUDED.token, expirydate = EXCLUDED.expirydate;";
            var sql = reinsert ? sql2 : sql1;

            try
            {
                await _connection.OpenAsync();

                using (var cmd = new NpgsqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@Token", refreshToken);
                    cmd.Parameters.AddWithValue("@Email", email);

                    await cmd.ExecuteNonQueryAsync();
                }
                await _connection.CloseAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in InsertRefreshTokenIntoDb: " + ex);
                return false;
            }
        }

        private async Task<bool> InsertEmailConfirmTokenIntoDb(string email, string emailConfirmToken)
        {
            var findUser = await GetUser(email);
            if (findUser is null) return false;

            var sql = "INSERT INTO emailconfirmtokens (token, userid, expirydate) VALUES (@Token, (SELECT id FROM users WHERE email=@Email), NOW() + INTERVAL '10 days');";

            try
            {
                await _connection.OpenAsync();

                using (var cmd = new NpgsqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@Token", emailConfirmToken);
                    cmd.Parameters.AddWithValue("@Email", email);

                    await cmd.ExecuteNonQueryAsync();
                }
                await _connection.CloseAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in InsertEmailConfirmTokenIntoDb: " + ex);
                return false;
            }
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

        private string GenerateEmailConfirmationToken()
        {
            return GenerateRefreshToken();
        }
    }
}