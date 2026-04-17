# Contributing to InstallerClean

Thanks for your interest in contributing. InstallerClean is MIT-licensed and
welcomes pull requests.

## Build and test

```
dotnet build src/InstallerClean/InstallerClean.csproj
dotnet test src/InstallerClean.Tests/
```

The app requires **administrator privileges** to run because it accesses
`C:\Windows\Installer` and the Windows Installer API. You can run it from an
elevated terminal with `dotnet run --project src/InstallerClean` or launch the
built exe (which triggers a UAC prompt).

### CLI launcher

`InstallerClean-cli.exe` in the installer is a small console stub that the
Windows Installer builds from `cli-launcher/launcher.c`. The binary is
committed to the repo because rebuilding it requires mingw-w64, which most
contributors don't need.

To rebuild it on Linux:

```
sudo dnf install mingw64-gcc   # Fedora; use your distro's equivalent
cli-launcher/build.sh
```

The resulting `cli-launcher/InstallerClean-cli.exe` is deterministic and
approximately 44 KB.

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
