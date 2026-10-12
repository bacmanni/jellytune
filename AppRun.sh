#!/bin/bash

# Check the system's glibc version
# Ubuntu 26.04 uses glibc 2.43
SYSTEM_GLIBC=$(ldd --version | head -n1 | awk '{print $NF}')
REQUIRED_GLIBC="2.43"

# Sort the versions to see if the system has at least the required version
if [ "$(printf '%s\n' "$REQUIRED_GLIBC" "$SYSTEM_GLIBC" | sort -V | head -n1)" != "$REQUIRED_GLIBC" ]; then
    ERROR_MSG="This AppImage requires glibc $REQUIRED_GLIBC or newer (e.g., Ubuntu 26.04+).\nYour system has glibc $SYSTEM_GLIBC."
    
    # Try to show a GUI error message if zenity is installed, otherwise print to terminal
    if command -v zenity &> /dev/null; then
        zenity --error --text="$ERROR_MSG" --title="System Too Old"
    else
        echo -e "\nERROR: $ERROR_MSG\n"
    fi
    exit 1
fi

# If the check passes, launch the actual application
# AppRun scripts are passed the path to the extracted AppImage via $APPDIR
exec "$APPDIR/usr/bin/JellyTune" "$@"
