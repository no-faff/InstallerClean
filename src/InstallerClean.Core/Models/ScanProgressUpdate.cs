namespace InstallerClean.Models;

/// <summary>
/// One scan progress update. <see cref="IsMilestone"/> separates the
/// fixed phase messages ("Asking Windows about installed software...",
/// "Found 12 registered packages.") from the per-product ticker that
/// names each package as the enumeration passes it. The split exists
/// for screen readers: the milestone stream is read out through a live
/// region, while the ticker, which can run to hundreds of updates in a
/// few seconds, is display-only, because announcing every update queues
/// speech far faster than it can be spoken.
/// </summary>
public sealed record ScanProgressUpdate(string Message, bool IsMilestone = true);
