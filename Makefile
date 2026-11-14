.DEFAULT_GOAL := help

MACOS_DIR := apps/macos
WINDOWS_DIR := apps/windows

.PHONY: help check macos-build macos-test macos-verify macos-app macos-run macos-install macos-clean windows-build windows-test windows-verify windows-publish

help:
	@echo "Defaulty"
	@echo ""
	@echo "  make check           Run automated checks (currently macOS tests)"
	@echo "  make macos-build     Build the macOS app (debug)"
	@echo "  make macos-test      Run macOS unit tests"
	@echo "  make macos-verify    Tests + app bundle + Info.plist + signature checks"
	@echo "  make macos-app       Build apps/macos/build/Defaulty.app"
	@echo "  make macos-run       Build and launch the macOS app"
	@echo "  make macos-install   Install to INSTALL_DIR (default: /Applications)"
	@echo "  make macos-clean     Remove macOS build artifacts"
	@echo ""
	@echo "  make windows-build   Build the Windows solution (Release)"
	@echo "  make windows-test    Run Windows tests"
	@echo "  make windows-verify  Build + tests + single-file publish"
	@echo "  make windows-publish Publish self-contained single-file win-x64 artifact"

check: macos-test

macos-build:
	$(MAKE) -C $(MACOS_DIR) build

macos-test:
	$(MAKE) -C $(MACOS_DIR) test

macos-verify:
	$(MAKE) -C $(MACOS_DIR) verify

macos-app:
	$(MAKE) -C $(MACOS_DIR) app

macos-run:
	$(MAKE) -C $(MACOS_DIR) run

macos-install:
	$(MAKE) -C $(MACOS_DIR) install INSTALL_DIR="$(or $(INSTALL_DIR),/Applications)"

macos-clean:
	$(MAKE) -C $(MACOS_DIR) clean

windows-build:
	cd $(WINDOWS_DIR) && dotnet build Defaulty.sln -c Release

windows-test:
	cd $(WINDOWS_DIR) && dotnet test Defaulty.sln -c Release

windows-publish:
	cd $(WINDOWS_DIR) && pwsh packaging/publish.ps1

windows-verify: windows-build windows-test windows-publish
	@test -f $(WINDOWS_DIR)/artifacts/win-x64/Defaulty.exe || (echo "missing artifact: $(WINDOWS_DIR)/artifacts/win-x64/Defaulty.exe" && exit 1)
	@echo "Verified $(WINDOWS_DIR)/artifacts/win-x64/Defaulty.exe"
