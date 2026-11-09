.DEFAULT_GOAL := help

MACOS_DIR := apps/macos

.PHONY: help check macos-build macos-test macos-verify macos-app macos-run macos-install macos-clean

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
	@echo "Windows targets will be added with the Windows implementation."

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
