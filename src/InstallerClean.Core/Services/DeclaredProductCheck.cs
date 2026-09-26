using System.Globalization;
using System.IO.Abstractions;
using InstallerClean.Interop;
using InstallerClean.Models;
using Microsoft.Win32;

namespace InstallerClean.Services;

/// <summary>
/// Production <see cref="IDeclaredProductCheck"/>. For each candidate installation
/// package it reads the package's own ProductCode through
/// <see cref="IPackageIdentityReader"/>, puts it to Windows through the same keyed
/// enumeration the patch-target route uses, and for an installed product reads the
/// <c>LocalPackage</c> each installation records, the package each one's source list
/// points at and the package in the folder each one records as its
/// <c>InstallSource</c>. Every answer about a product is held against the installations
/// the caller's own enumeration listed, and one leaving out any of them keeps the file.
/// For each candidate patch it reads the patch's own code and the products its Template
/// names, finds the registrations of that patch through the machine-wide patch
/// enumeration and the keyed patch read, and reads the <c>LocalPackage</c> each
/// registration records and the patch package the patch's source list points at in
/// each registration's account and context. A source list in a per-user-unmanaged
/// context is not read, and an installation or registration in one keeps the file.
/// Every source list it does read is read twice, through the API and from the registry
/// key that holds it, and a list the two do not agree on keeps the file. Every
/// <c>InstallSource</c> it reads is read the same two ways, and one the two do not
/// agree on keeps the file too.
///
/// IT COMPOSES THINGS THAT ALREADY EXIST. The reading of each file, package or
/// patch, is the reader's. The asking is
/// <see cref="InstallerQueryService.ResolveProductInstances"/>,
/// <see cref="InstallerQueryService.EnumeratePatchHoldersAcrossAllProducts"/>,
/// <see cref="InstallerQueryService.ReadProductProperty"/>,
/// <see cref="InstallerQueryService.GetPatchProperty"/> and
/// <see cref="InstallerQueryService.ReadSourceListProperty"/>, shared rather than copied
/// because the part of them that decides anything is which returns are allowed to
/// mean "not installed", "not registered", "the end of the list" or "no value":
/// those allowlists are the difference between a file kept and a file offered, and
/// a second copy of them is a second thing to keep right. The comparison of recorded
/// packages against the candidate is <see cref="IFileIdentityReader"/>, the reader
/// the scan's own path comparison uses.
/// </summary>
public sealed class DeclaredProductCheck : IDeclaredProductCheck
{
    private readonly IMsiApi _msi;
    private readonly IPackageIdentityReader _identityReader;
    private readonly IFileIdentityReader? _fileIdentities;
    private readonly IFileSystem? _fileSystem;
    private readonly IRegistryReader? _registry;

    /// <param name="fileIdentities">
    /// Identifies the file each recorded package path opens, and the candidate's own.
    /// </param>
    /// <param name="fileSystem">
    /// Answers whether a recorded package path names a file, so that a value naming a
    /// folder is not taken for a package.
    /// </param>
    /// <param name="registry">
    /// Reads the registry key each source list is held in, which the list the API
    /// returns is checked against.
    /// </param>
    /// <remarks>
    /// WITHOUT BOTH FILE READERS NO RECORDED PACKAGE IS LOOKED AT, and every candidate
    /// whose declared product is installed is kept as
    /// <see cref="DeclaredProductOutcome.DeclaredProductInstalled"/>, and every
    /// candidate whose declared patch is registered as
    /// <see cref="DeclaredProductOutcome.DeclaredPatchRegistered"/>. WITHOUT THE
    /// REGISTRY READER NO SOURCE LIST IS RELIED ON, and every candidate the comparison
    /// reaches a source list for is kept the same way. That is the direction a missing
    /// dependency has to fail in. The composition root supplies all three.
    /// </remarks>
    public DeclaredProductCheck(
        IMsiApi msi,
        IPackageIdentityReader identityReader,
        IFileIdentityReader? fileIdentities = null,
        IFileSystem? fileSystem = null,
        IRegistryReader? registry = null)
    {
        _msi = msi;
        _identityReader = identityReader;
        _fileIdentities = fileIdentities;
        _fileSystem = fileSystem;
        _registry = registry;
    }

    /// <summary>Whether this check compares recorded packages with the candidate.</summary>
    internal bool ComparesRecordedPackages => _fileIdentities is not null && _fileSystem is not null;

    /// <summary>Whether this check reads the registry key each source list is held in.</summary>
    internal bool ReadsSourceListKeys => _registry is not null;

