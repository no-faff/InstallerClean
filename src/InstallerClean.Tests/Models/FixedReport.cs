using System.Reflection;
using InstallerClean.Models;

namespace InstallerClean.Tests.Models;

/// <summary>
/// A report with a distinct value in every field, built by reflection over the record
/// constructors so a field added to the report gets one too. The values are the same on
/// every runtime: each field takes the next of a fixed sequence, in the order the
/// constructors declare their parameters. The strings take in what the serialiser
/// escapes or passes through: letters outside ASCII, HTML-sensitive characters, quotes,
/// control characters, the two Unicode line separators, a surrogate pair and the empty
/// string, and every fourth string is null.
/// </summary>
internal static class FixedReport
{
    private static readonly string[] Texts =
    {
        "3.8.0", "en-GB", "Fran\u00E7ais \u65E5\u672C\u8A9E \u0420\u0443\u0441\u0441\u043A\u0438\u0439",
        "<a href=\"x\">&amp;</a> + 'q' `b`", "line\nbreak\r\ttab\\back\u0001\u001F\u007F",
        "\u2028\u2029\u00A0\u00AD\u200B\uFFFD", "face \uD83D\uDE00 end", "",
    };

    public static ResultLogEntry Build()
    {
        var next = 0;
        return (ResultLogEntry)Make(typeof(ResultLogEntry), ref next)!;
    }

    private static object? Make(Type type, ref int next)
    {
        next++;
        if (type == typeof(int))
            return next % 7 == 0 ? 0 : next % 11 == 0 ? -next : next % 13 == 0 ? int.MaxValue : next * 37;
        if (type == typeof(long))
            return next % 5 == 0 ? long.MaxValue - next : next * 1_000_003_000L;
        if (type == typeof(string))
            return next % 4 == 0 ? null : Texts[next % Texts.Length];
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            var element = type.GetGenericArguments()[0];
            var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
            list.Add(Make(element, ref next));
            list.Add(Make(element, ref next));
            return list;
        }

        var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"{type} has no public constructor to fill.");
        var parameters = constructor.GetParameters();
        var arguments = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            arguments[i] = Make(parameters[i].ParameterType, ref next);
        return constructor.Invoke(arguments);
    }
}
