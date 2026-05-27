# Changelog

## 1.3.2

- Refined camera collision smoothing so camera pulls in quickly when obstructed but returns back out smoothly.

## 1.3.1

- Fixed third-person grabbing being able to target objects between the camera and the character.
- Fixed third-person held objects reacting to camera collision pull-in when the view is pushed closer by walls or obstacles.
- Fixed Head Grabber puke direction in third-person so it follows the character body direction instead of the camera crosshair.
- Reduced held-object wobble when the third-person camera is obstructed by separating grab logic from visual camera collision and animated head/hand look targets.
- Fixed local third-person flashlight aiming so the flashlight model, light cone, and left-arm pose stay synchronized.
- Fixed third-person grabber/flashlight arm poses so the local model visibly reaches forward while grabbing or using the flashlight.
- Fixed custom-player-model compatibility so local third-person facing fixes are not applied to every player in multiplayer.
- Disabled noisy periodic camera snapshot logs during normal play.
- Adjusted grab debug visuals so the yellow helper ray starts from the visible character body while gameplay grabbing remains driven by the crosshair path.
- Kept the v1.3.0 permanent shoulder-camera behavior and original flashlight behavior.

## 1.3.0

- Made third-person use a permanent right-shoulder camera.
- Removed grab-only shoulder camera transitions to avoid camera/aim state issues while grabbing.
- Fixed third-person grab distance so initial/max grab distance is based on the character/head origin instead of the shoulder camera.
- Fixed grabbed objects being pulled directly into the camera on grab start.
- Improved third-person crosshair selection while preserving character-origin grabbing.
- Changed camera collision pull-in so obstruction compression collapses toward the character head.
- Fixed FP/TP crosshair synchronization when toggling perspective.
- Changed the true third-person grab origin to the local player head center.
- Fixed stale body facing when switching back to third-person after rotating in first-person.
- Added visible grab debug rays/points with state colors.
- Added third-person map/tablet overlay support.
- Disabled near-camera local body fade/outline behavior by default.
- Improved custom local player model visibility and compatibility.
- Added bug report and source-code links through GitHub.

## 1.2.0

- Added camera-relative action movement on top of the native third-person camera path.
- Reworked root rotation handling so `A`, `S`, and `D` no longer shake, fling, or snap incorrectly.
- Moved third-person camera updates to `LateUpdate` only to remove local player jitter while rotating the camera.
- Added smoother visible body turning for better-looking third-person movement.
- Fixed broken third-person recovery after death, restart, or entering a new run.
- Improved local custom-model compatibility for visible third-person player models.
- Changed right mouse button behavior so the character can face the crosshair direction for flashlight aiming.
- Removed old experimental ghosting-related config options from the public config.

## 1.1.1

- Improved grab and interaction alignment when using adjusted third-person offsets.
- Selection now targets the rendered camera center while keeping the interaction origin on the avatar.

## 1.1.0

- Added in-game camera offset adjustment with the arrow keys.
- Added a reset key for restoring the loaded config offsets.

## 1.0.1

- Rebuilt the Thunderstore package with the latest third-person camera code.
- Preserved the existing package name, dependency, config keys, and client-side behavior.

## 1.0.0

- Initial Thunderstore package.
