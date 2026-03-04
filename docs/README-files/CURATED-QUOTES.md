# Curated forum quotes for landing page and marketing

Collected from ~20 Reddit threads, SuperUser posts and Microsoft Answers threads.
For use on the Netlify site, social media, README updates.

Screenshots of many of these posts are saved alongside the text files in this folder.

---

## The frustration (people who know the problem and can't find help)

> "All of the threads I've found tend to recommend the same things which don't solve the problem, and then go dead."
>
> ksparks519, r/Windows10 (2y ago, 8 upvotes) — **USED IN README**

> "180 gigs on a 240 gb drive... only 700mb is left while windows installer folder has occupied 180gb. i did clean disk but it only cleared 600mb."
>
> Anonymous, [Microsoft Answers](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb) — **180GB thread, LINKED IN README**

> "I ran different tools, like CCleaner, Windows Disk Cleanup and so on, they manage to remove a total of 1 GB of space, none of it from these folders."
>
> and69, r/Windows10 (5y ago) — 55 GB Installer folder, tried everything

> "It's been a consistent problem for me for years, and right now my windows installer folder is over 50gb."
>
> ksparks519, r/Windows10 (same thread as the "go dead" quote)

> "Thankfully the know-it-all mod jagoffs here didn't delete this question! It's literally the only helpful answer to this question I have ever found!"
>
> SamAndrew81, SuperUser (2017) — referring to the PatchCleaner answer

> "WHY HASN'T MICROSOFT COME UP WITH A SOLUTION TO THIS PROBLEM?"
>
> Anonymous, Microsoft Answers (2018) — exact phrasing from a Microsoft Answers post

> "it only removed like 4Gb when I ran it as admin, I still have like 56Gb in the installer dir..."
>
> The-tridents-fate, r/techsupport (1y ago) — after running Disk Cleanup

> "Mine is 18GB and Disk Cleanup doesn't touch it"
>
> RaffScallionn, r/techsupport (2mo ago)

---

## The wrong advice (confidently wrong, well-meaning people)

> "don't mess with it." / "what should I do then?" / "I just told you."
>
> CanadianTimeWaster + The-tridents-fate, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1hw4suq/) — **USED IN README**

> "what, exactly, makes you think the windows operating system shouldn't take up 60 gigs of storage"
>
> MadisonDissariya, r/techsupport — confusing one cache folder with the entire OS

> "how big do you think a whole operating system is??? don't touch it or you will most likely brick you're os"
>
> Ogga6165, r/techsupport — same thread, same confusion

> "The official Microsoft stance is leave it the hell alone. Far safer to just use the built in windows disk cleanup utility."
>
> DrDan21, r/WindowsHelp (6mo ago, 7 upvotes) — Disk Cleanup doesn't touch this folder

> "No. Never safe to delete things in the windows folder."
>
> tlasan1, r/techsupport (9mo ago)

> "Your installations are broken by this. Any diff update will crash."
>
> puppy2016, r/Windows10 (9y ago) — about PatchCleaner. Replied to by SurfaceDockGuy who correctly explains how it works.

> "This is what Disk cleanup (System) do without any other damage."
>
> puppy2016, r/Windows10 — same thread. Corrected by SurfaceDockGuy: "The system option in disk cleanup does not remove all the superseded msi in \installer"

> "The proper way to alleviate space pressure in this directory is to uninstall any unneeded applications."
>
> joscon (Microsoft employee), TechNet — quoted on SuperUser. Technically true but unhelpful when the files are from software already uninstalled.

> "I wouldn't recommend it. Sounds like a CCleaner rip-off which doesn't do much anyway."
>
> Th4tBriti5hGuy, r/techsupport — about PatchCleaner. Hadn't heard of it and assumed it was malware.

---

## Real-world folder sizes

