using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Abstractions;
using System.Runtime.ExceptionServices;
using System.Text;
using InstallerClean.Helpers;
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
/// <c>InstallSource</c>. A package in a folder on the network is read only for a
/// candidate whose name it could be (<see cref="AddSourcePackages"/>). Every answer
/// about a product is held against the installations
/// the caller's own enumeration listed, and one leaving out any of them keeps the file.
/// The cached package of each of those installations is read too, and a candidate is
/// also put to every installation whose cached package declares the candidate's code
/// while the installation is registered under another, read by the code it is registered
/// under. Every candidate installation package is compared as well with the packages
/// each installation the caller could not rule out as a second copy of a program opens,
/// its cached package and the packages its sources name, and where those cannot all be
/// seen every candidate installation package is kept. So is every one where an
/// installation's cached package does not say which product it declares, unless its own
/// record shows it to be an ordinary installation, or it records no cached package and
/// has no source list, by the API's answer and in the registry, so that Windows Installer
/// has no package to open for it. An installation that records no cached package and has
/// a source list opens what its sources name, and every candidate is compared with those
/// packages where they can all be seen.
/// For each candidate patch it reads the patch's own code and the products its Template
/// names, finds the registrations of that patch through the machine-wide patch
/// enumeration and the keyed patch read, and reads the <c>LocalPackage</c> each
/// registration records. A product's source list in a per-user-unmanaged context is
/// not read, and an installation in one keeps the file. Every source list it does read
/// is read twice, through the API and from the registry key that holds it, and a list
/// the two do not agree on keeps the file. Every <c>InstallSource</c> it reads is read
/// the same two ways, and one the two do not agree on keeps the file too.
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
    private readonly IRunningAccount? _runningAccount;

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
    /// <param name="runningAccount">
    /// The account this process runs as, which a per-user installation's account is held
    /// against before its own record is read (<see cref="WhatItsOwnRecordShows"/>).
    /// </param>
    /// <remarks>
    /// WITHOUT BOTH FILE READERS NO RECORDED PACKAGE IS LOOKED AT, and every candidate
    /// whose declared product is installed is kept as
    /// <see cref="DeclaredProductOutcome.DeclaredProductInstalled"/>, and every
    /// candidate whose declared patch is registered as
    /// <see cref="DeclaredProductOutcome.DeclaredPatchRegistered"/>, and beside an
    /// installation not ruled out as a second copy every candidate the answer would let
    /// through is kept as <see cref="DeclaredProductOutcome.SecondCopyUnestablished"/>.
    /// WITHOUT THE REGISTRY READER NO SOURCE LIST IS RELIED ON, and every candidate the comparison
    /// reaches a source list for is kept the same way. WITHOUT THE RUNNING ACCOUNT NO
    /// PER-USER INSTALLATION'S RECORD IS READ, so one whose cached package does not say
    /// what it declares keeps every installation package. That is the direction a missing
    /// dependency has to fail in. The composition root supplies all four.
    /// </remarks>
    public DeclaredProductCheck(
        IMsiApi msi,
        IPackageIdentityReader identityReader,
        IFileIdentityReader? fileIdentities = null,
        IFileSystem? fileSystem = null,
        IRegistryReader? registry = null,
        IRunningAccount? runningAccount = null)
    {
        _msi = msi;
        _identityReader = identityReader;
        _fileIdentities = fileIdentities;
        _fileSystem = fileSystem;
        _registry = registry;
        _runningAccount = runningAccount;
    }

    /// <summary>Whether this check compares recorded packages with the candidate.</summary>
    internal bool ComparesRecordedPackages => _fileIdentities is not null && _fileSystem is not null;

    /// <summary>Whether this check reads the registry key each source list is held in.</summary>
    internal bool ReadsSourceListKeys => _registry is not null;

    /// <summary>Whether this check knows the account this process runs as.</summary>
    internal bool KnowsTheRunningAccount => _runningAccount is not null;

    /// <inheritdoc />
    public DeclaredProductScreening Screen(
        IReadOnlyList<OrphanedFile> candidates,
        IReadOnlyList<ListedInstallation> installations,
        CancellationToken cancellationToken = default,
        Action<Exception, string>? recordRefusal = null,
        Func<string, bool?>? namesAFileInInstallerFolder = null,
        Action<int>? candidateReached = null,
        Action<SourceFolderWait?>? waitingOn = null)
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
        var pass = new PassAnswers(_msi, installations, cancellationToken, waitingOn);

        for (var i = 0; i < candidates.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            candidateReached?.Invoke(i + 1);

            var candidate = candidates[i];

            // A PATCH DECLARES A PATCH CODE AND NOT A PRODUCT CODE, so it is screened
            // against the registrations of that patch rather than against the
            // installations of a product.
            if (candidate.IsPatch)
            {
                outcomes[i] = ScreenPatch(candidate.FullPath, pass, recordRefusal);
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
                answer = Ask(code, pass, namesAFileInInstallerFolder, recordRefusal);
                asked[code] = answer;
            }

            // The product-level answer is shared by every candidate declaring the
            // code; whether the recorded packages are OTHER files is a question about
            // this candidate, so it is asked per file.
            outcomes[i] = Settle(
                candidate.FullPath, answer, pass, namesAFileInInstallerFolder, recordRefusal, out var givenUp);

            // A file kept where its check stopped at a read refused for a root given up
            // counts towards that root. A patch reads no source folder, so it never does.
            if (givenUp is not null && outcomes[i].Withholds()) pass.CountKept(givenUp);
        }

        return new DeclaredProductScreening(outcomes, pass.GivenUp(), pass.WaitCount, pass.Census.Census());
    }

    /// <summary>
    /// What Windows holds for one declared product code, asked once per code per
    /// pass: the installations of that code, and every installation the caller listed
    /// whose cached package declares it while the installation is registered under
    /// another code.
    ///
    /// AN INSTALLATION CAN ANSWER FOR A CODE IT IS NOT REGISTERED UNDER. A program
    /// installed a second time under an instance transform is registered under the
    /// product code the transform produced, while the original package it was installed
    /// from declares the base code, and so can the package cached for it. That original
    /// can be this candidate: a file in the Installer folder which the copy's source list
    /// names. So an installation whose cached package declares the candidate's code
    /// answers for the candidate beside the installations of the code itself, each read
    /// by the code it is registered under (<see cref="LinksOf"/>).
    /// </summary>
    private DeclarationAnswer Ask(
        string code,
        PassAnswers pass,
        Func<string, bool?>? namesAFileInInstallerFolder,
        Action<Exception, string>? recordRefusal)
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

        var links = LinksOf(pass, namesAFileInInstallerFolder, recordRefusal);

        var installations = new List<(string RegisteredCode, string? Sid, MsiInstallContext Context)>();
        foreach (var (sid, context) in resolved.Instances) installations.Add((code, sid, context));
        if (links.ByDeclaredCode.TryGetValue(code, out var linked)) installations.AddRange(linked);

        if (installations.Count == 0)
            return new DeclarationAnswer(DeclaredProductOutcome.DeclaredProductNotInstalled, null);

        var packages = PackagesOpenedBy(code, installations, pass, namesAFileInInstallerFolder, out var givenUp);
        return new DeclarationAnswer(
            DeclaredProductOutcome.DeclaredProductInstalled, packages?.Identities, packages?.ByName, givenUp);
    }

    /// <summary>
    /// The verdict for one installation package, given the answer about the product it
    /// declares: that answer, put to the packages opened by every installation the
    /// caller could not rule out as a second copy of a program.
    ///
    /// ONLY A CANDIDATE THE ANSWER LETS THROUGH IS PUT TO THEM. A verdict that already
    /// keeps the file stands, so a file whose own product's packages cannot be seen stays
    /// <see cref="DeclaredProductOutcome.DeclaredProductInstalled"/> whatever a second
    /// copy opens.
    ///
    /// A SECOND COPY OPENS PACKAGES THAT DECLARE NO PARTICULAR CODE. A program installed
    /// under an instance transform is registered under the code the transform produced,
    /// and the package it was installed from, and the package cached for it, need not
    /// declare that code or any code the check can link it by. So the candidate is
    /// compared by file identity with every package such an installation opens, its
    /// cached package and the packages its sources name, whatever each declares, and with
    /// the packages the sources name of an installation recording no cached package whose
    /// record does not show an ordinary installation. A candidate that opens as one of them
    /// is kept as <see cref="DeclaredProductOutcome.DeclaredProductInstalled"/>, and a
    /// candidate shown to be a different file from all of them is given the answer it
    /// already had. Where those packages cannot all be seen
    /// (<see cref="PackagesSecondCopiesOpen"/>), or an installation's cached package does
    /// not say what it declares and its own record does not show an ordinary installation
    /// nor that it opens no package, nor, where it records none, that every package its
    /// sources name can be seen (<see cref="LinksOf"/>), every candidate the answer lets
    /// through is <see cref="DeclaredProductOutcome.SecondCopyUnestablished"/>.
    ///
    /// A PACKAGE IN A FOLDER ON THE NETWORK IS READ HERE, FOR THIS CANDIDATE, and only
    /// where its package name could be this candidate's name
    /// (<see cref="WithPackagesItCouldBe"/>). One that cannot be ruled out keeps this
    /// candidate, with the verdict a package read for every candidate gives them all:
    /// <see cref="DeclaredProductOutcome.DeclaredProductInstalled"/> among its own
    /// product's packages, and
    /// <see cref="DeclaredProductOutcome.SecondCopyUnestablished"/> among a second copy's.
    ///
    /// THE CANDIDATE'S IDENTITY IS READ ONCE, against its own product's packages and the
    /// second copies' together, where both can be seen. A candidate whose product is not
    /// installed and which no such installation stands beside is let through without its
    /// identity being read, having nothing to be compared with.
    ///
    /// <paramref name="givenUp"/> is the root whose give-up refused the read the verdict
    /// stopped at (<see cref="ReadSourcePackage"/>), or that alone left an installation's
    /// cached package unsettled (<see cref="InstallationLinks.GivenUp"/>), and null for every
    /// other verdict, among them that of a candidate its own comparison keeps while the second
    /// copies' packages cannot be seen.
    /// </summary>
    private DeclaredProductOutcome Settle(
        string candidatePath,
        DeclarationAnswer answer,
        PassAnswers pass,
        Func<string, bool?>? namesAFileInInstallerFolder,
        Action<Exception, string>? recordRefusal,
        out string? givenUp)
    {
        givenUp = null;
        var candidate = new CandidateNames(candidatePath, NamesInFolderOf);

        IReadOnlyList<FileIdentity> recorded;
        DeclaredProductOutcome letThrough;
        switch (answer.Outcome)
        {
            case DeclaredProductOutcome.DeclaredProductInstalled when answer.RecordedPackages is { } packages:
                if (WithPackagesItCouldBe(
                        candidate, packages, answer.ByName ?? [], pass, namesAFileInInstallerFolder, out givenUp)
                    is not { } own)
                    return DeclaredProductOutcome.DeclaredProductInstalled;

                recorded = own;
                letThrough = DeclaredProductOutcome.DeclaredProductCachedAsAnotherFile;
                break;
            case DeclaredProductOutcome.DeclaredProductNotInstalled:
                recorded = [];
                letThrough = DeclaredProductOutcome.DeclaredProductNotInstalled;
                break;
            default:
                givenUp = answer.GivenUp;
                return answer.Outcome;
        }

        IReadOnlyList<FileIdentity>? secondCopies = null;
        string? secondCopiesGivenUp;
        var unseenByName = false;
        var links = LinksOf(pass, namesAFileInInstallerFolder, recordRefusal);
        if (links.UnreadPackageNotRuledOut)
        {
            secondCopiesGivenUp = links.GivenUp;
        }
        else if (PackagesSecondCopiesOpen(pass, links, namesAFileInInstallerFolder, out secondCopiesGivenUp) is { } second)
        {
            secondCopies = WithPackagesItCouldBe(
                candidate, second.Identities, second.ByName, pass, namesAFileInInstallerFolder, out secondCopiesGivenUp);
            unseenByName = secondCopies is null;
        }

        if (secondCopies is null)
        {
            var verdict = recorded.Count == 0
                ? letThrough
                : CompareWithRecorded(candidatePath, recorded, letThrough, DeclaredProductOutcome.DeclaredProductInstalled);
            if (verdict.Withholds()) return verdict;

            if (unseenByName) pass.Census.UnseenByName();
            givenUp = secondCopiesGivenUp;
            return DeclaredProductOutcome.SecondCopyUnestablished;
        }

        IReadOnlyList<FileIdentity> opened = secondCopies.Count == 0 ? recorded : [.. recorded, .. secondCopies];
        return opened.Count == 0
            ? letThrough
            : CompareWithRecorded(candidatePath, opened, letThrough, DeclaredProductOutcome.DeclaredProductInstalled);
    }

    /// <summary>
    /// The identity of every package opened by an installation the caller listed and
    /// could not rule out as a second copy of a program
    /// (<see cref="ListedInstallation.SecondCopyNotRuledOut"/>): the cached package each
    /// records, and the original package at each folder its sources name, a package in a
    /// folder on the network being left to be read for each candidate whose name it could
    /// be (<see cref="OpenedPackages.ByName"/>); with them, the packages the sources name of
    /// each installation recording no cached package whose record does not show an ordinary
    /// installation, read for the pass's links (<see cref="InstallationLinks.Opened"/>). Those
    /// alone where no such installation is listed, and null where any package read here
    /// cannot be seen. Read once per pass, the first time a candidate the answer lets through
    /// is put to them, and kept for every candidate after it.
    ///
    /// NULL IS THE ANSWER THAT KEEPS EVERY CANDIDATE, and every way such an
    /// installation's packages can fail to be seen reaches it: a <c>LocalPackage</c> read
    /// that failed, a value that names nothing, names a folder, will not open to an
    /// identity, or names a file that yields no product code; any source
    /// <see cref="AddSourcePackages"/> cannot rule out, a per-user-unmanaged context among
    /// them, an installation recording no cached package being judged by its sources alone
    /// unless it has no package to open at all (<see cref="OpensNoPackage"/>); and a check
    /// built without its file readers, having no way to look. One such installation is
    /// enough, because any candidate could be the package it opens.
    ///
    /// EACH INSTALLATION IS READ BY THE CODE IT IS REGISTERED UNDER, in its own account
    /// and context, and its cached package is not required to declare that code.
    ///
    /// <paramref name="givenUp"/> is the root whose give-up refused the read that made it
    /// null (<see cref="ReadSourcePackage"/>), kept for the pass with the answer, and null
    /// otherwise.
    ///
    /// Each installation read is counted in the pass's census, the one that makes it null by
    /// the step that stopped it (<see cref="CensusTally.SecondCopyRead"/>).
    /// </summary>
    private OpenedPackages? PackagesSecondCopiesOpen(
        PassAnswers pass, InstallationLinks links, Func<string, bool?>? namesAFileInInstallerFolder, out string? givenUp)
    {
        if (pass.SecondCopiesRead)
        {
            givenUp = pass.SecondCopiesGivenUp;
            return pass.SecondCopies;
        }

        givenUp = null;
        List<FileIdentity>? identities = [.. links.Opened.Identities];
        List<NetworkPackage> byName = [.. links.Opened.ByName];
        foreach (var installation in pass.Installations)
        {
            pass.CancellationToken.ThrowIfCancellationRequested();
            if (!installation.SecondCopyNotRuledOut) continue;

            var seen = AddSecondCopyPackages(
                installation, pass, namesAFileInInstallerFolder, identities, byName, out givenUp, out var reading);
            pass.Census.SecondCopyRead(reading, (MsiInstallContext)installation.Context);
            if (!seen)
            {
                identities = null;
                break;
            }
        }

        pass.SecondCopies = identities is null ? null : new OpenedPackages(identities, byName);
        pass.SecondCopiesGivenUp = givenUp;
        pass.SecondCopiesRead = true;
        return pass.SecondCopies;
    }

    /// <summary>
    /// Adds to <paramref name="opened"/> the identity of the cached package one
    /// installation records and of the package at each folder its sources name, and
    /// answers false where any of them cannot be seen. A package in a folder on the
    /// network goes into <paramref name="byName"/> instead (<see cref="AddSourcePackages"/>).
    /// The cached package has to be a file that is there, that identifies, and that
    /// yields a product code: a value naming anything else shows nothing about which
    /// package the installation opens. An installation that records none adds the packages
    /// its sources name, and nothing where it has no package to open at all
    /// (<see cref="OpensNoPackage"/>). <paramref name="givenUp"/> is as
    /// <see cref="AddSourcePackages"/> gives it, and <paramref name="reading"/> says which
    /// step answered false, or <see cref="SecondCopyReading.Seen"/>.
    /// </summary>
    private bool AddSecondCopyPackages(
        ListedInstallation installation,
        PassAnswers pass,
        Func<string, bool?>? namesAFileInInstallerFolder,
        List<FileIdentity> opened,
        List<NetworkPackage> byName,
        out string? givenUp,
        out SecondCopyReading reading)
    {
        givenUp = null;
        reading = SecondCopyReading.NoReaders;
        if (_fileIdentities is null || _fileSystem is null) return false;

        var context = (MsiInstallContext)installation.Context;
        var read = InstallerQueryService.ReadProductProperty(
            _msi, installation.ProductCode, installation.UserSid, context, MsiInstallProperty.LocalPackage);
        reading = SecondCopyReading.PathUnreadable;
        if (read.Unreadable) return false;

        // An installation recording no cached package opens what its sources name, and nothing
        // where it has no package to open at all (OpensNoPackage).
        var path = read.Value.TrimEnd('\0');
        if (path.Length == 0)
        {
            var seen = OpensNoPackage(installation.ProductCode, installation.UserSid, context, pass)
                || AddSourcePackages(installation.ProductCode, installation.UserSid, context, pass,
                    namesAFileInInstallerFolder, opened, byName, out givenUp, out _);
            reading = seen ? SecondCopyReading.Seen : SecondCopyReading.NoneRecorded;
            return seen;
        }

        // File.Exists is false for a folder and for a path that will not parse, and the
        // identity read below opens folders too, so this is what keeps a value naming a
        // folder from standing in for a package.
        reading = SecondCopyReading.NotThere;
        if (!_fileSystem.File.Exists(path)) return false;

        reading = SecondCopyReading.WouldNotIdentify;
        if (_fileIdentities.ReadOutcome(path, out var recorded) != FileIdentityRead.Read) return false;

        var declared = _identityReader.Read(path, isPatch: false, out _, out var refusal);
        reading = declared is null && refusal == PackageReadRefusal.WouldNotRead
            ? SecondCopyReading.WouldNotRead
            : SecondCopyReading.NoProductCode;
        if (declared is null || declared.Value.IsPatch || declared.Value.Code.Length == 0) return false;

        opened.Add(recorded);

        var sourcesSeen = AddSourcePackages(installation.ProductCode, installation.UserSid, context, pass,
            namesAFileInInstallerFolder, opened, byName, out givenUp, out var sourceRefusal);
        reading = sourceRefusal switch
        {
            SourceRefusal.None => SecondCopyReading.Seen,
            SourceRefusal.PerUserUnmanaged => SecondCopyReading.PerUserUnmanaged,
            SourceRefusal.GivenUp => SecondCopyReading.SourcesGivenUp,
            _ => SecondCopyReading.SourceNotRuledOut,
        };
        return sourcesSeen;
    }

    /// <summary>
    /// Every installation the caller listed that is registered under a code other than
    /// the one its cached package declares, keyed by the declared code; whether an
    /// installation whose cached package did not say what it declares is not shown by its
    /// own record to be an ordinary installation nor shown to open no package, nor, where it
    /// records no cached package, shown to open nothing but packages that can all be seen;
    /// and those packages. Each installation is counted in the pass's census by what its
    /// cached package gave, whether it opens no package, what its record showed, and, for
    /// one recording no cached package, what its sources showed (<see cref="CensusTally"/>).
    /// Read once per pass, the first time it is needed.
    ///
    /// EVERY CONTEXT IS READ, AND A FAILED READ KEEPS UNLESS THE RECORD RULES IT OUT. A
    /// link only ever adds an installation to the ones a candidate is compared with, so
    /// it can keep a file and never offer one. Where an installation's cached package
    /// does not say what it declares, nothing links it to any candidate, so that
    /// installation keeps every file unless its own record shows it to be an ordinary
    /// installation (<see cref="WhatItsOwnRecordShows"/>), or it records no cached package
    /// and Windows Installer has no package to open for it (<see cref="OpensNoPackage"/>):
    /// <see cref="Settle"/> then gives every candidate the answer would let through
    /// <see cref="DeclaredProductOutcome.SecondCopyUnestablished"/>.
    ///
    /// AN INSTALLATION RECORDING NO CACHED PACKAGE OPENS WHAT ITS SOURCES NAME. Where it has a
    /// package to open and its record does not show an ordinary installation, its sources are
    /// read (<see cref="AddSourcePackages"/>). Where every package they name can be seen, the
    /// installation keeps no file, and every candidate the answer would let through is
    /// compared by identity with those packages instead (<see cref="PackagesSecondCopiesOpen"/>);
    /// where one cannot, it keeps every file as above.
    ///
    /// A FILE KEPT HERE COUNTS TOWARDS A DRIVE OR SHARE GIVEN UP ONLY WHERE THAT IS ALL THAT KEPT
    /// IT: where every installation keeping every file does so because a read of its sources was
    /// refused for a root given up for the pass, the first such root, and otherwise none
    /// (<see cref="InstallationLinks.GivenUp"/>).
    ///
    /// The codes are compared as GUIDs, because the reader canonicalises the declared
    /// code and the listed code is in whatever spelling the caller's enumeration gave.
    /// Compared as text, an ordinary installation listed in lower case would be linked
    /// to its own code.
    /// </summary>
    private InstallationLinks LinksOf(
        PassAnswers pass, Func<string, bool?>? namesAFileInInstallerFolder, Action<Exception, string>? recordRefusal)
    {
        if (pass.Links is { } read) return read;

        var byDeclaredCode =
            new Dictionary<string, List<(string RegisteredCode, string? Sid, MsiInstallContext Context)>>(
                StringComparer.Ordinal);
        List<FileIdentity> opened = [];
        List<NetworkPackage> byName = [];
        string? unread = null;
        string? givenUp = null;
        var keptOtherwise = false;

        foreach (var installation in pass.Installations)
        {
            pass.CancellationToken.ThrowIfCancellationRequested();

            var context = (MsiInstallContext)installation.Context;
            var declared = CodeTheCachedPackageDeclares(
                installation.ProductCode, installation.UserSid, context, out var reading, out var detail);
            if (declared is null)
            {
                var noPackage = reading == CachedPackageReading.NoneRecorded
                    ? NoPackageReadingOf(installation.ProductCode, installation.UserSid, context, pass)
                    : default(NoPackageReading?);
                if (noPackage?.Answer == NoPackageAnswer.OpensNone)
                {
                    pass.Census.OpensNoPackage();
                    continue;
                }

                var record = WhatItsOwnRecordShows(installation.ProductCode, installation.UserSid, context);
                if (record != RecordReading.Ordinary)
                {
                    if (noPackage is { } found)
                    {
                        List<FileIdentity> itsPackages = [];
                        List<NetworkPackage> itsByName = [];
                        if (AddSourcePackages(installation.ProductCode, installation.UserSid, context, pass,
                                namesAFileInInstallerFolder, itsPackages, itsByName, out var sourceGivenUp, out _))
                        {
                            opened.AddRange(itsPackages);
                            byName.AddRange(itsByName);
                            pass.Census.ReleasedBySources();
                            continue;
                        }

                        detail = WhyItsSourcesKeep(found);
                        pass.Census.NoneRecordedKept(found.Answer);
                        if (sourceGivenUp is null) keptOtherwise = true;
                        else givenUp ??= sourceGivenUp;
                    }
                    else
                    {
                        keptOtherwise = true;
                    }

                    unread ??= detail;
                }

                pass.Census.Undeclared(reading, record, context);
                continue;
            }

            pass.Census.Declared();
            if (SameCode(declared, installation.ProductCode)) continue;

            if (!byDeclaredCode.TryGetValue(declared, out var linked))
                byDeclaredCode[declared] = linked = new();
            linked.Add((installation.ProductCode, installation.UserSid, context));
        }

        // Once for the pass: one installation keeps every installation package, and the
        // log says why once.
        if (unread is not null)
            recordRefusal?.Invoke(
                new InvalidOperationException(
                    "A program's cached package did not yield the product code it declares, and its own "
                    + "record did not show it to be an ordinary installation, so every installation package "
                    + "is kept rather than offered. Detail: " + unread + "."),
                unread);

        return pass.Links = new InstallationLinks(
            byDeclaredCode,
            unread is not null,
            new OpenedPackages(opened, byName),
            unread is not null && !keptOtherwise ? givenUp : null);
    }

    /// <summary>
    /// The detail the log gives for an installation that records no cached package and keeps
    /// every installation package, its sources not ruled out: what <paramref name="reading"/>
    /// shows kept it.
    /// </summary>
    private static string WhyItsSourcesKeep(NoPackageReading reading) => reading.Answer switch
    {
        NoPackageAnswer.OtherAnswer =>
            "the installation records no cached package, and Windows Installer answered "
            + reading.Error.ToString(CultureInfo.InvariantCulture)
            + " when asked its source list's package name",
        NoPackageAnswer.RegistryDisagrees =>
            "the installation records no cached package, and the registry holds a cached package or a source "
            + "list where Windows Installer answers that it has none, or a key would not read",
        _ => "the installation records no cached package, and a source it names could not be ruled out",
    };

    /// <summary>
    /// What Windows answers, to this process, of the record one installation keeps of
    /// itself. <see cref="RecordReading.Ordinary"/> where its <c>PackageCode</c> reads as a
    /// value and its <c>InstanceType</c> reads as an ordinary installation, through the
    /// classification the scan's own reading uses
    /// (<see cref="InstallerQueryService.ReadInstanceType"/>), and otherwise the first of
    /// those two that did not. Both are read by the code the installation is registered
    /// under, in its account and context.
    ///
    /// A PER-USER INSTALLATION HAS TO BELONG TO THE ACCOUNT THIS PROCESS RUNS AS. A
    /// per-user installation keeps these properties under its own account, and asked
    /// about another account's installation, an answer need not come from that account's
    /// record. So in the two per-user contexts the installation's account is
    /// compared with <see cref="IRunningAccount.Sid"/>, without regard to case, and any
    /// other account, or no account, is <see cref="RecordReading.AnotherAccount"/> with
    /// nothing read. A per-machine installation's record is the machine's, and no account is
    /// compared for it.
    ///
    /// <c>PACKAGECODE</c> IS WHAT SHOWS THE RECORD ANSWERED. Every installed product has a
    /// package code. The returns <see cref="InstallerQueryService.ReadProductProperty"/>
    /// reads as benign give an empty value, so an <c>InstanceType</c> that reads as
    /// ordinary looks the same whether the record carries no such value or did not answer
    /// at all. A <c>PackageCode</c> that comes back as a value tells the two apart.
    /// </summary>
    private RecordReading WhatItsOwnRecordShows(string code, string? sid, MsiInstallContext context)
    {
        if (context != MsiInstallContext.Machine
            && (sid is null
                || _runningAccount?.Sid is not { } account
                || !string.Equals(sid, account, StringComparison.OrdinalIgnoreCase)))
            return RecordReading.AnotherAccount;

        var packageCode = InstallerQueryService.ReadProductProperty(
            _msi, code, sid, context, MsiInstallProperty.PackageCode);
        if (packageCode.Unreadable || packageCode.Value.TrimEnd('\0').Length == 0)
            return RecordReading.PackageCodeUnanswered;

        return InstallerQueryService.ReadInstanceType(_msi, code, sid, context)
            == InstallerQueryService.InstanceReading.Ordinary
            ? RecordReading.Ordinary
            : RecordReading.InstanceTypeNotOrdinary;
    }

    /// <summary>
    /// Whether Windows Installer has no package to open for one installation that records
    /// no cached package: whether it has no source list either, by the API's answer and by
    /// the registry alike. Asked only where the installation's <c>LocalPackage</c> has
    /// already read as none. An installation for which this answers false can open only what
    /// its sources name, and is judged by them (<see cref="AddSourcePackages"/>).
    ///
    /// WINDOWS INSTALLER OPENS A PRODUCT'S PACKAGE IN TWO WAYS ONLY: the cached copy its
    /// <c>LocalPackage</c> names, and the file named by its package name in a folder on its
    /// source list. An installation with neither opens no file in the Installer folder, so
    /// it is no reason to keep one.
    ///
    /// TRUE ONLY WHERE ALL OF THESE HOLD, and anything short of them answers false
    /// (<see cref="NoPackageReadingOf"/> says which):
    /// - the installation is per machine, with no account;
    /// - <c>MsiSourceListGetInfo</c> answers <see cref="MsiError.BadConfiguration"/> for its
    ///   package name, and no other return;
    /// - its <c>InstallProperties</c> key (<see cref="InstallPropertiesKeyPath"/>) is absent or
    ///   records no cached package (<see cref="RecordsNoCachedPackage"/>), and its
    ///   <c>SourceList</c> key (<see cref="SourceListKeyPath"/>) is absent, a key that will not
    ///   read answering false.
    /// The API is asked first, so a registry read is made only for an installation whose
    /// source list the API has already answered for. A check built without its registry
    /// reader answers false for every installation.
    ///
    /// AN ANSWER THAT KEEPS FILES STANDS FOR THE PASS, AND ONE THAT LETS FILES THROUGH IS ASKED
    /// AGAIN AT THE NEXT CALL (<see cref="PassAnswers.OpensNoPackageOf"/>). Once any of the three
    /// steps finds a package to open for an installation, whatever the case of the code's letters,
    /// every later step of the pass takes that answer without asking. <see cref="LinksOf"/> and
    /// <see cref="PackagesSecondCopiesOpen"/> each reach their conclusion once in a pass, and
    /// <see cref="PackagesOpenedBy"/> once for each declared code, and every later candidate
    /// takes that conclusion without a further call. A source list that appears while the pass
    /// runs is found by the next step that asks, and that step reads the sources it names.
    /// </summary>
    private bool OpensNoPackage(string code, string? sid, MsiInstallContext context, PassAnswers pass) =>
        NoPackageReadingOf(code, sid, context, pass).Answer == NoPackageAnswer.OpensNone;

    /// <summary>
    /// What <see cref="OpensNoPackage"/> finds for one installation that records no cached
    /// package: <see cref="NoPackageAnswer.OpensNone"/> where it answers true, and otherwise
    /// the first of its conditions that does not hold.
    /// </summary>
    private NoPackageReading NoPackageReadingOf(string code, string? sid, MsiInstallContext context, PassAnswers pass)
    {
        if (context != MsiInstallContext.Machine || sid is not null || _registry is null)
            return new NoPackageReading(NoPackageAnswer.NotAsked);

        return pass.OpensNoPackageOf(code, sid, context, () => AsksAndFindsNoPackage(code, sid, context));
    }

    /// <summary>The questions <see cref="OpensNoPackage"/> puts, for one installation it has not yet asked about.</summary>
    private NoPackageReading AsksAndFindsNoPackage(string code, string? sid, MsiInstallContext context)
    {
        uint length = 0;
        var answered = _msi.GetSourceListInfo(code, sid, context, MsiSourceListOptions.Product,
            MsiInstallProperty.PackageName, null, ref length);
        if (answered is MsiError.Success or MsiError.MoreData)
            return new NoPackageReading(NoPackageAnswer.ListAnswered, answered);
        if (answered != MsiError.BadConfiguration)
            return new NoPackageReading(NoPackageAnswer.OtherAnswer, answered);

        var properties = InstallPropertiesKeyPath(code, sid, context);
        var sourceList = SourceListKeyPath(code, sid, context);
        return properties is not null
            && sourceList is not null
            && RecordsNoCachedPackage(_registry!.LocalMachineValues(properties))
            && _registry.LocalMachineValues(sourceList).Presence == RegistryKeyPresence.Absent
                ? new NoPackageReading(NoPackageAnswer.OpensNone, answered)
                : new NoPackageReading(NoPackageAnswer.RegistryDisagrees, answered);
    }

    /// <summary>
    /// Whether an installation's <c>InstallProperties</c> key, <paramref name="key"/>, records no
    /// cached package: the key is not there, or it is there and holds no value of either name a
    /// cached package is recorded under (<see cref="InstallerQueryService.CachedPackageValueNames"/>),
    /// of any type and any content. A key that will not read answers false.
    /// </summary>
    private static bool RecordsNoCachedPackage(RegistryKeyValues key)
    {
        if (key.Presence == RegistryKeyPresence.Absent) return true;
        if (key.Presence != RegistryKeyPresence.Present || key.Values is null) return false;

        foreach (var name in InstallerQueryService.CachedPackageValueNames)
            if (ValueNamed(key.Values, name, out _) is not null) return false;

        return true;
    }

    /// <summary>What <see cref="OpensNoPackage"/> found for one installation (<see cref="NoPackageReadingOf"/>).</summary>
    /// <param name="Answer">Which of its conditions answered.</param>
    /// <param name="Error">
    /// What <c>MsiSourceListGetInfo</c> answered for the installation's package name, where it was asked.
    /// </param>
    private readonly record struct NoPackageReading(NoPackageAnswer Answer, uint Error = MsiError.Success);

    /// <summary>Which of the conditions of <see cref="OpensNoPackage"/> answered for one installation.</summary>
    private enum NoPackageAnswer
    {
        /// <summary>All of them: it has no package to open.</summary>
        OpensNone,

        /// <summary>None was asked: a per-user installation, or a check without its registry reader.</summary>
        NotAsked,

        /// <summary>
        /// <c>MsiSourceListGetInfo</c> answered the installation's package name, with
        /// <see cref="MsiError.Success"/> or <see cref="MsiError.MoreData"/>.
        /// </summary>
        ListAnswered,

        /// <summary>
        /// <c>MsiSourceListGetInfo</c> answered with an error other than
        /// <see cref="MsiError.BadConfiguration"/>.
        /// </summary>
        OtherAnswer,

        /// <summary>
        /// <c>MsiSourceListGetInfo</c> answered <see cref="MsiError.BadConfiguration"/>, and the
        /// installation's <c>InstallProperties</c> key records a cached package or its
        /// <c>SourceList</c> key is there, or either key would not read.
        /// </summary>
        RegistryDisagrees,
    }

    /// <summary>
    /// The product code the cached package of one installation declares, read by the
    /// code the installation is registered under, with <paramref name="reading"/>
    /// <see cref="CachedPackageReading.Declared"/>. Null, with what went wrong in
    /// <paramref name="reading"/> and in words in <paramref name="detail"/>, where the
    /// record names no package, the value will not read, names no file, or names a file
    /// that does not yield a product code; and for every installation where the check
    /// was built without its file readers, having no way to look.
    /// </summary>
    private string? CodeTheCachedPackageDeclares(
        string registeredCode, string? sid, MsiInstallContext context,
        out CachedPackageReading reading, out string detail)
    {
        if (!ComparesRecordedPackages)
        {
            reading = CachedPackageReading.NoReaders;
            detail = "no way to read a cached package";
            return null;
        }

        var read = InstallerQueryService.ReadProductProperty(
            _msi, registeredCode, sid, context, MsiInstallProperty.LocalPackage);
        if (read.Unreadable)
        {
            reading = CachedPackageReading.PathUnreadable;
            detail = "the cached package's path would not read";
            return null;
        }

        var path = read.Value.TrimEnd('\0');
        if (path.Length == 0)
        {
            reading = CachedPackageReading.NoneRecorded;
            detail = "the installation records no cached package";
            return null;
        }

        if (!_fileSystem!.File.Exists(path))
        {
            reading = CachedPackageReading.NotThere;
            detail = "the cached package is not a file that is there";
            return null;
        }

        var identity = _identityReader.Read(path, isPatch: false, out var readerDetail, out var refusal);
        if (identity is null)
        {
            reading = refusal == PackageReadRefusal.WouldNotRead
                ? CachedPackageReading.WouldNotRead
                : CachedPackageReading.NoProductCode;
            detail = readerDetail.Length != 0 ? readerDetail
                : reading == CachedPackageReading.WouldNotRead ? "the cached package would not read"
                : "the cached package declares no product code";
            return null;
        }

        if (identity.Value.IsPatch || identity.Value.Code.Length == 0)
        {
            reading = CachedPackageReading.NoProductCode;
            detail = "the cached package declares no product code";
            return null;
        }

        reading = CachedPackageReading.Declared;
        detail = string.Empty;
        return identity.Value.Code;
    }

    /// <summary>What an installation's cached package gave (<see cref="CodeTheCachedPackageDeclares"/>).</summary>
    private enum CachedPackageReading
    {
        /// <summary>A product code.</summary>
        Declared,

        /// <summary>Nothing, the check having no file readers to read it with.</summary>
        NoReaders,

        /// <summary>Nothing: its path would not read.</summary>
        PathUnreadable,

        /// <summary>Nothing: the installation records none.</summary>
        NoneRecorded,

        /// <summary>Nothing: its path names no file that is there.</summary>
        NotThere,

        /// <summary>Nothing: the file would not give up its product code (<see cref="PackageReadRefusal.WouldNotRead"/>).</summary>
        WouldNotRead,

        /// <summary>
        /// No product code: the file declares none, or one that is not a well-formed GUID, or
        /// reads as a patch.
        /// </summary>
        NoProductCode,
    }

    /// <summary>What an installation's own record showed (<see cref="WhatItsOwnRecordShows"/>).</summary>
    private enum RecordReading
    {
        /// <summary>An ordinary installation: a package code, and an ordinary <c>InstanceType</c>.</summary>
        Ordinary,

        /// <summary>Not read: a per-user installation not shown to be the running account's.</summary>
        AnotherAccount,

        /// <summary>Its <c>PackageCode</c> did not read as a value.</summary>
        PackageCodeUnanswered,

        /// <summary>Its <c>InstanceType</c> read as a second instance, or did not read.</summary>
        InstanceTypeNotOrdinary,
    }

    /// <summary>
    /// Which step answered for one installation not ruled out as a second copy
    /// (<see cref="AddSecondCopyPackages"/>): <see cref="Seen"/> where its packages can all be
    /// seen, and otherwise the step that could not see one.
    /// </summary>
    private enum SecondCopyReading
    {
        /// <summary>
        /// Its cached package, where it records one, and the packages its sources name can all be
        /// seen, or it records no cached package and has no package to open at all.
        /// </summary>
        Seen,

        /// <summary>Not looked for, the check having no file readers.</summary>
        NoReaders,

        /// <summary>Its cached package's path would not read.</summary>
        PathUnreadable,

        /// <summary>
        /// It records no cached package, and a source it names could not be ruled out, it not
        /// being shown to have no package to open.
        /// </summary>
        NoneRecorded,

        /// <summary>Its cached package's path names no file that is there.</summary>
        NotThere,

        /// <summary>Its cached package's volume and file ID would not read.</summary>
        WouldNotIdentify,

        /// <summary>Its cached package would not give up its product code.</summary>
        WouldNotRead,

        /// <summary>
        /// Its cached package declares no product code, or one that is not a well-formed GUID,
        /// or reads as a patch.
        /// </summary>
        NoProductCode,

        /// <summary>It is in the per-user unmanaged context, whose source list is not read.</summary>
        PerUserUnmanaged,

        /// <summary>A package its sources name is under a root given up for the pass.</summary>
        SourcesGivenUp,

        /// <summary>
        /// Its sources could not be ruled out for any other reason, a check with no way to read
        /// them included (<see cref="SourceRefusal.NotRuledOut"/>).
        /// </summary>
        SourceNotRuledOut,
    }

    /// <summary>
    /// Which kind of check answered false in <see cref="AddSourcePackages"/>, or
    /// <see cref="None"/> where it answered true.
    /// </summary>
    private enum SourceRefusal
    {
        /// <summary>Every package the sources name was read, or left to be read by name.</summary>
        None,

        /// <summary>The per-user unmanaged context, whose source list is not read.</summary>
        PerUserUnmanaged,

        /// <summary>A package read refused for a root given up for the pass.</summary>
        GivenUp,

        /// <summary>
        /// Any other check: the check has no way to read the sources, or the package name, the
        /// source list or the installed-from folder would not read, held a form that is not
        /// compared or differed from what the registry holds, or a package would not identify
        /// or could be a file in the Installer folder.
        /// </summary>
        NotRuledOut,
    }

    /// <summary>
    /// The counts of a <see cref="CachedPackageCensus"/>, taken one installation at a time by
    /// <see cref="LinksOf"/> and <see cref="PackagesSecondCopiesOpen"/>, and one file at a time
    /// by <see cref="Settle"/>. An installation a check without its file readers could not look
    /// at is counted nowhere, so every installation counted as setting the hold is in exactly
    /// one count of what its cached package gave and one of what its record showed.
    /// </summary>
    private sealed class CensusTally
    {
        private readonly int[] _byReading = new int[Enum.GetValues<CachedPackageReading>().Length];
        private readonly int[] _byRecord = new int[Enum.GetValues<RecordReading>().Length];
        private readonly int[] _bySecondCopyReading = new int[Enum.GetValues<SecondCopyReading>().Length];
        private int _listedChecked;
        private int _opensNoPackage;
        private int _releasedBySources;
        private int _keptNoneRecordedOtherAnswer;
        private int _keptNoneRecordedRegistryDisagrees;
        private int _keptNoneRecordedSourcesNotRuledOut;
        private int _perMachine;
        private int _unruledChecked;
        private int _unseenPerMachine;
        private int _unseenByNameFiles;

        /// <summary>An installation whose cached package declares a product code.</summary>
        internal void Declared() => _listedChecked++;

        /// <summary>
        /// An installation that records no cached package and has no package to open at all
        /// (<see cref="OpensNoPackage"/>), whatever its record shows, so it does not set the
        /// hold. Its record is not read.
        /// </summary>
        internal void OpensNoPackage()
        {
            _listedChecked++;
            _opensNoPackage++;
        }

        /// <summary>
        /// An installation that records no cached package, has a package to open and a record
        /// that does not show an ordinary installation, and whose sources name packages that
        /// can all be seen, so it does not set the hold.
        /// </summary>
        internal void ReleasedBySources()
        {
            _listedChecked++;
            _releasedBySources++;
        }

        /// <summary>
        /// An installation that records no cached package and sets the hold, its sources not
        /// ruled out, by what <paramref name="answer"/> shows kept it: an answer of the
        /// source-list API other than <see cref="MsiError.BadConfiguration"/> or a package name,
        /// the registry disagreeing with that answer, or otherwise its sources. It is counted by
        /// what its cached package gave and its record showed as well
        /// (<see cref="Undeclared"/>).
        /// </summary>
        internal void NoneRecordedKept(NoPackageAnswer answer)
        {
            switch (answer)
            {
                case NoPackageAnswer.OtherAnswer:
                    _keptNoneRecordedOtherAnswer++;
                    break;
                case NoPackageAnswer.RegistryDisagrees:
                    _keptNoneRecordedRegistryDisagrees++;
                    break;
                default:
                    _keptNoneRecordedSourcesNotRuledOut++;
                    break;
            }
        }

        /// <summary>
        /// An installation whose cached package gave <paramref name="reading"/> rather than a
        /// product code, and whose own record showed <paramref name="record"/>. Unless the check
        /// has no file readers, every one is counted as checked and by what its record showed,
        /// and only one that sets the hold is counted by what its cached package gave and by its
        /// context.
        /// </summary>
        internal void Undeclared(CachedPackageReading reading, RecordReading record, MsiInstallContext context)
        {
            if (reading == CachedPackageReading.NoReaders) return;

            _listedChecked++;
            _byRecord[(int)record]++;
            if (record == RecordReading.Ordinary) return;

            _byReading[(int)reading]++;
            if (context == MsiInstallContext.Machine) _perMachine++;
        }

        /// <summary>
        /// An installation not ruled out as a second copy, whose packages gave
        /// <paramref name="reading"/>. Unless the check has no file readers, every one is counted
        /// as checked, and one whose packages could not all be seen by the step that could not
        /// see one and by its context.
        /// </summary>
        internal void SecondCopyRead(SecondCopyReading reading, MsiInstallContext context)
        {
            if (reading == SecondCopyReading.NoReaders) return;

            _unruledChecked++;
            if (reading == SecondCopyReading.Seen) return;

            _bySecondCopyReading[(int)reading]++;
            if (context == MsiInstallContext.Machine) _unseenPerMachine++;
        }

        /// <summary>
        /// A file held back because a package in a folder on the network, which a second copy's
        /// sources name, or those of an installation released by its sources, and which the file
        /// could be by its name, could not be ruled out.
        /// </summary>
        internal void UnseenByName() => _unseenByNameFiles++;

        /// <summary>The counts taken.</summary>
        internal CachedPackageCensus Census() => new(
            ListedChecked: _listedChecked,
            KeptPathUnreadable: _byReading[(int)CachedPackageReading.PathUnreadable],
            KeptNoneRecorded: _byReading[(int)CachedPackageReading.NoneRecorded],
            KeptNotThere: _byReading[(int)CachedPackageReading.NotThere],
            KeptWouldNotRead: _byReading[(int)CachedPackageReading.WouldNotRead],
            KeptNoProductCode: _byReading[(int)CachedPackageReading.NoProductCode],
            KeptAnotherAccount: _byRecord[(int)RecordReading.AnotherAccount],
            KeptPackageCodeUnanswered: _byRecord[(int)RecordReading.PackageCodeUnanswered],
            KeptInstanceTypeNotOrdinary: _byRecord[(int)RecordReading.InstanceTypeNotOrdinary],
            KeptPerMachine: _perMachine,
            ReleasedOrdinary: _byRecord[(int)RecordReading.Ordinary],
            UnruledChecked: _unruledChecked,
            UnseenPathUnreadable: _bySecondCopyReading[(int)SecondCopyReading.PathUnreadable],
            UnseenNoneRecorded: _bySecondCopyReading[(int)SecondCopyReading.NoneRecorded],
            UnseenNotThere: _bySecondCopyReading[(int)SecondCopyReading.NotThere],
            UnseenWouldNotIdentify: _bySecondCopyReading[(int)SecondCopyReading.WouldNotIdentify],
            UnseenWouldNotRead: _bySecondCopyReading[(int)SecondCopyReading.WouldNotRead],
            UnseenNoProductCode: _bySecondCopyReading[(int)SecondCopyReading.NoProductCode],
            UnseenPerUserUnmanaged: _bySecondCopyReading[(int)SecondCopyReading.PerUserUnmanaged],
            UnseenSourcesGivenUp: _bySecondCopyReading[(int)SecondCopyReading.SourcesGivenUp],
            UnseenSourceNotRuledOut: _bySecondCopyReading[(int)SecondCopyReading.SourceNotRuledOut],
            UnseenPerMachine: _unseenPerMachine,
            UnseenByNameFiles: _unseenByNameFiles,
            ReleasedOpensNoPackage: _opensNoPackage,
            ReleasedBySources: _releasedBySources,
            KeptNoneRecordedOtherAnswer: _keptNoneRecordedOtherAnswer,
            KeptNoneRecordedRegistryDisagrees: _keptNoneRecordedRegistryDisagrees,
            KeptNoneRecordedSourcesNotRuledOut: _keptNoneRecordedSourcesNotRuledOut);
    }

    /// <summary>Whether two spellings name one product code.</summary>
    private static bool SameCode(string a, string b) =>
        Guid.TryParse(a, out var first) && Guid.TryParse(b, out var second)
            ? first == second
            : string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The identity of every file the installations answering for <paramref name="code"/>
    /// open as their package: the cached package each installation records, and the
    /// original package at each folder on its source list that holds one, a package in a
    /// folder on the network being left to be read for each candidate whose name it could
    /// be (<see cref="OpenedPackages.ByName"/>). Null where any package read here cannot
    /// be seen.
    ///
    /// EACH INSTALLATION IS READ BY THE CODE IT IS REGISTERED UNDER, which for a second
    /// copy is not <paramref name="code"/>, and its cached package has to declare
    /// <paramref name="code"/> all the same.
    ///
    /// AN INSTALLATION RECORDING NO CACHED PACKAGE ADDS THE PACKAGES ITS SOURCES NAME, its
    /// sources being all it can open a package from, and nothing where it has no package to
    /// open at all (<see cref="OpensNoPackage"/>). Every other installation of the code is read
    /// as well.
    ///
    /// NULL IS THE ANSWER THAT KEEPS THE FILE, and every way an installation's package
    /// can fail to be seen reaches it: a <c>LocalPackage</c> read that failed, a value that
    /// names nothing, names a folder, will not open to an identity, or names a file that
    /// does not declare <paramref name="code"/>; and any source the check cannot rule out,
    /// which <see cref="AddSourcePackages"/> sets out. One such installation is enough,
    /// because its package is the one this candidate could be. <paramref name="givenUp"/> is
    /// the root whose give-up refused the read that made it null
    /// (<see cref="ReadSourcePackage"/>), and null otherwise.
    /// </summary>
    private OpenedPackages? PackagesOpenedBy(
        string code,
        IReadOnlyList<(string RegisteredCode, string? Sid, MsiInstallContext Context)> installations,
        PassAnswers pass,
        Func<string, bool?>? namesAFileInInstallerFolder,
        out string? givenUp)
    {
        givenUp = null;
        if (_fileIdentities is null || _fileSystem is null) return null;

        var identities = new List<FileIdentity>(installations.Count);
        var byName = new List<NetworkPackage>();
        foreach (var (registeredCode, sid, context) in installations)
        {
            var read = InstallerQueryService.ReadProductProperty(
                _msi, registeredCode, sid, context, MsiInstallProperty.LocalPackage);
            if (read.Unreadable) return null;

            // An installation recording no cached package adds the packages its sources name,
            // and nothing where it has no package to open at all (OpensNoPackage).
            var path = read.Value.TrimEnd('\0');
            if (path.Length == 0)
            {
                if (OpensNoPackage(registeredCode, sid, context, pass)) continue;
                if (!AddSourcePackages(
                        registeredCode, sid, context, pass, namesAFileInInstallerFolder, identities, byName, out givenUp, out _))
                    return null;
                continue;
            }

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

            if (!AddSourcePackages(
                    registeredCode, sid, context, pass, namesAFileInInstallerFolder, identities, byName, out givenUp, out _))
                return null;
        }

        return new OpenedPackages(identities, byName);
    }

    /// <summary>
    /// Adds to <paramref name="opened"/> the identity of the original package at each
    /// folder one installation of a product has as a source, and answers false where
    /// those sources cannot be ruled out. A package in a folder on the network is not
    /// read here and goes into <paramref name="byName"/> (<see cref="ComparedByName"/>).
    ///
    /// WHAT IT COMPARES, AND WHAT KEEPS THE COPY INSTEAD. When Windows Installer needs a
    /// product's original package rather than its cached copy, a repair among other
    /// things, it tries the source it used last and then the sources on the product's
    /// source list, network folders, media and URLs, looking in each for the file named
    /// by <c>PackageName</c>. The folders compared are the network sources, the source
    /// used last where it is a network source (<see cref="SourceUsedLastOf"/>), and the
    /// folder the installation records as its <c>InstallSource</c>, the one its package
    /// was installed from. Windows Installer puts that folder on the list when it
    /// installs the product, and the list can change afterwards, so the folder is
    /// compared whether or not the list still holds it (<see cref="InstallSourceOf"/>).
    /// The source used last is compared whether or not the list holds it in the same
    /// spelling, case included.
    /// Everything else on the list is read to decide whether the copy is kept, and it is
    /// kept for any of these:
    /// - A URL ENTRY, WHATEVER ITS SCHEME. A web address can name this PC as well as any
    ///   other. No URL is compared, and one on the list keeps the copy.
    /// - AN ENTRY NAMING AN ENVIRONMENT VARIABLE, one holding a '%'. Whoever reads the
    ///   entry may expand the variable in its own environment, so the folder Windows
    ///   Installer looks in need not be the text compared.
    /// - AN ENTRY STARTING NEITHER WITH A DRIVE LETTER, A ':' AND A '\' NOR WITH TWO '\'
    ///   (<see cref="IsOnADriveOrAShare"/>). Only those two forms are compared. A relative
    ///   path is read against the working folder of whoever reads it, and Windows
    ///   Installer's service need not have this process's, so the folder Windows Installer
    ///   looks in need not be the one compared.
    /// - A LIST THE REGISTRY HOLDS DIFFERENTLY. The list the API returns is compared
    ///   with the key it is held in (<see cref="SourceListKeyPath"/>), entry by entry
    ///   (<see cref="IsTheList"/>), so an entry the API does not return, past a gap in
    ///   the numbering or anywhere else, keeps the copy. So does a package name the key
    ///   holds otherwise than the API answers it, or holds as a REG_EXPAND_SZ
    ///   (<see cref="HoldsThePackageName"/>).
    /// - A SOURCE USED LAST THAT IS NOT A NETWORK SOURCE, it being the one Windows
    ///   Installer tries first, or that the registry holds differently
    ///   (<see cref="SourceUsedLastOf"/>).
    /// - A MEDIA PACKAGE PATH, the package's path on the installation media
    ///   (<see cref="NamesNoMediaPackagePath"/>). No media source is compared, and a list
    ///   naming a path on one keeps the copy.
    ///
    /// A PACKAGE IN A FOLDER ON THE NETWORK IS READ ONLY FOR A CANDIDATE WHOSE NAME IT
    /// COULD BE (<see cref="WithPackagesItCouldBe"/>). Windows Installer looks in each
    /// source folder for the file named by <c>PackageName</c> and for no other. A folder on
    /// the network can be this PC's Installer folder reached through a share, and the file
    /// found there by that name is the one with that name or that short name
    /// (<see cref="CandidateNames.CouldBe"/>). A package in a local folder is read here for
    /// every candidate whatever its name, since a symbolic link there, which Windows
    /// follows by default, can carry the package's name and point at any file. So is a
    /// package on the network while Windows is set to follow a symbolic link reached
    /// through a network path to this PC or to another network path
    /// (<see cref="RemoteLinksCanBeFollowed"/>).
    ///
    /// A PACKAGE ON A LOCAL DRIVE IS COMPARED BY ITS IDENTITY ALONE, in the Installer folder
    /// as in any other folder (<see cref="IsLocalDrive"/>). The identity read follows a link,
    /// a short name or another spelling of the folder to the file the path opens, so the
    /// candidate that opens as that file is the one kept, and every other copy of the
    /// product goes on to the rest of the check. A package read here on any other root is
    /// first put to the Installer-folder test, and one that would be a file directly in the
    /// Installer folder keeps every copy.
    ///
    /// FALSE, WHICH KEEPS THE FILE, for: no way to compare against the Installer folder
    /// or to read the registry; a per-user-unmanaged account and context, whose list is
    /// not read; a package name, a source list or a property of the list that will not
    /// read; an empty package name, or one holding a '\', a '/', a ':', a '%' or a null;
    /// each of the six above; a source entry holding a null; a source used last that
    /// <see cref="SourceUsedLastOf"/> answers null for; an <c>InstallSource</c> that
    /// <see cref="InstallSourceOf"/> answers null for; and, for a package read
    /// here, one not on a local drive that would be a file directly in the Installer
    /// folder or where that cannot be established, one that exists and will not identify,
    /// one whose read has not answered within the time limit, and one under a drive, a share
    /// or any other root (<see cref="RootOf"/>) given up for the pass
    /// (<see cref="ReadSourcePackage"/>). A source package that is not there is skipped,
    /// being no file.
    ///
    /// Where false is a package read refused for a root given up for the pass,
    /// <paramref name="givenUp"/> is that root (<see cref="ReadSourcePackage"/>); for every
    /// other answer it is null. <paramref name="refusal"/> says which kind of check answered
    /// false, and is <see cref="SourceRefusal.None"/> where the answer is true.
    /// </summary>
    private bool AddSourcePackages(
        string code,
        string? sid,
        MsiInstallContext context,
        PassAnswers pass,
        Func<string, bool?>? namesAFileInInstallerFolder,
        List<FileIdentity> opened,
        List<NetworkPackage> byName,
        out string? givenUp,
        out SourceRefusal refusal)
    {
        givenUp = null;
        refusal = SourceRefusal.NotRuledOut;
        if (namesAFileInInstallerFolder is null || _fileIdentities is null || _registry is null) return false;

        // A PER-USER-UNMANAGED SOURCE LIST IS NOT READ, IN ANY ACCOUNT, AND THE COPY IS
        // KEPT. Microsoft documents that an administrator cannot enumerate another
        // user's per-user-unmanaged installations, and not what the call answers in their
        // place, so a list read there with no network source on it cannot be told from a
        // list that was never read. So no answer from such a list is used, and the copy
        // is kept whatever the list would say. The account this process runs as is not
        // compared with the installation's, so its own per-user-unmanaged installations
        // are kept the same way.
        if (context == MsiInstallContext.UserUnmanaged)
        {
            refusal = SourceRefusal.PerUserUnmanaged;
            return false;
        }

        var name = InstallerQueryService.ReadProductProperty(
            _msi, code, sid, context, MsiInstallProperty.PackageName);
        if (name.Unreadable) return false;

        var packageName = name.Value.TrimEnd('\0');
        if (packageName.Length == 0) return false;

        // A package name holding a '\', a '/' or a ':' names a folder, a drive or a stream
        // as well as a file, so the file it opens need not be the one its last part spells,
        // and it keeps the copy. On a media source with no media package path such a name
        // reaches below the root of the volume, and no media source is compared. A package
        // name holding a '%' names a variable, which whoever reads the name may expand in its
        // own environment, and one holding a null is cut short at the null wherever it is read
        // as a path. Both keep the copy as well.
        if (packageName.IndexOfAny(['\\', '/', ':', '%', '\0']) >= 0) return false;

        var sources = SourcesOf(code, sid, context, MsiSourceListOptions.Network);
        if (sources is null) return false;

        var urls = SourcesOf(code, sid, context, MsiSourceListOptions.Url);
        if (urls is null || urls.Count > 0) return false;

        // An entry naming an environment variable keeps the copy, and so does one holding
        // a null, which would cut the entry short wherever it is read as a path, and one of
        // a form this check does not compare.
        foreach (var entry in sources)
            if (entry.Contains('%') || entry.Contains('\0') || !IsOnADriveOrAShare(entry)) return false;

        var path = SourceListKeyPath(code, sid, context);
        if (path is null) return false;

        var sourceList = _registry.LocalMachineValues(path);
        if (sourceList.Presence != RegistryKeyPresence.Present || sourceList.Values is null) return false;

        // The name the key holds has to be the one the API answered, text for text, so
        // a '\', a '/', a ':', a '%' or a null keeps the copy whichever of the two holds it.
        // Loosen that comparison and the stored name needs the character test of its own.
        if (!HoldsThePackageName(sourceList.Values, packageName)) return false;

        if (!IsTheList(_registry.LocalMachineValues(path + @"\Net"), sources)
            || !IsTheList(_registry.LocalMachineValues(path + @"\URL"), urls)
            || !NamesNoMediaPackagePath(code, sid, context, _registry.LocalMachineValues(path + @"\Media")))
            return false;

        var usedLast = SourceUsedLastOf(code, sid, context, sourceList.Values);
        if (usedLast is null) return false;

        var installSource = InstallSourceOf(_registry, code, sid, context);
        if (installSource is null) return false;

        // The source used last and the InstallSource join the folders compared, each
        // unless the same text is already among them. Text differing only in case is a
        // folder of its own here: a folder can be case sensitive, and only the filesystem
        // can say which file each spelling opens.
        var folders = new List<string>(sources);
        if (usedLast.Length > 0 && !folders.Contains(usedLast, StringComparer.Ordinal)) folders.Add(usedLast);
        if (installSource.Length > 0 && !folders.Contains(installSource, StringComparer.Ordinal))
            folders.Add(installSource);

        foreach (var folder in folders)
        {
            // Joined with a backslash by hand rather than with Path.Combine, whose
            // separator is the host's.
            var package = folder.EndsWith('\\') ? folder + packageName : folder + '\\' + packageName;

            if (ComparedByName(package, _registry, pass))
            {
                byName.Add(new NetworkPackage(package, packageName));
                continue;
            }

            var inInstallerFolder = IsLocalDrive(RootOf(package), pass) ? null : namesAFileInInstallerFolder;
            if (!ReadSourcePackage(package, pass, inInstallerFolder, out var identity, out givenUp))
            {
                if (givenUp is not null) refusal = SourceRefusal.GivenUp;
                return false;
            }

            if (identity is { } read) opened.Add(read);
        }

        refusal = SourceRefusal.None;
        return true;
    }

    /// <summary>
    /// <paramref name="identities"/>, the packages an installation opens that were read for
    /// every candidate, with the identity of each package in <paramref name="byName"/> this
    /// candidate could be by its name (<see cref="CandidateNames.CouldBe"/>); null where one
    /// of those cannot be ruled out, which keeps the candidate. A package it could not be by
    /// its name is not read for it.
    ///
    /// Each package is read once per pass (<see cref="ReadSourcePackage"/>), and its answer is
    /// kept for every candidate after it, with the root, where there is one, whose give-up
    /// refused it: <paramref name="givenUp"/> where that answer makes this null, and null
    /// otherwise.
    /// </summary>
    private IReadOnlyList<FileIdentity>? WithPackagesItCouldBe(
        CandidateNames candidate,
        IReadOnlyList<FileIdentity> identities,
        IReadOnlyList<NetworkPackage> byName,
        PassAnswers pass,
        Func<string, bool?>? namesAFileInInstallerFolder,
        out string? givenUp)
    {
        givenUp = null;
        if (byName.Count == 0) return identities;
        if (namesAFileInInstallerFolder is null) return null;

        List<FileIdentity>? opened = null;
        foreach (var package in byName)
        {
            pass.CancellationToken.ThrowIfCancellationRequested();
            if (!candidate.CouldBe(package.Name)) continue;

            if (!pass.NetworkPackageReads.TryGetValue(package.Path, out var read))
            {
                var settled = ReadSourcePackage(
                    package.Path, pass, namesAFileInInstallerFolder, out var identity, out var refusedFor);
                pass.NetworkPackageReads[package.Path] = read = (settled, identity, refusedFor);
            }

            if (!read.Settled)
            {
                givenUp = read.GivenUp;
                return null;
            }

            if (read.Identity is { } found) (opened ??= [.. identities]).Add(found);
        }

        return opened ?? identities;
    }

    /// <summary>
    /// Reads the package at <paramref name="package"/>, a path built from a source folder,
    /// for <see cref="AddSourcePackages"/>, or for <see cref="WithPackagesItCouldBe"/> where
    /// the folder is on the network: true with its identity where it opens, true with null
    /// where no file is there, and false where the package cannot be ruled out.
    ///
    /// FALSE for a package that exists and will not identify; a read that has not answered
    /// within <see cref="SourceFolderTimeLimit"/>; a package under a root given up for the
    /// pass, which is not read; and, where <paramref name="namesAFileInInstallerFolder"/> is
    /// given, a package that would be a file directly in the Installer folder, or where that
    /// cannot be established. It is given for every package but one on a local drive, which
    /// is compared by its identity alone (<see cref="AddSourcePackages"/>).
    ///
    /// WHAT FALSE KEEPS IS WHAT THE PACKAGE WAS READ FOR. Read for the installations answering
    /// for a product code (<see cref="PackagesOpenedBy"/>), it keeps every candidate declaring
    /// that code. Read for the installations not ruled out as second copies
    /// (<see cref="PackagesSecondCopiesOpen"/>), or for an installation recording no cached
    /// package whose record does not show an ordinary installation (<see cref="LinksOf"/>), it
    /// keeps every installation package the answer about its own product would let through.
    /// Read for one candidate whose name it could be (<see cref="WithPackagesItCouldBe"/>), it
    /// keeps that candidate. A package on a local drive is never left to be read by name
    /// (<see cref="ComparedByName"/>), so it is read for one of the others.
    ///
    /// THE READ IS WAITED FOR UP TO THE TIME LIMIT (<see cref="AnswersWithin"/>). A source
    /// folder can be on a server that does not answer, or on a drive that does not, and an
    /// open there waits until Windows gives up; the open itself takes no time limit.
    ///
    /// A ROOT THAT DOES NOT ANSWER COSTS A PASS ONE WAIT. The root, a share, a drive or a path
    /// of any other form (<see cref="RootOf"/>), is given up for the rest of the pass once a
    /// read under it has not answered within the limit, or has answered false only after
    /// waiting longer than <see cref="SourceFolderSlowFailure"/>. Such a read waited on the
    /// root itself, as one on a server that Windows gives up on does. Every later package
    /// under a root given up answers false without being read, so no further read is started
    /// there. A drive whose kind has not answered within the limit is given up the same way
    /// (<see cref="KindOf"/>), and so is a root the pass's caller has stopped waiting for
    /// (<see cref="PassAnswers.StopFor"/>).
    ///
    /// READS THAT FAIL ADD UP. A read that answers false within
    /// <see cref="SourceFolderSlowFailure"/> adds the time it took to its root's total of
    /// reads that fail (<see cref="PassAnswers.TimeFailing"/>), however short, and the root is
    /// given up the same way once that total passes <see cref="SourceFolderFailedWaitBudget"/>.
    /// A read that fails at once adds next to nothing, so the total is the time actually spent
    /// on reads that failed.
    ///
    /// AND SO DO ALL THE READS UNDER A ROOT, WHATEVER EACH ANSWERED. Every read adds the time
    /// it took to a second total (<see cref="PassAnswers.TimeReading"/>), and the root is given
    /// up the same way once that total passes <see cref="SourceFolderReadBudget"/>
    /// (<see cref="AnswersWithin"/>). A read that opens its package, or finds no file there,
    /// adds to this total and not to the first, so a drive or a share slow to answer every
    /// read has its packages read one after another until those reads add up past the budget,
    /// and each wait is told to the pass's caller, which can stop waiting for the root.
    /// Cancelling the pass ends the wait at once.
    ///
    /// WHERE FALSE COMES FROM THE ROOT BEING GIVEN UP, <paramref name="givenUp"/> IS THE ROOT:
    /// for a package under a root already given up, a read that has not answered within the
    /// time limit, a read the caller's stop refused or ended before it answered, and a read
    /// that answered false only after waiting longer than <see cref="SourceFolderSlowFailure"/>,
    /// which waited on the root itself. For every other answer it is null. A read that answers
    /// false sooner gave its own answer, and so did the read that takes its root's reads past
    /// either total.
    /// </summary>
    private bool ReadSourcePackage(
        string package,
        PassAnswers pass,
        Func<string, bool?>? namesAFileInInstallerFolder,
        out FileIdentity? identity,
        out string? givenUp)
    {
        identity = null;
        givenUp = null;

        var root = RootOf(package);
        if (pass.IsGivenUp(root))
        {
            givenUp = root;
            return false;
        }

        var identities = _fileIdentities!;
        var answered = AnswersWithin(
            () =>
            {
                // Where the test is given, a package that would be a file directly in the
                // Installer folder, or where that is not established, cannot be ruled out.
                if (namesAFileInInstallerFolder is not null && namesAFileInInstallerFolder(package) is not false)
                    return (false, null);

                return identities.ReadOutcome(package, out var read) switch
                {
                    FileIdentityRead.Read => (true, read),
                    FileIdentityRead.NamesNothing => (true, (FileIdentity?)null),
                    _ => (false, null),
                };
            },
            root,
            pass,
            out (bool Settled, FileIdentity? Identity) answer,
            out var took);

        if (!answered)
        {
            givenUp = root;
            return false;
        }

        if (!answer.Settled)
        {
            if (took > SourceFolderSlowFailure)
            {
                pass.GiveUp(root, SourceRootGiveUpRoute.SlowFailure);
                givenUp = root;
                return false;
            }

            var failing = pass.TimeFailing.GetValueOrDefault(root) + took;
            pass.TimeFailing[root] = failing;
            if (failing > SourceFolderFailedWaitBudget) pass.GiveUp(root, SourceRootGiveUpRoute.FailedReadsAddUp);
        }

        identity = answer.Identity;
        return answer.Settled;
    }

    /// <summary>
    /// Runs <paramref name="read"/>, a read under <paramref name="root"/>, on a thread of its
    /// own and waits for it up to <see cref="SourceFolderTimeLimit"/>: true with its answer
    /// where it answered in time, false where it did not, and in <paramref name="took"/> how
    /// long it was waited for. A read that answers after the limit finishes on its own thread
    /// and its answer is not used. Cancelling ends the wait at once.
    ///
    /// THE ROOT IS GIVEN UP FOR THE PASS (<see cref="PassAnswers.GiveUp"/>) where the caller has
    /// stopped waiting for it, where the read has not answered in time, and where the reads
    /// under the root in this pass, this one among them, have taken longer than
    /// <see cref="SourceFolderReadBudget"/> between them, whatever each answered
    /// (<see cref="PassAnswers.TimeReading"/>). The answer of a read that took its root past the
    /// budget is still used. What a package read answered, and how long it took, can also give
    /// its root up in <see cref="ReadSourcePackage"/>, by a route that outranks the budget's.
    ///
    /// A READ STILL WAITING AFTER <see cref="SourceFolderWaitThreshold"/> IS TOLD TO THE PASS'S
    /// CALLER (<see cref="PassAnswers.WaitingOn"/>), as a <see cref="SourceFolderWait"/> naming
    /// the root, and null is told when the wait ends, answered or not. A wait that cancelling
    /// ends is not told null: whoever shows the wait is already showing the cancel.
    ///
    /// THE CALLER CAN STOP WAITING FOR THE ROOT (<see cref="PassAnswers.StopFor"/>), through that
    /// wait or any other it was told of under the root in this pass, and the root is then given
    /// up for the pass by the stop's own route, which outranks every other the read meets
    /// (<see cref="SourceRootGiveUpRoute.StoppedWaiting"/>). A read waiting there ends at once,
    /// and its answer is used only where it has already come in. No read is started there after
    /// the stop, and a read the stop ends within the threshold is not told.
    /// </summary>
    private bool AnswersWithin<T>(Func<T> read, string root, PassAnswers pass, out T value, out TimeSpan took)
    {
        value = default!;
        took = TimeSpan.Zero;

        var stopped = pass.Stopped(root);
        if (stopped.IsCompleted)
        {
            pass.GiveUp(stopped.Result, SourceRootGiveUpRoute.StoppedWaiting);
            return false;
        }

        var answered = new TaskCompletionSource<(T Value, ExceptionDispatchInfo? Fault)>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        // A background thread, so a read still waiting when the app closes does not keep
        // the process open, and not a thread-pool one, which a read that does not answer
        // would hold until Windows gives up. Nothing escapes the thread: a throw is carried
        // back and thrown again on the calling thread, as it would be from a read made
        // there.
        var reader = new Thread(() =>
        {
            try
            {
                answered.TrySetResult((read(), null));
            }
            catch (Exception ex)
            {
                answered.TrySetResult((default!, ExceptionDispatchInfo.Capture(ex)));
            }
        })
        {
            IsBackground = true,
            Name = "Source package read",
        };
        var started = Clock.GetTimestamp();
        reader.Start();

        // True where the read answers, or the caller stops waiting for the root, within the
        // timeout. Cancelling the pass throws out of the wait, so the end of a wait it ends is
        // not told.
        Task[] answerOrStop = [answered.Task, stopped];
        bool EndsWithin(TimeSpan timeout) =>
            Task.WaitAny(answerOrStop, WholeMilliseconds(timeout), pass.CancellationToken) >= 0;

        var notice = SourceFolderWaitThreshold < SourceFolderTimeLimit ? SourceFolderWaitThreshold : SourceFolderTimeLimit;
        var ended = EndsWithin(notice);
        if (!ended && !stopped.IsCompleted)
        {
            pass.WaitCount++;
            pass.WaitingOn?.Invoke(new SourceFolderWait(root, pass.StopFor(root)));
            ended = EndsWithin(SourceFolderTimeLimit - notice);
            pass.WaitingOn?.Invoke(null);
        }

        // Where the answer and the stop have both come in, the answer is used.
        var inTime = ended && answered.Task.IsCompleted;

        took = Clock.GetElapsedTime(started);

        var reading = pass.TimeReading.GetValueOrDefault(root) + took;
        pass.TimeReading[root] = reading;

        // A read the stop ended has not answered either, and the stop's route outranks the
        // time limit's (PassAnswers.GiveUp).
        if (stopped.IsCompleted) pass.GiveUp(stopped.Result, SourceRootGiveUpRoute.StoppedWaiting);
        if (!inTime) pass.GiveUp(root, SourceRootGiveUpRoute.NoAnswer);
        if (reading > SourceFolderReadBudget) pass.GiveUp(root, SourceRootGiveUpRoute.ReadsAddUp);

        if (!inTime) return false;

        var (result, fault) = answered.Task.Result;
        fault?.Throw();
        value = result;
        return true;
    }

    /// <summary>
    /// <paramref name="timeout"/> in whole milliseconds, the unit
    /// <see cref="Task.WaitAny(Task[], int, CancellationToken)"/> takes, held between nought and
    /// the longest wait that can be given.
    /// </summary>
    private static int WholeMilliseconds(TimeSpan timeout) => (int)Math.Clamp(timeout.TotalMilliseconds, 0, int.MaxValue);

    /// <summary>
    /// The part of <paramref name="path"/> a server or a drive answers for: <c>\\server\share</c>
    /// for a UNC path and the drive letter with its colon for a path on a drive, read through
    /// a <c>\\?\</c> or <c>\\.\</c> prefix. Any other form is its own root. The pass
    /// compares roots without case.
    /// </summary>
    private static string RootOf(string path)
    {
        if (path.StartsWith(@"\\?\", StringComparison.Ordinal) || path.StartsWith(@"\\.\", StringComparison.Ordinal))
        {
            var rest = path[4..];
            return rest.StartsWith(@"UNC\", StringComparison.OrdinalIgnoreCase)
                ? RootOf(@"\\" + rest[4..])
                : RootOf(rest);
        }

        if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            var server = path.IndexOf('\\', 2);
            var share = server < 0 ? -1 : path.IndexOf('\\', server + 1);
            return share < 0 ? path : path[..share];
        }

        return path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':' ? path[..2] : path;
    }

    /// <summary>
    /// Whether <paramref name="root"/>, from <see cref="RootOf"/>, is a drive letter and its colon.
    /// The lines naming a drive or share the check waited for or gave up tell a drive from a
    /// share or a path of another form by this same test
    /// (<see cref="Helpers.DisplayHelpers.SourceRootName"/>), so a change here changes what they
    /// call a drive as well as which roots the check asks the kind of (<see cref="KindOf"/>).
    /// </summary>
    internal static bool IsDriveLetter(string root) =>
        root.Length == 2 && char.IsAsciiLetter(root[0]) && root[1] == ':';

    /// <summary>
    /// Whether <paramref name="root"/>, from <see cref="RootOf"/>, is a drive letter that
    /// Windows reports as a local drive: fixed, removable, an optical drive or a RAM disk
    /// (<see cref="KindOf"/>). A drive whose kind does not answer within the time limit is
    /// not, and neither is a root of any other form.
    /// </summary>
    private bool IsLocalDrive(string root, PassAnswers pass) =>
        IsDriveLetter(root)
        && KindOf(root, pass) is DriveType.Fixed or DriveType.Removable or DriveType.CDRom or DriveType.Ram;

    /// <summary>
    /// Whether the package at <paramref name="package"/> is left to be read for each
    /// candidate whose name it could be (<see cref="WithPackagesItCouldBe"/>): whether it
    /// is in a folder on the network, and Windows is set to follow no symbolic link
    /// reached through a network path to this PC or to another network path
    /// (<see cref="RemoteLinksCanBeFollowed"/>).
    ///
    /// A FOLDER ON THE NETWORK IS A SHARE OR A NETWORK DRIVE. A share is known by its
    /// spelling, <c>\\server\share</c> in any form <see cref="RootOf"/> reads. A drive letter
    /// is on the network where Windows reports it as a network drive
    /// (<see cref="KindOf"/>); one it reports as anything else is read for every candidate,
    /// and so is a path of any other form. A drive whose kind does not answer within the
    /// time limit is given up for the pass, and its packages are kept without being read.
    /// </summary>
    private bool ComparedByName(string package, IRegistryReader registry, PassAnswers pass)
    {
        var root = RootOf(package);
        var onTheNetwork = root.StartsWith(@"\\", StringComparison.Ordinal)
            || (IsDriveLetter(root) && KindOf(root, pass) == DriveType.Network);

        return onTheNetwork && !RemoteLinksCanBeFollowed(registry, pass);
    }

    /// <summary>
    /// Whether Windows may follow a symbolic link reached through a network path to a
    /// target on this PC or on another network path: whether
    /// <c>SymlinkRemoteToLocalEvaluation</c> or <c>SymlinkRemoteToRemoteEvaluation</c>
    /// turns either on, in this PC's own setting or in the policy that sets it
    /// (<see cref="LinkEvaluationKeys"/>). A value that is not there, and one holding 0,
    /// turn nothing on. Any other number, a value of another type and one that will not
    /// read all answer true. Read once per pass.
    ///
    /// WHILE IT ANSWERS TRUE, A PACKAGE ON THE NETWORK IS READ FOR EVERY CANDIDATE. A
    /// symbolic link on a share can carry a program's package name and point at a file in
    /// this PC's Installer folder, and where Windows follows it, that file is the package
    /// whatever its own name.
    /// </summary>
    private static bool RemoteLinksCanBeFollowed(IRegistryReader registry, PassAnswers pass)
    {
        return pass.RemoteLinksFollowed ??= AnyTurnedOn();

        bool AnyTurnedOn()
        {
            foreach (var key in LinkEvaluationKeys)
                foreach (var value in RemoteLinkEvaluationValues)
                {
                    var setting = registry.LocalMachineDwordValue(key, value);
                    if (setting.State == RegistryDwordState.Absent) continue;
                    if (setting is { State: RegistryDwordState.Read, Value: 0 }) continue;
                    return true;
                }

            return false;
        }
    }

    /// <summary>
    /// The keys <see cref="RemoteLinksCanBeFollowed"/> reads: this PC's own setting, and
    /// the policy "Selectively allow the evaluation of a symbolic link", which holds the
    /// same values under the policies key.
    /// </summary>
    private static readonly string[] LinkEvaluationKeys =
    [
        @"SYSTEM\CurrentControlSet\Control\FileSystem",
        @"SOFTWARE\Policies\Microsoft\Windows\Filesystems\NTFS",
    ];

    /// <summary>
    /// The two values that turn on following a symbolic link reached through a network
    /// path: to a target on this PC, and to a target on another network path.
    /// </summary>
    private static readonly string[] RemoteLinkEvaluationValues =
        ["SymlinkRemoteToLocalEvaluation", "SymlinkRemoteToRemoteEvaluation"];

    /// <summary>
    /// What <see cref="DriveKindOf"/> answers within the time limit for
    /// <paramref name="drive"/>, a drive letter and its colon, or null where it has not
    /// answered by then. Asked once per drive per pass, and waited for as a package read
    /// under the drive is (<see cref="AnswersWithin"/>): a drive whose kind has not answered
    /// is given up for the rest of the pass, so none of its packages is read, and the time
    /// it took to answer counts towards <see cref="SourceFolderReadBudget"/> with the drive's
    /// package reads.
    /// </summary>
    private DriveType? KindOf(string drive, PassAnswers pass)
    {
        if (!pass.DriveKinds.TryGetValue(drive, out var kind))
            pass.DriveKinds[drive] = kind =
                AnswersWithin(() => DriveKindOf(drive), drive, pass, out var answered, out _)
                    ? answered
                    : null;

        return kind;
    }

    /// <summary>
    /// The kind of drive the drive letter <c>X:</c> given is, as Windows reports it to this
    /// process.
    /// </summary>
    internal Func<string, DriveType> DriveKindOf { get; init; } = drive =>
        StorageHelpers.GetDriveKind(drive + @"\");

    /// <summary>
    /// The names the folder's entry for a candidate's path holds: its name, and its 8.3
    /// short name where it has one of its own (<see cref="StorageHelpers.GetNamesInFolder"/>).
    /// Null where Windows does not answer.
    /// </summary>
    internal Func<string, IReadOnlyList<string>?> NamesInFolderOf { get; init; } = StorageHelpers.GetNamesInFolder;

    /// <summary>
    /// How long <see cref="ReadSourcePackage"/> waits for one source folder's package to
    /// answer, and <see cref="KindOf"/> for a drive's kind, before the root is given up for
    /// the pass and what depended on it kept. Shorten it and a drive or a server that is
    /// only slow to answer, a hard disk waking from standby among them, has every copy that
    /// depends on it kept in the pass it is slow in. Lengthen it and a root that does not
    /// answer at all costs each pass that much longer.
    /// </summary>
    internal TimeSpan SourceFolderTimeLimit { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long a source folder's package read can take to answer false before its root is
    /// given up for the pass as well (<see cref="ReadSourcePackage"/>). Raise it and a
    /// server that Windows gives up on sooner is left to be read again, so later packages
    /// under it can wait as long, until the reads that failed there add up to
    /// <see cref="SourceFolderFailedWaitBudget"/>. Lower it and a root that is only slow to
    /// answer that one package will not open is given up with every package under it.
    /// </summary>
    internal TimeSpan SourceFolderSlowFailure { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long, in one pass, the package reads under one root that each answered false can
    /// add up to before the root is given up for the rest of the pass
    /// (<see cref="ReadSourcePackage"/>). Every such read adds the time it took, however
    /// short. A read that opens its package, or finds no file there, adds nothing to this
    /// total, however long it took, and counts towards <see cref="SourceFolderReadBudget"/>
    /// alone. Lower it and a root whose reads fail is given up sooner, so a copy that depends
    /// on a later package there that would have opened is kept; raise it and such a root
    /// costs each pass that much longer, up to <see cref="SourceFolderReadBudget"/>, which
    /// counts these reads too.
    /// </summary>
    internal TimeSpan SourceFolderFailedWaitBudget { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How long, in one pass, all the reads under one root can add up to before the root is
    /// given up for the rest of the pass (<see cref="AnswersWithin"/>). Every read adds the
    /// time it took, whatever it answered and however short, the read asking a drive its kind
    /// among them (<see cref="KindOf"/>). The read that takes the total past still has its
    /// answer used. Lower it and a root slow to answer every read is given up sooner, so more
    /// of the copies that depend on it are kept; raise it and such a root holds each pass that
    /// much longer.
    /// </summary>
    internal TimeSpan SourceFolderReadBudget { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long a read under a root can take before it is a wait, and the pass's caller is
    /// told what it is waiting on (<see cref="AnswersWithin"/>). So a read that answers
    /// promptly is never shown. A read is waited for up to <see cref="SourceFolderTimeLimit"/>
    /// whatever this is. Raise it and a read waits that much longer before the caller is
    /// told; lower it and a read that is only not instant flashes a line on screen.
    /// </summary>
    internal TimeSpan SourceFolderWaitThreshold { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The clock <see cref="AnswersWithin"/> times each read on. Every read adds the time it
    /// took on this clock towards <see cref="SourceFolderReadBudget"/>. A package read that
    /// answers false after longer than <see cref="SourceFolderSlowFailure"/> on this clock
    /// gives its root up, and one that answers false sooner adds the time it took on this
    /// clock towards <see cref="SourceFolderFailedWaitBudget"/> (<see cref="ReadSourcePackage"/>).
    /// The waits themselves are timed by the thread that waits, whatever this clock says.
    /// </summary>
    internal TimeProvider Clock { get; init; } = TimeProvider.System;

    /// <summary>
    /// The entries on one installation's source list, network or URL as
    /// <paramref name="sourceType"/> says, in the order Windows lists them, or null where
    /// the list did not read to its end.
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
    private IReadOnlyList<string>? SourcesOf(string code, string? sid, MsiInstallContext context, uint sourceType)
    {
        var options = MsiSourceListOptions.Product | sourceType;
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
    /// Whether neither the API nor the registry gives an installation's source list a
    /// media package path: the API's answer for
    /// <see cref="MsiInstallProperty.MediaPackagePath"/> and the <c>MediaPackage</c>
    /// value of the list's <c>Media</c> key, <paramref name="media"/>. A key that is not
    /// there names none. A read of the property that fails, a key that will not read and
    /// a value of a type other than a string all answer false, which keeps the file.
    /// </summary>
    private bool NamesNoMediaPackagePath(string code, string? sid, MsiInstallContext context, RegistryKeyValues media)
    {
        var answered = InstallerQueryService.ReadSourceListProperty(
            _msi, code, sid, context, MsiSourceListOptions.Product, MsiInstallProperty.MediaPackagePath);
        if (answered.Unreadable || answered.Value.TrimEnd('\0').Length > 0) return false;

        if (media.Presence == RegistryKeyPresence.Absent) return true;
        if (media.Presence != RegistryKeyPresence.Present || media.Values is null) return false;

        var stored = ValueNamed(media.Values, "MediaPackage", out var count)?.Text;
        return count == 0 || stored is { Length: 0 };
    }

    /// <summary>
    /// The folder Windows Installer used last as the source of one installation of a
    /// product, the one it tries first: the folder; an empty string where the API and the
    /// registry agree that there is none, Windows Installer then going straight to the
    /// list; or null, which keeps the file.
    ///
    /// THE REGISTRY HOLDS IT AS ONE VALUE of the <c>SourceList</c> key,
    /// <paramref name="sourceList"/>: the source's type, its index on the list and its
    /// text, each followed by a ';' but the last. The API answers the type and the text
    /// as two properties. The stored type and text have to be the two the API answers,
    /// and a value that is not there or is empty has to be answered as no source at all.
    ///
    /// THE FOLDER IS NOT LOOKED FOR ON THE LIST. Its package is compared in
    /// <see cref="AddSourcePackages"/> like that of a folder on the list, so whichever
    /// spelling Windows Installer recorded, case included, the file that path opens is the
    /// one compared.
    ///
    /// NULL for: a read through the API that fails; a URL or a media source, or a source
    /// of any other kind; one the registry holds differently; and a folder holding a '%'
    /// or a null, or starting neither with a drive letter, a ':' and a '\' nor with two
    /// '\', for the reasons a list entry like it keeps the file.
    /// </summary>
    private string? SourceUsedLastOf(
        string code,
        string? sid,
        MsiInstallContext context,
        IReadOnlyList<RegistryValue> sourceList)
    {
        var source = InstallerQueryService.ReadSourceListProperty(
            _msi, code, sid, context, MsiSourceListOptions.Product, MsiInstallProperty.LastUsedSource);
        var type = InstallerQueryService.ReadSourceListProperty(
            _msi, code, sid, context, MsiSourceListOptions.Product, MsiInstallProperty.LastUsedType);
        if (source.Unreadable || type.Unreadable) return null;

        var folder = source.Value.TrimEnd('\0');
        var folderType = type.Value.TrimEnd('\0');

        var stored = ValueNamed(sourceList, MsiInstallProperty.LastUsedSource, out var count)?.Text;
        if (count > 0 && stored is null) return null;
        if (string.IsNullOrEmpty(stored)) return folder.Length == 0 && folderType.Length == 0 ? string.Empty : null;

        var parts = stored.Split(';', 3);
        if (parts.Length != 3
            || !string.Equals(parts[0], folderType, StringComparison.Ordinal)
            || !string.Equals(parts[2], folder, StringComparison.Ordinal)
            || folder.Length == 0
            || !string.Equals(folderType, "n", StringComparison.Ordinal))
            return null;

        if (folder.Contains('%') || folder.Contains('\0') || !IsOnADriveOrAShare(folder)) return null;

        return folder;
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

        return IsOnADriveOrAShare(folder) ? folder : null;
    }

    /// <summary>
    /// Whether <paramref name="folder"/> starts with a drive letter, a ':' and a '\', or
    /// with two '\': the two forms of folder this check compares, on a source list, as the
    /// source used last and as an <c>InstallSource</c> alike.
    /// </summary>
    private static bool IsOnADriveOrAShare(string folder) =>
        (folder.Length >= 3 && char.IsAsciiLetter(folder[0]) && folder[1] == ':' && folder[2] == '\\')
        || folder.StartsWith(@"\\", StringComparison.Ordinal);

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
    /// The HKLM path this check reads a product's <c>SourceList</c> key from, in one
    /// account and context. Per machine, <c>SOFTWARE\Classes\Installer\Products</c>, then
    /// the code in its packed form, then <c>SourceList</c>; per user and managed, the
    /// same below
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
    private static string? SourceListKeyPath(string code, string? sid, MsiInstallContext context)
    {
        var packed = InstallerQueryService.PackRegistryCode(code);
        if (packed is null) return null;

        if (context == MsiInstallContext.Machine && sid is null)
            return $@"SOFTWARE\Classes\Installer\Products\{packed}\SourceList";

        if (context == MsiInstallContext.UserManaged && InstallerQueryService.IsAccount(sid))
            return $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Managed\{sid}\Installer\Products\{packed}\SourceList";

        return null;
    }

    /// <summary>
    /// The HKLM path this check reads a product installation's <c>InstallProperties</c>
    /// key from: <c>SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData</c>, then
    /// the account subtree <see cref="InstallerQueryService.UserDataAccount"/> names,
    /// <c>S-1-5-18</c> per machine or the account per user, then <c>Products</c>, the code
    /// in its packed form and <c>InstallProperties</c>. A key not found at this path keeps
    /// the file.
    ///
    /// NULL, WHICH KEEPS THE FILE, per machine with an account, per user without one, and
    /// for a code or an account that will not make a key name. A per-user unmanaged
    /// installation has a path here and never reaches it: <see cref="AddSourcePackages"/>
    /// keeps the file for that context before it reads an <c>InstallSource</c>.
    /// </summary>
    private static string? InstallPropertiesKeyPath(string code, string? sid, MsiInstallContext context)
    {
        var packed = InstallerQueryService.PackRegistryCode(code);
        if (packed is null) return null;

        var account = InstallerQueryService.UserDataAccount(sid, context);

        return account is null
            ? null
            : $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\{account}\Products\{packed}\InstallProperties";
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
    /// it declares, and if so whether this file is shown to be a different file from the
    /// cached copy every registration records.
    /// </summary>
    private DeclaredProductOutcome ScreenPatch(string path, PassAnswers pass, Action<Exception, string>? recordRefusal)
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
            answer = AskAboutPatch(code, targets, pass);
            pass.Patches[key] = answer;
        }

        // As for a product: the registrations are shared by every copy declaring the
        // patch, and whether the copies they record are OTHER files is asked per file.
        return answer.Outcome == DeclaredProductOutcome.DeclaredPatchRegistered
            && answer.RecordedPackages is { } recorded
                ? CompareWithRecorded(path, recorded,
                    DeclaredProductOutcome.DeclaredPatchCachedAsAnotherFile, answer.Outcome)
                : answer.Outcome;
    }

    /// <summary>
    /// What Windows holds for one declared patch, asked once per patch code and target
    /// list per pass: every registration the machine-wide patch enumeration lists for
    /// the code, unioned with every installation of a named target product, and every
    /// installation the caller's enumeration listed, that answers the keyed patch read
    /// with a state or with no value at all.
    ///
    /// THE UNION IS WHY IT IS ALL THREE. The enumeration names a registration against a
    /// product the patch's Template does not list; the keyed read reaches an
    /// installation of a listed product the enumeration does not name; and put to every
    /// installation the caller listed, it reaches one that holds the patch whether or
    /// not the Template names its product or the enumeration lists it. Each can only add
    /// a registration.
    ///
    /// AND ANY OF THEM FAILING KEEPS THE FILE. An enumeration that did not run to its
    /// end, a named product whose installations would not list or were listed without
    /// one the caller's enumeration listed, and an installation that would not answer
    /// the keyed read each leave registrations unfound, and the answer is
    /// <see cref="DeclaredProductOutcome.DeclaredPatchUnestablished"/>. The enumeration
    /// is walked once per pass, so where it fails, every patch copy the pass asks about
    /// is kept.
    /// </summary>
    private DeclarationAnswer AskAboutPatch(string code, IReadOnlyList<string> targets, PassAnswers pass)
    {
        var holders = pass.PatchHolders;
        if (holders is null)
            return new DeclarationAnswer(DeclaredProductOutcome.DeclaredPatchUnestablished, null);

        var registrations = new List<(string ProductCode, string? Sid, MsiInstallContext Context)>();
        if (holders.TryGetValue(code, out var listed)) registrations.AddRange(listed);

        // False where the installation gave no answer that can be used.
        bool AskInstallation(string productCode, string? sid, MsiInstallContext context)
        {
            // Already a registration: the enumeration listed it, and its copy is read
            // below whatever the keyed read would say.
            if (IsListed(registrations, productCode, sid, context)) return true;

            var state = pass.PatchStateOf(code, productCode, sid, context);

            // ONLY THE INSTALLATION ANSWERING THAT IT HOLDS NO RECORD OF THE PATCH IS
            // SKIPPED, and that answer is read first because it is marked unreadable as
            // well, for the other readers of the same call. An answer that the
            // installation's product is not installed is not that answer: the keyed
            // product enumeration, or the caller's own, listed this installation in this
            // account and context, so it contradicts what the pass established and keeps
            // the file with every other read that did not answer.
            if (state.PatchNotHeld) return true;
            if (state.Unreadable) return false;

            registrations.Add((productCode, sid, context));
            return true;
        }

        foreach (var target in targets)
        {
            pass.CancellationToken.ThrowIfCancellationRequested();

            var resolved = pass.InstancesOf(target);
            if (resolved.Unaskable)
                return new DeclarationAnswer(DeclaredProductOutcome.DeclaredPatchUnestablished, null);

            foreach (var (sid, context) in resolved.Instances)
                if (!AskInstallation(target, sid, context))
                    return new DeclarationAnswer(DeclaredProductOutcome.DeclaredPatchUnestablished, null);
        }

        // WHERE A REGISTRATION FOUND SO FAR RECORDS A COPY THAT CANNOT BE SEEN, EVERY COPY
        // OF THE PATCH IS ALREADY KEPT, and the reads below could only add registrations,
        // so they are not made.
        IReadOnlyList<FileIdentity>? copies = null;
        if (registrations.Count > 0)
        {
            copies = CopiesRecordedBy(code, registrations);
            if (copies is null)
                return new DeclarationAnswer(DeclaredProductOutcome.DeclaredPatchRegistered, null);
        }

        // AND EVERY INSTALLATION THE CALLER'S ENUMERATION LISTED, PUT THE SAME QUESTION.
        // The Template names the products that can accept the patch, and a registration
        // against a product it does not name is not ruled out: Windows documents applying
        // a patch to one instance of a program by that instance's own product code, and
        // does not say that code has to be in the Template. An installation holding the
        // patch answers by the patch's code whatever the Template says, and its copy is
        // compared like any other registration's. Most installations answer in one call
        // that they hold no record of the patch, and each answer is kept for the pass, so
        // a second copy declaring the same patch asks nothing again.
        var found = registrations.Count;
        foreach (var installation in pass.Installations)
        {
            pass.CancellationToken.ThrowIfCancellationRequested();

            if (!AskInstallation(installation.ProductCode, installation.UserSid, (MsiInstallContext)installation.Context))
                return new DeclarationAnswer(DeclaredProductOutcome.DeclaredPatchUnestablished, null);
        }

        if (registrations.Count == 0)
            return new DeclarationAnswer(DeclaredProductOutcome.DeclaredPatchNotRegistered, null);

        if (registrations.Count == found)
            return new DeclarationAnswer(DeclaredProductOutcome.DeclaredPatchRegistered, copies);

        var more = CopiesRecordedBy(code, registrations.GetRange(found, registrations.Count - found));
        return new DeclarationAnswer(
            DeclaredProductOutcome.DeclaredPatchRegistered,
            more is null ? null : [.. copies ?? [], .. more]);
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
    /// The identity of the cached copy every registration of <paramref name="code"/>
    /// records, or null where any of them cannot be seen.
    ///
    /// NULL IS THE ANSWER THAT KEEPS THE FILE, as it is for
    /// <see cref="PackagesOpenedBy"/>, and every way a registration's copy can fail to
    /// be seen reaches it: a <c>LocalPackage</c> read that failed or came back empty, a
    /// value that names nothing, names a folder, will not open to an identity, or names
    /// a file that does not read as patch <paramref name="code"/>. One such registration
    /// is enough, because its copy is the one this candidate could be.
    ///
    /// NO SOURCE LIST OF THE PATCH IS READ, for the reason
    /// <see cref="IDeclaredProductCheck"/> gives: a registration whose cached copy is not
    /// there already keeps the file.
    ///
    /// A READ ANSWERING THAT THE PATCH IS NOT THERE KEEPS THE FILE LIKE ANY OTHER
    /// FAILED READ. Every registration here was named by one of the two routes moments
    /// earlier, so that answer contradicts it, and which copy the registration records
    /// is then not known.
    ///
    /// Several registrations can record one copy, so each path is looked at once.
    /// </summary>
    private IReadOnlyList<FileIdentity>? CopiesRecordedBy(
        string code,
        IReadOnlyList<(string ProductCode, string? Sid, MsiInstallContext Context)> registrations)
    {
        if (_fileIdentities is null || _fileSystem is null) return null;

        var identities = new List<FileIdentity>(registrations.Count);
        var looked = new Dictionary<string, FileIdentity>(StringComparer.Ordinal);

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
        }

        return identities;
    }

    /// <summary>
    /// The verdict for the candidate at <paramref name="candidatePath"/> against every
    /// file in <paramref name="recorded"/>: every package an installation of a product
    /// opens, or the cached copy every registration of a patch records.
    /// <paramref name="differentFromEvery"/> where the candidate is shown to be a
    /// different file from all of them, and <paramref name="matched"/> where it opens as
    /// one of them.
    ///
    /// A CANDIDATE WHOSE OWN IDENTITY DOES NOT READ IS
    /// <see cref="DeclaredProductOutcome.CandidateIdentityUnestablished"/>, which keeps
    /// it. Every recorded file was identified, so what was not established is about this
    /// file alone. A candidate gone by the time it is read answers the same way. Do not
    /// let it through with <paramref name="differentFromEvery"/>: that verdict says the
    /// candidate is a different file from every recorded one, and nothing at a path that
    /// names no file shows that.
    /// </summary>
    private DeclaredProductOutcome CompareWithRecorded(
        string candidatePath,
        IReadOnlyList<FileIdentity> recorded,
        DeclaredProductOutcome differentFromEvery,
        DeclaredProductOutcome matched)
    {
        if (_fileIdentities is null) return matched;
        if (_fileIdentities.ReadOutcome(candidatePath, out var candidate) != FileIdentityRead.Read)
            return DeclaredProductOutcome.CandidateIdentityUnestablished;

        foreach (var package in recorded)
            if (package == candidate) return matched;

        return differentFromEvery;
    }

    /// <param name="Outcome">The verdict the declared code alone gives.</param>
    /// <param name="RecordedPackages">
    /// For an installed product, the identity of every file an installation opens as
    /// its package; for a registered patch, the identity of the cached copy every
    /// registration records. Null where any of them could not be seen, and null for every
    /// other verdict.
    /// </param>
    /// <param name="ByName">
    /// Beside <paramref name="RecordedPackages"/> for an installed product, every package in
    /// a folder on the network an installation opens, read only for a candidate whose name
    /// it could be (<see cref="OpenedPackages.ByName"/>). Null for every other verdict.
    /// </param>
    /// <param name="GivenUp">
    /// For an installed product whose <paramref name="RecordedPackages"/> is null because a
    /// package read was refused for a root given up for the pass, that root
    /// (<see cref="PackagesOpenedBy"/>). Null for every other answer.
    /// </param>
    private readonly record struct DeclarationAnswer(
        DeclaredProductOutcome Outcome,
        IReadOnlyList<FileIdentity>? RecordedPackages,
        IReadOnlyList<NetworkPackage>? ByName = null,
        string? GivenUp = null);

    /// <param name="Identities">The identity of every package read for every candidate.</param>
    /// <param name="ByName">
    /// Every package in a folder on the network, read only for a candidate whose name it
    /// could be (<see cref="WithPackagesItCouldBe"/>).
    /// </param>
    private sealed record OpenedPackages(IReadOnlyList<FileIdentity> Identities, IReadOnlyList<NetworkPackage> ByName);

    /// <param name="Path">The path Windows Installer opens: the source folder and the package name.</param>
    /// <param name="Name">The package name.</param>
    private readonly record struct NetworkPackage(string Path, string Name);

    /// <summary>
    /// The names one candidate has, for comparing with a package name: the last component
    /// of its path, and the names its folder's entry holds for it
    /// (<see cref="NamesInFolderOf"/>), read the first time a package name is not the first.
    /// </summary>
    private sealed class CandidateNames(string path, Func<string, IReadOnlyList<string>?> namesInFolderOf)
    {
        private IReadOnlyList<string>? _inFolder;
        private bool _inFolderRead;

        /// <summary>
        /// Whether a package named <paramref name="packageName"/> could be this candidate by
        /// its name: whether that name and one of the candidate's could name one file
        /// (<see cref="CouldNameOneFile"/>). Where the folder's entry does not read, the
        /// candidate could have any short name, so every package name could be one of its.
        /// </summary>
        internal bool CouldBe(string packageName)
        {
            if (CouldNameOneFile(packageName, path[(path.LastIndexOfAny(['\\', '/']) + 1)..])) return true;

            if (!_inFolderRead)
            {
                _inFolder = namesInFolderOf(path);
                _inFolderRead = true;
            }

            if (_inFolder is null) return true;

            foreach (var name in _inFolder)
                if (CouldNameOneFile(packageName, name)) return true;

            return false;
        }
    }

    /// <summary>
    /// Whether two names could name one file in one folder, as Windows reads the last
    /// component of a path. Trailing dots and spaces are taken off both first, as Windows
    /// takes them off a path's last component, and the two are then one name where they are
    /// equal with ASCII letters compared without case.
    ///
    /// A NAME EMPTY ONCE THAT IS DONE COULD NAME ANY FILE, and so could a name holding any
    /// character outside ASCII. A name of dots alone, '.' and '..' among them, can stand for
    /// the source's own folder or the one above it rather than for a file in it, and either
    /// can be a file's path. A file system compares letters outside ASCII without case
    /// through a table of its own, which need not be the one this process has.
    ///
    /// A PACKAGE NAME REACHING IT HOLDS NO '\', '/', ':', '%' OR NULL, <see cref="AddSourcePackages"/>
    /// having kept the copy for one that does. Let such a name through and this comparison
    /// has to take in what each of them does to a path.
    /// </summary>
    private static bool CouldNameOneFile(string first, string second)
    {
        var a = first.AsSpan().TrimEnd(". ");
        var b = second.AsSpan().TrimEnd(". ");
        if (a.IsEmpty || b.IsEmpty || !Ascii.IsValid(a) || !Ascii.IsValid(b)) return true;

        return Ascii.EqualsIgnoreCase(a, b);
    }

    /// <param name="ByDeclaredCode">
    /// Every listed installation registered under a code other than the one its cached
    /// package declares, keyed by the declared code.
    /// </param>
    /// <param name="UnreadPackageNotRuledOut">
    /// Whether an installation whose cached package did not say what it declares is not
    /// shown by its own record to be an ordinary installation, nor shown to open no package
    /// (<see cref="OpensNoPackage"/>), nor, where it records no cached package, shown to open
    /// nothing but packages that can all be seen.
    /// </param>
    /// <param name="Opened">
    /// The packages the sources of each installation recording no cached package name, where
    /// its record does not show an ordinary installation and every one of them was seen, which
    /// every candidate the answer would let through is compared with.
    /// </param>
    /// <param name="GivenUp">
    /// Where <paramref name="UnreadPackageNotRuledOut"/> is true only because reads of sources
    /// were refused for roots given up for the pass, the first of those roots, and null
    /// otherwise (<see cref="LinksOf"/>).
    /// </param>
    private sealed record InstallationLinks(
        Dictionary<string, List<(string RegisteredCode, string? Sid, MsiInstallContext Context)>> ByDeclaredCode,
        bool UnreadPackageNotRuledOut,
        OpenedPackages Opened,
        string? GivenUp);

    /// <summary>
    /// What one pass has asked Windows about installations and patch registrations,
    /// kept so that nothing outlives the pass and nothing is asked twice inside it, except
    /// whether an installation has no package to open, which is asked again until one is
    /// found (<see cref="OpensNoPackageOf"/>).
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
            IMsiApi msi,
            IReadOnlyList<ListedInstallation> installations,
            CancellationToken cancellationToken,
            Action<SourceFolderWait?>? waitingOn)
        {
            _msi = msi;
            _listed = InstallerQueryService.InstallationsByCode(
                installations.Select(i => (i.ProductCode, i.UserSid, (MsiInstallContext)i.Context)));
            Installations = installations;
            CancellationToken = cancellationToken;
            WaitingOn = waitingOn;
        }

        internal CancellationToken CancellationToken { get; }

        /// <summary>
        /// Told each wait a read makes, naming the root it is under, and null when that wait
        /// ends (<see cref="AnswersWithin"/>).
        /// </summary>
        internal Action<SourceFolderWait?>? WaitingOn { get; }

        /// <summary>
        /// How many waits the pass has made, each told to <see cref="WaitingOn"/> where one is
        /// set (<see cref="AnswersWithin"/>, <see cref="DeclaredProductScreening.WaitCount"/>).
        /// </summary>
        internal int WaitCount { get; set; }

        /// <summary>
        /// For each root the pass has read under or been told to stop waiting for, a task
        /// completed once the caller stops waiting for it, with the root as the caller spelled
        /// it, keyed by root without case. A stop comes from the caller's thread and is looked
        /// for on the pass's, so this is the concurrent kind.
        /// </summary>
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _stops =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// An action that stops waiting for <paramref name="root"/> for the rest of the pass, from
        /// any thread (<see cref="SourceFolderWait.StopWaiting"/>): a wait there ends at once, and
        /// the read that sees the stop gives the root up (<see cref="AnswersWithin"/>), under the
        /// spelling of the wait the caller stopped. A second stop is the same as the first.
        ///
        /// The action holds that root's stop and that spelling and nothing else, so a window still
        /// holding the wait after the pass has ended does not keep the pass alive.
        /// </summary>
        internal Action StopFor(string root)
        {
            var stop = StopOf(root);
            return () => stop.TrySetResult(root);
        }

        /// <summary>
        /// The task the action from <see cref="StopFor"/> completes for <paramref name="root"/>,
        /// with the root as the first stop spelled it.
        /// </summary>
        internal Task<string> Stopped(string root) => StopOf(root).Task;

        // Made to run its continuations asynchronously, so code that continues from a stop runs
        // on the thread pool and not on the caller's thread, which completes it. The pass waits
        // on it with Task.WaitAny, which the runtime wakes from the caller's thread all the same.
        private TaskCompletionSource<string> StopOf(string root) =>
            _stops.GetOrAdd(root, _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));

        /// <summary>The pass's links, once the first declared code has been asked about.</summary>
        internal InstallationLinks? Links { get; set; }

        /// <summary>
        /// What the pass has found about the two conditions that hold back every installation
        /// package it would otherwise let through (<see cref="CachedPackageCensus"/>).
        /// </summary>
        internal CensusTally Census { get; } = new();

        /// <summary>
        /// The packages opened by the installations not ruled out as second copies, or
        /// null where those read for every candidate could not all be seen, once
        /// <see cref="SecondCopiesRead"/>.
        /// </summary>
        internal OpenedPackages? SecondCopies { get; set; }

        /// <summary>Whether <see cref="SecondCopies"/> has been read for this pass.</summary>
        internal bool SecondCopiesRead { get; set; }

        /// <summary>
        /// The root whose give-up refused the read that left <see cref="SecondCopies"/> null,
        /// and null otherwise (<see cref="PackagesSecondCopiesOpen"/>).
        /// </summary>
        internal string? SecondCopiesGivenUp { get; set; }

        /// <summary>Every installation the caller's enumeration listed.</summary>
        internal IReadOnlyList<ListedInstallation> Installations { get; }

        private readonly Dictionary<(string PatchCode, string ProductCode, string? Sid, MsiInstallContext Context),
            InstallerQueryService.PropertyRead> _patchStates = new();

        private readonly Dictionary<(string ProductCode, string? Sid, MsiInstallContext Context), NoPackageReading>
            _hasAPackage = new();

        /// <summary>
        /// What one installation was found to have to open
        /// (<see cref="DeclaredProductCheck.NoPackageReadingOf"/>): the answer of an earlier ask in
        /// this pass that found a package, without asking again, and otherwise
        /// <paramref name="ask"/>'s answer, one finding a package being kept for the rest of the
        /// pass. The code and the account compare without case, as <see cref="PatchStateOf"/>'s do.
        ///
        /// Only an answer finding a package is remembered, so an installation with none is asked
        /// again at the next call and a source list that appears while the pass runs is found
        /// there (<see cref="DeclaredProductCheck.OpensNoPackage"/>).
        /// </summary>
        internal NoPackageReading OpensNoPackageOf(
            string productCode, string? sid, MsiInstallContext context, Func<NoPackageReading> ask)
        {
            var key = (productCode.ToUpperInvariant(), sid?.ToUpperInvariant(), context);
            if (_hasAPackage.TryGetValue(key, out var found)) return found;

            var answer = ask();
            if (answer.Answer != NoPackageAnswer.OpensNone) _hasAPackage[key] = answer;
            return answer;
        }

        /// <summary>
        /// One installation's answer to the keyed read of a patch's <c>State</c>, asked
        /// once per pass whichever copy of the patch, and whichever of its target lists,
        /// asks. The codes and the account compare without case.
        /// </summary>
        internal InstallerQueryService.PropertyRead PatchStateOf(
            string patchCode, string productCode, string? sid, MsiInstallContext context)
        {
            var key = (patchCode.ToUpperInvariant(), productCode.ToUpperInvariant(), sid?.ToUpperInvariant(), context);
            if (!_patchStates.TryGetValue(key, out var read))
                _patchStates[key] = read = InstallerQueryService.GetPatchProperty(
                    _msi, patchCode, productCode, sid, context, MsiInstallProperty.State);

            return read;
        }

        /// <summary>Every root given up for this pass, keyed by root without case.</summary>
        private readonly Dictionary<string, RootGivenUp> _givenUp = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>The same roots, in the order the pass gave them up.</summary>
        private readonly List<RootGivenUp> _givenUpInOrder = [];

        /// <summary>
        /// Whether <paramref name="root"/> is given up for this pass, so that no package under
        /// it is read (<see cref="ReadSourcePackage"/>).
        /// </summary>
        internal bool IsGivenUp(string root) => _givenUp.ContainsKey(root);

        /// <summary>
        /// Gives <paramref name="root"/> up for the rest of the pass, by
        /// <paramref name="route"/>: a root under which a read has not answered within the time
        /// limit (<see cref="AnswersWithin"/>), one under which a package read has answered
        /// false only after waiting longer than <see cref="SourceFolderSlowFailure"/>, one whose
        /// package reads that answered false have taken longer than
        /// <see cref="SourceFolderFailedWaitBudget"/> between them (<see cref="TimeFailing"/>),
        /// one whose reads have taken longer than <see cref="SourceFolderReadBudget"/> between
        /// them, whatever each answered (<see cref="TimeReading"/>), and one the caller has
        /// stopped waiting for (<see cref="StopFor"/>). The reads under a drive include the one
        /// that asks its kind (<see cref="KindOf"/>).
        ///
        /// A ROOT GIVEN UP AGAIN TAKES WHICHEVER OF ITS ROUTES IS DECLARED FIRST IN
        /// <see cref="SourceRootGiveUpRoute"/>, so a read meeting more than one condition gives
        /// its root the first of them, whatever order they are checked in. The spelling kept is
        /// the one it was first given up under, which for a root the caller stopped waiting for
        /// is the spelling of the wait it stopped (<see cref="StopFor"/>).
        ///
        /// It runs on the pass's thread alone. A stop made on the caller's thread gives its root
        /// up here through the read that sees it (<see cref="AnswersWithin"/>).
        /// </summary>
        internal void GiveUp(string root, SourceRootGiveUpRoute route)
        {
            if (_givenUp.TryGetValue(root, out var given))
            {
                if (route < given.Route) given.Route = route;
                return;
            }

            given = new RootGivenUp(root, route);
            _givenUp[root] = given;
            _givenUpInOrder.Add(given);
        }

        /// <summary>
        /// Counts one more candidate kept where its check stopped at a read refused for
        /// <paramref name="root"/>, a root given up for this pass (<see cref="Screen"/>).
        /// </summary>
        internal void CountKept(string root) => _givenUp[root].FilesKept++;

        /// <summary>Every root given up for this pass, in the order it gave them up.</summary>
        internal IReadOnlyList<SourceRootGivenUp> GivenUp() =>
            _givenUpInOrder.Select(given => new SourceRootGivenUp(given.Root, given.Route, given.FilesKept)).ToList();

        /// <summary>
        /// How long the package reads under each root that answered false, each within
        /// <see cref="SourceFolderSlowFailure"/>, have taken in this pass, keyed by root without
        /// case (<see cref="ReadSourcePackage"/>).
        /// </summary>
        internal Dictionary<string, TimeSpan> TimeFailing { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// How long all the reads under each root have taken in this pass, whatever each
        /// answered, the read asking a drive its kind among them, keyed by root without case
        /// (<see cref="AnswersWithin"/>).
        /// </summary>
        internal Dictionary<string, TimeSpan> TimeReading { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Each drive's kind, or null where it did not answer within the time limit, once
        /// asked (<see cref="KindOf"/>). Keyed by the drive letter and its colon, without
        /// case.
        /// </summary>
        internal Dictionary<string, DriveType?> DriveKinds { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Whether Windows may follow a symbolic link reached through a network path, once
        /// read (<see cref="RemoteLinksCanBeFollowed"/>).
        /// </summary>
        internal bool? RemoteLinksFollowed { get; set; }

        /// <summary>
        /// Each package on the network read for a candidate, keyed by its path, with what
        /// <see cref="ReadSourcePackage"/> answered and the root whose give-up refused it,
        /// where one did.
        /// </summary>
        internal Dictionary<string, (bool Settled, FileIdentity? Identity, string? GivenUp)> NetworkPackageReads { get; } =
            new(StringComparer.Ordinal);

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

        /// <summary>One root given up for the pass, as <see cref="GiveUp"/> and <see cref="CountKept"/> leave it.</summary>
        private sealed class RootGivenUp(string root, SourceRootGiveUpRoute route)
        {
            internal string Root { get; } = root;

            internal SourceRootGiveUpRoute Route { get; set; } = route;

            internal int FilesKept { get; set; }
        }
    }
}
