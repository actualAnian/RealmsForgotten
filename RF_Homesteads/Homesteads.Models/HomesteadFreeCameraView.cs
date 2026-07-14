using System;
using System.Reflection;
using Homesteads.Views;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace Homesteads.Models;

public class HomesteadFreeCameraView : MissionView
{
	public static HomesteadFreeCameraView? Instance;

	private static readonly MethodInfo? _setCameraBearing = typeof(MissionScreen).GetProperty("CameraBearing")?.GetSetMethod(nonPublic: true);

	private static readonly MethodInfo? _setCameraElevation = typeof(MissionScreen).GetProperty("CameraElevation")?.GetSetMethod(nonPublic: true);

	private static readonly FieldInfo? _bearingDeltaField = typeof(MissionScreen).GetField("_cameraBearingDelta", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo? _elevationDeltaField = typeof(MissionScreen).GetField("_cameraElevationDelta", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly MethodInfo? _setLastFollowedAgent = typeof(MissionScreen).GetProperty("LastFollowedAgent")?.GetSetMethod(nonPublic: true);

	private bool _isActive;

	private bool _needsAgentFreeze;

	private Vec3 _cameraPosition;

	private float _cameraBearing;

	private float _cameraElevation = -0.65f;

	private bool _shiftHeld;

	private float _shiftMouseAccum;

	private bool _shiftWasDrag;

	private const float ShiftDragThreshold = 4f;

	private Vec3 _mouseRayBegin = Vec3.Invalid;

	private Vec3 _mouseRayEnd = Vec3.Invalid;

	private bool _prevLmbDown;

	private bool _lmbClickedThisFrame;

	private bool _prevRmbDown;

	private Vec3 _rmbSavedRayBegin = Vec3.Invalid;

	private Vec3 _rmbSavedRayEnd = Vec3.Invalid;

	private bool _rmbRayLocked;

	private Vec2 _rmbLockReleasedAt;

	private bool _conversationCamActive;

	private Vec3 _conversationCamPos;

	private Vec3 _conversationLookTarget;

	private bool _capturedPreFrame;

	private MatrixFrame _preConversationFrame;

	private bool _blendingOut;

	private float _blendT;

	private float _blendDuration = 0.35f;

	private Vec3 _blendFromCamPos;

	private Vec3 _blendFromLook;

	private Vec3 _blendToCamPos;

	private Vec3 _blendToLook;

	public bool LmbClickedThisFrame => _lmbClickedThisFrame;

	public bool IsActive => _isActive;

	public bool IsFreezingRay
	{
		get
		{
			if (_isActive)
			{
				if (!_shiftHeld || !_shiftWasDrag)
				{
					MissionScreen missionScreen = ((MissionView)this).MissionScreen;
					return ((missionScreen == null) ? ((bool?)null) : missionScreen.SceneLayer?.Input?.IsKeyDown(InputKey.RightMouseButton)) == true;
				}
				return true;
			}
			return false;
		}
	}

	public float SceneMouseMoveX
	{
		get
		{
			if (_isActive)
			{
				MissionScreen missionScreen = ((MissionView)this).MissionScreen;
				if (((missionScreen != null) ? missionScreen.SceneLayer : null) != null)
				{
					return ((MissionView)this).MissionScreen.SceneLayer.Input.GetMouseMoveX();
				}
			}
			return 0f;
		}
	}

	public float SceneMouseMoveY
	{
		get
		{
			if (_isActive)
			{
				MissionScreen missionScreen = ((MissionView)this).MissionScreen;
				if (((missionScreen != null) ? missionScreen.SceneLayer : null) != null)
				{
					return ((MissionView)this).MissionScreen.SceneLayer.Input.GetMouseMoveY();
				}
			}
			return 0f;
		}
	}

	public Vec3 CameraRightHorizontal
	{
		get
		{
			if (_isActive)
			{
				MissionScreen missionScreen = ((MissionView)this).MissionScreen;
				if (!(((missionScreen != null) ? missionScreen.CombatCamera : null) == null))
				{
					Vec3 s = ((MissionView)this).MissionScreen.CombatCamera.Frame.rotation.s;
					s.z = 0f;
					float length = s.Length;
					if (!(length > 0.001f))
					{
						return new Vec3(1f);
					}
					return s * (1f / length);
				}
			}
			return new Vec3(1f);
		}
	}

	public Vec3 CameraForwardHorizontal
	{
		get
		{
			if (_isActive)
			{
				MissionScreen missionScreen = ((MissionView)this).MissionScreen;
				if (!(((missionScreen != null) ? missionScreen.CombatCamera : null) == null))
				{
					Vec3 vec = -((MissionView)this).MissionScreen.CombatCamera.Frame.rotation.u;
					vec.z = 0f;
					float length = vec.Length;
					if (!(length > 0.001f))
					{
						return new Vec3(0f, 1f);
					}
					return vec * (1f / length);
				}
			}
			return new Vec3(0f, 1f);
		}
	}

	public Mat3 CameraHorizontalRotation
	{
		get
		{
			Mat3 identity = Mat3.Identity;
			identity.RotateAboutSide(TaleWorlds.Library.MathF.PI / 2f);
			identity.RotateAboutForward(_cameraBearing);
			return identity;
		}
	}

	public void SetConversationCamera(Vec3 camPos, Vec3 lookTarget)
	{
		_conversationCamPos = camPos;
		_conversationLookTarget = lookTarget;
		_conversationCamActive = true;
		_blendingOut = false;
	}

	public void BeginConversationBlendOut(float duration = 0.35f)
	{
		if (!_conversationCamActive || !_capturedPreFrame)
		{
			ClearConversationCamera();
			return;
		}
		_blendFromCamPos = _conversationCamPos;
		_blendFromLook = _conversationLookTarget;
		_blendToCamPos = _preConversationFrame.origin;
		_blendToLook = _preConversationFrame.origin + -_preConversationFrame.rotation.u * 5f;
		_blendDuration = ((duration > 0.01f) ? duration : 0.35f);
		_blendT = 0f;
		_blendingOut = true;
	}

	public void ClearConversationCamera()
	{
		_conversationCamActive = false;
		_blendingOut = false;
		_capturedPreFrame = false;
	}

	public override void OnMissionScreenInitialize()
	{
		((MissionView)this).OnMissionScreenInitialize();
		Instance = this;
		base.ViewOrderPriority = 1;
	}

	public override void OnMissionScreenFinalize()
	{
		((MissionView)this).OnMissionScreenFinalize();
		_conversationCamActive = false;
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void SetActive(bool active)
	{
		if (_isActive == active)
		{
			return;
		}
		_isActive = active;
		if (active)
		{
			_cameraPosition = ((MissionView)this).MissionScreen.CombatCamera.Frame.origin;
			_cameraBearing = ((MissionView)this).MissionScreen.CameraBearing;
			_cameraElevation = -0.65f;
			float groundHeightAtPosition = ((MissionBehavior)this).Mission.Scene.GetGroundHeightAtPosition(_cameraPosition);
			if (groundHeightAtPosition < 9999f)
			{
				_cameraPosition.z = groundHeightAtPosition + 20f;
			}
			_bearingDeltaField?.SetValue(((MissionView)this).MissionScreen, 0f);
			_elevationDeltaField?.SetValue(((MissionView)this).MissionScreen, 0f);
			_setLastFollowedAgent?.Invoke(((MissionView)this).MissionScreen, new object[1]);
			((MissionView)this).MissionScreen.LastFollowedAgentVisuals = null;
			_shiftHeld = false;
			_shiftMouseAccum = 0f;
			_shiftWasDrag = false;
			_prevLmbDown = false;
			_lmbClickedThisFrame = false;
			((ScreenBase)(object)((MissionView)this).MissionScreen).MouseVisible = true;
			MouseManager.ShowCursor(show: true);
			if (Agent.Main != null)
			{
				Agent.Main.Controller = AgentControllerType.AI;
			}
			else
			{
				_needsAgentFreeze = true;
			}
		}
		else
		{
			_needsAgentFreeze = false;
			if (Agent.Main != null)
			{
				Agent.Main.Controller = AgentControllerType.Player;
			}
			((ScreenBase)(object)((MissionView)this).MissionScreen).MouseVisible = false;
			MouseManager.ShowCursor(show: false);
			_setCameraBearing?.Invoke(((MissionView)this).MissionScreen, new object[1] { _cameraBearing });
			_setCameraElevation?.Invoke(((MissionView)this).MissionScreen, new object[1] { _cameraElevation });
		}
	}

	public override bool UpdateOverridenCamera(float dt)
	{
		if (_isActive)
		{
			UpdateFreeCameraFrame(dt);
			return true;
		}
		if (_conversationCamActive)
		{
			if (!_capturedPreFrame)
			{
				_preConversationFrame = ((MissionView)this).MissionScreen.CombatCamera.Frame;
				_capturedPreFrame = true;
			}
			if (_blendingOut)
			{
				_blendT += dt / _blendDuration;
				float num = ((_blendT < 0f) ? 0f : ((_blendT > 1f) ? 1f : _blendT));
				float alpha = num * num * (3f - 2f * num);
				_conversationCamPos = Vec3.Lerp(_blendFromCamPos, _blendToCamPos, alpha);
				_conversationLookTarget = Vec3.Lerp(_blendFromLook, _blendToLook, alpha);
				ApplyConversationFrame();
				if (_blendT >= 1f)
				{
					ClearConversationCamera();
				}
				return true;
			}
			ApplyConversationFrame();
			return true;
		}
		return ((MissionView)this).UpdateOverridenCamera(dt);
	}

	private void ApplyConversationFrame()
	{
		Vec3 vec = _conversationLookTarget - _conversationCamPos;
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec3(0f, 1f);
		}
		vec.Normalize();
		Vec3 vec2 = Vec3.CrossProduct(vb: new Vec3(0f, 0f, 1f), va: vec);
		if (vec2.LengthSquared < 0.0001f)
		{
			vec2 = new Vec3(1f);
		}
		vec2.Normalize();
		Vec3 f = Vec3.CrossProduct(vec2, vec);
		MatrixFrame cameraFrame = MatrixFrame.Identity;
		cameraFrame.rotation.s = vec2;
		cameraFrame.rotation.f = f;
		cameraFrame.rotation.u = -vec;
		cameraFrame.origin = _conversationCamPos;
		((MissionView)this).MissionScreen.CombatCamera.Frame = cameraFrame;
		((MissionBehavior)this).Mission.SetCameraFrame(ref cameraFrame, 1f);
		((MissionView)this).MissionScreen.SceneView?.SetCamera(((MissionView)this).MissionScreen.CombatCamera);
	}

	public override void OnMissionScreenTick(float dt)
	{
		((MissionView)this).OnMissionScreenTick(dt);
		if (!_isActive)
		{
			return;
		}
		if (_needsAgentFreeze && Agent.Main != null)
		{
			Agent.Main.Controller = AgentControllerType.AI;
			_needsAgentFreeze = false;
		}
		bool flag = ((MissionView)this).MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.LeftShift);
		if (flag && !_shiftHeld)
		{
			_shiftHeld = true;
			_shiftMouseAccum = 0f;
			_shiftWasDrag = false;
		}
		else if (!flag && _shiftHeld)
		{
			_shiftHeld = false;
			_shiftWasDrag = false;
			((ScreenBase)(object)((MissionView)this).MissionScreen).MouseVisible = true;
			MouseManager.ShowCursor(show: true);
		}
		if (_shiftHeld)
		{
			float mouseMoveX = ((MissionView)this).MissionScreen.SceneLayer.Input.GetMouseMoveX();
			float mouseMoveY = ((MissionView)this).MissionScreen.SceneLayer.Input.GetMouseMoveY();
			_shiftMouseAccum += TaleWorlds.Library.MathF.Abs(mouseMoveX) + TaleWorlds.Library.MathF.Abs(mouseMoveY);
			if (_shiftMouseAccum > 4f)
			{
				_shiftWasDrag = true;
				((ScreenBase)(object)((MissionView)this).MissionScreen).MouseVisible = false;
				MouseManager.ShowCursor(show: false);
			}
		}
		if (!_shiftHeld || !_shiftWasDrag)
		{
			((ScreenBase)(object)((MissionView)this).MissionScreen).MouseVisible = true;
			MouseManager.ShowCursor(show: true);
		}
		bool flag2 = ((MissionView)this).Input.IsKeyDown(InputKey.LeftMouseButton);
		_lmbClickedThisFrame = !flag2 && _prevLmbDown && (!_shiftHeld || !_shiftWasDrag);
		_prevLmbDown = flag2;
		bool flag3 = ((MissionView)this).MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton);
		if (flag3 && !_prevRmbDown)
		{
			_rmbSavedRayBegin = _mouseRayBegin;
			_rmbSavedRayEnd = _mouseRayEnd;
			_rmbRayLocked = false;
		}
		if (!flag3 && _prevRmbDown && _rmbSavedRayBegin.IsValid)
		{
			_rmbRayLocked = true;
			_rmbLockReleasedAt = ((MissionView)this).MissionScreen.SceneLayer.Input.GetMousePositionRanged();
		}
		_prevRmbDown = flag3;
		RefreshMouseRay();
	}

