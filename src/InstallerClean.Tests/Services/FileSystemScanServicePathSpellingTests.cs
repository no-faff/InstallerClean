using System.IO.Abstractions.TestingHelpers;
using InstallerClean.Models;
using InstallerClean.Services;
using NSubstitute;

namespace InstallerClean.Tests.Services;

/// <summary>
/// How the scan decides that a walked file is the one a registration names, when
/// the registration's recorded path and the walk's path are not the same string.
///
/// THIS IS THE GATE THE WHOLE OFFER RESTS ON. A registration the scan fails to
/// match does not make its file safe to remove; it makes the file a candidate.
/// What is left between a candidate and the user is the declared-product screen,
/// which asks an installation package what it says it belongs to and puts that to
/// Windows. It runs on installation packages only, and a patch has no second
/// reader behind this gate. Everything here is about not needing a second chance.
///
/// Two mechanisms, pinned separately because they close different things. The
/// comparer handles case, which is the divergence real machines actually produce.
/// The file-identity reader handles every other spelling at once, which matters
/// because the list of other spellings is somebody's enumeration and nothing says
/// when it is complete.
/// </summary>
public class FileSystemScanServicePathSpellingTests
{
    private const string CacheRoot = @"C:\Windows\Installer";
    private const string Walked = @"C:\Windows\Installer\9f05cba.msi";
    private const string Orphan = @"C:\Windows\Installer\orphan.msi";

    /// <summary>
    /// One registration naming a file directly in the walked folder, present on
    /// disk, added to every fixture here by <see cref="Scan"/>.
    ///
    /// WITHOUT IT NONE OF THESE TESTS REACHES ITS OWN SUBJECT, and for five of them
    /// that was true from the day they were written. The scan refuses outright when
    /// the records hold rows, no row names a file in the folder it walked, and the
    /// walk still produced candidates: two sides that describe different places
    /// cannot be compared, so nothing is offered and nothing is claimed. Every test
    /// below that registers a path OUTSIDE the folder (through a junction, or
    /// somewhere else on the disk, or nowhere at all) builds exactly that shape, so
    /// the scan threw before the mechanism under test could be read. The scan was
    /// right and the fixtures were not.
    ///
    /// AND IT CHANGES NOTHING ANY TEST HERE ASSERTS, which is the part to check
    /// rather than assume, because a fixture added to satisfy a gate can quietly
    /// alter the pass it was added to protect. It is claimed by the path comparison
    /// in the walk loop, so it never enters the candidate list and the count of
    /// candidates the gates are measured against is unmoved. The numeric correlation
    /// gate needs a folder whose in-folder registrations are almost all missing, and
    /// this one is present. The only quantity it moves is the number of
    /// registrations, and the identity attempts, which two tests read and name.
    ///
    /// IT READS CLEANLY IN EVERY IDENTITY MAP AND IT USED NOT TO, which is a change
    /// this file could not have survived. It was absent from every map, yielding no
    /// identity, on the reasoning that a registration that cannot be identified
    /// claims nothing extra. That is exactly the reasoning 3.0.0 refutes: an
    /// unidentifiable registration now withholds the whole walk-derived offer, so
    /// the old arrangement would have emptied the offer in every test here and each
    /// of them would have failed for a reason that had nothing to do with its
    /// subject. It claims its own file, which is already off the candidate list.
    ///
    /// It is what every real machine has and none of these fixtures had: a record
    /// pointing at a file that is really there.
    /// </summary>
    private const string Anchor = @"C:\Windows\Installer\anchor.msi";

    // ---- The comparer, which is what actually carries real machines ----

    [Theory]
    // The three spellings one machine's own records carry for one folder, counted
    // 2026-08-11 across its 138 registrations: 121 upper, 15 mixed, 2 with a
    // lower-case drive letter. Windows Installer records whatever spelling was in
    // force when the product was installed and normalises nothing, so all three
    // are ordinary and none of them is the spelling the folder walk produces.
    [InlineData(@"C:\WINDOWS\Installer\9f05cba.msi")]
    [InlineData(@"C:\Windows\Installer\9f05cba.msi")]
    [InlineData(@"c:\Windows\Installer\9f05cba.msi")]
    [InlineData(@"C:\windows\installer\9F05CBA.MSI")]
    public async Task A_registration_spelled_in_another_case_still_claims_its_file(string registeredAs)
    {
        // On the machine this was measured on, 123 of 138 registrations differ
        // from the walk's spelling by case alone. An ordinal comparison would put
        // every one of those files into the candidate set on a single scan, so
        // this is not a nicety about tidy strings: it is the difference between
        // the app claiming its registrations and the app handing the lot to the
        // identity check to rescue.
        var result = await Scan(
            walkedFiles: new[] { Walked },
            registeredPaths: new[] { registeredAs });

        Assert.Empty(result.RemovableFiles);
    }

