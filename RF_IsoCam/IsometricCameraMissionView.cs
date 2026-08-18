using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
// MissionView has an instance property "Input" (MissionScreen.SceneLayer.Input) that shadows the
// static TaleWorlds.InputSystem.Input class. The instance context gates every key behind
// InputContext.IsKeysAllowed, which UI layers can turn off (e.g. town walkabout HUD), so the
// toggle key never registered. Alias the static class and read raw keyboard state instead.
using GlobalInput = TaleWorlds.InputSystem.Input;

namespace RF_IsoCam
{
    /// <summary>
    /// The heart of RF_IsoCam. A MissionView that, on a configurable key, swaps the normal
    /// combat camera for an elevated "RPG / RTS" isometric view that follows Agent.Main.
    ///
    /// Camera-override mechanism (verified against the installed 1.4.8 binaries):
    ///   MissionScreen exposes a public settable "CustomCamera" (TaleWorlds.Engine.Camera).
    ///   Every frame MissionScreen.CheckForUpdateCamera() checks it: when non-null it does
    ///   CombatCamera.FillParametersFrom(CustomCamera) + SceneView.SetCamera(CombatCamera)
    ///   and SKIPS the vanilla UpdateCamera(). Setting CustomCamera = null hands control back
    ///   to the game. So we own a Camera, point it each tick, and just assign/clear the field.
    ///   (See MissionScreen.CheckForUpdateCamera in TaleWorlds.MountAndBlade.View.dll.)
    ///
    /// The camera NEVER throws into the mission: every entry point is wrapped in try/catch and
    /// degrades to the normal game camera on any error.
    /// </summary>
    public class IsometricCameraMissionView : MissionView
    {
        private const string LogPrefix = "[RF_IsoCam] ";

        // Campaign-map style mouse controls (non-combat missions only).
        private const float MouseOrbitSensitivity = 0.006f; // radians per pixel of RMB drag
        private const float MouseTiltSensitivity = 0.08f;   // degrees per pixel of RMB drag
        private const float ZoomStepMeters = 1.5f;          // distance change per wheel notch
        private const float FacingSensitivity = 0.005f;     // radians per pixel of mouse-look

        private Camera _isoCamera;
        private bool _isoEnabled;        // is the iso view currently driving the camera?
        private bool _suspendedByState;  // temporarily off due to conversation/photo mode
        private float _bearing;          // horizontal orbit angle around the player (radians)
        private float _facingYaw;        // mouse-look yaw of the character (radians)

        // MissionMainAgentController derives the player's LookDirection (and WASD mapping,
        // which is relative to look) from MissionScreen.CameraBearing/CameraElevation. With
        // CustomCamera set, CheckForUpdateCamera never refreshes those scalars, so they go
        // stale and the player faces/walks toward the pre-toggle camera direction. We sync
        // them every frame. CameraElevation's setter is private -> cached reflection.
        private static readonly MethodInfo CameraElevationSetter =
            typeof(MissionScreen).GetProperty("CameraElevation")?.GetSetMethod(true);
        private static readonly object[] HorizontalElevation = { 0f };

        // MissionMainAgentController.CustomLookDir (public, only reset in its ctor): when
        // non-zero it REPLACES the CameraBearing-based look direction. We use it for RPG-style
        // facing: while moving, the character faces the movement direction; while idle it keeps
        // the last facing, so orbiting the camera does NOT spin the character.
        private MissionMainAgentController _mainAgentController;

        // Facing-arrow HUD (screen-center arrow showing where the mouse-look aims).
        private GauntletLayer _uiLayer;
        private FacingArrowVM _arrowVm;

        // ---- lifecycle -------------------------------------------------------

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            try
            {
                _isoEnabled = false;
                _suspendedByState = false;
                _bearing = 0f;
                Log("MissionView initialized on mission.");
            }
            catch (Exception e) { Log("OnMissionScreenInitialize error: " + e.Message); }
        }

        public override void OnMissionScreenFinalize()
        {
            try
            {
                ClearCustomCamera();
                ReleaseLookDirection();
                _mainAgentController = null;
                if (_uiLayer != null)
                {
                    MissionScreen?.RemoveLayer(_uiLayer);
                    _uiLayer = null;
                    _arrowVm = null;
                }
                if (_isoCamera != null)
                {
                    _isoCamera.ReleaseCamera();
                    _isoCamera = null;
                }
            }
            catch (Exception e) { Log("OnMissionScreenFinalize error: " + e.Message); }
            base.OnMissionScreenFinalize();
        }

