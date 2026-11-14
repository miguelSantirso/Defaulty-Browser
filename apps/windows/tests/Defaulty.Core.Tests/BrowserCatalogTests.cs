using Defaulty.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Defaulty.Core.Tests;

[TestClass]
public class BrowserCatalogTests
{
    [TestMethod]
    public void Normalize_FiltersHidden()
    {
        var browsers = new[]
        {
            new Browser("a", "Alpha", Hidden: false),
            new Browser("b", "Beta", Hidden: true),
        };
        var result = BrowserCatalog.Normalize(browsers);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("a", result[0].Id);
    }

    [TestMethod]
    public void Normalize_FiltersStale()
    {
        var browsers = new[]
        {
            new Browser("a", "Alpha", CapabilitiesExists: true, ExecutableExists: true, ExecutablePath: "C:\\a.exe"),
            new Browser("b", "Beta", CapabilitiesExists: false),
            new Browser("c", "Gamma", ExecutablePath: "C:\\c.exe", ExecutableExists: false),
        };
        var result = BrowserCatalog.Normalize(browsers);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("a", result[0].Id);
    }

    [TestMethod]
    public void Normalize_DeduplicatesByPrimaryProgId_PrefersUser()
    {
        var machine = new Browser("machine:Chrome", "Chrome", PrimaryProgId: "ChromeHTML", Hive: RegistrationHive.Machine, View: RegistryViewKind.Native64, RegistrationName: "Chrome");
        var user = new Browser("user:Chrome", "Chrome", PrimaryProgId: "ChromeHTML", Hive: RegistrationHive.User, RegistrationName: "Chrome");
        var result = BrowserCatalog.Normalize(new[] { machine, user });
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("user:Chrome", result[0].Id);
    }

    [TestMethod]
    public void Normalize_DeduplicatesByExecutablePath()
    {
        var a = new Browser("a", "Browser A", ExecutablePath: "C:\\app\\b.exe", PrimaryProgId: null);
        var b = new Browser("b", "Browser B", ExecutablePath: "C:\\app\\b.exe", PrimaryProgId: null);
        var result = BrowserCatalog.Normalize(new[] { a, b });
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void Normalize_SortsByDisplayNameCaseInsensitiveThenId()
    {
        var browsers = new[]
        {
            new Browser("z", "zebra"),
            new Browser("a", "Apple"),
            new Browser("m", "apple"),
        };
        var result = BrowserCatalog.Normalize(browsers);
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("a", result[0].Id);
        Assert.AreEqual("m", result[1].Id);
        Assert.AreEqual("z", result[2].Id);
    }

    [TestMethod]
    public void MatchBrowser_ByProgId()
    {
        var b = new Browser("id", "Name", PrimaryProgId: "FooHTML");
        var info = new AssociationInfo("https", "FooHTML", null, null, null, null);
        var matched = BrowserCatalog.MatchBrowser(new[] { b }, info);
        Assert.AreSame(b, matched);
    }

    [TestMethod]
    public void MatchBrowser_ByExecutablePath()
    {
        var b = new Browser("id", "Name", ExecutablePath: "C:\\x\\app.exe");
        var info = new AssociationInfo("https", "OtherProgId", "C:\\x\\app.exe", null, null, null);
        var matched = BrowserCatalog.MatchBrowser(new[] { b }, info);
        Assert.AreSame(b, matched);
    }

    [TestMethod]
    public void ResolveCurrent_MismatchDetected()
    {
        var chrome = new Browser("chrome", "Chrome", PrimaryProgId: "ChromeHTML");
        var firefox = new Browser("firefox", "Firefox", PrimaryProgId: "FirefoxURL");
        var state = new CurrentAssociationState(
            new AssociationInfo("https", "ChromeHTML", null, "Chrome", null, null),
            new AssociationInfo("http", "FirefoxURL", null, "Firefox", null, null),
            true, true);
        var res = BrowserCatalog.ResolveCurrent(new[] { chrome, firefox }, state);
        Assert.IsTrue(res.IsMismatch);
        Assert.AreEqual("chrome", res.CheckedBrowser!.Id);
        Assert.AreEqual("firefox", res.HttpBrowser!.Id);
    }

    [TestMethod]
    public void ResolveCurrent_NoMismatchWhenSameBrowserDifferentProgIds()
    {
        var browser = new Browser("b", "Browser", PrimaryProgId: "ProgA");
        // same browser registered; https uses ProgA, http uses ProgB but same Id? In dedup, same browser would have both progids in ProgIds.
        var browser2 = browser with { ProgIds = new Dictionary<string,string> { ["https"]="ProgA", ["http"]="ProgB" }, PrimaryProgId = "ProgA" };
        var state = new CurrentAssociationState(
            new AssociationInfo("https", "ProgA", null, "Browser", null, null),
            new AssociationInfo("http", "ProgB", null, "Browser", null, null),
            true, true);
        var res = BrowserCatalog.ResolveCurrent(new[] { browser2 }, state);
        // MatchBrowser by ProgId: https matches (PrimaryProgId ProgA). http: primary ProgId ProgA != ProgB, but ProgIds contains ProgB => matches same browser.
        Assert.IsFalse(res.IsMismatch);
    }

    [TestMethod]
    public void ResolveCurrent_SyntheticWhenNotRegistered()
    {
        var state = new CurrentAssociationState(
            new AssociationInfo("https", "UnknownProg", "C:\\unknown.exe", "Unknown Browser", "icon,0", null),
            new AssociationInfo("http", "UnknownProg", "C:\\unknown.exe", "Unknown Browser", "icon,0", null),
            true, true);
        var res = BrowserCatalog.ResolveCurrent(Array.Empty<Browser>(), state);
        Assert.IsTrue(res.CheckedBrowser!.IsSynthetic);
        Assert.AreEqual("Unknown Browser", res.CheckedBrowser.DisplayName);
    }

    [TestMethod]
    public void WithCurrentDefault_AppendsSynthetic()
    {
        var state = new CurrentAssociationState(
            new AssociationInfo("https", "UnknownProg", null, "Unknown", null, null),
            new AssociationInfo("http", "UnknownProg", null, "Unknown", null, null),
            true, true);
        var result = BrowserCatalog.WithCurrentDefault(Array.Empty<Browser>(), state, out var res);
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result[0].IsSynthetic);
    }

    [TestMethod]
    public void CreateSynthetic_UsesFriendlyNameThenExeThenUnknown()
    {
        var info = new AssociationInfo("https", "P", "C:\\dir\\app.exe", null, null, null);
        var syn = BrowserCatalog.CreateSynthetic(info, "https");
        Assert.AreEqual("app", syn.DisplayName);
    }
}
