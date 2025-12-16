using System.ComponentModel.DataAnnotations;

namespace Otomar.Persistance.Options
{
    public class EmailOptions
    {
        [Required]
        [Range(1, 65535)]
        public int Port { get; set; }

        [Required]
        public bool EnableSsl { get; set; }

        [Required]
        public string Host { get; set; } = default!;

        [Required]
        public EmailCredentials Credentials { get; set; } = default!;

        public string? RequiredCc { get; set; }

        public string? RequiredBcc { get; set; }

        public string? ErrorTo { get; set; }

        // Helper methods
        public List<string> GetRequiredCcList()
        {
            return SplitEmailAddresses(RequiredCc);
        }

        public List<string> GetRequiredBccList()
        {
            return SplitEmailAddresses(RequiredBcc);
        }

        public List<string> GetErrorToList()
        {
            return SplitEmailAddresses(ErrorTo);
        }

        private List<string> SplitEmailAddresses(string? emailString)
        {
            if (string.IsNullOrWhiteSpace(emailString))
                return new List<string>();

            return emailString
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();
        }

        public class EmailCredentials
        {
            [Required]
            [EmailAddress]
            public string UserName { get; set; } = default!;

            [Required]
            public string Password { get; set; } = default!;
        }
    }
}