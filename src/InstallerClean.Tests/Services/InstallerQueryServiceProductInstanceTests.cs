using InstallerClean.Interop;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// THE MACHINE THESE ARE ABOUT, IN PLAIN WORDS. One product code names more than one
/// installation on the same PC: installed for the machine and for a user at once, or
/// under two user accounts. Each of those is its own row in the filtered product
/// enumeration, with its own account and its own installation context, and a keyed
/// patch read is put in one account and one context and answers about that instance
/// alone.
///
/// SO THE ACCOUNT AND CONTEXT ARE WHAT MAKES A ROW WORTH HAVING. A cached patch can be
/// applied under one instance and superseded under another, and the row that says it is
/// still needed is the one that keeps the file. What these pin is that every row is read
/// and that a row the walk cannot read takes the whole code out of the judged set rather
/// than shortening it.
///
/// THEY DRIVE THE RESOLVE DIRECTLY, and the screen tests beside them drive it through
/// the one caller whose answer is a verdict rather than a list. The other two callers
/// hand their rows to keyed patch reads, which is the enumeration's own subject.
/// </summary>
public class InstallerQueryServiceProductInstanceTests
{
    private const string ProductA = "{11111111-1111-1111-1111-111111111111}";
    private const string UserSid = "S-1-5-21-1111111111-2222222222-3333333333-1001";
    private const string OtherSid = "S-1-5-21-1111111111-2222222222-3333333333-1002";

    [Fact]
    public void Every_instance_of_one_code_comes_back_with_its_own_account_and_context()
    {
        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA,
            (null, MsiInstallContext.Machine),
            (UserSid, MsiInstallContext.UserUnmanaged),
            (OtherSid, MsiInstallContext.UserManaged));

        var resolved = InstallerQueryService.ResolveProductInstances(msi, ProductA);

        Assert.False(resolved.Unaskable);
        Assert.Equal(
            new[]
            {
                ((string?)null, MsiInstallContext.Machine),
                (UserSid, MsiInstallContext.UserUnmanaged),
                (OtherSid, MsiInstallContext.UserManaged),
            },
            resolved.Instances);
    }

    [Fact]
    public void One_instance_is_the_ordinary_answer_and_still_comes_back_whole()
    {
        // The must-miss half of the test above: the shape a machine with nothing
        // unusual on it produces, so a walk that returned every row for the wrong
        // reason would show up here as more than one.
        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA);

        var resolved = InstallerQueryService.ResolveProductInstances(msi, ProductA);

        Assert.False(resolved.Unaskable);
        Assert.Equal(new[] { ((string?)null, MsiInstallContext.Machine) }, resolved.Instances);
    }

    [Theory]
    [InlineData(MsiError.NoMoreItems)]
    [InlineData(MsiError.UnknownProduct)]
    public void A_code_the_machine_does_not_hold_comes_back_empty_and_answered(uint absence)
    {
        // Empty and NOT unaskable, which is the distinction the callers turn on: a
        // product that is not there holds no patches, so this is an answer rather than
        // a failure to get one. Both returns that carry that meaning are read, because
        // which ones may is IsProductNotInstalled's decision and there is more than one.
        var msi = new ScriptedMsiProducts();
        msi.NotInstalled(ProductA, absence);

        var resolved = InstallerQueryService.ResolveProductInstances(msi, ProductA);

        Assert.False(resolved.Unaskable);
        Assert.Empty(resolved.Instances);
    }

    [Fact]
    public void A_row_the_walk_cannot_read_makes_the_whole_code_unaskable()
    {
        // The first row reads cleanly and the second does not. What comes back is
        // nothing at all rather than the row that was read: a list short by an unknown
        // amount cannot be told from a machine that holds only what it handed over, and
        // the answer that withholds is the one true of both.
        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA, (null, MsiInstallContext.Machine), (UserSid, MsiInstallContext.UserUnmanaged));
        msi.AnswersAtRow(ProductA, index: 1, MsiError.AccessDenied);

        var resolved = InstallerQueryService.ResolveProductInstances(msi, ProductA);

        Assert.True(resolved.Unaskable);
        Assert.Empty(resolved.Instances);
    }

    [Fact]
    public void A_first_row_that_cannot_be_read_is_unaskable()
    {
        var msi = new ScriptedMsiProducts();
        msi.Answers(ProductA, MsiError.AccessDenied);

        var resolved = InstallerQueryService.ResolveProductInstances(msi, ProductA);

        Assert.True(resolved.Unaskable);
        Assert.Empty(resolved.Instances);
    }

    [Fact]
    public void The_walk_asks_about_the_code_it_was_given_and_stops_where_the_rows_do()
    {
        // One question per row plus the one that ends them, and every one of them keyed
        // to this code. A walk that read the machine's list instead would answer from
        // whichever product sat at the index.
        var msi = new ScriptedMsiProducts();
        msi.Installed(ProductA, (null, MsiInstallContext.Machine), (UserSid, MsiInstallContext.UserUnmanaged));

        var resolved = InstallerQueryService.ResolveProductInstances(msi, ProductA);

        Assert.Equal(2, resolved.Instances.Count);
        Assert.Equal(new[] { ProductA }, msi.Asked);
        Assert.Equal(3, msi.Rows);
    }
}
