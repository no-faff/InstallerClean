using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using InstallerClean.Interop.Native;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Every Windows function Core declares, read off the declarations themselves, and every
/// struct those declarations pass. A declaration that compiles can still pass Windows the
/// wrong bytes: a string or a char in the ANSI character set reaches a W function as one
/// byte a character, a bool goes as whatever width the marshaller picks, and a char[] the
/// marshaller copies comes back unchanged without [Out]. These find each of those by what
/// the compiled declaration says, whichever file it is in.
/// </summary>
public class NativeDeclarationTests
{
    private static readonly Assembly Core = typeof(Kernel32).Assembly;

    /// <summary>
    /// Every method in Core that the runtime calls into a native library for, found by the
    /// method's own P/Invoke flag rather than by an attribute, with its DllImport.
    /// </summary>
    private static IReadOnlyList<(MethodInfo Method, DllImportAttribute Import)> Declarations()
    {
        var found = new List<(MethodInfo, DllImportAttribute)>();
        foreach (var type in Core.GetTypes())
        {
            foreach (var method in type.GetMethods(
                         BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0) continue;
                var import = method.GetCustomAttribute<DllImportAttribute>()
                    ?? throw new InvalidOperationException($"{Name(method)} is a P/Invoke with no DllImport to read.");
                found.Add((method, import));
            }
        }
        return found;
    }

    private static string Name(MethodInfo method) => $"{method.DeclaringType!.Name}.{method.Name}";

    /// <summary>
    /// The walk finds the declarations it has to: one in each of three files, the console
    /// handler's among them. A walk that found none would pass every test below.
    /// </summary>
    [Fact]
    public void The_walk_finds_the_declarations()
    {
        var names = Declarations().Select(d => Name(d.Method)).ToList();

        Assert.Contains("Msi.MsiEnumProductsEx", names);
        Assert.Contains("Kernel32.CreateFile", names);
        Assert.Contains("Kernel32.SetConsoleCtrlHandler", names);
        Assert.Contains("Ntdll.NtCreateFile", names);
    }

    [Fact]
    public void Every_declaration_names_its_entry_point_exactly()
    {
        var loose = Declarations()
            .Where(d => !d.Import.ExactSpelling || string.IsNullOrEmpty(d.Import.EntryPoint))
            .Select(d => Name(d.Method))
            .ToList();

        Assert.Empty(loose);
    }

    /// <summary>
    /// A W function reads text as UTF-16, so every declaration of one, and every declaration
    /// passing a string, a char, a char[] or a StringBuilder, is CharSet.Unicode.
    /// </summary>
    [Fact]
    public void Every_declaration_passing_text_is_Unicode()
    {
        var ansi = Declarations()
            .Where(d => d.Import.CharSet != CharSet.Unicode
                && (d.Import.EntryPoint!.EndsWith("W", StringComparison.Ordinal)
                    || d.Method.GetParameters().Any(p => IsText(p.ParameterType))
                    || IsText(d.Method.ReturnType)))
            .Select(d => $"{Name(d.Method)} ({d.Import.EntryPoint}, {d.Import.CharSet})")
            .ToList();

        Assert.Empty(ansi);
    }

    private static bool IsText(Type type)
    {
        var t = type.IsByRef || type.IsArray ? type.GetElementType()! : type;
        return t == typeof(string) || t == typeof(char) || t == typeof(StringBuilder);
    }

    /// <summary>Every bool, passed or returned, is marshalled as the four-byte Win32 BOOL.</summary>
    [Fact]
    public void Every_bool_is_a_Win32_BOOL()
    {
        var unstated = new List<string>();
        foreach (var (method, _) in Declarations())
        {
            foreach (var parameter in method.GetParameters().Append(method.ReturnParameter))
            {
                var type = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
                if (type != typeof(bool)) continue;
                if (parameter.GetCustomAttribute<MarshalAsAttribute>()?.Value != UnmanagedType.Bool)
                    unstated.Add($"{Name(method)} {(parameter.Position < 0 ? "return" : parameter.Name)}");
            }
        }

        Assert.Empty(unstated);
    }

    /// <summary>
    /// A char[] is a buffer Windows writes into, and [Out] is what copies the writing back
    /// wherever the marshaller copies the array rather than pinning it.
    /// </summary>
    [Fact]
    public void Every_char_buffer_is_Out()
    {
        var inOnly = Declarations()
            .SelectMany(d => d.Method.GetParameters()
                .Where(p => p.ParameterType == typeof(char[]) && !p.IsOut)
                .Select(p => $"{Name(d.Method)} {p.Name}"))
            .ToList();

        Assert.Empty(inOnly);
    }

    /// <summary>A delegate Windows calls back is called with the Windows calling convention.</summary>
    [Fact]
    public void Every_callback_is_declared_for_Windows_calling_convention()
    {
        var undeclared = Declarations()
            .SelectMany(d => d.Method.GetParameters()
                .Where(p => typeof(Delegate).IsAssignableFrom(p.ParameterType)
                    && p.ParameterType.GetCustomAttribute<UnmanagedFunctionPointerAttribute>()?.CallingConvention
                        != CallingConvention.Winapi)
                .Select(p => $"{Name(d.Method)} {p.Name}"))
            .ToList();

        Assert.Empty(undeclared);
    }

