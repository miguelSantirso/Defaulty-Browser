import AppKit
import DefaultyCore

@MainActor
final class StatusMenuController: NSObject, NSMenuDelegate {
    private let statusItem: NSStatusItem
    private let menu = NSMenu()
    private let catalog: BrowserCatalog
    private let service: DefaultBrowserService
    private let ownBundleIdentifier: String

    private var browsers: [Browser] = []
    private var currentDefaultID: String?

    init(
        catalog: BrowserCatalog = BrowserCatalog(),
        service: DefaultBrowserService = DefaultBrowserService(),
        ownBundleIdentifier: String = Bundle.main.bundleIdentifier
            ?? LaunchServicesBrowserDiscovery.ownBundleIdentifier
    ) {
        self.catalog = catalog
        self.service = service
        self.ownBundleIdentifier = ownBundleIdentifier
        self.statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)

        super.init()

        configureStatusItem()
        menu.delegate = self
        menu.autoenablesItems = false
        menu.minimumWidth = 220
        statusItem.menu = menu
        refresh()
    }

    private func configureStatusItem() {
        guard let button = statusItem.button else { return }
        let image = NSImage(systemSymbolName: "globe", accessibilityDescription: "Defaulty")
        image?.isTemplate = true
        button.image = image
        updateStatusItemCopy()
    }

    /// Re-reads the installed browsers and the system default. Safe to call
    /// while the menu is closed; it also updates any existing item checkmarks.
    private func refresh() {
        browsers = catalog.browsers(excluding: [ownBundleIdentifier])
        currentDefaultID = service.currentDefaultBrowserBundleIdentifier()
        updateStatusItemCopy()

        for item in menu.items {
            guard let bundleIdentifier = item.representedObject as? String else { continue }
            item.state = bundleIdentifier == currentDefaultID ? .on : .off
            item.isEnabled = true
        }
    }

    private func updateStatusItemCopy() {
        guard let button = statusItem.button else { return }

        let defaultName = currentDefaultID.flatMap { identifier in
            browsers.first(where: { $0.bundleIdentifier == identifier })?.name
        } ?? currentDefaultID ?? "none"
        let description = "Defaulty — default browser: \(defaultName)"

        button.toolTip = description
        button.setAccessibilityLabel(description)
    }

    private func rebuildMenu() {
        refresh()
        menu.removeAllItems()

        if browsers.isEmpty {
            let emptyItem = NSMenuItem(title: "No browsers found", action: nil, keyEquivalent: "")
            emptyItem.isEnabled = false
            menu.addItem(emptyItem)
        } else {
            for browser in browsers {
                let item = NSMenuItem(
                    title: browser.name,
                    action: #selector(selectBrowser(_:)),
                    keyEquivalent: ""
                )
                item.target = self
                item.representedObject = browser.bundleIdentifier
                item.image = menuIcon(for: browser)
                item.state = browser.bundleIdentifier == currentDefaultID ? .on : .off
                item.isEnabled = true
                menu.addItem(item)
            }
        }

        menu.addItem(.separator())

        let quitItem = NSMenuItem(
            title: "Quit Defaulty",
            action: #selector(NSApplication.terminate(_:)),
            keyEquivalent: "q"
        )
        quitItem.target = NSApplication.shared
        menu.addItem(quitItem)
    }

    private func menuIcon(for browser: Browser) -> NSImage {
        let source = NSWorkspace.shared.icon(forFile: browser.url.path)
        let targetSize = NSSize(width: 16, height: 16)
        let sourceSize = source.size
        guard sourceSize.width > 0, sourceSize.height > 0 else {
            source.size = targetSize
            return source
        }

        let scale = min(targetSize.width / sourceSize.width, targetSize.height / sourceSize.height)
        let drawnSize = NSSize(
            width: sourceSize.width * scale,
            height: sourceSize.height * scale
        )
        let drawnRect = NSRect(
            x: (targetSize.width - drawnSize.width) / 2,
            y: (targetSize.height - drawnSize.height) / 2,
            width: drawnSize.width,
            height: drawnSize.height
        )

        return NSImage(size: targetSize, flipped: false) { _ in
            source.draw(in: drawnRect)
            return true
        }
    }

    @objc
    private func selectBrowser(_ sender: NSMenuItem) {
        guard let bundleIdentifier = sender.representedObject as? String,
              let browser = browsers.first(where: { $0.bundleIdentifier == bundleIdentifier }),
              browser.bundleIdentifier != currentDefaultID
        else {
            return
        }

        sender.isEnabled = false

        Task { @MainActor [weak self] in
            guard let self else { return }
            do {
                try await self.service.setDefaultBrowser(browser)
                self.rebuildMenu()
            } catch {
                self.presentError(error)
                self.refresh()
            }
        }
    }

    private func presentError(_ error: any Error) {
        if #available(macOS 14.0, *) {
            NSApp.activate()
        } else {
            NSApp.activate(ignoringOtherApps: true)
        }

        let alert = NSAlert()
        alert.alertStyle = .warning
        alert.messageText = "Couldn’t change the default browser"
        alert.informativeText = error.localizedDescription
        alert.addButton(withTitle: "OK")
        alert.runModal()
    }

    // MARK: - NSMenuDelegate

    func menuNeedsUpdate(_ menu: NSMenu) {
        rebuildMenu()
    }
}
