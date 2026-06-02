namespace InstallerClean.Services;

/// <summary>
/// What happened to one file passed to <see cref="IRecycleEngine.RecycleFile"/>.
/// </summary>
public enum RecycleOutcome
{
    /// <summary>Sent to the Recycle Bin (the shell reported a newly created bin item).</summary>
    Recycled,

    /// <summary>
    /// Deleted, but not recycled: gone from disk with no Recycle Bin
    /// entry. <c>FOFX_RECYCLEONDELETE</c> is recycle-or-permanently-delete,
    /// so this is the silent-nuke case the engine surfaces rather than
    /// hides. Unrecoverable.
    /// </summary>
    PermanentlyDeleted,

    /// <summary>The operation failed and the file was not deleted. Carries the HRESULT.</summary>
    Failed,
}

/// <summary>
/// Per-file result. <see cref="HResult"/> is the relevant shell HRESULT:
/// the per-item <c>hrDelete</c> for <see cref="RecycleOutcome.Recycled"/>
/// and <see cref="RecycleOutcome.PermanentlyDeleted"/>, or the failing
/// code (activation, shell-item creation, or delete) for
/// <see cref="RecycleOutcome.Failed"/>. It is the raw integer for
/// telemetry; human formatting is the caller's concern.
/// </summary>
public readonly record struct RecycleFileOutcome(RecycleOutcome Outcome, int HResult);

/// <summary>
/// Sends single files to the Recycle Bin through the Windows shell
/// <c>IFileOperation</c> API. Owns a dedicated STA thread (the API is
/// STA-only) shared by the WPF host and the headless CLI, so both
/// drive the same engine through <see cref="IDeleteFilesService"/>.
///
/// Injected into <see cref="DeleteFilesService"/> (mirroring
/// <c>IFileSystem</c> / <c>IRegistryReader</c> / <c>IMutexProbe</c>) so
/// the delete loop is unit-testable against a fake; the real COM path
/// is exercised only by Windows integration tests.
/// </summary>
public interface IRecycleEngine
{
    /// <summary>
    /// True only if a throwaway probe file placed on the same volume as
    /// <paramref name="anyPathOnThatVolume"/> demonstrably recycled
    /// (the shell reported a newly created bin item). False on any
    /// doubt: bin disabled or full for the volume, the probe file could
    /// not be created on that volume, or activation failed. Recycle
    /// behaviour is per-volume, so the verdict is only valid for the
    /// volume of the given path. Used as the pre-delete safety check so
    /// the engine never silently permanently deletes a batch when the
    /// bin is unavailable.
    /// </summary>
    bool CanRecycleToVolume(string anyPathOnThatVolume);

    /// <summary>
    /// Sends one existing file to the Recycle Bin. One
    /// <c>IFileOperation</c> per call. Never throws for a shell failure;
    /// returns <see cref="RecycleOutcome.Failed"/> with the HRESULT.
    /// Fails closed: if the shell cannot be activated the file is left
    /// untouched (no fallback that could permanently delete it).
    /// </summary>
    RecycleFileOutcome RecycleFile(string filePath);
}
