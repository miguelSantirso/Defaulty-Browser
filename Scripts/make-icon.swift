#!/usr/bin/env swift
import AppKit
import Foundation

let scriptURL = URL(fileURLWithPath: CommandLine.arguments[0])
let rootURL = scriptURL.deletingLastPathComponent().deletingLastPathComponent()
let resourcesURL = rootURL.appendingPathComponent("Resources")
let iconsetURL = resourcesURL.appendingPathComponent("AppIcon.iconset")
let icnsURL = resourcesURL.appendingPathComponent("AppIcon.icns")

struct IconVariant {
    let filename: String
    let pixels: Int
}

let variants = [
    IconVariant(filename: "icon_16x16.png", pixels: 16),
    IconVariant(filename: "icon_16x16@2x.png", pixels: 32),
    IconVariant(filename: "icon_32x32.png", pixels: 32),
    IconVariant(filename: "icon_32x32@2x.png", pixels: 64),
    IconVariant(filename: "icon_128x128.png", pixels: 128),
    IconVariant(filename: "icon_128x128@2x.png", pixels: 256),
    IconVariant(filename: "icon_256x256.png", pixels: 256),
    IconVariant(filename: "icon_256x256@2x.png", pixels: 512),
    IconVariant(filename: "icon_512x512.png", pixels: 512),
    IconVariant(filename: "icon_512x512@2x.png", pixels: 1024),
]

func drawIcon(in rect: NSRect) {
    let tile = rect.insetBy(dx: rect.width * 0.06, dy: rect.height * 0.06)
    let radius = tile.width * 0.225
    let tilePath = NSBezierPath(roundedRect: tile, xRadius: radius, yRadius: radius)

    let gradient = NSGradient(
        starting: NSColor(srgbRed: 0.26, green: 0.52, blue: 0.98, alpha: 1),
        ending: NSColor(srgbRed: 0.05, green: 0.25, blue: 0.72, alpha: 1)
    )
    gradient?.draw(in: tilePath, angle: -90)

    let globeRect = tile.insetBy(dx: tile.width * 0.22, dy: tile.height * 0.22)
    let lineWidth = max(1, rect.width * 0.026)
    NSColor.white.setStroke()

    let circle = NSBezierPath(ovalIn: globeRect)
    circle.lineWidth = lineWidth
    circle.stroke()

    NSGraphicsContext.saveGraphicsState()
    circle.addClip()

    for scale in [1.0, 0.55, 0.18] {
        let meridianRect = NSRect(
            x: globeRect.midX - globeRect.width * 0.5 * scale,
            y: globeRect.minY,
            width: globeRect.width * scale,
            height: globeRect.height
        )
        let meridian = NSBezierPath(ovalIn: meridianRect)
        meridian.lineWidth = lineWidth
        meridian.stroke()
    }

    for fraction in [1.0 / 3.0, 0.5, 2.0 / 3.0] {
        let y = globeRect.minY + globeRect.height * fraction
        let parallel = NSBezierPath()
        parallel.move(to: NSPoint(x: globeRect.minX, y: y))
        parallel.line(to: NSPoint(x: globeRect.maxX, y: y))
        parallel.lineWidth = lineWidth
        parallel.stroke()
    }

    NSGraphicsContext.restoreGraphicsState()
}

func pngData(pixels: Int) -> Data? {
    guard let representation = NSBitmapImageRep(
        bitmapDataPlanes: nil,
        pixelsWide: pixels,
        pixelsHigh: pixels,
        bitsPerSample: 8,
        samplesPerPixel: 4,
        hasAlpha: true,
        isPlanar: false,
        colorSpaceName: .deviceRGB,
        bytesPerRow: 0,
        bitsPerPixel: 0
    ) else {
        return nil
    }

    representation.size = NSSize(width: pixels, height: pixels)
    guard let context = NSGraphicsContext(bitmapImageRep: representation) else {
        return nil
    }

    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = context
    context.imageInterpolation = .high
    drawIcon(in: NSRect(x: 0, y: 0, width: pixels, height: pixels))
    context.flushGraphics()
    NSGraphicsContext.restoreGraphicsState()

    return representation.representation(using: .png, properties: [:])
}

let fileManager = FileManager.default
try? fileManager.removeItem(at: iconsetURL)
try fileManager.createDirectory(at: iconsetURL, withIntermediateDirectories: true)

for variant in variants {
    guard let data = pngData(pixels: variant.pixels) else {
        fputs("error: could not render \(variant.filename)\n", stderr)
        exit(1)
    }
    try data.write(to: iconsetURL.appendingPathComponent(variant.filename))
}

let iconutil = Process()
iconutil.executableURL = URL(fileURLWithPath: "/usr/bin/iconutil")
iconutil.arguments = ["-c", "icns", iconsetURL.path, "-o", icnsURL.path]
try iconutil.run()
iconutil.waitUntilExit()

guard iconutil.terminationStatus == 0 else {
    fputs("error: iconutil failed with status \(iconutil.terminationStatus)\n", stderr)
    exit(1)
}

try? fileManager.removeItem(at: iconsetURL)
print("Created \(icnsURL.path)")
