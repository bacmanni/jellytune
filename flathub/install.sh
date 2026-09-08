#!/usr/bin/env bash
set -e

echo "Building JellyTune for runtime: ${RUNTIME}"

dotnet publish JellyTune.Gnome/JellyTune.Gnome.csproj -c Release -o ./publish --source nuget-sources --self-contained true --runtime ${RUNTIME}
mkdir -p ${FLATPAK_DEST}/bin
cp -r --remove-destination ./publish/* ${FLATPAK_DEST}/bin
install -Dm644 Icons/JellyTune.svg ${FLATPAK_DEST}/share/icons/hicolor/scalable/apps/io.github.bacmanni.jellytune.svg
install -Dm644 flathub/io.github.bacmanni.jellytune.desktop ${FLATPAK_DEST}/share/applications/io.github.bacmanni.jellytune.desktop
install -Dm644 flathub/io.github.bacmanni.jellytune.metainfo.xml ${FLATPAK_DEST}/share/metainfo/io.github.bacmanni.jellytune.metainfo.xml
