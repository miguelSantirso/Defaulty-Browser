import DefaultyCore
import XCTest

private struct FakeBrowserDiscovery: BrowserDiscovering {
    let candidates: [Browser]

    func discoverBrowsers() -> [Browser] {
        candidates
    }
}

private func browser(_ id: String, _ name: String) -> Browser {
    Browser(
        bundleIdentifier: id,
        name: name,
        url: URL(fileURLWithPath: "/Applications/\(name).app")
    )
}

final class BrowserCatalogTests: XCTestCase {
    func testExcludesRequestedBundleIdentifiers() {
        let discovery = FakeBrowserDiscovery(candidates: [
            browser("com.example.safari", "Safari"),
            browser("com.example.chrome", "Chrome"),
        ])
        let catalog = BrowserCatalog(discovery: discovery)

        let result = catalog.browsers(excluding: ["com.example.safari"])

        XCTAssertEqual(result.map(\.bundleIdentifier), ["com.example.chrome"])
    }

    func testRemovesDuplicateBundleIdentifiers() {
        let discovery = FakeBrowserDiscovery(candidates: [
            browser("com.example.chrome", "Chrome"),
            browser("com.example.chrome", "Google Chrome"),
            browser("com.example.safari", "Safari"),
        ])
        let catalog = BrowserCatalog(discovery: discovery)

        let result = catalog.browsers()

        XCTAssertEqual(result.count, 2)
        XCTAssertEqual(result.map(\.bundleIdentifier), ["com.example.chrome", "com.example.safari"])
        XCTAssertEqual(result.first?.name, "Chrome", "The first discovered candidate wins")
    }

    func testSortsNamesCaseInsensitivelyLikeFinder() {
        let discovery = FakeBrowserDiscovery(candidates: [
            browser("com.example.safari", "Safari"),
            browser("com.example.arc", "arc"),
            browser("com.example.chrome", "Chrome"),
            browser("com.example.brave", "Brave"),
        ])
        let catalog = BrowserCatalog(discovery: discovery)

        let result = catalog.browsers()

        XCTAssertEqual(result.map(\.name), ["arc", "Brave", "Chrome", "Safari"])
    }

    func testExclusionAndDeduplicationWorkTogether() {
        let discovery = FakeBrowserDiscovery(candidates: [
            browser("com.example.safari", "Safari"),
            browser("com.example.self", "Defaulty"),
            browser("com.example.chrome", "Chrome"),
            browser("com.example.chrome", "Chrome"),
        ])
        let catalog = BrowserCatalog(discovery: discovery)

        let result = catalog.browsers(excluding: ["com.example.self"])

        XCTAssertEqual(result.map(\.bundleIdentifier), ["com.example.chrome", "com.example.safari"])
    }

    func testLaunchServicesDiscoveryReturnsValidUniqueBrowsers() {
        let browsers = LaunchServicesBrowserDiscovery().discoverBrowsers()

        XCTAssertEqual(Set(browsers.map(\.bundleIdentifier)).count, browsers.count)
        for browser in browsers {
            XCTAssertFalse(browser.bundleIdentifier.isEmpty)
            XCTAssertFalse(browser.name.isEmpty)
            XCTAssertEqual(browser.url.pathExtension.lowercased(), "app")
            XCTAssertTrue(
                FileManager.default.isReadableFile(atPath: browser.url.path),
                "\(browser.name) should resolve to a readable application bundle"
            )
            XCTAssertNotEqual(browser.bundleIdentifier, LaunchServicesBrowserDiscovery.ownBundleIdentifier)
        }
    }
}
