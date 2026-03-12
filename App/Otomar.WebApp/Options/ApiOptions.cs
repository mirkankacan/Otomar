using System.ComponentModel.DataAnnotations;

namespace Otomar.WebApp.Options
{
    public class ApiOptions
    {
        [Required]
        public string BaseUrl { get; set; } = default!;
    }
}