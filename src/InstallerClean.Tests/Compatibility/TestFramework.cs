using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using InstallerClean.Helpers;
using InstallerClean.Interop.Native;
using Microsoft.Win32.SafeHandles;

// The .NET 10 members the tests call that .NET Framework 4.8 does not have, each written
// to answer as the .NET 10 member does (dotnet/runtime v10.0.0) for the way the tests call
// it, in the namespace of the type it extends so each call binds here unchanged. The
// shipped projects' replacements are in InstallerClean.Core's Compatibility folder; these
// are the tests' own and nothing that ships calls them.

namespace System.IO
{
    internal static class TestFrameworkFile
    {
        private const uint SymbolicLinkFlagDirectory = 0x1;
        private const uint SymbolicLinkFlagAllowUnprivilegedCreate = 0x2;
        private const int ErrorInvalidParameter = 87;

        [DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool CreateSymbolicLinkW(string lpSymlinkFileName, string lpTargetFileName, uint dwFlags);

        extension(File)
        {
            /// <summary>Writes <paramref name="contents"/> as UTF-8 without a byte order mark, as .NET 10 does.</summary>
            public static Task WriteAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default) =>
                Run(() => File.WriteAllText(path, contents), cancellationToken);

            public static Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default) =>
                Run(() => File.WriteAllBytes(path, bytes), cancellationToken);

            public static Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
            {
                if (cancellationToken.IsCancellationRequested) return Task.FromCanceled<byte[]>(cancellationToken);
                try
                {
                    return Task.FromResult(File.ReadAllBytes(path));
                }
                catch (Exception ex)
                {
                    return Task.FromException<byte[]>(ex);
                }
            }

            /// <summary>
            /// A symbolic link at <paramref name="path"/> to the file <paramref name="pathToTarget"/>,
            /// made as .NET 10 makes one: allowed without elevation where Windows allows it, and
            /// tried again without that flag where Windows refuses the flag itself.
            /// </summary>
            public static FileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
            {
                var fullPath = Path.GetFullPath(path);
                MakeLink(path, pathToTarget, isDirectory: false);
                return new FileInfo(fullPath);
            }

            /// <summary>
            /// A handle to the file, opened as .NET 10's File.OpenHandle opens one: shared for
            /// reading, with no options.
            /// </summary>
            public static SafeFileHandle OpenHandle(string path, FileMode mode, FileAccess access)
            {
                uint desired = (access & FileAccess.Read) != 0 ? 0x80000000u : 0;
                if ((access & FileAccess.Write) != 0) desired |= 0x40000000u;
                var handle = Kernel32.CreateFile(Path.GetFullPath(path), desired, (uint)FileShare.Read,
                    IntPtr.Zero, (uint)mode, 0, IntPtr.Zero);
                if (handle.IsInvalid)
                {
                    var error = Marshal.GetLastWin32Error();
                    handle.Dispose();
                    throw ExceptionFor(error, path);
                }
                return handle;
            }
        }

        private static Task Run(Action write, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);
            try
            {
                write();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        internal static void MakeLink(string path, string pathToTarget, bool isDirectory)
        {
            var flags = isDirectory ? SymbolicLinkFlagDirectory : 0;
            if (CreateSymbolicLinkW(path, pathToTarget, flags | SymbolicLinkFlagAllowUnprivilegedCreate)) return;
            var error = Marshal.GetLastWin32Error();
            if (error == ErrorInvalidParameter)
            {
                if (CreateSymbolicLinkW(path, pathToTarget, flags)) return;
                error = Marshal.GetLastWin32Error();
            }
            throw ExceptionFor(error, path);
        }

        /// <summary>The exception .NET 10 throws for <paramref name="error"/> on <paramref name="path"/>.</summary>
        internal static Exception ExceptionFor(int error, string path) =>
            Win32Marshal.GetExceptionForWin32Error(error, path);
    }

    internal static class TestFrameworkDirectory
    {
        private const int ErrorAlreadyExists = 183;

        [DllImport("kernel32.dll", EntryPoint = "CreateDirectoryW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateDirectoryW(string lpPathName, IntPtr lpSecurityAttributes);

        extension(Directory)
        {
            public static FileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
            {
                var fullPath = Path.GetFullPath(path);
                TestFrameworkFile.MakeLink(path, pathToTarget, isDirectory: true);
                return new DirectoryInfo(fullPath);
            }

            /// <summary>
            /// A new, empty folder in the temporary folder, named <paramref name="prefix"/> and
            /// eight random characters, made as .NET 10 makes one: a name already taken is never
            /// reused, and another is tried.
            /// </summary>
            public static DirectoryInfo CreateTempSubdirectory(string? prefix = null)
            {
                var tempPath = Path.GetTempPath();
                while (true)
                {
                    var path = tempPath + prefix + Path.GetRandomFileName().Replace(".", "").Substring(0, 8);
                    if (CreateDirectoryW(path, IntPtr.Zero)) return new DirectoryInfo(path);
                    var error = Marshal.GetLastWin32Error();
                    if (error != ErrorAlreadyExists) throw TestFrameworkFile.ExceptionFor(error, path);
                }
            }
        }

    }
}

namespace System.Threading.Tasks
{
    internal static class TestFrameworkTask
    {
        extension(Task task)
        {
            /// <summary>True where the task ran to completion, as .NET 10's property answers.</summary>
            public bool IsCompletedSuccessfully => task.Status == TaskStatus.RanToCompletion;

