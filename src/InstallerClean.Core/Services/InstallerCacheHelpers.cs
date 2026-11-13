using System.Buffers;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using Microsoft.Win32.SafeHandles;

namespace InstallerClean.Services;

/// <summary>
/// What <see cref="InstallerCacheHelpers.ResolveFinalPathOutcome(string, out string)"/> established,
/// with the five ways it can fail kept apart rather than collapsed into one
/// <c>false</c>.
///
/// ALL FIVE WITHHOLD, ON THE ONE THING TRUE OF EVERY MEMBER: the resolver was asked
/// about a recorded path and did not answer. A registration whose spelling the
/// filesystem would not settle is compared in a form the folder walk never produces,
/// so any candidate in the list, and any superseded row on the offer, can be the cached
/// file it names, and which one it is cannot be established. That holds for every
/// member alike.
///
/// ALL FIVE WITHHOLD ALIKE, THE TWO ORDINARY MACHINE STATES INCLUDED. Where the app
/// can detect that one of its own checks did not answer, it offers nothing that scan,
/// and how often a condition arises is not admissible in that decision. So an
/// unattached drive and a refused handle withhold exactly as the other three do, and
/// nothing here splits the five.
///
/// A REGISTRATION WHOSE FILE IS SIMPLY GONE REACHES NONE OF THESE, which is what
/// keeps the rule off ordinary machines. The walk climbs to an existing ancestor and
/// reattaches the missing suffix as text, so a missing file resolves normally and
/// answers <see cref="Resolved"/>.
///
/// NOTHING IN THE APPLICATION BRANCHES ON WHICH MEMBER IT IS: every containment gate
/// asks the bool question. The counts travel to the opt-in report AND decide the
/// offer, through <c>EnumerationCensus.AnyRecordedPathUnestablished</c>, which is the
/// single place the question is asked.
/// </summary>
internal enum PathResolution
{
    /// <summary>
    /// The kernel expanded the path, so the out value is a proven location rather
    /// than a spelling. The only member any gate treats as success.
    /// </summary>
    Resolved,

    /// <summary>
    /// <see cref="FullPaths.GetFullPath(string)"/> refused the string outright: one of
    /// nothing but spaces, an embedded null, or a length past the API's limit. The
    /// value names nothing that can be looked for and cannot be improved.
    /// </summary>
    NotAPath,

    /// <summary>
    /// Nothing on the path exists, all the way up to the root, so there was no
    /// ancestor to open a handle on.
    /// </summary>
    NoExistingAncestor,

    /// <summary>
    /// An ancestor exists and <c>CreateFile</c> would not hand back a handle on
    /// it.
    /// </summary>
    OpenRefused,

    /// <summary>
    /// The handle opened and <c>GetFinalPathNameByHandle</c> answered zero, on
    /// either the first call or the resized retry.
    /// </summary>
    FinalNameUnavailable,

    /// <summary>
    /// The attempt threw. Distinct from every member above, each of which is the
    /// call answering rather than failing to complete.
    /// </summary>
    Faulted,
}

internal static class InstallerCacheHelpers
{
    internal static readonly string InstallerFolder =
        PathText.Combine(KnownFolders.GetPath(Environment.SpecialFolder.Windows), "Installer");

    /// <summary>
    /// True if <paramref name="path"/> resolves to <c>C:\Windows\Installer</c>
    /// or any descendant after symlinks/junctions/subst-mapped drives are
    /// expanded, or reaches it through this PC's own share
    /// (<see cref="ResolvesInsideInstallerFolder"/>). Used as the bottom-line
    /// safety check before any move: the entire restore-after-mistakes story
    /// collapses if files end up back inside the Installer folder.
    /// </summary>
    internal static bool IsInstallerFolderOrChild(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        var proven = TryResolveFinalPath(path, out var resolvedInput);
        return ResolvesInsideInstallerFolder(resolvedInput, proven);
    }

