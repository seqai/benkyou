namespace BenkyouBot.Infrastructure;

public static class StringHelpers
{
    public static string[] Tokenize(this string s) =>
        s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string[] Tokenize(this string s, int count) =>
        s.Split(' ', count, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}