            /// <summary>
            /// The task, or a TimeoutException once <paramref name="timeout"/> has passed
            /// without it completing, as .NET 10's Task.WaitAsync answers.
            /// </summary>
            public Task WaitAsync(TimeSpan timeout) => WaitFor(task, timeout);
        }

        extension<TResult>(Task<TResult> task)
        {
            public Task<TResult> WaitAsync(TimeSpan timeout) => WaitForResult(task, timeout);
        }

        private static async Task WaitFor(Task task, TimeSpan timeout)
        {
            if (!task.IsCompleted)
            {
                using var stop = new CancellationTokenSource();
                if (await Task.WhenAny(task, FrameworkTask.Delay(timeout, stop.Token)).ConfigureAwait(false) != task)
                    throw new TimeoutException();
                stop.Cancel();
            }
            await task.ConfigureAwait(false);
        }

        private static async Task<TResult> WaitForResult<TResult>(Task<TResult> task, TimeSpan timeout)
        {
            await WaitFor(task, timeout).ConfigureAwait(false);
            return task.Result;
        }
    }
}

namespace System.Diagnostics
{
    internal static class TestFrameworkProcess
    {
        extension(Process process)
        {
            /// <summary>
            /// Completes when the process exits, or is cancelled by <paramref name="cancellationToken"/>,
            /// as .NET 10's WaitForExitAsync does: through the Exited event, then a wait that
            /// lets redirected output finish.
            /// </summary>
            public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                process.EnableRaisingEvents = true;
                var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                EventHandler handler = (_, _) => exited.TrySetResult(true);
                process.Exited += handler;
                try
                {
                    if (!process.HasExited)
                    {
                        using (cancellationToken.Register(() => exited.TrySetCanceled(cancellationToken)))
                            await exited.Task.ConfigureAwait(false);
                    }
                }
                finally
                {
                    process.Exited -= handler;
                }
                process.WaitForExit();
            }

            /// <summary>
            /// Waits up to <paramref name="timeout"/>, in whole milliseconds, refusing a span
            /// outside -1 to int.MaxValue milliseconds as .NET 10 does.
            /// </summary>
            public bool WaitForExit(TimeSpan timeout)
            {
                var milliseconds = (long)timeout.TotalMilliseconds;
                if (milliseconds < -1 || milliseconds > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(timeout));
                return process.WaitForExit((int)milliseconds);
            }
        }
    }
}

namespace System.Linq
{
    internal static class TestFrameworkEnumerable
    {
        extension<T>(IEnumerable<T> source)
        {
            /// <summary>The elements in ascending order, stable, as .NET 10's Order sorts them.</summary>
            public IOrderedEnumerable<T> Order() => source.OrderBy(static x => x);

            public IOrderedEnumerable<T> Order(IComparer<T>? comparer) => source.OrderBy(static x => x, comparer);

            /// <summary>The elements of both sequences in pairs, as far as the shorter goes, as .NET 10's Zip pairs them.</summary>
            public IEnumerable<(T First, TSecond Second)> Zip<TSecond>(IEnumerable<TSecond> second) =>
                source.Zip(second, static (a, b) => (a, b));
        }
    }
}

namespace System
{
    internal static class TestFrameworkSystem
    {
        extension(Enum)
        {
            public static TEnum Parse<TEnum>(string value) where TEnum : struct, Enum =>
                (TEnum)Enum.Parse(typeof(TEnum), value);

            public static string[] GetNames<TEnum>() where TEnum : struct, Enum => Enum.GetNames(typeof(TEnum));
        }

        extension<T>(T[] array)
        {
            /// <summary>
            /// A copy of the part of the array <paramref name="range"/> names, of the array's own
            /// element type, as .NET 10 makes for <c>array[range]</c>.
            /// </summary>
            public T[] Slice(Range range)
            {
                var (offset, length) = range.GetOffsetAndLength(array.Length);
                T[] dest;
                if (typeof(T[]) == array.GetType())
                {
                    if (length == 0) return Array.Empty<T>();
                    dest = new T[length];
                }
                else
                {
                    dest = (T[])Array.CreateInstance(array.GetType().GetElementType()!, length);
                }
                Array.Copy(array, offset, dest, 0, length);
                return dest;
            }
        }

        extension(Guid guid)
        {
            /// <summary>The version: the top four bits of the seventh byte, as .NET 10 reads it.</summary>
            public int Version => guid.ToByteArray()[7] >> 4;

            /// <summary>The variant: the top four bits of the ninth byte, as .NET 10 reads it.</summary>
            public int Variant => guid.ToByteArray()[8] >> 4;
        }
    }
}

namespace System.Runtime.InteropServices
{
    /// <summary>The two NativeLibrary members the tests call, as .NET 10 has them on Windows.</summary>
    internal static class NativeLibrary
    {
        private const uint LoadWithAlteredSearchPath = 0x8;

        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr LoadLibraryExW(string lpLibFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hLibModule);

        /// <summary>Loads the library at a full path, its dependencies searched for beside it.</summary>
        public static IntPtr Load(string libraryPath)
        {
            var module = LoadLibraryExW(libraryPath,
                IntPtr.Zero, Path.IsPathRooted(libraryPath) ? LoadWithAlteredSearchPath : 0);
            if (module == IntPtr.Zero)
                throw new DllNotFoundException($"Unable to load DLL '{libraryPath}': {new ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message}");
            return module;
        }

        public static void Free(IntPtr handle)
        {
            if (handle != IntPtr.Zero) FreeLibrary(handle);
        }
    }
}
