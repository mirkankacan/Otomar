using System.Text;
using System.Text.RegularExpressions;

namespace Otomar.Shared.Helpers
{
    /// <summary>
    /// URL-friendly slug oluşturma ve slug'dan başlık üretme yardımcısı.
    /// Türkçe karakter desteği içerir.
    /// </summary>
    public static partial class SlugHelper
    {
        private static readonly Dictionary<char, char> TurkishMap = new()
        {
            {'ç', 'c'}, {'Ç', 'c'},
            {'ğ', 'g'}, {'Ğ', 'g'},
            {'ı', 'i'}, {'İ', 'i'},
            {'ö', 'o'}, {'Ö', 'o'},
            {'ş', 's'}, {'Ş', 's'},
            {'ü', 'u'}, {'Ü', 'u'}
        };

        /// <summary>
        /// Verilen metni URL-friendly slug'a dönüştürür.
        /// Türkçe karakterleri ASCII karşılıklarına çevirir.
        /// </summary>
        /// <param name="text">Slug'a dönüştürülecek metin.</param>
        /// <returns>URL-friendly slug. Boş veya null metin için boş string döner.</returns>
        public static string Generate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var slug = new StringBuilder(text.Length);

            foreach (var c in text)
            {
                slug.Append(TurkishMap.TryGetValue(c, out var replacement) ? replacement : c);
            }

            var result = slug.ToString()
                .ToLowerInvariant()
                .Trim();

            result = WhitespaceRegex().Replace(result, "-");
            result = NonSlugCharRegex().Replace(result, "");
            result = MultipleDashRegex().Replace(result, "-");
            result = result.Trim('-');

            return result;
        }

        /// <summary>
        /// Slug'ı insan-okunabilir başlık formatına dönüştürür.
        /// Her kelimenin ilk harfini büyük yapar.
        /// </summary>
        /// <param name="slug">Başlığa dönüştürülecek slug.</param>
        /// <returns>Title Case formatında başlık. Boş veya null slug için boş string döner.</returns>
        public static string ToTitle(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return string.Empty;

            var words = slug.Replace("-", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var result = new StringBuilder();

            foreach (var word in words)
            {
                if (result.Length > 0)
                    result.Append(' ');

                result.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1)
                    result.Append(word[1..].ToLowerInvariant());
            }

            return result.ToString();
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        [GeneratedRegex(@"[^a-z0-9\-]")]
        private static partial Regex NonSlugCharRegex();

        [GeneratedRegex(@"-{2,}")]
        private static partial Regex MultipleDashRegex();
    }
}
