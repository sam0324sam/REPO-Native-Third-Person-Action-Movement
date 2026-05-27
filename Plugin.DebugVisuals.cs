using UnityEngine;
using UnityEngine.Rendering;

namespace RepoThirdPerson;

public sealed partial class Plugin
{
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

	private static Vector3 GetCharacterBodyDebugOrigin(PlayerAvatar avatar, Vector3 grabOrigin)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)avatar == (Object)null)
		{
			return grabOrigin;
		}
		Vector3 position = ((Component)avatar).transform.position;
		return Vector3.Lerp(position, grabOrigin, 0.45f);
	}

	private void UpdateGrabDebugVisuals(Vector3 grabOrigin, Vector3 cameraRayDebugOrigin, Vector3 cameraHitPoint, Vector3 grabTarget, float grabRange, bool hasCameraHit, bool hitIsReachable)
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
		SetDebugLine(GetOrCreateDebugLine(ref _debugGrabCameraRayLine, ref _debugGrabCameraRayMaterial, "REPO Native Third Person Grab Camera Ray Debug", val, 0.018f), cameraRayDebugOrigin, cameraHitPoint, val, active: true);
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
}
