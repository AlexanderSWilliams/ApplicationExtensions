using System.Text.RegularExpressions;

namespace Application.Text.RegularExpressions
{
    public static class RegularExpressions
    {
        public static Regex Create(string pattern, bool cultureInvariant, bool ECMAScript, bool explicitCapture, bool ignoreCase,
            bool ignorePatternWhitespace, bool multiline, bool rightToLeft, bool singleLine)
        {
            var option = RegexOptions.None;
            option = cultureInvariant ? option | RegexOptions.CultureInvariant : option;
            option = ECMAScript ? option | RegexOptions.ECMAScript : option;
            option = explicitCapture ? option | RegexOptions.ExplicitCapture : option;
            option = ignoreCase ? option | RegexOptions.IgnoreCase : option;
            option = ignorePatternWhitespace ? option | RegexOptions.IgnorePatternWhitespace : option;
            option = multiline ? option | RegexOptions.Multiline : option;
            option = rightToLeft ? option | RegexOptions.RightToLeft : option;
            option = singleLine ? option | RegexOptions.Singleline : option;

            return new Regex(pattern, option);
        }
    }
}