        // ---- per-frame -------------------------------------------------------

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            try
            {
                Settings s = SafeSettings();
                bool masterEnabled = (s == null) || s.EnableIsoCamera;

                // Master switch off -> make sure we are not holding the camera.
                if (!masterEnabled)
                {
                    if (_isoEnabled) DisableIso("master switch off");
                    return;
                }

                // Toggle on key press.
                if (GlobalInput.IsKeyPressed(GetKey(s?.ToggleKey, InputKey.H)))
                {
                    if (_isoEnabled) DisableIso("toggled off");
                    else EnableIso();
                }

                if (!_isoEnabled) return;

                // Only makes sense while there is a controllable player agent.
                Agent main = Agent.Main;
                if (MissionScreen == null || main == null || !main.IsActive())
                {
                    // Keep the toggle "armed" but hand the camera back until the agent returns.
                    ClearCustomCamera();
                    if (_arrowVm != null) _arrowVm.IsArrowVisible = false;
                    return;
                }

                // If a game-owned state (conversation / photo mode) suspended us, don't fight it.
                if (_suspendedByState)
                {
                    ClearCustomCamera();
                    if (_arrowVm != null) _arrowVm.IsArrowVisible = false;
                    return;
                }

                // Re-assert ownership (cheap and idempotent) in case something cleared it.
                if ((NativeObject)(object)MissionScreen.CustomCamera != (NativeObject)(object)_isoCamera)
                {
                    MissionScreen.CustomCamera = _isoCamera;
                    MissionScreen.AllowInputWithCustomCamera = true;
                }

                // Rotation input (held).
                float rotSpeed = (s?.RotationSpeed ?? 70f) * (MathF.PI / 180f);
                if (GlobalInput.IsKeyDown(GetKey(s?.RotateLeftKey, InputKey.Delete))) _bearing += rotSpeed * dt;
                if (GlobalInput.IsKeyDown(GetKey(s?.RotateRightKey, InputKey.PageDown))) _bearing -= rotSpeed * dt;

                // Campaign-map style mouse controls, but ONLY outside combat (in battle/arena
                // the right mouse button is block/aim and must stay untouched). NOTE: town
                // missions leave Mission.CombatType at its default (Combat), so the reliable
                // discriminator is Mission.Mode: walkabout scenes run StartUp/Stealth while
                // real fights run Battle/Duel/Tournament/Deployment.
                if (s != null && IsNonCombatScene())
                {
                    if (GlobalInput.IsKeyDown(InputKey.RightMouseButton))
                    {
                        _bearing -= GlobalInput.MouseMoveX * MouseOrbitSensitivity;
                        s.CameraAngle = MBMath.ClampFloat(
                            s.CameraAngle + GlobalInput.MouseMoveY * MouseTiltSensitivity, 15f, 85f);
                    }
                    float wheel = GlobalInput.DeltaMouseScroll;
                    if (wheel != 0f)
                    {
                        // DeltaMouseScroll reports ~±120 per notch (GetMouseDeltaZ).
                        s.CameraDistance = MBMath.ClampFloat(
                            s.CameraDistance - wheel / 120f * ZoomStepMeters, 3f, 45f);
                    }
                }

                _bearing = MBMath.WrapAngle(_bearing);

                UpdateIsoCameraFrame(main, s);

                // Sync the game's camera-direction scalars: bearing = our orbit angle, so WASD
                // stays camera-relative (and mounted steering keeps working); elevation is
                // pinned horizontal so the character doesn't aim at the ground.
                MissionScreen.CameraBearing = _bearing;
                CameraElevationSetter?.Invoke(MissionScreen, HorizontalElevation);

                ApplyRpgMovement(main, s);

                if (_arrowVm != null)
                {
                    _arrowVm.IsArrowVisible = true;
                    // Screen-relative aim: 0 = up the screen (facing == camera bearing).
                    // UI rotation is clockwise-positive in degrees.
                    _arrowVm.ArrowRotation = -MBMath.WrapAngle(_facingYaw - _bearing) * (180f / MathF.PI);
                }
            }
            catch (Exception e)
            {
                Log("OnMissionScreenTick error, disabling iso cam: " + e.Message);
                SafeDisable();
            }
        }

        /// <summary>Positions the iso camera above/behind the player and aims it at them.</summary>
        private void UpdateIsoCameraFrame(Agent main, Settings s)
        {
            float distance = s?.CameraDistance ?? 18f;
            float extraHeight = s?.CameraHeight ?? 0f;
            float pitch = (s?.CameraAngle ?? 65f) * (MathF.PI / 180f);

            // Decompose the straight-line distance into horizontal reach + height from the tilt.
            float horiz = distance * MathF.Cos(pitch);
            float vert = distance * MathF.Sin(pitch) + extraHeight;

            // Look at roughly the player's torso, raised by the extra-height slider.
            Vec3 target = main.VisualPosition;
            target.z += 1.0f + extraHeight;

            // Camera sits "behind" the player along the current bearing.
            Vec3 camPos = target;
            camPos.x += MathF.Sin(_bearing) * horiz;
            camPos.y += -MathF.Cos(_bearing) * horiz;
            camPos.z += vert;

            // Inherit sane near/far/aspect from the combat camera, then aim.
            Camera combat = MissionScreen.CombatCamera;
            if ((NativeObject)(object)combat != (NativeObject)null)
            {
                _isoCamera.FillParametersFrom(combat);
                float aspect = combat.GetAspectRatio();
                float fov = (s?.FieldOfView ?? 40f) * (MathF.PI / 180f);
                _isoCamera.SetFovVertical(fov, aspect, 0.10f, 8000f);
            }

            _isoCamera.LookAt(camPos, target, new Vec3(0f, 0f, 1f, -1f));
        }

        /// <summary>
        /// RPG-style facing and movement. Runs AFTER MissionMainAgentController's tick (our
        /// view is registered later), so we can re-map what it wrote this frame:
        ///  - moving: face the world direction the player is steering toward on screen, and
        ///    feed the engine a pure-forward input (movement is interpreted relative to look);
        ///  - idle: leave CustomLookDir at the last value -> camera orbits around a character
        ///    that holds their facing (side/front views possible).
        /// Mounted agents are skipped: native horse steering is already camera-relative via
        /// the CameraBearing sync and must keep receiving raw x-axis input to turn.
        /// </summary>
        private void ApplyRpgMovement(Agent main, Settings s)
        {
            if (_mainAgentController == null) return;
            if (main.HasMount) return;

            bool mouseSteer = s?.MouseTurnsCharacter ?? true;

            Vec2 input = main.MovementInputVector;
            float lenSq = input.x * input.x + input.y * input.y;

            // Screen axes in world space for the current orbit angle.
            Vec2 forward = new Vec2(-MathF.Sin(_bearing), MathF.Cos(_bearing));
            Vec2 right = new Vec2(forward.y, -forward.x);

            if (mouseSteer)
            {
                // First-person style: mouse X spins the character in place — except while the
                // mouse is busy orbiting the camera (RMB drag outside combat).
                bool cameraDrag = IsNonCombatScene() &&
                                  GlobalInput.IsKeyDown(InputKey.RightMouseButton);
                if (!cameraDrag)
                {
                    _facingYaw = MBMath.WrapAngle(_facingYaw - GlobalInput.MouseMoveX * FacingSensitivity);
                }

                Vec2 face = new Vec2(-MathF.Sin(_facingYaw), MathF.Cos(_facingYaw));
                _mainAgentController.CustomLookDir = new Vec3(face.x, face.y, 0f, -1f);

                if (lenSq >= 1e-6f)
                {
                    float len = MathF.Min(1f, MathF.Sqrt(lenSq));
                    Vec2 world = new Vec2(
                        right.x * input.x + forward.x * input.y,
                        right.y * input.x + forward.y * input.y);
                    float worldLen = MathF.Sqrt(world.x * world.x + world.y * world.y);
                    if (worldLen >= 1e-6f)
                    {
                        world.x = world.x / worldLen * len;
                        world.y = world.y / worldLen * len;
                        // The engine interprets MovementInputVector relative to the look
                        // direction, so express the intended world direction in the facing
                        // frame -> the character strafes while the mouse holds the facing.
                        main.MovementInputVector = new Vec2(
                            face.y * world.x - face.x * world.y,   // dot(faceRight, world)
                            face.x * world.x + face.y * world.y);  // dot(faceForward, world)
                    }
                }
                return;
            }

            // Movement-facing mode: the character simply faces wherever they walk.
            if (lenSq < 1e-6f) return;

            float len2 = MathF.Min(1f, MathF.Sqrt(lenSq));
            Vec2 world2 = new Vec2(
                right.x * input.x + forward.x * input.y,
                right.y * input.x + forward.y * input.y);
            float worldLen2 = MathF.Sqrt(world2.x * world2.x + world2.y * world2.y);
            if (worldLen2 < 1e-6f) return;
            world2.x /= worldLen2;
            world2.y /= worldLen2;

            _facingYaw = MathF.Atan2(-world2.x, world2.y); // keep modes in sync when switching
            _mainAgentController.CustomLookDir = new Vec3(world2.x, world2.y, 0f, -1f);
            main.MovementInputVector = new Vec2(0f, len2);
        }

        /// <summary>Walkabout-like scene where the mouse is free for camera control.</summary>
        private bool IsNonCombatScene()
        {
            if (Mission == null) return false;
            MissionMode mode = Mission.Mode;
            return mode == MissionMode.StartUp || mode == MissionMode.Stealth;
        }

        /// <summary>Returns look control to the vanilla camera (mouse-look works again).</summary>
        private void ReleaseLookDirection()
        {
            try
            {
                if (_mainAgentController != null)
                {
                    _mainAgentController.CustomLookDir = Vec3.Zero;
                }
            }
            catch (Exception e) { Log("ReleaseLookDirection error: " + e.Message); }
        }

        // ---- state hooks so we never trap the player -------------------------

        public override void OnConversationBegin()
        {
            base.OnConversationBegin();
            _suspendedByState = true;
            ClearCustomCamera();
            ReleaseLookDirection();
        }

        public override void OnConversationEnd()
        {
            base.OnConversationEnd();
            _suspendedByState = false;
            // Ownership is re-asserted next tick if still enabled.
        }

        public override void OnPhotoModeActivated()
        {
            base.OnPhotoModeActivated();
            _suspendedByState = true;
            ClearCustomCamera();
            ReleaseLookDirection();
        }

        public override void OnPhotoModeDeactivated()
        {
            base.OnPhotoModeDeactivated();
            _suspendedByState = false;
        }

        // ---- enable / disable ------------------------------------------------

        private void EnableIso()
        {
            try
            {
                if (MissionScreen == null) { Log("EnableIso: no MissionScreen."); return; }
                if (Agent.Main == null) { Notify("Iso camera needs a controllable character here."); return; }

                if (_isoCamera == null) _isoCamera = Camera.CreateCamera();
                if (_mainAgentController == null)
                    _mainAgentController = Mission.GetMissionBehavior<MissionMainAgentController>();

                // Start mouse-look from the character's current facing so nothing snaps.
                _facingYaw = Agent.Main.LookDirectionAsAngle;

                if (_uiLayer == null)
                {
                    _arrowVm = new FacingArrowVM();
                    _uiLayer = new GauntletLayer("RFIsoCamHUD", -1);
                    MissionScreen.AddLayer(_uiLayer);
                    _uiLayer.LoadMovie("RFIsoCamFacingArrow", _arrowVm);
                }

                MissionScreen.CustomCamera = _isoCamera;
                MissionScreen.AllowInputWithCustomCamera = true;
                _isoEnabled = true;
                Notify("Isometric camera: ON");
                Log("Isometric camera ENABLED.");
            }
            catch (Exception e)
            {
                Log("EnableIso error: " + e.Message);
                SafeDisable();
            }
        }

        private void DisableIso(string reason)
        {
            _isoEnabled = false;
            ClearCustomCamera();
            ReleaseLookDirection();
            if (_arrowVm != null) _arrowVm.IsArrowVisible = false;
            Notify("Isometric camera: OFF");
            Log("Isometric camera DISABLED (" + reason + ").");
        }

        /// <summary>Hands the camera back to the game without changing the toggle intent.</summary>
        private void ClearCustomCamera()
        {
            try
            {
                if (MissionScreen != null &&
                    (NativeObject)(object)MissionScreen.CustomCamera == (NativeObject)(object)_isoCamera)
                {
                    MissionScreen.CustomCamera = null;
                }
            }
            catch (Exception e) { Log("ClearCustomCamera error: " + e.Message); }
        }

        /// <summary>Last-resort: fully turn everything off, never rethrow.</summary>
        private void SafeDisable()
        {
            try { _isoEnabled = false; ClearCustomCamera(); }
            catch { /* swallow: camera must never crash the mission */ }
        }

        // ---- helpers ---------------------------------------------------------

        private static Settings SafeSettings()
        {
            try { return Settings.Instance; }
            catch { return null; }
        }

        private static InputKey GetKey(string name, InputKey fallback)
        {
            if (string.IsNullOrWhiteSpace(name)) return fallback;
            try { return (InputKey)Enum.Parse(typeof(InputKey), name.Trim(), true); }
            catch { return fallback; }
        }

        private static void Notify(string msg)
        {
            try { InformationManager.DisplayMessage(new InformationMessage(LogPrefix + msg)); }
            catch { /* ignore UI failures */ }
        }

        private static void Log(string msg)
        {
            try { Debug.Print(LogPrefix + msg); }
            catch { /* ignore */ }
        }
    }
}
