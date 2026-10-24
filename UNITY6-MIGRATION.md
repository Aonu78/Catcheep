# Unity 6 migration

Open this directory (`Catcheep`) in **Unity 6000.6.4f1**. The version used by Unity is recorded in `ProjectSettings/ProjectVersion.txt`; the loose files at the repository root are leftovers from the original project.

Start the game from **Assets/Opening.unity**. That scene creates the persistent music, timer, and Play Games services. The opening leads to the introduction on a new save, then the menu. Desktop players can use the mouse to catch sheep; Android players use touch.

## Changes

- Migrated the old TextMesh Pro DLL references in scenes, prefabs, fonts, and settings to the text system provided by Unity UI. Font atlas assets and game artwork are retained.
- Updated advertising to the Unity Ads 4 initialization, explicit loading, and completion callbacks. Rewards are granted only after a completed rewarded ad. Ads are optional in the editor and Windows builds.
- Replaced Google Play Games 0.9.40 and its old resolver/native libraries with the official Google Play Games 2.2.1 package and External Dependency Manager 1.2.182. The existing game ID and achievement/leaderboard IDs are retained.
- Replaced the old Android notifications AAR with Unity Mobile Notifications 2.5.0, including notification channels and Android notification permission handling.
- Fixed the loading screen's loop so it yields on every frame and loads the requested scene through the loading scene.
- Replaced the removed `WWW` API in network time requests and added a five-second timeout so the daily-spin fallback can run when the old time server is unavailable.
- Replaced construction of a `MonoBehaviour` with a component lookup in sheep movement.
- Updated catching to handle each touch press once, tolerate arbitrary finger IDs, ignore UI presses, and support the mouse.
- Removed the obsolete `TouchInputModule` from the switch editor tool; `StandaloneInputModule` handles both mouse and touch.

Original plugins are kept in **LegacyPlugins/**, outside Unity's asset import tree. Do not copy them back into Assets alongside the current packages.

The unreferenced 86 MB motion-capture sample `Mixed Motions.fbx` and its metadata are preserved in **LegacyAssets/Animatii/**. Its obsolete importer was delaying Unity 6 asset imports; the game does not reference this model.

## Verification commands

Close the Unity editor before running batch verification from PowerShell:

```powershell
./tools/verify-unity6.ps1 -Target Validate
./tools/verify-unity6.ps1 -Target Windows
./tools/verify-unity6.ps1 -Target Android
```

Validation checks the enabled scenes and game prefabs for missing scripts, upgrades the existing font data, and checks text components. Windows verification builds a development player and runs an automated playthrough of the introduction, menu, loading screen, Farm, Snow, City, Alien, and Credit scenes. The QA player uses a separate company/product name to isolate its save data from the original game.

Logs are written to **Logs/**. Build output goes to **Builds/WindowsSmoke/** and **Builds/Android/**. The Android verification build uses ARM64 and IL2CPP.

## Connected services

The Android Ads game ID (`1453095`), ad units (`video`, `rewardedVideo`), and Play Games app ID (`231330244512`) come from the original project. Their current server-side configuration must be checked in their respective dashboards. Google Play Games sign-in, achievements, and leaderboards also require a correctly registered package name and signing certificate. Offline gameplay does not require sign-in or an available ad.

Real ad delivery, Android notification display, and Google Play Games authentication require an Android device and the corresponding service configuration. A local compile or player smoke test cannot verify those services.
