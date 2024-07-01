namespace JwtApi.Models
{
    public class PasswordEntry
    {
        public PasswordEntry() { }
        public PasswordEntry(PasswordEntry entry)
        {
            this.EntryId = entry.EntryId;
            this.UserId = entry.UserId;
            this.Url = entry.Url;
            this.Name = entry.Name;
            this.Note = entry.Note;
            this.Username = entry.Username;
            this.Password = entry.Password;
            this.CreatedAt = entry.CreatedAt;
            this.UpdatedAt = entry.UpdatedAt;
        }
        public Guid EntryId { get; set; }
        public Guid UserId { get; set; }
        public string? Url { get; set; }
        public string? Name { get; set; }
        public string? Note { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
