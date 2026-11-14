.PHONY: build test verify publish clean help

build:
	dotnet build Defaulty.sln -c Release

test:
	dotnet test Defaulty.sln -c Release

publish:
	pwsh packaging/publish.ps1

verify: build test publish
	@test -f artifacts/win-x64/Defaulty.exe

clean:
	rm -rf artifacts bin obj tests/*/bin tests/*/obj src/*/bin src/*/obj

help:
	@echo "Defaulty Windows targets:"
	@echo "  build    Build the solution (Release)"
	@echo "  test     Run all tests"
	@echo "  publish  Publish self-contained single-file win-x64 artifact"
	@echo "  verify   Build + tests + publish + artifact check"
	@echo "  clean    Remove build outputs"