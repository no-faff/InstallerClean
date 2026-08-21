using System.Buffers;
using System.IO.Abstractions;
using System.Security;
using InstallerClean.Interop.Native;

namespace InstallerClean.Services;

/// <summary>
/// What <see cref="InstallerCacheHelpers.ResolveFinalPathOutcome"/> established,
/// with the five ways it can fail kept apart rather than collapsed into one
/// <c>false</c>.
///
/// ALL FIVE WITHHOLD, ON THE ONE THING TRUE OF EVERY MEMBER: the resolver was asked
/// about a recorded path and did not answer. A registration whose spelling the
/// filesystem would not settle is compared in a form the folder walk never produces,
/// so the cached file it names sits in the candidate list unclaimed, and which
/// candidate it is cannot be established. That is the whole argument and it does not
/// distinguish between the members.
///
/// THIS OVERTURNS A DECISION TAKEN TWICE, AND THE REASONING IT OVERTURNS WAS SOUND
/// ON ITS OWN TERMS. Acting on <see cref="NoExistingAncestor"/> was designed twice
/// and withdrawn twice, the second time by the session that proposed it, because it
/// would empty a machine's whole offer because a USB drive was unplugged. That was a
/// trade-off between an offer and a certainty, and the owner has since ruled the
/// trade-off away rather than resolved it: where the app can detect that one of its
/// own checks did not answer, it offers nothing that scan. How often a condition
/// arises is not admissible in that decision, so the two ordinary machine states
/// here, an unattached drive and a refused handle, withhold exactly as the other
/// three do. The old note split the five on that distinction and the split is gone,
/// because nothing reads it.
///
/// A REGISTRATION WHOSE FILE IS SIMPLY GONE REACHES NONE OF THESE, which is the
/// first objection anybody raises and is what keeps the rule off ordinary machines.
/// The walk climbs to an existing ancestor and reattaches the missing suffix as text,
/// so a missing file resolves normally and answers <see cref="Resolved"/>.
///
/// NOTHING IN THE APPLICATION BRANCHES ON WHICH MEMBER IT IS, and that is still true
/// of the gates: every containment gate asks the bool question. What changed is that
/// the counts are no longer carried for measurement alone. They travel to the opt-in
/// report AND they decide the offer, through
/// <c>EnumerationCensus.AnyRecordedPathUnestablished</c>, which is the single place
/// the question is asked.
/// </summary>
internal enum PathResolution
{
    /// <summary>
    /// The kernel expanded the path, so the out value is a proven location rather
    /// than a spelling. The only member any gate treats as success.
    /// </summary>
    Resolved,

    /// <summary>
    /// <see cref="Path.GetFullPath(string)"/> refused the string outright: an
    /// embedded null, a device name, or a length past the API's limit. The value
    /// names nothing that can be looked for and cannot be improved.
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
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");

    /// <summary>
    /// True if <paramref name="path"/> resolves to <c>C:\Windows\Installer</c>
    /// or any descendant after symlinks/junctions/subst-mapped drives are
    /// expanded. Used as the bottom-line safety check before any move:
    /// the entire restore-after-mistakes story collapses if files end up
    /// back inside the Installer folder.
    /// </summary>
    /// <param name="installerFolderRoot">
    /// Test-only real-folder override for the cache root (null in production,
    /// which uses the real <see cref="InstallerFolder"/>). The comparison still
    /// runs against the REAL filesystem via <see cref="ResolveFinalPath"/>, so
    /// this only relocates the check to a real sandbox directory for the
    /// integration tests; it does NOT let a MockFileSystem bypass the gate. It
    /// mirrors <c>FileSystemScanService</c>'s own installer-folder override.
    /// </param>
    internal static bool IsInstallerFolderOrChild(string path, string? installerFolderRoot = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        TryResolveFinalPath(path, out var resolvedInput);
        return ResolvesInsideInstallerFolder(resolvedInput, installerFolderRoot);
    }

