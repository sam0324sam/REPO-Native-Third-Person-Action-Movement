using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;


[assembly: AssemblyCompany("RepoThirdPerson")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.3.0.0")]
[assembly: AssemblyInformationalVersion("1.3.0")]
[assembly: AssemblyProduct("RepoThirdPerson")]
[assembly: AssemblyTitle("RepoThirdPerson")]
[assembly: AssemblyVersion("1.3.0.0")]
namespace RepoThirdPerson;

[BepInPlugin("com.reponativemods.thirdperson", "REPO Native Third Person", "1.3.0")]
public sealed class Plugin : BaseUnityPlugin
{
	private struct ClipPlaneState
	{
		public float Near;

		public float Far;
	}

	private static class RepoUpdatePatches
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlayerController), "Update")]
		private static void PlayerControllerUpdatePostfix()
		{
			Instance?.TickInput();
		}

		[HarmonyPrefix]
		[HarmonyPriority(800)]
		[HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
		private static void PlayerControllerFixedUpdatePrefix(PlayerController __instance)
		{
			Instance?.BeginActionMovementRewrite(__instance);
		}

		[HarmonyTranspiler]
		[HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
		private static IEnumerable<CodeInstruction> PlayerControllerFixedUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> list = new List<CodeInstruction>(instructions);
			FieldInfo vector3Y = AccessTools.Field(typeof(Vector3), "y");
			MethodInfo euler = AccessTools.Method(typeof(Quaternion), "Euler", new Type[3]
			{
				typeof(float),
				typeof(float),
				typeof(float)
			}, (Type[])null);
			MethodInfo resolveYaw = AccessTools.Method(typeof(RepoUpdatePatches), "ResolveActionMovementYaw", (Type[])null, (Type[])null);
			bool patched = false;
			for (int i = 0; i < list.Count; i++)
			{
				yield return list[i];
				if (patched || !(list[i].opcode == OpCodes.Ldfld) || !object.Equals(list[i].operand, vector3Y))
				{
					continue;
				}
				for (int j = i + 1; j < list.Count && j <= i + 4; j++)
				{
					if (Calls(list[j], euler))
					{
						yield return new CodeInstruction(OpCodes.Call, (object)resolveYaw);
						patched = true;
						break;
					}
				}
			}
		}

		private static bool Calls(CodeInstruction instruction, MethodInfo method)
		{
			if (instruction.opcode == OpCodes.Call)
			{
				return object.Equals(instruction.operand, method);
			}
			return false;
		}

		private static float ResolveActionMovementYaw(float originalYaw)
		{
			if ((Object)(object)Instance == (Object)null)
			{
				return originalYaw;
			}
			if (Instance.IsLocalGrabActive() && Instance.TryGetGameplayAimYaw(out var yaw))
			{
				_hasRewriteTurnYaw = true;
				_lastRewriteTurnYaw = yaw;
				return yaw;
			}
			if ((IsActionMovementCameraLockHeld() || Instance.IsMapAimLockActive()) && Instance.TryGetGameplayAimYaw(out var yaw2))
			{
				_hasRewriteTurnYaw = true;
				_lastRewriteTurnYaw = yaw2;
				return yaw2;
			}
			if (!Instance.CanAcceptActionMovementInput())
			{
				return originalYaw;
			}
			if (_rewriteMovementInput)
			{
				return _rewriteTurnYaw;
			}
			if (!_hasRewriteTurnYaw)
			{
				return originalYaw;
			}
			return _lastRewriteTurnYaw;
		}

		internal static bool IsActionMovementCameraLockHeld()
		{
			if ((Object)(object)Instance != (Object)null && Instance.IsCameraLockTemporarilySuppressed())
			{
				return false;
			}
			if (Mouse.current != null && Mouse.current.rightButton != null)
			{
				return Mouse.current.rightButton.isPressed;
			}
			return Input.GetMouseButton(1);
		}

		[HarmonyPostfix]
		[HarmonyPriority(0)]
		[HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
		private static void PlayerControllerFixedUpdatePostfix(PlayerController __instance)
		{
			Instance?.TickActionMovementAfterFixedUpdate(__instance);
		}

		[HarmonyFinalizer]
		[HarmonyPriority(0)]
		[HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
		private static void PlayerControllerFixedUpdateFinalizer()
		{
			Instance?.EndActionMovementRewrite();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SemiFunc), "InputMovementX")]
		private static void SemiFuncInputMovementXPostfix(ref float __result)
		{
			if (_rewriteMovementInput)
			{
				__result = _rewriteMovementX;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SemiFunc), "InputMovementY")]
		private static void SemiFuncInputMovementYPostfix(ref float __result)
		{
			if (_rewriteMovementInput)
			{
				__result = _rewriteMovementY;
			}
		}

		[HarmonyPostfix]
		[HarmonyPriority(0)]
		[HarmonyPatch(typeof(PlayerAvatarVisuals), "Update")]
		private static void PlayerAvatarVisualsUpdatePostfix(PlayerAvatarVisuals __instance)
		{
			Instance?.TickActionMovementVisuals(__instance);
		}

		[HarmonyPriority(0)]
		private static void ModdedModelPlayerAvatarUpdatePostfix(object __instance)
		{
			Instance?.TickModdedModelPlayerAvatarPostUpdate(__instance);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CameraPosition), "Update")]
		private static void CameraPositionUpdatePostfix(CameraPosition __instance)
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CameraCrouchPosition), "Update")]
		private static void CameraCrouchPositionUpdatePostfix(CameraCrouchPosition __instance)
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CameraCrawlPosition), "Update")]
		private static void CameraCrawlPositionUpdatePostfix(CameraCrawlPosition __instance)
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlayerLocalCamera), "GetOverrideTransform")]
		private static void PlayerLocalCameraGetOverrideTransformPostfix(PlayerLocalCamera __instance, ref Transform __result)
		{
			Instance?.TryGetSelectionOverride(__instance, ref __result);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlayerLocalCamera), "GetOverrideActive")]
		private static void PlayerLocalCameraGetOverrideActivePostfix(PlayerLocalCamera __instance, ref bool __result)
		{
			Instance?.TryGetSelectionOverrideActive(__instance, ref __result);
		}

		[HarmonyPostfix]
		[HarmonyPriority(0)]
		[HarmonyPatch(typeof(FlashlightController), "Update")]
		private static void FlashlightControllerUpdatePostfix(FlashlightController __instance)
		{
			Instance?.TickFlashlightControllerPostUpdate(__instance);
		}
	}

	public const string PluginGuid = "com.reponativemods.thirdperson";

	public const string PluginName = "REPO Native Third Person";

	public const string PluginVersion = "1.3.0";

	private const string SelectionTransformName = "REPO Native Third Person Selection Transform";

	private const string ToggleActionName = "REPO Native Third Person Toggle";

	private const string ZoomInActionName = "REPO Native Third Person Zoom In";

	private const string ZoomOutActionName = "REPO Native Third Person Zoom Out";

	private const string DefaultLayerName = "Default";

	private const string GroundLayerName = "Ground";

	private const string WallLayerName = "Wall";

	private const string PlayerLayerName = "Player";

	private const float InputDeadZone = 0.001f;

	private const float MinimumCameraCastDistance = 0.001f;

	private const float MinimumSelectionDirectionSqrMagnitude = 0.0001f;

	private const float MinimumConfiguredDistance = 0.1f;

	private const float CameraPositionOverrideDuration = 0.1f;

	private const float ShowSelfOverrideDuration = 0.15f;

	private const float ObstructedMinimumDistance = 0.05f;

	private const float NearFadeStartDistance = 2.6f;

	private const float NearFadeEndDistance = 1.15f;

	private ConfigEntry<KeyCode> _toggleKey;

	private ConfigEntry<KeyCode> _zoomInKey;

	private ConfigEntry<KeyCode> _zoomOutKey;

	private ConfigEntry<KeyCode> _resetOffsetsKey;

	private ConfigEntry<float> _scrollSensitivity;

	private ConfigEntry<float> _keyZoomSpeed;

	private ConfigEntry<float> _offsetAdjustSpeed;

	private ConfigEntry<float> _offsetX;

	private ConfigEntry<float> _offsetY;

	private ConfigEntry<float> _defaultDistance;

	private ConfigEntry<float> _minDistance;

	private ConfigEntry<float> _maxDistance;

	private ConfigEntry<float> _collisionRadius;

	private ConfigEntry<float> _collisionPadding;

	private ConfigEntry<float> _minimumFarClipPlane;

	private ConfigEntry<float> _nearClipPlane;

	private ConfigEntry<bool> _cameraCenteredSelection;

	private ConfigEntry<string> _selectionOriginMode;

	private ConfigEntry<float> _selectionMaxDistance;

	private ConfigEntry<bool> _actionMovementEnabled;

	private ConfigEntry<float> _actionTurnSpeed;

	private ConfigEntry<bool> _actionMovementWhileGrabbing;

	private ConfigEntry<bool> _forceLocalModdedModel;

	private ConfigEntry<float> _cameraCloseSmoothTime;

	private ConfigEntry<float> _cameraFarSmoothTime;

	private ConfigEntry<float> _grabCameraDistance;

	private ConfigEntry<float> _grabCameraOffsetX;

	private ConfigEntry<float> _nearFadeStartZoomRatio;

	private ConfigEntry<float> _nearFadeMinAlpha;

	private ConfigEntry<bool> _lockRuntimeCameraTuning;

	private ConfigEntry<string> _localTransparencyMode;

	private ConfigEntry<float> _silhouetteScale;

	private ConfigEntry<float> _outlineWidth;

	private ConfigEntry<float> _outlineColorR;

	private ConfigEntry<float> _outlineColorG;

	private ConfigEntry<float> _outlineColorB;

	private ConfigEntry<float> _outlineColorA;

	private ConfigEntry<bool> _thirdPersonMapOverlay;

	private ConfigEntry<float> _mapOverlayX;

	private ConfigEntry<float> _mapOverlayY;

	private ConfigEntry<float> _mapOverlayZ;

	private ConfigEntry<float> _mapOverlayScale;

	private ConfigEntry<float> _mapOverlayPitch;

	private ConfigEntry<float> _mapOverlayYaw;

	private ConfigEntry<float> _mapOverlayRoll;

	private ConfigEntry<bool> _debugShowCameraPoints;

	private ConfigEntry<bool> _debugShowGrabSelection;

	private ConfigEntry<bool> _debugLogCameraSettings;

	private ConfigEntry<float> _debugCameraLogInterval;

	private readonly Dictionary<Camera, ClipPlaneState> _originalClipPlanes = new Dictionary<Camera, ClipPlaneState>();

	private readonly Dictionary<Renderer, MotionVectorGenerationMode> _originalRendererMotionVectors = new Dictionary<Renderer, MotionVectorGenerationMode>();

	private readonly Dictionary<GameObject, int> _originalRendererLayers = new Dictionary<GameObject, int>();

	private readonly Dictionary<Renderer, Material[]> _originalRendererMaterials = new Dictionary<Renderer, Material[]>();

	private readonly Dictionary<Renderer, Material[]> _transparentRendererMaterials = new Dictionary<Renderer, Material[]>();

	private readonly Dictionary<Renderer, bool> _originalRendererEnabledStates = new Dictionary<Renderer, bool>();

	private readonly Dictionary<Renderer, ShadowCastingMode> _originalRendererShadowModes = new Dictionary<Renderer, ShadowCastingMode>();

	private readonly Dictionary<Renderer, Renderer> _silhouetteRenderers = new Dictionary<Renderer, Renderer>();

	private readonly Dictionary<Renderer, LineRenderer[]> _outlineRenderers = new Dictionary<Renderer, LineRenderer[]>();

	private readonly Dictionary<Renderer, Renderer> _contourDepthRenderers = new Dictionary<Renderer, Renderer>();

	private readonly Dictionary<Renderer, Renderer> _contourOutlineRenderers = new Dictionary<Renderer, Renderer>();

	private readonly HashSet<Renderer> _activeTransparentRenderers = new HashSet<Renderer>();

	private readonly HashSet<Renderer> _activeObstructionTransparentRenderers = new HashSet<Renderer>();

	private readonly MaterialPropertyBlock _transparencyPropertyBlock = new MaterialPropertyBlock();

	private readonly List<Camera> _cameraBuffer = new List<Camera>(16);

	private bool _thirdPersonActive;

	private float _currentDistance;

	private float _resolvedDistance;

	private float _resolvedDistanceVelocity;

	private float _grabCameraBlend;

	private float _grabCameraBlendVelocity;

	private bool _wasLocalGrabActive;

	private bool _hasGrabAimLockTarget;

	private Vector3 _grabAimLockTarget;

	private float _grabAimLockUntilTime;

	private float _thirdPersonCrouchYOffset;

	private float _thirdPersonCrouchYOffsetVelocity;

	private int _collisionMask;

	private InputAction _toggleAction;

	private InputAction _zoomInAction;

	private InputAction _zoomOutAction;

	private Harmony _harmony;

	private Transform _selectionTransform;

	private int _lastInputTickFrame = -1;

	private int _lastCameraTickFrame = -1;

	private bool _temporarilyFirstPerson;

	private float _forceFirstPersonLocalModelHiddenUntil;

	private float _cameraLockSuppressUntilTime;

	private float _startingOffsetX;

	private float _startingOffsetY;

	private float _runtimeOffsetX;

	private float _runtimeOffsetY;

	private int _lastActionMovementTickFrame = -1;

	private Vector3 _lastActionMoveDirection = Vector3.zero;

	private float _lastActionMoveDirectionTime;

	private bool _loggedModelCompatibility;

	private bool _loggedModelCompatibilityMissing;

	private Type _moddedModelPlayerAvatarType;

	private FieldInfo _forceShowLocalModelField;

	private FieldInfo _currentModelInstanceField;

	private FieldInfo _cachedModelRenderersField;

	private MethodInfo _applyModelToVisualMethod;

	private bool _moddedModelUpdatePatched;

	private PlayerAvatar _cachedModelAvatar;

	private Component _cachedModelComponent;

	private GameObject _cachedModelInstance;

	private Renderer[] _cachedForcedRenderers;

	private bool _localModdedModelVisible;

	private PlayerAvatarVisuals _cachedAvatarVisualsForMotionVectors;

	private Renderer[] _cachedAvatarVisualRenderers;

	private float _nextModelCompatibilityRetryTime;

	private float _nextRendererRefreshTime;

	private float _nextAvatarRendererRefreshTime;

	private float _nextCameraDebugLogTime;

	private float _nextTransparencyDebugLogTime;

	private float _nextCameraSettingsLogTime;

	private float _nextMapOverlayLogTime;

	private GameObject _localMapOverlayClone;

	private Transform _localMapOverlayVisualRoot;

	private MapToolController _localMapOverlaySource;

	private Material _silhouetteMaterial;

	private Material _outlineMaterial;

	private Material _contourDepthMaterial;

	private Material _contourOutlineMaterial;

	private bool _contourPreferSkinnedRenderers;

	private float _contourBodyVolumeThreshold;

	private bool _motionBlurOverrideActive;

	private bool _motionBlurOriginalActive;

	private float _motionBlurOriginalShutterAngle;

	private static FieldInfo _menuCurrentPageField;

	private static bool _rewriteMovementInput;

	private static float _rewriteMovementX;

	private static float _rewriteMovementY;

	private static float _rewriteTurnYaw;

	private static float _lastRewriteTurnYaw;

	private static bool _hasRewriteTurnYaw;

	private static Quaternion _visualFacingRotation = Quaternion.identity;

	private static bool _hasVisualFacingRotation;

	private PlayerAvatar _lastPlayerAvatar;

	private int _lastGameState = -1;

	private FieldInfo _avatarIsDisabledField;

	private FieldInfo _avatarDeadSetField;

	private FieldInfo _postProcessingMotionBlurField;

	private FieldInfo _avatarVisualPositionField;

	private FieldInfo _avatarVisualFollowLerpField;

	private FieldInfo _avatarVisualBodySpringTargetField;

	private FieldInfo _cameraPositionOverrideTimerField;

	private FieldInfo _cameraPositionOverrideTargetField;

	private FieldInfo _cameraCrouchLerpField;

	private FieldInfo _cameraCrawlLerpField;

	private FieldInfo _playerAvatarVisionTargetField;

	private FieldInfo _visionTargetCurrentPositionField;

	private FieldInfo _visionTargetStandPositionField;

	private Transform _debugAnchorPoint;

	private Transform _debugHeadPoint;

	private Transform _debugGrabOriginPoint;

	private Transform _debugGrabCameraHitPoint;

	private Transform _debugGrabTargetPoint;

	private LineRenderer _debugGrabCameraRayLine;

	private LineRenderer _debugGrabCharacterRayLine;

	private LineRenderer _debugGrabRangeLine;

	private Material _debugGrabCameraRayMaterial;

	private Material _debugGrabCharacterRayMaterial;

	private Material _debugGrabRangeMaterial;

	internal static Plugin Instance { get; private set; }

	private void Awake()
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected O, but got Unknown
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Expected O, but got Unknown
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Expected O, but got Unknown
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Expected O, but got Unknown
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Expected O, but got Unknown
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Expected O, but got Unknown
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Expected O, but got Unknown
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Expected O, but got Unknown
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Expected O, but got Unknown
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Expected O, but got Unknown
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Expected O, but got Unknown
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Expected O, but got Unknown
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Expected O, but got Unknown
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Expected O, but got Unknown
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Expected O, but got Unknown
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Expected O, but got Unknown
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Expected O, but got Unknown
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Expected O, but got Unknown
		//IL_03e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f0: Expected O, but got Unknown
		//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Expected O, but got Unknown
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Expected O, but got Unknown
		//IL_042e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Expected O, but got Unknown
		//IL_0488: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Expected O, but got Unknown
		//IL_0492: Unknown result type (might be due to invalid IL or missing references)
		//IL_049c: Expected O, but got Unknown
		//IL_050d: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Expected O, but got Unknown
		//IL_0517: Unknown result type (might be due to invalid IL or missing references)
		//IL_0521: Expected O, but got Unknown
		//IL_0550: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Expected O, but got Unknown
		//IL_055a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0564: Expected O, but got Unknown
		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a2: Expected O, but got Unknown
		//IL_059d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a7: Expected O, but got Unknown
		//IL_05d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e5: Expected O, but got Unknown
		//IL_05e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ea: Expected O, but got Unknown
		//IL_0619: Unknown result type (might be due to invalid IL or missing references)
		//IL_0628: Expected O, but got Unknown
		//IL_0623: Unknown result type (might be due to invalid IL or missing references)
		//IL_062d: Expected O, but got Unknown
		//IL_065c: Unknown result type (might be due to invalid IL or missing references)
		//IL_066b: Expected O, but got Unknown
		//IL_0666: Unknown result type (might be due to invalid IL or missing references)
		//IL_0670: Expected O, but got Unknown
		//IL_0705: Unknown result type (might be due to invalid IL or missing references)
		//IL_0714: Expected O, but got Unknown
		//IL_070f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0719: Expected O, but got Unknown
		//IL_0748: Unknown result type (might be due to invalid IL or missing references)
		//IL_0757: Expected O, but got Unknown
		//IL_0752: Unknown result type (might be due to invalid IL or missing references)
		//IL_075c: Expected O, but got Unknown
		//IL_078b: Unknown result type (might be due to invalid IL or missing references)
		//IL_079a: Expected O, but got Unknown
		//IL_0795: Unknown result type (might be due to invalid IL or missing references)
		//IL_079f: Expected O, but got Unknown
		//IL_07ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_07dd: Expected O, but got Unknown
		//IL_07d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e2: Expected O, but got Unknown
		//IL_0811: Unknown result type (might be due to invalid IL or missing references)
		//IL_0820: Expected O, but got Unknown
		//IL_081b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0825: Expected O, but got Unknown
		//IL_0854: Unknown result type (might be due to invalid IL or missing references)
		//IL_0863: Expected O, but got Unknown
		//IL_085e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0868: Expected O, but got Unknown
		//IL_0897: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a6: Expected O, but got Unknown
		//IL_08a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ab: Expected O, but got Unknown
		//IL_08fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_090a: Expected O, but got Unknown
		//IL_0905: Unknown result type (might be due to invalid IL or missing references)
		//IL_090f: Expected O, but got Unknown
		//IL_093e: Unknown result type (might be due to invalid IL or missing references)
		//IL_094d: Expected O, but got Unknown
		//IL_0948: Unknown result type (might be due to invalid IL or missing references)
		//IL_0952: Expected O, but got Unknown
		//IL_0981: Unknown result type (might be due to invalid IL or missing references)
		//IL_0990: Expected O, but got Unknown
		//IL_098b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0995: Expected O, but got Unknown
		//IL_09c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d3: Expected O, but got Unknown
		//IL_09ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d8: Expected O, but got Unknown
		//IL_0a07: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a16: Expected O, but got Unknown
		//IL_0a11: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a1b: Expected O, but got Unknown
		//IL_0a4a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a59: Expected O, but got Unknown
		//IL_0a54: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5e: Expected O, but got Unknown
		//IL_0a8d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a9c: Expected O, but got Unknown
		//IL_0a97: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa1: Expected O, but got Unknown
		//IL_0b45: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b54: Expected O, but got Unknown
		//IL_0b4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b59: Expected O, but got Unknown
		//IL_0c52: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c80: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cdc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d0a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d2a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d34: Expected O, but got Unknown
		//IL_0d63: Unknown result type (might be due to invalid IL or missing references)
		Instance = this;
		KeepAliveOutsideScene(((Component)this).gameObject);
		_toggleKey = ((BaseUnityPlugin)this).Config.Bind<KeyCode>("General", "ToggleKey", (KeyCode)120, "Key to toggle third-person mode.");
		_offsetX = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "OffsetX", 0.45f, "Always-on third-person right shoulder offset.");
		_offsetY = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "OffsetY", 1.965f, "Vertical offset from the local player origin.");
		_defaultDistance = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "DefaultDistance", 3f, new ConfigDescription("Default distance behind the player.", (AcceptableValueBase)new AcceptableValueRange<float>(0.25f, 25f), Array.Empty<object>()));
		_minDistance = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "MinDistance", 1f, new ConfigDescription("Minimum zoom distance.", (AcceptableValueBase)new AcceptableValueRange<float>(0.1f, 25f), Array.Empty<object>()));
		_maxDistance = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "MaxDistance", 3.33f, new ConfigDescription("Maximum zoom distance.", (AcceptableValueBase)new AcceptableValueRange<float>(0.25f, 50f), Array.Empty<object>()));
		_collisionRadius = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "CollisionRadius", 0.035f, new ConfigDescription("Very small spherecast radius used to keep the camera out of walls while avoiding false pull-in near doors.", (AcceptableValueBase)new AcceptableValueRange<float>(0.005f, 0.25f), Array.Empty<object>()));
		_collisionPadding = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "CollisionPadding", 0.15f, new ConfigDescription("Distance kept between the camera and collision surfaces.", (AcceptableValueBase)new AcceptableValueRange<float>(0f, 2f), Array.Empty<object>()));
		_nearClipPlane = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "NearClipPlane", 0.03f, new ConfigDescription("Near clip plane while third-person is active. Lower values reduce close zoom clipping.", (AcceptableValueBase)new AcceptableValueRange<float>(0.01f, 1f), Array.Empty<object>()));
		_minimumFarClipPlane = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "MinimumFarClipPlane", 1000f, new ConfigDescription("Minimum far clip plane while third-person is active. The mod also preserves any larger original far clip value.", (AcceptableValueBase)new AcceptableValueRange<float>(100f, 100000f), Array.Empty<object>()));
		_cameraCenteredSelection = ((BaseUnityPlugin)this).Config.Bind<bool>("Interaction", "CameraCenteredSelection", true, "Aim selection/grabbing at the object centered by the third-person camera while keeping the origin at the player vision transform.");
		_selectionOriginMode = ((BaseUnityPlugin)this).Config.Bind<string>("Interaction", "SelectionOriginMode", "Camera", new ConfigDescription("Origin used by third-person selection/grab override. Camera projects the third-person crosshair through the character grab range; PlayerVision is a legacy fallback.", (AcceptableValueBase)new AcceptableValueList<string>(new string[2] { "Camera", "PlayerVision" }), Array.Empty<object>()));
		_selectionMaxDistance = ((BaseUnityPlugin)this).Config.Bind<float>("Interaction", "SelectionMaxDistance", 100f, new ConfigDescription("Maximum distance used when converting the third-person camera center ray into a player-origin selection ray.", (AcceptableValueBase)new AcceptableValueRange<float>(10f, 500f), Array.Empty<object>()));
		_zoomInKey = ((BaseUnityPlugin)this).Config.Bind<KeyCode>("Controls", "ZoomInKey", (KeyCode)270, "Key to zoom in.");
		_zoomOutKey = ((BaseUnityPlugin)this).Config.Bind<KeyCode>("Controls", "ZoomOutKey", (KeyCode)269, "Key to zoom out.");
		_resetOffsetsKey = ((BaseUnityPlugin)this).Config.Bind<KeyCode>("Controls", "ResetOffsetsKey", (KeyCode)278, "Key to reset runtime camera offsets to the loaded config values.");
		_scrollSensitivity = ((BaseUnityPlugin)this).Config.Bind<float>("Controls", "ScrollSensitivity", 1f, new ConfigDescription("Mouse wheel zoom sensitivity.", (AcceptableValueBase)new AcceptableValueRange<float>(0f, 10f), Array.Empty<object>()));
		_keyZoomSpeed = ((BaseUnityPlugin)this).Config.Bind<float>("Controls", "KeyZoomSpeed", 5f, new ConfigDescription("Zoom speed when using zoom keys.", (AcceptableValueBase)new AcceptableValueRange<float>(0f, 50f), Array.Empty<object>()));
		_offsetAdjustSpeed = ((BaseUnityPlugin)this).Config.Bind<float>("Controls", "OffsetAdjustSpeed", 1f, new ConfigDescription("Offset adjustment speed when holding arrow keys, in world units per second.", (AcceptableValueBase)new AcceptableValueRange<float>(0.05f, 10f), Array.Empty<object>()));
		_actionMovementEnabled = ((BaseUnityPlugin)this).Config.Bind<bool>("Action Movement", "Enabled", true, "When third-person is active, turn the avatar toward the camera-relative WASD direction.");
		_actionTurnSpeed = ((BaseUnityPlugin)this).Config.Bind<float>("Action Movement", "TurnSpeed", 14f, new ConfigDescription("How quickly the avatar turns toward the camera-relative movement direction.", (AcceptableValueBase)new AcceptableValueRange<float>(1f, 40f), Array.Empty<object>()));
		_actionMovementWhileGrabbing = ((BaseUnityPlugin)this).Config.Bind<bool>("Action Movement", "WhileGrabbing", false, "Keep action movement active while grabbing. Disabled by default so grab aim keeps vanilla behavior.");
		_forceLocalModdedModel = ((BaseUnityPlugin)this).Config.Bind<bool>("Compatibility", "ForceLocalModdedModel", true, "While third-person is active, ask known model-swap mods to show the local custom model.");
		_cameraCloseSmoothTime = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "CloseSmoothTime", 0.35f, new ConfigDescription("Seconds used to smooth camera distance when moving closer due to collision.", (AcceptableValueBase)new AcceptableValueRange<float>(0.01f, 1f), Array.Empty<object>()));
		_cameraFarSmoothTime = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "FarSmoothTime", 0.35f, new ConfigDescription("Seconds used to smooth camera distance when moving farther away after collision clears.", (AcceptableValueBase)new AcceptableValueRange<float>(0.01f, 1f), Array.Empty<object>()));
		_grabCameraDistance = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "GrabCameraDistance", 1.65f, new ConfigDescription("Temporary third-person camera distance while grabbing so heavy objects remain visible.", (AcceptableValueBase)new AcceptableValueRange<float>(0.5f, 4f), Array.Empty<object>()));
		_grabCameraOffsetX = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "GrabCameraOffsetX", 0.45f, new ConfigDescription("Temporary right shoulder camera offset while grabbing.", (AcceptableValueBase)new AcceptableValueRange<float>(0f, 1.5f), Array.Empty<object>()));
		_nearFadeStartZoomRatio = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "NearFadeStartZoomRatio", 0.67f, new ConfigDescription("Start model fade only after zooming in this much of the zoom range (0-1). 0.67 means starts after zooming in about two-thirds.", (AcceptableValueBase)new AcceptableValueRange<float>(0f, 1f), Array.Empty<object>()));
		_nearFadeMinAlpha = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "NearFadeMinAlpha", 0.1f, new ConfigDescription("Minimum model alpha when camera is very close. 0.1 = about 90% transparent.", (AcceptableValueBase)new AcceptableValueRange<float>(0.02f, 1f), Array.Empty<object>()));
		_lockRuntimeCameraTuning = ((BaseUnityPlugin)this).Config.Bind<bool>("Camera", "LockRuntimeTuning", true, "Lock mouse wheel zoom and runtime camera offset keys to the current config values.");
		_localTransparencyMode = ((BaseUnityPlugin)this).Config.Bind<string>("Camera", "LocalTransparencyMode", "Disabled", new ConfigDescription("Local player near-camera visibility mode. Disabled keeps the local player model visible at all third-person camera distances.", (AcceptableValueBase)new AcceptableValueList<string>(new string[9] { "Disabled", "ContourOnly", "BoundsOutlineDebug", "SilhouetteClone", "MaterialAndPropertyBlock", "MaterialAlpha", "PropertyBlockOnly", "RendererDisable", "ShadowsOnly" }), Array.Empty<object>()));
		_silhouetteScale = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "SilhouetteScale", 1.035f, new ConfigDescription("Scale multiplier for the local-only silhouette clone when Camera.LocalTransparencyMode is SilhouetteClone.", (AcceptableValueBase)new AcceptableValueRange<float>(1f, 1.2f), Array.Empty<object>()));
		_outlineWidth = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "OutlineWidth", 0.025f, new ConfigDescription("Thickness for ContourOnly local player near-camera mode. BoundsOutlineDebug also uses this as line width.", (AcceptableValueBase)new AcceptableValueRange<float>(0.002f, 0.12f), Array.Empty<object>()));
		_outlineColorR = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "OutlineColorR", 0.25f, new ConfigDescription("ContourOnly outline red channel.", (AcceptableValueBase)new AcceptableValueRange<float>(0f, 1f), Array.Empty<object>()));
		_outlineColorG = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "OutlineColorG", 0.95f, new ConfigDescription("ContourOnly outline green channel.", (AcceptableValueBase)new AcceptableValueRange<float>(0f, 1f), Array.Empty<object>()));
		_outlineColorB = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "OutlineColorB", 1f, new ConfigDescription("ContourOnly outline blue channel.", (AcceptableValueBase)new AcceptableValueRange<float>(0f, 1f), Array.Empty<object>()));
		_outlineColorA = ((BaseUnityPlugin)this).Config.Bind<float>("Camera", "OutlineColorA", 0.9f, new ConfigDescription("ContourOnly outline alpha channel.", (AcceptableValueBase)new AcceptableValueRange<float>(0f, 1f), Array.Empty<object>()));
		_thirdPersonMapOverlay = ((BaseUnityPlugin)this).Config.Bind<bool>("Map", "ThirdPersonScreenOverlay", true, "Show a local-only first-person-style map model in front of the camera while the map is open in third-person.");
		_mapOverlayX = ((BaseUnityPlugin)this).Config.Bind<float>("Map", "OverlayX", 0f, new ConfigDescription("Local-only third-person map overlay X offset from the camera.", (AcceptableValueBase)new AcceptableValueRange<float>(-2f, 2f), Array.Empty<object>()));
		_mapOverlayY = ((BaseUnityPlugin)this).Config.Bind<float>("Map", "OverlayY", -0.55f, new ConfigDescription("Local-only third-person map overlay Y offset from the camera.", (AcceptableValueBase)new AcceptableValueRange<float>(-2f, 2f), Array.Empty<object>()));
		_mapOverlayZ = ((BaseUnityPlugin)this).Config.Bind<float>("Map", "OverlayZ", 0.73f, new ConfigDescription("Local-only third-person map overlay Z offset from the camera.", (AcceptableValueBase)new AcceptableValueRange<float>(0.1f, 3f), Array.Empty<object>()));
		_mapOverlayScale = ((BaseUnityPlugin)this).Config.Bind<float>("Map", "OverlayScale", 1.55f, new ConfigDescription("Local-only third-person map overlay scale.", (AcceptableValueBase)new AcceptableValueRange<float>(0.05f, 3f), Array.Empty<object>()));
		_mapOverlayPitch = ((BaseUnityPlugin)this).Config.Bind<float>("Map", "OverlayPitch", -90f, new ConfigDescription("Local-only third-person map overlay pitch.", (AcceptableValueBase)new AcceptableValueRange<float>(-90f, 90f), Array.Empty<object>()));
		_mapOverlayYaw = ((BaseUnityPlugin)this).Config.Bind<float>("Map", "OverlayYaw", 0f, new ConfigDescription("Local-only third-person map overlay yaw.", (AcceptableValueBase)new AcceptableValueRange<float>(-90f, 90f), Array.Empty<object>()));
		_mapOverlayRoll = ((BaseUnityPlugin)this).Config.Bind<float>("Map", "OverlayRoll", 0f, new ConfigDescription("Local-only third-person map overlay roll.", (AcceptableValueBase)new AcceptableValueRange<float>(-180f, 180f), Array.Empty<object>()));
		LockAcceptedMapOverlaySettings();
		LockAcceptedCameraCollisionSettings();
		LockAcceptedGrabCameraSettings();
		_debugShowCameraPoints = ((BaseUnityPlugin)this).Config.Bind<bool>("Debug", "ShowCameraPoints", false, "Show debug dots for camera anchor (green) and head center point (red).");
		_debugShowGrabSelection = ((BaseUnityPlugin)this).Config.Bind<bool>("Debug", "ShowGrabSelection", true, "Show third-person grab debug lines and points. Yellow = camera crosshair ray, cyan/green = character grab ray/valid target, red = unreachable camera hit.");
		_debugLogCameraSettings = ((BaseUnityPlugin)this).Config.Bind<bool>("Debug", "LogCameraSettings", true, "Periodically log current camera distance, resolved collision distance, offsets, and aim/camera angles while third-person is active.");
		_debugCameraLogInterval = ((BaseUnityPlugin)this).Config.Bind<float>("Debug", "CameraLogInterval", 1f, new ConfigDescription("Seconds between camera setting logs when Debug.LogCameraSettings is enabled.", (AcceptableValueBase)new AcceptableValueRange<float>(0.2f, 10f), Array.Empty<object>()));
		_currentDistance = ClampDistance(_defaultDistance.Value);
		_resolvedDistance = _currentDistance;
		_resolvedDistanceVelocity = 0f;
		_grabCameraBlend = 0f;
		_grabCameraBlendVelocity = 0f;
		_wasLocalGrabActive = false;
		_hasGrabAimLockTarget = false;
		_startingOffsetX = Mathf.Max(0f, _offsetX?.Value ?? 0.45f);
		_startingOffsetY = _offsetY.Value;
		_runtimeOffsetX = _startingOffsetX;
		_runtimeOffsetY = _startingOffsetY;
		_collisionMask = LayerMask.GetMask(new string[3] { "Default", "Ground", "Wall" });
		_selectionTransform = CreatePersistentTransform("REPO Native Third Person Selection Transform");
		_debugAnchorPoint = CreateDebugPoint("REPO Native Third Person Anchor Debug", new Color(0.2f, 1f, 0.2f, 0.9f), 0.18f);
		_debugHeadPoint = CreateDebugPoint("REPO Native Third Person Head Debug", new Color(1f, 0.2f, 0.2f, 0.9f), 0.18f);
		_debugGrabOriginPoint = CreateDebugPoint("REPO Native Third Person Grab Origin Debug", new Color(0.25f, 0.95f, 1f, 0.95f), 0.16f);
		_debugGrabCameraHitPoint = CreateDebugPoint("REPO Native Third Person Grab Camera Hit Debug", new Color(1f, 0.85f, 0.1f, 0.95f), 0.14f);
		_debugGrabTargetPoint = CreateDebugPoint("REPO Native Third Person Grab Target Debug", new Color(0.2f, 1f, 0.2f, 0.95f), 0.18f);
		CreateInputActions();
		_harmony = new Harmony("com.reponativemods.thirdperson");
		_harmony.PatchAll(typeof(RepoUpdatePatches));
		Logger.LogInfo((object)$"Loaded action-movement build 55 with character-origin held grab override. ToggleKey={_toggleKey.Value}, CollisionRadius={_collisionRadius.Value:0.###}, OffsetX={_runtimeOffsetX:0.###}, GrabOffset={_grabCameraOffsetX.Value:0.###}, DebugPoints={_debugShowCameraPoints.Value}, GrabDebug={_debugShowGrabSelection.Value}, TransparencyMode={_localTransparencyMode.Value}, SelectionOrigin={_selectionOriginMode.Value}, RuntimeTuningLocked={_lockRuntimeCameraTuning.Value}");
	}

	private void LockAcceptedMapOverlaySettings()
	{
		if (_mapOverlayX != null)
		{
			_mapOverlayX.Value = 0f;
		}
		if (_mapOverlayY != null)
		{
			_mapOverlayY.Value = -0.55f;
		}
		if (_mapOverlayZ != null)
		{
			_mapOverlayZ.Value = 0.73f;
		}
		if (_mapOverlayScale != null)
		{
			_mapOverlayScale.Value = 1.55f;
		}
		if (_mapOverlayPitch != null)
		{
			_mapOverlayPitch.Value = -90f;
		}
		if (_mapOverlayYaw != null)
		{
			_mapOverlayYaw.Value = 0f;
		}
		if (_mapOverlayRoll != null)
		{
			_mapOverlayRoll.Value = 0f;
		}
	}

	private void LockAcceptedCameraCollisionSettings()
	{
		if (_collisionRadius != null)
		{
			_collisionRadius.Value = Mathf.Min(_collisionRadius.Value, 0.035f);
		}
	}

	private void LockAcceptedGrabCameraSettings()
	{
		if (_offsetX != null)
		{
			_offsetX.Value = 0.45f;
		}
		if (_grabCameraOffsetX != null)
		{
			_grabCameraOffsetX.Value = 0f;
		}
	}

	private static Transform CreatePersistentTransform(string objectName)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		GameObject val = new GameObject(objectName);
		KeepAliveOutsideScene(val);
		return val.transform;
	}

	private static void KeepAliveOutsideScene(GameObject owner)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		if ((Object)(object)owner != (Object)null)
		{
			owner.transform.SetParent((Transform)null, true);
			((Object)owner).hideFlags = (HideFlags)61;
			Object.DontDestroyOnLoad((Object)owner);
		}
	}

	private static Transform CreateDebugPoint(string objectName, Color color, float scale)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = GameObject.CreatePrimitive((PrimitiveType)0);
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		((Object)val).name = objectName;
		val.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
		int num = LayerMask.NameToLayer("TopLayer");
		if (num >= 0)
		{
			val.layer = num;
		}
		Collider component = val.GetComponent<Collider>();
		if ((Object)(object)component != (Object)null)
		{
			Object.Destroy((Object)component);
		}
		Renderer component2 = val.GetComponent<Renderer>();
		if ((Object)(object)component2 != (Object)null)
		{
			Material material = component2.material;
			if ((Object)(object)material != (Object)null)
			{
				material.color = color;
			}
		}
		KeepAliveOutsideScene(val);
		val.SetActive(false);
		return val.transform;
	}

	private LineRenderer GetOrCreateDebugLine(ref LineRenderer line, ref Material material, string objectName, Color color, float width)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)line != (Object)null)
		{
			return line;
		}
		GameObject val = new GameObject(objectName);
		KeepAliveOutsideScene(val);
		int num = LayerMask.NameToLayer("TopLayer");
		if (num >= 0)
		{
			val.layer = num;
		}
		line = val.AddComponent<LineRenderer>();
		line.positionCount = 2;
		line.useWorldSpace = true;
		line.loop = false;
		((Renderer)line).shadowCastingMode = (ShadowCastingMode)0;
		((Renderer)line).receiveShadows = false;
		line.widthMultiplier = Mathf.Max(0.002f, width);
		material = CreateDebugLineMaterial(objectName + " Material", color);
		if ((Object)(object)material != (Object)null)
		{
			((Renderer)line).material = material;
		}
		line.startColor = color;
		line.endColor = color;
		val.SetActive(false);
		return line;
	}

	private static Material CreateDebugLineMaterial(string name, Color color)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		Shader val = Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		Material val2 = new Material(val);
		((Object)val2).name = name;
		if (val2.HasProperty("_Color"))
		{
			val2.SetColor("_Color", color);
		}
		if (val2.HasProperty("_SrcBlend"))
		{
			val2.SetFloat("_SrcBlend", 5f);
		}
		if (val2.HasProperty("_DstBlend"))
		{
			val2.SetFloat("_DstBlend", 10f);
		}
		if (val2.HasProperty("_ZWrite"))
		{
			val2.SetFloat("_ZWrite", 0f);
		}
		val2.renderQueue = 5000;
		val2.SetOverrideTag("RenderType", "Transparent");
		return val2;
	}

	private void Update()
	{
		TickInput();
		EnforceFirstPersonLocalModelHidden();
	}

	internal void TickInput()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (_lastInputTickFrame != Time.frameCount)
		{
			_lastInputTickFrame = Time.frameCount;
			if ((IsActionPressedThisFrame(_toggleAction) || IsKeyPressedThisFrame(_toggleKey.Value)) && CanToggleThirdPerson())
			{
				SetThirdPersonActive(!_thirdPersonActive, preserveCrosshair: true);
			}
			if (_thirdPersonActive)
			{
				HandleZoomInput();
				HandleOffsetInput();
			}
		}
	}

	internal void TickActionMovement()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		if (_lastActionMovementTickFrame == Time.frameCount)
		{
			return;
		}
		_lastActionMovementTickFrame = Time.frameCount;
		if (!CanAcceptActionMovementInput())
		{
			return;
		}
		PlayerAvatar instance = PlayerAvatar.instance;
		CameraAim instance2 = CameraAim.Instance;
		if ((Object)(object)instance != (Object)null && (Object)(object)instance2 != (Object)null)
		{
			Vector3 val = ReadCameraRelativeMoveDirection(((Component)instance2).transform);
			if (!(val.sqrMagnitude < 0.0001f))
			{
				_lastActionMoveDirection = val;
				_lastActionMoveDirectionTime = Time.time;
				RotateAvatarTowardMovement(instance, val);
			}
		}
	}

	internal void TickActionMovementVisuals(PlayerAvatarVisuals visuals)
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)visuals == (Object)null || (Object)(object)visuals.playerAvatar == (Object)null || (Object)(object)visuals.playerAvatar != (Object)(object)PlayerAvatar.instance)
		{
			return;
		}
		PlayerAvatar instance = PlayerAvatar.instance;
		if ((Object)(object)instance == (Object)null || (Object)(object)visuals != (Object)(object)instance.playerAvatarVisuals || visuals.isMenuAvatar || (Object)(object)visuals.playerAvatarMenu != (Object)null)
		{
			return;
		}
		Quaternion rotation2;
		if (IsLocalGrabActive() && TryGetGameplayAimRotation(out var rotation))
		{
			Quaternion val = (_hasVisualFacingRotation ? _visualFacingRotation : ((Component)visuals).transform.rotation);
			float num = Mathf.Max(1f, (_actionTurnSpeed?.Value ?? 14f) * 1.5f);
			float num2 = 1f - Mathf.Exp(0f - num * Mathf.Max(0f, Time.deltaTime));
			_visualFacingRotation = Quaternion.Slerp(val, rotation, num2);
			_hasVisualFacingRotation = true;
			ApplyVisualFacingRotation(visuals);
		}
		else if ((RepoUpdatePatches.IsActionMovementCameraLockHeld() || IsMapAimLockActive()) && TryGetGameplayAimRotation(out rotation2))
		{
			Quaternion val2 = (_hasVisualFacingRotation ? _visualFacingRotation : ((Component)visuals).transform.rotation);
			float num3 = Mathf.Max(1f, _actionTurnSpeed?.Value ?? 14f);
			float num4 = 1f - Mathf.Exp(0f - num3 * Mathf.Max(0f, Time.deltaTime));
			_visualFacingRotation = Quaternion.Slerp(val2, rotation2, num4);
			_hasVisualFacingRotation = true;
			ApplyVisualFacingRotation(visuals);
		}
		else
		{
			if (!CanAcceptActionMovementInput())
			{
				return;
			}
			CameraAim instance2 = CameraAim.Instance;
			if ((Object)(object)instance2 == (Object)null)
			{
				return;
			}
			Vector3 moveDirection = ReadGameInputCameraRelativeMoveDirection(((Component)instance2).transform);
			if (moveDirection.sqrMagnitude < 0.0001f)
			{
				if (Time.time - _lastActionMoveDirectionTime > 0.2f)
				{
					return;
				}
				moveDirection = _lastActionMoveDirection;
			}
			if (!(moveDirection.sqrMagnitude < 0.0001f))
			{
				RotateTransformToward(((Component)visuals).transform, moveDirection);
			}
		}
	}

	private bool IsMapAimLockActive()
	{
		if (_thirdPersonActive && !_temporarilyFirstPerson && (Object)(object)Map.Instance != (Object)null)
		{
			return Map.Instance.Active;
		}
		return false;
	}

	private bool IsLocalGrabActive()
	{
		if (!_thirdPersonActive || _temporarilyFirstPerson)
		{
			return false;
		}
		if ((Object)(object)PhysGrabber.instance != (Object)null && PhysGrabber.instance.grabbed)
		{
			return true;
		}
		if ((Object)(object)PlayerController.instance != (Object)null)
		{
			return PlayerController.instance.physGrabActive;
		}
		return false;
	}

	internal void BeginActionMovementRewrite(PlayerController controller)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		_rewriteMovementInput = false;
		if ((Object)(object)controller == (Object)null || (Object)(object)controller != (Object)(object)PlayerController.instance || !CanAcceptActionMovementInput())
		{
			return;
		}
		CameraAim instance = CameraAim.Instance;
		if ((Object)(object)instance == (Object)null || RepoUpdatePatches.IsActionMovementCameraLockHeld())
		{
			return;
		}
		float num = SemiFunc.InputMovementX();
		float num2 = SemiFunc.InputMovementY();
		if (!(Mathf.Abs(num) <= 0.001f) || !(Mathf.Abs(num2) <= 0.001f))
		{
			Vector3 val = ReadCameraRelativeMoveDirection(((Component)instance).transform, num, num2);
			if (!(val.sqrMagnitude < 0.0001f))
			{
				_lastActionMoveDirection = val;
				_lastActionMoveDirectionTime = Time.time;
				Quaternion val2 = ExactLookRotation(val, ((Component)controller).transform.rotation);
				Vector2 val3 = new Vector2(num, num2);
				float num3 = Mathf.Clamp01(val3.magnitude);
				_rewriteMovementX = 0f;
				_rewriteMovementY = Mathf.Max(0.01f, num3);
				_rewriteTurnYaw = val2.eulerAngles.y;
				_lastRewriteTurnYaw = _rewriteTurnYaw;
				_hasRewriteTurnYaw = true;
				_rewriteMovementInput = true;
			}
		}
	}

	internal void EndActionMovementRewrite()
	{
		_rewriteMovementInput = false;
	}

	internal void TickActionMovementAfterFixedUpdate(PlayerController controller)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		if (!_rewriteMovementInput || (Object)(object)controller == (Object)null || (Object)(object)controller != (Object)(object)PlayerController.instance || !CanAcceptActionMovementInput() || (Object)(object)CameraAim.Instance == (Object)null)
		{
			return;
		}
		Vector3 lastActionMoveDirection = _lastActionMoveDirection;
		if (!(lastActionMoveDirection.sqrMagnitude < 0.0001f))
		{
			_lastActionMoveDirection = lastActionMoveDirection;
			_lastActionMoveDirectionTime = Time.time;
			PlayerAvatar instance = PlayerAvatar.instance;
			if ((Object)(object)instance != (Object)null && (Object)(object)instance.playerAvatarVisuals != (Object)null)
			{
				RotateVisualsTowardMovement(instance.playerAvatarVisuals, lastActionMoveDirection, Time.fixedDeltaTime);
			}
		}
	}

	internal void TickModdedModelPlayerAvatarPostUpdate(object moddedModelPlayerAvatar)
	{
		if (_thirdPersonActive && !_temporarilyFirstPerson && moddedModelPlayerAvatar != null)
		{
			object? obj = _currentModelInstanceField?.GetValue(moddedModelPlayerAvatar);
			GameObject val = (GameObject)((obj is GameObject) ? obj : null);
			if (!((Object)(object)val == (Object)null))
			{
				StabilizeLocalModdedModel(val);
			}
		}
	}

	private bool CanAcceptActionMovementInput()
	{
		if (!_thirdPersonActive || _temporarilyFirstPerson || _actionMovementEnabled == null || !_actionMovementEnabled.Value)
		{
			return false;
		}
		if (!CanAcceptGameplayCameraInput())
		{
			return false;
		}
		if (_actionMovementWhileGrabbing == null || !_actionMovementWhileGrabbing.Value)
		{
			if ((Object)(object)PhysGrabber.instance != (Object)null && PhysGrabber.instance.grabbed)
			{
				return false;
			}
			if ((Object)(object)PlayerController.instance != (Object)null && PlayerController.instance.physGrabActive)
			{
				return false;
			}
		}
		return true;
	}

	private static bool CanAcceptGameplayCameraInput()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Invalid comparison between Unknown and I4
		if (!((Object)(object)GameDirector.instance != (Object)null) || (int)GameDirector.instance.currentState != 2)
		{
			return false;
		}
		if (!((Object)(object)PlayerController.instance != (Object)null) || !((Object)(object)PlayerAvatar.instance != (Object)null) || !((Object)(object)CameraAim.Instance != (Object)null))
		{
			return false;
		}
		bool flag = (Object)(object)Map.Instance != (Object)null && Map.Instance.Active;
		if (!flag && (int)Cursor.lockState != 1)
		{
			return false;
		}
		if (!flag && (IsAnyMenuPageOpen() || SemiFunc.MenuLevel() || !SemiFunc.NoTextInputsActive()))
		{
			return false;
		}
		return true;
	}

	private static bool CanToggleThirdPerson()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		if (!((Object)(object)GameDirector.instance != (Object)null) || (int)GameDirector.instance.currentState != 2)
		{
			return false;
		}
		if (!((Object)(object)PlayerController.instance != (Object)null) || !((Object)(object)PlayerAvatar.instance != (Object)null) || !((Object)(object)CameraAim.Instance != (Object)null))
		{
			return false;
		}
		if (IsAnyMenuPageOpen() || SemiFunc.MenuLevel() || !SemiFunc.NoTextInputsActive())
		{
			return false;
		}
		return true;
	}

	private static bool IsAnyMenuPageOpen()
	{
		MenuManager instance = MenuManager.instance;
		if (!((Object)(object)instance != (Object)null))
		{
			return false;
		}
		if (_menuCurrentPageField == null)
		{
			_menuCurrentPageField = typeof(MenuManager).GetField("currentMenuPage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		object? obj = _menuCurrentPageField?.GetValue(instance);
		Object val = (Object)((obj is Object) ? obj : null);
		if (val != null)
		{
			return val != (Object)null;
		}
		return false;
	}

	private Vector3 ReadCameraRelativeMoveDirection(Transform cameraTransform)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		float num2 = 0f;
		if (IsKeyHeld((KeyCode)119))
		{
			num2 += 1f;
		}
		if (IsKeyHeld((KeyCode)115))
		{
			num2 -= 1f;
		}
		if (IsKeyHeld((KeyCode)100))
		{
			num += 1f;
		}
		if (IsKeyHeld((KeyCode)97))
		{
			num -= 1f;
		}
		if (Mathf.Abs(num) <= 0.001f && Mathf.Abs(num2) <= 0.001f)
		{
			return Vector3.zero;
		}
		return ReadCameraRelativeMoveDirection(cameraTransform, num, num2);
	}

	private static Vector3 ReadGameInputCameraRelativeMoveDirection(Transform cameraTransform)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		return ReadCameraRelativeMoveDirection(cameraTransform, SemiFunc.InputMovementX(), SemiFunc.InputMovementY());
	}

	private static Vector3 ReadCameraRelativeMoveDirection(Transform cameraTransform, float horizontal, float vertical)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)cameraTransform == (Object)null || (Mathf.Abs(horizontal) <= 0.001f && Mathf.Abs(vertical) <= 0.001f))
		{
			return Vector3.zero;
		}
		Vector3 val = cameraTransform.forward;
		val.y = 0f;
		if (val.sqrMagnitude < 0.0001f)
		{
			val = cameraTransform.rotation * Vector3.forward;
			val.y = 0f;
		}
		val = val.normalized;
		Vector3 val2 = cameraTransform.right;
		val2.y = 0f;
		if (val2.sqrMagnitude < 0.0001f)
		{
			val2 = Vector3.Cross(Vector3.up, val);
		}
		val2 = val2.normalized;
		Vector3 result = val * vertical + val2 * horizontal;
		if (result.sqrMagnitude > 1f)
		{
			result = result.normalized;
		}
		return result;
	}

	private Vector3 ReadActionMovementDirectionFromCamera()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		CameraAim instance = CameraAim.Instance;
		if ((Object)(object)instance == (Object)null)
		{
			return Vector3.zero;
		}
		Vector3 val = ReadCameraRelativeMoveDirection(((Component)instance).transform);
		if (val.sqrMagnitude >= 0.0001f)
		{
			_lastActionMoveDirection = val;
			_lastActionMoveDirectionTime = Time.time;
		}
		return val;
	}

	private void RotateAvatarTowardMovement(PlayerAvatar avatar, Vector3 moveDirection)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		Transform actionMovementRotationTarget = GetActionMovementRotationTarget(avatar);
		if ((Object)(object)actionMovementRotationTarget != (Object)null)
		{
			RotateTransformToward(actionMovementRotationTarget, moveDirection);
		}
	}

	private void RotateTransformToward(Transform target, Vector3 moveDirection)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		RotateTransformToward(target, moveDirection, Time.deltaTime);
	}

	private void RotateTransformToward(Transform target, Vector3 moveDirection, float deltaTime)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)target == (Object)null))
		{
			moveDirection.y = 0f;
			if (!(moveDirection.sqrMagnitude < 0.0001f))
			{
				target.rotation = SmoothedLookRotation(target.rotation, moveDirection, deltaTime);
			}
		}
	}

	private void RotateVisualsTowardMovement(PlayerAvatarVisuals visuals, Vector3 moveDirection, float deltaTime)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)visuals == (Object)null))
		{
			moveDirection.y = 0f;
			if (!(moveDirection.sqrMagnitude < 0.0001f))
			{
				Quaternion currentRotation = (_hasVisualFacingRotation ? _visualFacingRotation : ((Component)visuals).transform.rotation);
				_visualFacingRotation = SmoothedLookRotation(currentRotation, moveDirection, deltaTime);
				_hasVisualFacingRotation = true;
				ApplyVisualFacingRotation(visuals);
			}
		}
	}

	private void ApplyVisualFacingRotation(PlayerAvatarVisuals visuals)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (_hasVisualFacingRotation && !((Object)(object)visuals == (Object)null))
		{
			((Component)visuals).transform.rotation = _visualFacingRotation;
			SetPlayerAvatarVisualsBodySpringTarget(visuals, _visualFacingRotation);
		}
	}

	private void SetPlayerAvatarVisualsBodySpringTarget(PlayerAvatarVisuals visuals, Quaternion rotation)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)visuals == (Object)null))
		{
			if (_avatarVisualBodySpringTargetField == null)
			{
				_avatarVisualBodySpringTargetField = typeof(PlayerAvatarVisuals).GetField("bodySpringTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			_avatarVisualBodySpringTargetField?.SetValue(visuals, rotation);
		}
	}

	private Quaternion SmoothedLookRotation(Quaternion currentRotation, Vector3 moveDirection, float deltaTime)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		moveDirection.y = 0f;
		if (moveDirection.sqrMagnitude < 0.0001f)
		{
			return currentRotation;
		}
		Quaternion val = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
		float num = Mathf.Max(1f, _actionTurnSpeed?.Value ?? 14f);
		float num2 = 1f - Mathf.Exp(0f - num * Mathf.Max(0f, deltaTime));
		return Quaternion.Slerp(currentRotation, val, num2);
	}

	private void StabilizeCachedLocalModdedModel()
	{
		if (_thirdPersonActive && !_temporarilyFirstPerson && !((Object)(object)_cachedModelInstance == (Object)null))
		{
			StabilizeLocalModdedModel(_cachedModelInstance);
		}
	}

	private void StabilizeLocalModdedModel(GameObject model)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)model == (Object)null))
		{
			model.transform.rotation = GetStableLocalModelRotation(model.transform.rotation);
		}
	}

	private void LockLocalAvatarVisualsToRoot()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if (!_thirdPersonActive || _temporarilyFirstPerson)
		{
			return;
		}
		PlayerAvatar instance = PlayerAvatar.instance;
		if (!((Object)(object)instance == (Object)null) && !((Object)(object)instance.playerAvatarVisuals == (Object)null) && !IsAvatarDeadOrDisabled(instance))
		{
			PlayerAvatarVisuals playerAvatarVisuals = instance.playerAvatarVisuals;
			Vector3 position = ((Component)instance).transform.position;
			((Component)playerAvatarVisuals).transform.position = position;
			ApplyVisualFacingRotation(playerAvatarVisuals);
			if (_avatarVisualPositionField == null)
			{
				_avatarVisualPositionField = typeof(PlayerAvatarVisuals).GetField("visualPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (_avatarVisualFollowLerpField == null)
			{
				_avatarVisualFollowLerpField = typeof(PlayerAvatarVisuals).GetField("visualFollowLerp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			_avatarVisualPositionField?.SetValue(playerAvatarVisuals, position);
			_avatarVisualFollowLerpField?.SetValue(playerAvatarVisuals, 1f);
		}
	}

	private Quaternion GetStableLocalModelRotation(Quaternion fallbackRotation)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if (_hasVisualFacingRotation)
		{
			return _visualFacingRotation;
		}
		PlayerController instance = PlayerController.instance;
		if ((Object)(object)instance != (Object)null)
		{
			return ((Component)instance).transform.rotation;
		}
		if (_hasRewriteTurnYaw)
		{
			return Quaternion.Euler(0f, _lastRewriteTurnYaw, 0f);
		}
		if (_lastActionMoveDirection.sqrMagnitude > 0.0001f)
		{
			return ExactLookRotation(_lastActionMoveDirection, fallbackRotation);
		}
		return fallbackRotation;
	}

	private static Quaternion ExactLookRotation(Vector3 moveDirection, Quaternion fallbackRotation)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		moveDirection.y = 0f;
		if (moveDirection.sqrMagnitude < 0.0001f)
		{
			return fallbackRotation;
		}
		return Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
	}

	private Transform GetActionMovementRotationTarget(PlayerAvatar avatar)
	{
		if (!((Object)(object)avatar != (Object)null))
		{
			return null;
		}
		return ((Component)avatar).transform;
	}

	private bool TryGetGameplayAimRotation(out Quaternion rotation)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		rotation = Quaternion.identity;
		if (!_thirdPersonActive || _temporarilyFirstPerson || !CanAcceptGameplayCameraInput())
		{
			return false;
		}
		CameraAim instance = CameraAim.Instance;
		if ((Object)(object)instance == (Object)null)
		{
			return false;
		}
		Vector3 forward = ((Component)instance).transform.forward;
		forward.y = 0f;
		if (forward.sqrMagnitude < 0.0001f)
		{
			return false;
		}
		rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
		return true;
	}

	private bool TryGetGameplayAimYaw(out float yaw)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		yaw = 0f;
		if (!TryGetGameplayAimRotation(out var rotation))
		{
			return false;
		}
		yaw = rotation.eulerAngles.y;
		return true;
	}

	private bool IsCameraLockTemporarilySuppressed()
	{
		return Time.time < _cameraLockSuppressUntilTime;
	}

	private void SuppressCameraLockForSeconds(float duration)
	{
		_cameraLockSuppressUntilTime = Mathf.Max(_cameraLockSuppressUntilTime, Time.time + Mathf.Max(0f, duration));
	}

	private bool TryGetGrabAimRotation(out Quaternion rotation)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		rotation = Quaternion.identity;
		if (!_thirdPersonActive || _temporarilyFirstPerson)
		{
			return false;
		}
		PhysGrabber instance = PhysGrabber.instance;
		if ((Object)(object)instance == (Object)null || !instance.grabbed)
		{
			return false;
		}
		Transform grabbedObjectTransform = instance.grabbedObjectTransform;
		if ((Object)(object)grabbedObjectTransform == (Object)null)
		{
			return false;
		}
		PlayerAvatar instance2 = PlayerAvatar.instance;
		Transform val = instance2?.PlayerVisionTarget?.VisionTransform;
		Vector3 val2 = (((Object)(object)val != (Object)null) ? val.position : (((Object)(object)instance2 != (Object)null) ? ((Component)instance2).transform.position : Vector3.zero));
		Vector3 val3 = grabbedObjectTransform.position - val2;
		val3.y = 0f;
		if (val3.sqrMagnitude < 0.0001f)
		{
			return false;
		}
		rotation = Quaternion.LookRotation(val3.normalized, Vector3.up);
		return true;
	}

	private bool TryGetGrabAimYaw(out float yaw)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		yaw = 0f;
		if (!TryGetGrabAimRotation(out var rotation))
		{
			return false;
		}
		yaw = rotation.eulerAngles.y;
		return true;
	}

	private void LateUpdate()
	{
		TickCamera();
		LockLocalAvatarVisualsToRoot();
		StabilizeCachedLocalModdedModel();
		UpdateThirdPersonMapOverlay();
		UpdateLocalModelTransparency();
		LogCameraSettingsIfNeeded();
	}

	internal void TickCamera()
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		if (_lastCameraTickFrame == Time.frameCount)
		{
			return;
		}
		_lastCameraTickFrame = Time.frameCount;
		HandleLifecycleReset();
		if (!_thirdPersonActive)
		{
			return;
		}
		PlayerAvatar instance = PlayerAvatar.instance;
		CameraAim instance2 = CameraAim.Instance;
		CameraPosition instance3 = CameraPosition.instance;
		if (!((Object)(object)instance != (Object)null) || !((Object)(object)instance2 != (Object)null) || !((Object)(object)instance3 != (Object)null))
		{
			return;
		}
		if (!CanAcceptGameplayCameraInput())
		{
			EnterTemporaryFirstPerson(instance3, instance);
			return;
		}
		ApplyThirdPersonCompatibility(active: true);
		if (ShouldTemporarilyUseFirstPerson())
		{
			EnterTemporaryFirstPerson(instance3, instance);
			return;
		}
		_temporarilyFirstPerson = false;
		Transform transform = ((Component)instance2).transform;
		UpdateGrabAimLockState(instance);
		Vector3 val = CalculateCameraPosition(instance, transform);
		Ray thirdPersonGameplayRay = GetThirdPersonGameplayRay(instance, val, transform);
		UpdateSelectionTransform(instance, thirdPersonGameplayRay);
		instance3.OverridePosition(val, 0.1f);
		((Component)instance3).transform.position = val;
		if ((Object)(object)instance.playerAvatarVisuals != (Object)null)
		{
			instance.playerAvatarVisuals.ShowSelfOverride(0.15f);
		}
		ApplyClipPlanes();
	}

	private void HandleLifecycleReset()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar instance = PlayerAvatar.instance;
		int num = ((!((Object)(object)GameDirector.instance != (Object)null)) ? (-1) : ((int)GameDirector.instance.currentState));
		if (_lastGameState != num)
		{
			_lastGameState = num;
			if (_thirdPersonActive && num != 2)
			{
				ResetThirdPersonState("game state changed");
				return;
			}
		}
		if (_lastPlayerAvatar != instance)
		{
			_lastPlayerAvatar = instance;
			if (_thirdPersonActive)
			{
				ResetThirdPersonState("local player changed");
				return;
			}
		}
		if (_thirdPersonActive && IsAvatarDeadOrDisabled(instance))
		{
			ResetThirdPersonState("local player died or disabled");
		}
	}

	private bool IsAvatarDeadOrDisabled(PlayerAvatar avatar)
	{
		if ((Object)(object)avatar == (Object)null)
		{
			return true;
		}
		Type type = ((object)avatar).GetType();
		if (_avatarIsDisabledField == null)
		{
			_avatarIsDisabledField = type.GetField("isDisabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		if (_avatarDeadSetField == null)
		{
			_avatarDeadSetField = type.GetField("deadSet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		try
		{
			bool flag = default(bool);
			int num;
			if (_avatarIsDisabledField != null)
			{
				object value = _avatarIsDisabledField.GetValue(avatar);
				if (value is bool)
				{
					flag = (bool)value;
					num = 1;
				}
				else
				{
					num = 0;
				}
			}
			else
			{
				num = 0;
			}
			if (((uint)num & (flag ? 1u : 0u)) != 0)
			{
				return true;
			}
			bool flag2 = default(bool);
			int num2;
			if (_avatarDeadSetField != null)
			{
				object value = _avatarDeadSetField.GetValue(avatar);
				if (value is bool)
				{
					flag2 = (bool)value;
					num2 = 1;
				}
				else
				{
					num2 = 0;
				}
			}
			else
			{
				num2 = 0;
			}
			if (((uint)num2 & (flag2 ? 1u : 0u)) != 0)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private void ResetThirdPersonState(string reason)
	{
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		if (_thirdPersonActive)
		{
			_thirdPersonActive = false;
			_temporarilyFirstPerson = false;
			_currentDistance = ClampDistance(_defaultDistance.Value);
			_resolvedDistance = _currentDistance;
			_resolvedDistanceVelocity = 0f;
			_grabCameraBlend = 0f;
			_grabCameraBlendVelocity = 0f;
			_wasLocalGrabActive = false;
			_hasGrabAimLockTarget = false;
			_thirdPersonCrouchYOffset = 0f;
			_thirdPersonCrouchYOffsetVelocity = 0f;
			_forceFirstPersonLocalModelHiddenUntil = Time.time + 1f;
			ApplyThirdPersonCompatibility(active: false);
			CameraPosition instance = CameraPosition.instance;
			if ((Object)(object)instance != (Object)null && (Object)(object)instance.playerTransform != (Object)null)
			{
				ClearCameraOverride(instance);
				instance.OverridePosition(Vector3.zero, 0f);
			}
			PlayerAvatar instance2 = PlayerAvatar.instance;
			if ((Object)(object)instance2 != (Object)null && (Object)(object)instance2.playerAvatarVisuals != (Object)null)
			{
				instance2.playerAvatarVisuals.ShowSelfOverride(0f);
			}
			RestoreClipPlanes();
			RestoreObstructionTransparency();
			RestoreSilhouetteRenderers();
			HideLocalMapOverlay();
			HideDebugPoints();
			_lastActionMoveDirection = Vector3.zero;
			_lastActionMoveDirectionTime = 0f;
			_hasRewriteTurnYaw = false;
			_hasVisualFacingRotation = false;
			SuppressCameraLockForSeconds(0.2f);
			RestoreLocalModelMotionVectors();
			Logger.LogInfo((object)("Third-person camera reset: " + reason + "."));
		}
	}

	private void OnDisable()
	{
		SetThirdPersonActive(active: false);
		HideLocalMapOverlay();
	}

	private void OnDestroy()
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_006a: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		SetThirdPersonActive(active: false);
		HideLocalMapOverlay();
		Harmony harmony = _harmony;
		if (harmony != null)
		{
			harmony.UnpatchSelf();
		}
		_harmony = null;
		DisposeInputActions();
		if ((Object)(object)_selectionTransform != (Object)null)
		{
			Object.Destroy((Object)((Component)_selectionTransform).gameObject);
			_selectionTransform = null;
		}
		if ((Object)Instance == (Object)this)
		{
			Instance = null;
		}
	}

	private void SetThirdPersonActive(bool active, bool preserveCrosshair = false)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		if (_thirdPersonActive == active)
		{
			return;
		}
		Vector3 target = Vector3.zero;
		bool flag = preserveCrosshair && TryGetCurrentCrosshairWorldTarget(out target);
		_thirdPersonActive = active;
		if (active)
		{
			_currentDistance = ClampDistance(_defaultDistance.Value);
			_resolvedDistance = _currentDistance;
			_resolvedDistanceVelocity = 0f;
			_grabCameraBlend = 0f;
			_grabCameraBlendVelocity = 0f;
			_wasLocalGrabActive = false;
			_hasGrabAimLockTarget = false;
			_thirdPersonCrouchYOffset = 0f;
			_thirdPersonCrouchYOffsetVelocity = 0f;
			SuppressCameraLockForSeconds(0.2f);
			ApplyClipPlanes();
			ApplyThirdPersonCompatibility(active: true);
			if (flag)
			{
				AlignAimToCrosshairTarget(target, enteringThirdPerson: true);
			}
			CaptureCurrentAimAsVisualFacing();
			Logger.LogInfo((object)"Third-person camera enabled.");
			return;
		}
		_currentDistance = ClampDistance(_defaultDistance.Value);
		_resolvedDistance = _currentDistance;
		_resolvedDistanceVelocity = 0f;
		_grabCameraBlend = 0f;
		_grabCameraBlendVelocity = 0f;
		_wasLocalGrabActive = false;
		_hasGrabAimLockTarget = false;
		_forceFirstPersonLocalModelHiddenUntil = Time.time + 1f;
		RestoreLocalRendererTransparency();
		ApplyThirdPersonCompatibility(active: false);
		CameraPosition instance = CameraPosition.instance;
		if ((Object)(object)instance != (Object)null && (Object)(object)instance.playerTransform != (Object)null)
		{
			ForceExitThirdPersonCamera(instance);
		}
		PlayerAvatar instance2 = PlayerAvatar.instance;
		if ((Object)(object)instance2 != (Object)null && (Object)(object)instance2.playerAvatarVisuals != (Object)null)
		{
			instance2.playerAvatarVisuals.ShowSelfOverride(0f);
		}
		if (flag)
		{
			AlignAimToCrosshairTarget(target, enteringThirdPerson: false);
		}
		RestoreObstructionTransparency();
		RestoreClipPlanes();
		HideDebugPoints();
		Logger.LogInfo((object)"Third-person camera disabled.");
	}

	private void HideDebugPoints()
	{
		if ((Object)(object)_debugAnchorPoint != (Object)null)
		{
			((Component)_debugAnchorPoint).gameObject.SetActive(false);
		}
		if ((Object)(object)_debugHeadPoint != (Object)null)
		{
			((Component)_debugHeadPoint).gameObject.SetActive(false);
		}
		HideGrabDebugVisuals();
	}

	private static bool ShouldTemporarilyUseFirstPerson()
	{
		return false;
	}

	private void CaptureCurrentAimAsVisualFacing()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		CameraAim instance = CameraAim.Instance;
		Quaternion visualFacingRotation = Quaternion.identity;
		bool flag = false;
		if ((Object)(object)instance != (Object)null)
		{
			Vector3 forward = ((Component)instance).transform.forward;
			forward.y = 0f;
			if (forward.sqrMagnitude > 0.0001f)
			{
				visualFacingRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
				flag = true;
			}
		}
		if (!flag && (Object)(object)PlayerController.instance != (Object)null)
		{
			visualFacingRotation = ((Component)PlayerController.instance).transform.rotation;
			flag = true;
		}
		if (flag)
		{
			_visualFacingRotation = visualFacingRotation;
			_hasVisualFacingRotation = true;
		}
	}

	private bool TryGetCurrentCrosshairWorldTarget(out Vector3 target)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		target = Vector3.zero;
		Camera main = Camera.main;
		if ((Object)(object)main == (Object)null)
		{
			return false;
		}
		Ray val = main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		int mask = LayerMask.GetMask(new string[1] { "Player" });
		int num = GetCameraOcclusionMask() & ~mask;
		RaycastHit val2 = default(RaycastHit);
		if (Physics.Raycast(val.origin, val.direction, out val2, 100f, num, (QueryTriggerInteraction)1))
		{
			target = val2.point;
			return true;
		}
		Vector3 origin = val.origin;
		Vector3 direction = val.direction;
		target = origin + direction.normalized * 20f;
		return true;
	}

	private void AlignAimToCrosshairTarget(Vector3 target, bool enteringThirdPerson)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar instance = PlayerAvatar.instance;
		CameraAim instance2 = CameraAim.Instance;
		if ((Object)(object)instance == (Object)null || (Object)(object)instance2 == (Object)null)
		{
			return;
		}
		Quaternion val = ((Component)instance2).transform.rotation;
		for (int i = 0; i < 3; i++)
		{
			Vector3 val2 = (enteringThirdPerson ? EstimateThirdPersonCameraPosition(instance, val) : GetFirstPersonAimOrigin(instance));
			Vector3 val3 = target - val2;
			if (val3.sqrMagnitude < 0.0001f)
			{
				return;
			}
			val = Quaternion.LookRotation(val3.normalized, Vector3.up);
		}
		instance2.SetPlayerAim(val, true);
		instance2.OverrideNoSmooth(0.08f);
	}

	private Vector3 EstimateThirdPersonCameraPosition(PlayerAvatar avatar, Quaternion aimRotation)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Vector3 headCenterPoint = GetHeadCenterPoint(avatar);
		float num = ClampDistance(_defaultDistance.Value);
		return headCenterPoint - aimRotation * Vector3.forward * num;
	}

	private Vector3 GetFirstPersonAimOrigin(PlayerAvatar avatar)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		CameraPosition instance = CameraPosition.instance;
		if ((Object)(object)instance != (Object)null)
		{
			return ((Component)instance).transform.position;
		}
		return GetHeadCenterPoint(avatar);
	}

	private void EnterTemporaryFirstPerson(CameraPosition cameraPosition, PlayerAvatar avatar)
	{
		if (!_temporarilyFirstPerson)
		{
			_temporarilyFirstPerson = true;
			Logger.LogInfo((object)"Map/tablet active; temporarily using first-person camera.");
		}
		_forceFirstPersonLocalModelHiddenUntil = Time.time + 1f;
		if ((Object)(object)cameraPosition != (Object)null && (Object)(object)cameraPosition.playerTransform != (Object)null)
		{
			ForceExitThirdPersonCamera(cameraPosition);
		}
		RestoreLocalRendererTransparency();
		if ((Object)(object)avatar != (Object)null && (Object)(object)avatar.playerAvatarVisuals != (Object)null)
		{
			avatar.playerAvatarVisuals.ShowSelfOverride(0f);
		}
		HideForcedLocalModel();
		HideDebugPoints();
		RestoreObstructionTransparency();
	}

	private void EnforceFirstPersonLocalModelHidden()
	{
		if ((!_thirdPersonActive || _temporarilyFirstPerson) && (!_thirdPersonActive || !(Time.time > _forceFirstPersonLocalModelHiddenUntil)))
		{
			RestoreLocalRendererTransparency();
			PlayerAvatar instance = PlayerAvatar.instance;
			if ((Object)(object)instance != (Object)null && (Object)(object)instance.playerAvatarVisuals != (Object)null)
			{
				instance.playerAvatarVisuals.ShowSelfOverride(0f);
			}
			HideForcedLocalModel();
		}
	}

	internal void TickFlashlightControllerPostUpdate(FlashlightController controller)
	{
		if (_thirdPersonActive && !_temporarilyFirstPerson && !((Object)(object)controller == (Object)null) && !((Object)(object)controller.PlayerAvatar == (Object)null) && !((Object)(object)controller.PlayerAvatar != (Object)(object)PlayerAvatar.instance) && (Object)(object)controller.halo != (Object)null)
		{
			controller.halo.enabled = false;
		}
	}

	private static void ClearCameraOverride(CameraPosition cameraPosition)
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)cameraPosition == (Object)null || (Object)(object)cameraPosition.playerTransform == (Object)null)
		{
			return;
		}
		if ((Object)(object)Instance != (Object)null)
		{
			if (Instance._cameraPositionOverrideTimerField == null)
			{
				Instance._cameraPositionOverrideTimerField = typeof(CameraPosition).GetField("overridePositionTimer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (Instance._cameraPositionOverrideTargetField == null)
			{
				Instance._cameraPositionOverrideTargetField = typeof(CameraPosition).GetField("overridePositionTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
		}
		Vector3 val = cameraPosition.playerTransform.localPosition + cameraPosition.playerOffset;
		Instance?._cameraPositionOverrideTimerField?.SetValue(cameraPosition, 0f);
		Instance?._cameraPositionOverrideTargetField?.SetValue(cameraPosition, val);
		cameraPosition.PositionOverrideToggled(val, false);
		((Component)cameraPosition).transform.localRotation = Quaternion.identity;
	}

	private void ForceExitThirdPersonCamera(CameraPosition cameraPosition)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)cameraPosition == (Object)null || (Object)(object)cameraPosition.playerTransform == (Object)null)
		{
			return;
		}
		Vector3 val = cameraPosition.playerTransform.localPosition + cameraPosition.playerOffset;
		ClearCameraOverride(cameraPosition);
		cameraPosition.OverridePosition(val, 0f);
		Transform transform = ((Component)cameraPosition).transform;
		transform.localPosition = val;
		if ((Object)(object)transform.parent != (Object)null)
		{
			transform.position = transform.parent.TransformPoint(val);
		}
		else
		{
			transform.position = val;
		}
		transform.localRotation = Quaternion.identity;
		PlayerAvatar instance = PlayerAvatar.instance;
		if ((Object)(object)instance != (Object)null)
		{
			PhysGrabber physGrabber = instance.physGrabber;
			if (physGrabber != null)
			{
				physGrabber.SetThirdPerson(false);
			}
			FlashlightController flashlightController = instance.flashlightController;
			if (flashlightController != null)
			{
				flashlightController.SetThirdPerson(false);
			}
		}
	}

	private void ForceCameraCrouchLocalPosition(CameraCrouchPosition crouchPosition)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)crouchPosition == (Object)null))
		{
			if (_cameraCrouchLerpField == null)
			{
				_cameraCrouchLerpField = typeof(CameraCrouchPosition).GetField("Lerp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (!(_cameraCrouchLerpField == null) && _cameraCrouchLerpField.GetValue(crouchPosition) is float num)
			{
				float num2 = Mathf.Clamp01(num);
				float num3 = crouchPosition.AnimationCurve.Evaluate(num2) * crouchPosition.Position;
				((Component)crouchPosition).transform.localPosition = new Vector3(0f, num3, 0f);
			}
		}
	}

	private void ForceCameraCrawlLocalPosition(CameraCrawlPosition crawlPosition)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)crawlPosition == (Object)null))
		{
			if (_cameraCrawlLerpField == null)
			{
				_cameraCrawlLerpField = typeof(CameraCrawlPosition).GetField("Lerp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (!(_cameraCrawlLerpField == null) && _cameraCrawlLerpField.GetValue(crawlPosition) is float num)
			{
				float num2 = Mathf.Clamp01(num);
				float num3 = crawlPosition.AnimationCurve.Evaluate(num2) * crawlPosition.Position;
				((Component)crawlPosition).transform.localPosition = new Vector3(0f, num3, 0f);
			}
		}
	}

	private void ApplyFirstPersonCrouchLocalOffset(CameraPosition cameraPosition)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)cameraPosition == (Object)null) && !_thirdPersonActive && !_temporarilyFirstPerson && !((Object)(object)cameraPosition.playerTransform == (Object)null))
		{
			float crouchCameraYOffset = GetCrouchCameraYOffset();
			Vector3 localPosition = cameraPosition.playerTransform.localPosition + cameraPosition.playerOffset;
			if (Mathf.Abs(crouchCameraYOffset) <= 0.001f)
			{
				((Component)cameraPosition).transform.localPosition = localPosition;
				return;
			}
			localPosition.y += crouchCameraYOffset;
			((Component)cameraPosition).transform.localPosition = localPosition;
		}
	}

	private float GetCrouchCameraYOffset()
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		PlayerController instance = PlayerController.instance;
		if ((Object)(object)instance == (Object)null)
		{
			return 0f;
		}
		if (!instance.Crouching && !instance.Crawling && !instance.Sliding)
		{
			return 0f;
		}
		float num = 0f;
		if ((Object)(object)CameraCrouchPosition.instance != (Object)null)
		{
			num += ((Component)CameraCrouchPosition.instance).transform.localPosition.y;
		}
		if ((Object)(object)CameraCrawlPosition.instance != (Object)null)
		{
			num += ((Component)CameraCrawlPosition.instance).transform.localPosition.y;
		}
		if (Mathf.Abs(num) <= 0.001f)
		{
			if (instance.Crawling)
			{
				num = -1.05f;
			}
			else if (instance.Crouching || instance.Sliding)
			{
				num = -0.65f;
			}
		}
		return Mathf.Clamp(num, -1.4f, 0.05f);
	}

	private void NormalizeFirstPersonOverrideActive(PlayerLocalCamera localCamera, ref bool overrideActive)
	{
		if (ShouldForceFirstPersonCrouchMode(localCamera))
		{
			overrideActive = false;
		}
	}

	private void NormalizeFirstPersonOverrideTransform(PlayerLocalCamera localCamera, ref Transform transform)
	{
		if (ShouldForceFirstPersonCrouchMode(localCamera))
		{
			transform = ((Component)localCamera).transform;
		}
	}

	private bool ShouldForceFirstPersonCrouchMode(PlayerLocalCamera localCamera)
	{
		if ((Object)(object)localCamera == (Object)null || (Object)(object)localCamera.playerAvatar == (Object)null || localCamera.playerAvatar != PlayerAvatar.instance)
		{
			return false;
		}
		if (_thirdPersonActive || _temporarilyFirstPerson)
		{
			return false;
		}
		if ((Object)(object)Map.Instance != (Object)null && Map.Instance.Active)
		{
			return false;
		}
		return true;
	}

	private void ApplyThirdPersonCompatibility(bool active)
	{
		_localModdedModelVisible = false;
		if (_forceLocalModdedModel == null || !_forceLocalModdedModel.Value)
		{
			return;
		}
		if (!EnsureModdedModelCompatibilityCached())
		{
			if (active && !_loggedModelCompatibilityMissing)
			{
				_loggedModelCompatibilityMissing = true;
				Logger.LogWarning((object)"Could not find ModdedModelPlayerAvatar.ForceShowLocalModel yet; local model compatibility will retry occasionally while third-person is active.");
			}
			return;
		}
		_forceShowLocalModelField?.SetValue(null, active);
		if (active)
		{
			_localModdedModelVisible = ForceVisibleLocalModdedModel(PlayerAvatar.instance);
			return;
		}
		HideForcedLocalModel();
		_cachedModelAvatar = null;
		_cachedModelComponent = null;
		_cachedModelInstance = null;
		_cachedForcedRenderers = null;
		_localModdedModelVisible = false;
	}

	private bool ForceVisibleLocalModdedModel(PlayerAvatar avatar)
	{
		if (_forceLocalModdedModel == null || !_forceLocalModdedModel.Value || !_thirdPersonActive || _temporarilyFirstPerson || !((Object)(object)avatar != (Object)null))
		{
			return false;
		}
		if (!EnsureModdedModelCompatibilityCached())
		{
			return false;
		}
		_forceShowLocalModelField?.SetValue(null, true);
		if (_cachedModelAvatar != avatar || (Object)(object)_cachedModelComponent == (Object)null)
		{
			_cachedModelAvatar = avatar;
			_cachedModelComponent = ((Component)avatar).GetComponent(_moddedModelPlayerAvatarType) ?? ((Component)avatar).GetComponentInChildren(_moddedModelPlayerAvatarType, true);
			_cachedModelInstance = null;
			_cachedForcedRenderers = null;
		}
		Component cachedModelComponent = _cachedModelComponent;
		if (!((Object)(object)cachedModelComponent != (Object)null))
		{
			return false;
		}
		object? obj = _currentModelInstanceField?.GetValue(cachedModelComponent);
		GameObject val = (GameObject)((obj is GameObject) ? obj : null);
		if (!((Object)(object)val != (Object)null) && _applyModelToVisualMethod != null && (Object)(object)avatar.playerAvatarVisuals != (Object)null && Time.time >= _nextModelCompatibilityRetryTime)
		{
			_nextModelCompatibilityRetryTime = Time.time + 1f;
			try
			{
				_applyModelToVisualMethod.Invoke(cachedModelComponent, new object[1] { avatar.playerAvatarVisuals });
				object? obj2 = _currentModelInstanceField?.GetValue(cachedModelComponent);
				val = (GameObject)((obj2 is GameObject) ? obj2 : null);
			}
			catch (Exception ex)
			{
				Logger.LogWarning((object)("[ThirdPersonCompat] ApplyModelToVisual failed: " + ex.Message));
			}
		}
		if (!((Object)(object)val != (Object)null))
		{
			return false;
		}
		val.SetActive(true);
		if (_cachedModelInstance != val)
		{
			_cachedModelInstance = val;
			List<Renderer> list = new List<Renderer>();
			if (_cachedModelRenderersField?.GetValue(cachedModelComponent) is IEnumerable enumerable)
			{
				foreach (object item in enumerable)
				{
					Renderer val2 = (Renderer)((item is Renderer) ? item : null);
					if (val2 != null && (Object)(object)val2 != (Object)null)
					{
						list.Add(val2);
					}
				}
			}
			if (list.Count == 0)
			{
				list.AddRange(val.GetComponentsInChildren<Renderer>(true));
			}
			_cachedForcedRenderers = list.ToArray();
			_nextRendererRefreshTime = 0f;
		}
		if (_cachedForcedRenderers != null && Time.time >= _nextRendererRefreshTime)
		{
			_nextRendererRefreshTime = Time.time + 0.5f;
			Renderer[] cachedForcedRenderers = _cachedForcedRenderers;
			foreach (Renderer val3 in cachedForcedRenderers)
			{
				if ((Object)(object)val3 != (Object)null)
				{
					val3.enabled = true;
				}
			}
			ApplyLocalVisibleModelRendererCompatibility(_cachedForcedRenderers);
		}
		if (!_loggedModelCompatibility)
		{
			_loggedModelCompatibility = true;
			Logger.LogInfo((object)"Forced local ModdedModelPlayerAvatar model visible for third-person camera.");
		}
		return true;
	}

	private void HideForcedLocalModel()
	{
		if (_cachedForcedRenderers != null)
		{
			Renderer[] cachedForcedRenderers = _cachedForcedRenderers;
			foreach (Renderer val in cachedForcedRenderers)
			{
				if ((Object)(object)val != (Object)null)
				{
					val.enabled = false;
				}
			}
		}
		if ((Object)(object)_cachedModelInstance != (Object)null)
		{
			_cachedModelInstance.SetActive(false);
		}
		_forceShowLocalModelField?.SetValue(null, false);
		_localModdedModelVisible = false;
	}

	private void ApplyNoMotionVectorsToAvatarVisuals(PlayerAvatarVisuals visuals)
	{
	}

	private void ApplyLocalVisibleModelRendererCompatibility(IEnumerable<Renderer> renderers)
	{
	}

	private void RestoreLocalModelMotionVectors()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		RestoreLocalRendererTransparency();
		foreach (KeyValuePair<Renderer, MotionVectorGenerationMode> originalRendererMotionVector in _originalRendererMotionVectors)
		{
			if ((Object)(object)originalRendererMotionVector.Key != (Object)null)
			{
				originalRendererMotionVector.Key.motionVectorGenerationMode = originalRendererMotionVector.Value;
			}
		}
		_originalRendererMotionVectors.Clear();
		foreach (KeyValuePair<GameObject, int> originalRendererLayer in _originalRendererLayers)
		{
			if ((Object)(object)originalRendererLayer.Key != (Object)null)
			{
				originalRendererLayer.Key.layer = originalRendererLayer.Value;
			}
		}
		_originalRendererLayers.Clear();
		_cachedAvatarVisualsForMotionVectors = null;
		_cachedAvatarVisualRenderers = null;
		_nextAvatarRendererRefreshTime = 0f;
	}

	private void ApplyThirdPersonMotionBlurOverride(bool active)
	{
		if (!active)
		{
			RestoreThirdPersonMotionBlurOverride();
		}
	}

	private void UpdateThirdPersonMapOverlay()
	{
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		if (!_thirdPersonActive || _temporarilyFirstPerson || _thirdPersonMapOverlay == null || !_thirdPersonMapOverlay.Value)
		{
			HideLocalMapOverlay();
			return;
		}
		MapToolController instance = MapToolController.instance;
		if ((Object)(object)instance == (Object)null || (Object)(object)instance.VisualTransform == (Object)null || (Object)(object)Map.Instance == (Object)null || !Map.Instance.Active)
		{
			HideLocalMapOverlay();
			return;
		}
		Camera main = Camera.main;
		if ((Object)(object)main == (Object)null)
		{
			HideLocalMapOverlay();
			return;
		}
		EnsureLocalMapOverlay(instance);
		if (!((Object)(object)_localMapOverlayClone == (Object)null))
		{
			Transform transform = ((Component)main).transform;
			_localMapOverlayClone.transform.SetParent((Transform)null, true);
			Vector3 val = new Vector3(_mapOverlayX.Value, _mapOverlayY.Value, _mapOverlayZ.Value);
			_localMapOverlayClone.transform.position = transform.TransformPoint(val);
			_localMapOverlayClone.transform.rotation = transform.rotation * Quaternion.Euler(_mapOverlayPitch.Value, _mapOverlayYaw.Value, _mapOverlayRoll.Value);
			_localMapOverlayClone.transform.localScale = Vector3.one * _mapOverlayScale.Value;
			ResetLocalMapOverlayVisualPose(instance);
			if (!_localMapOverlayClone.activeSelf)
			{
				_localMapOverlayClone.SetActive(true);
			}
			LogMapOverlaySettingsIfNeeded();
		}
	}

	private void LogMapOverlaySettingsIfNeeded()
	{
		if (!(Time.time < _nextMapOverlayLogTime))
		{
			_nextMapOverlayLogTime = Time.time + 1f;
			Logger.LogInfo((object)$"[ThirdPersonMapOverlay] x={_mapOverlayX.Value:0.###}, y={_mapOverlayY.Value:0.###}, z={_mapOverlayZ.Value:0.###}, scale={_mapOverlayScale.Value:0.###}, pitch={_mapOverlayPitch.Value:0.###}, yaw={_mapOverlayYaw.Value:0.###}, roll={_mapOverlayRoll.Value:0.###}");
		}
	}

	private void EnsureLocalMapOverlay(MapToolController controller)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_localMapOverlayClone != (Object)null && _localMapOverlaySource == controller)
		{
			return;
		}
		HideLocalMapOverlay();
		if ((Object)(object)controller == (Object)null || (Object)(object)controller.VisualTransform == (Object)null)
		{
			return;
		}
		_localMapOverlaySource = controller;
		_localMapOverlayClone = new GameObject("REPO Native Third Person Local Map Overlay");
		KeepAliveOutsideScene(_localMapOverlayClone);
		GameObject val = Object.Instantiate<GameObject>(((Component)controller.VisualTransform).gameObject);
		((Object)val).name = "REPO Native Third Person Local Map Overlay Visual";
		val.transform.SetParent(_localMapOverlayClone.transform, false);
		val.transform.localPosition = Vector3.zero;
		val.transform.localRotation = Quaternion.identity;
		val.transform.localScale = Vector3.one;
		_localMapOverlayVisualRoot = val.transform;
		MonoBehaviour[] componentsInChildren = _localMapOverlayClone.GetComponentsInChildren<MonoBehaviour>(true);
		foreach (MonoBehaviour val2 in componentsInChildren)
		{
			if ((Object)(object)val2 != (Object)null)
			{
				((Behaviour)val2).enabled = false;
			}
		}
		Collider[] componentsInChildren2 = _localMapOverlayClone.GetComponentsInChildren<Collider>(true);
		foreach (Collider val3 in componentsInChildren2)
		{
			if ((Object)(object)val3 != (Object)null)
			{
				val3.enabled = false;
			}
		}
		Renderer[] componentsInChildren3 = _localMapOverlayClone.GetComponentsInChildren<Renderer>(true);
		foreach (Renderer val4 in componentsInChildren3)
		{
			if ((Object)(object)val4 != (Object)null)
			{
				val4.shadowCastingMode = (ShadowCastingMode)0;
				val4.receiveShadows = false;
			}
		}
		int num = LayerMask.NameToLayer("TopLayer");
		if (num < 0)
		{
			num = LayerMask.NameToLayer("PlayerVisualsLocal");
		}
		if (num >= 0)
		{
			SetLayerRecursive(_localMapOverlayClone, num);
		}
		_localMapOverlayClone.SetActive(false);
		ResetLocalMapOverlayVisualPose(controller);
		Logger.LogInfo((object)"Created local-only third-person map overlay clone.");
	}

	private void HideLocalMapOverlay()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		if ((Object)(object)_localMapOverlayClone != (Object)null)
		{
			Object.Destroy((Object)_localMapOverlayClone);
			_localMapOverlayClone = null;
		}
		_localMapOverlayVisualRoot = null;
		_localMapOverlaySource = null;
	}

	private void ResetLocalMapOverlayVisualPose(MapToolController controller)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)_localMapOverlayVisualRoot == (Object)null) && !((Object)(object)controller == (Object)null))
		{
			_localMapOverlayVisualRoot.localPosition = Vector3.zero;
			_localMapOverlayVisualRoot.localRotation = Quaternion.identity;
			_localMapOverlayVisualRoot.localScale = Vector3.one;
			ResetClonedLocalRotation(controller.HideTransform);
			ResetClonedLocalRotation(controller.displaySpringTransform);
			ResetClonedLocalRotation(controller.mainSpringTransform);
		}
	}

	private void ResetClonedLocalRotation(Transform source)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		Transform val = FindLocalMapOverlayCloneTransform(source);
		if (!((Object)(object)val == (Object)null))
		{
			val.localRotation = Quaternion.identity;
		}
	}

	private Transform FindLocalMapOverlayCloneTransform(Transform source)
	{
		if ((Object)(object)source == (Object)null || (Object)(object)_localMapOverlaySource == (Object)null || (Object)(object)_localMapOverlaySource.VisualTransform == (Object)null || (Object)(object)_localMapOverlayVisualRoot == (Object)null)
		{
			return null;
		}
		string relativeTransformPath = GetRelativeTransformPath(_localMapOverlaySource.VisualTransform, source);
		if (!string.IsNullOrEmpty(relativeTransformPath))
		{
			return _localMapOverlayVisualRoot.Find(relativeTransformPath);
		}
		return _localMapOverlayVisualRoot;
	}

	private static string GetRelativeTransformPath(Transform root, Transform child)
	{
		if ((Object)(object)root == (Object)null || (Object)(object)child == (Object)null)
		{
			return null;
		}
		if ((Object)(object)root == (Object)(object)child)
		{
			return string.Empty;
		}
		Stack<string> stack = new Stack<string>();
		Transform val = child;
		while ((Object)(object)val != (Object)null && (Object)(object)val != (Object)(object)root)
		{
			stack.Push(((Object)((Component)val).gameObject).name);
			val = val.parent;
		}
		if ((Object)(object)val != (Object)(object)root)
		{
			return null;
		}
		return string.Join("/", stack.ToArray());
	}

	private static void SetLayerRecursive(GameObject owner, int layer)
	{
		if ((Object)(object)owner == (Object)null)
		{
			return;
		}
		owner.layer = layer;
		Transform[] componentsInChildren = owner.GetComponentsInChildren<Transform>(true);
		foreach (Transform val in componentsInChildren)
		{
			if ((Object)(object)val != (Object)null)
			{
				((Component)val).gameObject.layer = layer;
			}
		}
	}

	private void RestoreThirdPersonMotionBlurOverride()
	{
		if (_motionBlurOverrideActive)
		{
			MotionBlur postProcessingMotionBlur = GetPostProcessingMotionBlur();
			if ((Object)(object)postProcessingMotionBlur != (Object)null)
			{
				((ParameterOverride<float>)(object)postProcessingMotionBlur.shutterAngle).value = _motionBlurOriginalShutterAngle;
				((PostProcessEffectSettings)postProcessingMotionBlur).active = _motionBlurOriginalActive;
			}
			_motionBlurOverrideActive = false;
		}
	}

	private MotionBlur GetPostProcessingMotionBlur()
	{
		PostProcessing instance = PostProcessing.Instance;
		if ((Object)(object)instance == (Object)null)
		{
			return null;
		}
		if (_postProcessingMotionBlurField == null)
		{
			_postProcessingMotionBlurField = typeof(PostProcessing).GetField("motionBlur", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		object? obj = _postProcessingMotionBlurField?.GetValue(instance);
		return (MotionBlur)((obj is MotionBlur) ? obj : null);
	}

	private bool EnsureModdedModelCompatibilityCached()
	{
		if (_moddedModelPlayerAvatarType != null)
		{
			return true;
		}
		if (Time.time < _nextModelCompatibilityRetryTime)
		{
			return false;
		}
		_nextModelCompatibilityRetryTime = Time.time + 1f;
		_moddedModelPlayerAvatarType = FindLoadedType("ModdedModelPlayerAvatar");
		if (_moddedModelPlayerAvatarType == null || !typeof(Component).IsAssignableFrom(_moddedModelPlayerAvatarType))
		{
			_moddedModelPlayerAvatarType = null;
			return false;
		}
		_forceShowLocalModelField = _moddedModelPlayerAvatarType.GetField("ForceShowLocalModel", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		_currentModelInstanceField = _moddedModelPlayerAvatarType.GetField("currentModelInstance", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		_cachedModelRenderersField = _moddedModelPlayerAvatarType.GetField("_cachedModelRenderers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		_applyModelToVisualMethod = _moddedModelPlayerAvatarType.GetMethod("ApplyModelToVisual", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		TryPatchModdedModelUpdate();
		return _forceShowLocalModelField != null;
	}

	private void TryPatchModdedModelUpdate()
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		if (_moddedModelUpdatePatched || _harmony == null || _moddedModelPlayerAvatarType == null)
		{
			return;
		}
		MethodInfo method = _moddedModelPlayerAvatarType.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		MethodInfo method2 = typeof(RepoUpdatePatches).GetMethod("ModdedModelPlayerAvatarUpdatePostfix", BindingFlags.Static | BindingFlags.NonPublic);
		if (method == null || method2 == null)
		{
			return;
		}
		try
		{
			HarmonyMethod val = new HarmonyMethod(method2);
			val.priority = 0;
			_harmony.Patch((MethodBase)method, (HarmonyMethod)null, val, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			_moddedModelUpdatePatched = true;
			Logger.LogInfo((object)"Patched ModdedModelPlayerAvatar.Update for action-movement facing.");
		}
		catch (Exception ex)
		{
			Logger.LogWarning((object)("[ThirdPersonCompat] Failed to patch ModdedModelPlayerAvatar.Update: " + ex.Message));
		}
	}

	private static bool SetKnownStaticBool(string typeName, string fieldName, bool value)
	{
		Type type = FindLoadedType(typeName);
		if (type == null)
		{
			return false;
		}
		FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (field != null && field.FieldType == typeof(bool))
		{
			field.SetValue(null, value);
			return true;
		}
		return false;
	}

	private static Type FindLoadedType(string typeName)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			Type type = assembly.GetType(typeName);
			if (type != null)
			{
				return type;
			}
			try
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type2 in types)
				{
					if (type2.Name == typeName)
					{
						return type2;
					}
				}
			}
			catch (ReflectionTypeLoadException ex)
			{
				Type[] types = ex.Types;
				foreach (Type type3 in types)
				{
					if (type3 != null && type3.Name == typeName)
					{
						return type3;
					}
				}
			}
		}
		return null;
	}

	private void HandleZoomInput()
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		if (_lockRuntimeCameraTuning != null && _lockRuntimeCameraTuning.Value)
		{
			_currentDistance = ClampDistance(_defaultDistance?.Value ?? 3f);
		}
		else if (CanAcceptZoomInput())
		{
			float currentDistance = _currentDistance;
			float num = ReadScrollWheel();
			if (Mathf.Abs(num) > 0.001f)
			{
				_currentDistance -= num * _scrollSensitivity.Value;
			}
			if (IsActionHeld(_zoomInAction) || IsKeyHeld(_zoomInKey.Value))
			{
				_currentDistance -= _keyZoomSpeed.Value * Time.deltaTime;
			}
			if (IsActionHeld(_zoomOutAction) || IsKeyHeld(_zoomOutKey.Value))
			{
				_currentDistance += _keyZoomSpeed.Value * Time.deltaTime;
			}
			_currentDistance = ClampDistance(_currentDistance);
			if (Mathf.Abs(_currentDistance - currentDistance) > 0.001f)
			{
				LogCameraSettings("Camera distance adjusted");
			}
		}
	}

	private void HandleOffsetInput()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (_lockRuntimeCameraTuning != null && _lockRuntimeCameraTuning.Value)
		{
			_runtimeOffsetX = _startingOffsetX;
			_runtimeOffsetY = _startingOffsetY;
		}
		else
		{
			if (!CanAcceptOffsetInput())
			{
				return;
			}
			if (IsKeyPressedThisFrame(_resetOffsetsKey.Value))
			{
				_runtimeOffsetX = _startingOffsetX;
				_runtimeOffsetY = _startingOffsetY;
				LogCameraSettings("Camera offset reset");
				return;
			}
			float num = _offsetAdjustSpeed.Value * Time.deltaTime;
			if (!(num <= 0f))
			{
				float runtimeOffsetY = _runtimeOffsetY;
				if (IsKeyHeld((KeyCode)273))
				{
					_runtimeOffsetY += num;
				}
				if (IsKeyHeld((KeyCode)274))
				{
					_runtimeOffsetY -= num;
				}
				if (Mathf.Abs(_runtimeOffsetY - runtimeOffsetY) > 0.001f)
				{
					LogCameraSettings("Camera offset adjusted");
				}
			}
		}
	}

	private static bool CanAcceptZoomInput()
	{
		if (!CanAcceptGameplayCameraInput())
		{
			return false;
		}
		if ((Object)(object)PhysGrabber.instance != (Object)null && PhysGrabber.instance.grabbed)
		{
			return false;
		}
		if ((Object)(object)PlayerController.instance != (Object)null && PlayerController.instance.physGrabActive)
		{
			return false;
		}
		return true;
	}

	private static bool CanAcceptOffsetInput()
	{
		return CanAcceptGameplayCameraInput();
	}

	private void LogCameraSettingsIfNeeded()
	{
		if (_thirdPersonActive && _debugLogCameraSettings != null && _debugLogCameraSettings.Value)
		{
			float num = Mathf.Max(0.2f, (_debugCameraLogInterval != null) ? _debugCameraLogInterval.Value : 1f);
			if (!(Time.time < _nextCameraSettingsLogTime))
			{
				_nextCameraSettingsLogTime = Time.time + num;
				LogCameraSettings("Camera snapshot");
			}
		}
	}

	private void LogCameraSettings(string reason)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		Transform val = (((Object)(object)CameraAim.Instance != (Object)null) ? ((Component)CameraAim.Instance).transform : null);
		Camera main = Camera.main;
		Vector3 val2;
		Quaternion rotation;
		if (!((Object)(object)val != (Object)null))
		{
			val2 = Vector3.zero;
		}
		else
		{
			rotation = val.rotation;
			val2 = rotation.eulerAngles;
		}
		Vector3 val3 = val2;
		Vector3 val4;
		if (!((Object)(object)main != (Object)null))
		{
			val4 = Vector3.zero;
		}
		else
		{
			rotation = ((Component)main).transform.rotation;
			val4 = rotation.eulerAngles;
		}
		Vector3 val5 = val4;
		Vector3 val6 = (((Object)(object)main != (Object)null) ? ((Component)main).transform.position : Vector3.zero);
		Logger.LogInfo((object)$"[ThirdPersonCameraSettings] {reason}: distance={_currentDistance:0.###}, resolved={_resolvedDistance:0.###}, offsetX={_runtimeOffsetX:0.###}, offsetY={_runtimeOffsetY:0.###}, min={_minDistance.Value:0.###}, max={_maxDistance.Value:0.###}, closeSmooth={_cameraCloseSmoothTime.Value:0.###}, farSmooth={_cameraFarSmoothTime.Value:0.###}, aimEuler={val3}, cameraEuler={val5}, cameraPos={val6}");
	}

	private Vector3 GetDynamicCameraAnchor(PlayerAvatar avatar, Transform aimTransform)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return GetHeadCenterPoint(avatar);
	}

	private Vector3 GetHeadCenterPoint(PlayerAvatar avatar)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = ((Component)avatar).transform.position;
		float crouchCameraYOffset = GetCrouchCameraYOffset();
		_thirdPersonCrouchYOffset = Mathf.SmoothDamp(_thirdPersonCrouchYOffset, crouchCameraYOffset, ref _thirdPersonCrouchYOffsetVelocity, 0.12f, float.PositiveInfinity, Time.deltaTime);
		float num = position.y + Mathf.Clamp(_runtimeOffsetY + _thirdPersonCrouchYOffset, 0.15f, 2.2f);
		return new Vector3(position.x, num, position.z);
	}

	private Vector3 CalculateCameraPosition(PlayerAvatar avatar, Transform aimTransform)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		Vector3 headCenterPoint = GetHeadCenterPoint(avatar);
		UpdateGrabCameraBlend();
		float effectiveCameraDistance = GetEffectiveCameraDistance();
		float effectiveCameraOffsetX = GetEffectiveCameraOffsetX();
		Vector3 val = headCenterPoint - aimTransform.forward * effectiveCameraDistance + aimTransform.right * effectiveCameraOffsetX - headCenterPoint;
		float magnitude = val.magnitude;
		if (magnitude <= 0.001f)
		{
			_resolvedDistance = 0.05f;
			return headCenterPoint;
		}
		Vector3 val2 = val / magnitude;
		int cameraOcclusionMask = GetCameraOcclusionMask();
		float num = magnitude;
		float num2 = Mathf.Clamp((_collisionRadius != null) ? _collisionRadius.Value : 0.035f, 0.005f, 0.05f);
		RaycastHit[] array = Physics.SphereCastAll(headCenterPoint - val2 * 0.08f, num2, val2, magnitude + 0.08f, cameraOcclusionMask, (QueryTriggerInteraction)1);
		if (array != null && array.Length > 1)
		{
			Array.Sort(array, (RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance));
		}
		if (array != null)
		{
			RaycastHit[] array2 = array;
			for (int num3 = 0; num3 < array2.Length; num3++)
			{
				RaycastHit hit = array2[num3];
				if (!((Object)(object)hit.collider == (Object)null) && !(hit.distance <= 0.001f) && IsCameraOcclusionHitBehindHead(headCenterPoint, val2, hit))
				{
					num = Mathf.Max(0.05f, hit.distance - 0.08f - _collisionPadding.Value);
					break;
				}
			}
		}
		num = Mathf.Min(num, ResolveReverseCameraOcclusionDistance(headCenterPoint, val2, magnitude, num2, cameraOcclusionMask));
		float num4 = Mathf.Clamp((_resolvedDistance > 0f) ? _resolvedDistance : _currentDistance, 0.05f, _maxDistance.Value);
		float num5 = Mathf.Max(_cameraCloseSmoothTime.Value, _cameraFarSmoothTime.Value);
		_resolvedDistance = Mathf.SmoothDamp(num4, num, ref _resolvedDistanceVelocity, Mathf.Max(0.01f, num5), float.PositiveInfinity, Time.deltaTime);
		_resolvedDistance = Mathf.Clamp(_resolvedDistance, 0.05f, _maxDistance.Value);
		Vector3 val3 = headCenterPoint + val2 * _resolvedDistance;
		UpdateLineOfSightTransparency(val3, headCenterPoint, cameraOcclusionMask);
		UpdateDebugPoints(headCenterPoint, headCenterPoint);
		return val3;
	}

	private float GetEffectiveCameraDistance()
	{
		return ClampDistance(_currentDistance);
	}

	private float GetEffectiveCameraOffsetX()
	{
		return Mathf.Max(0f, _runtimeOffsetX);
	}

	private void UpdateGrabAimLockState(PlayerAvatar avatar)
	{
		bool flag = IsLocalGrabActive();
		if (flag && !_wasLocalGrabActive)
		{
			CaptureGrabAimLockTarget(avatar);
		}
		else if (!flag && _wasLocalGrabActive)
		{
			_hasGrabAimLockTarget = false;
		}
		else if (!flag)
		{
			_hasGrabAimLockTarget = false;
		}
		_wasLocalGrabActive = flag;
	}

	private void CaptureGrabAimLockTarget(PlayerAvatar avatar)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (TryGetCurrentPhysGrabPoint(out var target) || TryGetSelectionTarget(avatar, out target))
		{
			_grabAimLockTarget = target;
			_grabAimLockUntilTime = Time.time + 0.05f;
			_hasGrabAimLockTarget = true;
		}
	}

	private bool TryGetCurrentPhysGrabPoint(out Vector3 target)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		target = Vector3.zero;
		PhysGrabber instance = PhysGrabber.instance;
		if ((Object)(object)instance == (Object)null || !instance.grabbed)
		{
			return false;
		}
		if ((Object)(object)instance.physGrabPoint != (Object)null)
		{
			target = instance.physGrabPoint.position;
			return true;
		}
		if ((Object)(object)instance.grabbedObjectTransform != (Object)null)
		{
			target = instance.grabbedObjectTransform.position;
			return true;
		}
		return false;
	}

	private bool TryGetSelectionTarget(PlayerAvatar avatar, out Vector3 target)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		target = Vector3.zero;
		if ((Object)(object)_selectionTransform == (Object)null || (Object)(object)avatar == (Object)null)
		{
			return false;
		}
		Vector3 characterGrabOrigin = GetCharacterGrabOrigin(avatar);
		Vector3 forward = _selectionTransform.forward;
		if (forward.sqrMagnitude < 0.0001f)
		{
			return false;
		}
		target = characterGrabOrigin + forward.normalized * GetCurrentGrabRange();
		return true;
	}

	private bool IsGrabAimLockActive()
	{
		if (_hasGrabAimLockTarget && IsLocalGrabActive())
		{
			return Time.time <= _grabAimLockUntilTime;
		}
		return false;
	}

	private void UpdateGrabCameraBlend()
	{
		float num = (IsLocalGrabActive() ? 1f : 0f);
		_grabCameraBlend = Mathf.SmoothDamp(_grabCameraBlend, num, ref _grabCameraBlendVelocity, 0.08f, float.PositiveInfinity, Time.deltaTime);
		if (Mathf.Abs(_grabCameraBlend - num) < 0.001f)
		{
			_grabCameraBlend = num;
			_grabCameraBlendVelocity = 0f;
		}
	}

	private float GetSmoothedGrabCameraBlend()
	{
		float num = Mathf.Clamp01(_grabCameraBlend);
		return num * num * (3f - 2f * num);
	}

	private int GetCameraOcclusionMask()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		int mask = LayerMask.GetMask(new string[1] { "Player" });
		int num = SemiFunc.LayerMaskGetVisionObstruct();
		int num2 = ((_collisionMask != 0) ? _collisionMask : LayerMask.GetMask(new string[3] { "Default", "Ground", "Wall" }));
		int num3 = num | num2;
		if (num3 == 0)
		{
			return ~mask;
		}
		return num3 & ~mask;
	}

	private float ResolveReverseCameraOcclusionDistance(Vector3 headCenter, Vector3 directionFromHead, float desiredDistance, float castRadius, int mask)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		RaycastHit[] array = Physics.SphereCastAll(headCenter + directionFromHead * desiredDistance, castRadius, -directionFromHead, desiredDistance, mask, (QueryTriggerInteraction)1);
		if (array == null || array.Length == 0)
		{
			return desiredDistance;
		}
		Array.Sort(array, (RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance));
		RaycastHit[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			RaycastHit hit = array2[num];
			if (!((Object)(object)hit.collider == (Object)null) && !(hit.distance <= 0.001f) && IsCameraOcclusionHitBehindHead(headCenter, directionFromHead, hit))
			{
				float num2 = desiredDistance - hit.distance;
				return Mathf.Max(0.05f, num2 - _collisionPadding.Value);
			}
		}
		return desiredDistance;
	}

	private static bool IsCameraOcclusionHitBehindHead(Vector3 headCenter, Vector3 directionFromHeadToCamera, RaycastHit hit)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)hit.collider == (Object)null)
		{
			return false;
		}
		Vector3 val = hit.point;
		if (val.sqrMagnitude < 0.0001f)
		{
			val = hit.collider.ClosestPoint(headCenter);
		}
		if (Vector3.Dot(val - headCenter, directionFromHeadToCamera) <= 0.03f)
		{
			return false;
		}
		return true;
	}

	private void UpdateDebugPoints(Vector3 anchor, Vector3 headCenter)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		bool flag = _debugShowCameraPoints != null && _debugShowCameraPoints.Value && _thirdPersonActive && !_temporarilyFirstPerson;
		if ((Object)(object)_debugAnchorPoint != (Object)null)
		{
			((Component)_debugAnchorPoint).gameObject.SetActive(false);
		}
		if ((Object)(object)_debugHeadPoint != (Object)null)
		{
			((Component)_debugHeadPoint).gameObject.SetActive(flag);
			if (flag)
			{
				_debugHeadPoint.position = headCenter;
			}
		}
		if (flag && Time.time >= _nextCameraDebugLogTime)
		{
			_nextCameraDebugLogTime = Time.time + 0.5f;
			Logger.LogInfo((object)$"[ThirdPersonDebug] head={headCenter} currentDistance={_currentDistance:0.00} resolvedDistance={_resolvedDistance:0.00}");
		}
	}

	private static void AddColliderRenderers(Collider collider, HashSet<Renderer> renderers)
	{
		if ((Object)(object)collider == (Object)null || renderers == null)
		{
			return;
		}
		Renderer[] array = ((Component)collider).GetComponentsInParent<Renderer>();
		if (array == null || array.Length == 0)
		{
			array = ((Component)collider).GetComponentsInChildren<Renderer>();
		}
		if (array == null)
		{
			return;
		}
		Renderer[] array2 = array;
		foreach (Renderer val in array2)
		{
			if ((Object)(object)val != (Object)null)
			{
				renderers.Add(val);
			}
		}
	}

	private void UpdateLineOfSightTransparency(Vector3 cameraPosition, Vector3 headCenter, int mask)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = headCenter - cameraPosition;
		float magnitude = val.magnitude;
		if (magnitude <= 0.05f)
		{
			RestoreObstructionTransparency();
			return;
		}
		Vector3 val2 = val / magnitude;
		RaycastHit[] array = Physics.RaycastAll(cameraPosition, val2, magnitude, mask, (QueryTriggerInteraction)1);
		if (array == null || array.Length == 0)
		{
			RestoreObstructionTransparency();
			return;
		}
		HashSet<Renderer> hashSet = new HashSet<Renderer>();
		RaycastHit[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			RaycastHit val3 = array2[i];
			if (!((Object)(object)val3.collider == (Object)null))
			{
				AddColliderRenderers(val3.collider, hashSet);
			}
		}
		if (hashSet.Count == 0)
		{
			RestoreObstructionTransparency();
		}
		else
		{
			UpdateObstructionTransparency(hashSet);
		}
	}

	private void UpdateObstructionTransparency(HashSet<Renderer> current)
	{
		if (current == null || current.Count == 0)
		{
			RestoreObstructionTransparency();
			return;
		}
		foreach (Renderer item in current)
		{
			if (!((Object)(object)item == (Object)null) && !IsLocalPlayerRenderer(item))
			{
				ApplyTransparencyToRenderer(item, 0.28f);
			}
		}
		List<Renderer> list = new List<Renderer>();
		foreach (Renderer activeObstructionTransparentRenderer in _activeObstructionTransparentRenderers)
		{
			if ((Object)(object)activeObstructionTransparentRenderer != (Object)null && !current.Contains(activeObstructionTransparentRenderer) && !_activeTransparentRenderers.Contains(activeObstructionTransparentRenderer))
			{
				list.Add(activeObstructionTransparentRenderer);
			}
		}
		foreach (Renderer item2 in list)
		{
			RestoreRendererTransparency(item2);
			_activeObstructionTransparentRenderers.Remove(item2);
		}
		foreach (Renderer item3 in current)
		{
			if ((Object)(object)item3 != (Object)null && !IsLocalPlayerRenderer(item3))
			{
				_activeObstructionTransparentRenderers.Add(item3);
			}
		}
	}

	private void RestoreObstructionTransparency()
	{
		if (_activeObstructionTransparentRenderers.Count == 0)
		{
			if (_activeTransparentRenderers.Count == 0)
			{
				CleanupTransparentRendererMaterials();
			}
			return;
		}
		foreach (Renderer activeObstructionTransparentRenderer in _activeObstructionTransparentRenderers)
		{
			if ((Object)(object)activeObstructionTransparentRenderer != (Object)null && !_activeTransparentRenderers.Contains(activeObstructionTransparentRenderer))
			{
				RestoreRendererTransparency(activeObstructionTransparentRenderer);
			}
		}
		_activeObstructionTransparentRenderers.Clear();
		if (_activeTransparentRenderers.Count == 0)
		{
			CleanupTransparentRendererMaterials();
		}
	}

	private float ResolveOcclusionDistance(Vector3 source, Vector3 candidatePosition, int collisionMask)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = candidatePosition - source;
		float magnitude = val.magnitude;
		if (magnitude <= 0.001f)
		{
			return 0.05f;
		}
		Vector3 val2 = val / magnitude;
		RaycastHit val3 = default(RaycastHit);
		if (!Physics.SphereCast(source, Mathf.Max(0.05f, _collisionRadius.Value * 0.9f), val2, out val3, magnitude, collisionMask, (QueryTriggerInteraction)1))
		{
			return magnitude;
		}
		return Mathf.Max(0.05f, val3.distance - _collisionPadding.Value);
	}

	private void UpdateLocalModelTransparency()
	{
		RestoreLocalRendererTransparency();
	}

	private float GetLocalModelTransparencyAlpha()
	{
		float num = Mathf.Max(0.05f, (_minDistance != null) ? _minDistance.Value : 0.1f);
		float num2 = Mathf.Max(num, (_maxDistance != null) ? _maxDistance.Value : (num + 1f));
		float num3 = Mathf.Clamp01((_nearFadeStartZoomRatio != null) ? _nearFadeStartZoomRatio.Value : 0.67f);
		float num4 = Mathf.Lerp(num2, num, num3);
		float num5 = num;
		float num6 = Mathf.InverseLerp(num4, num5, _resolvedDistance);
		num6 = Mathf.Clamp01(num6);
		num6 = num6 * num6 * (3f - 2f * num6);
		float num7 = Mathf.Clamp((_nearFadeMinAlpha != null) ? _nearFadeMinAlpha.Value : 0.1f, 0.02f, 1f);
		return Mathf.Lerp(1f, num7, num6);
	}

	private void CollectLocalAvatarRenderers(List<Renderer> renderers)
	{
		PlayerAvatar instance = PlayerAvatar.instance;
		PlayerAvatarVisuals val = instance?.playerAvatarVisuals;
		Renderer[] componentsInChildren;
		if ((Object)(object)instance != (Object)null)
		{
			componentsInChildren = ((Component)instance).GetComponentsInChildren<Renderer>(true);
			foreach (Renderer val2 in componentsInChildren)
			{
				if ((Object)(object)val2 != (Object)null)
				{
					renderers.Add(val2);
				}
			}
		}
		if ((Object)(object)val == (Object)null)
		{
			_cachedAvatarVisualsForMotionVectors = null;
			_cachedAvatarVisualRenderers = null;
			return;
		}
		if (_cachedAvatarVisualsForMotionVectors != val || _cachedAvatarVisualRenderers == null || Time.time >= _nextAvatarRendererRefreshTime)
		{
			_cachedAvatarVisualsForMotionVectors = val;
			_cachedAvatarVisualRenderers = ((Component)val).GetComponentsInChildren<Renderer>(true);
			_nextAvatarRendererRefreshTime = Time.time + 0.5f;
		}
		if (_cachedAvatarVisualRenderers == null)
		{
			return;
		}
		componentsInChildren = _cachedAvatarVisualRenderers;
		foreach (Renderer val3 in componentsInChildren)
		{
			if ((Object)(object)val3 != (Object)null)
			{
				renderers.Add(val3);
			}
		}
	}

	private void CollectForcedModelRenderers(List<Renderer> renderers)
	{
		if (_cachedForcedRenderers == null)
		{
			return;
		}
		Renderer[] cachedForcedRenderers = _cachedForcedRenderers;
		foreach (Renderer val in cachedForcedRenderers)
		{
			if ((Object)(object)val != (Object)null)
			{
				renderers.Add(val);
			}
		}
	}

	private bool IsLocalPlayerRenderer(Renderer renderer)
	{
		if ((Object)(object)renderer == (Object)null)
		{
			return false;
		}
		Transform transform = ((Component)renderer).transform;
		PlayerAvatar instance = PlayerAvatar.instance;
		if ((Object)(object)instance != (Object)null && transform.IsChildOf(((Component)instance).transform))
		{
			return true;
		}
		PlayerAvatarVisuals val = instance?.playerAvatarVisuals;
		if ((Object)(object)val != (Object)null && transform.IsChildOf(((Component)val).transform))
		{
			return true;
		}
		if ((Object)(object)_cachedModelInstance != (Object)null && transform.IsChildOf(_cachedModelInstance.transform))
		{
			return true;
		}
		if (_cachedForcedRenderers != null && Array.IndexOf(_cachedForcedRenderers, renderer) >= 0)
		{
			return true;
		}
		return false;
	}

	private void ApplyTransparencyToRenderer(Renderer renderer, float alpha, bool useLocalTransparencyMode = false)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		Material[] sharedMaterials = renderer.sharedMaterials;
		if (sharedMaterials == null || sharedMaterials.Length == 0)
		{
			return;
		}
		if (!_originalRendererEnabledStates.ContainsKey(renderer))
		{
			_originalRendererEnabledStates[renderer] = renderer.enabled;
		}
		if (!_originalRendererShadowModes.ContainsKey(renderer))
		{
			_originalRendererShadowModes[renderer] = renderer.shadowCastingMode;
		}
		string text = (useLocalTransparencyMode ? GetLocalTransparencyMode() : "materialandpropertyblock");
		int num;
		switch (text)
		{
		case "contouronly":
			if (UpdateContourOutline(renderer, alpha))
			{
				renderer.enabled = alpha >= 0.999f;
				renderer.shadowCastingMode = (ShadowCastingMode)((alpha < 0.999f) ? 3 : ((int)_originalRendererShadowModes[renderer]));
			}
			else
			{
				renderer.enabled = !_originalRendererEnabledStates.TryGetValue(renderer, out var value) || value;
				renderer.shadowCastingMode = (_originalRendererShadowModes.TryGetValue(renderer, out var value2) ? value2 : renderer.shadowCastingMode);
			}
			return;
		case "boundsoutlinedebug":
		case "outlineonly":
			if (UpdateContourOutline(renderer, alpha))
			{
				renderer.enabled = alpha >= 0.999f;
				renderer.shadowCastingMode = (ShadowCastingMode)3;
			}
			else
			{
				HideBoundsOutline(renderer);
				renderer.enabled = alpha >= 0.999f;
				renderer.shadowCastingMode = (ShadowCastingMode)3;
			}
			return;
		case "silhouetteclone":
			renderer.enabled = alpha >= 0.999f;
			renderer.shadowCastingMode = (ShadowCastingMode)3;
			UpdateSilhouetteClone(renderer, alpha);
			return;
		case "rendererdisable":
			renderer.enabled = alpha >= 0.999f;
			return;
		case "shadowsonly":
			renderer.enabled = true;
			renderer.shadowCastingMode = (ShadowCastingMode)((alpha < 0.999f) ? 3 : ((int)_originalRendererShadowModes[renderer]));
			return;
		default:
			num = ((text == "materialandpropertyblock") ? 1 : 0);
			break;
		case "materialalpha":
			num = 1;
			break;
		}
		bool flag = (byte)num != 0;
		bool flag2 = text == "propertyblockonly" || text == "materialandpropertyblock";
		Material[] array = (flag ? GetTransparentMaterialsForRenderer(renderer, sharedMaterials) : sharedMaterials);
		bool flag3 = false;
		Material[] array2 = array;
		foreach (Material val in array2)
		{
			if (!((Object)(object)val == (Object)null))
			{
				flag3 |= SetMaterialAlpha(val, alpha);
			}
		}
		if (flag2)
		{
			_transparencyPropertyBlock.Clear();
			_transparencyPropertyBlock.SetFloat("_Alpha", alpha);
			_transparencyPropertyBlock.SetFloat("_Opacity", alpha);
			if (array.Length != 0 && (Object)(object)array[0] != (Object)null)
			{
				if (array[0].HasProperty("_Color"))
				{
					Color color = array[0].GetColor("_Color");
					color.a = alpha;
					_transparencyPropertyBlock.SetColor("_Color", color);
				}
				if (array[0].HasProperty("_BaseColor"))
				{
					Color color2 = array[0].GetColor("_BaseColor");
					color2.a = alpha;
					_transparencyPropertyBlock.SetColor("_BaseColor", color2);
				}
			}
			renderer.SetPropertyBlock(_transparencyPropertyBlock);
		}
		else
		{
			renderer.SetPropertyBlock((MaterialPropertyBlock)null);
		}
		if (flag3 || flag2)
		{
			renderer.enabled = true;
			renderer.shadowCastingMode = (_originalRendererShadowModes.TryGetValue(renderer, out var value3) ? value3 : renderer.shadowCastingMode);
			if (flag && !_activeTransparentRenderers.Contains(renderer))
			{
				renderer.sharedMaterials = array;
			}
		}
	}

	private void HideFirstPersonBodyRenderersFromTransparencySet()
	{
		if (_activeTransparentRenderers.Count == 0)
		{
			return;
		}
		foreach (Renderer activeTransparentRenderer in _activeTransparentRenderers)
		{
			if (!((Object)(object)activeTransparentRenderer == (Object)null) && IsLikelyPlayerBodyContourRenderer(activeTransparentRenderer, requireSkinned: false, ignoreVolumeThreshold: true))
			{
				activeTransparentRenderer.enabled = false;
			}
		}
	}

	private string GetLocalTransparencyMode()
	{
		string text = ((_localTransparencyMode != null) ? _localTransparencyMode.Value : "MaterialAndPropertyBlock");
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim().ToLowerInvariant();
		}
		return "materialandpropertyblock";
	}

	private bool IsLocalTransparencyMode(string mode)
	{
		return string.Equals(GetLocalTransparencyMode(), mode, StringComparison.OrdinalIgnoreCase);
	}

	private bool HasUsableSkinnedContourRenderer(IEnumerable<Renderer> renderers)
	{
		if (renderers == null)
		{
			return false;
		}
		foreach (Renderer renderer in renderers)
		{
			if (renderer is SkinnedMeshRenderer && IsLikelyPlayerBodyContourRenderer(renderer, requireSkinned: true))
			{
				return true;
			}
		}
		return false;
	}

	private float GetContourBodyVolumeThreshold(IEnumerable<Renderer> renderers, bool requireSkinned)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		if (renderers == null)
		{
			return 0f;
		}
		foreach (Renderer renderer in renderers)
		{
			if ((!requireSkinned || renderer is SkinnedMeshRenderer) && IsLikelyPlayerBodyContourRenderer(renderer, requireSkinned, ignoreVolumeThreshold: true))
			{
				Bounds bounds = renderer.bounds;
				Vector3 size = bounds.size;
				float num2 = Mathf.Max(0f, size.x) * Mathf.Max(0f, size.y) * Mathf.Max(0f, size.z);
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		if (!(num > 0f))
		{
			return 0f;
		}
		return Mathf.Max(0.005f, num * (requireSkinned ? 0.18f : 0.3f));
	}

	private bool IsLikelyPlayerBodyContourRenderer(Renderer renderer, bool requireSkinned)
	{
		return IsLikelyPlayerBodyContourRenderer(renderer, requireSkinned, ignoreVolumeThreshold: false);
	}

	private bool IsLikelyPlayerBodyContourRenderer(Renderer renderer, bool requireSkinned, bool ignoreVolumeThreshold)
	{
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)renderer == (Object)null)
		{
			return false;
		}
		if (requireSkinned && !(renderer is SkinnedMeshRenderer))
		{
			return false;
		}
		string text = ((object)renderer).GetType().Name.ToLowerInvariant();
		if (renderer is LineRenderer || renderer is TrailRenderer || text.Contains("particle"))
		{
			return false;
		}
		GameObject gameObject = ((Component)renderer).gameObject;
		string text2 = GetTransformPath(gameObject.transform).ToLowerInvariant();
		string[] array = new string[28]
		{
			"outline", "silhouette", "contour", "debug", "map", "tool", "item", "prop", "phys", "grab",
			"flashlight", "light", "weapon", "valuable", "cart", "ui", "text", "particle", "effect", "fx",
			"flash", "beam", "spot", "cone", "orb", "marker", "gizmo", "helper"
		};
		for (int i = 0; i < array.Length; i++)
		{
			if (text2.Contains(array[i]))
			{
				return false;
			}
		}
		PlayerAvatar instance = PlayerAvatar.instance;
		if ((Object)(object)instance == (Object)null)
		{
			return true;
		}
		Vector3 position = ((Component)instance).transform.position;
		Bounds bounds = renderer.bounds;
		Vector3 center = bounds.center;
		Vector3 size = bounds.size;
		float num = Mathf.Max(0f, size.x) * Mathf.Max(0f, size.y) * Mathf.Max(0f, size.z);
		if (!ignoreVolumeThreshold && _contourBodyVolumeThreshold > 0f)
		{
			if (_contourPreferSkinnedRenderers && renderer is SkinnedMeshRenderer && num < _contourBodyVolumeThreshold)
			{
				return false;
			}
			if (!_contourPreferSkinnedRenderers && num < _contourBodyVolumeThreshold)
			{
				return false;
			}
		}
		Vector2 val = new Vector2(center.x - position.x, center.z - position.z);
		if (val.magnitude > 1.25f)
		{
			return false;
		}
		float num2 = center.y - position.y;
		if (num2 < -0.55f || num2 > 2.75f)
		{
			return false;
		}
		return true;
	}

	private string GetTransformPath(Transform transform)
	{
		if ((Object)(object)transform == (Object)null)
		{
			return string.Empty;
		}
		string text = ((Object)transform).name;
		Transform parent = transform.parent;
		while ((Object)(object)parent != (Object)null)
		{
			text = ((Object)parent).name + "/" + text;
			parent = parent.parent;
		}
		return text;
	}

	private Color GetOutlineColor()
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		return new Color((_outlineColorR != null) ? _outlineColorR.Value : 0.25f, (_outlineColorG != null) ? _outlineColorG.Value : 0.95f, (_outlineColorB != null) ? _outlineColorB.Value : 1f, (_outlineColorA != null) ? _outlineColorA.Value : 0.9f);
	}

	private bool UpdateContourOutline(Renderer source, float alpha)
	{
		if ((Object)(object)source == (Object)null)
		{
			return false;
		}
		if (!IsLikelyPlayerBodyContourRenderer(source, _contourPreferSkinnedRenderers))
		{
			SetRendererActive(GetExistingContourRenderer(source, depthOnly: true), active: false);
			SetRendererActive(GetExistingContourRenderer(source, depthOnly: false), active: false);
			return false;
		}
		bool flag = alpha < 0.999f;
		Renderer orCreateContourRenderer = GetOrCreateContourRenderer(source, depthOnly: true);
		Renderer orCreateContourRenderer2 = GetOrCreateContourRenderer(source, depthOnly: false);
		SetRendererActive(orCreateContourRenderer, flag);
		SetRendererActive(orCreateContourRenderer2, flag);
		if (!flag)
		{
			return false;
		}
		SyncContourRenderer(source, orCreateContourRenderer, 1f, GetContourDepthMaterial());
		float scale = 1f + ((_outlineWidth != null) ? _outlineWidth.Value : 0.025f);
		SyncContourRenderer(source, orCreateContourRenderer2, scale, GetContourOutlineMaterial());
		return true;
	}

	private void SetRendererActive(Renderer renderer, bool active)
	{
		if (!((Object)(object)renderer == (Object)null))
		{
			((Component)renderer).gameObject.SetActive(active);
			renderer.enabled = active;
		}
	}

	private Renderer GetOrCreateContourRenderer(Renderer source, bool depthOnly)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		Dictionary<Renderer, Renderer> dictionary = (depthOnly ? _contourDepthRenderers : _contourOutlineRenderers);
		if (dictionary.TryGetValue(source, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		GameObject val = new GameObject(depthOnly ? "REPO Native Third Person Contour Depth" : "REPO Native Third Person Contour Outline");
		KeepAliveOutsideScene(val);
		val.layer = ((Component)source).gameObject.layer;
		Renderer val2 = null;
		SkinnedMeshRenderer val3 = (SkinnedMeshRenderer)(object)((source is SkinnedMeshRenderer) ? source : null);
		if (val3 != null)
		{
			SkinnedMeshRenderer obj = val.AddComponent<SkinnedMeshRenderer>();
			obj.sharedMesh = val3.sharedMesh;
			obj.rootBone = val3.rootBone;
			obj.bones = val3.bones;
			obj.updateWhenOffscreen = true;
			val2 = (Renderer)(object)obj;
		}
		else if (source is MeshRenderer)
		{
			MeshFilter component = ((Component)source).GetComponent<MeshFilter>();
			MeshFilter val4 = val.AddComponent<MeshFilter>();
			if ((Object)(object)component != (Object)null)
			{
				val4.sharedMesh = component.sharedMesh;
			}
			val2 = (Renderer)(object)val.AddComponent<MeshRenderer>();
		}
		if ((Object)(object)val2 == (Object)null)
		{
			Object.Destroy((Object)val);
			return null;
		}
		val2.shadowCastingMode = (ShadowCastingMode)0;
		val2.receiveShadows = false;
		val2.sharedMaterial = (depthOnly ? GetContourDepthMaterial() : GetContourOutlineMaterial());
		val.SetActive(false);
		dictionary[source] = val2;
		return val2;
	}

	private Renderer GetExistingContourRenderer(Renderer source, bool depthOnly)
	{
		if ((Object)(object)source == (Object)null)
		{
			return null;
		}
		if (!(depthOnly ? _contourDepthRenderers : _contourOutlineRenderers).TryGetValue(source, out var value))
		{
			return null;
		}
		return value;
	}

	private void SyncContourRenderer(Renderer source, Renderer clone, float scale, Material material)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)source == (Object)null || (Object)(object)clone == (Object)null)
		{
			return;
		}
		Transform transform = ((Component)source).transform;
		Transform transform2 = ((Component)clone).transform;
		transform2.position = transform.position;
		transform2.rotation = transform.rotation;
		transform2.localScale = transform.lossyScale * scale;
		clone.sharedMaterial = material;
		clone.shadowCastingMode = (ShadowCastingMode)0;
		clone.receiveShadows = false;
		SkinnedMeshRenderer val = (SkinnedMeshRenderer)(object)((source is SkinnedMeshRenderer) ? source : null);
		if (val != null)
		{
			SkinnedMeshRenderer val2 = (SkinnedMeshRenderer)(object)((clone is SkinnedMeshRenderer) ? clone : null);
			if (val2 != null)
			{
				val2.sharedMesh = val.sharedMesh;
				val2.rootBone = val.rootBone;
				val2.bones = val.bones;
				val2.updateWhenOffscreen = true;
				return;
			}
		}
		if (source is MeshRenderer && clone is MeshRenderer)
		{
			MeshFilter component = ((Component)source).GetComponent<MeshFilter>();
			MeshFilter component2 = ((Component)clone).GetComponent<MeshFilter>();
			if ((Object)(object)component != (Object)null && (Object)(object)component2 != (Object)null)
			{
				component2.sharedMesh = component.sharedMesh;
			}
		}
	}

	private Material GetContourDepthMaterial()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_contourDepthMaterial != (Object)null)
		{
			return _contourDepthMaterial;
		}
		Shader val = Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		_contourDepthMaterial = new Material(val);
		((Object)_contourDepthMaterial).name = "REPO Native Third Person Contour Depth Material";
		if (_contourDepthMaterial.HasProperty("_Color"))
		{
			_contourDepthMaterial.SetColor("_Color", new Color(0f, 0f, 0f, 0f));
		}
		if (_contourDepthMaterial.HasProperty("_ColorMask"))
		{
			_contourDepthMaterial.SetFloat("_ColorMask", 0f);
		}
		if (_contourDepthMaterial.HasProperty("_ZWrite"))
		{
			_contourDepthMaterial.SetFloat("_ZWrite", 1f);
		}
		if (_contourDepthMaterial.HasProperty("_ZTest"))
		{
			_contourDepthMaterial.SetFloat("_ZTest", 4f);
		}
		if (_contourDepthMaterial.HasProperty("_Cull"))
		{
			_contourDepthMaterial.SetFloat("_Cull", 0f);
		}
		_contourDepthMaterial.renderQueue = 2990;
		return _contourDepthMaterial;
	}

	private Material GetContourOutlineMaterial()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_contourOutlineMaterial != (Object)null)
		{
			if (_contourOutlineMaterial.HasProperty("_Color"))
			{
				_contourOutlineMaterial.SetColor("_Color", GetOutlineColor());
			}
			return _contourOutlineMaterial;
		}
		Shader val = Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		_contourOutlineMaterial = new Material(val);
		((Object)_contourOutlineMaterial).name = "REPO Native Third Person Contour Outline Material";
		if (_contourOutlineMaterial.HasProperty("_Color"))
		{
			_contourOutlineMaterial.SetColor("_Color", GetOutlineColor());
		}
		if (_contourOutlineMaterial.HasProperty("_SrcBlend"))
		{
			_contourOutlineMaterial.SetFloat("_SrcBlend", 5f);
		}
		if (_contourOutlineMaterial.HasProperty("_DstBlend"))
		{
			_contourOutlineMaterial.SetFloat("_DstBlend", 10f);
		}
		if (_contourOutlineMaterial.HasProperty("_ZWrite"))
		{
			_contourOutlineMaterial.SetFloat("_ZWrite", 0f);
		}
		if (_contourOutlineMaterial.HasProperty("_ZTest"))
		{
			_contourOutlineMaterial.SetFloat("_ZTest", 4f);
		}
		if (_contourOutlineMaterial.HasProperty("_Cull"))
		{
			_contourOutlineMaterial.SetFloat("_Cull", 1f);
		}
		_contourOutlineMaterial.renderQueue = 3000;
		_contourOutlineMaterial.SetOverrideTag("RenderType", "Transparent");
		_contourOutlineMaterial.EnableKeyword("_ALPHABLEND_ON");
		return _contourOutlineMaterial;
	}

	private void HideBoundsOutline(Renderer source)
	{
		if ((Object)(object)source == (Object)null || !_outlineRenderers.TryGetValue(source, out var value) || value == null)
		{
			return;
		}
		LineRenderer[] array = value;
		foreach (LineRenderer val in array)
		{
			if ((Object)(object)val != (Object)null)
			{
				((Component)val).gameObject.SetActive(false);
			}
		}
	}

	private void UpdateBoundsOutline(Renderer source, float alpha)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)source == (Object)null)
		{
			return;
		}
		LineRenderer[] orCreateBoundsOutline = GetOrCreateBoundsOutline(source);
		if (orCreateBoundsOutline == null)
		{
			return;
		}
		bool flag = alpha < 0.999f;
		Bounds bounds = source.bounds;
		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		Vector3[] array = (Vector3[])(object)new Vector3[8]
		{
			new Vector3(min.x, min.y, min.z),
			new Vector3(max.x, min.y, min.z),
			new Vector3(max.x, max.y, min.z),
			new Vector3(min.x, max.y, min.z),
			new Vector3(min.x, min.y, max.z),
			new Vector3(max.x, min.y, max.z),
			new Vector3(max.x, max.y, max.z),
			new Vector3(min.x, max.y, max.z)
		};
		int[,] array2 = new int[12, 2]
		{
			{ 0, 1 },
			{ 1, 2 },
			{ 2, 3 },
			{ 3, 0 },
			{ 4, 5 },
			{ 5, 6 },
			{ 6, 7 },
			{ 7, 4 },
			{ 0, 4 },
			{ 1, 5 },
			{ 2, 6 },
			{ 3, 7 }
		};
		float widthMultiplier = ((_outlineWidth != null) ? _outlineWidth.Value : 0.018f);
		Color outlineColor = GetOutlineColor();
		for (int i = 0; i < orCreateBoundsOutline.Length; i++)
		{
			LineRenderer val = orCreateBoundsOutline[i];
			if (!((Object)(object)val == (Object)null))
			{
				((Component)val).gameObject.SetActive(flag);
				if (flag)
				{
					val.widthMultiplier = widthMultiplier;
					val.startColor = outlineColor;
					val.endColor = outlineColor;
					val.SetPosition(0, array[array2[i, 0]]);
					val.SetPosition(1, array[array2[i, 1]]);
				}
			}
		}
	}

	private LineRenderer[] GetOrCreateBoundsOutline(Renderer source)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		if (_outlineRenderers.TryGetValue(source, out var value) && value != null && value.Length == 12)
		{
			return value;
		}
		LineRenderer[] array = (LineRenderer[])(object)new LineRenderer[12];
		Material outlineMaterial = GetOutlineMaterial();
		for (int i = 0; i < array.Length; i++)
		{
			GameObject val = new GameObject("REPO Native Third Person Outline Line");
			KeepAliveOutsideScene(val);
			val.layer = ((Component)source).gameObject.layer;
			LineRenderer val2 = val.AddComponent<LineRenderer>();
			val2.positionCount = 2;
			val2.useWorldSpace = true;
			val2.loop = false;
			((Renderer)val2).shadowCastingMode = (ShadowCastingMode)0;
			((Renderer)val2).receiveShadows = false;
			((Renderer)val2).material = outlineMaterial;
			val2.startColor = new Color(0.25f, 0.95f, 1f, 0.9f);
			val2.endColor = new Color(0.25f, 0.95f, 1f, 0.9f);
			val.SetActive(false);
			array[i] = val2;
		}
		_outlineRenderers[source] = array;
		return array;
	}

	private Material GetOutlineMaterial()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_outlineMaterial != (Object)null)
		{
			return _outlineMaterial;
		}
		Shader val = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		_outlineMaterial = new Material(val);
		((Object)_outlineMaterial).name = "REPO Native Third Person Outline Material";
		if (_outlineMaterial.HasProperty("_Color"))
		{
			_outlineMaterial.SetColor("_Color", new Color(0.25f, 0.95f, 1f, 0.9f));
		}
		_outlineMaterial.renderQueue = 3100;
		return _outlineMaterial;
	}

	private void UpdateSilhouetteClone(Renderer source, float alpha)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)source == (Object)null)
		{
			return;
		}
		Renderer orCreateSilhouetteRenderer = GetOrCreateSilhouetteRenderer(source);
		if ((Object)(object)orCreateSilhouetteRenderer == (Object)null)
		{
			return;
		}
		bool flag = alpha < 0.999f;
		((Component)orCreateSilhouetteRenderer).gameObject.SetActive(flag);
		if (!flag)
		{
			return;
		}
		Transform transform = ((Component)source).transform;
		Transform transform2 = ((Component)orCreateSilhouetteRenderer).transform;
		transform2.position = transform.position;
		transform2.rotation = transform.rotation;
		transform2.localScale = transform.lossyScale * ((_silhouetteScale != null) ? _silhouetteScale.Value : 1.035f);
		orCreateSilhouetteRenderer.enabled = true;
		orCreateSilhouetteRenderer.shadowCastingMode = (ShadowCastingMode)0;
		orCreateSilhouetteRenderer.receiveShadows = false;
		Material silhouetteMaterial = GetSilhouetteMaterial();
		if ((Object)(object)silhouetteMaterial != (Object)null)
		{
			Color val = default(Color);
			val = new Color(0.25f, 0.95f, 1f, 0.72f);
			if (silhouetteMaterial.HasProperty("_Color"))
			{
				silhouetteMaterial.SetColor("_Color", val);
			}
			orCreateSilhouetteRenderer.sharedMaterial = silhouetteMaterial;
		}
		SkinnedMeshRenderer val2 = (SkinnedMeshRenderer)(object)((source is SkinnedMeshRenderer) ? source : null);
		if (val2 != null)
		{
			SkinnedMeshRenderer val3 = (SkinnedMeshRenderer)(object)((orCreateSilhouetteRenderer is SkinnedMeshRenderer) ? orCreateSilhouetteRenderer : null);
			if (val3 != null)
			{
				val3.sharedMesh = val2.sharedMesh;
				val3.rootBone = val2.rootBone;
				val3.bones = val2.bones;
				val3.updateWhenOffscreen = true;
				return;
			}
		}
		if (source is MeshRenderer && orCreateSilhouetteRenderer is MeshRenderer)
		{
			MeshFilter component = ((Component)source).GetComponent<MeshFilter>();
			MeshFilter component2 = ((Component)orCreateSilhouetteRenderer).GetComponent<MeshFilter>();
			if ((Object)(object)component != (Object)null && (Object)(object)component2 != (Object)null)
			{
				component2.sharedMesh = component.sharedMesh;
			}
		}
	}

	private Renderer GetOrCreateSilhouetteRenderer(Renderer source)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		if (_silhouetteRenderers.TryGetValue(source, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		GameObject val = new GameObject("REPO Native Third Person Silhouette");
		KeepAliveOutsideScene(val);
		val.layer = ((Component)source).gameObject.layer;
		Renderer val2 = null;
		SkinnedMeshRenderer val3 = (SkinnedMeshRenderer)(object)((source is SkinnedMeshRenderer) ? source : null);
		if (val3 != null)
		{
			SkinnedMeshRenderer obj = val.AddComponent<SkinnedMeshRenderer>();
			obj.sharedMesh = val3.sharedMesh;
			obj.rootBone = val3.rootBone;
			obj.bones = val3.bones;
			obj.updateWhenOffscreen = true;
			val2 = (Renderer)(object)obj;
		}
		else if (source is MeshRenderer)
		{
			MeshFilter component = ((Component)source).GetComponent<MeshFilter>();
			MeshFilter val4 = val.AddComponent<MeshFilter>();
			if ((Object)(object)component != (Object)null)
			{
				val4.sharedMesh = component.sharedMesh;
			}
			val2 = (Renderer)(object)val.AddComponent<MeshRenderer>();
		}
		if ((Object)(object)val2 == (Object)null)
		{
			Object.Destroy((Object)val);
			return null;
		}
		val2.sharedMaterial = GetSilhouetteMaterial();
		val2.shadowCastingMode = (ShadowCastingMode)0;
		val2.receiveShadows = false;
		val.SetActive(false);
		_silhouetteRenderers[source] = val2;
		return val2;
	}

	private Material GetSilhouetteMaterial()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_silhouetteMaterial != (Object)null)
		{
			return _silhouetteMaterial;
		}
		Shader val = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		_silhouetteMaterial = new Material(val);
		((Object)_silhouetteMaterial).name = "REPO Native Third Person Silhouette Material";
		if (_silhouetteMaterial.HasProperty("_Color"))
		{
			_silhouetteMaterial.SetColor("_Color", new Color(0.25f, 0.95f, 1f, 0.72f));
		}
		if (_silhouetteMaterial.HasProperty("_Mode"))
		{
			_silhouetteMaterial.SetFloat("_Mode", 3f);
		}
		if (_silhouetteMaterial.HasProperty("_SrcBlend"))
		{
			_silhouetteMaterial.SetFloat("_SrcBlend", 5f);
		}
		if (_silhouetteMaterial.HasProperty("_DstBlend"))
		{
			_silhouetteMaterial.SetFloat("_DstBlend", 10f);
		}
		if (_silhouetteMaterial.HasProperty("_ZWrite"))
		{
			_silhouetteMaterial.SetFloat("_ZWrite", 0f);
		}
		_silhouetteMaterial.renderQueue = 3000;
		_silhouetteMaterial.SetOverrideTag("RenderType", "Transparent");
		_silhouetteMaterial.EnableKeyword("_ALPHABLEND_ON");
		return _silhouetteMaterial;
	}

	private void RestoreSilhouetteRenderers()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		RestoreBoundsOutlines();
		RestoreContourRenderers();
		foreach (KeyValuePair<Renderer, Renderer> silhouetteRenderer in _silhouetteRenderers)
		{
			if ((Object)(object)silhouetteRenderer.Value != (Object)null)
			{
				Object.Destroy((Object)((Component)silhouetteRenderer.Value).gameObject);
			}
		}
		_silhouetteRenderers.Clear();
		if ((Object)(object)_silhouetteMaterial != (Object)null)
		{
			Object.Destroy((Object)_silhouetteMaterial);
			_silhouetteMaterial = null;
		}
	}

	private void RestoreContourRenderers()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Expected O, but got Unknown
		foreach (KeyValuePair<Renderer, Renderer> contourDepthRenderer in _contourDepthRenderers)
		{
			if ((Object)(object)contourDepthRenderer.Value != (Object)null)
			{
				Object.Destroy((Object)((Component)contourDepthRenderer.Value).gameObject);
			}
		}
		foreach (KeyValuePair<Renderer, Renderer> contourOutlineRenderer in _contourOutlineRenderers)
		{
			if ((Object)(object)contourOutlineRenderer.Value != (Object)null)
			{
				Object.Destroy((Object)((Component)contourOutlineRenderer.Value).gameObject);
			}
		}
		_contourDepthRenderers.Clear();
		_contourOutlineRenderers.Clear();
		if ((Object)(object)_contourDepthMaterial != (Object)null)
		{
			Object.Destroy((Object)_contourDepthMaterial);
			_contourDepthMaterial = null;
		}
		if ((Object)(object)_contourOutlineMaterial != (Object)null)
		{
			Object.Destroy((Object)_contourOutlineMaterial);
			_contourOutlineMaterial = null;
		}
	}

	private void RestoreBoundsOutlines()
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected O, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		foreach (KeyValuePair<Renderer, LineRenderer[]> outlineRenderer in _outlineRenderers)
		{
			LineRenderer[] value = outlineRenderer.Value;
			if (value == null)
			{
				continue;
			}
			LineRenderer[] array = value;
			foreach (LineRenderer val in array)
			{
				if ((Object)(object)val != (Object)null)
				{
					Object.Destroy((Object)((Component)val).gameObject);
				}
			}
		}
		_outlineRenderers.Clear();
		if ((Object)(object)_outlineMaterial != (Object)null)
		{
			Object.Destroy((Object)_outlineMaterial);
			_outlineMaterial = null;
		}
	}

	private void RestoreLocalRendererTransparency(bool restoreRendererEnabled = true)
	{
		RestoreSilhouetteRenderers();
		if (_activeTransparentRenderers.Count == 0)
		{
			if (_activeObstructionTransparentRenderers.Count == 0)
			{
				CleanupTransparentRendererMaterials();
			}
			return;
		}
		foreach (Renderer activeTransparentRenderer in _activeTransparentRenderers)
		{
			if ((Object)(object)activeTransparentRenderer != (Object)null)
			{
				RestoreRendererTransparency(activeTransparentRenderer, restoreRendererEnabled);
			}
		}
		if (restoreRendererEnabled)
		{
			_activeTransparentRenderers.Clear();
			if (_activeObstructionTransparentRenderers.Count == 0)
			{
				CleanupTransparentRendererMaterials();
			}
		}
	}

	private void CleanupTransparentRendererMaterials()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		foreach (KeyValuePair<Renderer, Material[]> transparentRendererMaterial in _transparentRendererMaterials)
		{
			Material[] value = transparentRendererMaterial.Value;
			if (value == null)
			{
				continue;
			}
			Material[] array = value;
			foreach (Material val in array)
			{
				if ((Object)(object)val != (Object)null)
				{
					Object.Destroy((Object)val);
				}
			}
		}
		_transparentRendererMaterials.Clear();
		_originalRendererMaterials.Clear();
		_originalRendererEnabledStates.Clear();
		_originalRendererShadowModes.Clear();
	}

	private Material[] GetTransparentMaterialsForRenderer(Renderer renderer, Material[] sharedMaterials)
	{
		if (_transparentRendererMaterials.TryGetValue(renderer, out var value) && value != null && value.Length == sharedMaterials.Length)
		{
			return value;
		}
		_originalRendererMaterials[renderer] = sharedMaterials;
		Material[] array = (Material[])(object)new Material[sharedMaterials.Length];
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			Material val = sharedMaterials[i];
			if (!((Object)(object)val == (Object)null))
			{
				Material val2 = CreateTransparentMaterialCopy(val);
				ConfigureMaterialForTransparency(val2);
				array[i] = val2;
			}
		}
		_transparentRendererMaterials[renderer] = array;
		return array;
	}

	private Material CreateTransparentMaterialCopy(Material source)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		return new Material(source);
	}

	private void ConfigureMaterialForTransparency(Material material)
	{
		if (!((Object)(object)material == (Object)null))
		{
			if (material.HasProperty("_Surface"))
			{
				material.SetFloat("_Surface", 1f);
			}
			if (material.HasProperty("_Blend"))
			{
				material.SetFloat("_Blend", 0f);
			}
			if (material.HasProperty("_SrcBlend"))
			{
				material.SetFloat("_SrcBlend", 5f);
			}
			if (material.HasProperty("_DstBlend"))
			{
				material.SetFloat("_DstBlend", 10f);
			}
			if (material.HasProperty("_ZWrite"))
			{
				material.SetFloat("_ZWrite", 0f);
			}
			if (material.HasProperty("_Mode"))
			{
				material.SetFloat("_Mode", 3f);
			}
			if (material.HasProperty("_UseBaseColorAlpha"))
			{
				material.SetFloat("_UseBaseColorAlpha", 1f);
			}
			if (material.HasProperty("_AlphaClip"))
			{
				material.SetFloat("_AlphaClip", 0f);
			}
			material.DisableKeyword("_ALPHATEST_ON");
			material.EnableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.EnableKeyword("_USEBASECOLORALPHA_ON");
			material.renderQueue = 3000;
			material.SetOverrideTag("RenderType", "Transparent");
		}
	}

	private static bool SetMaterialAlpha(Material material, float alpha)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		bool result = false;
		if (material.HasProperty("_Color"))
		{
			Color color = material.GetColor("_Color");
			color.a = alpha;
			material.SetColor("_Color", color);
			result = true;
		}
		if (material.HasProperty("_BaseColor"))
		{
			Color color2 = material.GetColor("_BaseColor");
			color2.a = alpha;
			material.SetColor("_BaseColor", color2);
			result = true;
		}
		if (material.HasProperty("_MainColor"))
		{
			Color color3 = material.GetColor("_MainColor");
			color3.a = alpha;
			material.SetColor("_MainColor", color3);
			result = true;
		}
		if (material.HasProperty("_TintColor"))
		{
			Color color4 = material.GetColor("_TintColor");
			color4.a = alpha;
			material.SetColor("_TintColor", color4);
			result = true;
		}
		if (material.HasProperty("_Alpha"))
		{
			material.SetFloat("_Alpha", alpha);
			result = true;
		}
		if (material.HasProperty("_Opacity"))
		{
			material.SetFloat("_Opacity", alpha);
			result = true;
		}
		return result;
	}

	private bool TryGetStableLocalPlayerBounds(out Bounds bounds)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		bounds = default(Bounds);
		List<Renderer> list = new List<Renderer>(32);
		CollectLocalAvatarRenderers(list);
		CollectForcedModelRenderers(list);
		bool flag = false;
		foreach (Renderer item in list)
		{
			if (!((Object)(object)item == (Object)null) && item.enabled)
			{
				if (!flag)
				{
					bounds = item.bounds;
					flag = true;
				}
				else
				{
					bounds.Encapsulate(item.bounds);
				}
			}
		}
		return flag;
	}

	private void RestoreRendererTransparency(Renderer renderer)
	{
		RestoreRendererTransparency(renderer, restoreRendererEnabled: true);
	}

	private void RestoreRendererTransparency(Renderer renderer, bool restoreRendererEnabled)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)renderer == (Object)null))
		{
			renderer.SetPropertyBlock((MaterialPropertyBlock)null);
			if (_originalRendererMaterials.TryGetValue(renderer, out var value) && value != null)
			{
				renderer.sharedMaterials = value;
			}
			if (restoreRendererEnabled && _originalRendererEnabledStates.TryGetValue(renderer, out var value2))
			{
				renderer.enabled = value2;
			}
			if (_originalRendererShadowModes.TryGetValue(renderer, out var value3))
			{
				renderer.shadowCastingMode = value3;
			}
		}
	}

	private static Ray GetThirdPersonCenterRay(Vector3 origin, Quaternion fallbackRotation)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		Camera main = Camera.main;
		if ((Object)(object)main != (Object)null)
		{
			Ray val = main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
			return new Ray(origin, val.direction);
		}
		return new Ray(origin, fallbackRotation * Vector3.forward);
	}

	private Ray GetThirdPersonGameplayRay(PlayerAvatar avatar, Vector3 visualCameraPosition, Transform aimTransform)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		return GetThirdPersonCenterRay(visualCameraPosition, ((Object)(object)aimTransform != (Object)null) ? aimTransform.rotation : Quaternion.identity);
	}

	private void UpdateSelectionTransform(PlayerAvatar avatar, Ray cameraCenterRay)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)_selectionTransform != (Object)null) || !((Object)(object)avatar != (Object)null))
		{
			return;
		}
		Transform val = avatar.PlayerVisionTarget?.VisionTransform;
		Vector3 val2 = ((_selectionOriginMode == null || !string.Equals(_selectionOriginMode.Value, "PlayerVision", StringComparison.OrdinalIgnoreCase)) ? GetCharacterGrabOrigin(avatar) : (((Object)(object)val != (Object)null) ? val.position : GetCharacterGrabOrigin(avatar)));
		Vector3 direction = cameraCenterRay.direction;
		Vector3 normalized = direction.normalized;
		float currentGrabRange = GetCurrentGrabRange();
		Vector3 val3 = ResolveCharacterReachCrosshairTarget(val2, cameraCenterRay, currentGrabRange);
		int mask = LayerMask.GetMask(new string[1] { "Player" });
		int num = SemiFunc.LayerMaskGetVisionObstruct() - mask;
		RaycastHit val4 = default(RaycastHit);
		bool hasCameraHit = false;
		bool hitIsReachable = false;
		Vector3 cameraHitPoint = cameraCenterRay.origin + normalized * Mathf.Min(_selectionMaxDistance.Value, currentGrabRange + _resolvedDistance + 2f);
		if (IsGrabAimLockActive())
		{
			val3 = _grabAimLockTarget;
			cameraHitPoint = _grabAimLockTarget;
			hasCameraHit = true;
			hitIsReachable = Vector3.Distance(val2, val3) <= currentGrabRange + 0.05f;
		}
		else if (Physics.Raycast(cameraCenterRay.origin, normalized, out val4, _selectionMaxDistance.Value, num, (QueryTriggerInteraction)1))
		{
			hasCameraHit = true;
			cameraHitPoint = val4.point;
			if (Vector3.Distance(val2, val4.point) <= currentGrabRange + 0.05f)
			{
				hitIsReachable = true;
				val3 = val4.point;
			}
		}
		Vector3 val5 = val3 - val2;
		if (val5.sqrMagnitude < 0.0001f)
		{
			val5 = normalized;
		}
		_selectionTransform.position = val2;
		_selectionTransform.rotation = Quaternion.LookRotation(val5.normalized, Vector3.up);
		UpdateGrabDebugVisuals(val2, cameraCenterRay.origin, cameraHitPoint, val3, currentGrabRange, hasCameraHit, hitIsReachable);
	}

	private Vector3 GetCharacterGrabOrigin(PlayerAvatar avatar)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)avatar == (Object)null)
		{
			return Vector3.zero;
		}
		Vector3 position = ((Component)avatar).transform.position;
		PlayerAvatarVisuals playerAvatarVisuals = avatar.playerAvatarVisuals;
		if ((Object)(object)playerAvatarVisuals != (Object)null && (Object)(object)playerAvatarVisuals.headLookAtTransform != (Object)null)
		{
			return playerAvatarVisuals.headLookAtTransform.position;
		}
		return position + Vector3.up * Mathf.Clamp(_runtimeOffsetY + _thirdPersonCrouchYOffset, 0.9f, 1.8f);
	}

	private float GetCurrentGrabRange()
	{
		PhysGrabber instance = PhysGrabber.instance;
		if ((Object)(object)instance != (Object)null && instance.grabRange > 0.1f)
		{
			return instance.grabRange;
		}
		return Mathf.Max(0.1f, (_selectionMaxDistance != null) ? Mathf.Min(_selectionMaxDistance.Value, 4f) : 4f);
	}

	private static Vector3 ResolveCharacterReachCrosshairTarget(Vector3 characterOrigin, Ray cameraCenterRay, float grabRange)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		Vector3 direction = cameraCenterRay.direction;
		Vector3 normalized = direction.normalized;
		if (normalized.sqrMagnitude < 0.0001f)
		{
			return characterOrigin + Vector3.forward * grabRange;
		}
		float num = Mathf.Max(0.1f, grabRange);
		Vector3 val = cameraCenterRay.origin - characterOrigin;
		float num2 = Vector3.Dot(val, normalized);
		float num3 = val.sqrMagnitude - num * num;
		float num4 = num2 * num2 - num3;
		if (num4 >= 0f)
		{
			float num5 = Mathf.Sqrt(num4);
			float num6 = 0f - num2 - num5;
			float num7 = 0f - num2 + num5;
			float num8 = ((num7 >= 0f) ? num7 : num6);
			if (num8 >= 0f)
			{
				return cameraCenterRay.origin + normalized * num8;
			}
		}
		float num9 = Mathf.Max(0f, 0f - num2);
		Vector3 val2 = cameraCenterRay.origin + normalized * num9 - characterOrigin;
		if (val2.sqrMagnitude < 0.0001f)
		{
			val2 = normalized;
		}
		return characterOrigin + val2.normalized * num;
	}

	private void UpdateGrabDebugVisuals(Vector3 grabOrigin, Vector3 cameraOrigin, Vector3 cameraHitPoint, Vector3 grabTarget, float grabRange, bool hasCameraHit, bool hitIsReachable)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		if (_debugShowGrabSelection == null || !_debugShowGrabSelection.Value || !_thirdPersonActive || _temporarilyFirstPerson)
		{
			HideGrabDebugVisuals();
			return;
		}
		Color val = default(Color);
		val = new Color(1f, 0.85f, 0.05f, 0.95f);
		Color val2 = default(Color);
		val2 = new Color(0.2f, 1f, 0.25f, 0.95f);
		Color val3 = default(Color);
		val3 = new Color(0.25f, 0.95f, 1f, 0.95f);
		Color val4 = default(Color);
		val4 = new Color(1f, 0.1f, 0.08f, 0.95f);
		Color color = default(Color);
		color = new Color(1f, 1f, 1f, 0.45f);
		Color val5 = ((!hasCameraHit) ? val3 : (hitIsReachable ? val2 : val4));
		SetDebugPoint(_debugGrabOriginPoint, grabOrigin, val3, active: true);
		SetDebugPoint(_debugGrabCameraHitPoint, cameraHitPoint, hasCameraHit ? val5 : val, active: true);
		SetDebugPoint(_debugGrabTargetPoint, grabTarget, val5, active: true);
		SetDebugLine(GetOrCreateDebugLine(ref _debugGrabCameraRayLine, ref _debugGrabCameraRayMaterial, "REPO Native Third Person Grab Camera Ray Debug", val, 0.018f), cameraOrigin, cameraHitPoint, val, active: true);
		SetDebugLine(GetOrCreateDebugLine(ref _debugGrabCharacterRayLine, ref _debugGrabCharacterRayMaterial, "REPO Native Third Person Grab Character Ray Debug", val5, 0.028f), grabOrigin, grabTarget, val5, active: true);
		Vector3 val6 = grabTarget - grabOrigin;
		Vector3 end = grabOrigin + ((val6.sqrMagnitude > 0.0001f) ? val6.normalized : Vector3.forward) * Mathf.Max(0.1f, grabRange);
		SetDebugLine(GetOrCreateDebugLine(ref _debugGrabRangeLine, ref _debugGrabRangeMaterial, "REPO Native Third Person Grab Range Debug", color, 0.012f), grabOrigin, end, color, active: true);
	}

	private static void SetDebugPoint(Transform point, Vector3 position, Color color, bool active)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)point == (Object)null)
		{
			return;
		}
		GameObject gameObject = ((Component)point).gameObject;
		gameObject.SetActive(active);
		if (active)
		{
			point.position = position;
			Renderer component = gameObject.GetComponent<Renderer>();
			if ((Object)(object)component != (Object)null && (Object)(object)component.material != (Object)null)
			{
				component.material.color = color;
			}
		}
	}

	private static void SetDebugLine(LineRenderer line, Vector3 start, Vector3 end, Color color, bool active)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)line == (Object)null)
		{
			return;
		}
		((Component)line).gameObject.SetActive(active);
		if (active)
		{
			line.startColor = color;
			line.endColor = color;
			line.SetPosition(0, start);
			line.SetPosition(1, end);
			if ((Object)(object)((Renderer)line).material != (Object)null && ((Renderer)line).material.HasProperty("_Color"))
			{
				((Renderer)line).material.SetColor("_Color", color);
			}
		}
	}

	private void HideGrabDebugVisuals()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		SetDebugPoint(_debugGrabOriginPoint, Vector3.zero, Color.clear, active: false);
		SetDebugPoint(_debugGrabCameraHitPoint, Vector3.zero, Color.clear, active: false);
		SetDebugPoint(_debugGrabTargetPoint, Vector3.zero, Color.clear, active: false);
		SetDebugLine(_debugGrabCameraRayLine, Vector3.zero, Vector3.zero, Color.clear, active: false);
		SetDebugLine(_debugGrabCharacterRayLine, Vector3.zero, Vector3.zero, Color.clear, active: false);
		SetDebugLine(_debugGrabRangeLine, Vector3.zero, Vector3.zero, Color.clear, active: false);
	}

	internal bool TryGetSelectionOverride(PlayerLocalCamera localCamera, ref Transform result)
	{
		if (!CanOverrideSelection(localCamera))
		{
			return false;
		}
		result = _selectionTransform;
		return true;
	}

	internal bool TryGetSelectionOverrideActive(PlayerLocalCamera localCamera, ref bool result)
	{
		if (!CanOverrideSelection(localCamera))
		{
			return false;
		}
		result = true;
		return true;
	}

	private bool CanOverrideSelection(PlayerLocalCamera localCamera)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0052: Expected O, but got Unknown
		if (!_thirdPersonActive || !_cameraCenteredSelection.Value || _temporarilyFirstPerson)
		{
			return false;
		}
		if (!((Object)(object)_selectionTransform != (Object)null) || !((Object)(object)localCamera != (Object)null))
		{
			return false;
		}
		return (Object)localCamera.playerAvatar == (Object)PlayerAvatar.instance;
	}

	private float ClampDistance(float distance)
	{
		float num = Mathf.Max(0.1f, _minDistance?.Value ?? 1f);
		float num2 = Mathf.Max(num, _maxDistance?.Value ?? 10f);
		return Mathf.Clamp(distance, num, num2);
	}

	private void ApplyClipPlanes()
	{
		CollectCameras();
		foreach (Camera item in _cameraBuffer)
		{
			if ((Object)(object)item != (Object)null)
			{
				if (!_originalClipPlanes.ContainsKey(item))
				{
					_originalClipPlanes.Add(item, new ClipPlaneState
					{
						Near = item.nearClipPlane,
						Far = item.farClipPlane
					});
				}
				ClipPlaneState clipPlaneState = _originalClipPlanes[item];
				item.nearClipPlane = Mathf.Min(clipPlaneState.Near, _nearClipPlane.Value);
				item.farClipPlane = Mathf.Max(clipPlaneState.Far, _minimumFarClipPlane.Value);
			}
		}
	}

	private void RestoreClipPlanes()
	{
		foreach (KeyValuePair<Camera, ClipPlaneState> originalClipPlane in _originalClipPlanes)
		{
			Camera key = originalClipPlane.Key;
			if ((Object)(object)key != (Object)null)
			{
				key.nearClipPlane = originalClipPlane.Value.Near;
				key.farClipPlane = originalClipPlane.Value.Far;
			}
		}
		_originalClipPlanes.Clear();
		_cameraBuffer.Clear();
	}

	private void CollectCameras()
	{
		_cameraBuffer.Clear();
		AddCamera(Camera.main);
		Camera[] allCameras = Camera.allCameras;
		for (int i = 0; i < allCameras.Length; i++)
		{
			AddCamera(allCameras[i]);
		}
		CameraZoom instance = CameraZoom.Instance;
		if (instance?.cams == null)
		{
			return;
		}
		foreach (Camera cam in instance.cams)
		{
			AddCamera(cam);
		}
	}

	private void AddCamera(Camera cam)
	{
		if ((Object)(object)cam != (Object)null && !cam.orthographic && !_cameraBuffer.Contains(cam))
		{
			_cameraBuffer.Add(cam);
		}
	}

	private void CreateInputActions()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		DisposeInputActions();
		_toggleAction = CreateKeyboardAction("REPO Native Third Person Toggle", _toggleKey.Value);
		_zoomInAction = CreateKeyboardAction("REPO Native Third Person Zoom In", _zoomInKey.Value);
		_zoomOutAction = CreateKeyboardAction("REPO Native Third Person Zoom Out", _zoomOutKey.Value);
	}

	private static InputAction CreateKeyboardAction(string name, KeyCode keyCode)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		string text = KeyCodeToKeyboardPath(keyCode);
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		InputAction val = new InputAction(name, (InputActionType)1, text, (string)null, (string)null, (string)null);
		val.Enable();
		return val;
	}

	private void DisposeInputActions()
	{
		DisposeInputAction(ref _toggleAction);
		DisposeInputAction(ref _zoomInAction);
		DisposeInputAction(ref _zoomOutAction);
	}

	private static void DisposeInputAction(ref InputAction action)
	{
		if (action != null)
		{
			action.Disable();
			action.Dispose();
			action = null;
		}
	}

	private static bool IsActionPressedThisFrame(InputAction action)
	{
		if (action != null && action.enabled)
		{
			return action.WasPressedThisFrame();
		}
		return false;
	}

	private static bool IsActionHeld(InputAction action)
	{
		if (action != null && action.enabled)
		{
			return action.IsPressed();
		}
		return false;
	}

	private static bool IsKeyPressedThisFrame(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return Input.GetKeyDown(key);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsKeyHeld(KeyCode key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return Input.GetKey(key);
		}
		catch
		{
			return false;
		}
	}

	private static float ReadScrollWheel()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)InputManager.instance != (Object)null)
		{
			float scrollY = InputManager.instance.GetScrollY();
			if (Mathf.Abs(scrollY) > 0.001f)
			{
				return Mathf.Sign(scrollY);
			}
		}
		if (Mouse.current != null)
		{
			float y = ((InputControl<Vector2>)(object)Mouse.current.scroll).ReadValue().y;
			if (Mathf.Abs(y) > 0.001f)
			{
				return Mathf.Sign(y);
			}
		}
		try
		{
			return Input.mouseScrollDelta.y;
		}
		catch
		{
			return 0f;
		}
	}

	private static bool TryGetKeyboardKey(KeyCode keyCode, out Key key)
	{
		key = (Key)0;
		return false;
	}

	private static string KeyCodeToKeyboardPath(KeyCode keyCode)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Expected I4, but got Unknown
		int num = (int)keyCode;
		if (num >= 97 && num <= 122)
		{
			return $"<Keyboard>/{(char)num}";
		}
		if (num >= 48 && num <= 57)
		{
			return $"<Keyboard>/{num - 48}";
		}
		if (num >= 256 && num <= 265)
		{
			return $"<Keyboard>/numpad{num - 256}";
		}
		switch (num)
		{
		case 8:
			return "<Keyboard>/backspace";
		case 9:
			return "<Keyboard>/tab";
		case 13:
			return "<Keyboard>/enter";
		case 27:
			return "<Keyboard>/escape";
		case 32:
			return "<Keyboard>/space";
		case 43:
		case 61:
			return "<Keyboard>/equals";
		case 45:
			return "<Keyboard>/minus";
		case 127:
			return "<Keyboard>/delete";
		case 273:
			return "<Keyboard>/upArrow";
		case 274:
			return "<Keyboard>/downArrow";
		case 275:
			return "<Keyboard>/rightArrow";
		case 276:
			return "<Keyboard>/leftArrow";
		case 277:
			return "<Keyboard>/insert";
		case 278:
			return "<Keyboard>/home";
		case 279:
			return "<Keyboard>/end";
		case 280:
			return "<Keyboard>/pageUp";
		case 281:
			return "<Keyboard>/pageDown";
		default:
			if (num >= 282 && num <= 293)
			{
				return $"<Keyboard>/f{num - 281}";
			}
			return null;
		}
	}
}