    /// <inheritdoc />
    public IReadOnlyList<DeclaredProductOutcome> Screen(
        IReadOnlyList<OrphanedFile> candidates,
        IReadOnlyList<ListedInstallation> installations,
        CancellationToken cancellationToken = default,
        Action<Exception, string>? recordRefusal = null,
        Func<string, bool?>? namesAFileInInstallerFolder = null)
    {
        var outcomes = new DeclaredProductOutcome[candidates.Count];

        // Per pass, so it cannot outlive the machine state it describes. A folder
        // holding six cached packages of one program declares one product code
        // six times, and the answer to a keyed enumeration does not change inside
        // a scan.
        //
        // ORDINAL, because the reader canonicalises every code it returns to the
        // braced upper-case form for exactly this: two readings of one code are
        // then the same string, and a comparer that folded case would be covering
        // for a reader that had stopped doing that.
        var asked = new Dictionary<string, DeclarationAnswer>(StringComparer.Ordinal);

        // What the pass has asked about installations and patch registrations, shared
        // by both halves, for the same reason and with the same lifetime, and the
        // installations the caller's enumeration listed, which every answer about a
        // product is held against.
        var pass = new PassAnswers(_msi, installations, cancellationToken);

        for (var i = 0; i < candidates.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = candidates[i];

            // A PATCH DECLARES A PATCH CODE AND NOT A PRODUCT CODE, so it is screened
            // against the registrations of that patch rather than against the
            // installations of a product.
            if (candidate.IsPatch)
            {
                outcomes[i] = ScreenPatch(candidate.FullPath, pass, recordRefusal, namesAFileInInstallerFolder);
                continue;
            }

            var identity = _identityReader.Read(candidate.FullPath, isPatch: false, out var detail);

            // THREE REFUSALS UNDER ONE ARM, and each of them is the file failing
            // to give this pass something to ask about. Null is the reader's own
            // "nothing here to ask", documented as covering everything from a
            // file that would not open to a code that is not a GUID. An empty
            // code is the same outcome reached without a null, which the seam's
            // do-nothing implementations produce. A reading that came back
            // marked as a patch is a reader that did not answer the question
            // asked, and a code of the wrong kind put to a keyed product
            // enumeration would be answered about nothing.
            if (identity is null
                || identity.Value.Code.Length == 0
                || identity.Value.IsPatch)
            {
                // ONLY THE NULL ARM HAS A DETAIL TO KEEP. The other two are answers
                // the reader gave rather than failures it had, so it wrote nothing down
                // about them and a note saying the file did not yield a code would be
                // untrue of one that did. What the detail is for, and why no path goes
                // with it, is at the sibling site in InstallerQueryService.
                if (identity is null)
                    recordRefusal?.Invoke(
                        new InvalidOperationException(
                            "A cached package did not yield the product code it declares, so it is "
                            + "kept rather than offered. Reader detail: "
                            + (detail.Length == 0 ? "none given" : detail) + "."),
                        detail);

                outcomes[i] = DeclaredProductOutcome.Unestablished;
                continue;
            }

            var code = identity.Value.Code;
            if (!asked.TryGetValue(code, out var answer))
            {
                answer = Ask(code, pass, namesAFileInInstallerFolder);
                asked[code] = answer;
            }

            // The product-level answer is shared by every candidate declaring the
            // code; whether the recorded packages are OTHER files is a question about
            // this candidate, so it is asked per file.
            outcomes[i] = answer.Outcome == DeclaredProductOutcome.DeclaredProductInstalled
                && answer.RecordedPackages is { } recorded
                && IsNoneOf(candidate.FullPath, recorded)
                    ? DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile
                    : answer.Outcome;
        }

        return outcomes;
    }

    /// <summary>
    /// What Windows holds for one declared product code, asked once per code per
    /// pass.
    /// </summary>
    private DeclarationAnswer Ask(
        string code, PassAnswers pass, Func<string, bool?>? namesAFileInInstallerFolder)
    {
        var resolved = pass.InstancesOf(code);

        // THE ORDER OF THE ARMS IS THE WHOLE OF IT, and the unaskable one is
        // first because it is the one that reads as an answer if it is left
        // last. A call that could not be made has not shown the product to be
        // absent, and neither has an answer leaving out an installation the caller's
        // enumeration listed, whether "not installed" or a list short of it, which
        // comes back unaskable too (PassAnswers.InstancesOf). Either keeps the file.
        if (resolved.Unaskable)
            return new DeclarationAnswer(DeclaredProductOutcome.Unestablished, null);

        if (resolved.Instances.Count == 0)
            return new DeclarationAnswer(DeclaredProductOutcome.DeclaredProductNotInstalled, null);

        return new DeclarationAnswer(
            DeclaredProductOutcome.DeclaredProductInstalled,
            PackagesOpenedBy(code, resolved.Instances, namesAFileInInstallerFolder));
    }

    /// <summary>
    /// The identity of every file an installation of <paramref name="code"/> opens as
    /// its package: the cached package each installation records, and the original
    /// package at each folder on its source list that holds one. Null where any of
    /// them cannot be seen.
    ///
    /// NULL IS THE ANSWER THAT KEEPS THE FILE, and every way an installation's package
    /// can fail to be seen reaches it: a <c>LocalPackage</c> read that failed or came
    /// back empty, a value that names nothing, names a folder, will not open to an
    /// identity, or names a file that does not declare <paramref name="code"/>; and
    /// any source the check cannot rule out, which <see cref="AddSourcePackages"/>
    /// sets out. One such installation is enough, because its package is the one this
    /// candidate could be.
    /// </summary>
    private IReadOnlyList<FileIdentity>? PackagesOpenedBy(
        string code,
        IReadOnlyList<(string? Sid, MsiInstallContext Context)> instances,
        Func<string, bool?>? namesAFileInInstallerFolder)
    {
        if (_fileIdentities is null || _fileSystem is null) return null;

        var identities = new List<FileIdentity>(instances.Count);
        foreach (var (sid, context) in instances)
        {
            var read = InstallerQueryService.ReadProductProperty(
                _msi, code, sid, context, MsiInstallProperty.LocalPackage);
            if (read.Unreadable) return null;

            var path = read.Value.TrimEnd('\0');
            if (path.Length == 0) return null;

            // File.Exists is false for a folder and for a path that will not parse,
            // and the identity read below opens folders too, so this is what keeps a
            // value naming a folder from standing in for a package.
            if (!_fileSystem.File.Exists(path)) return null;

            if (_fileIdentities.ReadOutcome(path, out var recorded) != FileIdentityRead.Read)
                return null;

            // THE RECORDED PACKAGE HAS TO DECLARE THE SAME PRODUCT. The Windows Installer
            // record says which file the installation uses and this reads the file
            // itself, so the verdict rests on the two agreeing: a value naming a file that
            // is not this product's package shows nothing about where the package is, and
            // keeps the candidate.
            var declared = _identityReader.Read(path, isPatch: false, out _);
            if (declared is null
                || declared.Value.IsPatch
                || !string.Equals(declared.Value.Code, code, StringComparison.Ordinal))
                return null;

            identities.Add(recorded);

            if (!AddSourcePackages(code, isPatch: false, sid, context, namesAFileInInstallerFolder, identities))
                return null;
        }

        return identities;
    }