    /// <summary>
    /// The containment comparison itself, over an ALREADY-resolved input. This
    /// is the DESTINATION side's question ("is this forbidden"), so a
    /// descendant counts; the source side asks a narrower one and takes
    /// <see cref="ResolvesDirectlyInInstallerFolder"/> instead. Both are paired
    /// with <see cref="TryResolveFinalPath"/> by their callers, so that a
    /// source gate can refuse a path whose resolution degraded; this gate
    /// answers "not inside" for one where the text does not match, and the two
    /// sides want opposite answers for it. An unresolvable answer of "not
    /// forbidden" lets a move proceed, where refusing would strand a user with
    /// no destination they could use. The source gate asks "is this in bounds",
    /// and the whole reason it exists is that a corrupt LocalPackage value can
    /// name a file anywhere on disk, so "could not prove it is in bounds" and
    /// "out of bounds" earn the same refusal.
    ///
    /// The cache root is compared in its best-effort form even when ITS
    /// resolution degraded. A fully resolved input is a real canonical path, so
    /// a root left unexpanded cannot match it as text and the comparison answers
    /// "not inside", which this gate reads as permission. The strict form below
    /// compares the root the same way, where the same answer becomes the source
    /// side's refusal.
    ///
    /// WHERE THE TEXT DOES NOT MATCH, THE LOCAL PATH IS COMPARED TOO
    /// (<see cref="LocalPathOf"/>). The Installer folder reached through this PC's
    /// own share, <c>\\localhost\C$\Windows\Installer</c>, the same with the
    /// computer's name, or a drive mapped onto that share, resolves to the UNC
    /// spelling and not to the root's, and it is the root all the same.
    /// </summary>
    /// <param name="proven">
    /// Whether the kernel resolved <paramref name="resolvedInput"/>, as
    /// <see cref="TryResolveFinalPath"/> answers it.
    /// </param>
    /// <param name="localPathOf">
    /// Gives the local path of a folder reached through a share. Null in production
    /// (<see cref="LocalPathThroughThisPCsVolumes"/>), which asks the real machine; only a test
    /// passes one, through these two and never through the gates above them.
    /// </param>
    internal static bool ResolvesInsideInstallerFolder(
        string resolvedInput, bool proven, LocalPathReader? localPathOf = null)
    {
        var installer = ResolveFinalPath(InstallerFolder)
            .TrimEnd(Path.DirectorySeparatorChar);

        if (IsAtOrUnder(resolvedInput, installer)) return true;

        return LocalPathOf(resolvedInput, proven, [installer], localPathOf) is { } local
            && IsAtOrUnder(local, installer);
    }

