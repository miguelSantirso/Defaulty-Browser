using Defaulty.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Defaulty.Core.Tests;

public sealed class FakeAssocQueryString : IAssocQueryString
{
    private readonly Dictionary<(AssocF, AssocStr, string), string?> _values = new();
    public void Set(AssocF flags, AssocStr str, string assoc, string? value) => _values[(flags, str, assoc)] = value;
    public string? Query(AssocF flags, AssocStr str, string assoc)
    {
        _values.TryGetValue((flags, str, assoc), out var v);
        return v;
    }
}

[TestClass]
public class AssociationReaderTests
{
    [TestMethod]
    public void ReadScheme_PopulatesFields()
    {
        var fake = new FakeAssocQueryString();
        var flags = AssocF.IsProtocol | AssocF.AppToApp;
        fake.Set(flags, AssocStr.ProgId, "https", "ChromeHTML");
        fake.Set(flags, AssocStr.Executable, "https", "C:\\chrome.exe");
        fake.Set(flags, AssocStr.FriendlyAppName, "https", "Google Chrome");
        fake.Set(flags, AssocStr.AppIconReference, "https", "chrome.exe,0");
        fake.Set(flags, AssocStr.AppId, "https", "ChromeAppId");

        var reader = new DefaultAssociationReader(fake);
        var info = reader.ReadScheme("https");
        Assert.AreEqual("https", info.Scheme);
        Assert.AreEqual("ChromeHTML", info.ProgId);
        Assert.AreEqual("C:\\chrome.exe", info.ExecutablePath);
        Assert.AreEqual("Google Chrome", info.FriendlyName);
        Assert.AreEqual("chrome.exe,0", info.IconReference);
        Assert.AreEqual("ChromeAppId", info.AppId);
    }

    [TestMethod]
    public void ReadCurrent_ResolvedFlags()
    {
        var fake = new FakeAssocQueryString();
        var flags = AssocF.IsProtocol | AssocF.AppToApp;
        fake.Set(flags, AssocStr.ProgId, "https", "ChromeHTML");
        // http unresolved
        var reader = new DefaultAssociationReader(fake);
        var state = reader.ReadCurrent();
        Assert.IsTrue(state.HttpsResolved);
        Assert.IsFalse(state.HttpResolved);
    }

    [TestMethod]
    public void Cli_FormatCurrent_PrintsNoneForUnresolved()
    {
        var state = new CurrentAssociationState(
            new AssociationInfo("https", null, null, null, null, null),
            new AssociationInfo("http", null, null, null, null, null),
            false, false);
        var output = Cli.FormatCurrent(state);
        Assert.IsTrue(output.Contains("https=none"));
        Assert.IsTrue(output.Contains("http=none"));
    }
}
