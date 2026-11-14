using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Defaulty.Safety.Tests;

/// <summary>
/// Static safety audit for the Windows app: scans source, manifest, and build
/// configuration for forbidden patterns/APIs that would violate the product spec
/// (registry writes, UserChoice access, association setters, UI automation,
/// network calls, startup/login registration, elevation).
/// </summary>
[TestClass]
public class SafetyAuditTests
{
    private static readonly string WindowsRoot = FindWindowsRoot();
    private static readonly IReadOnlyList<string> SourceFiles = CollectSourceFiles();

    [TestMethod]
    public void NoRegistryWritesOrSetters()
    {
        var patterns = new[]
        {
            new Regex(@"\bCreateSubKey\b", RegexOptions.IgnoreCase),
            new Regex(@"\bSetValue\s*\(", RegexOptions.IgnoreCase),
            new Regex(@"\bDeleteSubKey\w*\b", RegexOptions.IgnoreCase),
            new Regex(@"\bDeleteValue\s*\(", RegexOptions.IgnoreCase),
            new Regex(@"OpenSubKey\s*\([^)]*,\s*true\s*\)", RegexOptions.IgnoreCase),
            new Regex(@"\bwritable\s*[:=]\s*true\b", RegexOptions.IgnoreCase),
            new Regex(@"\bRegistry\.SetValue\b", RegexOptions.IgnoreCase),
        };
        AssertNoForbidden(patterns, "registry write/setter");
    }

    [TestMethod]
    public void NoDirectUserChoiceAccess()
    {
        var patterns = new[]
        {
            new Regex(@"UserChoice", RegexOptions.IgnoreCase),
        };
        AssertNoForbidden(patterns, "direct UserChoice access");
    }

    [TestMethod]
    public void NoAssociationSetterApis()
    {
        var patterns = new[]
        {
            new Regex(@"MakeDefault", RegexOptions.IgnoreCase),
            new Regex(@"SetUserFTA", RegexOptions.IgnoreCase),
            new Regex(@"ApplicationAssociationRegistration", RegexOptions.IgnoreCase),
            new Regex(@"IAssocHandler", RegexOptions.IgnoreCase),
            new Regex(@"\bAssocSet\w*\b", RegexOptions.IgnoreCase),
            new Regex(@"SHSetDefaultProperties", RegexOptions.IgnoreCase),
        };
        AssertNoForbidden(patterns, "association-setter API");
    }

    [TestMethod]
    public void NoUiAutomationOrSendKeys()
    {
        var patterns = new[]
        {
            new Regex(@"SendKeys", RegexOptions.IgnoreCase),
            new Regex(@"UIAutomation", RegexOptions.IgnoreCase),
            new Regex(@"AutomationElement", RegexOptions.IgnoreCase),
            new Regex(@"System\.Windows\.Automation", RegexOptions.IgnoreCase),
            new Regex(@"\bSendInput\b", RegexOptions.IgnoreCase),
            new Regex(@"\bkeybd_event\b", RegexOptions.IgnoreCase),
            new Regex(@"\bmouse_event\b", RegexOptions.IgnoreCase),
        };
        AssertNoForbidden(patterns, "UI Automation / keystroke injection");
    }

    [TestMethod]
    public void NoNetworkCalls()
    {
        var patterns = new[]
        {
            new Regex(@"\bHttpClient\b", RegexOptions.IgnoreCase),
            new Regex(@"\bWebRequest\b", RegexOptions.IgnoreCase),
            new Regex(@"\bWebClient\b", RegexOptions.IgnoreCase),
            new Regex(@"\bTcpClient\b", RegexOptions.IgnoreCase),
            new Regex(@"\bUdpClient\b", RegexOptions.IgnoreCase),
            new Regex(@"\bHttpWebRequest\b", RegexOptions.IgnoreCase),
            new Regex(@"\bWebSocket\b", RegexOptions.IgnoreCase),
            new Regex(@"System\.Net\.Http", RegexOptions.IgnoreCase),
            new Regex(@"\bDownloadFile\b", RegexOptions.IgnoreCase),
            new Regex(@"\bSocket\b", RegexOptions.IgnoreCase),
        };
        AssertNoForbidden(patterns, "network API");
    }

