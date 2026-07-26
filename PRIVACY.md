# InstallerClean privacy policy

Last updated 26 July 2026.

InstallerClean has no analytics, no tracking and no accounts. It runs on your machine and keeps to itself. Here is every time it touches the network or writes anything down.

**Update check.** When you launch it, InstallerClean asks GitHub's releases page whether a newer version exists. That is one web request to github.com. It sends nothing about you or your machine beyond what any web request tells the server it contacts, which here means your IP address reaches GitHub, and it downloads nothing. If a newer version exists you get a line on screen with a link, and your browser opens only if you click it. You can turn the check off in the About screen, and it stays off.

**Optional report.** After a scan you can press "Send report". It does nothing unless you press it, and before anything is sent it shows you exactly what would go: the app version, your Windows version, how many files were found and processed, and how much space was freed. There are no file names, no paths and nothing that identifies you or your machine. Nothing is sent unless you press Send.

**Links.** Buttons that open the documentation, the project page or the donate page open your browser, and only when you click them.

**On your machine only.** If the app hits an error it writes a crash log locally to help with diagnosis. Your settings, including the move location and whether the update check is on, are saved locally. If you send a report, a copy of its figures is kept in a local log. The command-line tool writes one summary line per run to the Windows Application event log, which is a normal Windows record and stays on the machine. None of this leaves your computer unless you choose to send it.

That is all of it. The source is public at [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean), so you can check every word of this for yourself.
