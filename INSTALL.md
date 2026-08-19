This is just a checklist when creating new version
[] Run: dotnet tool run Nickvision.FlatpakGenerator generate -d 9 -i JellyTune.Gnome/JellyTune.Gnome.csproj -o flathub/nuget-sources.json -s false
[] Update CHANGES-file
[] Check that JellyTune.pupnet.conf and update AppVersionRelease to match release. Also check FlatpakPlatformVersion
