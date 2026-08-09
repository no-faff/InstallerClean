# InstallerClean privacy policy

Last updated 9 August 2026.

I don't find out anything about you or your files - unless you choose to send the totally optional anonymous report, which just lets me know it's working. No ads, no telemetry. Here is every time InstallerClean touches the network or writes anything down.

**Update check.** When you launch it, InstallerClean asks GitHub's releases page whether a newer version exists. That is one web request to github.com. It sends nothing about you or your machine beyond what any web request tells the server it contacts, which here means your IP address reaches GitHub, and one line naming InstallerClean and its version, the same line any browser sends to say what it is. It downloads nothing. If a newer version exists you get a line on screen with a link, and your browser opens only if you click it. You can turn the check off in the About screen, and it stays off.

**Optional report.** At the end of a scan you can press "Send report". It does nothing unless you press it, and before anything is sent it shows you the whole thing on screen, so this list is not the thing to trust, the dialog is. It goes to nofaff.netlify.app/api/result-log, a site of mine. What goes: the app version, which Windows you are on and whether it is 64-bit, how long the scan took, how many installer files were registered, orphaned, superseded, obsoleted or missing from disk, whether the run ended as a scan, a move or a delete, how many files it processed and how many failed, how much space was freed, a count of each kind of error if any happened, and whether a move went to the same drive, another drive, a removable drive or a network share. All of it is numbers and fixed labels. There are no file names, no paths and nothing that identifies you or your machine. Sending it tells my site what the version check tells GitHub, your IP address and the app's name and version, and nothing else. Send one and you are never asked again.

**Links.** Buttons that open the documentation, the project page or the donate page open your browser, and only when you click them.

**On your machine only.** If the app hits an error it writes a crash log locally to help with diagnosis. Your settings, including the backup folder and whether the update check is on, are saved locally. After every scan, move or delete, the figures for that run are written to a local file: that is the file the Send report button sends, and it is written whether you ever press the button or not. The command-line tool writes one summary line per run to the Windows Application event log, which is a normal Windows record and stays on the machine. None of this leaves your computer unless you choose to send it.

That is all of it. The source is public at [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean), so you can check every word of this for yourself.
