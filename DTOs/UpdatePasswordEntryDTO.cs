﻿namespace JwtApi.DTOs
{
    public class UpdatePasswordEntryDTO
    {
        public Guid EntryId { get; set; }
        public string? Url { get; set; }
        public string? Name { get; set; }
        public string? Note { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
