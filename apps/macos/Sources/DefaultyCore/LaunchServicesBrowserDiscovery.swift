import AppKit

/// Discovers browsers that claim the `http` and/or `https` URL schemes in
/// Launch Services, resolving each handler to its application bundle.
public struct LaunchServicesBrowserDiscovery: BrowserDiscovering {
    /// Defaulty itself never handles web URLs, but excluding it here keeps the
    /// catalog clean if the app is launched in an unusual configuration.
    public static let ownBundleIdentifier = "com.miguels.defaulty"

    public init() {}

    public func discoverBrowsers() -> [Browser] {
        var bundleIdentifiers = Set<String>()

        for scheme in ["http", "https"] {
            guard let probeURL = URL(string: "\(scheme)://example.com") else { continue }
            for applicationURL in NSWorkspace.shared.urlsForApplications(toOpen: probeURL) {
                guard let bundleIdentifier = Bundle(url: applicationURL)?.bundleIdentifier,
                      !bundleIdentifier.isEmpty
                else {
                    continue
                }
                bundleIdentifiers.insert(bundleIdentifier)
            }
        }

        var seen = Set<String>()
        var browsers: [Browser] = []

        for bundleIdentifier in bundleIdentifiers {
            guard !bundleIdentifier.isEmpty,
                  bundleIdentifier != Self.ownBundleIdentifier,
                  seen.insert(bundleIdentifier).inserted,
                  let url = NSWorkspace.shared.urlForApplication(withBundleIdentifier: bundleIdentifier),
                  url.pathExtension.lowercased() == "app",
                  FileManager.default.isReadableFile(atPath: url.path)
            else {
                continue
            }

            browsers.append(
                Browser(
                    bundleIdentifier: bundleIdentifier,
                    name: Self.displayName(for: url),
                    url: url
                )
            )
        }

        return browsers
    }

    private static func displayName(for url: URL) -> String {
        if let bundle = Bundle(url: url) {
            if let displayName = bundle.object(forInfoDictionaryKey: "CFBundleDisplayName") as? String,
               !displayName.isEmpty {
                return displayName
            }
            if let bundleName = bundle.object(forInfoDictionaryKey: "CFBundleName") as? String,
               !bundleName.isEmpty {
                return bundleName
            }
        }
        return url.deletingPathExtension().lastPathComponent
    }
}
