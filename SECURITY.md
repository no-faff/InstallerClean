# Security policy

## Supported versions

Only the latest release gets fixes. Older versions are not patched. Check the [releases page](../../releases) for what's current.

## Reporting a vulnerability

Please do **not** open a public GitHub issue for security problems. [Open a private security advisory](../../security/advisories/new) with:

- A description of the issue
- Steps to reproduce
- Your InstallerClean version and Windows version

I'll confirm I've received it, keep you posted on the fix and credit you in the release notes if you'd like.

## Scope

InstallerClean runs with administrator privileges and touches the Windows Installer database. Of particular interest:

- Anything that could cause Move or Delete to act on files outside `C:\Windows\Installer` or its subfolders
- Anything that could cause a registered file to be flagged as removable
- Path traversal or TOCTOU issues around the Move destination

Out of scope:

- Windows SmartScreen warnings or antivirus heuristic flags on unsigned binaries (see the [releases page](../../releases) for VirusTotal scan hashes)
- Issues that require prior administrator access to exploit
