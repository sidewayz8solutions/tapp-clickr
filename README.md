# Tapp Bird Clickr — Unity Edition

A relaxing woodpecker tapper. Tap to peck trees, break them for coins, buy upgrades.
**Every art asset and sound effect is procedurally generated in C# at runtime** —
there are no PNG/XMP/WAV files in the project, so nothing can go stale or look "asset-storey".

Targets: portrait iOS (and runs on Android/desktop editor for testing that tap).

## What's here

```
Assets/
  Scenes/Bootstrap.unity        # empty scene — the game builds itself at runtime
  Scripts/
    Core/GameBootstrap.cs       # entry point: builds camera, world, characters, UI (no GUID wiring)
    Core/GameManager.cs         # persistent state (PlayerPrefs JSON), buying upgrades, events
    Core/GameConfig.cs          # ALL tuning: tree health, coin reward, upgrade costs, layout
    Art/ArtGen.cs               # draws every sprite (bird, tree, sky, clouds, coins, UI)
    Art/Palette.cs              # the color palette (warm pastel cartoon)
    Utils/ArtCanvas.cs          # tiny software rasterizer (polygons, ellipses, gradients, AA)
    Utils/Tween.cs              # tween engine (scale/rotate/move/easing/screen shake)
    Utils/Sfx.cs                # synth-generated SFX: peck, break, coin, buy, pop
    Gameplay/Bird.cs            # peck lunge animation (squash & stretch), idle bob
    Gameplay/TreeBlock.cs       # tree health, hit shake/flash
```

## Play

1. Install **Unity Hub** (see below), install a **Unity 6** editor (any 6000.0.x), open this folder.
2. Unity asks to open with your installed version — accept. Let it import (generates `.meta` files).
3. Open `Assets/Scenes/Bootstrap.unity`.
4. Press **Play**. Tap to peck; the shop button opens upgrades; the speaker toggles sound.

Because everything is built from `[RuntimeInitializeOnLoadMethod]`, the scene stays blank in the editor —
the whole game appears in Play mode. There are **no script GUID references** to break.

## Unity install on this machine

This box is a Flatpak environment (`Freedesktop SDK 25.08`), so install Unity Hub from Flathub:

```
flatpak install flathub com.unity.UnityHub
flatpak run com.unity.UnityHub
```

In Hub: **Installs → Install Editor → Unity 6 LTS (6000.0.x)** (Editor; Linux). About 4–6 GB.
Then sign in / activate a **Unity Personal license** (free, needs a Unity account).
Note: the editor is RAM- and VRAM-hungry — close other apps first (this box has ~3 GB free RAM).

> `ProjectSettings/ProjectVersion.txt` pins `6000.0.32f1`; if you install a different patch
> Unity will offer to switch — that's expected and harmless.

If the Flatpak editor is too slow here, the same folder opens unchanged on a Windows/Mac machine.

## Tuning (fastest iteration)

All balance + layout numbers live in `Assets/Scripts/Core/GameConfig.cs`:

| Constant | Effect |
| --- | --- |
| `TreeHealth(n)` / `CoinsForTree(n)` | difficulty & reward curve |
| `PeckPower`, `AutoPeckInterval`, `CoinBoost` | upgrade formulas |
| `UpgradeCost(type, level)` / `MaxUpgradeLevel` | shop prices |
| `GroundLine`, `TreePos`, `BirdPos` | world layout & scale |
| `WorldWidth` | how much world fits the screen (portrait) |

Art tweaks: colors in `Art/Palette.cs`; every shape in `Art/ArtGen.cs`
(seeded RNG = identical art every run until you change it).

## iOS build via GitHub Actions (no Mac needed)

A workflow is included: `.github/workflows/ios-build.yml`. It runs on GitHub's
cloud macOS runner, so you never need a Mac. What it does:

1. Downloads the exact Unity editor (`6000.0.32f1`) with the iOS module
2. Runs `TappBird.EditorTools.BuildTools.BuildiOS` (headless) → Xcode project
3. Compiles the Xcode project on the runner (unsigned) and zips `TappBird-unsigned.ipa`
4. Uploads two artifacts: the Xcode project and the unsigned `.ipa`

### One-time setup

1. **Push this project to GitHub** (empty repo, no README is fine):
   ```
   git init && git add -A && git commit -m "Tapp Bird Clickr — Unity"
   git remote add origin git@github.com:YOUR_USER/REPO.git
   git push -u origin main
   ```
2. **Create the `UNITY_LICENSE` secret.** Unity must be activated once, anywhere,
   then the license file is base64'd into a GitHub Actions secret:
   - Install Unity Hub/editor on *any* machine (this Linux box is fine via Flathub),
     open the project once, log in with your Unity ID (Personal license is free).
     Exit Unity.
   - The license file lives at (Linux) `~/.local/share/unity3d/Unity/Unity_lic.ulf`,
     (macOS) `~/Library/Application Support/Unity/Unity_lic.ulf`,
     (Windows) `%USERPROFILE%\AppData\Local\Unity\Editor\Unity_lic.ulf`.
   - Generate its base64: `base64 -w0 Unity_lic.ulf` and paste into
     GitHub → repo → **Settings → Secrets and variables → Actions → New repository secret**
     named `UNITY_LICENSE`. (Repos with a Unity license in a secret don't need
     email/password secrets when using `game-ci/unity-builder` + `.ulf`.)
3. Run the workflow: **Actions → "iOS build" → Run workflow**. ~30–60 min.
   Download `TappBird-unsigned-ipa` (and optionally the `Unity-iPhone-project`).

### What you get & what to do with it

- **`TappBird-unsigned.ipa`** — won't install on a device as-is. You must re-sign it:
  with a Mac (`codesign`/Xcode), or cross-sign on Linux with `zsign`:
  ```
  zsign -k "DistributionCert.p12" -p "$CERT_PASS" -m "Profile.mobileprovision" \
        -o TappBird-signed.ipa TappBird-unsigned.ipa
  ```
  Choose the cert type to match the provisioning profile:
  **App Store** → upload via Transporter, or **Ad Hoc** → install directly to test iPhones.
- **Signing fully in CI** (auto-signed `.ipa`, no manual step): add secrets for a
  distribution cert (exported `.p12`) + mobileprovision and a second job that
  imports them into a keychain before `xcodebuild`. Happy to add that job on request.
- For **TestFlight/App Store**, an Apple Developer account ($99/yr) is required.

### Version match

The workflow pins the editor in `env.UNITY_VERSION` and `ProjectVersion.txt` is set to
`6000.0.32f1`. If you install a different patch locally, update **both** to the same
string (also needed so CI artifacts match your local testing).

## iOS build (from a Mac, requires Xcode)

For reference if you ever rent/buy a Mac instead of CI:

```
Open the project → File → Build Settings → iOS → Build
```

Export from Xcode with a Team + bundle id. Bundle id is preconfigured: `com.tappbird.clickr`.

## Notes / known limits

- Tap input uses legacy Input Manager (`projectsettings: activeInputHandler 0`). If input ever stops
  working, check Project Settings → Player → Active Input Handling is **Input Manager (Old)**.
- The bird pecks toward the tree visually; chips burst at the trunk's peck hole.
- Sound is synthesized; on iOS, ensure the device isn't muted for audio to play.
- (PlayerPrefs is not the right storage for production — swap `GameManager` to a proper save system later.)