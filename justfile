set windows-shell := ["cmd.exe", "/c"]

icon_cache := AppData + "\\LeagueMasteryOverlay\\icons"
settings_file := AppData + "\\LeagueMasteryOverlay\\settings.json"

# List available recipes
default:
    @just --list

# Build the project (Debug)
build:
    dotnet build --configuration Debug

# Build the project (Release)
build-release:
    dotnet build --configuration Release

# Run the app (Debug build)
run:
    dotnet run --configuration Debug

# Clean build artifacts
clean:
    dotnet clean
    rmdir /s /q bin 2>nul || exit 0
    rmdir /s /q obj 2>nul || exit 0

# Remove the cached icons from AppData
clean-icons:
    rmdir /s /q "{{icon_cache}}" 2>nul || exit 0
    @echo Icon cache cleared.

# Remove all persisted app data (icons + settings)
clean-appdata: clean-icons
    del /f /q "{{settings_file}}" 2>nul || exit 0
    @echo App data cleared.

# List the currently cached icon files
list-icons:
    @if exist "{{icon_cache}}" ( dir /b "{{icon_cache}}" ) else ( echo No icons cached yet. )

# Run the app then list cached icons afterwards (useful for validating downloads)
run-and-verify: run list-icons
