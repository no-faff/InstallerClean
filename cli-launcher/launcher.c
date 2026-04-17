/*
 * InstallerClean CLI launcher.
 *
 * InstallerClean.exe is a WPF WinExe (PE subsystem 2). When a WinExe is
 * started from PowerShell or cmd, the shell does not wait for it: the
 * prompt returns immediately and the exe's console output interleaves
 * with whatever the shell wrote next. That makes /s, /d, /m unusable as
 * real CLI commands.
 *
 * This launcher is a Console-subsystem stub (PE subsystem 3). When you
 * type `installerclean-cli /s` the shell waits for THIS process, which
 * CreateProcessW's the main WinExe and blocks until it exits. Output
 * flows through the current console cleanly and the exit code
 * propagates.
 *
 * Build (Linux):
 *   x86_64-w64-mingw32-gcc -O2 -s -municode -Wall -Wextra \
 *     -o InstallerClean-cli.exe launcher.c
 *
 * The build produces a deterministic ~20 KB static exe with no runtime
 * dependency on MSVCRT or any Visual C++ redistributable.
 */

#include <windows.h>
#include <stdio.h>
#include <wchar.h>

/*
 * Windows command-line quoting per
 *   https://learn.microsoft.com/en-us/cpp/cpp/main-function-command-line-args
 * and
 *   https://learn.microsoft.com/en-us/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way
 *
 * Rules:
 *  - If the argument has no spaces, tabs or quotes, append as-is.
 *  - Otherwise wrap in quotes.
 *  - Backslashes followed by a quote double; backslashes immediately
 *    before the closing quote double; other backslashes are literal.
 *  - Embedded quotes become \" with preceding backslashes doubled.
 */
static void quote_into(wchar_t *dst, size_t cap, const wchar_t *src)
{
    size_t dlen = wcslen(dst);
    if (dlen >= cap) return;

    int needs_quoting = (*src == L'\0');
    for (const wchar_t *p = src; *p; p++) {
        if (*p == L' ' || *p == L'\t' || *p == L'"') { needs_quoting = 1; break; }
    }

    if (!needs_quoting) {
        size_t slen = wcslen(src);
        if (dlen + slen >= cap) return;
        wmemcpy(dst + dlen, src, slen);
        dst[dlen + slen] = L'\0';
        return;
    }

    if (dlen + 1 >= cap) return;
    dst[dlen++] = L'"';

    for (const wchar_t *p = src; *p; ) {
        int backslashes = 0;
        while (*p == L'\\') { backslashes++; p++; }

        if (*p == L'\0') {
            for (int i = 0; i < backslashes * 2; i++) {
                if (dlen + 1 >= cap) return;
                dst[dlen++] = L'\\';
            }
            break;
        } else if (*p == L'"') {
            for (int i = 0; i < backslashes * 2 + 1; i++) {
                if (dlen + 1 >= cap) return;
                dst[dlen++] = L'\\';
            }
            if (dlen + 1 >= cap) return;
            dst[dlen++] = L'"';
            p++;
        } else {
            for (int i = 0; i < backslashes; i++) {
                if (dlen + 1 >= cap) return;
                dst[dlen++] = L'\\';
            }
            if (dlen + 1 >= cap) return;
            dst[dlen++] = *p++;
        }
    }

    if (dlen + 1 >= cap) return;
    dst[dlen++] = L'"';
    dst[dlen] = L'\0';
}

int wmain(int argc, wchar_t **argv)
{
    wchar_t own_path[MAX_PATH];
    DWORD len = GetModuleFileNameW(NULL, own_path, MAX_PATH);
    if (len == 0 || len >= MAX_PATH) {
        fwprintf(stderr, L"installerclean-cli: cannot resolve launcher path\n");
        return 2;
    }

    wchar_t *sep = wcsrchr(own_path, L'\\');
    if (!sep) {
        fwprintf(stderr, L"installerclean-cli: launcher path has no directory\n");
        return 2;
    }
    *(sep + 1) = L'\0';

    wchar_t target[MAX_PATH];
    if (_snwprintf(target, MAX_PATH, L"%sInstallerClean.exe", own_path) < 0) {
        fwprintf(stderr, L"installerclean-cli: target path too long\n");
        return 2;
    }

    static wchar_t cmdline[32768];
    cmdline[0] = L'\0';
    quote_into(cmdline, sizeof(cmdline) / sizeof(wchar_t), target);
    for (int i = 1; i < argc; i++) {
        size_t dlen = wcslen(cmdline);
        if (dlen + 1 >= sizeof(cmdline) / sizeof(wchar_t)) break;
        cmdline[dlen] = L' ';
        cmdline[dlen + 1] = L'\0';
        quote_into(cmdline, sizeof(cmdline) / sizeof(wchar_t), argv[i]);
    }

    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    /* bInheritHandles = FALSE so a future file or socket opened here
     * cannot leak into the child WinExe. */
    BOOL ok = CreateProcessW(
        target,
        cmdline,
        NULL, NULL,
        FALSE,
        0,
        NULL, NULL,
        &si, &pi);

    if (!ok) {
        DWORD err = GetLastError();
        fwprintf(stderr, L"installerclean-cli: cannot launch %ls (error %lu)\n", target, err);
        return 2;
    }

    CloseHandle(pi.hThread);
    WaitForSingleObject(pi.hProcess, INFINITE);

    DWORD exit_code = 1;
    GetExitCodeProcess(pi.hProcess, &exit_code);
    CloseHandle(pi.hProcess);
    return (int)exit_code;
}
