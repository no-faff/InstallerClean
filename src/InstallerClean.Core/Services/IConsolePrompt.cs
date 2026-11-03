namespace InstallerClean.Services;

/// <summary>
/// The console as a question reaches it: lines out, text left waiting at the cursor,
/// and one line of the person's answer back. The command line's update and its report
/// offer both ask through it.
///
/// <see cref="WriteLine"/> and <see cref="Write"/> never throw: a write the console
/// refuses is the host's to deal with, and the run goes on.
/// </summary>
internal interface IConsolePrompt
{
    /// <summary>Prints a line. Never throws.</summary>
    void WriteLine(string line);

    /// <summary>Prints text and leaves the cursor after it, for the question. Never throws.</summary>
    void Write(string text);

    /// <summary>
    /// Discards key presses typed before the question appeared, so the read that follows
    /// takes only what is typed at the question. Acts only on a console somebody types
    /// into. Never throws.
    /// </summary>
    void DiscardTypedAhead();

    /// <summary>
    /// Reads one line of the person's answer, or null at the end of input, which is also
    /// what Ctrl+C at the prompt can end the read with, and null where
    /// <paramref name="token"/> is cancelled first, as a console being closed cancels it
    /// with the read still waiting.
    /// </summary>
    string? ReadLine(CancellationToken token);
}
