using System.Globalization;

namespace Defaulty.Core;

public sealed class BrowserCatalog
{
    public BrowserCatalog() { }

    public static IReadOnlyList<Browser> Normalize(IEnumerable<Browser> browsers, bool _ = true)
        => new BrowserCatalog().Normalize(browsers);

    public static IReadOnlyList<Browser> Build(IEnumerable<Browser> browsers) => Normalize(browsers);

    public IReadOnlyList<Browser> GetBrowsers(IEnumerable<Browser> browsers) => Normalize(browsers);

    public IReadOnlyList<Browser> Normalize(IEnumerable<Browser> browsers)
    {
        // Filter hidden + stale
        var candidates = browsers.Where(b => !b.Hidden && !b.IsStale).ToList();

        // Deduplicate by primary ProgId or canonical executable path, preferring
        // valid per-user registrations, then native 64-bit HKLM.
        var groups = new Dictionary<string, List<Browser>>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in candidates)
        {
            var key = DedupKey(b);
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<Browser>();
                groups[key] = list;
            }
            list.Add(b);
        }

        var result = new List<Browser>();
        foreach (var group in groups.Values)
        {
            result.Add(PickPreferred(group));
        }

        // Sort by display name (current culture, case-insensitive), then Id.
        result.Sort((a, b) =>
        {
            int cmp = string.Compare(a.DisplayName, b.DisplayName, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);
            if (cmp != 0)
            {
                return cmp;
            }
            return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        });

        return result;
    }

    private static string DedupKey(Browser b)
    {
        if (!string.IsNullOrWhiteSpace(b.PrimaryProgId))
        {
            return "progid:" + b.PrimaryProgId;
        }
        if (!string.IsNullOrWhiteSpace(b.ExecutablePath))
        {
            return "exe:" + CanonicalPath(b.ExecutablePath!);
        }
        return "id:" + b.Id;
    }

    private static string CanonicalPath(string path)
    {
        try
        {
            return Path.GetFullPath(path).ToLowerInvariant();
        }
        catch
        {
            return path.ToLowerInvariant();
        }
    }

    private static Browser PickPreferred(List<Browser> group)
    {
        if (group.Count == 1)
        {
            return group[0];
        }
        // Prefer valid per-user registration
        var user = group.Where(b => b.Hive == RegistrationHive.User && !b.IsStale).ToList();
        if (user.Count > 0)
        {
            return user[0];
        }
        // Then native 64-bit HKLM
        var native = group.Where(b => b.Hive == RegistrationHive.Machine && b.View == RegistryViewKind.Native64 && !b.IsStale).ToList();
        if (native.Count > 0)
        {
            return native[0];
        }
        // Fallback: first
        return group[0];
    }

    // --- Matching current default to a registered browser ---

    public static Browser? MatchBrowser(IReadOnlyList<Browser> browsers, AssociationInfo info)
    {
        if (info is null)
        {
            return null;
        }
        // By ProgId first
        if (!string.IsNullOrWhiteSpace(info.ProgId))
        {
            foreach (var b in browsers)
            {
                if (string.Equals(b.PrimaryProgId, info.ProgId, StringComparison.OrdinalIgnoreCase))
                {
                    return b;
                }
                if (b.EffectiveProgIds.Values.Any(v => string.Equals(v, info.ProgId, StringComparison.OrdinalIgnoreCase)))
                {
                    return b;
                }
            }
        }
        // Then by canonical executable path
        if (!string.IsNullOrWhiteSpace(info.ExecutablePath))
        {
            var target = CanonicalPath(info.ExecutablePath!);
            foreach (var b in browsers)
            {
                if (!string.IsNullOrWhiteSpace(b.ExecutablePath) && CanonicalPath(b.ExecutablePath!) == target)
                {
                    return b;
                }
            }
        }
        return null;
    }

    public static Browser CreateSynthetic(AssociationInfo info, string scheme)
    {
        string? displayName = info.FriendlyName;
        if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(info.ExecutablePath))
        {
            displayName = Path.GetFileNameWithoutExtension(info.ExecutablePath);
        }
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Unknown browser";
        }

        string id;
        if (!string.IsNullOrWhiteSpace(info.ProgId))
        {
            id = "synthetic:" + info.ProgId;
        }
        else if (!string.IsNullOrWhiteSpace(info.ExecutablePath))
        {
            id = "synthetic:" + info.ExecutablePath;
        }
        else
        {
            id = "synthetic:" + scheme;
        }

        return new Browser(
            Id: id,
            DisplayName: displayName!,
            IconReference: string.IsNullOrWhiteSpace(info.IconReference) ? info.ExecutablePath : info.IconReference,
            ExecutablePath: info.ExecutablePath,
            PrimaryProgId: info.ProgId,
            ProgIds: new Dictionary<string, string> { [scheme] = info.ProgId ?? string.Empty },
            RegistrationName: null,
            Hive: RegistrationHive.Machine,
            View: RegistryViewKind.Native64,
            Hidden: false,
            CapabilitiesExists: true,
            ExecutableExists: true,
            IsSynthetic: true,
            SupportsPerAppSettingsLink: false);
    }

    public sealed record CurrentDefaultResolution(
        Browser? CheckedBrowser,
        Browser? HttpBrowser,
        bool IsMismatch,
        bool HasDefault);

    public static CurrentDefaultResolution ResolveCurrent(
        IReadOnlyList<Browser> browsers,
        CurrentAssociationState state)
    {
        // Canonical: https first; fall back to http if https unresolved.
        Browser? checkedBrowser = null;
        bool hasDefault = false;

        if (state.HttpsResolved)
        {
            checkedBrowser = MatchBrowser(browsers, state.Https);
            if (checkedBrowser is null)
            {
                checkedBrowser = CreateSynthetic(state.Https, "https");
            }
            hasDefault = true;
        }
        else if (state.HttpResolved)
        {
            checkedBrowser = MatchBrowser(browsers, state.Http);
            if (checkedBrowser is null)
            {
                checkedBrowser = CreateSynthetic(state.Http, "http");
            }
            hasDefault = true;
        }

        Browser? httpBrowser = null;
        if (state.HttpResolved)
        {
            httpBrowser = MatchBrowser(browsers, state.Http);
            if (httpBrowser is null)
            {
                httpBrowser = CreateSynthetic(state.Http, "http");
            }
        }

        bool isMismatch = false;
        if (hasDefault && state.HttpsResolved && state.HttpResolved && checkedBrowser is not null && httpBrowser is not null)
        {
            isMismatch = !string.Equals(checkedBrowser.Id, httpBrowser.Id, StringComparison.Ordinal);
        }

        return new CurrentDefaultResolution(checkedBrowser, httpBrowser, isMismatch, hasDefault);
    }

    public static IReadOnlyList<Browser> WithCurrentDefault(
        IReadOnlyList<Browser> browsers,
        CurrentAssociationState state,
        out CurrentDefaultResolution resolution)
    {
        resolution = ResolveCurrent(browsers, state);
        if (!resolution.HasDefault || resolution.CheckedBrowser is null)
        {
            return browsers;
        }
        // If checked browser already in list, return as-is
        var checkedId = resolution.CheckedBrowser!.Id;
        if (browsers.Any(b => b.Id == checkedId))
        {
            return browsers;
        }
        // Append synthetic checked entry
        var list = browsers.ToList();
        list.Add(resolution.CheckedBrowser);
        return list;
    }
}