    [Fact]
    public async Task A_file_no_registration_names_is_still_offered()
    {
        // The must-fail half of the pair. Without it the case theory above passes
        // just as well against a scan that offers nothing at all, which is the
        // shape a broken gate would have.
        var result = await Scan(
            walkedFiles: new[] { Walked, Orphan },
            registeredPaths: new[] { @"C:\WINDOWS\Installer\9f05cba.msi" });

        var offered = Assert.Single(result.RemovableFiles);
        Assert.Equal(Orphan, offered.FullPath);
    }

    // ---- The file-identity reader, which closes the spellings nobody has seen ----

    [Fact]
    public async Task A_candidate_that_is_the_same_FILE_as_a_registration_is_not_offered()
    {
        // The registration is recorded through a junction, so no comparison of
        // strings can match it, and the app's own resolution does not flag that
        // form. Asking the filesystem which file each path names settles it.
        const string throughAJunction = @"C:\Junction\9f05cba.msi";
        var ids = Identities([
            (throughAJunction, 1),
            (Walked, 1),
            (Orphan, 2)]);

        var result = await Scan(
            walkedFiles: new[] { Walked, Orphan },
            registeredPaths: new[] { throughAJunction },
            fileIds: ids);

        var offered = Assert.Single(result.RemovableFiles);
        Assert.Equal(Orphan, offered.FullPath);
    }

    [Fact]
    public async Task A_candidate_that_is_a_different_file_is_still_offered()
    {
        // The must-fail control for the mechanism above: a reader that answered
        // "same file" to everything would pass the previous test and empty every
        // offer, and the two look identical from the outside.
        var ids = Identities([
            (@"C:\Elsewhere\other.msi", 7),
            (Orphan, 8)]);

        var result = await Scan(
            walkedFiles: new[] { Orphan },
            registeredPaths: new[] { @"C:\Elsewhere\other.msi" },
            fileIds: ids);

        Assert.Single(result.RemovableFiles);
    }

    // ---- What a read that did not answer costs, which is where this file was wrong ----

    [Theory]
    [InlineData(FileIdentityRead.OpenRefused)]
    [InlineData(FileIdentityRead.IdentityUnavailable)]
    [InlineData(FileIdentityRead.Faulted)]
    [InlineData(FileIdentityRead.NotAPath)]
    public async Task A_registration_this_scan_could_not_identify_takes_the_whole_offer_with_it(
        FileIdentityRead outcome)
    {
        // THIS TEST USED TO ASSERT THE OPPOSITE AND ITS REASONING IS WORTH KEEPING,
        // because it is the reasoning the whole fix had to refute. It read: no
        // identity for the registration means nothing to compare against, so the
        // candidate goes on exactly as it did before this mechanism existed, and the
        // failure direction is the safety argument for having added it.
        //
        // That is true of the PASS, which only ever subtracts, and false about the
        // MACHINE. The registration nobody could identify is one whose cached file
        // may be sitting in this candidate list right now, unclaimed, and WHICH file
        // cannot be established, so every candidate is one it could have meant. The
        // old assertion was pinning a needed file onto the offer.
        var ids = Identities(
            [(Orphan, 3)],
            (@"C:\Elsewhere\registered.msi", outcome));

        var result = await Scan(
            walkedFiles: new[] { Orphan },
            registeredPaths: new[] { @"C:\Elsewhere\registered.msi" },
            fileIds: ids);

        Assert.Empty(result.RemovableFiles);
        Assert.True(result.WalkOfferWithheldWholesale);
        Assert.Equal(Orphan, Assert.Single(result.WithheldFiles!).FullPath);
        Assert.Equal(1, result.RegistrationIdentityReads.RefusedTotal);
    }

    [Theory]
    [InlineData(FileIdentityRead.OpenRefused)]
    [InlineData(FileIdentityRead.IdentityUnavailable)]
    [InlineData(FileIdentityRead.Faulted)]
    [InlineData(FileIdentityRead.NotAPath)]
    public async Task A_candidate_this_scan_could_not_identify_is_kept_back_and_its_neighbours_are_not(
        FileIdentityRead outcome)
    {
        // THE OTHER HALF OF THE SAME CORRECTION, and this one was never reported to
        // anybody until the work was nearly finished. The old test said an
        // unreadable answer must never be read as "not the same file, therefore
        // offer it" any more than as "the same file, therefore keep it", and left it
        // on the offer, which is the first of those two readings wearing the
        // language of the second.
        //
        // AND THE ACTION HERE IS DELIBERATELY NARROWER THAN THE REGISTRATION SIDE'S.
        // Every other candidate was compared against the registrations by a read
        // that answered, so only this one is unaccounted for. Emptying the offer
        // would cost a machine everything for a fact about one of its files, and
        // that is the accommodation running the wrong way.
        var ids = Identities(
            [(@"C:\Elsewhere\registered.msi", 9), (Walked, 4)],
            (Orphan, outcome));

        var result = await Scan(
            walkedFiles: new[] { Orphan, Walked },
            registeredPaths: new[] { @"C:\Elsewhere\registered.msi" },
            fileIds: ids);

        Assert.Equal(Walked, Assert.Single(result.RemovableFiles).FullPath);
        Assert.False(result.WalkOfferWithheldWholesale);
        Assert.Equal(Orphan, Assert.Single(result.WithheldFiles!).FullPath);
        Assert.Equal(1, result.CandidateIdentityReads.RefusedTotal);
        Assert.Equal(0, result.RegistrationIdentityReads.RefusedTotal);
    }

