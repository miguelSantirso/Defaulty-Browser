INSTALL_DIR ?= /Applications

.PHONY: build test verify app run install clean help

build:
	swift build

test:
	swift test

verify: test app
	@plutil -lint build/Defaulty.app/Contents/Info.plist
	@test "$$(/usr/libexec/PlistBuddy -c 'Print :LSUIElement' build/Defaulty.app/Contents/Info.plist)" = "true"
	@codesign --verify --strict build/Defaulty.app
	@echo "Verified build/Defaulty.app"

app:
	./Scripts/build-app.sh

run: app
	open build/Defaulty.app

install: app
	@mkdir -p "$(INSTALL_DIR)"
	rm -rf "$(INSTALL_DIR)/Defaulty.app"
	cp -R build/Defaulty.app "$(INSTALL_DIR)/Defaulty.app"
	@echo "Installed $(INSTALL_DIR)/Defaulty.app"

clean:
	swift package clean
	rm -rf build

help:
	@echo "Defaulty macOS targets:"
	@echo "  build    Build the Swift package (debug)"
	@echo "  test     Run the unit tests"
	@echo "  verify   Run tests, build the app, and check the plist and signature"
	@echo "  app      Build build/Defaulty.app and ad-hoc sign it"
	@echo "  run      Build the app and open it"
	@echo "  install  Build the app and copy it to INSTALL_DIR (default: /Applications)"
	@echo "  clean    Remove Swift and app build artifacts"
