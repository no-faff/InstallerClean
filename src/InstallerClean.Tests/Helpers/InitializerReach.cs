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

    /// <summary>
    /// Whether <paramref name="member"/> is in one of the app's own assemblies. Read from its
    /// module, since a global method has no declaring type.
    /// </summary>
    public static bool IsOwn(MemberInfo? member) => member is not null && IsOwn(member.Module.Assembly);

    public static bool IsOwn(Assembly assembly) =>
        // The command line's assembly is installerclean-cli, in lower case.
        assembly.GetName().Name?.StartsWith("InstallerClean", StringComparison.OrdinalIgnoreCase) == true;

    public static InitializerReach From(MethodBase start)
    {
        var reach = new InitializerReach();
        reach.Visit(start);
        return reach;
    }

    /// <summary>
    /// <paramref name="roots"/> and every assembly they reference, directly or through each
    /// other, of which a copy sits in a root's own folder: the assemblies shipped beside the
    /// exes. A reference that has a copy there and does not load throws, so the set is never
    /// quietly short of one.
    /// </summary>
    public static IReadOnlyList<Assembly> ShippedClosure(params Assembly[] roots)
    {
        var folders = roots.Select(r => Path.GetDirectoryName(r.Location)!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        bool Shipped(string name) => folders.Any(f =>
            File.Exists(Path.Combine(f, name + ".dll")) || File.Exists(Path.Combine(f, name + ".exe")));

        var found = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        var notShipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<Assembly>(roots);
        while (queue.Count > 0)
        {
            var assembly = queue.Dequeue();
            var assemblyName = assembly.GetName().Name!;
            if (found.ContainsKey(assemblyName)) continue;
            found[assemblyName] = assembly;
            foreach (var name in assembly.GetReferencedAssemblies())
            {
                if (found.ContainsKey(name.Name!) || notShipped.Contains(name.Name!)) continue;
                if (!Shipped(name.Name!)) { notShipped.Add(name.Name!); continue; }
                queue.Enqueue(LoadShipped(name));
            }
        }
        return found.Values.ToList();
    }

    // A reference to another version than the copy beside the exes is redirected to that copy,
    // so the copy of that name is the one loaded.
    private static Assembly LoadShipped(AssemblyName name)
    {
        const int DefinitionDoesNotMatchReference = unchecked((int)0x80131040);
        try { return Assembly.Load(name); }
        catch (FileLoadException ex) when (ex.HResult == DefinitionDoesNotMatchReference)
        {
            return Assembly.Load(new AssemblyName(name.Name!));
        }
    }

    /// <summary>
    /// The module type initializer of each module of <paramref name="assembly"/>: the method
    /// the runtime runs as the module is first touched, which calls every method the compiler
    /// was given as a module initializer. The module type's methods are the first rows of the
    /// method table, so the walk stops at the first method that belongs to a type.
    /// </summary>
    public static IReadOnlyList<MethodBase> ModuleTypeInitializers(Assembly assembly)
    {
        var found = new List<MethodBase>();
        foreach (var module in assembly.GetModules())
        {
            for (var token = 0x06000001; ; token++)
            {
                MethodBase method;
                try { method = module.ResolveMethod(token)!; }
                catch (ArgumentException) { break; }
                if (method.DeclaringType is not null) break;
                // IsConstructor answers for instance constructors only.
                if (method is ConstructorInfo && method.IsStatic) found.Add(method);
            }
        }
        return found;
    }

    /// <summary>
    /// Every method of <paramref name="assembly"/> marked as a module initializer, matched by
    /// the attribute's name because on .NET Framework the attribute is a copy the build adds.
    /// </summary>
    public static IReadOnlyList<MethodBase> MarkedModuleInitializers(Assembly assembly) =>
        assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributesData().Any(a =>
                a.AttributeType.FullName == "System.Runtime.CompilerServices.ModuleInitializerAttribute"))
            .ToList<MethodBase>();

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
                    if (IsOwn(called)) Visit(called, dispatched: op == OpCodes.Callvirt || op == OpCodes.Ldvirtftn);
                    else Outside.Add((called, method));
                    break;
                case OperandType.InlineField:
                    var field = method.Module.ResolveField(BitConverter.ToInt32(il, i), typeArgs, methodArgs)!;
                    if (IsOwn(field)) { if (field.DeclaringType?.TypeInitializer is { } fieldCctor) Visit(fieldCctor); }
                    else Outside.Add((field, method));
                    break;
                case OperandType.InlineType:
                case OperandType.InlineTok:
                    var member = method.Module.ResolveMember(BitConverter.ToInt32(il, i), typeArgs, methodArgs)!;
                    if (member is MethodBase m) { if (IsOwn(m)) Visit(m); else Outside.Add((m, method)); }
                    else if (member is FieldInfo f) { if (!IsOwn(f)) Outside.Add((f, method)); else if (f.DeclaringType?.TypeInitializer is { } tokenCctor) Visit(tokenCctor); }
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
    /// AppContext. The list is of what a module initializer here may call, member by member:
    /// interop handles and errors, IntPtr, Nullable, delegates built and invoked, an empty List
    /// added to, the type and module handles, and two exception constructors that take their
    /// message as given. Anything else is answered false,
    /// so a new call fails the check until it is read and added. A name-based ban would pass a
    /// framework member that reads AppContext inside itself, as Assembly.Location does through
    /// its path permission.
    /// </summary>
    /// <remarks>
    /// On .NET Framework 4.8 the two constructors only set the exception's fields, down through
    /// Exception's own. The other constructors are not on the list: Win32Exception's without a
    /// message asks Windows for one, and the framework's parameterless ones look theirs up in
    /// the framework's resources.
    /// </remarks>
    public static bool KnownNotToReachAppContext(MemberInfo member)
    {
        var type = member as Type ?? member.DeclaringType;
        if (type is null) return false;
        if (type.IsGenericType) type = type.GetGenericTypeDefinition();
        var name = type.FullName ?? string.Empty;
        if (name == "System.Object")
            return member is Type || member.Name == ".ctor";
        if (name == "System.IntPtr")
            return member is Type || member.Name is ".ctor" or "Zero" or "op_Equality" or "op_Inequality";
        if (name == "System.Nullable`1")
            return member is Type || member.Name is ".ctor" or "get_HasValue" or "GetValueOrDefault";
        if (name is "System.Action`1" or "System.Func`2")
            return member is Type || member.Name is ".ctor" or "Invoke";
        if (name == "System.Collections.Generic.List`1")
            return member is Type || (member is ConstructorInfo c && c.GetParameters().Length == 0) || member.Name == "Add";
        if (name == "System.Runtime.InteropServices.Marshal")
            return member.Name is "GetHINSTANCE" or "GetLastWin32Error";
        if (name == "System.Type")
            return member.Name is "GetTypeFromHandle" or "get_Assembly";
        if (name == "System.Reflection.Assembly")
            return member.Name == "get_ManifestModule";
        if (member is ConstructorInfo constructor)
        {
            var parameters = string.Join(",", constructor.GetParameters().Select(p => p.ParameterType.FullName));
            return (name, parameters) is ("System.InvalidOperationException", "System.String")
                or ("System.ComponentModel.Win32Exception", "System.Int32,System.String");
        }
        return member is Type t && (t.IsValueType || t == typeof(object));
    }
}