	public bool TryGetMouseRay(out Vec3 rayBegin, out Vec3 rayEnd)
	{
		rayBegin = _mouseRayBegin;
		rayEnd = _mouseRayEnd;
		if (_isActive)
		{
			return _mouseRayBegin.IsValid;
		}
		return false;
	}

	public bool IsKeyPressedOnScene(InputKey key)
	{
		if (_isActive && (!_shiftHeld || !_shiftWasDrag))
		{
			MissionScreen missionScreen = ((MissionView)this).MissionScreen;
			return ((missionScreen == null) ? ((bool?)null) : missionScreen.SceneLayer?.Input?.IsKeyPressed(key)) == true;
		}
		return false;
	}

	public void AdjustCameraHeight(float delta)
	{
		_cameraPosition.z += delta;
	}

	private void UpdateFreeCameraFrame(float dt)
	{
		if (HomesteadBuildingPickerView.IsOpen)
		{
			return;
		}
		if (_shiftHeld && _shiftWasDrag)
		{
			HandleRotateInput(dt);
		}
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			Vec2 keyState = Input.GetKeyState(InputKey.ControllerRStick);
			if (TaleWorlds.Library.MathF.Abs(keyState.X) > 0.15f || TaleWorlds.Library.MathF.Abs(keyState.Y) > 0.15f)
			{
				_cameraBearing -= keyState.X * 2f * dt;
				_cameraElevation += keyState.Y * 1.4f * dt;
				_cameraElevation = MBMath.ClampFloat(_cameraElevation, -1.36591f, 0.5f);
			}
		}
		MatrixFrame frame = MatrixFrame.Identity;
		frame.rotation.RotateAboutSide(TaleWorlds.Library.MathF.PI / 2f);
		frame.rotation.RotateAboutForward(_cameraBearing);
		frame.rotation.RotateAboutSide(_cameraElevation);
		frame.origin = _cameraPosition;
		HandleMoveInput(dt, ref frame);
		ClampToTerrain(ref frame);
		_cameraPosition = frame.origin;
		((MissionView)this).MissionScreen.CombatCamera.Frame = frame;
		((MissionBehavior)this).Mission.SetCameraFrame(ref frame, 1f);
		((MissionView)this).MissionScreen.SceneView?.SetCamera(((MissionView)this).MissionScreen.CombatCamera);
		_setCameraBearing?.Invoke(((MissionView)this).MissionScreen, new object[1] { _cameraBearing });
		_setCameraElevation?.Invoke(((MissionView)this).MissionScreen, new object[1] { _cameraElevation });
	}

	private void HandleRotateInput(float dt)
	{
		float mouseSensitivity = ((MissionView)this).MissionScreen.SceneLayer.Input.GetMouseSensitivity();
		float num = 5.4E-05f * mouseSensitivity * ((MissionView)this).MissionScreen.CameraViewAngle;
		float mouseMoveX = ((MissionView)this).MissionScreen.SceneLayer.Input.GetMouseMoveX();
		float mouseMoveY = ((MissionView)this).MissionScreen.SceneLayer.Input.GetMouseMoveY();
		_cameraBearing -= mouseMoveX * num;
		_cameraElevation += (NativeConfig.InvertMouse ? mouseMoveY : (0f - mouseMoveY)) * num;
		_cameraElevation = MBMath.ClampFloat(_cameraElevation, -1.36591f, 0.5f);
	}

	private void HandleMoveInput(float dt, ref MatrixFrame frame)
	{
		float groundHeightAtPosition = ((MissionBehavior)this).Mission.Scene.GetGroundHeightAtPosition(frame.origin);
		float num = frame.origin.z - ((groundHeightAtPosition < 9999f) ? groundHeightAtPosition : (frame.origin.z - 5f));
		float num2 = 3f * TaleWorlds.Library.MathF.Clamp(1f + num / 2f, 1f, 30f);
		Vec3 vec = (-frame.rotation.u).NormalizedCopy();
		Vec3 vec2 = frame.rotation.s.AsVec2.ToVec3().NormalizedCopy();
		float x = vec.x;
		float y = vec.y;
		float num3 = TaleWorlds.Library.MathF.Sqrt(x * x + y * y);
		Vec3 vec3 = ((num3 > 0.001f) ? new Vec3(x / num3, y / num3) : vec);
		Vec3 vec4 = (((MissionView)this).Input.IsKeyDown(InputKey.LeftShift) ? vec : vec3);
		if (((MissionView)this).Input.IsKeyDown(InputKey.W))
		{
			frame.origin += vec4 * num2 * dt;
		}
		if (((MissionView)this).Input.IsKeyDown(InputKey.S))
		{
			frame.origin -= vec4 * num2 * dt;
		}
		if (((MissionView)this).Input.IsKeyDown(InputKey.A))
		{
			frame.origin -= vec2 * num2 * dt;
		}
		if (((MissionView)this).Input.IsKeyDown(InputKey.D))
		{
			frame.origin += vec2 * num2 * dt;
		}
		if (((MissionView)this).Input.IsKeyDown(InputKey.Space))
		{
			frame.origin.z += num2 * dt;
		}
		if (((MissionView)this).Input.IsKeyDown(InputKey.LeftAlt))
		{
			frame.origin.z -= num2 * dt;
		}
		if (!TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			return;
		}
		InputKey inputKey = HomesteadsReloaded.Settings?.GetCtrlModifierKey() ?? InputKey.ControllerRUp;
		if (inputKey != InputKey.Invalid && ((MissionView)this).Input.IsKeyDown(inputKey))
		{
			Vec2 keyState = Input.GetKeyState(InputKey.ControllerLStick);
			if (TaleWorlds.Library.MathF.Abs(keyState.X) > 0.15f || TaleWorlds.Library.MathF.Abs(keyState.Y) > 0.15f)
			{
				frame.origin += vec * (keyState.Y * num2 * dt);
				frame.origin += vec2 * (keyState.X * num2 * dt);
			}
		}
		InputKey inputKey2 = HomesteadsReloaded.Settings?.GetCtrlCameraDownKey() ?? InputKey.ControllerLBumper;
		InputKey inputKey3 = HomesteadsReloaded.Settings?.GetCtrlCameraUpKey() ?? InputKey.ControllerRBumper;
		if (inputKey2 != InputKey.Invalid && ((MissionView)this).Input.IsKeyDown(inputKey2))
		{
			frame.origin.z -= num2 * dt;
		}
		if (inputKey3 != InputKey.Invalid && ((MissionView)this).Input.IsKeyDown(inputKey3))
		{
			frame.origin.z += num2 * dt;
		}
	}

	private void ClampToTerrain(ref MatrixFrame frame)
	{
		float groundHeightAtPosition = ((MissionBehavior)this).Mission.Scene.GetGroundHeightAtPosition(frame.origin + new Vec3(0f, 0f, 100f));
		if (groundHeightAtPosition < 9999f)
		{
			frame.origin.z = TaleWorlds.Library.MathF.Max(frame.origin.z, groundHeightAtPosition + 1.5f);
		}
	}

	private void RefreshMouseRay()
	{
		_mouseRayBegin = Vec3.Invalid;
		_mouseRayEnd = Vec3.Invalid;
		MissionScreen missionScreen = ((MissionView)this).MissionScreen;
		if (((missionScreen != null) ? missionScreen.SceneLayer : null) == null || (_shiftHeld && _shiftWasDrag) || ((MissionView)this).MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton))
		{
			return;
		}
		if (_rmbRayLocked)
		{
			Vec2 vec = ((MissionView)this).MissionScreen.SceneLayer.Input.GetMousePositionRanged() - _rmbLockReleasedAt;
			if (vec.x * vec.x + vec.y * vec.y < 2E-05f)
			{
				_mouseRayBegin = _rmbSavedRayBegin;
				_mouseRayEnd = _rmbSavedRayEnd;
				return;
			}
			_rmbRayLocked = false;
		}
		try
		{
			Vec2 mousePositionRanged = ((MissionView)this).MissionScreen.SceneLayer.Input.GetMousePositionRanged();
			if (((MissionView)this).MissionScreen != null)
			{
				Vec3 vec2 = default(Vec3);
				Vec3 vec3 = default(Vec3);
				((MissionView)this).MissionScreen.ScreenPointToWorldRay(mousePositionRanged, out vec2, out vec3);
				if (vec2.IsValid && (vec3 - vec2).LengthSquared > 0.001f)
				{
					_mouseRayBegin = vec2;
					_mouseRayEnd = vec3;
					return;
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadFreeCameraView", "RefreshMouseRay (ScreenPointToWorldRay) failed: " + ex.Message);
		}
		try
		{
			Vec3 position = ((MissionView)this).MissionScreen.CombatCamera.Position;
			Vec3 vec4 = ((MissionView)this).MissionScreen.CombatCamera.Direction.NormalizedCopy();
			if (vec4.IsValid && vec4.LengthSquared > 0.001f)
			{
				_mouseRayBegin = position;
				_mouseRayEnd = position + vec4 * 1000f;
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadFreeCameraView", "RefreshMouseRay (fallback) failed: " + ex2.Message);
		}
	}
}
