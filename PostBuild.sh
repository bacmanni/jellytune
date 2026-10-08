#!/bin/bash

sed \
  -e "s|\${INSTALL_EXEC}|${INSTALL_EXEC}|g" \
  -e "s|\${APP_BASE_NAME}|${APP_BASE_NAME}|g" \
  -e "s|\${APP_FRIENDLY_NAME}|${APP_FRIENDLY_NAME}|g" \
  -e "s|\${APP_ID}|${APP_ID}|g" \
  -e "s|\${APP_VERSION}|${APP_VERSION}|g" \
  -e "s|\${DESKTOP_NODISPLAY}|${DESKTOP_NODISPLAY}|g" \
  -e "s|\${DESKTOP_TERMINAL}|${DESKTOP_TERMINAL}|g" \
  -e "s|\${PRIME_CATEGORY}|${PRIME_CATEGORY}|g" \
  JellyTune.desktop \
  > "Deploy/JellyTune.desktop"
