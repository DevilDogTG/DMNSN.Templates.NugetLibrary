namespace DMNSN.Templates.NugetLibrary.Extensions;

public static class StringExtension
{
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
