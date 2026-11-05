using System.Reflection;
using System.Runtime.InteropServices;
using InstallerClean.Interop.Native;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Every COM interface Core declares, read off the compiled declarations. A COM interface
/// is called by its slot in the object's vtable, so a method declared out of order, a
/// method missing before the one called, or a base interface's methods left out calls the
/// wrong function, and none of that fails to compile.
/// </summary>
public class ComInterfaceDeclarationTests
{
    private static readonly Assembly Core = typeof(Kernel32).Assembly;

    /// <summary>Every interface in Core the runtime calls into COM through.</summary>
    internal static IReadOnlyList<Type> ComInterfaces() =>
        Core.GetTypes().Where(t => t.IsInterface && t.IsImport).ToList();

    /// <summary>An interface's own methods in the order they are declared, which is their slot order.</summary>
    internal static IReadOnlyList<MethodInfo> SlotOrder(Type type) =>
        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .OrderBy(m => m.MetadataToken)
            .ToList();

    /// <summary>
    /// The walk finds the interfaces it has to: the first and the last of the chain that
    /// opens a link. A walk that found none would pass every test below.
    /// </summary>
    [Fact]
    public void The_walk_finds_the_COM_interfaces()
    {
        var found = ComInterfaces();

        Assert.Contains(typeof(IShellWindows), found);
        Assert.Contains(typeof(IShellDispatch2), found);
    }

    /// <summary>
    /// Every COM interface is declared in Core, beside the other Windows declarations, so
    /// every test here reads it. The window app and the command line declare none.
    /// </summary>
    [Fact]
    public void No_COM_interface_is_declared_outside_Core()
    {
        var hosts = new[] { typeof(InstallerClean.Services.ConfirmationService).Assembly, typeof(InstallerClean.Cli.Program).Assembly };
        var outside = hosts
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsInterface && t.IsImport)
            .Select(t => t.FullName)
            .ToList();

