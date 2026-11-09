import Foundation

/// A web browser installed on the system.
public struct Browser: Hashable, Sendable, Identifiable {
    public let bundleIdentifier: String
    public let name: String
    public let url: URL

    public var id: String { bundleIdentifier }

    public init(bundleIdentifier: String, name: String, url: URL) {
        self.bundleIdentifier = bundleIdentifier
        self.name = name
        self.url = url
    }
}
