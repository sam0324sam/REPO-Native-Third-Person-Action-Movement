using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.InputSystem;

namespace RepoThirdPerson;

public sealed partial class Plugin
{
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

		[HarmonyPostfix]
		[HarmonyPriority(0)]
		[HarmonyPatch(typeof(PlayerAvatarRightArm), "Update")]
		private static void PlayerAvatarRightArmUpdatePostfix(PlayerAvatarRightArm __instance)
		{
			Instance?.ForceLocalThirdPersonRightArmPose(__instance);
		}

		[HarmonyPostfix]
		[HarmonyPriority(0)]
		[HarmonyPatch(typeof(PlayerAvatarLeftArm), "Update")]
		private static void PlayerAvatarLeftArmUpdatePostfix(PlayerAvatarLeftArm __instance)
		{
			Instance?.ForceLocalThirdPersonLeftArmPose(__instance);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(EnemyOnScreen), "Awake")]
		private static void EnemyOnScreenAwakePostfix(EnemyOnScreen __instance)
		{
			Instance?.EnableEnemyOnScreenDuringThirdPerson(__instance);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(EnemyOnScreen), "OnEnable")]
		private static void EnemyOnScreenOnEnablePostfix(EnemyOnScreen __instance)
		{
			Instance?.EnableEnemyOnScreenDuringThirdPerson(__instance);
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SemiPuke), "PukeActive")]
		private static void SemiPukePukeActivePrefix(ref Quaternion _direction)
		{
			Instance?.OverridePukeDirection(ref _direction);
		}
	}
}
