namespace InstallerClean.Models;

/// <summary>
/// A patch file entry inside a <see cref="ProductRow"/>'s patch list.
/// </summary>
public sealed record PatchRow(
    string FileName,
    string FullPath,
    string SizeDisplay);
