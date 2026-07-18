namespace InstallerClean.Interop;

/// <summary>
/// Windows Installer API error codes returned by Msi* functions. Only
/// the values the code actually branches on are listed; the full set
/// is documented in Microsoft's MSI error reference.
/// </summary>
public static class MsiError
{
    public const uint Success = 0;
    public const uint AccessDenied = 5;
    public const uint MoreData = 234;
    public const uint NoMoreItems = 259;

    /// <summary>
    /// The record exists and does not carry the property asked for. Separates a
    /// benign absence (a product with no cached package) from a record that
    /// could not be read, which for LocalPackage is the difference between a
    /// file nothing claims and a file whose claim was lost; see
    /// <c>InstallerQueryService.IsBenignPropertyRead</c>.
    /// </summary>
    public const uint UnknownProperty = 1608;
}

/// <summary>
/// Installation context flags for MsiEnumProductsEx / MsiEnumPatchesEx /
/// MsiEnumComponentsEx. The values are msi.h's MSIINSTALLCONTEXT, declared
/// here in the header's own order: managed is 1 and unmanaged is 2, which is
/// the opposite of the order the names suggest. A context returned by the API
/// is handed straight back to the next call as an opaque number, so a
/// mislabelled member costs nothing until the first branch on one, and that
/// branch would then decide whether a file is registered.
/// </summary>
[Flags]
public enum MsiInstallContext : uint
{
    /// <summary>Per-user managed installation context (MSIINSTALLCONTEXT_USERMANAGED).</summary>
    UserManaged = 0x00000001,

    /// <summary>Per-user unmanaged installation context (MSIINSTALLCONTEXT_USERUNMANAGED).</summary>
    UserUnmanaged = 0x00000002,

    /// <summary>Per-machine installation context (MSIINSTALLCONTEXT_MACHINE).</summary>
    Machine = 0x00000004,

    /// <summary>
    /// All installation contexts (MSIINSTALLCONTEXT_ALL). Excludes
    /// ALLUSERMANAGED (8), which msi.h also leaves out of its own ALL.
    /// </summary>
    All = UserManaged | UserUnmanaged | Machine
}

/// <summary>
/// Filter flags for MsiEnumPatchesEx.
/// </summary>
[Flags]
public enum MsiPatchFilter : uint
{
    /// <summary>Include applied patches.</summary>
    Applied = 0x00000001,

    /// <summary>Include superseded patches (replaced by a newer patch).</summary>
    Superseded = 0x00000002,

    /// <summary>Include obsoleted patches.</summary>
    Obsoleted = 0x00000004,

    /// <summary>Include registered but not yet applied patches.</summary>
    Registered = 0x00000008,

    /// <summary>All patch states.</summary>
    All = Applied | Superseded | Obsoleted | Registered
}

/// <summary>
/// Install property name strings used with MsiGetProductInfoEx and
/// MsiGetPatchInfoEx.
/// </summary>
public static class MsiInstallProperty
{
    /// <summary>Local cached package path in %windir%\Installer.</summary>
    public const string LocalPackage = "LocalPackage";

    /// <summary>Display name of the installed product.</summary>
    public const string ProductName = "ProductName";

    /// <summary>Patch state: 1=Applied, 2=Superseded, 4=Obsoleted.</summary>
    public const string State = "State";

    /// <summary>Whether the patch can be uninstalled ("1" = yes).</summary>
    public const string Uninstallable = "Uninstallable";
}

/// <summary>
/// Property IDs for MsiSummaryInfoGetProperty.
/// </summary>
public static class MsiSummaryProperty
{
    public const uint Title    = 2;   // PID_TITLE
    public const uint Subject  = 3;   // PID_SUBJECT
    public const uint Author   = 4;   // PID_AUTHOR
    public const uint Keywords = 5;   // PID_KEYWORDS
    public const uint Comments = 6;   // PID_COMMENTS
    public const uint AppName  = 18;  // PID_APPNAME
}

/// <summary>
/// VARTYPE values returned by MsiSummaryInfoGetProperty in puiDataType.
/// The summary stream stores everything the project reads as VT_LPSTR;
/// other VARTYPEs are documented in Microsoft's STDOLE2 reference.
/// </summary>
public static class VtType
{
    public const uint String = 30; // VT_LPSTR
}
