import AppKit
import DefaultyCore

private let fallbackBundleIdentifier = LaunchServicesBrowserDiscovery.ownBundleIdentifier

private func writeStandardError(_ message: String) {
    FileHandle.standardError.write(Data((message + "\n").utf8))
}

private func usage() -> String {
    """
    Defaulty — a tiny menu bar app for switching the default web browser.

    Usage:
      Defaulty                         Launch the menu bar app
      Defaulty --list                  List installed browsers
      Defaulty --current               Print the current default browser bundle id
      Defaulty --set-default <id>      Make the browser with <id> the default
      Defaulty --help                  Show this help

    Run inside Defaulty.app for normal use; the CLI modes never create the
    status bar item.
    """
}

@MainActor
private func printBrowsers() {
    let catalog = BrowserCatalog()
    let current = DefaultBrowserService().currentDefaultBrowserBundleIdentifier()
    let browsers = catalog.browsers(excluding: [fallbackBundleIdentifier])

    for browser in browsers {
        let prefix = browser.bundleIdentifier == current ? "* " : ""
        print("\(prefix)\(browser.bundleIdentifier)\t\(browser.name)\t\(browser.url.path)")
    }
}

@MainActor
private func printCurrentBrowser() {
    let current = DefaultBrowserService().currentDefaultBrowserBundleIdentifier()
    print(current ?? "none")
}

@MainActor
private func setDefaultBrowser(bundleIdentifier: String) -> Never {
    let catalog = BrowserCatalog()
    let browsers = catalog.browsers(excluding: [fallbackBundleIdentifier])

    guard let browser = browsers.first(where: { $0.bundleIdentifier == bundleIdentifier }) else {
        writeStandardError("error: no installed browser with bundle id “\(bundleIdentifier)”")
        exit(1)
    }

    let service = DefaultBrowserService()

    Task { @MainActor in
        do {
            try await service.setDefaultBrowser(browser)
            print("Default browser set to \(browser.name) (\(browser.bundleIdentifier)).")
            exit(0)
        } catch {
            writeStandardError("error: \(error.localizedDescription)")
            exit(1)
        }
    }

    // NSWorkspace delivers its completion handlers on the main run loop, so
    // keep it spinning until the task finishes and exits the process.
    RunLoop.main.run()
    exit(1)
}

let arguments = Array(CommandLine.arguments.dropFirst())

switch arguments.first {
case nil:
    break
case "--list":
    printBrowsers()
    exit(0)
case "--current":
    printCurrentBrowser()
    exit(0)
case "--help", "-h":
    print(usage())
    exit(0)
case "--set-default":
    guard arguments.count >= 2 else {
        writeStandardError("error: --set-default requires a bundle identifier\n\n\(usage())")
        exit(1)
    }
    setDefaultBrowser(bundleIdentifier: arguments[1])
case let argument?:
    writeStandardError("error: unknown option “\(argument)”\n\n\(usage())")
    exit(1)
}

let application = NSApplication.shared
let delegate = AppDelegate()
application.delegate = delegate
application.setActivationPolicy(.accessory)
application.run()
