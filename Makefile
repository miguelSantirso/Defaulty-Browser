INSTALL_DIR ?= /Applications

.PHONY: build test app run install clean help

build:
	swift build

test:
	swift test

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
	@echo "Defaulty targets:"
	@echo "  build    Build the Swift package (debug)"
	@echo "  test     Run the unit tests"
	@echo "  app      Build build/Defaulty.app and ad-hoc sign it"
	@echo "  run      Build the app and open it"
	@echo "  install  Build the app and copy it to INSTALL_DIR (default: /Applications)"
	@echo "  clean    Remove Swift and app build artifacts"
