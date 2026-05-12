using InstallerClean.Services;
using Xunit;

namespace InstallerClean.Tests.Services;

/// <summary>
/// Configuration-pin tests for SettingsService. The integration tests
/// in Services.Integration/SettingsServiceTests.cs cover behaviour
/// against the real filesystem; these tests pin the in-memory
/// JsonSerializerOptions that the elevated process uses to parse
/// settings.json so a refactor that drops the explicit options and
/// inherits the BCL defaults fails CI rather than silently weakening
/// the cap.
/// </summary>
public class SettingsServiceConfigTests
{
    [Fact]
    public void JsonOptions_caps_depth_at_8()
    {
        // The settings schema is a flat object; 8 covers the deepest
        // expected nesting plus headroom. Pinned to keep the cap
        // explicit; the BCL default is 64 which is too loose for an
        // elevated parse.
        Assert.Equal(8, SettingsService.JsonOptions.MaxDepth);
    }
}