| Size | Source | Platform |
|------|--------|----------|
| 273 GB | CaptainFedora21, r/Windows11 | 1,133 files |
| 270 GB | coolbop32, r/sysadmin | Adobe files |
| 215 GB | Particular_Wish6133, r/WindowsHelp (24 Feb 2026) | "Each file about 1.2 gigs" |
| 180 GB | Microsoft Answers (2021) | 240 GB drive, "only 700mb left" |
| 100 GB+ | Loof27, r/sysadmin (87 upvotes) | "countless computers... sometimes in the 100s" |
| 100 GB | Correct_Substance261, r/sysadmin | Requesting cleanup script |
| 90 GB | Microsoft Answers | "Does Microsoft have a safe [solution]?" |
| 60 GB | The-tridents-fate, r/techsupport | PatchCleaner flagged on VirusTotal |
| 55 GB | and69, r/Windows10 | 256 GB SSD, "20% of my disk" |
| 52 GB | GeekOnDemand007, r/techsupport | Moved Adobe files with PatchCleaner |
| 50 GB | ksparks519, r/Windows10 | "consistent problem for years" |
| 35 GB | Microsoft Answers (2021) | "35GB is the total size of my Windows folder" |
| 30 GB+ | vawlk, r/SCCM | "some machines with over 30GB" |
| 30 GB | HeatherBunny1111, r/techsupport (2mo ago) | "29GB excluded by filters" |
| 23 GB | Vadzim, SuperUser | "reduced from 23 to 3 GB" |
| 20 GB | Sunny2456, r/Windows10 (9y ago) | PatchCleaner success: 29 GB to 12 GB |

---

## Adobe Acrobat (the #1 pain point)

> "Always enraged by the 1.1GB .msp patch files there! Why, Adobe! Why!!!"
>
> tjone270, r/sysadmin (22 upvotes)

> "I've had this issue on countless computers. The drive is full, I check what is taking up the space, and its always a 50GB+ C:\Windows\Installer folder, sometimes in the 100s. All I have to do is uninstall Acrobat and instantly the folder goes down to ~5GB"
>
> Loof27, r/sysadmin (87 upvotes) — **THREAD LINKED IN README**

> "just freed up 14gb uninstalling the free Adobe Acrobat. Wow, what a trash ass patch management process by Adobe."
>
> yapper604, r/techsupport (2mo ago)

> "I know something like this exists already called PatchCleaner, but it specifically has a filter to ignore all Acrobat files since they were incorrectly identified as orphaned"
>
> Loof27, r/sysadmin (OP of the 87-upvote thread)

> "Adobe, enough said."
>
> Kurgan_IT, r/sysadmin

> "I've downloaded Patchcleaner to delete the orphaned .msp files... 29GB of the files are 'excluded by filters', so Patchcleaner doesn't seem to help."
>
> HeatherBunny1111, r/techsupport — **USED IN README**

> "Adobe Reader patches are detected incorrectly as orphaned files."
>
> PatchCleaner known issues page, quoted by Xanta91 on r/SCCM

> "PatchCleaner only deleted 480 MB"
>
> Common complaint across threads — because Adobe files are excluded

**The DM chain (r/sysadmin, 4mo ago):** Gorilla-P mentions having a cleanup script. Then roughly 10 people ask for it in replies and DMs: RemoveGlass1782, Jtrickz, grunzt, sysadm145, OutrageousAnteater38, Correct_Substance261, Klownicle, RecentPromotion210, fancy-caboodle, Nearby_Sink7122, Dereksversion, dogcatchersito86. Shows massive unmet demand. grunzt even asks: *"Would you mind sharing it, as the author didn't provide it to all requesters?"*

---

## PatchCleaner (respect + limitations)

> "I like this piece of abandoned software"
>
> Anonymous, recommending PatchCleaner in 2025

> "PatchCleaner has not been updated in several years and more recent reports indicate it is no longer as accurate as it once was at correctly identifying no-longer-required patches."
>
> music2myear, SuperUser (2021)

