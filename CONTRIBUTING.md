# Contributing to InstallerClean

Thanks for your interest in contributing. InstallerClean is Apache-2.0 licensed
and welcomes pull requests.

## Build and test

Every project targets .NET Framework 4.8, which Windows 10 and 11 already have.
Building needs the .NET SDK that `global.json` names.

```
dotnet build src/InstallerClean.sln
dotnet test src/InstallerClean.Tests/
```

Build the whole solution, not a single project. The solution holds four projects
and the test project references the other three, so building the solution is the
same work, and it stays right if a reference ever changes.

On Linux, add `-p:EnableWindowsTargeting=true` to the build. The tests and the
app itself run on Windows. CI builds and runs the whole suite on Windows for
every pull request.

The app requires **administrator privileges** to run because it accesses
`C:\Windows\Installer` and the Windows Installer API. You can run it from an
elevated terminal with `dotnet run --project src/InstallerClean` or launch the
built exe (which triggers a UAC prompt).

### CLI

`installerclean-cli.exe` is a .NET Framework 4.8 console exe built from
`src/InstallerClean.Cli`, with the rest of the solution. No external toolchain
(mingw etc) is required.

### The downloads

Each download is one exe. A Release build with `-p:OneFile=true` merges every
assembly the exe references into it with ILRepack:

```
dotnet build src/InstallerClean/InstallerClean.csproj -c Release -p:OneFile=true
dotnet build src/InstallerClean.Cli/InstallerClean.Cli.csproj -c Release -p:OneFile=true
```

These write `publish/window/InstallerClean.exe` and
`publish/cli/installerclean-cli.exe`. The release build,
`.github/workflows/release.yml`, runs the same two commands and compiles the
Inno Setup installer around the two exes.

## Commit conventions

Use a prefix: `feat:` / `fix:` / `refactor:` / `perf:` / `test:` / `docs:` /
`i18n:` / `ci:` / `build:` / `chore:`

Run `dotnet build`, and on Windows `dotnet test`, before committing.

## Filing issues

If you find a bug or have a feature idea, open an issue. Please include:

- What you expected to happen
- What actually happened
- Your Windows version, the InstallerClean version from the About window, and
  which download you are running (setup, portable or the command-line tool)

## Translations

The app, the installer and the command-line tool are translated into every
language in the app's language menu, and the README is in each of those and in
Arabic too.

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

### Changing the English, or adding a language

The English lives in `src/InstallerClean.Core/Resources/Strings.resx`.
`scripts/translations/gen-strings-template.mjs` holds a copy of it, and a new
language starts as a copy of that file, so the copy has to keep up. After
rewording a string or adding one, run this with the key of each string you
changed:

```
node scripts/refresh-template-english.mjs <Key.Name>
```

`--list` names every key the template has not caught
up with, and changes nothing. CI runs every generator in `scripts/translations/`
on each push and fails unless the template's run ends `TEMPLATE OK`.

For a new language, copy `gen-strings-template.mjs` to
`gen-strings-<code>.mjs`. Its opening comment walks through the rest:
pointing it at the new `Strings.<code>.resx`, translating its values, and
running it until it ends `GENERATION OK`.

## Pull requests

- Keep PRs focused on a single change
- Include a short description of what the PR does and why
- Make sure the build and tests pass

All contributions are appreciated.
