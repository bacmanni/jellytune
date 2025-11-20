## JellyTune, Gnome audio player for Jellyfin

![Screenshot of JellyTune](Screenshots/Wide.png)

My idea is to have native Gnome app for Jellyfin that is also fully supported for mobile devices. Nothing more, nothing less. So simple audio player. That means no massive edit functionality and no video playback. Main focus is on audio. My wish is to use this on my mobile Linux device at some point.

For now, it only supports one audio collection at the time. That is the way I use Jellyfin. If you are using some other way, then leave a comment through support ticket and I see what we can do about it. Same thing goes to playlists when they are implemented.

Oh, and the whole reason for this all is that I like to buy records and have them locally. I don't use streaming services for music :)

## Requirements
JellyTune for Gnome requires at least libadwaita 1.6. So everything above Gnome 45 should be fine. Only Linux is supported, but I have run the app on MacOS.

## Packages
There will only be two ways to get JellyTune. Downloading appimage or fetching it through flatpak.

## Project
Project is written in C# and uses following libraries: Gir.Core, SoundFlow and Jellyfin-Api

JellyTune is separated in two projects.
JellyTune.Shared - Can be used for shared functionality if there will be similiar player for example KDE.
JellyTune.Gnome - Gnome UI of JellyTune

## Thanks
My thanks for the people who have and are working with these:
[Gir.Core](https://github.com/gircore/gir.core),
[SoundFlow](https://github.com/LSXPrime/SoundFlow) and
[Jellyfin](https://jellyfin.org)
 
Also special thanks to Ruut Kiiskilä for providing the logo to JellyTune. Nice job!

Last mention to [Jetbrains](https://www.jetbrains.com/) for providing open source licenses of their products. Thank you.

## Planned versions:

**1.0 Beta (Only source code)**
- Startup wizard for creating Jellyfin server configuration
- Basic functionality for playing Jellyfin audio from single album and single collection
- Basic support for playlist and collections

**1.0 (Release packages)**
- Flatpack support
- Appimage support

**1.1**
- MPRIS support

**1.2**
- User can add/modify/delete collection/playlists

**1.3**
- Feel free to suggest features
