import Foundation

/// A source of installed browsers.
public protocol BrowserDiscovering: Sendable {
    func discoverBrowsers() -> [Browser]
}
