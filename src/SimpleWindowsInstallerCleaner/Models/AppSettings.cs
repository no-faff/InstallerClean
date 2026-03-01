namespace SimpleWindowsInstallerCleaner.Models;

public sealed class AppSettings
{
    internal const string DefaultExclusionFilter = "Acrobat";

    public string MoveDestination { get; set; } = string.Empty;
    public List<string> ExclusionFilters { get; set; } = new() { DefaultExclusionFilter };
    public bool ShortcutOffered { get; set; }
}
