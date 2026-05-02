namespace UrlShortener.Core.Services;

public class ShortCodeGenerator
{
    private const string Base62Characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int DefaultCodeLength = 7;
    private static readonly Random Random = new();

    public string Generate(int length = DefaultCodeLength)
    {
        var code = new char[length];

        for (int i = 0; i < length; i++)
        {
            code[i] = Base62Characters[Random.Next(Base62Characters.Length)];
        }

        return new string(code);
    }

    public bool IsValidFormat(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        if (code.Length < 3 || code.Length > 20)
            return false;

        return code.All(c => Base62Characters.Contains(c));
    }
}
