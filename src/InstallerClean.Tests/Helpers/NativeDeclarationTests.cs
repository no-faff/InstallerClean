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

    /// <summary>
    /// Core's imports search System32 alone for the libraries they name, and no declaration
    /// widens its own search.
    /// </summary>
    [Fact]
    public void Every_library_is_looked_for_in_System32_alone()
    {
        Assert.Equal(
            DllImportSearchPath.System32,
            Core.GetCustomAttribute<DefaultDllImportSearchPathsAttribute>()?.Paths);
        var widened = Declarations()
            .Where(d => d.Method.GetCustomAttribute<DefaultDllImportSearchPathsAttribute>() is { } own
                && own.Paths != DllImportSearchPath.System32)
            .Select(d => Name(d.Method))
            .ToList();

        Assert.True(widened.Count == 0, string.Join("; ", widened));
    }

    [Fact]
    public void Every_declaration_names_its_entry_point_exactly()
    {
        var loose = Declarations()
            .Where(d => !d.Import.ExactSpelling || string.IsNullOrEmpty(d.Import.EntryPoint))
            .Select(d => Name(d.Method))
            .ToList();

        Assert.True(loose.Count == 0, string.Join("; ", loose));
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

        Assert.True(ansi.Count == 0, string.Join("; ", ansi));
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

        Assert.True(unstated.Count == 0, string.Join("; ", unstated));
    }

    /// <summary>
    /// A char[] is a buffer Windows writes into, and [Out] is what copies the writing back
    /// wherever the marshaller copies the array rather than pinning it. A text buffer is
    /// passed in no other form: a StringBuilder or a char[] by reference has copy rules of
    /// its own, and none is declared.
    /// </summary>
    [Fact]
    public void Every_char_buffer_is_an_Out_char_array()
    {
        var wrong = Declarations()
            .SelectMany(d => d.Method.GetParameters()
                .Where(p => (p.ParameterType == typeof(char[]) && !p.IsOut)
                    || p.ParameterType == typeof(StringBuilder)
                    || (p.ParameterType.IsByRef && p.ParameterType.GetElementType() is { } e
                        && (e == typeof(char[]) || e == typeof(StringBuilder))))
                .Select(p => $"{Name(d.Method)} {p.Name} ({p.ParameterType.Name})"))
            .ToList();

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
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

        Assert.True(undeclared.Count == 0, string.Join("; ", undeclared));
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
    /// Every struct a declaration or a COM interface method passes, by value or by
    /// reference, and every struct declared beside the declarations, with each struct
    /// inside one, a fixed buffer's own type included.
    /// </summary>
    internal static IReadOnlyCollection<Type> Structs()
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

        foreach (var method in Declarations().Select(d => d.Method)
                     .Concat(ComInterfaceDeclarationTests.ComInterfaces().SelectMany(ComInterfaceDeclarationTests.SlotOrder)))
        {
            foreach (var parameter in method.GetParameters()) Add(parameter.ParameterType);
            Add(method.ReturnType);
        }
        foreach (var type in Core.GetTypes())
            if (IsInteropStruct(type)) Add(type);

        while (pending.Count > 0)
            foreach (var field in pending.Dequeue().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                Add(field.FieldType);
        return seen;
    }

    /// <summary>A struct declared beside the declarations, in Core's interop namespace.</summary>
    private static bool IsInteropStruct(Type type) =>
        type.IsValueType && type.Assembly == Core && type.Namespace == typeof(Kernel32).Namespace;

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
    /// Every field of every struct is one the marshaller copies as it is: an integer, a
    /// floating-point number, an IntPtr, a pointer, an enum, a GUID, or another struct
    /// declared beside the declarations that the walk reached, whose own fields this
    /// checks in turn. The marshaller converts the rest, so a struct holding one is a
    /// different size or a different shape where Windows reads it than in memory: a bool
    /// becomes a four-byte BOOL, a char one byte under the default ANSI set, a DateTime an
    /// eight-byte OLE date, a decimal a DECIMAL, and a string or any other reference by
    /// rules of its own.
    /// </summary>
    [Fact]
    public void Every_struct_field_is_one_the_marshaller_copies_as_it_is()
    {
        var structs = Structs();
        var converted = structs
            .SelectMany(s => s.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !IsCopiedAsItIs(f.FieldType, structs))
                .Select(f => $"{s.FullName}.{f.Name} ({f.FieldType.FullName})"))
            .ToList();

        Assert.True(converted.Count == 0, string.Join("; ", converted));
    }

    internal static bool IsCopiedAsItIs(Type type, IReadOnlyCollection<Type> checkedStructs) =>
        type.IsPointer
        || type.IsEnum
        || (type.IsPrimitive && type != typeof(bool) && type != typeof(char))
        || type == typeof(Guid)
        || (IsInteropStruct(type) && checkedStructs.Contains(type));

    /// <summary>
    /// Each struct is the same size in memory as the marshaller makes it, so Windows reads
    /// and writes the bytes the code reads and writes, and a size the code passes Windows
    /// from <c>Unsafe.SizeOf</c> or <c>sizeof</c> is the size Windows checks. A struct the
    /// marshaller gives no size, such as a DateTime, whose layout the runtime chooses, fails.
    /// </summary>
    [Fact]
    public void Every_struct_is_the_size_in_memory_that_the_marshaller_makes_it()
    {
        var sizeOf = typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!;
        var differing = new List<string>();
        foreach (var s in Structs())
        {
            int marshalled;
            try
            {
                marshalled = Marshal.SizeOf(s);
            }
            catch (ArgumentException)
            {
                differing.Add($"{s.FullName}: the marshaller gives it no size");
                continue;
            }
            var memory = (int)sizeOf.MakeGenericMethod(s).Invoke(null, null)!;
            if (memory != marshalled) differing.Add($"{s.FullName}: {memory} in memory, {marshalled} marshalled");
        }

        Assert.True(differing.Count == 0, string.Join("; ", differing));
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

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
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

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }
}
