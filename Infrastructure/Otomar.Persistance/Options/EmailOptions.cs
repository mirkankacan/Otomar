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

        public List<string> RequiredCc { get; set; } = [];

        public List<string> RequiredBcc { get; set; } = [];

        public List<string> ErrorTo { get; set; } = [];

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