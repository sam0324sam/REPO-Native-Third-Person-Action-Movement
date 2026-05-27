using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RepoThirdPerson;

public sealed partial class Plugin
{
	internal void TickFlashlightControllerPostUpdate(FlashlightController controller)
	{
		if (_thirdPersonActive && !_temporarilyFirstPerson && !((Object)(object)controller == (Object)null) && !((Object)(object)controller.PlayerAvatar == (Object)null) && !((Object)(object)controller.PlayerAvatar != (Object)(object)PlayerAvatar.instance))
		{
			controller.SetThirdPerson(true);
			AlignLocalThirdPersonFlashlight(controller);
			if ((Object)(object)controller.halo != (Object)null)
			{
				controller.halo.enabled = false;
			}
		}
	}

	private void AlignLocalThirdPersonFlashlight(FlashlightController controller)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar playerAvatar = controller.PlayerAvatar;
		if ((Object)(object)playerAvatar == (Object)null)
		{
			return;
		}
		Vector3 val;
		if (_hasVisualFacingRotation)
		{
			val = _visualFacingRotation * Vector3.forward;
		}
		else if ((Object)(object)playerAvatar.playerAvatarVisuals != (Object)null)
		{
			val = ((Component)playerAvatar.playerAvatarVisuals).transform.forward;
		}
		else
		{
			val = ((Component)playerAvatar).transform.forward;
		}
		val.y = 0f;
		if (val.sqrMagnitude < 0.0001f)
		{
			return;
		}
		val.Normalize();
		Transform transform = ((Component)controller).transform;
		transform.rotation = Quaternion.LookRotation(val, Vector3.up);
		if ((Object)(object)playerAvatar.flashlightLightAim != (Object)null)
		{
			playerAvatar.flashlightLightAim.clientAimPoint = transform.position + val * 100f;
		}
	}

	internal void ForceLocalThirdPersonRightArmPose(PlayerAvatarRightArm arm)
	{
		if (!_thirdPersonActive || _temporarilyFirstPerson || (Object)(object)arm == (Object)null || (Object)(object)arm.playerAvatar != (Object)(object)PlayerAvatar.instance)
		{
			return;
		}
		PhysGrabber physGrabber = arm.playerAvatar.physGrabber;
		if ((Object)(object)physGrabber == (Object)null || (!IsPhysGrabBeamActive(physGrabber) && !physGrabber.grabbed))
		{
			return;
		}
		if (_rightArmGrabberPoseOverrideMethod == null)
		{
			_rightArmGrabberPoseOverrideMethod = typeof(PlayerAvatarRightArm).GetMethod("GrabberPoseOverride", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		_rightArmGrabberPoseOverrideMethod?.Invoke(arm, new object[1] { 0.2f });
		ForceRightArmPoseNow(arm, arm.grabberPose);
	}

	internal void ForceLocalThirdPersonLeftArmPose(PlayerAvatarLeftArm arm)
	{
		if (!_thirdPersonActive || _temporarilyFirstPerson || (Object)(object)arm == (Object)null || (Object)(object)arm.playerAvatar != (Object)(object)PlayerAvatar.instance)
		{
			return;
		}
		FlashlightController flashlightController = arm.flashlightController;
		if ((Object)(object)flashlightController == (Object)null || (!flashlightController.LightActive && ((Object)(object)flashlightController.spotlight == (Object)null || !flashlightController.spotlight.enabled)))
		{
			return;
		}
		ForceLeftArmPoseNow(arm, arm.flashlightPose);
	}

	private void ForceRightArmPoseNow(PlayerAvatarRightArm arm, Vector3 pose)
	{
		if ((Object)(object)arm == (Object)null)
		{
			return;
		}
		if (_rightArmSetPoseMethod == null)
		{
			_rightArmSetPoseMethod = typeof(PlayerAvatarRightArm).GetMethod("SetPose", BindingFlags.Instance | BindingFlags.NonPublic);
		}
		if (_rightArmAnimatePoseMethod == null)
		{
			_rightArmAnimatePoseMethod = typeof(PlayerAvatarRightArm).GetMethod("AnimatePose", BindingFlags.Instance | BindingFlags.NonPublic);
		}
		_rightArmSetPoseMethod?.Invoke(arm, new object[1] { pose });
		_rightArmAnimatePoseMethod?.Invoke(arm, null);
	}

	private void ForceLeftArmPoseNow(PlayerAvatarLeftArm arm, Vector3 pose)
	{
		if ((Object)(object)arm == (Object)null)
		{
			return;
		}
		if (_leftArmSetPoseMethod == null)
		{
			_leftArmSetPoseMethod = typeof(PlayerAvatarLeftArm).GetMethod("SetPose", BindingFlags.Instance | BindingFlags.NonPublic);
		}
		if (_leftArmAnimatePoseMethod == null)
		{
			_leftArmAnimatePoseMethod = typeof(PlayerAvatarLeftArm).GetMethod("AnimatePose", BindingFlags.Instance | BindingFlags.NonPublic);
		}
		_leftArmSetPoseMethod?.Invoke(arm, new object[1] { pose });
		_leftArmAnimatePoseMethod?.Invoke(arm, null);
	}

	private bool IsPhysGrabBeamActive(PhysGrabber physGrabber)
	{
		if ((Object)(object)physGrabber == (Object)null)
		{
			return false;
		}
		if (_physGrabBeamActiveField == null)
		{
			_physGrabBeamActiveField = typeof(PhysGrabber).GetField("physGrabBeamActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		return _physGrabBeamActiveField != null && _physGrabBeamActiveField.GetValue(physGrabber) is bool flag && flag;
	}

	private PlayerAvatarRightArm GetPlayerAvatarRightArm(PlayerAvatarVisuals visuals)
	{
		if ((Object)(object)visuals == (Object)null)
		{
			return null;
		}
		if (_playerAvatarRightArmField == null)
		{
			_playerAvatarRightArmField = typeof(PlayerAvatarVisuals).GetField("playerAvatarRightArm", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		object value = _playerAvatarRightArmField?.GetValue(visuals);
		PlayerAvatarRightArm val = (PlayerAvatarRightArm)((value is PlayerAvatarRightArm) ? value : null);
		return (Object)(object)val != (Object)null ? val : ((Component)visuals).GetComponentInChildren<PlayerAvatarRightArm>(true);
	}

}