    /// <summary>
    /// Adds to <paramref name="opened"/> the identity of the original package at each
    /// folder on one network source list, a product's for one installation or a
    /// patch's in one account and context, and answers false where those sources
    /// cannot be ruled out.
    ///
    /// WHAT IT COMPARES, AND WHAT KEEPS THE COPY INSTEAD. When Windows Installer needs a
    /// product's original package rather than its cached copy, a repair among other
    /// things, it tries the source it used last and then the sources on the product's
    /// source list, network folders, media and URLs, looking in each for the file named
    /// by <c>PackageName</c>. A patch has a source list and a package name of its own,
    /// and this reads them as it reads a product's. The folders compared are the network
    /// sources and, for a product, the folder the installation records as its
    /// <c>InstallSource</c>, the one its package was installed from. Windows Installer
    /// puts that folder on the list when it installs the product, and the list can
    /// change afterwards, so the folder is compared whether or not the list still holds
    /// it (<see cref="InstallSourceOf"/>). Everything else on the list is read to decide
    /// whether the copy is kept, and it is kept for any of these:
    /// - A URL ENTRY, WHATEVER ITS SCHEME. A web address can name this PC as well as any
    ///   other. No URL is compared, and one on the list keeps the copy.
    /// - AN ENTRY NAMING AN ENVIRONMENT VARIABLE, one holding a '%'. Whoever reads the
    ///   entry may expand the variable in its own environment, so the folder Windows
    ///   Installer looks in need not be the text compared.
    /// - A LIST THE REGISTRY HOLDS DIFFERENTLY. The list the API returns is compared
    ///   with the key it is held in (<see cref="SourceListKeyPath"/>), entry by entry
    ///   (<see cref="IsTheList"/>), so an entry the API does not return, past a gap in
    ///   the numbering or anywhere else, keeps the copy. So does a package name the key
    ///   holds otherwise than the API answers it, or holds as a REG_EXPAND_SZ
    ///   (<see cref="HoldsThePackageName"/>).
    /// - A SOURCE USED LAST THAT IS NOT A NETWORK ENTRY ON THE LIST, it being the one
    ///   Windows Installer tries first, or that the registry holds differently
    ///   (<see cref="SourceUsedLastIsOnTheList"/>).
    /// - A MEDIA PACKAGE PATH, the package's path on the installation media
    ///   (<see cref="NamesNoMediaPackagePath"/>). No media source is compared, and a list
    ///   naming a path on one keeps the copy.
    ///
    /// FALSE, WHICH KEEPS THE FILE, for: no way to compare against the Installer folder
    /// or to read the registry; a per-user-unmanaged account and context, whose list is
    /// not read; a package name, a source list or a property of the list that will not
    /// read; an empty package name, or one holding a '\', a '/', a ':' or a '%'; each
    /// of the five above; a source entry holding a null; a product's
    /// <c>InstallSource</c> that <see cref="InstallSourceOf"/> answers null for; a source
    /// whose package would be a file directly in the Installer folder, or where that
    /// cannot be established; and a source package that exists and will not identify. A
    /// source package that is not there is skipped, being no file.
    /// </summary>
    /// <param name="isPatch">
    /// Whether <paramref name="code"/> is a patch code rather than a product code. The
    /// source-list calls are told which.
    /// </param>
    private bool AddSourcePackages(
        string code,
        bool isPatch,
        string? sid,
        MsiInstallContext context,
        Func<string, bool?>? namesAFileInInstallerFolder,
        List<FileIdentity> opened)
    {
        if (namesAFileInInstallerFolder is null || _fileIdentities is null || _registry is null) return false;

        // A PER-USER-UNMANAGED SOURCE LIST IS NOT READ, IN ANY ACCOUNT, AND THE COPY IS
        // KEPT. Microsoft documents that an administrator cannot enumerate another
        // user's per-user-unmanaged installations, and not what the call answers in their
        // place, so a list read there with no network source on it cannot be told from a
        // list that was never read. So no answer from such a list is used, and the copy
        // is kept whatever the list would say. The account this process runs as is not
        // compared with the registration's, so its own per-user-unmanaged installations
        // are kept the same way.
        if (context == MsiInstallContext.UserUnmanaged) return false;

        // A patch's package name is read off its source list, MsiGetPatchInfoEx not
        // taking the property.
        var name = isPatch
            ? InstallerQueryService.ReadSourceListProperty(
                _msi, code, sid, context, MsiSourceListOptions.Patch, MsiInstallProperty.PackageName)
            : InstallerQueryService.ReadProductProperty(
                _msi, code, sid, context, MsiInstallProperty.PackageName);
        if (name.Unreadable) return false;

        var packageName = name.Value.TrimEnd('\0');
        if (packageName.Length == 0) return false;

        // A package name holding a '\', a '/' or a ':' names a folder or a drive as well
        // as a file. Such a name keeps the copy, since with no media package path a media
        // source's package is that name below the root of its volume, and no media source
        // is compared. A package name holding a '%' names a variable, which whoever reads
        // the name may expand in its own environment, and keeps the copy as well.
        if (packageName.IndexOfAny(['\\', '/', ':', '%']) >= 0) return false;

        var sources = SourcesOf(code, isPatch, sid, context, MsiSourceListOptions.Network);
        if (sources is null) return false;

        var urls = SourcesOf(code, isPatch, sid, context, MsiSourceListOptions.Url);
        if (urls is null || urls.Count > 0) return false;

        // An entry naming an environment variable keeps the copy, and so does one holding
        // a null, which would cut the entry short wherever it is read as a path.
        foreach (var entry in sources)
            if (entry.Contains('%') || entry.Contains('\0')) return false;

        var path = SourceListKeyPath(code, isPatch, sid, context);
        if (path is null) return false;

        var sourceList = _registry.LocalMachineValues(path);
        if (sourceList.Presence != RegistryKeyPresence.Present || sourceList.Values is null) return false;

        // The name the key holds has to be the one the API answered, text for text, so
        // a '\', a '/', a ':' or a '%' keeps the copy whichever of the two holds it.
        // Loosen that comparison and the stored name needs the character test of its own.
        if (!HoldsThePackageName(sourceList.Values, packageName)) return false;

        if (!IsTheList(_registry.LocalMachineValues(path + @"\Net"), sources)
            || !IsTheList(_registry.LocalMachineValues(path + @"\URL"), urls)
            || !NamesNoMediaPackagePath(code, isPatch, sid, context, _registry.LocalMachineValues(path + @"\Media"))
            || !SourceUsedLastIsOnTheList(code, isPatch, sid, context, sources, sourceList.Values))
            return false;

        // A product's InstallSource joins the folders compared, unless it is already one
        // of the network entries. It is read for a product only.
        var folders = sources;
        if (!isPatch)
        {
            var installSource = InstallSourceOf(_registry, code, sid, context);
            if (installSource is null) return false;
            if (installSource.Length > 0 && !sources.Contains(installSource, StringComparer.Ordinal))
                folders = [.. sources, installSource];
        }

        foreach (var folder in folders)
        {
            // Joined with a backslash by hand rather than with Path.Combine, whose
            // separator is the host's.
            var package = folder.EndsWith('\\') ? folder + packageName : folder + '\\' + packageName;

            // A source in the Installer folder keeps every copy of the product or the
            // patch, not only the one it names: the folder it was installed or applied
            // from is the cache itself.
            if (namesAFileInInstallerFolder(package) is not false) return false;

            switch (_fileIdentities.ReadOutcome(package, out var identity))
            {
                case FileIdentityRead.Read:
                    opened.Add(identity);
                    break;
                case FileIdentityRead.NamesNothing:
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The entries on one source list, network or URL as <paramref name="sourceType"/>
    /// says, of a product or a patch as <paramref name="isPatch"/> says, in the order
    /// Windows lists them, or null where the list did not read to its end.
    ///
    /// ONE CALL PER ENTRY, WITH THE BUFFER. Windows keeps the position of the walk
    /// between calls. A call at index 0 starts the walk again, a call at the position
    /// is answered, and a call at any other index answers ERROR_INVALID_PARAMETER. A
    /// success moves the position on by one, and a call made to learn the length alone
    /// is a success too, so do not split a read into that call and a second one at the
    /// same index: the second is then out of sequence at every index after the first.
    /// Microsoft requires every call of one walk to come from the same thread, and this
    /// loop makes them all with nothing awaited between them.
    ///
    /// ONLY <see cref="MsiError.NoMoreItems"/> ENDS THE LIST. Every other return keeps
    /// the file, <see cref="MsiError.MoreData"/> from an entry longer than the buffer
    /// among them, and so does a list that runs past <see cref="MaxSourceIndex"/>
    /// without ending, since what lies beyond it is unread. The index moves on only
    /// after a success, so no entry is passed over.
    /// </summary>
    private IReadOnlyList<string>? SourcesOf(
        string code, bool isPatch, string? sid, MsiInstallContext context, uint sourceType)
    {
        var options = (isPatch ? MsiSourceListOptions.Patch : MsiSourceListOptions.Product) | sourceType;
        var sources = new List<string>();
        var buffer = new char[SourceBufferLength];

        for (uint index = 0; index < MaxSourceIndex; index++)
        {
            // Cleared per entry, so a length reported longer than what was written
            // reads as nulls and not as the tail of the entry before.
            Array.Clear(buffer);

            uint length = (uint)buffer.Length;
            var error = _msi.EnumSources(code, sid, context, options, index, buffer, ref length);
            if (error == MsiError.NoMoreItems) return sources;
            if (error != MsiError.Success) return null;

            var source = new string(buffer, 0, (int)Math.Min(length, (uint)buffer.Length)).TrimEnd('\0');
            if (source.Length == 0) return null;
            sources.Add(source);
        }

        return null;
    }

    /// <summary>
    /// Whether neither the API nor the registry gives a product's or a patch's list a
    /// media package path: the API's answer for
    /// <see cref="MsiInstallProperty.MediaPackagePath"/> and the <c>MediaPackage</c>
    /// value of the list's <c>Media</c> key, <paramref name="media"/>. A key that is not
    /// there names none. A read of the property that fails, a key that will not read and
    /// a value of a type other than a string all answer false, which keeps the file.
    /// </summary>
    private bool NamesNoMediaPackagePath(
        string code, bool isPatch, string? sid, MsiInstallContext context, RegistryKeyValues media)
    {
        var kind = isPatch ? MsiSourceListOptions.Patch : MsiSourceListOptions.Product;
        var answered = InstallerQueryService.ReadSourceListProperty(
            _msi, code, sid, context, kind, MsiInstallProperty.MediaPackagePath);
        if (answered.Unreadable || answered.Value.TrimEnd('\0').Length > 0) return false;

        if (media.Presence == RegistryKeyPresence.Absent) return true;
        if (media.Presence != RegistryKeyPresence.Present || media.Values is null) return false;

        var stored = ValueNamed(media.Values, "MediaPackage", out var count)?.Text;
        return count == 0 || stored is { Length: 0 };
    }

    /// <summary>
    /// Whether the source Windows Installer used last for a product or a patch, in one
    /// account and context, is one of <paramref name="network"/>, compared as text, so
    /// that the folder it tries first is one of the folders compared; and whether the
    /// registry holds it as the API answers it.
    ///
    /// THE REGISTRY HOLDS IT AS ONE VALUE of the <c>SourceList</c> key,
    /// <paramref name="sourceList"/>: the source's type, its index on the list and its
    /// text, each followed by a ';' but the last. The API answers the type and the text
    /// as two properties. The stored type and text have to be the two the API answers,
    /// and a value that is not there or is empty has to be answered as no source at all.
    ///
    /// NO SOURCE USED LAST ANSWERS TRUE, Windows Installer then going straight to the
    /// list. A URL or a media source, a source of any other kind, one on no list, one the
    /// registry holds differently and a read that fails answer false, which keeps the
    /// file.
    /// </summary>
    private bool SourceUsedLastIsOnTheList(
        string code,
        bool isPatch,
        string? sid,
        MsiInstallContext context,
        IReadOnlyList<string> network,
        IReadOnlyList<RegistryValue> sourceList)
    {
        var kind = isPatch ? MsiSourceListOptions.Patch : MsiSourceListOptions.Product;
        var source = InstallerQueryService.ReadSourceListProperty(
            _msi, code, sid, context, kind, MsiInstallProperty.LastUsedSource);
        var type = InstallerQueryService.ReadSourceListProperty(
            _msi, code, sid, context, kind, MsiInstallProperty.LastUsedType);
        if (source.Unreadable || type.Unreadable) return false;

        var folder = source.Value.TrimEnd('\0');
        var folderType = type.Value.TrimEnd('\0');

        var stored = ValueNamed(sourceList, MsiInstallProperty.LastUsedSource, out var count)?.Text;
        if (count > 0 && stored is null) return false;
        if (string.IsNullOrEmpty(stored)) return folder.Length == 0 && folderType.Length == 0;

        var parts = stored.Split(';', 3);
        if (parts.Length != 3
            || !string.Equals(parts[0], folderType, StringComparison.Ordinal)
            || !string.Equals(parts[2], folder, StringComparison.Ordinal)
            || folder.Length == 0
            || !string.Equals(folderType, "n", StringComparison.Ordinal))
            return false;

        foreach (var entry in network)
            if (string.Equals(entry, folder, StringComparison.Ordinal)) return true;

        return false;
    }

    /// <summary>
    /// The folder one installation of a product records as its <c>InstallSource</c>, read
    /// through the API and from the installation's <c>InstallProperties</c> key
    /// (<see cref="InstallPropertiesKeyPath"/>): the folder; an empty string where the
    /// two agree that there is none; or null, which keeps the file.
    ///
    /// NULL for: a read through the API that fails; a key that is not there or will not
    /// read, and a context or account that will not make its path; a key whose
    /// <c>InstallSource</c> is not one REG_SZ holding the API's text, or which holds one
    /// where the API answers none; a folder holding a '%' or a null; and a folder that
    /// starts neither with a drive letter, a ':' and a '\' nor with two '\'. A
    /// REG_EXPAND_SZ is refused even where its text is the same, being a value its reader
    /// may expand in the reader's own environment. Only those two forms of folder are
    /// compared, so a URL and any form not named here keep the file.
    /// </summary>
    private string? InstallSourceOf(IRegistryReader registry, string code, string? sid, MsiInstallContext context)
    {
        var read = InstallerQueryService.ReadProductProperty(
            _msi, code, sid, context, MsiInstallProperty.InstallSource);
        if (read.Unreadable) return null;

        var folder = read.Value.TrimEnd('\0');

        var path = InstallPropertiesKeyPath(code, sid, context);
        if (path is null) return null;

        var properties = registry.LocalMachineValues(path);
        if (properties.Presence != RegistryKeyPresence.Present || properties.Values is null) return null;

        var stored = ValueNamed(properties.Values, MsiInstallProperty.InstallSource, out var count);
        var agrees = count == 0
            ? folder.Length == 0
            : count == 1
                && stored is { Kind: RegistryValueKind.String } value
                && string.Equals(value.Text, folder, StringComparison.Ordinal);
        if (!agrees) return null;

        if (folder.Length == 0) return folder;
        if (folder.Contains('%') || folder.Contains('\0')) return null;

        var onADrive = folder.Length >= 3 && char.IsAsciiLetter(folder[0]) && folder[1] == ':' && folder[2] == '\\';
        return onADrive || folder.StartsWith(@"\\", StringComparison.Ordinal) ? folder : null;
    }

    /// <summary>
    /// Whether <paramref name="key"/> holds exactly <paramref name="entries"/>: values
    /// named 1 to n and nothing else, n being the number of entries, each holding as its
    /// text the entry at its place. A key that is not there holds an empty list. A key
    /// that will not read, a gap in the numbering, a value of any other name or of a type
    /// other than a string, one entry more or fewer, and any other text all answer false.
    /// The text is compared as stored, entries holding a '%' having already kept the file.
    /// </summary>
    private static bool IsTheList(RegistryKeyValues key, IReadOnlyList<string> entries)
    {
        if (key.Presence == RegistryKeyPresence.Absent) return entries.Count == 0;
        if (key.Presence != RegistryKeyPresence.Present || key.Values is null) return false;
        if (key.Values.Count != entries.Count) return false;

        for (var i = 0; i < entries.Count; i++)
        {
            var text = ValueNamed(key.Values, (i + 1).ToString(CultureInfo.InvariantCulture), out var count)?.Text;
            if (count != 1 || !string.Equals(text, entries[i], StringComparison.Ordinal)) return false;
        }

        return true;
    }

    /// <summary>
    /// Whether the values of the <c>SourceList</c> key, <paramref name="sourceList"/>,
    /// hold <paramref name="packageName"/>, the API's answer, as their one
    /// <c>PackageName</c> value: a REG_SZ with that text, compared as text. A value that
    /// is not there, one holding other text and one of any other type answer false,
    /// which keeps the file. A REG_EXPAND_SZ is among them even where its text is the
    /// same, being a value its reader may expand in the reader's own environment, so the
    /// name Windows Installer looks for need not be the text compared.
    /// </summary>
    private static bool HoldsThePackageName(IReadOnlyList<RegistryValue> sourceList, string packageName)
    {
        var stored = ValueNamed(sourceList, MsiInstallProperty.PackageName, out var count);
        return count == 1
            && stored is { Kind: RegistryValueKind.String } value
            && string.Equals(value.Text, packageName, StringComparison.Ordinal);
    }

    /// <summary>
    /// The value named <paramref name="name"/> among <paramref name="values"/>, or null
    /// where none is, and in <paramref name="count"/> how many values carry that name,
    /// compared without case as the registry compares value names.
    /// </summary>
    private static RegistryValue? ValueNamed(IReadOnlyList<RegistryValue> values, string name, out int count)
    {
        RegistryValue? found = null;
        count = 0;
        foreach (var value in values)
        {
            if (!string.Equals(value.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
            found = value;
            count++;
        }

        return found;
    }

    /// <summary>
    /// The HKLM path this check reads a product's or a patch's <c>SourceList</c> key
    /// from, in one account and context. Per machine,
    /// <c>SOFTWARE\Classes\Installer\Products</c> or <c>...\Patches</c>, then the code
    /// in its packed form, then <c>SourceList</c>; per user and managed, the same below
    /// <c>SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Managed\&lt;SID&gt;\Installer</c>.
    /// The network entries are read from its <c>Net</c> key and the URL entries from its
    /// <c>URL</c> key, each value named by the entry's number from 1. A key not found at
    /// this path keeps the file.
    ///
    /// NULL, WHICH KEEPS THE FILE, for any other context, per machine with an account,
    /// per user and managed without one, and a code or an account that will not make a
    /// key name. An account is taken only as 'S-' followed by digits and hyphens, so no
    /// account names a key outside its own.
    /// </summary>
    private static string? SourceListKeyPath(string code, bool isPatch, string? sid, MsiInstallContext context)
    {
        var packed = InstallerQueryService.PackRegistryCode(code);
        if (packed is null) return null;

        var kind = isPatch ? "Patches" : "Products";
        if (context == MsiInstallContext.Machine && sid is null)
            return $@"SOFTWARE\Classes\Installer\{kind}\{packed}\SourceList";

        if (context == MsiInstallContext.UserManaged && IsAccount(sid))
            return $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Managed\{sid}\Installer\{kind}\{packed}\SourceList";

        return null;
    }

    /// <summary>
    /// The HKLM path this check reads a product installation's <c>InstallProperties</c>
    /// key from: <c>SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData</c>, then
    /// <c>S-1-5-18</c> per machine or the account per user and managed, then
    /// <c>Products</c>, the code in its packed form and <c>InstallProperties</c>. A key
    /// not found at this path keeps the file.
    ///
    /// NULL, WHICH KEEPS THE FILE, for the contexts, accounts and codes
    /// <see cref="SourceListKeyPath"/> answers null for.
    /// </summary>
    private static string? InstallPropertiesKeyPath(string code, string? sid, MsiInstallContext context)
    {
        var packed = InstallerQueryService.PackRegistryCode(code);
        if (packed is null) return null;

        var account = context == MsiInstallContext.Machine && sid is null ? "S-1-5-18"
            : context == MsiInstallContext.UserManaged && IsAccount(sid) ? sid
            : null;

        return account is null
            ? null
            : $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\{account}\Products\{packed}\InstallProperties";
    }

    /// <summary>Whether <paramref name="sid"/> is 'S-' followed by digits and hyphens.</summary>
    private static bool IsAccount(string? sid)
    {
        if (sid is null || sid.Length < 3 || !sid.StartsWith("S-", StringComparison.Ordinal)) return false;
        for (var i = 2; i < sid.Length; i++)
            if (!char.IsAsciiDigit(sid[i]) && sid[i] != '-') return false;

        return true;
    }

    /// <summary>
    /// The length in characters of the buffer a source entry is read into: the longest
    /// path the Windows API takes, 32,767 characters, and a terminator.
    /// </summary>
    private const int SourceBufferLength = 32_768;

    /// <summary>
    /// How many entries of one source list are read before the list is taken as not
    /// having ended. A source list holds a handful of folders; the bound is there so an
    /// answer that never reports an end cannot hold the scan.
    /// </summary>
    private const uint MaxSourceIndex = 1024;

    /// <summary>
    /// The verdict for one patch copy: whether Windows holds a registration of the patch
    /// it declares, and if so whether this file is shown to be a different file from
    /// every copy each registration opens, cached or original.
    /// </summary>
    private DeclaredProductOutcome ScreenPatch(
        string path,
        PassAnswers pass,
        Action<Exception, string>? recordRefusal,
        Func<string, bool?>? namesAFileInInstallerFolder)
    {
        var identity = _identityReader.Read(path, isPatch: true, out var detail);

        // FOUR READINGS LEAVE NOTHING TO ASK ABOUT, and each of them is the file
        // failing to give this pass a patch code and the products to put it to. Null is
        // the reader's own "nothing here to ask". An empty code is the same outcome
        // reached without a null, which the seam's do-nothing implementations produce.
        // A reading not marked as a patch did not answer the question asked. And a
        // patch naming no product gives the keyed patch read no installation to ask.
        if (identity is null
            || identity.Value.Code.Length == 0
            || !identity.Value.IsPatch
            || identity.Value.TargetProductCodes.Count == 0)
        {
            // ONLY THE NULL READING HAS A DETAIL TO KEEP, for the reason given at the
            // product half's arm: the other three are answers the reader gave rather
            // than failures it had, and it wrote nothing down about them.
            if (identity is null)
                recordRefusal?.Invoke(
                    new InvalidOperationException(
                        "A cached patch did not yield the patch code and target products it "
                        + "declares, so it is kept rather than offered. Reader detail: "
                        + (detail.Length == 0 ? "none given" : detail) + "."),
                    detail);

            return DeclaredProductOutcome.DeclaredPatchUnestablished;
        }

        var code = identity.Value.Code;
        var targets = identity.Value.TargetProductCodes;

        // KEYED BY THE TARGET LIST AS WELL AS THE CODE. The keyed patch read asks the
        // products the file names, so two files carrying one patch code with different
        // lists put different questions, and each is answered from its own list.
        var key = code + "|" + string.Join(";", targets);
        if (!pass.Patches.TryGetValue(key, out var answer))
        {
            answer = AskAboutPatch(code, targets, pass, namesAFileInInstallerFolder);
            pass.Patches[key] = answer;
        }

        // As for a product: the registrations are shared by every copy declaring the
        // patch, and whether the copies they open are OTHER files is asked per file.
        return answer.Outcome == DeclaredProductOutcome.DeclaredPatchRegistered
            && answer.RecordedPackages is { } recorded
            && IsNoneOf(path, recorded)
                ? DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile
                : answer.Outcome;
    }

    /// <summary>
    /// What Windows holds for one declared patch, asked once per patch code and target
    /// list per pass: every registration the machine-wide patch enumeration lists for
    /// the code, unioned with every installation of a named target product that
    /// answers the keyed patch read with a state or with no value at all.
    ///
    /// THE UNION IS WHY IT IS BOTH. The enumeration names a registration against a
    /// product the patch's Template does not list; the keyed read reaches an
    /// installation of a listed product the enumeration does not name. Each can only
    /// add a registration.
    ///
    /// AND EITHER FAILING KEEPS THE FILE. An enumeration that did not run to its end, a
    /// named product whose installations would not list or were listed without one the
    /// caller's enumeration listed, and an installation that would not answer the keyed
    /// read each leave registrations unfound, and the answer is
    /// <see cref="DeclaredProductOutcome.DeclaredPatchUnestablished"/>. The enumeration
    /// is walked once per pass, so where it fails, every patch copy the pass asks about
    /// is kept.
    /// </summary>
    private DeclarationAnswer AskAboutPatch(
        string code,
        IReadOnlyList<string> targets,
        PassAnswers pass,
        Func<string, bool?>? namesAFileInInstallerFolder)
    {
        var holders = pass.PatchHolders;
        if (holders is null)
            return new DeclarationAnswer(DeclaredProductOutcome.DeclaredPatchUnestablished, null);

        var registrations = new List<(string ProductCode, string? Sid, MsiInstallContext Context)>();
        if (holders.TryGetValue(code, out var listed)) registrations.AddRange(listed);

        foreach (var target in targets)
        {
            pass.CancellationToken.ThrowIfCancellationRequested();

            var resolved = pass.InstancesOf(target);
            if (resolved.Unaskable)
                return new DeclarationAnswer(DeclaredProductOutcome.DeclaredPatchUnestablished, null);

            foreach (var (sid, context) in resolved.Instances)
            {
                // Already a registration: the enumeration listed it, and its copy is
                // read below whatever the keyed read would say.
                if (IsListed(registrations, target, sid, context)) continue;

                var state = InstallerQueryService.GetPatchProperty(
                    _msi, code, target, sid, context, MsiInstallProperty.State);

                // ONLY THE INSTALLATION ANSWERING THAT IT HOLDS NO RECORD OF THE PATCH IS
                // SKIPPED, and that answer is read first because it is marked unreadable
                // as well, for the other readers of the same call. An answer that the
                // installation's product is not installed is not that answer: the keyed
                // product enumeration listed this installation moments earlier, in this
                // account and context, so it contradicts what the pass established and
                // keeps the file with every other read that did not answer.
                if (state.PatchNotHeld) continue;
                if (state.Unreadable)
                    return new DeclarationAnswer(DeclaredProductOutcome.DeclaredPatchUnestablished, null);

                registrations.Add((target, sid, context));
            }
        }

        if (registrations.Count == 0)
            return new DeclarationAnswer(DeclaredProductOutcome.DeclaredPatchNotRegistered, null);

        return new DeclarationAnswer(
            DeclaredProductOutcome.DeclaredPatchRegistered,
            CopiesOpenedBy(code, registrations, namesAFileInInstallerFolder));
    }

    /// <summary>
    /// Whether <paramref name="registrations"/> already holds the installation of
    /// <paramref name="productCode"/> in <paramref name="sid"/> and
    /// <paramref name="context"/>. Codes and accounts are compared without case, the
    /// enumeration and the product walk each handing back their own spelling; the
    /// context is compared exactly.
    /// </summary>
    private static bool IsListed(
        List<(string ProductCode, string? Sid, MsiInstallContext Context)> registrations,
        string productCode,
        string? sid,
        MsiInstallContext context)
    {
        foreach (var registration in registrations)
            if (registration.Context == context
                && string.Equals(registration.ProductCode, productCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(registration.Sid, sid, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    /// <summary>
    /// The identity of every file a registration of <paramref name="code"/> opens as the
    /// patch: the cached copy each registration records, and the patch package at each
    /// folder on the patch's source list in each registration's account and context.
    /// Null where any of them cannot be seen.
    ///
    /// NULL IS THE ANSWER THAT KEEPS THE FILE, as it is for
    /// <see cref="PackagesOpenedBy"/>, and every way a registration's copy can fail to
    /// be seen reaches it: a <c>LocalPackage</c> read that failed or came back empty, a
    /// value that names nothing, names a folder, will not open to an identity, or names
    /// a file that does not read as patch <paramref name="code"/>; and any source the
    /// check cannot rule out, which <see cref="AddSourcePackages"/> sets out. One such
    /// registration is enough, because its copy is the one this candidate could be.
    ///
    /// A READ ANSWERING THAT THE PATCH IS NOT THERE KEEPS THE FILE LIKE ANY OTHER
    /// FAILED READ. Every registration here was named by one of the two routes moments
    /// earlier, so that answer contradicts it, and which copy the registration records
    /// is then not known.
    ///
    /// Several registrations can record one copy, so each path is looked at once.
    /// </summary>
    private IReadOnlyList<FileIdentity>? CopiesOpenedBy(
        string code,
        IReadOnlyList<(string ProductCode, string? Sid, MsiInstallContext Context)> registrations,
        Func<string, bool?>? namesAFileInInstallerFolder)
    {
        if (_fileIdentities is null || _fileSystem is null) return null;

        var identities = new List<FileIdentity>(registrations.Count);
        var looked = new Dictionary<string, FileIdentity>(StringComparer.Ordinal);

        // ONE SOURCE LIST PER ACCOUNT AND CONTEXT, NOT ONE PER REGISTRATION. The
        // source-list calls take the patch code with an account and a context and no
        // product, so a patch registered against several products in one account has
        // one list there, read the first time a registration in it is reached. Accounts
        // are compared without case, as IsListed compares them.
        var sourceListsRead = new HashSet<(string? Sid, MsiInstallContext Context)>();

        foreach (var (productCode, sid, context) in registrations)
        {
            var read = InstallerQueryService.GetPatchProperty(
                _msi, code, productCode, sid, context, MsiInstallProperty.LocalPackage);
            if (read.Unreadable) return null;

            var path = read.Value.TrimEnd('\0');
            if (path.Length == 0) return null;

            if (!looked.TryGetValue(path, out var recorded))
            {
                // File.Exists is false for a folder and for a path that will not parse,
                // and the identity read below opens folders too, so this is what keeps a
                // value naming a folder from standing in for a copy.
                if (!_fileSystem.File.Exists(path)) return null;

                if (_fileIdentities.ReadOutcome(path, out recorded) != FileIdentityRead.Read)
                    return null;

                // THE RECORDED COPY HAS TO READ AS THE SAME PATCH, for the reason the
                // product half's recorded package has to declare the same product: the
                // verdict rests on the record and the file agreeing.
                var declared = _identityReader.Read(path, isPatch: true, out _);
                if (declared is null
                    || !declared.Value.IsPatch
                    || !string.Equals(declared.Value.Code, code, StringComparison.Ordinal))
                    return null;

                looked[path] = recorded;
            }

            identities.Add(recorded);

            if (sourceListsRead.Add((sid?.ToUpperInvariant(), context))
                && !AddSourcePackages(code, isPatch: true, sid, context, namesAFileInInstallerFolder, identities))
                return null;
        }

        return identities;
    }

    /// <summary>
    /// Whether the candidate at <paramref name="candidatePath"/> is a different file
    /// from every one in <paramref name="recorded"/>: every package an installation of
    /// a product opens, or every copy a registration of a patch opens. A candidate
    /// whose own identity will not read is not shown to be different, so it answers
    /// false and is kept.
    /// </summary>
    private bool IsNoneOf(string candidatePath, IReadOnlyList<FileIdentity> recorded)
    {
        if (_fileIdentities is null) return false;
        if (_fileIdentities.ReadOutcome(candidatePath, out var candidate) != FileIdentityRead.Read)
            return false;

        foreach (var package in recorded)
            if (package == candidate) return false;

        return true;
    }

    /// <param name="Outcome">The verdict the declared code alone gives.</param>
    /// <param name="RecordedPackages">
    /// For an installed product, the identity of every file an installation opens as
    /// its package; for a registered patch, the identity of every file a registration
    /// opens as the patch. Null where any of them could not be seen, and null for every
    /// other verdict.
    /// </param>
    private readonly record struct DeclarationAnswer(
        DeclaredProductOutcome Outcome,
        IReadOnlyList<FileIdentity>? RecordedPackages);

    /// <summary>
    /// What one pass has asked Windows about installations and patch registrations,
    /// kept so that nothing is asked twice inside the pass and nothing outlives it.
    /// </summary>
    private sealed class PassAnswers
    {
        private readonly IMsiApi _msi;
        private readonly Dictionary<string, (IReadOnlyList<(string? Sid, MsiInstallContext Context)> Instances, bool Unaskable)>
            _instances = new(StringComparer.Ordinal);

        private readonly Dictionary<string, List<(string? Sid, MsiInstallContext Context)>> _listed;

        private Dictionary<string, List<(string ProductCode, string? Sid, MsiInstallContext Context)>>? _holders;
        private bool _holdersRead;

        internal PassAnswers(
            IMsiApi msi, IReadOnlyList<ListedInstallation> installations, CancellationToken cancellationToken)
        {
            _msi = msi;
            _listed = InstallerQueryService.InstallationsByCode(installations);
            CancellationToken = cancellationToken;
        }

        internal CancellationToken CancellationToken { get; }

        /// <summary>Each declared patch's answer, keyed by patch code and target list.</summary>
        internal Dictionary<string, DeclarationAnswer> Patches { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Every installation of one product code, asked once per pass whichever half
        /// asks.
        ///
        /// AN ANSWER LEAVING OUT AN INSTALLATION THE CALLER'S ENUMERATION LISTED COMES
        /// BACK UNASKABLE, the answer for a question that could not be put
        /// (<see cref="InstallerQueryService.HoldsEveryListedInstallation"/>). Held
        /// against that list, every installation the run knows of is asked about or the
        /// file is kept.
        /// </summary>
        internal (IReadOnlyList<(string? Sid, MsiInstallContext Context)> Instances, bool Unaskable)
            InstancesOf(string productCode)
        {
            if (!_instances.TryGetValue(productCode, out var resolved))
            {
                resolved = InstallerQueryService.ResolveProductInstances(_msi, productCode);
                if (!resolved.Unaskable
                    && !InstallerQueryService.HoldsEveryListedInstallation(_listed, productCode, resolved.Instances))
                    resolved = (Array.Empty<(string?, MsiInstallContext)>(), true);

                _instances[productCode] = resolved;
            }

            return resolved;
        }

        /// <summary>
        /// Every patch registration the machine-wide enumeration lists, keyed by patch
        /// code, or null where the enumeration did not run to its end. Walked the first
        /// time a patch asks, and not at all on a pass holding none.
        /// </summary>
        internal Dictionary<string, List<(string ProductCode, string? Sid, MsiInstallContext Context)>>? PatchHolders
        {
            get
            {
                if (!_holdersRead)
                {
                    _holders = InstallerQueryService.EnumeratePatchHoldersAcrossAllProducts(_msi, CancellationToken);
                    _holdersRead = true;
                }

                return _holders;
            }
        }
    }
}
