using System.Text;
using System.Text.RegularExpressions;

namespace Otomar.WebApp.Helpers
{
    public static class SlugHelper
    {
        public static string Generate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var turkishMap = new Dictionary<char, char>
    {
        {'ç', 'c'}, {'Ç', 'c'},
        {'ğ', 'g'}, {'Ğ', 'g'},
        {'ı', 'i'}, {'İ', 'i'},
        {'ö', 'o'}, {'Ö', 'o'},
        {'ş', 's'}, {'Ş', 's'},
        {'ü', 'u'}, {'Ü', 'u'}
    };

            var slug = new StringBuilder();

            foreach (var c in text)
            {
                if (turkishMap.TryGetValue(c, out var replacement))
                    slug.Append(replacement);
                else
                    slug.Append(c);
            }

            var result = slug.ToString()
                .ToLowerInvariant()
                .Trim();

            // Boşlukları tire yap
            result = Regex.Replace(result, @"\s+", "-");

            // Alfanumerik ve tire dışındakileri kaldır
            result = Regex.Replace(result, @"[^\w\-]+", "");

            // Çift tireleri tek yap
            result = Regex.Replace(result, @"\-\-+", "-");

            // Baş ve sondaki tireleri kaldır
            result = result.Trim('-');

            return result;
        }
        public static string ToTitle(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return string.Empty;

            // Tireleri boşluğa çevir
            var text = slug.Replace("-", " ");

            // Her kelimenin ilk harfini büyük yap
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();

            foreach (var word in words)
            {
                if (word.Length > 0)
                {
                    // İlk harfi büyük, geri kalanı küçük
                    result.Append(char.ToUpper(word[0]));
                    if (word.Length > 1)
                        result.Append(word.Substring(1).ToLower());
                    result.Append(" ");
                }
            }

            return result.ToString().Trim();
        }
    }
}