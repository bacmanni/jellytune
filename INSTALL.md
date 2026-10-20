This is just a checklist when creating new version

Nugets changed:
[] Run: dotnet tool run Nickvision.FlatpakGenerator generate --dotnet 9 --freedesktop 51 -i JellyTune.Gnome/JellyTune.Gnome.csproj -o flathub/gnome-nuget-sources.json -s true
[] Run: dotnet tool run Nickvision.FlatpakGenerator generate --dotnet 9 --freedesktop 51 -i JellyTune.Shared/JellyTune.Shared.csproj -o flathub/shared-nuget-sources.json -s true

Version update:
[] Update CHANGES-file
[] Check version in Program.cs at ApplicationInfo (at least version)
[] Check that JellyTune.pupnet.conf and update AppVersionRelease to match release. Also check FlatpakPlatformVersion
