using Defaulty.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Defaulty.Core.Tests;

public sealed class FakeOsVersionProvider : IOsVersionProvider
{
    public OsVersionInfo Value { get; set; } = new(0, 0);
    public OsVersionInfo GetOsVersion() => Value;
}

public sealed class FakeProcessStarter : IProcessStarter
{
    public string? LastUri { get; private set; }
    public bool ReturnValue { get; set; } = true;
    public bool Start(string uri) { LastUri = uri; return ReturnValue; }
}

[TestClass]
public class SettingsLauncherTests
{
    private static SettingsLauncher Create(int build, int ubr, out FakeProcessStarter starter)
    {
        starter = new FakeProcessStarter();
        var os = new FakeOsVersionProvider { Value = new OsVersionInfo(build, ubr) };
        return new SettingsLauncher(os, starter);
    }

    [TestMethod]
    public void IsPerAppOsSupported_23H2_NoUbrNeeded()
    {
        var sl = Create(22631, 0, out _);
        Assert.IsTrue(sl.IsPerAppOsSupported());
    }

    [TestMethod]
    public void IsPerAppOsSupported_22H2_UbrGate()
    {
        var slLow = Create(22621, 1000, out _);
        Assert.IsFalse(slLow.IsPerAppOsSupported());
        var slHigh = Create(22621, 1555, out _);
        Assert.IsTrue(slHigh.IsPerAppOsSupported());
    }

    [TestMethod]
    public void IsPerAppOsSupported_21H2_UbrGate()
    {
        var slLow = Create(22000, 1000, out _);
        Assert.IsFalse(slLow.IsPerAppOsSupported());
        var slHigh = Create(22000, 1817, out _);
        Assert.IsTrue(slHigh.IsPerAppOsSupported());
    }

    [TestMethod]
    public void IsPerAppOsSupported_Win10_False()
    {
        var sl = Create(19045, 0, out _);
        Assert.IsFalse(sl.IsPerAppOsSupported());
    }

    [TestMethod]
    public void GetSettingsUri_PerAppMachine_WhenSupported()
    {
        var sl = Create(22631, 0, out _);
        var b = new Browser("machine:Chrome", "Chrome", RegistrationName: "Google Chrome", Hive: RegistrationHive.Machine, SupportsPerAppSettingsLink: true);
        Assert.AreEqual("ms-settings:defaultapps?registeredAppMachine=Google%20Chrome", sl.GetSettingsUri(b));
    }

    [TestMethod]
    public void GetSettingsUri_PerAppUser()
    {
        var sl = Create(22631, 0, out _);
        var b = new Browser("user:Firefox", "Firefox", RegistrationName: "Firefox", Hive: RegistrationHive.User, SupportsPerAppSettingsLink: true);
        Assert.AreEqual("ms-settings:defaultapps?registeredAppUser=Firefox", sl.GetSettingsUri(b));
    }

    [TestMethod]
    public void GetSettingsUri_FallbackBase_WhenNotSupportedOs()
    {
        var sl = Create(19045, 0, out _);
        var b = new Browser("machine:Chrome", "Chrome", RegistrationName: "Chrome", Hive: RegistrationHive.Machine, SupportsPerAppSettingsLink: true);
        Assert.AreEqual("ms-settings:defaultapps", sl.GetSettingsUri(b));
    }

    [TestMethod]
    public void GetSettingsUri_FallbackBase_WhenWow64Only()
    {
        var sl = Create(22631, 0, out _);
        var b = new Browser("machine:Chrome", "Chrome", RegistrationName: "Chrome", Hive: RegistrationHive.Machine, View: RegistryViewKind.Wow64, SupportsPerAppSettingsLink: false);
        Assert.AreEqual("ms-settings:defaultapps", sl.GetSettingsUri(b));
    }

    [TestMethod]
    public void GetSettingsUri_FallbackBase_WhenNoRegistrationName()
    {
        var sl = Create(22631, 0, out _);
        var b = new Browser("synthetic:x", "X", RegistrationName: null);
        Assert.AreEqual("ms-settings:defaultapps", sl.GetSettingsUri(b));
    }

    [TestMethod]
    public void Launch_UsesStarterAndReturnsResult()
    {
        var sl = Create(22631, 0, out var starter);
        starter.ReturnValue = false;
        var b = new Browser("machine:Chrome", "Chrome", RegistrationName: "Chrome", Hive: RegistrationHive.Machine, SupportsPerAppSettingsLink: true);
        Assert.IsFalse(sl.Launch(b));
        Assert.AreEqual(sl.GetSettingsUri(b), starter.LastUri);
    }
}
