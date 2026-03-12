using System.ComponentModel.DataAnnotations;

namespace Otomar.Application.Options
{
    public class RedisOptions
    {
        [Required]
        public string ConnectionString { get; set; }

        [Required]
        public string InstanceName { get; set; }

        [Required]
        public int CartExpirationDays { get; set; }
    }
}