    // ---- The member that decides whether an ordinary machine keeps its offer ----

    [Fact]
    public async Task A_registration_whose_cached_file_has_gone_costs_the_offer_nothing()
    {
        // THE CONTROL THIS WHOLE CHANGE STANDS OR FALLS ON, and it is a fact about
        // ordinary machines rather than an edge case. A registration whose cached
        // file has been removed is the commonest failure the identity reader will
        // ever see: it is what the missing-from-disk banner exists to report, and it
        // is on any machine that has uninstalled anything.
        //
        // Such a registration claims none of the walked files, because there is no
        // file at the end of it for anything to have claimed, so nothing was given
        // up by failing to identify it. If NamesNothing ever joins a refusal total
        // this goes red, and what it will have caught is an app that offers nothing
        // to most of the machines in the world.
        //
        // The path is inside the cache folder and absent from the walk, which is the
        // real shape: the registration is one the folder no longer holds a file for.
        var ids = Identities(
            [(Orphan, 5)],
            (@"C:\Windows\Installer\removed.msi", FileIdentityRead.NamesNothing));

        var result = await Scan(
            walkedFiles: new[] { Orphan },
            registeredPaths: new[] { @"C:\Windows\Installer\removed.msi" },
            fileIds: ids);

        Assert.Equal(Orphan, Assert.Single(result.RemovableFiles).FullPath);
        Assert.False(result.WalkOfferWithheldWholesale);
        Assert.Empty(result.WithheldFiles!);
        Assert.Equal(0, result.RegistrationIdentityReads.RefusedTotal);
        Assert.Equal(1, result.RegistrationIdentityReads.NamesNothingCount);
    }

    [Fact]
    public async Task A_candidate_that_went_between_the_walk_and_the_read_is_still_offered()
    {
        // The same member on the other side. A file that is no longer there is one
        // no registration's identity could have matched either, so keeping it back
        // would establish nothing and cost an offer. It is left where it was, which
        // is what the act-time re-verify and the delete itself are for.
        var ids = Identities(
            [(@"C:\Elsewhere\registered.msi", 9)],
            (Orphan, FileIdentityRead.NamesNothing));

        var result = await Scan(
            walkedFiles: new[] { Orphan },
            registeredPaths: new[] { @"C:\Elsewhere\registered.msi" },
            fileIds: ids);

        Assert.Equal(Orphan, Assert.Single(result.RemovableFiles).FullPath);
        Assert.Empty(result.WithheldFiles!);
        Assert.Equal(0, result.CandidateIdentityReads.RefusedTotal);
        Assert.Equal(1, result.CandidateIdentityReads.NamesNothingCount);
    }

    [Fact]
    public async Task A_scan_whose_every_read_answered_withholds_nothing_and_still_counts_them()
    {
        // THE MUST-MISS CONTROL FOR THE FOUR TESTS ABOVE. Without it they pass just
        // as well against a scan that withholds unconditionally, and the two are
        // indistinguishable from the assertions alone. It also pins that a clean
        // read is counted as an attempt and as no failure, which is what makes five
        // zeros in a report readable.
        var ids = Identities([(@"C:\Elsewhere\registered.msi", 9), (Orphan, 4)]);

        var result = await Scan(
            walkedFiles: new[] { Orphan },
            registeredPaths: new[] { @"C:\Elsewhere\registered.msi" },
            fileIds: ids);

        Assert.Single(result.RemovableFiles);
        Assert.False(result.WalkOfferWithheldWholesale);
        Assert.Empty(result.WithheldFiles!);
        // Two registrations: the one named above and the anchor every fixture here
        // carries. One candidate.
        Assert.Equal(2, result.RegistrationIdentityReads.AttemptCount);
        Assert.Equal(0, result.RegistrationIdentityReads.RefusedTotal);
        Assert.Equal(1, result.CandidateIdentityReads.AttemptCount);
        Assert.Equal(0, result.CandidateIdentityReads.RefusedTotal);
    }

