#!/bin/bash

# Array of project files
PROJECTS=(
    "JellyTune.Gnome/JellyTune.Gnome.csproj"
    "JellyTune.Shared/JellyTune.Shared.csproj"
)

OUTPUT_JSON="JellyTune.Gnome/nuget-sources.json"
GENERATOR_SCRIPT="flatpak-dotnet-generator.py"

# Remove old file if exists
rm -f "$OUTPUT_JSON"

# Build argument list: first the output, then all project files
ARGS=("$OUTPUT_JSON")
for PROJECT_FILE in "${PROJECTS[@]}"; do
    echo "Including $PROJECT_FILE"
    ARGS+=("$PROJECT_FILE")
done

# Single call that processes ALL projects
python3 "$GENERATOR_SCRIPT" "${ARGS[@]}"

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

