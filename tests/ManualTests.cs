using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestTaskSolution;

public static class ManualTests
{
    public static void RunAll()
    {
        TestRleCodec();
        TestServerConcurrency();
        TestLogStandardizer();
        Console.WriteLine("All manual tests passed.");
    }

    private static void TestRleCodec()
    {
        AssertEqual("a3b2c3d2e", RleCodec.Compress("aaabbcccdde"), "compression example");
        AssertEqual("aaabbcccdde", RleCodec.Decompress("a3b2c3d2e"), "decompression example");
        AssertEqual(string.Empty, RleCodec.Compress(string.Empty), "empty compression");
        AssertEqual("z", RleCodec.Compress("z"), "single symbol compression");
        AssertEqual(new string('a', 12), RleCodec.Decompress("a12"), "multi-digit count");
        AssertThrows<ArgumentException>(() => RleCodec.Compress("abcD"), "non-lowercase input");
        AssertThrows<FormatException>(() => RleCodec.Decompress("a01"), "invalid leading zero");
    }

    private static void TestServerConcurrency()
    {
        Server.ResetForTests();

        const int writersCount = 100;
        const int readersCount = 1_000;
        var tasks = new List<Task>();

        tasks.AddRange(Enumerable.Range(0, writersCount).Select(_ => Task.Run(() => Server.AddToCount(1))));
        tasks.AddRange(Enumerable.Range(0, readersCount).Select(_ => Task.Run(() => _ = Server.GetCount())));

        Task.WaitAll(tasks.ToArray());
        AssertEqual(writersCount, Server.GetCount(), "concurrent writers result");
    }

    private static void TestLogStandardizer()
    {
        AssertTrue(
            LogStandardizer.TryParse(
                "10.03.2025 15:14:49.523 INFORMATION Версия программы: '3.4.0.48729'",
                out var first),
            "format 1 parsed");

        AssertEqual(
            "10-03-2025\t15:14:49.523\tINFO\tDEFAULT\tВерсия программы: '3.4.0.48729'",
            first.ToOutputLine(),
            "format 1 normalized");

        AssertTrue(
            LogStandardizer.TryParse(
                "2025-03-10 15:14:51.5882| INFO|11|MobileComputer.GetDeviceId| Код устройства: '@MINDEO-M40-D-410244015546'",
                out var second),
            "format 2 parsed");

        AssertEqual(
            "10-03-2025\t15:14:51.5882\tINFO\tMobileComputer.GetDeviceId\tКод устройства: '@MINDEO-M40-D-410244015546'",
            second.ToOutputLine(),
            "format 2 normalized");

        AssertFalse(LogStandardizer.TryParse("bad log line", out _), "invalid line rejected");
        AssertFalse(LogStandardizer.TryParse("2025-03-10 99:14:51.5882| INFO|11|M| message", out _), "invalid time rejected");
    }

    private static void AssertEqual<T>(T expected, T actual, string testName)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Test '{testName}' failed. Expected: {expected}. Actual: {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string testName)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Test '{testName}' failed.");
        }
    }

    private static void AssertFalse(bool condition, string testName)
    {
        if (condition)
        {
            throw new InvalidOperationException($"Test '{testName}' failed.");
        }
    }

    private static void AssertThrows<TException>(Action action, string testName)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Test '{testName}' failed. Expected exception: {typeof(TException).Name}.");
    }
}
