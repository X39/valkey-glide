using Xunit.Sdk;

namespace Valkey.Glide.TestCommon;

public static class Helpers
{
    public static string[] MultiCase(
        string input,
        string? prefix = null,
        string? suffix = null,
        int maxPermutations = Int32.MaxValue
    )
    {
        input = input.ToLower();
        var chars = input.ToCharArray();
        var permutations = Math.Min(chars.Length * 2, maxPermutations);
        var results = new string[permutations];
        for (var i = 0; i < permutations; i++)
        {
            var c = chars[i % chars.Length];
            chars[i % chars.Length] = char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c);
            var result = new string(chars);
            if (prefix is not null && suffix is not null)
                results[i] = string.Concat(prefix, result, suffix);
            else if (prefix is not null)
                results[i] = string.Concat(prefix, result);
            else if (suffix is not null)
                results[i] = string.Concat(result, suffix);
            else
                results[i] = result;
        }

        return results;
    }

    public static async Task IgnoreExceptionAsync(Func<Task> func)
    {
        try
        {
            await func();
        }
        catch (XunitException)
        {
            throw;
        }
        catch
        {
            // Ignore
        }
    }
}
