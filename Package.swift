// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "Defaulty",
    platforms: [
        .macOS(.v13),
    ],
    products: [
        .library(name: "DefaultyCore", targets: ["DefaultyCore"]),
        .executable(name: "Defaulty", targets: ["Defaulty"]),
    ],
    targets: [
        .target(name: "DefaultyCore"),
        .executableTarget(
            name: "Defaulty",
            dependencies: ["DefaultyCore"]
        ),
        .testTarget(
            name: "DefaultyCoreTests",
            dependencies: ["DefaultyCore"]
        ),
    ]
)
