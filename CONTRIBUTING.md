# Contributing to InstallerClean

Thanks for your interest in contributing. InstallerClean is MIT-licensed and
welcomes pull requests.

## Build and test

```
dotnet build src/InstallerClean.sln
dotnet test src/InstallerClean.Tests/
```

Build the whole solution, not a single project: the test project does not
reference the CLI, so a CLI-breaking change can build and test clean from
the GUI project alone.

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
- Your Windows version and .NET version (`dotnet --version`)

## Pull requests

- Keep PRs focused on a single change
- Include a short description of what the PR does and why
- Make sure the build and tests pass

All contributions are appreciated.
