namespace Otomar.WebApp.Options
{
    public class RecaptchaOptions
    {
        public string SiteKey { get; set; } = default!;
        public string SecretKey { get; set; } = default!;
        public double MinimumScore { get; set; } = 0.5;
        public bool Enabled { get; set; } = true;
    }
}
