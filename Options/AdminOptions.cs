namespace MVC.Options
{
    public class AdminOptions
    {
        public const string SectionName = "Admin";

        public required string Email { get; set; }
        public required string Password { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

    }
}
