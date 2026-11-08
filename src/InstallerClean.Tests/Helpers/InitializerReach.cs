using System.Reflection;
using System.Reflection.Emit;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Every member outside the app's own assemblies that a method can reach: each call, callvirt,
/// newobj, ldftn and ldvirtftn is followed into the app's own code, together with the type
/// initializer of every type of the app's whose method or field is reached, down to the first
/// member of another assembly on each path. A method of the app's own that has no body, or is
/// called through a virtual that an override could replace, is not followed but answered as
/// <see cref="Unfollowed"/>, so a path through one cannot pass unread.
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

    /// <summary>Methods of the app's own the walk could not read through.</summary>
    public List<MethodBase> Unfollowed { get; } = [];

    /// <summary>The app's own methods read, the starting one included.</summary>
    public IReadOnlyCollection<MethodBase> Own => _visited;

    public static bool IsOwn(Type? type) =>
        // The command line's assembly is installerclean-cli, in lower case.
        type?.Assembly.GetName().Name?.StartsWith("InstallerClean", StringComparison.OrdinalIgnoreCase) == true;

    public static InitializerReach From(MethodBase start)
    {
        var reach = new InitializerReach();
        reach.Visit(start);
        return reach;
    }

    private void Visit(MethodBase method, bool dispatched = false)
    {
        // A call through a virtual of the app's own may land in any override, which a walk of
        // the declared method alone would not read.
        if (dispatched && method.IsVirtual && !method.IsFinal && !(method.DeclaringType?.IsSealed ?? false))
        {
            Unfollowed.Add(method);
            return;
        }
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
                    if (IsOwn(called.DeclaringType)) Visit(called, dispatched: op == OpCodes.Callvirt || op == OpCodes.Ldvirtftn);
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
                    else if (member is FieldInfo f) { if (!IsOwn(f.DeclaringType)) Outside.Add((f, method)); else if (f.DeclaringType!.TypeInitializer is { } tokenCctor) Visit(tokenCctor); }
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
    /// Whether <paramref name="member"/>, a member of another assembly, is known not to reach
    /// AppContext. The list is of what a module initializer here may call: interop handles and
    /// errors, IntPtr, Nullable, delegates, List, Monitor, exception constructors and the type
    /// and module handles. Anything else is answered false, so a new call fails the check until
    /// it is read and added. A name-based ban would pass a framework member that reads
    /// AppContext inside itself, as Assembly.Location does through its path permission.
    /// </summary>
    public static bool KnownNotToReachAppContext(MemberInfo member)
    {
        var type = member as Type ?? member.DeclaringType;
        if (type is null) return false;
        if (type.IsGenericType) type = type.GetGenericTypeDefinition();
        var name = type.FullName ?? string.Empty;
        if (name is "System.Object" or "System.IntPtr" or "System.Nullable`1"
            or "System.Action`1" or "System.Func`2" or "System.Collections.Generic.List`1"
            or "System.Threading.Monitor")
            return true;
        if (name == "System.Runtime.InteropServices.Marshal")
            return member.Name is "GetHINSTANCE" or "GetLastWin32Error";
        if (name == "System.Type")
            return member.Name is "GetTypeFromHandle" or "get_Assembly";
        if (name == "System.Reflection.Assembly")
            return member.Name == "get_ManifestModule";
        if (member is ConstructorInfo && typeof(Exception).IsAssignableFrom(type))
            return true;
        return member is Type t && (t.IsValueType || t == typeof(object));
    }
}
