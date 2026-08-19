using System;
using OracleOfDereth;

internal static class Program
{
    private static int Main()
    {
        Assert(null, "null");
        Assert("", "\"\"");
        Assert("quote \" slash \\", "\"quote \\\" slash \\\\\"");
        Assert("\b\f\n\r\t", "\"\\b\\f\\n\\r\\t\"");
        Assert("a\u0001b", "\"a\\u0001b\"");
        Console.WriteLine("JSON encoding regression tests passed.");
        return 0;
    }

    private static void Assert(string input, string expected)
    {
        string actual = Util.JsonString(input);
        if (actual != expected) throw new InvalidOperationException($"Expected {expected}, got {actual}");
    }
}
