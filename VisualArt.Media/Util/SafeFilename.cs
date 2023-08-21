using System.Text.RegularExpressions;

namespace VisualArt.Media.Util
{
    public class SafeFileName
    {
        // https://msdn.microsoft.com/en-us/library/aa365247.aspx#naming_conventions
        // http://stackoverflow.com/questions/146134/how-to-remove-illegal-characters-from-path-and-filenames
        private static readonly Regex removeInvalidChars = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string MakeSafe(string fileName, string replacement = "_")
        {
            return removeInvalidChars.Replace(fileName, replacement);
        }

    }
}