    /// <summary>
    /// The containment comparison itself, over an ALREADY-resolved input. This
    /// is the DESTINATION side's question ("is this forbidden"), so a
    /// descendant counts; the source side asks a narrower one and takes
    /// <see cref="ResolvesDirectlyInInstallerFolder"/> instead. Both are paired
    /// with <see cref="TryResolveFinalPath"/> by their callers, so that a
    /// source gate can refuse a path whose resolution degraded; the destination
    /// gate above ignores that distinction, and the two sides want opposite
    /// answers for it. An unresolvable answer of "not forbidden" lets a move
    /// proceed, where refusing would strand a user with no destination they
    /// could use. The source gate asks "is this in bounds", and the whole
    /// reason it exists is that a corrupt LocalPackage value can name a file
    /// anywhere on disk, so "could not prove it is in bounds" and "out of
    /// bounds" earn the same refusal.
    ///
    /// The cache root is compared in its best-effort form even when ITS
    /// resolution degraded. A fully resolved input is a real canonical path, so
    /// a root left unexpanded cannot match it and the comparison answers "not
    /// inside", which this gate reads as permission. The strict form below
    /// compares the root the same way, where the same answer becomes the source
    /// side's refusal.
    /// </summary>
    internal static bool ResolvesInsideInstallerFolder(string resolvedInput, string? installerFolderRoot = null)
    {
        var input = resolvedInput
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var installer = ResolveFinalPath(installerFolderRoot ?? InstallerFolder)
            .TrimEnd(Path.DirectorySeparatorChar);

        return input.Equals(installer, StringComparison.OrdinalIgnoreCase)
            || input.StartsWith(installer + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
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

        var parent = Path.GetDirectoryName(input);
        return parent is not null
            && parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Equals(root.Resolved, StringComparison.OrdinalIgnoreCase);
    }

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

        var resolvedInput = ResolveFinalPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var systemRoots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        };

