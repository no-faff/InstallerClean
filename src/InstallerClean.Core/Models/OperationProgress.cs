using InstallerClean.Services;

namespace InstallerClean.Models;

/// <summary>One report from a Move or a Delete while it runs.</summary>
/// <param name="CurrentFile">How many files the batch has reached, the one named among them.</param>
/// <param name="TotalFiles">How many files are in the batch now. A check made in the middle of a batch can drop some.</param>
/// <param name="CurrentFileName">The file the batch has just reached. Empty where <paramref name="Check"/> is set.</param>
/// <param name="Check">
/// Set where the report is not of a file: the batch is waiting for Windows Installer, is
/// making its check again, or that check is waiting on a source folder. Null for a file's
/// own line.
/// </param>
public sealed record OperationProgress(
    int CurrentFile,
    int TotalFiles,
    string CurrentFileName,
    BatchCheckProgress? Check = null);
