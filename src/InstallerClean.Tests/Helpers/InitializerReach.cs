using System.Reflection;
using System.Reflection.Emit;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Every member outside the app's own assemblies that a method can reach: each call, callvirt,
/// newobj, ldftn and ldvirtftn is followed into the app's own code, together with the type
/// initializer of every type of the app's whose method or field is reached, down to the first
/// member of another assembly on each path. An abstract or interface method of the app's own is
/// not followed but answered as <see cref="Unfollowed"/>, so a path through one cannot pass
/// unread.
/// </summary>
internal sealed class InitializerReach
{
    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(o => o.Value);

    private readonly HashSet<MethodBase> _visited = [];

    /// <summary>Members of other assemblies reached, each with the app method that names it.</summary>
    public List<(MemberInfo Member, MethodBase From)> Outside { get; } = [];

    /// <summary>Methods of the app's own that have no body to read.</summary>
    public List<MethodBase> Unfollowed { get; } = [];

    /// <summary>The app's own methods read, the starting one included.</summary>
    public IReadOnlyCollection<MethodBase> Own => _visited;

    public static bool IsOwn(Type? type) =>
        type?.Assembly.GetName().Name?.StartsWith("InstallerClean", StringComparison.Ordinal) == true;

    public static InitializerReach From(MethodBase start)
    {
        var reach = new InitializerReach();
        reach.Visit(start);
        return reach;
    }

    private void Visit(MethodBase method)
    {
        if (!_visited.Add(method)) return;
        if (method.DeclaringType?.TypeInitializer is { } cctor) Visit(cctor);

        var body = method.GetMethodBody();
        if (body is null)
        {
            if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0) Unfollowed.Add(method);
            return;
        }

        var il = body.GetILAsByteArray()!;
        var typeArgs = method.DeclaringType is { IsGenericType: true } t ? t.GetGenericArguments() : null;
        var methodArgs = method.IsGenericMethod ? method.GetGenericArguments() : null;
        var i = 0;
        while (i < il.Length)
        {
            short value = il[i++];
            if (value == 0xFE) value = unchecked((short)(0xFE00 | il[i++]));
            if (!OpCodesByValue.TryGetValue(value, out var op))
                throw new InvalidOperationException($"Unknown opcode 0x{value:X} in {method.DeclaringType}.{method.Name}.");

            switch (op.OperandType)
            {
                case OperandType.InlineMethod:
                    var token = BitConverter.ToInt32(il, i);
                    var called = method.Module.ResolveMethod(token, typeArgs, methodArgs)!;
                    if (IsOwn(called.DeclaringType)) Visit(called);
                    else Outside.Add((called, method));
                    break;
                case OperandType.InlineField:
                    var field = method.Module.ResolveField(BitConverter.ToInt32(il, i), typeArgs, methodArgs)!;
                    if (IsOwn(field.DeclaringType)) { if (field.DeclaringType!.TypeInitializer is { } fieldCctor) Visit(fieldCctor); }
                    else Outside.Add((field, method));
                    break;
                case OperandType.InlineType:
                case OperandType.InlineTok:
                    var member = method.Module.ResolveMember(BitConverter.ToInt32(il, i), typeArgs, methodArgs)!;
                    if (member is MethodBase m) { if (IsOwn(m.DeclaringType)) Visit(m); else Outside.Add((m, method)); }
                    else if (member is Type ty && !IsOwn(ty)) Outside.Add((ty, method));
                    else if (member is Type own && own.TypeInitializer is { } typeCctor) Visit(typeCctor);
                    break;
            }

            i += op.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, i),
                _ => 4,
            };
        }
    }

    /// <summary>
    /// Whether <paramref name="member"/> fills in the runtime's AppContext defaults where it is the
    /// first such call in the process: AppContext itself, any assembly's LocalAppContext or
    /// AppContext switch class, System.IO, Assembly.Location and FileIOPermission.
    /// </summary>
    public static bool FillsInAppContextDefaults(MemberInfo member)
    {
        var type = member as Type ?? member.DeclaringType;
        if (type is null) return false;
        var name = type.Name;
        var ns = type.Namespace ?? string.Empty;
        return type.FullName == "System.AppContext"
            || name == "LocalAppContext"
            || name.EndsWith("AppContextSwitches", StringComparison.Ordinal)
            || ns == "System.IO" || ns.StartsWith("System.IO.", StringComparison.Ordinal)
            || (member.Name is "get_Location" or "get_CodeBase" or "get_EscapedCodeBase" && typeof(Assembly).IsAssignableFrom(type))
            || type.FullName == "System.Security.Permissions.FileIOPermission";
    }
}