> "my windows installer folder is like 60gb so I wanna clean it, and it seems like patch-cleaner is my only option... but virus total flagged it as 4/72, so I don't wanna take that risk"
>
> The-tridents-fate, r/techsupport — screenshot of VirusTotal result saved in this folder

> "Just not comfortable using an unsigned/closed/abandonware program."
>
> WordsByCampbell, r/sysadmin (34 upvotes) — fleet of 7,000 machines, 15-40 GB cleanup each

> "It doesn't look like it has been under active development for nearly a decade."
>
> mrmattipants, r/sysadmin

> Developer jcrawfor74 commented on SuperUser in November 2016 planning a portable version and investigating a Silverlight bug. No updates since. The tool last shipped 3 March 2016.

---

## The "dead weight" angle (files that don't come back)

> "I stopped bothering with clearing cache and temp files etc years ago. Most of the time programs just recreate this stuff right away anyway."
>
> gbroon, r/Windows11

This is the perfect setup for our counter: "The files InstallerClean removes are dead weight. They don't come back."

---

## Enterprise / sysadmin angle

> "Fleet of 7k win7 machines that occasionally bloat with patches and we see a safe 15-40gb cleanup using [PatchCleaner]. Just not comfortable using an unsigned/closed/abandonware program."
>
> WordsByCampbell, r/sysadmin (34 upvotes)

> "we had to create a script to clean these up without removing necessary .MSP files."
>
> Gorilla-P, r/sysadmin — followed by chain of ~10 people begging for the script

> "My Acrobat update workflow includes a script which removes all previous versions before installing the newest, just because of this."
>
> MrReed_06, r/sysadmin

> InstallerClean CLI (`/d`, `/m`, `/m PATH`) makes it scriptable for fleet deployment without needing custom PowerShell scripts.

---

## People who solved it (potential testimonial format for landing page)

> "PatchCleaner took about 20 minutes to parse the 30 GB of Windows Installer cache (seriously, MSFT, wtf) and then with one click was able to free up 20 GB. Bravo."
>
> GraehamF, SuperUser — about PatchCleaner, but shows the experience we want to deliver

> "PatchCleaner just clawed back 32Gb in unrequired patches. That's not only amazing but means I don't have to find thousands of dollars to buy a new mobile development environment."
>
> Zodman, SuperUser (2017) — Surface Pro 1, 128 GB drive

> "Reduced my C:\Windows\Installer folder by 17gigs with PatchCleaner."
>
> Sunny2456, r/Windows10 (9y ago, 6 upvotes)

---

## Useful context for landing page copy

**Why Disk Cleanup doesn't work here:**
Disk Cleanup (`cleanmgr.exe`) handles Windows Update files (via DISM under the hood), temp files, thumbnails, etc. It does NOT touch `C:\Windows\Installer`. This is confirmed by multiple users across all threads: running Disk Cleanup on a 180 GB Installer folder freed 600 MB (from other locations, not Installer). DISM `/Cleanup-Image /StartComponentCleanup` is for `WinSxS`, not `C:\Windows\Installer`.

**Why "don't touch it" is half-right:**
The advice to not manually delete files from C:\Windows\Installer IS correct. Randomly deleting files can break software installation, patching and uninstallation. But removing *orphaned* files (no product references them) and *superseded patches* (replaced by newer versions) is different. The Windows Installer API provides exactly the data needed to distinguish the two.

**The SurfaceDockGuy correction (r/Windows10, 9y ago):**
When puppy2016 claimed "This is what Disk cleanup (System) do without any other damage", SurfaceDockGuy replied: *"The system option in disk cleanup does not remove all the superseded msi in \installer nor does it consistently remove superseded bits from \winsxs"* (5 upvotes). This is someone who understands the difference.

**The problem has existed for 16+ years:**
The earliest SuperUser question about C:\Windows\Installer is from August 2009. Same question, same wrong answers, for over 16 years.