    [Fact]
    public async Task No_reader_at_all_leaves_the_scan_exactly_as_it_was()
    {
        // Every other test in the suite constructs the scan without one, so this
        // pins what that means rather than leaving it to be assumed: the string
        // comparison alone, and an offer identical to the one made before any of
        // this existed.
        var result = await Scan(
            walkedFiles: new[] { Orphan },
            registeredPaths: new[] { @"C:\Elsewhere\other.msi" },
            fileIds: null);

        Assert.Single(result.RemovableFiles);
    }

    // ---- Helpers ----

    /// <summary>
    /// The anchor's own file number. It reads cleanly like the ordinary
    /// registration it is; see <see cref="Anchor"/> for why every fixture has one.
    /// </summary>
    private const ulong AnchorFile = 99;

    /// <summary>
    /// A reader answering from a map. <paramref name="reads"/> are paths that
    /// identify cleanly, by file number: two paths carrying the same number are the
    /// same file. <paramref name="answers"/> are paths that do not, each stating
    /// which of the five failures it met, because a fixture that says only "no
    /// answer" cannot tell a file that has gone from one that would not open, and
    /// the app now treats those as opposite things.
    ///
    /// A PATH NO FIXTURE LISTED IS A GIVE-UP AND THAT IS NOT ARBITRARY.
    /// <c>FileIdentityRead.Read</c> is the enum's zero value, which is right for the
    /// production code and a trap here: an unconfigured substitute answers zero, so
    /// it would claim to have identified every path on the machine AND hand every
    /// one of them the same default identity, which reads as one file wearing many
    /// names. A fixture that forgets a path withholds instead, which is visible.
    ///
    /// The volume serial is fixed and the id's high half is zero, so the tests vary
    /// the one field they are about. Both halves being part of the value is pinned
    /// by the type rather than here.
    /// </summary>
    private static IFileIdentityReader Identities(
        (string Path, ulong File)[] reads,
        params (string Path, FileIdentityRead Answer)[] answers)
    {
        var map = new Dictionary<string, (FileIdentityRead Answer, ulong File)>(
            StringComparer.OrdinalIgnoreCase)
        {
            // Centrally, for the same reason the anchor is walked and registered
            // centrally: a fixture whose only in-folder registration cannot be
            // identified is a machine in trouble, not the ordinary one these tests
            // mean to describe.
            [Anchor] = (FileIdentityRead.Read, AnchorFile),
        };
        foreach (var (path, file) in reads) map[path] = (FileIdentityRead.Read, file);
        foreach (var (path, answer) in answers) map[path] = (answer, 0);

        var reader = Substitute.For<IFileIdentityReader>();
        reader.ReadOutcome(Arg.Any<string>(), out Arg.Any<FileIdentity>())
            .Returns(call =>
            {
                var path = (string?)call[0] ?? string.Empty;
                if (!map.TryGetValue(path, out var entry))
                {
                    call[1] = default(FileIdentity);
                    return FileIdentityRead.OpenRefused;
                }

                // Nothing but a clean read fills the identity, which is the
                // production contract: a fake that filled it on a failure would let
                // a caller that read the out value without checking the outcome
                // pass a test it should fail.
                call[1] = entry.Answer == FileIdentityRead.Read
                    ? new FileIdentity(VolumeSerialNumber: 1, FileIdLow: entry.File, FileIdHigh: 0)
                    : default(FileIdentity);
                return entry.Answer;
            });
        return reader;
    }

    private static async Task<ScanResult> Scan(
        string[] walkedFiles,
        string[] registeredPaths,
        IFileIdentityReader? fileIds = null)
    {
        // The anchor goes in on all three sides at once, because that is what makes
        // it an ordinary registration rather than a token: walked, on the disk, and
        // in the records. See its own note for why it is here and why it moves
        // nothing. Added centrally so no fixture can omit it and go red at the gate
        // instead of at its own subject, which is what happened to five of these.
        var walked = walkedFiles.Append(Anchor).ToArray();
        var registered = registeredPaths.Append(Anchor).ToArray();

        var fs = new MockFileSystem();
        fs.AddDirectory(CacheRoot);
        foreach (var path in walked) fs.AddFile(path, new MockFileData("x"));

        var query = Substitute.For<IInstallerQueryService>();
        query.GetRegisteredPackagesAsync(
                Arg.Any<IProgress<ScanProgressUpdate>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new InstallerQueryResult(
                registered.Select(p => new RegisteredPackage(p, "Product", "{code}")).ToList())));

        return await new FileSystemScanService(
            query, fs, null, walked, CacheRoot, fileIds)
            .ScanAsync();
    }
}
