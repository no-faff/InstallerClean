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

    /// <summary>
    /// The product named is not installed ("This action is only valid for
    /// products that are currently installed", Microsoft's Windows Installer
    /// error-code table). Distinct from <see cref="UnknownProperty"/>, which is a
    /// record that exists and does not carry the property asked for; this is no
    /// record to ask. Observed live on the owner's machine through this project's
    /// own property reads, so it is a code this code path really meets rather
    /// than one taken out of a table.
    /// </summary>
    public const uint UnknownProduct = 1605;

    /// <summary>
    /// The patch named is not applied to the product named ("The patch isn't
    /// applied to this product", same table). The patch half of
    /// <see cref="UnknownProduct"/>: the pairing a claim describes does not
    /// exist, rather than existing and failing to be read.
    /// </summary>
    public const uint UnknownPatch = 1647;
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
    /// <summary>
    /// Path to the cached package. That is the whole of what Microsoft states for
    /// INSTALLPROPERTY_LOCALPACKAGE, and this line named a location on no source:
    /// "%windir%\Installer".
    ///
    /// DO NOT NARROW A PATH TEST ON IT. FileSystemScanService is written for values
    /// that fall outside that folder, which is why NamesFileDirectlyIn exists to
    /// separate the in-folder registrations from the rest and why the
    /// missing-from-disk counts deliberately cover every registration whose file has
    /// gone wherever it pointed. Read out of one machine's hive on 2026-08-28, all 147
    /// of its values are inside the folder in three spellings, and one machine cannot
    /// make the stronger claim true.
    /// </summary>
    public const string LocalPackage = "LocalPackage";

    /// <summary>Display name of the installed product.</summary>
    public const string ProductName = "ProductName";

    /// <summary>Patch state: 1=Applied, 2=Superseded, 4=Obsoleted.</summary>
    public const string State = "State";

    /// <summary>Whether the patch can be uninstalled ("1" = yes).</summary>
    public const string Uninstallable = "Uninstallable";

    /// <summary>
    /// Whether a product was installed as one of several instances of itself.
    /// Microsoft: "A missing value or a value of 0 (zero) indicates a normal
    /// product installation. A value of one (1) indicates a product installed
    /// using a multiple instance transform and the MSINEWINSTANCE property",
    /// and the property "is available for advertised or installed products"
    /// (<see href="https://learn.microsoft.com/en-us/windows/win32/api/msi/nf-msi-msigetproductinfoexw"/>).
    /// </summary>
    public const string InstanceType = "InstanceType";
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

    /// <summary>
    /// PID_TEMPLATE. What it holds depends on the kind of package, and the two
    /// meanings have nothing to do with each other: on an installation package
    /// it is the platform-and-language string ("Intel;1033"), and on a PATCH it
    /// is the semicolon-delimited list of product codes the patch may be applied
    /// to. Only the patch reading is used here, and only for a <c>.msp</c>.
    /// </summary>
    public const uint Template = 7;

    /// <summary>
    /// PID_REVNUMBER. On a patch this begins with the patch code, followed by
    /// the codes of the patches it obsoletes and then the package code, all as
    /// braced GUIDs run together with no separator. The leading 38 characters
    /// are therefore the patch's own identity, and a value whose length is not a
    /// whole multiple of 38 is one this fixed-width reading does not hold for.
    /// </summary>
    public const uint RevisionNumber = 9;
}

/// <summary>
/// Open modes for <c>MsiOpenDatabase</c>. msiquery.h declares these as pointer
/// values rather than strings (<c>#define MSIDBOPEN_READONLY (LPCTSTR)0</c>),
/// which is why the P/Invoke takes an <see cref="IntPtr"/>: the number IS the
/// argument, and no text is ever passed.
/// </summary>
public static class MsiDbOpen
{
    /// <summary>
    /// MSIDBOPEN_READONLY. Opens the database without a transaction and without
    /// taking a copy, and the source file is not modified. The only mode this
    /// app has any business using: every other one either writes or creates.
    /// </summary>
    public static readonly IntPtr ReadOnly = IntPtr.Zero;
}

/// <summary>
/// VARTYPE values returned by MsiSummaryInfoGetProperty in puiDataType.
/// The summary stream stores everything the project reads as VT_LPSTR;
/// the other VARTYPE values are documented under Microsoft's VARENUM
/// enumeration.
/// </summary>
public static class VtType
{
    public const uint String = 30; // VT_LPSTR
}
