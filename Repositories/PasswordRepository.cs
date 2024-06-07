using JwtApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using static JwtApi.Responses.CustomResponses;
using JwtApi.Models;
using System.Security.Principal;
using System.Reflection;
using System.Data;

namespace JwtApi.Repositories
{
    public class PasswordRepository : IPasswordRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly IConfiguration _config;
        private readonly IAccount _account;
        
        public PasswordRepository(NpgsqlConnection connection, IConfiguration config, IAccount account)
        {
            _connection = connection;
            _config = config;
            _account = account;
        }

        public async Task<ActionResult<GetAllPasswordsApiResonse>> GetPasswordEntriesAsync(string email)
        {
            var findUser = await _account.GetUser(email);
            if (findUser is null)
                return new GetAllPasswordsApiResonse(false, "Failed to fetch password list.");

            var passwordEntries = new List<PasswordEntry>();

            try
            {
                await _connection.OpenAsync();
                var sql = "SELECT * FROM password_entries WHERE user_id = (SELECT id FROM users WHERE Email = @Email)";

                using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@Email", email);
                
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
                    var entry = new PasswordEntry
                    {
                        EntryId = reader.GetInt32(entryIdOrdinal),
                        UserId = reader.GetInt32(userIdOrdinal),
                        Url = reader.IsDBNull(urlOrdinal) ? null : reader.GetString(urlOrdinal),
                        Name = reader.IsDBNull(nameOrdinal) ? null : reader.GetString(nameOrdinal),
                        Note = reader.IsDBNull(noteOrdinal) ? null : reader.GetString(noteOrdinal),
                        Username = reader.IsDBNull(usernameOrdinal) ? null : reader.GetString(usernameOrdinal),
                        Password = reader.IsDBNull(passwordOrdinal) ? null : reader.GetString(passwordOrdinal),
                        CreatedAt = reader.GetDateTime(createdAtOrdinal),
                        UpdatedAt = reader.GetDateTime(updatedAtOrdinal)
                    };
                    passwordEntries.Add(entry);
                }
                    
                await _connection.CloseAsync();
            }
            catch
            {
                return new GetAllPasswordsApiResonse(false, "Failed to fetch password list.");
            }
            finally
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    await _connection.CloseAsync();
                }
            }
            return new GetAllPasswordsApiResonse(true, "Success.", passwordEntries);
        }

        public async Task<ActionResult<ApiResponse>> InsertPasswordEntryAsync(NewPasswordEntryDTO entry)
        {
            try
            {
                await _connection.OpenAsync();

                var sql = "INSERT INTO password_entries (user_id, url, name, note, username, password) VALUES (@UserId, @Url, @Name, @Note, @Username, @Password)";

                using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@UserId", entry.UserId);
                command.Parameters.AddWithValue("@Url", (object?) entry.Url ?? DBNull.Value);
                command.Parameters.AddWithValue("@Name", (object?) entry.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("@Note", (object?) entry.Note ?? DBNull.Value);
                command.Parameters.AddWithValue("@Username", (object?) entry.Username ?? DBNull.Value);
                command.Parameters.AddWithValue("@Password", (object?) entry.Password ?? DBNull.Value);

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

        public async Task<ActionResult<ApiResponse>> UpdatePasswordEntryAsync(PasswordEntry entry)
        {
            try
            {
                await _connection.OpenAsync();

                var sql = "UPDATE password_entries SET url = @Url, name = @Name, note = @Note, username = @Username, password = @Password WHERE entry_id = @EntryId";

                using var command = new NpgsqlCommand(sql, _connection);
                command.Parameters.AddWithValue("@EntryId", entry.EntryId);
                command.Parameters.AddWithValue("@Url", (object?)entry.Url ?? DBNull.Value);
                command.Parameters.AddWithValue("@Name", (object?)entry.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("@Note", (object?)entry.Note ?? DBNull.Value);
                command.Parameters.AddWithValue("@Username", (object?)entry.Username ?? DBNull.Value);
                command.Parameters.AddWithValue("@Password", (object?)entry.Password ?? DBNull.Value);

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
            return new ApiResponse(true, "Password entry updated successfully.");
        }
    }
}
