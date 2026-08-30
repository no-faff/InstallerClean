# Contributing to InstallerClean

Thanks for your interest in contributing. InstallerClean is Apache-2.0 licensed
and welcomes pull requests.

## Build and test

```
dotnet build src/InstallerClean.sln
dotnet test src/InstallerClean.Tests/
```

Build the whole solution, not a single project. The solution holds four projects
and the test project references the other three, so building the solution is the
same work, and it stays right if a reference ever changes.

The app requires **administrator privileges** to run because it accesses
`C:\Windows\Installer` and the Windows Installer API. You can run it from an
elevated terminal with `dotnet run --project src/InstallerClean` or launch the
built exe (which triggers a UAC prompt).

### CLI

`installerclean-cli.exe` is a real .NET 10 console exe published from
`src/InstallerClean.Cli`. It builds with the rest of the solution
(`dotnet build src/InstallerClean.sln`) and is bundled into the
Inno-built setup.exe by the Stage-1 release script. No external
toolchain (mingw etc) is required.

## Commit conventions

Use a prefix: `feat:` / `fix:` / `refactor:` / `chore:` / `test:` / `docs:`

Always run both `dotnet build` and `dotnet test` before committing.

## Filing issues

If you find a bug or have a feature idea, open an issue. Please include:

- What you expected to happen
- What actually happened
- Your Windows version, the InstallerClean version from the About window, and
  which download you are running (setup, portable or the command-line tool)

## Translations

InstallerClean ships in 16 languages: the app, the installer and the command-line
tool. The README is in 17, those 16 and Arabic.

To read a translation, the string tables in
[`docs/translations/`](docs/translations/) show every line in English beside each
language. Those pages are generated, though, so a change made there can't be
used. The translations themselves live in
[`scripts/translations/`](scripts/translations/), one file per language, and
that's the one to edit.

Open the file for your language, find the English line you want to fix, and
change the words next to it. Leave the rest of the file alone, since that's the
machinery that builds the app's string files. Then open a pull request, with as
few or as many changes as you like.

What I'll do with it: I'll merge it first, and then if I disagree with anything
I'll change it in a commit of my own afterwards, rather than editing your work
before it lands. That way the history shows what you wrote and what I changed,
and you can tell me if I've got it wrong. I'd rather do it that way round than
quietly rewrite you.

If you'd rather not use git at all, a
[translation-feedback issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md)
or a [discussion](../../discussions) is just as welcome.

## Pull requests

- Keep PRs focused on a single change
- Include a short description of what the PR does and why
- Make sure the build and tests pass

All contributions are appreciated.
