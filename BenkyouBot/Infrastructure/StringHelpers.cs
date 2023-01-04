using System.Text.RegularExpressions;

namespace BenkyouBot.Infrastructure;

public static class StringHelpers
{
    private static readonly Regex _regex = new(@"\s+", RegexOptions.Compiled);

    public static string[] Tokenize(this string s) =>
        _regex.Split(s.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();

    public static string[] Tokenize(this string s, int count) =>
        _regex.Split(s.Trim(), count).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
}