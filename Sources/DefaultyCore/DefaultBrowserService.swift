import AppKit
import Foundation

public enum DefaultBrowserError: LocalizedError, Sendable {
    case browserNotInstalled(bundleIdentifier: String)
    case couldNotSetScheme(scheme: String, reason: String)

    public var errorDescription: String? {
        switch self {
        case let .browserNotInstalled(bundleIdentifier):
            return "The browser \(bundleIdentifier) is no longer installed."
        case let .couldNotSetScheme(scheme, reason):
            return "macOS could not make this app the default for \(scheme) links. \(reason)"
        }
    }

    public var recoverySuggestion: String? {
        switch self {
        case .browserNotInstalled:
            return "Choose another browser from the Defaulty menu."
        case .couldNotSetScheme:
            return "You can change the default web browser in System Settings › Desktop & Dock."
        }
    }
}

/// Reads and changes the system default web browser through `NSWorkspace`.
///
/// The setter must run on the main actor because `NSWorkspace` delivers its
/// completion handlers on the main run loop; callers simply `await` it.
@MainActor
public final class DefaultBrowserService {
    public init() {}

    public func currentDefaultBrowserBundleIdentifier() -> String? {
        guard let probeURL = URL(string: "https://example.com"),
              let applicationURL = NSWorkspace.shared.urlForApplication(toOpen: probeURL)
        else {
            return nil
        }
        return Bundle(url: applicationURL)?.bundleIdentifier
    }

    /// Sets `browser` as the default handler for both `https` and `http`.
    ///
    /// `https` is attempted first: if the system rejects it there is no reason
    /// to change the `http` handler. Both registrations are awaited before
    /// returning so callers can refresh their UI immediately.
    public func setDefaultBrowser(_ browser: Browser) async throws {
        guard FileManager.default.fileExists(atPath: browser.url.path) else {
            throw DefaultBrowserError.browserNotInstalled(bundleIdentifier: browser.bundleIdentifier)
        }

        do {
            try await register(browser, forScheme: "https")
        } catch {
            throw DefaultBrowserError.couldNotSetScheme(
                scheme: "https",
                reason: error.localizedDescription
            )
        }

        do {
            try await register(browser, forScheme: "http")
        } catch {
            throw DefaultBrowserError.couldNotSetScheme(
                scheme: "http",
                reason: error.localizedDescription
            )
        }
    }

    private func register(_ browser: Browser, forScheme scheme: String) async throws {
        try await withCheckedThrowingContinuation { continuation in
            let gate = ContinuationGate(continuation)
            NSWorkspace.shared.setDefaultApplication(
                at: browser.url,
                toOpenURLsWithScheme: scheme
            ) { error in
                gate.resume(throwing: error)
            }
        }
    }
}

/// Protects a checked continuation against a completion handler that fires
/// more than once (which would otherwise trap).
private final class ContinuationGate: @unchecked Sendable {
    private let lock = NSLock()
    private var continuation: CheckedContinuation<Void, any Error>?

    init(_ continuation: CheckedContinuation<Void, any Error>) {
        self.continuation = continuation
    }

    func resume(throwing error: (any Error)?) {
        lock.lock()
        let continuation = continuation
        self.continuation = nil
        lock.unlock()

        guard let continuation else { return }
        if let error {
            continuation.resume(throwing: error)
        } else {
            continuation.resume()
        }
    }
}
