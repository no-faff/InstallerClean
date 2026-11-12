using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The system menu is shown with the flags .NET 10's SystemCommands passes
/// (dotnet/wpf v10.0.0, SystemCommands.cs): TPM_LEFTBUTTON | TPM_RIGHTBUTTON |
/// TPM_RETURNCMD. .NET Framework's passes TPM_RETURNCMD alone, 0x0100, with which the
/// right mouse button chooses nothing.
/// </summary>
public class SystemMenuTests
{
    [Fact]
    public void The_menu_returns_the_command_and_takes_either_mouse_button()
    {
        Assert.Equal(0x0102u, WindowChromeExtensions.SystemMenuFlags);
    }
}
