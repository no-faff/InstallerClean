# InstallerClean launch strategy

## The opportunity

PatchCleaner (homedev.com.au) is the only well-known tool for cleaning
C:\Windows\Installer. It was last updated 3 March 2016 (v1.4.2.0) and still
gets 7,233 downloads/week on SourceForge after 10 years (as of March 2026).
~800,000 total downloads.

InstallerClean launches 3 March 2026 — exactly 10 years later.

**PatchCleaner's weaknesses (our advantages):**
- Not updated since 2016
- Closed source
- Depends on VBScript (deprecated in Windows 11 24H2)
- Built for Windows 7/8 era
- No dark mode, dated UI

**Our story:** Open source, modern replacement. Built for Windows 11.
MIT licence. No VBScript. Actively maintained.

## How people find PatchCleaner (and how they'll find us)

Typical discovery path:
1. Run out of disk space
2. Use WizTree/WinDirStat/TreeSize to find what's big
3. Find C:\Windows\Installer is huge
4. Google "C:\Windows\Installer" or "C:\Windows\Installer safe to delete"
5. Find PatchCleaner

We need to appear at step 5.

## Launch plan

### 1. GitHub README (before launch)

The README is the landing page. It needs to mention:
- "C:\Windows\Installer" (the search term people use)
- "PatchCleaner" (people search for alternatives)
- "orphaned files", "safe to delete", "free up space"
- Screenshots
- One-click download link

GitHub repos rank well on Google. A good README with the right keywords
will get indexed quickly.

### 2. SourceForge listing

PatchCleaner gets 5,000 downloads/week from SourceForge. Submit
InstallerClean there too. People browsing PatchCleaner's page may see
alternatives. SourceForge accepts submissions via a form.

### 3. Reddit posts (launch day or soon after)

Target subreddits:
- **r/sysadmin** — they recommend PatchCleaner constantly. An open source,
  maintained alternative will get attention. These people share tools at work.
- **r/software** — "I built an open source replacement for PatchCleaner"
- **r/Windows10** / **r/Windows11** — "how to clean C:\Windows\Installer safely"
- **r/DataHoarder** — disk space obsessives
- **r/opensource** — if it's genuinely good, they'll appreciate it

A genuine "I built this" post works best. Not spammy, just honest:
"PatchCleaner hasn't been updated in 10 years. I built a modern replacement."

### 4. Software directories

Submit to:
- SourceForge
- Fosshub (open source focus)
- MajorGeeks
- AlternativeTo.net (list as alternative to PatchCleaner)
- GitHub Topics (tag the repo: windows, installer, cleanup, disk-space)

### 5. Blog post / write-up (optional but high value)

A single page explaining the C:\Windows\Installer problem and the tool.
Good for SEO. Could go on the no-faff site or GitHub Pages.

Target phrases:
- "C:\Windows\Installer safe to delete"
- "C:\Windows\Installer cleanup tool"
- "PatchCleaner alternative 2026"
- "Windows Installer folder too big"
- "free up space Windows Installer"

## Key links

- PatchCleaner homepage: https://www.homedev.com.au/Free/PatchCleaner
- PatchCleaner on SourceForge: https://sourceforge.net/projects/patchcleaner/
- Last release: v1.4.2.0, 3 March 2016

## What NOT to do

- Don't trash PatchCleaner. It works. Ours is just newer and maintained.
- Don't spam. One post per subreddit, genuine tone.
- Don't launch without screenshots in the README.

## Timeline

- Before 3 March: README with screenshots, download link
- 3 March: Launch. Reddit post in r/sysadmin. Submit to AlternativeTo.
- Week after: SourceForge, Fosshub, MajorGeeks submissions.
- Ongoing: Answer questions in threads about C:\Windows\Installer.
