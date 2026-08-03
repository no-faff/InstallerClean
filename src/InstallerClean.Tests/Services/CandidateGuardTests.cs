using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

/// <summary>
/// What <see cref="CandidateGuard"/> answers, held from outside the two action
/// services that consume it. The guard's own verdicts are exercised against the
/// real filesystem by the integration tests, which is the only place they can be:
/// both of its checks read the real filesystem whatever <c>IFileSystem</c> is
/// injected, by design.
/// </summary>
public class CandidateGuardTests
{
    [Fact]
    public void RemovalSafety_carries_the_three_answers_both_action_services_handle()
    {
        // A canary, and it is the only test that can stand behind the guard sites'
        // default arms: those arms are unreachable while the enum has these three
        // members, so nothing at runtime can reach them and nothing at runtime can
        // prove they are right.
        //
        // Failing here is not a defect. It means the enum grew, and the moment to
        // decide whether the new answer deserves an error and a crash-log sentence
        // of its own is before it silently inherits the vague ones. Neither
        // service can delete or move on it either way, which is the property the
        // inverted guards buy and this test does not need to restate.
        Assert.Equal(
            new[] { "Safe", "Refused", "Unproven" },
            Enum.GetNames<CandidateGuard.RemovalSafety>());
    }
}
