# REPO Native Third Person Action Movement

Language: [中文](#中文) | [English](#english)

Source code / bug reports: [sam0324sam/REPO-Native-Third-Person-Action-Movement](https://github.com/sam0324sam/REPO-Native-Third-Person-Action-Movement)

---

## 中文

這是 R.E.P.O. 的第三人稱攝影機 fork，基於 Team_Bingus 原本的 native-camera 做法。

本 fork 保留使用遊戲原生 `CameraPosition.OverridePosition` 路徑的概念，不另外生成第二台攝影機，並加入動作遊戲式移動、常駐越肩第三人稱、改良抓取、地圖支援與自訂玩家模型相容性。

### 原始來源

此 mod 基於 **Team_Bingus - Repo Native ThirdPerson**：

- 原始 package: [Team_Bingus/Repo_Native_ThirdPerson](https://thunderstore.io/c/repo/p/Team_Bingus/Repo_Native_ThirdPerson/)

此 fork 與 Team_Bingus 無隸屬關係。

### 特色

- 使用遊戲原生第三人稱攝影機 override
- 常駐右側越肩第三人稱視角
- 依攝影機方向判定的 WASD 動作移動
- 第三人稱時顯示本機玩家身體與外觀
- 穩定支援自訂玩家模型
- FP / TP 切換時保留準心指向
- 以準心選取目標，但抓取距離以角色 origin 計算
- 視野碰到障礙物時，攝影機會朝角色頭部拉近
- 第三人稱地圖 / 平板 overlay 支援
- 可顯示抓取 debug 線與狀態點
- 純 client-side，其他玩家不需要安裝

### 1.3.3 更新

- 優化相機碰撞平滑時間，當視野被牆壁擋住時能快速拉近，離開牆壁時能平滑回彈。
- 修正第三人稱相機覆寫模式下，導致部分需要螢幕判定（OnScreen）觸發的怪物機制失效的問題。
- 修正本地端第三人稱下，角色進行抓取或使用手電筒時手臂不會向前舉起伸出的問題。

### 1.3.1 更新

- 修正第三人稱會抓到「攝影機和角色之間」物品的問題。
- 修正視野被牆或障礙物拉近時，已抓取物品會跟著攝影機移動的問題。
- 修正 Head Grabber 抓住玩家時，嘔吐方向會跟第三人稱準心而不是角色身體方向的問題。
- 降低第三人稱攝影機被遮擋時，動畫頭部/手部造成抓取物晃動的問題。
- 修正第三人稱手電筒，讓手電筒模型、光源與左手姿勢保持同步。
- 修正第三人稱抓取/手電筒時，本機可見模型的手臂不會向前伸出的問題。
- 修正多人模式自訂玩家模型相容性，避免遠端玩家的移動/朝向被本機 WASD 方向影響。
- 關閉過量 camera snapshot log，並調整抓取 debug 輔助線顯示。

### 重要提醒

如果安裝此 fork，請先移除原本的 `Team_Bingus-Repo_Native_ThirdPerson` package。

此 fork 保留相同 plugin GUID 與 DLL 名稱，方便乾淨取代原版；也因此兩個 package 不應同時安裝。

### 安裝

使用 Thunderstore / r2modman / Gale 安裝，或將 `RepoThirdPerson.dll` 放進 BepInEx plugins 資料夾。

### 操作

- 切換第三人稱：`X`
- 按住滑鼠右鍵：讓角色面向準心方向
- 其他 zoom / offset 設定可在 config 中調整，但測試版預設會鎖定 runtime tuning

### 主要設定

設定檔會生成於 `com.reponativemods.thirdperson.cfg`。

常用選項：

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

### 問題回報

有問題請到 GitHub 發問或開 issue：

[sam0324sam/REPO-Native-Third-Person-Action-Movement](https://github.com/sam0324sam/REPO-Native-Third-Person-Action-Movement)

回報時建議附上：

- 發生了什麼
- 預期應該如何
- 當時是在第一人稱或第三人稱
- 如果與抓取有關，正在抓什麼物品
- 截圖或短影片
- mod list，尤其是其他攝影機或玩家模型 mod

### 原始碼結構

mod 仍會編譯成單一 `RepoThirdPerson.dll`，以維持 BepInEx / Thunderstore 相容性，但原始碼已依職責拆分：

- `RepoThirdPerson_ActionMovement.cs`：plugin 狀態、config、camera、movement、selection、grabbing、compatibility core
- `Plugin.Patches.cs`：Harmony patch 入口
- `Plugin.AvatarVisuals.cs`：本機角色、手電筒、手臂姿勢、可見模型輔助
- `Plugin.DebugVisuals.cs`：camera / grab debug 點與線

---

## English

Third-person camera fork for R.E.P.O. built on top of the original native-camera approach from Team_Bingus.

This fork keeps the original idea of using REPO's own `CameraPosition.OverridePosition` path instead of spawning a second camera, then extends it with action-style movement, always-on shoulder third-person, improved grabbing, map support, and custom player model compatibility.

### Original Source

This mod is based on **Team_Bingus - Repo Native ThirdPerson**:

- Original package: [Team_Bingus/Repo_Native_ThirdPerson](https://thunderstore.io/c/repo/p/Team_Bingus/Repo_Native_ThirdPerson/)

This fork is not affiliated with Team_Bingus.

### Highlights

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

### Version 1.3.3 Changes

- Refined camera collision smoothing so camera pulls in quickly when obstructed but returns back out smoothly.
- Fixed EnemyOnScreen detection being bypassed in third-person camera override mode, enabling screen-triggered enemy behavior.
- Fixed local third-person grabber and flashlight arm raising poses so the character model arms correctly reach forward.

### Version 1.3.1 Changes

- Fixed third-person grabbing being able to grab objects between the camera and the character.
- Fixed held objects moving when the third-person camera is pushed closer by wall or obstacle collision.
- Fixed Head Grabber puke direction so it follows the character body direction in third-person.
- Reduced held-object wobble from obstructed third-person camera views by keeping grab logic on stable gameplay origins instead of visual camera collision or animated head/hand targets.
- Fixed third-person flashlight handling so the flashlight model, light cone, and left-arm pose point together.
- Fixed third-person grabber/flashlight arm poses so the local visible model reaches forward while grabbing or using the flashlight.
- Fixed custom player model compatibility so multiplayer player movement/facing is not affected by the local player's WASD direction.
- Disabled noisy camera snapshot logs and refined grab debug helper lines.

### Important

If you install this fork, remove the original `Team_Bingus-Repo_Native_ThirdPerson` package first.

This fork keeps the same plugin GUID and DLL name so it can replace the original cleanly, but that also means both packages should not be installed at the same time.

### Install

Install with Thunderstore / r2modman / Gale, or place `RepoThirdPerson.dll` in a BepInEx plugins folder.

### Controls

- Toggle third person: `X`
- Hold right mouse button to make the character face the crosshair direction
- Optional zoom/offset controls exist in config, but runtime tuning is locked by default for the tested release camera

### Main Config

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

### Bug Reports

Please report bugs or ask questions on GitHub:

[sam0324sam/REPO-Native-Third-Person-Action-Movement](https://github.com/sam0324sam/REPO-Native-Third-Person-Action-Movement)

When reporting a bug, include:

- what happened
- what you expected
- whether you were in first-person or third-person
- what item/object you were grabbing, if relevant
- screenshots or short clips if possible
- your mod list if another camera/player-model mod may be involved

### Source Layout

The mod still builds into one `RepoThirdPerson.dll` for BepInEx/Thunderstore compatibility, but the plugin source is split by responsibility:

- `RepoThirdPerson_ActionMovement.cs`: plugin state, config, camera, movement, selection, grabbing, compatibility core
- `Plugin.Patches.cs`: Harmony patch entry points
- `Plugin.AvatarVisuals.cs`: local avatar, flashlight, arm-pose, and visible-model helpers
- `Plugin.DebugVisuals.cs`: camera/grab debug points and line renderers
