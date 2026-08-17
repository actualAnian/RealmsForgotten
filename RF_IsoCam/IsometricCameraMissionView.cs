using System;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

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

        private Camera _isoCamera;
        private bool _isoEnabled;        // is the iso view currently driving the camera?
        private bool _suspendedByState;  // temporarily off due to conversation/photo mode
        private float _bearing;          // horizontal orbit angle around the player (radians)

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
                if (Input.IsKeyPressed(GetKey(s?.ToggleKey, InputKey.H)))
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
                    return;
                }

                // If a game-owned state (conversation / photo mode) suspended us, don't fight it.
                if (_suspendedByState)
                {
                    ClearCustomCamera();
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
                if (Input.IsKeyDown(GetKey(s?.RotateLeftKey, InputKey.Delete))) _bearing += rotSpeed * dt;
                if (Input.IsKeyDown(GetKey(s?.RotateRightKey, InputKey.PageDown))) _bearing -= rotSpeed * dt;
                _bearing = MBMath.WrapAngle(_bearing);

                UpdateIsoCameraFrame(main, s);
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
            float distance = s?.CameraDistance ?? 14f;
            float extraHeight = s?.CameraHeight ?? 6f;
            float pitch = (s?.CameraAngle ?? 55f) * (MathF.PI / 180f);

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
                _isoCamera.SetFovVertical(65f * (MathF.PI / 180f), aspect, 0.10f, 8000f);
            }

            _isoCamera.LookAt(camPos, target, new Vec3(0f, 0f, 1f, -1f));
        }

        // ---- state hooks so we never trap the player -------------------------

        public override void OnConversationBegin()
        {
            base.OnConversationBegin();
            _suspendedByState = true;
            ClearCustomCamera();
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
