#!/bin/bash

PROJECT_FILE="JellyTune.Gnome/JellyTune.Gnome.csproj"
OUTPUT_JSON="JellyTune.Gnome/nuget-sources.json"
GENERATOR_SCRIPT="flatpak-dotnet-generator.py"

python3 "$GENERATOR_SCRIPT" "$OUTPUT_JSON" "$PROJECT_FILE"

if [ -f "$OUTPUT_JSON" ]; then
    if command -v sha256sum &> /dev/null; then
        HASH=$(sha256sum "$OUTPUT_JSON" | awk '{ print $1 }')
    else
        HASH=$(shasum -a 256 "$OUTPUT_JSON" | awk '{ print $1 }')
    fi

    echo "$HASH"
else
    exit 1
fi
