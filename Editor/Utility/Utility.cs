using System.Text;
using System.Text.RegularExpressions;

namespace io.github.rollphes.epmanager.utility {
    internal class Utility {
        internal static string ConvertToSearchText(string input) {
            // Convert to NFKD & Lower
            var s = input.Normalize(NormalizationForm.FormKD).ToLower();

            // Convert to Kana
            var sb = new StringBuilder();
            var target = s.ToCharArray();
            char c;
            for (var i = 0; i < target.Length; i++) {
                c = target[i];
                if (c is >= 'ぁ' and <= 'ヴ') {
                    c = (char)(c + 0x0060);
                }
                sb.Append(c);
            }
            var kana = sb.ToString();

            // Escape
            return Regex.Escape(kana);
        }
    }
}