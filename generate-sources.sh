#!/bin/bash

# Array of project files
PROJECTS=(
    "JellyTune.Gnome/JellyTune.Gnome.csproj"
    "JellyTune.Shared/JellyTune.Shared.csproj"
)

OUTPUT_JSON="nuget-sources.json"
GENERATOR_SCRIPT="flatpak-dotnet-generator.py"

# Remove old file if exists
rm -f "$OUTPUT_JSON"

# Loop through all projects and feed them to the generator
for PROJECT_FILE in "${PROJECTS[@]}"; do
    echo "Processing $PROJECT_FILE"
    python3 "$GENERATOR_SCRIPT" "$OUTPUT_JSON" "$PROJECT_FILE"
done

# Verify output
if [ -f "$OUTPUT_JSON" ]; then
    if command -v sha256sum &> /dev/null; then
        HASH=$(sha256sum "$OUTPUT_JSON" | awk '{ print $1 }')
    else
        HASH=$(shasum -a 256 "$OUTPUT_JSON" | awk '{ print $1 }')
    fi

    echo "$HASH"
else
    echo "Failed to generate $OUTPUT_JSON"
    exit 1
fi