        foreach (var root in systemRoots)
        {
            if (string.IsNullOrEmpty(root)) continue;
            var resolvedRoot = ResolveFinalPath(root).TrimEnd(Path.DirectorySeparatorChar);
            if (resolvedInput.Equals(resolvedRoot, StringComparison.OrdinalIgnoreCase))
                return true;
            if (resolvedInput.StartsWith(resolvedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Expands symlinks, NTFS junctions and subst-mapped drives to the
    /// real on-disk path. Required so a destination check cannot be
    /// bypassed by picking a junction that points inside
    /// C:\Windows\Installer. Falls back to Path.GetFullPath if the
    /// kernel32 resolution call fails.
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
    /// every one of them takes. <see cref="ResolveFinalPathOutcome"/> is the same
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
    /// into the five distinct failures behind it. The resolution itself is here;
    /// the bool form above is this call with the answer narrowed, so the two
    /// cannot drift apart and no caller has to be trusted to keep them in step.
    ///
    /// WHY THE FIVE ARE WORTH SEPARATING is <see cref="PathResolution"/>'s own
    /// note: two of them are ordinary machine states and three cannot be produced
    /// by absence or by a permission, so a count over all five together says
    /// nothing anybody could act on.
    /// </summary>
    internal static PathResolution ResolveFinalPathOutcome(string path, out string resolved)
    {
        string normalised;
        try { normalised = Path.GetFullPath(path); }
        catch { resolved = path; return PathResolution.NotAPath; }

        resolved = normalised;

        // GetFinalPathNameByHandle needs an existing target; walk up
        // until an ancestor exists and open that.
        var probe = normalised;
        while (probe.Length > 0 && !Directory.Exists(probe) && !File.Exists(probe))
        {
            var parent = Path.GetDirectoryName(probe);
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
            using var handle = Kernel32.CreateFile(
                probe,
                0,
                Kernel32.FILE_SHARE_ALL,
                IntPtr.Zero,
                Kernel32.OPEN_EXISTING,
                Kernel32.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (handle.IsInvalid) return PathResolution.OpenRefused;

            var length = Kernel32.GetFinalPathNameByHandle(
                handle, buffer, (uint)buffer.Length, Kernel32.VOLUME_NAME_DOS);
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
                length = Kernel32.GetFinalPathNameByHandle(
                    handle, buffer, (uint)buffer.Length, Kernel32.VOLUME_NAME_DOS);
                if (length == 0) return PathResolution.FinalNameUnavailable;
            }

            var final = StripLongPathPrefix(new string(buffer, 0, (int)length));

            // Reattach the not-yet-created suffix to the resolved root.
            // Path.Combine handles the separator boundary; probe = "C:\"
            // gives a suffix without a leading separator, others with.
            if (probe.Length < normalised.Length)
            {
                var suffix = normalised.Substring(probe.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                final = Path.Combine(final, suffix);
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
    /// that are still rooted. Taking it off blind turned
    /// <c>\\?\Volume{...}\Windows\Installer\9f05cba.msi</c> into a path with no
    /// root at all, which <see cref="Path.GetFullPath(string)"/> then resolved
    /// against the process working directory: one registration, and the GUI and
    /// the command line answered differently for it. Left whole, a volume-GUID
    /// path is still a path Win32 accepts, so it names its file, answers true to
    /// <c>File.Exists</c>, and reads the same from either host. It does not match
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
    /// narrower than <see cref="Path.IsPathRooted(string)"/>, which counts a bare
    /// leading separator as rooted: <c>\Windows\Installer\9f05cba.msi</c> is
    /// rooted on whichever drive the process happens to be running from, and an
    /// answer that moves with the process is the one thing the caller above is
    /// there to refuse.
    /// </summary>
    private static bool HasDriveRoot(ReadOnlySpan<char> path) =>
        path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':';

    /// <summary>
    /// Deletes empty subdirectories inside C:\Windows\Installer. Sorted
    /// by descending path length, which puts every directory after its
    /// own descendants (a child's path is strictly longer than its
    /// parent's), so a nested empty tree collapses in one pass.
    /// Cancellable because a deeply nested Installer tree can take
    /// several seconds to walk.
    /// </summary>
    /// <remarks>
    /// Goes through the caller's <paramref name="fileSystem"/> rather than
    /// System.IO, unlike the two gates it runs after, which reach the real
    /// filesystem so that a MockFileSystem cannot defeat them:
    /// <see cref="CandidateGuard.CheckSafeToRemove"/> on every source file (a
    /// three-state <see cref="Helpers.StorageHelpers.ReparseCheck"/> plus a
    /// resolved comparison against the run's <see cref="InstallerCacheRoot"/>)
    /// and <see cref="IsInstallerFolderOrChild"/> on a Move's destination. This
    /// is cleanup, running once both have passed. Injected, a test's
    /// MockFileSystem has no C:\Windows\Installer, so the prune returns at the
    /// Exists check instead of walking the host's real cache folder.
    /// The folder is NOT a parameter, so no caller can aim the prune anywhere
    /// else.
    /// </remarks>
    internal static void PruneEmptySubdirectories(
        IFileSystem fileSystem,
        CancellationToken cancellationToken = default)
    {
        if (!fileSystem.Directory.Exists(InstallerFolder)) return;

        // Match FileSystemScanService: skip reparse points so a junction
        // planted inside the Installer folder cannot redirect the prune
        // pass to delete empty directories outside the cache.
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = true,
        };

        // Best-effort: the prune runs after a move or delete has already
        // committed, so a failure here must not undo that success.
        try
        {
            foreach (var dir in fileSystem.Directory.EnumerateDirectories(InstallerFolder, "*", options)
                .OrderByDescending(d => d.Length))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (!fileSystem.Directory.EnumerateFileSystemEntries(dir).Any())
                        fileSystem.Directory.Delete(dir);
                }
                catch (IOException) { /* directory not empty by the time Delete fires, or filesystem busy */ }
                catch (UnauthorizedAccessException) { /* DACL refuses the elevated process; rare but possible */ }
                catch (SecurityException) { /* permission denied at a higher tier */ }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failure enumerating or sorting the tree (the folder vanishing
            // mid-walk, or a share violation surfaced by the enumerator itself
            // rather than by a per-directory Delete) is swallowed: it would
            // otherwise propagate into a caller's generic catch and flip an
            // already-committed delete or move to a hard error. Cancellation is
            // excluded so the caller still sees a requested stop.
        }
    }

    // 520 chars covers any practical Windows long path (260 standard +
    // headroom for the \\?\ prefix and the not-yet-created suffix the
    // caller may attach).
    private const int PathBufferLength = 520;
}
