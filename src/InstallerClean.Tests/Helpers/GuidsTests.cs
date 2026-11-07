using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Guids hands back the GUID Windows made where CoCreateGuid returns zero, and throws an
/// <see cref="Exception"/> carrying the HRESULT for any other answer, as .NET 10's
/// Guid.NewGuid does: a plain Exception, with .NET 10's English message. A stand-in plays
/// Windows, so these run on any host.
/// </summary>
public class GuidsTests
{
    private const int S_FALSE = 1;
    private const int E_FAIL = unchecked((int)0x80004005);
    private const int E_OUTOFMEMORY = unchecked((int)0x8007000E);

    [Fact]
    public void On_zero_the_answer_is_the_GUID_Windows_made()
    {
        var made = new Guid("3F2504E0-4F89-41D3-9A0C-0305E82C3301");

        var guid = Guids.NewGuid((out Guid g) => { g = made; return 0; });

        Assert.Equal(made, guid);
    }

    [Theory]
    [InlineData(S_FALSE)]
    [InlineData(E_FAIL)]
    [InlineData(E_OUTOFMEMORY)]
    public void Any_other_answer_throws_an_Exception_carrying_it(int hr)
    {
        var thrown = Assert.Throws<Exception>(() => Guids.NewGuid((out Guid g) => { g = Guid.Empty; return hr; }));

        Assert.Equal(hr, thrown.HResult);
        Assert.Equal("Exception of type 'System.Exception' was thrown.", thrown.Message);
        Assert.Null(thrown.InnerException);
    }
}
