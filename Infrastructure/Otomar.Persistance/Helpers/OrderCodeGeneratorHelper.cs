namespace Otomar.Persistance.Helpers
{
    public static class OrderCodeGeneratorHelper
    {
        public static string Generate()
        {
            var random = new Random();
            var randomNumber = random.Next(10000, 99999);
            return "OTOMAR" + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + randomNumber;
        }
    }
}