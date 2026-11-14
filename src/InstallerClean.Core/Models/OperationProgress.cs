namespace InstallerClean.Models;

/// <summary>
/// One step of a Move or Delete batch, for the window's bar and the command line's lines.
/// </summary>
/// <param name="CurrentFile">The count of the file named, from 1.</param>
/// <param name="TotalFiles">The files the batch now holds.</param>
/// <param name="CurrentFileName">The file named.</param>
/// <param name="Finished">
/// True where the file named has been dealt with, so <paramref name="CurrentFile"/> files
/// are done; false where it is still in hand, so one fewer is.
/// </param>
public sealed record OperationProgress(
    int CurrentFile,
    int TotalFiles,
    string CurrentFileName,
    bool Finished = false);
