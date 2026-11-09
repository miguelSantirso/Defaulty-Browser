import Foundation

/// Normalizes discovered browsers into a stable, display-ready list.
public struct BrowserCatalog: Sendable {
    private let discovery: any BrowserDiscovering

    public init(discovery: any BrowserDiscovering = LaunchServicesBrowserDiscovery()) {
        self.discovery = discovery
    }

    /// Returns installed browsers, excluding any bundle identifiers in
    /// `excluded`, with duplicate identifiers removed and names sorted the way
    /// Finder sorts (case-insensitive, localized, numeric-aware).
    public func browsers(excluding excluded: Set<String> = []) -> [Browser] {
        var seen = Set<String>()
        var unique: [Browser] = []

        for browser in discovery.discoverBrowsers() {
            guard !excluded.contains(browser.bundleIdentifier),
                  seen.insert(browser.bundleIdentifier).inserted
            else {
                continue
            }
            unique.append(browser)
        }

        return unique.sorted { lhs, rhs in
            let order = lhs.name.localizedStandardCompare(rhs.name)
            if order == .orderedSame {
                return lhs.bundleIdentifier < rhs.bundleIdentifier
            }
            return order == .orderedAscending
        }
    }
}