    [TestMethod]
    public void NoStartupOrLoginItemRegistration()
    {
        var patterns = new[]
        {
            new Regex(@"CurrentVersion\\\\Run\b", RegexOptions.IgnoreCase),
            new Regex(@"\bRunOnce\b", RegexOptions.IgnoreCase),
            new Regex(@"\bStartupApproved\b", RegexOptions.IgnoreCase),
            new Regex(@"\bschtasks\b", RegexOptions.IgnoreCase),
            new Regex(@"\bTaskScheduler\b", RegexOptions.IgnoreCase),
            new Regex(@"\bLoginItems\b", RegexOptions.IgnoreCase),
            new Regex(@"RegisterApplicationRestart", RegexOptions.IgnoreCase),
        };
        AssertNoForbidden(patterns, "startup/login-item registration");
    }

    [TestMethod]
    public void NoElevationBeyondAsInvoker()
    {
        var patterns = new[]
        {
            new Regex(@"\brunas\b", RegexOptions.IgnoreCase),
            new Regex(@"requireAdministrator", RegexOptions.IgnoreCase),
            new Regex(@"highestAvailable", RegexOptions.IgnoreCase),
        };
        AssertNoForbidden(patterns, "elevation request");
    }

    [TestMethod]
    public void ManifestRequestsAsInvoker()
    {
        var manifestPath = Path.Combine(WindowsRoot, "packaging", "app.manifest");
        Assert.IsTrue(File.Exists(manifestPath), "app.manifest not found");
        var text = File.ReadAllText(manifestPath);
        Assert.IsTrue(
            Regex.IsMatch(text, @"requestedExecutionLevel[^>]*level\s*=\s*""asInvoker""", RegexOptions.IgnoreCase),
            "app.manifest must request asInvoker");
        Assert.IsFalse(
            Regex.IsMatch(text, @"level\s*=\s*""(requireAdministrator|highestAvailable)""", RegexOptions.IgnoreCase),
            "app.manifest must not request elevation");
    }

    private void AssertNoForbidden(Regex[] patterns, string category)
    {
        var violations = new List<string>();
        foreach (var file in SourceFiles)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch
            {
                continue;
            }
            for (int i = 0; i < lines.Length; i++)
            {
                foreach (var pattern in patterns)
                {
                    if (pattern.IsMatch(lines[i]))
                    {
                        var relative = Path.GetRelativePath(WindowsRoot, file);
                        violations.Add($"{relative}:{i + 1}: {lines[i].Trim()}");
                        break;
                    }
                }
            }
        }

        Assert.AreEqual(
            0,
            violations.Count,
            $"Forbidden {category} pattern(s) found:\n" + string.Join("\n", violations));
    }

    private static string FindWindowsRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Defaulty.sln")) &&
                Directory.Exists(Path.Combine(dir.FullName, "src", "Defaulty")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate apps/windows root from " + AppContext.BaseDirectory);
    }

    private static IReadOnlyList<string> CollectSourceFiles()
    {
        var root = FindWindowsRoot();
        var files = new List<string>();

        void AddDirectory(string relative)
        {
            var full = Path.Combine(root, relative);
            if (!Directory.Exists(full))
            {
                return;
            }
            foreach (var pattern in new[] { "*.cs", "*.csproj", "*.manifest", "*.ps1", "*.props", "*.targets", "*.json", "*.xml" })
            {
                files.AddRange(Directory.GetFiles(full, pattern, SearchOption.AllDirectories)
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                             && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)));
            }
        }

        AddDirectory("src");
        AddDirectory("packaging");

        // Root-level project/solution/config files.
        foreach (var pattern in new[] { "*.csproj", "*.sln", "*.manifest", "*.props", "*.targets" })
        {
            files.AddRange(Directory.GetFiles(root, pattern));
        }

        return files.Distinct().OrderBy(f => f).ToList();
    }
}
