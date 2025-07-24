using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OrangeJuiceModMaker;

public static partial class MyExtensions
{
    public static bool IsNumber(this string text) => MyRegex().IsMatch(text);

    public static bool IsInteger(this string text) => int.TryParse(text, out _);
    public static int ToInt(this string text) => int.Parse(text);
    public static int? ToIntOrNull(this string text) => int.TryParse(text, out int value) ? value : null;
    public static int ToIntOrDefault(this string text) => int.TryParse(text, out int value) ? value : 0;

    public static bool IsLong(this string text) => long.TryParse(text, out _);
    public static long ToLong(this string text) => long.Parse(text);
    public static long? ToLongOrNull(this string text) => long.TryParse(text, out long value) ? value : null;
    public static long ToLongOrDefault(this string text) => long.TryParse(text, out long value) ? value : 0;

    public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
    {
        foreach (T item in list)
        {
            action(item);
        }
    }

    public static async void ForEachAsync<T>(this IAsyncEnumerable<T> list, Action<T> action)
    {
        await foreach (T item in list)
        {
            action(item);
        }
    }

    public static string StripStart(this string s, int length) => s.Length > length ? s[length..] : "";
    public static string StripEnd(this string s, int length) => s.Length > length ? s[..^length] : "";

    public static string AsString(this IEnumerable<string> list) => string.Join(Environment.NewLine, list);

    public static bool CompareFiles(string path1, string path2)
    {
        if (!File.Exists(path1) || !File.Exists(path2))
        {
            return false;
        }

        byte[] f1 = File.ReadAllBytes(path1);
        byte[] f2 = File.ReadAllBytes(path2);

        return f1.SequenceEqual(f2);
    }

    public static int FindIndexOf<T>(this T[] array, Predicate<T> predicate)
    {
        for (int n = 0; n < array.Length; ++n)
        {
            if (predicate(array[n]))
            {
                return n;
            }
        }

        return -1;
    }

    public static bool CompareFiles(FileInfo info1, FileInfo info2) => CompareFiles(info1.FullName, info2.FullName);
    [GeneratedRegex("[^0-9]+")]
    private static partial Regex MyRegex();
}