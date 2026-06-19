using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TestTaskSolution;

public static class LogStandardizer
{
    
    private const string OutputDateFormat = "dd-MM-yyyy";

    private static readonly Regex Format1Regex = new(
        @"^(?<date>\d{2}\.\d{2}\.\d{4})\s+" +
        @"(?<time>\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?)\s+" +
        @"(?<level>[A-Z]+)\s+" +
        @"(?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex Format2Regex = new(
        @"^(?<date>\d{4}-\d{2}-\d{2})\s+" +
        @"(?<time>\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?)\s*\|\s*" +
        @"(?<level>[A-Z]+)\s*\|\s*" +
        @"(?<threadId>\d+)\s*\|\s*" +
        @"(?<method>[^|]+)\s*\|\s*" +
        @"(?<message>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static void StandardizeFile(string inputPath, string outputPath, string? problemsPath = null)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("Input path must not be empty.", nameof(inputPath));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path must not be empty.", nameof(outputPath));
        }

        problemsPath ??= Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? Environment.CurrentDirectory,
            "problems.txt");

        EnsureParentDirectoryExists(outputPath);
        EnsureParentDirectoryExists(problemsPath);

        using var output = new StreamWriter(outputPath, append: false, Encoding.UTF8);
        using var problems = new StreamWriter(problemsPath, append: false, Encoding.UTF8);

        foreach (string line in File.ReadLines(inputPath, Encoding.UTF8))
        {
            if (TryParse(line, out StandardLogRecord record))
            {
                output.WriteLine(record.ToOutputLine());
            }
            else
            {
                problems.WriteLine(line);
            }
        }
    }

    public static bool TryParse(string line, out StandardLogRecord record)
    {
        record = default;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return TryParseFormat1(line, out record) || TryParseFormat2(line, out record);
    }

    private static bool TryParseFormat1(string line, out StandardLogRecord record)
    {
        record = default;
        Match match = Format1Regex.Match(line);
        if (!match.Success)
        {
            return false;
        }

        if (!DateTime.TryParseExact(
                match.Groups["date"].Value,
                "dd.MM.yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime date))
        {
            return false;
        }

        string time = match.Groups["time"].Value;
        if (!IsValidTime(time))
        {
            return false;
        }

        if (!TryNormalizeLevel(match.Groups["level"].Value, out string level))
        {
            return false;
        }

        record = new StandardLogRecord(
            date,
            time,
            level,
            "DEFAULT",
            match.Groups["message"].Value.TrimStart());

        return true;
    }

    private static bool TryParseFormat2(string line, out StandardLogRecord record)
    {
        record = default;
        Match match = Format2Regex.Match(line);
        if (!match.Success)
        {
            return false;
        }

        if (!DateTime.TryParseExact(
                match.Groups["date"].Value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime date))
        {
            return false;
        }

        string time = match.Groups["time"].Value;
        if (!IsValidTime(time))
        {
            return false;
        }

        if (!TryNormalizeLevel(match.Groups["level"].Value, out string level))
        {
            return false;
        }

        string method = match.Groups["method"].Value.Trim();
        if (method.Length == 0)
        {
            return false;
        }

        record = new StandardLogRecord(
            date,
            time,
            level,
            method,
            match.Groups["message"].Value.TrimStart());

        return true;
    }

    private static bool TryNormalizeLevel(string sourceLevel, out string normalizedLevel)
    {
        normalizedLevel = sourceLevel.Trim().ToUpperInvariant() switch
        {
            "INFORMATION" or "INFO" => "INFO",
            "WARNING" or "WARN" => "WARN",
            "ERROR" => "ERROR",
            "DEBUG" => "DEBUG",
            _ => string.Empty
        };

        return normalizedLevel.Length > 0;
    }

    private static bool IsValidTime(string time)
    {
        string[] timeAndFraction = time.Split('.', count: 2);
        string[] parts = timeAndFraction[0].Split(':');

        if (parts.Length != 3)
        {
            return false;
        }

        return int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int hour)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int minute)
            && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int second)
            && hour is >= 0 and <= 23
            && minute is >= 0 and <= 59
            && second is >= 0 and <= 59;
    }

    private static void EnsureParentDirectoryExists(string path)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public readonly record struct StandardLogRecord(
        DateTime Date,
        string Time,
        string Level,
        string Method,
        string Message)
    {
        public string ToOutputLine()
        {
            return string.Join('\t',
                Date.ToString(OutputDateFormat, CultureInfo.InvariantCulture),
                Time,
                Level,
                Method,
                Message);
        }
    }
}
