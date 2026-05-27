# REPO Native Third Person Action Movement

Third-person camera fork for R.E.P.O. built on top of the original native-camera approach from Team_Bingus.

This fork keeps the original idea of using REPO's own `CameraPosition.OverridePosition` path instead of spawning a second camera, then extends it with action-style movement, always-on shoulder third-person, improved grabbing, map support, and custom player model compatibility.

## Original Source

This mod is based on **Team_Bingus - Repo Native ThirdPerson**:

- Original package: [Team_Bingus/Repo_Native_ThirdPerson](https://thunderstore.io/c/repo/p/Team_Bingus/Repo_Native_ThirdPerson/)

This fork is not affiliated with Team_Bingus.

## Highlights

- Native third-person camera override
- Always-on right-shoulder third-person view
- Camera-relative action movement
- Visible local player body and cosmetics while active
- Stable custom-player-model support
- Crosshair-preserving first-person / third-person toggle
- Camera-centered selection and grabbing with character-origin grab distance
- Camera collision that pulls the shoulder camera back toward the character head
- Third-person map/tablet overlay support
- Debug grab rays and points for troubleshooting
- Client-side only; other players do not need the mod

## Version 1.3.1 Changes

- Fixed third-person grabbing being able to grab objects between the camera and the character.
- Fixed held objects moving when the third-person camera is pushed closer by wall or obstacle collision.
- Fixed Head Grabber puke direction so it follows the character body direction in third-person.
- Reduced held-object wobble from obstructed third-person camera views by keeping grab logic on stable gameplay origins instead of visual camera collision or animated head/hand targets.
- Fixed third-person flashlight handling so the flashlight model, light cone, and left-arm pose point together.
- Fixed third-person grabber/flashlight arm poses so the local visible model reaches forward while grabbing or using the flashlight.
- Fixed custom player model compatibility so multiplayer player movement/facing is not affected by the local player's WASD direction.
- Disabled noisy camera snapshot logs and refined grab debug helper lines.

## Version 1.3.0 Changes

- Changed normal third-person into a permanent shoulder camera instead of switching into shoulder view only while grabbing.
- Fixed third-person grab distance so grabbed objects use the character/head origin instead of the shoulder camera position.
- Fixed grabbed objects being pulled into the camera on grab start.
- Changed grab targeting to use the camera crosshair while keeping the actual grab origin on the character.
- Changed camera collision so obstruction pull-in collapses toward the character head.
- Improved crosshair synchronization when switching between first-person and third-person.
- Changed true grab origin to the local player head center, improving pickup behavior near tables.
- Preserved third-person body facing when switching FP -> TP after rotating the view.
- Added visible grab debug objects and color-coded grab state lines.
- Improved map/tablet behavior in third-person and added a local screen-style map overlay.
- Disabled the near-camera body fade/outline behavior by default so the local model remains visible.

## Important

If you install this fork, remove the original `Team_Bingus-Repo_Native_ThirdPerson` package first.

This fork keeps the same plugin GUID and DLL name so it can replace the original cleanly, but that also means both packages should not be installed at the same time.

## Install

Install with Thunderstore / r2modman / Gale, or place `RepoThirdPerson.dll` in a BepInEx plugins folder.

## Controls

- Toggle third person: `X`
- Hold right mouse button to make the character face the crosshair direction
- Optional zoom/offset controls exist in config, but runtime tuning is locked by default for the tested release camera

## Main Config

The generated config file is `com.reponativemods.thirdperson.cfg`.

Useful options:

- `Action Movement.Enabled`
- `Action Movement.TurnSpeed`
- `Action Movement.WhileGrabbing`
- `Camera.OffsetX`
- `Camera.OffsetY`
- `Camera.DefaultDistance`
- `Camera.MinDistance`
- `Camera.MaxDistance`
- `Camera.CollisionRadius`
- `Camera.LockRuntimeTuning`
- `Camera.LocalTransparencyMode`
- `Interaction.CameraCenteredSelection`
- `Debug.ShowGrabSelection`
- `Compatibility.ForceLocalModdedModel`

## Bug Reports

Please report bugs with:

- what happened
- what you expected
- whether you were in first-person or third-person
- what item/object you were grabbing, if relevant
- screenshots or short clips if possible
- your mod list if another camera/player-model mod may be involved

Contact Discord ID: `211491216032923648`

## Source Layout

The mod still builds into one `RepoThirdPerson.dll` for BepInEx/Thunderstore compatibility, but the plugin source is split by responsibility:

- `RepoThirdPerson_ActionMovement.cs`: plugin state, config, camera, movement, selection, grabbing, compatibility core
- `Plugin.Patches.cs`: Harmony patch entry points
- `Plugin.AvatarVisuals.cs`: local avatar, flashlight, arm-pose, and visible-model helpers
- `Plugin.DebugVisuals.cs`: camera/grab debug points and line renderers
