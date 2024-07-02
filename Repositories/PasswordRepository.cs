using JwtApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using static JwtApi.Responses.CustomResponses;
using JwtApi.Models;
using System.Security.Principal;
using System.Reflection;
using System.Data;
using Microsoft.AspNetCore.DataProtection;
using NpgsqlTypes;

namespace JwtApi.Repositories
{
    public class PasswordRepository : IPasswordRepository
    {
        private readonly NpgsqlConnection _connection;
        // private readonly IConfiguration _config;
        private readonly IAccount _account;
        private readonly IDataProtector _protector;

        public PasswordRepository(NpgsqlConnection connection, IAccount account, IDataProtectionProvider dataProtectionProvider) // , IConfiguration config
        {
            _connection = connection;
            // _config = config;
            _account = account;
            _protector = dataProtectionProvider.CreateProtector("PasswordProtector");
        }

        public async Task<ActionResult<GetAllPasswordsApiResponse>> GetPasswordEntriesAsync(GetAllPasswordsDTO form)
        {
            var findUser = await _account.GetUser(form.Email);
            if (findUser is null)
                return new GetAllPasswordsApiResponse(false, "Email address does not exist.");

            var passwordEntries = new List<PasswordEntry>();
            try
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM password_entries WHERE user_id = (SELECT id FROM users WHERE Email = @Email AND id = @GUserId);";

                using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@Email", form.Email);
                command.Parameters.AddWithValue("@GUserId", form.UserId);

                using var reader = await command.ExecuteReaderAsync();

                int entryIdOrdinal = reader.GetOrdinal("entry_id");
                int userIdOrdinal = reader.GetOrdinal("user_id");
                int urlOrdinal = reader.GetOrdinal("url");
                int nameOrdinal = reader.GetOrdinal("name");
                int noteOrdinal = reader.GetOrdinal("note");
                int usernameOrdinal = reader.GetOrdinal("username");
                int passwordOrdinal = reader.GetOrdinal("password");
                int createdAtOrdinal = reader.GetOrdinal("created_at");
                int updatedAtOrdinal = reader.GetOrdinal("updated_at");

                while (await reader.ReadAsync())
                {
                    var protectedPassword = reader.GetString(passwordOrdinal);
                    var decryptedPassword = _protector.Unprotect(protectedPassword);
                    var entry = new PasswordEntry
                    {
                        EntryId = reader.GetGuid(entryIdOrdinal),
                        UserId = reader.GetGuid(userIdOrdinal),
                        Url = reader.IsDBNull(urlOrdinal) ? null : reader.GetString(urlOrdinal),
                        Name = reader.IsDBNull(nameOrdinal) ? null : reader.GetString(nameOrdinal),
                        Note = reader.IsDBNull(noteOrdinal) ? null : reader.GetString(noteOrdinal),
                        Username = reader.IsDBNull(usernameOrdinal) ? null : reader.GetString(usernameOrdinal),
                        Password = decryptedPassword, // reader.IsDBNull(passwordOrdinal) ? null : _protector.Unprotect(reader.GetString(passwordOrdinal)),
                        CreatedAt = reader.GetDateTime(createdAtOrdinal),
                        UpdatedAt = reader.GetDateTime(updatedAtOrdinal)
                    };
                    passwordEntries.Add(entry);
                }

                await _connection.CloseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to fetch password list.");
                Console.WriteLine("Failed to fetch password list.");
                System.Diagnostics.Debug.WriteLine($"Error: {ex}.");
                Console.WriteLine($"Error: {ex}.");

                //return new GetAllPasswordsApiResponse(false, "Failed to fetch password list.");
            }
            finally
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    await _connection.CloseAsync();
                }
            }
            return new GetAllPasswordsApiResponse(true, "Success.", passwordEntries);
        }

        public async Task<ActionResult<ApiResponse>> InsertPasswordEntryAsync(NewPasswordEntryDTO entry)
        {
            try
            {
                await _connection.OpenAsync();

                var sql = "INSERT INTO password_entries (user_id, url, name, note, username, password) VALUES (@UserId, @Url, @Name, @Note, @Username, @Password);";
                var encryptedPassword = _protector.Protect(entry.Password ?? "");

                using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@UserId", entry.UserId);
                command.Parameters.AddWithValue("@Url", (object?)entry.Url ?? DBNull.Value);
                command.Parameters.AddWithValue("@Name", (object?)entry.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("@Note", (object?)entry.Note ?? DBNull.Value);
                command.Parameters.AddWithValue("@Username", (object?)entry.Username ?? DBNull.Value);
                command.Parameters.AddWithValue("@Password", (object?)encryptedPassword ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
                await _connection.CloseAsync();
            }
            catch (Exception ex)
            {
                return new ApiResponse(false, $"Error inserting password entry: {ex.Message}");
            }
            finally
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    await _connection.CloseAsync();
                }
            }
            return new ApiResponse(true, "Password entry inserted successfully.");
        }

        public async Task<ActionResult<ApiResponse>> UpdatePasswordEntryAsync(UpdatePasswordEntryDTO entry)
        {
            try
            {
                await _connection.OpenAsync();
                var encryptedPassword = _protector.Protect(entry.Password ?? "");

                var sql = @$"
                    UPDATE password_entries 
                    SET url = '{((!string.IsNullOrEmpty(entry.Url)) ? entry.Url : "")}', 
                        name = '{((!string.IsNullOrEmpty(entry.Name)) ? entry.Name : "")}', 
                        note = '{((!string.IsNullOrEmpty(entry.Note)) ? entry.Note : "")}', 
                        username = '{((!string.IsNullOrEmpty(entry.Username)) ? entry.Username : "")}', 
                        password = '{encryptedPassword}',
                        updated_at = NOW()
                    WHERE entry_id = '{entry.EntryId}';";

                using var command = new NpgsqlCommand(sql, _connection);
                
                await command.ExecuteNonQueryAsync();
                await _connection.CloseAsync();
            }
            catch (Exception ex)
            {
                return new ApiResponse(false, $"Error updating password entry: {ex.Message}");
            }
            finally
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    await _connection.CloseAsync();
                }
            }
            return new ApiResponse(true, $"Password entry updated successfully.");
        }
    }
}
