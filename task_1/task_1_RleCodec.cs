using System;
using System.Text;

namespace TestTaskSolution;

public static class RleCodec
{
    public static string Compress(string source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (source.Length == 0)
        {
            return string.Empty;
        }

        var result = new StringBuilder(source.Length);
        char currentSymbol = ValidateLowerLatin(source[0], 0);
        int runLength = 1;

        for (int i = 1; i < source.Length; i++)
        {
            char symbol = ValidateLowerLatin(source[i], i);

            if (symbol == currentSymbol)
            {
                runLength++;
                continue;
            }

            AppendRun(result, currentSymbol, runLength);
            currentSymbol = symbol;
            runLength = 1;
        }

        AppendRun(result, currentSymbol, runLength);
        return result.ToString();
    }

    public static string Decompress(string compressed)
    {
        if (compressed is null)
        {
            throw new ArgumentNullException(nameof(compressed));
        }

        var result = new StringBuilder(compressed.Length);
        int i = 0;

        while (i < compressed.Length)
        {
            char symbol = ValidateLowerLatin(compressed[i], i);
            i++;

            if (i == compressed.Length || !char.IsDigit(compressed[i]))
            {
                result.Append(symbol);
                continue;
            }

            if (compressed[i] == '0')
            {
                throw new FormatException($"Run length cannot start with zero at position {i}.");
            }

            long count = 0;
            while (i < compressed.Length && char.IsDigit(compressed[i]))
            {
                count = checked(count * 10 + compressed[i] - '0');
                if (count > int.MaxValue)
                {
                    throw new FormatException("Run length is too large.");
                }

                i++;
            }

            if (count < 2)
            {
                throw new FormatException("Canonical compressed form omits count for single symbols.");
            }

            result.Append(symbol, (int)count);
        }

        return result.ToString();
    }

    private static void AppendRun(StringBuilder result, char symbol, int count)
    {
        result.Append(symbol);
        if (count > 1)
        {
            result.Append(count);
        }
    }

    private static char ValidateLowerLatin(char symbol, int position)
    {
        if (symbol is < 'a' or > 'z')
        {
            throw new ArgumentException(
                $"Only lowercase Latin letters are allowed. Invalid character '{symbol}' at position {position}.");
        }

        return symbol;
    }
}
