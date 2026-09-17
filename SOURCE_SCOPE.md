# Source scope

This repository contains the project's nine authored C# files under `Assets/Game/Scripts`, plus the original Unity editor version and package manifest.

## Included

- circle-packing and spatial-hash implementation
- Unity spawning adapter
- camera movement and desktop/mobile input abstractions
- gameplay bootstrap and tick contract
- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`

## Intentionally excluded

- scenes, prefabs, art, audio, and other content assets
- Unity `.meta` files and generated folders
- third-party source, packages, and binaries
- builds, caches, logs, local IDE files, and user settings

This repository is intended for code review and is not a standalone playable build. The source references Unity APIs; packages listed in `Packages/manifest.json` are not vendored into the repository.