    // Each struct's size on x64, from its declaration in the Windows SDK headers. A struct
    // passed to Windows that is missing here fails the test below until its size is added.
    private static readonly Dictionary<Type, int> HeaderSize = new()
    {
        [typeof(Kernel32.WIN32_FIND_DATA)] = 592,
        [typeof(Kernel32.FILETIME)] = 8,
        [typeof(Kernel32.BY_HANDLE_FILE_INFORMATION)] = 52,
        [typeof(Kernel32.FILE_ID_INFO)] = 24,
        [typeof(Kernel32.FILE_BASIC_INFO)] = 40,
        [typeof(Kernel32.SECURITY_ATTRIBUTES)] = 24,
        [typeof(Kernel32.FILE_DISPOSITION_INFO_EX)] = 4,
        [typeof(Kernel32.FILE_DISPOSITION_INFO)] = 1,
        [typeof(Kernel32.STARTUPINFO)] = 104,
        [typeof(Kernel32.PROCESS_INFORMATION)] = 24,
        [typeof(Ntdll.UNICODE_STRING)] = 16,
        [typeof(Ntdll.OBJECT_ATTRIBUTES)] = 48,
        [typeof(Ntdll.IO_STATUS_BLOCK)] = 16,
        [typeof(User32.MONITORINFO)] = 40,
        [typeof(User32.RECT)] = 16,
        [typeof(User32.USEROBJECTFLAGS)] = 12,
        [typeof(VARIANT)] = 24,
        [typeof(Guid)] = 16,
    };

    /// <summary>
    /// Every struct a declaration passes, by value or by reference, and every struct
    /// declared beside the declarations, with each struct inside one, a fixed buffer's
    /// own type included.
    /// </summary>
    private static IReadOnlyCollection<Type> Structs()
    {
        var seen = new HashSet<Type>();
        var pending = new Queue<Type>();
        void Add(Type type)
        {
            var t = type.IsByRef || type.IsArray || type.IsPointer ? type.GetElementType()! : type;
            if (!t.IsValueType || t.IsPrimitive || t.IsEnum || t == typeof(void) || t == typeof(IntPtr) || t == typeof(UIntPtr))
                return;
            if (seen.Add(t)) pending.Enqueue(t);
        }

        foreach (var (method, _) in Declarations())
        {
            foreach (var parameter in method.GetParameters()) Add(parameter.ParameterType);
            Add(method.ReturnType);
        }
        foreach (var type in Core.GetTypes())
            if (type.Namespace == typeof(Kernel32).Namespace) Add(type);

        while (pending.Count > 0)
            foreach (var field in pending.Dequeue().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                Add(field.FieldType);
        return seen;
    }

    private static bool IsFixedBufferType(Type type) => type.IsDefined(typeof(CompilerGeneratedAttribute));

    [Fact]
    public void The_struct_walk_reaches_the_folder_entry_and_its_name_buffers()
    {
        var structs = Structs();

        Assert.Contains(typeof(Kernel32.WIN32_FIND_DATA), structs);
        Assert.Contains(typeof(Kernel32.FILETIME), structs);
        Assert.Contains(structs, IsFixedBufferType);
    }

    /// <summary>
    /// No struct holds a bool, a char or a reference. The marshaller converts each of those,
    /// a char to one byte under the default ANSI set, so a struct holding one is a different
    /// size or a different shape in Windows' hands than in memory.
    /// </summary>
    [Fact]
    public void No_struct_holds_a_field_the_marshaller_converts()
    {
        var converted = Structs()
            .SelectMany(s => s.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => f.FieldType == typeof(bool) || f.FieldType == typeof(char) || !f.FieldType.IsValueType)
                .Select(f => $"{s.FullName}.{f.Name} ({f.FieldType.Name})"))
            .ToList();

        Assert.Empty(converted);
    }

    /// <summary>
    /// Each struct is the same size in memory as the marshaller makes it, so Windows reads
    /// and writes the bytes the code reads and writes, and a size the code passes Windows
    /// from <c>Unsafe.SizeOf</c> or <c>sizeof</c> is the size Windows checks.
    /// </summary>
    [Fact]
    public void Every_struct_is_the_size_in_memory_that_the_marshaller_makes_it()
    {
        var sizeOf = typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!;
        var differing = Structs()
            .Select(s => (Struct: s, Memory: (int)sizeOf.MakeGenericMethod(s).Invoke(null, null)!, Marshalled: Marshal.SizeOf(s)))
            .Where(s => s.Memory != s.Marshalled)
            .Select(s => $"{s.Struct.FullName}: {s.Memory} in memory, {s.Marshalled} marshalled")
            .ToList();

        Assert.Empty(differing);
    }

    /// <summary>Each struct is the size its Windows SDK declaration gives on x64.</summary>
    [Fact]
    public void Every_struct_is_the_size_its_header_gives()
    {
        Assert.Equal(8, IntPtr.Size);

        var wrong = new List<string>();
        foreach (var s in Structs().Where(s => !IsFixedBufferType(s)))
        {
            if (!HeaderSize.TryGetValue(s, out var size)) wrong.Add($"{s.FullName}: no header size listed");
            else if (Marshal.SizeOf(s) != size) wrong.Add($"{s.FullName}: {Marshal.SizeOf(s)}, the header gives {size}");
        }

        Assert.Empty(wrong);
    }

    /// <summary>Every enum a declaration passes is the four bytes of the DWORD or C enum it stands for.</summary>
    [Fact]
    public void Every_enum_passed_is_four_bytes()
    {
        var wrong = Declarations()
            .SelectMany(d => d.Method.GetParameters().Select(p => p.ParameterType).Append(d.Method.ReturnType))
            .Select(t => t.IsByRef ? t.GetElementType()! : t)
            .Where(t => t.IsEnum && Marshal.SizeOf(Enum.GetUnderlyingType(t)) != 4)
            .Select(t => t.FullName)
            .Distinct()
            .ToList();

        Assert.Empty(wrong);
    }
}
