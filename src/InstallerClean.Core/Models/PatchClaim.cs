namespace InstallerClean.Models;

/// <summary>
/// One product's claim on one cached patch file: everything
/// <c>MsiGetPatchInfoEx</c> needs to be asked about that pairing again later.
///
/// It exists because the merge that builds <see cref="RegisteredPackage"/> rows
/// cannot keep it. A patch is cached once and applied to as many products as
/// hold it, so several claims name one path, and the merge keeps ONE row per
/// path: the surviving row's product code is whichever product the enumeration
/// reached first. That is the right answer for a verdict, which has to be the
/// harshest across every claim, and the wrong one for an identity, because
/// asking about the survivor's product asks about one of several and cannot see
/// what any of the others now say.
///
/// So claims are collected per claim rather than per path, and a consumer
/// re-reading one path re-reads every claim naming it.
/// </summary>
/// <param name="LocalPackagePath">
/// The claimed path, normalised the same way the <see cref="RegisteredPackage"/>
/// rows are, so a consumer can match the two sets on it.
/// </param>
/// <param name="Context">
/// The <c>MSIINSTALLCONTEXT</c> the claim was enumerated in (1 per-user managed,
/// 2 per-user unmanaged, 4 per-machine), carried as the raw API value for the
/// same reason <see cref="RegisteredPackage.PatchState"/> is: the models keep no
/// dependency on the interop layer, and the value is an API integer either way.
/// </param>
public sealed record PatchClaim(
    string LocalPackagePath,
    string PatchCode,
    string ProductCode,
    string? UserSid,
    int Context);
