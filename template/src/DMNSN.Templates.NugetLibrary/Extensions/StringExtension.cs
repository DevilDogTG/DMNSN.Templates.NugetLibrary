namespace DMNSN.Templates.NugetLibrary.Extensions;

/// <summary>
/// Extension methods for <see cref="string"/>.
/// </summary>
public static class StringExtension
{
    /// <summary>
    /// Converts a space-separated string to title case, capitalising the first letter of each
    /// word and lower-casing the rest.
    /// </summary>
    /// <param name="str">The string to convert. Returned unchanged when null or empty.</param>
    /// <returns>The title-cased string.</returns>
    public static string ToTitleCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        var words = str.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                var firstChar = char.ToUpper(words[i][0]);
                var rest = words[i][1..].ToLower();
                words[i] = firstChar + rest;
            }
        }
        return string.Join(' ', words);
    }
}