    /// <summary>
    /// Whether <paramref name="input"/> is <paramref name="root"/> or a path below it,
    /// compared as text with trailing separators off the input. The root is given
    /// without one.
    /// </summary>
    private static bool IsAtOrUnder(string input, string root)
    {
        var trimmed = input.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return trimmed.Equals(root, StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <paramref name="resolvedInput"/>, already resolved, as the path of the same folder on
    /// this PC's own volume where it reaches that folder through a share, for a destination
    /// gate to compare as text; null where the input is not a share path, did not resolve, or
    /// its local path is not established.
    ///
    /// A SHARE ON THIS PC IS FOLLOWED BACK TO ITS FOLDER (<see cref="LocalPathThroughThisPCsVolumes"/>).
    /// So <c>\\localhost\C$\Windows\Installer\Moved</c> gives <c>C:\Windows\Installer\Moved</c>,
    /// and a share made on a folder inside a root, such as <c>print$</c> on
    /// <c>C:\Windows\System32\spool\drivers</c>, gives the path inside the root. A drive mapped
    /// onto a share resolves to the share's UNC spelling, so it is asked about the same way.
    ///
    /// ONLY A SHARE PATH IS ASKED ABOUT. The kernel's resolution of a local path is already the
    /// local path, and a volume carrying the same serial number as a root's, such as a clone of
    /// the system drive, would otherwise be read against the root's volume.
    ///
    /// WHERE THE LOCAL PATH IS NOT ESTABLISHED, THE GATE COMPARES THE SPELLING ALONE, as it does
    /// for a destination that does not resolve: refusing would leave a person with no
    /// destination they could use.
    /// </summary>
    internal static string? LocalPathOf(
        string resolvedInput, bool proven, IReadOnlyList<string> roots, LocalPathReader? localPathOf)
    {
        if (!proven || !IsUncPath(resolvedInput)) return null;

        return (localPathOf ?? LocalPathThroughThisPCsVolumes)(resolvedInput, roots);
    }

    /// <summary>
    /// Whether <paramref name="resolvedPath"/> is a share path, <c>\\server\share\...</c>, as a
    /// resolved path through a share or a drive mapped onto one is spelled. A path that keeps a
    /// <c>\\?\</c> or <c>\\.\</c> in front, such as a volume with no drive letter, is local.
    /// </summary>
    internal static bool IsUncPath(string resolvedPath) =>
        resolvedPath.StartsWith(@"\\", StringComparison.Ordinal)
        && !resolvedPath.StartsWith(@"\\?\", StringComparison.Ordinal)
        && !resolvedPath.StartsWith(@"\\.\", StringComparison.Ordinal);

    /// <summary>
    /// The local path of the folder <paramref name="sharePath"/> reaches through a share, with
    /// the part of the path below the nearest folder that is there put back on after it; null
    /// where that folder does not open as a folder on the volume of one of
    /// <paramref name="roots"/> (<see cref="ThroughThisPCsVolumesKernel"/>), or where the local
    /// path does not end in the folder names the share path has below its share root.
    ///
    /// THE FOLDER NAMES MUST AGREE. A volume on another computer carries the serial number of
    /// this PC's system drive where both are images of one disk, and a file id read off it can
    /// name some other folder here. Requiring the local path to end in the same names as the
    /// share path below <c>\\server\share</c> means such a folder is never read as the one its id
    /// names here.
    /// </summary>
    internal static string? LocalPathThroughThisPCsVolumes(string sharePath, IReadOnlyList<string> roots)
    {
        if (ResolveFinalPathOutcome(sharePath, out var local, new ThroughThisPCsVolumesKernel(roots))
            != PathResolution.Resolved)
            return null;

        var trimmedShare = PathText.TrimEndingDirectorySeparator(sharePath);
        var shareRoot = PathText.GetPathRoot(trimmedShare) ?? string.Empty;
        var below = trimmedShare.Substring(shareRoot.Length)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (below.Length == 0) return local;

        return PathText.TrimEndingDirectorySeparator(local)
            .EndsWith(Path.DirectorySeparatorChar + below, StringComparison.OrdinalIgnoreCase)
                ? local
                : null;
    }

    /// <summary>
    /// The stricter form the SOURCE gate takes, over an ALREADY-resolved input:
    /// the path must sit DIRECTLY in the cache root, not merely somewhere under
    /// it. Nothing below the root is ever a candidate. The orphan walk is
    /// root-only (<c>FileSystemScanService.GetInstallerFiles</c>), because a
    /// file in a subdirectory is one the registration data says nothing about,
    /// and <c>$PatchCache$</c> in particular holds the patch engine's baseline
    /// payload copies. A registered <c>LocalPackage</c> value names a file at
    /// the root too, so everything the descendant form additionally admitted was
    /// illegitimate: the cache folder itself, and a corrupt registration
    /// pointing into a subtree the app had deliberately put out of scope.
    /// SECURITY.md tells a reporter the app never acts inside a subfolder; this
    /// is where that holds rather than happens to be true.
    ///
    /// The root itself answers false: a candidate is a file, and the folder is
    /// not one.
    ///
    /// The root arrives already resolved, as an <see cref="InstallerCacheRoot"/>
    /// the caller made once for the whole run; the destination form above still
    /// resolves its own, being asked once per batch rather than once per file.
    /// </summary>
    internal static bool ResolvesDirectlyInInstallerFolder(string resolvedInput, InstallerCacheRoot root)
    {
        if (string.IsNullOrWhiteSpace(resolvedInput)) return false;

        var input = resolvedInput
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var parent = PathText.GetDirectoryName(input);
        return parent is not null
            && parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Equals(root.Resolved, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <paramref name="value"/>, a path Windows Installer recorded, with its environment
    /// variables expanded from this process's environment. A variable that is not set
    /// is left as it was, '%' signs and all.
    ///
    /// REFUSE A VALUE HOLDING A NULL BEFORE CALLING THIS. On Windows the expansion cuts
    /// such a value at the null and does not throw, and what comes back is a shorter,
    /// well-formed path that can name a real file.
    ///
    /// The recorded cached-package path is expanded here, and so is the <c>InstallSource</c>
    /// of a product key no installation accounts for, where the answer only ever keeps a
    /// file (<c>ExpandedInstallSource</c>). Do not pass a source-list entry through it: an entry
    /// naming a variable keeps its copy in <see cref="DeclaredProductCheck"/>
    /// (<c>AddSourcePackages</c>), and an expanded one would be compared instead.
    /// </summary>
    internal static string ExpandRecordedPath(string value) =>
        Environment.ExpandEnvironmentVariables(value);

    /// <summary>
    /// Whether <paramref name="path"/>, not yet resolved, names a file directly in the
    /// Installer folder once the kernel has expanded it: true or false where the
    /// comparison was made, and null where it could not be.
    ///
    /// ONLY A PROVEN LOCATION IS COMPARED. A root the kernel never expanded, and a path
    /// that answers anything but <see cref="PathResolution.Resolved"/>, give null,
    /// because a spelling that did not resolve is exactly what a junction or a
    /// substituted drive leaves behind. A path to a file that is not there still
    /// resolves, through the nearest folder that is, so a missing file is compared
    /// like any other.
    ///
    /// WHERE THE SPELLINGS DIFFER, THE FOLDER IS COMPARED BY IDENTITY. The Installer
    /// folder reached through this PC's own admin share,
    /// <c>\\localhost\C$\Windows\Installer</c> or the same with the computer's name,
    /// resolves to that UNC spelling and not to the root's, and it is the root all the
    /// same. So the folder the resolved path sits in and the root are both opened, and
    /// the same identity answers true and a different one false. A folder that is not
    /// there answers false, since the root is there. Any other failure to read either
    /// identity answers null.
    /// </summary>
    /// <param name="identities">
    /// Identifies the two folders. Null in production, which reads the real filesystem
    /// as the resolution above does.
    /// </param>
    internal static bool? NamesAFileDirectlyInInstallerFolder(
        string path, InstallerCacheRoot root, IFileIdentityReader? identities = null)
    {
        if (!root.Proven) return null;
        if (ResolveFinalPathOutcome(path, out var resolved) != PathResolution.Resolved) return null;
        if (ResolvesDirectlyInInstallerFolder(resolved, root)) return true;

        // The parent as ResolvesDirectlyInInstallerFolder takes it. A path with no
        // parent is a volume or a share root, which is no file in any folder.
        var folder = PathText.GetDirectoryName(
            resolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(folder)) return false;

        return OpensAsTheRoot(folder, root, identities);
    }

    /// <summary>
    /// Whether <paramref name="folder"/>, not yet resolved, is the Installer folder itself once
    /// the kernel has expanded it: true or false where the comparison was made, and null where it
    /// could not be. Asked on the terms <see cref="NamesAFileDirectlyInInstallerFolder"/> asks of
    /// the folder a path's file sits in: only a proven location is compared, the root's own
    /// spelling answers true without an identity being read, and any other folder is opened
    /// beside the root and compared by identity, a folder that is not there answering false. No
    /// file inside the folder is opened.
    /// </summary>
    /// <param name="identities">
    /// Identifies the two folders. Null in production, which reads the real filesystem as the
    /// resolution does.
    /// </param>
    internal static bool? IsTheInstallerFolder(
        string folder, InstallerCacheRoot root, IFileIdentityReader? identities = null)
    {
        if (!root.Proven) return null;
        if (ResolveFinalPathOutcome(folder, out var resolved) != PathResolution.Resolved) return null;

        // One trailing separator comes off, and none comes off a volume or share root: "C:\"
        // without it is "C:", which names the current folder on that drive rather than its root.
        var input = PathText.TrimEndingDirectorySeparator(resolved);
        if (input.Equals(root.Resolved, StringComparison.OrdinalIgnoreCase)) return true;

        return OpensAsTheRoot(input, root, identities);
    }

    /// <summary>
    /// Whether the folder at <paramref name="resolvedFolder"/>, already resolved, opens as the
    /// Installer folder: the same identity answers true and a different one false, a folder that
    /// is not there answers false, since the root is there, and any other failure to read either
    /// identity answers null.
    /// </summary>
    private static bool? OpensAsTheRoot(string resolvedFolder, InstallerCacheRoot root, IFileIdentityReader? identities)
    {
        identities ??= FolderIdentities;
        var read = identities.ReadOutcome(resolvedFolder, out var folderIdentity);
        if (read == FileIdentityRead.NamesNothing) return false;
        if (read != FileIdentityRead.Read) return null;

        return identities.ReadOutcome(root.Resolved, out var rootIdentity) == FileIdentityRead.Read
            ? folderIdentity == rootIdentity
            : null;
    }

    /// <summary>The reader <see cref="NamesAFileDirectlyInInstallerFolder"/> uses in production.</summary>
    private static readonly IFileIdentityReader FolderIdentities = new FileIdentityReader();

    /// <summary>
    /// True if <paramref name="path"/> resolves under any of the
    /// canonical Windows system folders: <c>%SystemRoot%</c>,
    /// <c>%ProgramFiles%</c>, <c>%ProgramFiles(x86)%</c>, or
    /// <c>%ProgramData%</c>. Symlinks, junctions and subst-mapped
    /// drives are expanded the same way as
    /// <see cref="IsInstallerFolderOrChild"/>. Move refuses a destination
    /// that resolves under one, anchored at the <c>MoveFilesService</c>
    /// boundary and repeated in the GUI's and CLI's own destination
    /// validation: those folders sit on documented DLL-search and
    /// SxS-resolution paths, and a CLI run writes to its saved
    /// destination without ever showing the user the resolved path.
    /// Per-user Documents/Desktop are deliberately not in this list:
    /// they're data folders, not system trust boundaries.
    /// </summary>
    internal static bool IsSystemFolderOrChild(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        var proven = TryResolveFinalPath(path, out var resolvedInput);
        return ResolvesInsideSystemFolder(resolvedInput, proven);
    }

    /// <summary>
    /// The containment comparison itself, over an ALREADY-resolved input, and the
    /// exact counterpart of <see cref="ResolvesInsideInstallerFolder"/> on the other
    /// forbidden set. Split out for the same reason that one was: a caller that has
    /// already resolved a path and is about to act on THAT value needs to ask the
    /// question of the value rather than of the path, or it is answering about a
    /// resolution it is not using.
    ///
    /// SHAPED TO MATCH ITS SIBLING RATHER THAN TO ITS OWN CONVENIENCE. Same
    /// already-resolved parameter, same trim of the input, same equality-or-prefix
    /// test, same treatment of a root that will not resolve, the same local path
    /// compared where the text does not match, and no root override on either:
    /// nothing needs to relocate these roots, and a parameter nobody passes is a seam
    /// nobody is testing.
    ///
    /// A DEGRADED INPUT ANSWERS "NOT INSIDE" WHERE THE TEXT DOES NOT MATCH, which is
    /// what the destination side wants and what the sibling does: a caller refusing
    /// on this answer would strand a user whose path merely could not be expanded.
    /// </summary>
    /// <param name="proven">
    /// Whether the kernel resolved <paramref name="resolvedInput"/>, as
    /// <see cref="TryResolveFinalPath"/> answers it.
    /// </param>
    /// <param name="localPathOf">
    /// Gives the local path of a folder reached through a share. Null in production
    /// (<see cref="LocalPathThroughThisPCsVolumes"/>), which asks the real machine; only a test
    /// passes one, through these two and never through the gates above them.
    /// </param>
    internal static bool ResolvesInsideSystemFolder(
        string resolvedInput, bool proven, LocalPathReader? localPathOf = null)
    {
        var systemRoots = new[]
        {
            KnownFolders.GetPath(Environment.SpecialFolder.Windows),
            KnownFolders.GetPath(Environment.SpecialFolder.ProgramFiles),
            KnownFolders.GetPath(Environment.SpecialFolder.ProgramFilesX86),
            KnownFolders.GetPath(Environment.SpecialFolder.CommonApplicationData),
        };

        var resolvedRoots = new List<string>(systemRoots.Length);
        foreach (var root in systemRoots)
        {
            if (string.IsNullOrEmpty(root)) continue;
            var resolvedRoot = ResolveFinalPath(root).TrimEnd(Path.DirectorySeparatorChar);
            if (IsAtOrUnder(resolvedInput, resolvedRoot)) return true;
            resolvedRoots.Add(resolvedRoot);
        }

        return LocalPathOf(resolvedInput, proven, resolvedRoots, localPathOf) is { } local
            && resolvedRoots.Any(root => IsAtOrUnder(local, root));
    }

    /// <summary>
    /// Expands symlinks, NTFS junctions and subst-mapped drives to the
    /// real on-disk path. Required so a destination check cannot be
    /// bypassed by picking a junction that points inside
    /// C:\Windows\Installer. Where the kernel32 resolution fails, answers the path made
    /// full, without a \\.\, \\?\ or \??\ in front of a drive or share, and the path as
    /// given where it cannot be made full.
    /// </summary>
    internal static string ResolveFinalPath(string path)
    {
        TryResolveFinalPath(path, out var resolved);
        return resolved;
    }

    /// <summary>
    /// <see cref="ResolveFinalPath"/> with its degradation made visible: false
    /// means the kernel never expanded this path and
    /// <paramref name="resolved"/> is the best-effort string instead of a proven
    /// one. Every caller gets the same string either way, so the bool is the
    /// only difference and no caller is forced to handle it.
    ///
    /// It matters because a degraded result is exactly a path whose reparse
    /// points went UNexpanded, which is the one thing a containment check is
    /// there to see through. A gate that cannot tell the two apart calls a
    /// junction in-bounds on the strength of how its name is spelled. See
    /// <see cref="ResolvesInsideInstallerFolder"/> for which side wants which.
    ///
    /// THE BOOL IS THE WHOLE OF WHAT ANY GATE ASKS, and this stays the entry
    /// every one of them takes. <see cref="ResolveFinalPathOutcome(string, out string)"/> is the same
    /// call with the failure named rather than collapsed, and the naming is read by
    /// the census and by the opt-in report; the offer is decided on the same
    /// not-<see cref="PathResolution.Resolved"/> question this bool asks. Routing
    /// the gates through the outcome and comparing members would put a containment
    /// decision one mistyped member away from treating a refusal as a proof.
    /// </summary>
    internal static bool TryResolveFinalPath(string path, out string resolved) =>
        ResolveFinalPathOutcome(path, out resolved) == PathResolution.Resolved;

    /// <summary>
    /// <see cref="TryResolveFinalPath"/> with its single <c>false</c> separated
    /// into the five distinct failures behind it. The bool form above is this call
    /// with the answer narrowed, so the two cannot drift apart and no caller has to
    /// be trusted to keep them in step.
    ///
    /// WHY THE FIVE ARE WORTH SEPARATING is <see cref="PathResolution"/>'s own
    /// note: two of them are ordinary machine states and three cannot be produced
    /// by absence or by a permission, so a count over all five together says
    /// nothing anybody could act on.
    ///
    /// IT DOES NOT THROW. A path <see cref="FullPaths.GetFullPath(string)"/> refuses
    /// answers <see cref="PathResolution.NotAPath"/>, the walk up to an existing
    /// ancestor asks <see cref="FrameworkFile.Exists(string)"/> and
    /// <see cref="FrameworkDirectory.Exists(string)"/>, which answer false rather than throw,
    /// and anything thrown from the open onwards answers
    /// <see cref="PathResolution.Faulted"/>. Keep it that way:
    /// <c>InstallerQueryService.NormaliseLocalPackagePath</c> counts a throw from this
    /// call as a refusal of its prefix step, which the opt-in report describes as a
    /// count no missing file, missing drive or permission produces.
    /// </summary>
    internal static PathResolution ResolveFinalPathOutcome(string path, out string resolved) =>
        ResolveFinalPathOutcome(path, out resolved, Win32FinalPathKernel.Instance);

    /// <summary>
    /// <see cref="ResolveFinalPathOutcome(string, out string)"/> with the handle open and
    /// the final-name read made through <paramref name="kernel"/>. The two-argument form
    /// is its only caller in the app, and passes
    /// <see cref="Win32FinalPathKernel.Instance"/>, which makes the <c>CreateFile</c> and
    /// <c>GetFinalPathNameByHandle</c> calls.
    /// </summary>
    internal static PathResolution ResolveFinalPathOutcome(
        string path, out string resolved, IFinalPathKernel kernel)
    {
        var outcome = ResolveCore(path, out resolved, kernel);

        // A failure hands back the path made full, and every gate compares it with the
        // cache folder and the system folders as text. GetFullPath leaves a \\?\ or \??\
        // path as given, so its prefix comes off here and nothing else in it changes: the
        // text compared is the text the Move goes on to use.
        if (outcome != PathResolution.Resolved && outcome != PathResolution.NotAPath)
            resolved = ComparedSpelling(resolved);

        return outcome;
    }

    private static PathResolution ResolveCore(string path, out string resolved, IFinalPathKernel kernel)
    {
        string normalised;
        try { normalised = FullPaths.GetFullPath(path); }
        catch { resolved = path; return PathResolution.NotAPath; }

        resolved = normalised;

        // GetFinalPathNameByHandle needs an existing target; walk up
        // until an ancestor exists and open that.
        var probe = normalised;
        while (probe.Length > 0 && !FrameworkDirectory.Exists(probe) && !FrameworkFile.Exists(probe))
        {
            var parent = PathText.GetDirectoryName(probe);
            if (string.IsNullOrEmpty(parent) || parent == probe)
                return PathResolution.NoExistingAncestor;
            probe = parent;
        }

        // Rented, not allocated: this runs once per candidate over a folder that
        // reaches 800,000 files, and a fresh 520-char array each time was the
        // single largest thing the containment guard put through gen0.
        var buffer = ArrayPool<char>.Shared.Rent(PathBufferLength);
        try
        {
            using var handle = kernel.Open(probe);

            if (handle.IsInvalid) return PathResolution.OpenRefused;

            var length = kernel.GetFinalName(handle, buffer, (uint)buffer.Length);
            if (length == 0) return PathResolution.FinalNameUnavailable;
            if (length >= buffer.Length)
            {
                // Buffer too small. The returned length includes the
                // null terminator in the required-size case, so ask for
                // exactly that many chars and retry. A rented array can come
                // back larger than the request, which is why the retry passes
                // the array's own length rather than the requested one.
                var larger = ArrayPool<char>.Shared.Rent((int)length);
                ArrayPool<char>.Shared.Return(buffer);
                buffer = larger;
                length = kernel.GetFinalName(handle, buffer, (uint)buffer.Length);
                if (length == 0) return PathResolution.FinalNameUnavailable;
            }

            var final = StripLongPathPrefix(new string(buffer, 0, (int)length));

            // Reattach the not-yet-created suffix to the resolved root.
            // PathText.Combine handles the separator boundary; probe = "C:\"
            // gives a suffix without a leading separator, others with.
            if (probe.Length < normalised.Length)
            {
                var suffix = normalised.Substring(probe.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                final = PathText.Combine(final, suffix);
            }

            resolved = final;
            return PathResolution.Resolved;
        }
        catch
        {
            resolved = normalised;
            return PathResolution.Faulted;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// <paramref name="fullPath"/> without the <c>\\.\</c>, <c>\\?\</c> or <c>\??\</c> in
    /// front of it, where a drive letter, a colon and a backslash follow it, or
    /// <c>UNC\</c> in any case, which becomes the <c>\\</c> of a share. Any other path
    /// comes back as given.
    /// </summary>
    private static string ComparedSpelling(string fullPath)
    {
        if (!fullPath.StartsWith(@"\\?\", StringComparison.Ordinal)
            && !fullPath.StartsWith(@"\\.\", StringComparison.Ordinal)
            && !fullPath.StartsWith(@"\??\", StringComparison.Ordinal))
            return fullPath;

        var rest = fullPath.AsSpan(4);
        if (rest.StartsWith(@"UNC\".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return @"\\" + fullPath.Substring(8);

        return rest.Length >= 3 && HasDriveRoot(rest) && rest[2] == '\\'
            ? fullPath.Substring(4)
            : fullPath;
    }

    /// <summary>
    /// Takes off the four-character prefix that puts an otherwise ordinary path
    /// outside the spelling the rest of the app compares against. Two forms, one
    /// rule: the <c>\\?\</c> long-path prefix the kernel adds, and the NT
    /// object-manager <c>\??\</c> that reaches this code from a registered
    /// <c>LocalPackage</c> value rather than from the kernel.
    /// <see cref="PendingRebootService"/> strips the same pair off the queued
    /// rename entries, for the same reason.
    ///
    /// A bare prefix comes off ONLY where a drive letter and colon follow, and
    /// the UNC form only where <c>UNC\</c> does. Those are the two remainders
    /// that are still rooted. A volume-GUID path keeps its prefix: without it,
    /// <c>\\?\Volume{...}\Windows\Installer\9f05cba.msi</c> has no root at all,
    /// <see cref="FullPaths.GetFullPath(string)"/> would resolve it against the process
    /// working directory, and the window and the command line would answer differently
    /// for one registration. Left whole, a volume-GUID path is still a path Win32
    /// accepts, so it names its file, answers true to
    /// <c>FrameworkFile.Exists</c>, and reads the same from either host. It does not match
    /// the folder walk's spelling, and no lexical rule can make it: a caller
    /// wanting the walk's spelling asks the filesystem, which is what
    /// <c>InstallerQueryService.NormaliseLocalPackagePath</c> does with the
    /// surviving prefix as its trigger.
    /// </summary>
    internal static string StripLongPathPrefix(string path)
    {
        const int prefixLength = 4;
        const string uncTail = @"UNC\";

        if (!path.StartsWith(@"\\?\", StringComparison.Ordinal)
            && !path.StartsWith(@"\??\", StringComparison.Ordinal))
            return path;

        var rest = path.AsSpan(prefixLength);
        if (rest.StartsWith(uncTail, StringComparison.Ordinal))
            return @"\\" + path.Substring(prefixLength + uncTail.Length);

        return HasDriveRoot(rest) ? path.Substring(prefixLength) : path;
    }

    /// <summary>
    /// True where the span opens with a drive letter and a colon. Deliberately
    /// narrower than <see cref="PathText.IsPathRooted(string)"/>, which counts a bare
    /// leading separator as rooted: <c>\Windows\Installer\9f05cba.msi</c> is
    /// rooted on whichever drive the process happens to be running from, and an
    /// answer that moves with the process is the one thing the caller above is
    /// there to refuse.
    /// </summary>
    private static bool HasDriveRoot(ReadOnlySpan<char> path) =>
        path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':';

    // 520 chars covers any practical Windows long path (260 standard +
    // headroom for the \\?\ prefix and the not-yet-created suffix the
    // caller may attach).
    private const int PathBufferLength = 520;
}

/// <summary>
/// Gives the local path of the folder a share path reaches, given the forbidden roots whose
/// volumes it may be on, or null where that is not established
/// (<see cref="InstallerCacheHelpers.LocalPathThroughThisPCsVolumes"/>).
/// </summary>
internal delegate string? LocalPathReader(string sharePath, IReadOnlyList<string> roots);

/// <summary>
/// The two kernel calls <see cref="InstallerCacheHelpers.ResolveFinalPathOutcome(string, out string, IFinalPathKernel)"/>
/// makes on the ancestor it has found: a handle open, and a read of the final name
/// through that handle.
/// </summary>
internal interface IFinalPathKernel
{
    /// <summary>
    /// A handle on <paramref name="path"/>, or an invalid one where the open was refused.
    /// </summary>
    SafeFileHandle Open(string path);

    /// <summary>
    /// The final path of <paramref name="handle"/>, drive-letter form, written into
    /// <paramref name="buffer"/>. Answers as <c>GetFinalPathNameByHandle</c> does: the
    /// length written, the length needed where that is <paramref name="length"/> or
    /// more, and zero where the call fails.
    /// </summary>
    uint GetFinalName(SafeFileHandle handle, char[] buffer, uint length);
}

/// <summary>
/// <see cref="IFinalPathKernel"/> over Win32. The open is the metadata open
/// (<see cref="StorageHelpers.OpenForMetadata"/>), so a directory opens as well as a file;
/// the name is read in the <c>VOLUME_NAME_DOS</c> form.
/// </summary>
internal sealed class Win32FinalPathKernel : IFinalPathKernel
{
    internal static readonly Win32FinalPathKernel Instance = new();

    private Win32FinalPathKernel() { }

    public SafeFileHandle Open(string path) => StorageHelpers.OpenForMetadata(path, out _);

    public uint GetFinalName(SafeFileHandle handle, char[] buffer, uint length) =>
        Kernel32.GetFinalPathNameByHandle(handle, buffer, length, Kernel32.VOLUME_NAME_DOS);
}

/// <summary>
/// <see cref="IFinalPathKernel"/> that opens a folder as this PC's own volume holds it, for the
/// Move destination gates. The folder is opened as the path names it, through a share where it
/// names one, and its 128-bit file id and volume serial number are read off that handle
/// (<c>FILE_ID_INFO</c>). Then, for each root in turn whose own volume carries that serial
/// number, the id is opened on that volume with <c>OpenFileById</c>, the root's handle naming
/// the volume. The final name of that handle is the folder's local path, read in the
/// <c>VOLUME_NAME_DOS</c> form.
///
/// A FOLDER ON NO ROOT'S VOLUME DOES NOT OPEN HERE, and nor does one whose id does not read
/// (<see cref="DirectoryGuard.IdOf"/>) or does not open on the volume carrying its serial
/// number. A share on another computer is such a folder, its serial number being its own
/// volume's. Each answers an invalid handle, which the resolution reads as a refused open.
///
/// THE OPENS ASK FOR NO ACCESS AND SHARE EVERYTHING, as <see cref="StorageHelpers.OpenForMetadata"/>
/// does, with <c>FILE_FLAG_BACKUP_SEMANTICS</c> so a folder opens.
/// </summary>
internal sealed class ThroughThisPCsVolumesKernel(IReadOnlyList<string> roots) : IFinalPathKernel
{
    public SafeFileHandle Open(string path)
    {
        FileIdentity reached;
        using (var throughTheShare = StorageHelpers.OpenForMetadata(path, out _))
        {
            if (throughTheShare.IsInvalid || DirectoryGuard.IdOf(throughTheShare) is not { } id) return Unopened();
            reached = id;
        }

        // The roots are usually all on one volume, so the first whose serial number matches
        // is the volume asked; a second root on it would answer the same.
        foreach (var root in roots)
        {
            using var onTheVolume = StorageHelpers.OpenForMetadata(root, out _);
            if (onTheVolume.IsInvalid
                || DirectoryGuard.IdOf(onTheVolume) is not { } rootIdentity
                || rootIdentity.VolumeSerialNumber != reached.VolumeSerialNumber)
                continue;

            var descriptor = new Kernel32.FILE_ID_DESCRIPTOR
            {
                dwSize = (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<Kernel32.FILE_ID_DESCRIPTOR>(),
                Type = Kernel32.ExtendedFileIdType,
                FileIdLow = reached.FileIdLow,
                FileIdHigh = reached.FileIdHigh,
            };
            return Kernel32.OpenFileById(
                onTheVolume, ref descriptor, 0, Kernel32.FILE_SHARE_ALL, IntPtr.Zero,
                Kernel32.FILE_FLAG_BACKUP_SEMANTICS);
        }

        return Unopened();
    }

    public uint GetFinalName(SafeFileHandle handle, char[] buffer, uint length) =>
        Kernel32.GetFinalPathNameByHandle(handle, buffer, length, Kernel32.VOLUME_NAME_DOS);

    private static SafeFileHandle Unopened() => new(new IntPtr(-1), ownsHandle: true);
}
