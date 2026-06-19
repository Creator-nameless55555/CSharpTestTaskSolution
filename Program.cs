using System;

namespace TestTaskSolution;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 1 && args[0] == "--test")
            {
                ManualTests.RunAll();
                return 0;
            }

            if (args.Length is < 2 or > 3)
            {
                Console.Error.WriteLine("Usage:");
                Console.Error.WriteLine("  dotnet run -- <input.log> <output.log> [problems.txt]");
                Console.Error.WriteLine("  dotnet run -- --test");
                return 1;
            }

            string inputPath = args[0];
            string outputPath = args[1];
            string? problemsPath = args.Length == 3 ? args[2] : null;

            LogStandardizer.StandardizeFile(inputPath, outputPath, problemsPath);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }
}
