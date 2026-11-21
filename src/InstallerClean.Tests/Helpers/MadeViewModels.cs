using InstallerClean.Helpers;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The view models a test made, and a crash-log folder of that test's own from the first one
/// it adds until it is disposed. A test reading <see cref="TestCrashLog.Text"/> after making
/// a view model sees only what its own work wrote. Disposing flushes the destination save
/// each view model may still be waiting to make, which otherwise lands about 400 ms later,
/// and then puts the previous folder back and deletes this one. Flushing is safe on a view
/// model a test has already disposed or flushed. The folder is the one
/// <see cref="CrashLog.FolderForTests"/> every test in the AppDomain shares, which is why the
/// tests run one at a time (AssemblyInfo.cs).
/// </summary>
internal sealed class MadeViewModels : IDisposable
{
    private readonly List<MainViewModel> _made = new();
    private IDisposable? _folder;

    /// <summary>
    /// Takes the test's folder at the first view model, so a test class whose constructor
    /// throws, and is therefore never disposed, has changed nothing.
    /// </summary>
    public MainViewModel Add(MainViewModel vm)
    {
        _folder ??= TestCrashLog.OwnFolder();
        _made.Add(vm);
        return vm;
    }

    public void Dispose()
    {
        try
        {
            foreach (var vm in _made)
                vm.Cleanup.FlushMoveDestination();
        }
        finally
        {
            _folder?.Dispose();
        }
    }
}