        Assert.True(outside.Count == 0, string.Join("; ", outside));
    }

    /// <summary>
    /// Each interface names its header's IID and is laid out from IUnknown: the runtime
    /// supplies the three IUnknown slots and every slot after them is declared. None derives
    /// from another interface, because a COM interface gets no slots from an interface it
    /// derives from.
    /// </summary>
    [Fact]
    public void Every_COM_interface_is_laid_out_from_IUnknown_with_its_header_IID()
    {
        var wrong = new List<string>();
        foreach (var type in ComInterfaces())
        {
            var guid = type.GetCustomAttribute<GuidAttribute>()?.Value;
            if (!Header.TryGetValue(type, out var header))
                wrong.Add($"{type.Name}: no header listed");
            else if (!string.Equals(guid, header.Iid, StringComparison.OrdinalIgnoreCase))
                wrong.Add($"{type.Name}: Guid {guid ?? "(none)"}, the header has {header.Iid}");
            if (type.GetCustomAttribute<InterfaceTypeAttribute>()?.Value != ComInterfaceType.InterfaceIsIUnknown)
                wrong.Add($"{type.Name}: not InterfaceIsIUnknown");
            if (type.GetInterfaces().Length != 0)
                wrong.Add($"{type.Name}: derives from {string.Join(", ", type.GetInterfaces().Select(i => i.Name))}");
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }

    /// <summary>
    /// Every method returns its HRESULT as it is, so each call's answer is read rather than
    /// turned into an exception, and passes, by value or by address, only what the marshaller
    /// copies as it is: what a struct field may be (NativeDeclarationTests). A bool, a char, a
    /// string, an interface, a DateTime or a decimal would each be converted, and none is
    /// declared.
    /// </summary>
    [Fact]
    public void Every_COM_method_returns_its_HRESULT_and_passes_nothing_the_marshaller_converts()
    {
        var structs = NativeDeclarationTests.Structs();
        var wrong = new List<string>();
        foreach (var type in ComInterfaces())
        {
            foreach (var method in SlotOrder(type))
            {
                if (method.ReturnType != typeof(int))
                    wrong.Add($"{type.Name}.{method.Name}: returns {method.ReturnType.Name}");
                if ((method.MethodImplementationFlags & MethodImplAttributes.PreserveSig) == 0)
                    wrong.Add($"{type.Name}.{method.Name}: not PreserveSig");
                foreach (var parameter in method.GetParameters())
                {
                    var t = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
                    if (!NativeDeclarationTests.IsCopiedAsItIs(t, structs))
                        wrong.Add($"{type.Name}.{method.Name} {parameter.Name} ({parameter.ParameterType.FullName})");
                }
            }
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }

    // Each interface's IID, and its methods after IUnknown's three in slot order, each with
    // its parameter count, from the C form of its vtable in the Windows SDK headers (the
    // <Name>Vtbl struct), as far as the last method the code calls. An interface missing
    // here fails the test below until it is added.
    private static readonly Dictionary<Type, (string Iid, string[] Slots)> Header = new()
    {
        [typeof(IShellWindows)] = ("85cb6900-4d95-11cf-960c-0080c7f4ee85", new[]
        {
            "GetTypeInfoCount(1)", "GetTypeInfo(3)", "GetIDsOfNames(5)", "Invoke(8)", "get_Count(1)", "Item(2)",
            "_NewEnum(1)", "Register(4)", "RegisterPending(5)", "Revoke(1)", "OnNavigate(2)", "OnActivated(2)",
            "FindWindowSW(6)",
        }),
        [typeof(InstallerClean.Interop.Native.IServiceProvider)] = ("6d5140c1-7436-11ce-8034-00aa006009fa", new[]
        {
            "QueryService(3)",
        }),
        [typeof(IShellBrowser)] = ("000214e2-0000-0000-c000-000000000046", new[]
        {
            "GetWindow(1)", "ContextSensitiveHelp(1)", "InsertMenusSB(2)", "SetMenuSB(3)", "RemoveMenusSB(1)",
            "SetStatusTextSB(1)", "EnableModelessSB(1)", "TranslateAcceleratorSB(2)", "BrowseObject(2)",
            "GetViewStateStream(2)", "GetControlWindow(2)", "SendControlMsg(5)", "QueryActiveShellView(1)",
        }),
        [typeof(IShellView)] = ("000214e3-0000-0000-c000-000000000046", new[]
        {
            "GetWindow(1)", "ContextSensitiveHelp(1)", "TranslateAccelerator(1)", "EnableModeless(1)",
            "UIActivate(1)", "Refresh(0)", "CreateViewWindow(5)", "DestroyViewWindow(0)", "GetCurrentInfo(1)",
            "AddPropertySheetPages(3)", "SaveViewState(0)", "SelectItem(2)", "GetItemObject(3)",
        }),
        [typeof(IShellFolderViewDual)] = ("e7a1af80-4d96-11cf-960c-0080c7f4ee85", new[]
        {
            "GetTypeInfoCount(1)", "GetTypeInfo(3)", "GetIDsOfNames(5)", "Invoke(8)", "get_Application(1)",
        }),
        [typeof(IShellDispatch2)] = ("a4c6892c-3ba9-11d2-9dea-00c04fb16162", new[]
        {
            "GetTypeInfoCount(1)", "GetTypeInfo(3)", "GetIDsOfNames(5)", "Invoke(8)", "get_Application(1)",
            "get_Parent(1)", "NameSpace(2)", "BrowseForFolder(5)", "Windows(1)", "Open(1)", "Explore(1)",
            "MinimizeAll(0)", "UndoMinimizeALL(0)", "FileRun(0)", "CascadeWindows(0)", "TileVertically(0)",
            "TileHorizontally(0)", "ShutdownWindows(0)", "Suspend(0)", "EjectPC(0)", "SetTime(0)",
            "TrayProperties(0)", "Help(0)", "FindFiles(0)", "FindComputer(0)", "RefreshMenu(0)",
            "ControlPanelItem(1)", "IsRestricted(3)", "ShellExecute(5)",
        }),
    };

    /// <summary>
    /// Each interface declares exactly the slots its header's vtable has, in the header's
    /// order, under the header's names and with the header's number of parameters, from the
    /// first after IUnknown's up to the last the code calls.
    /// </summary>
    [Fact]
    public void Every_COM_interface_declares_its_header_slots_in_order()
    {
        var wrong = new List<string>();
        foreach (var type in ComInterfaces())
        {
            if (!Header.TryGetValue(type, out var header))
            {
                wrong.Add($"{type.Name}: no header listed");
                continue;
            }
            var declared = SlotOrder(type).Select(m => $"{m.Name}({m.GetParameters().Length})").ToList();
            for (var i = 0; i < Math.Max(declared.Count, header.Slots.Length); i++)
            {
                var have = i < declared.Count ? declared[i] : "(none)";
                var want = i < header.Slots.Length ? header.Slots[i] : "(none)";
                if (have != want) wrong.Add($"{type.Name} slot {i + 3}: {have}, the header has {want}");
            }
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }
}
