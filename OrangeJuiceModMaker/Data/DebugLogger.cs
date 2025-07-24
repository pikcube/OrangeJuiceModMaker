using System;

namespace OrangeJuiceModMaker.Data;

public static class DebugLogger
{
    public static void LogLine(string o) => _log(o);

    public static void LogLine(object o) => LogLine(o.ToString() ?? string.Empty);

    private static Action<string> _log = _ => throw new Exception("DebugLogger not initialized");

    public static void Initialize(bool debug)
    {
        if (debug)
        {
            _log = Console.WriteLine;
        }
        else
        {
            _log = Log;
        }
    }

    private static void Log(string _)
    {
    }
}