using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SOTOR.AbilitySystem.SFX;

public class SotorJawFloater : ScriptComponentBehavior
{
	public float MinAngle;

	public float MaxAngle = 20f;

	public float Frequency = 2f;

	private MatrixFrame _restFrame;

	private float _elapsed;

	private bool _initialized;

	protected override void OnInit()
	{
		base.OnInit();
		CaptureRest();
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		CaptureRest();
	}

	private void CaptureRest()
	{
		_restFrame = base.GameEntity.GetFrame();
		_initialized = true;
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected override void OnTick(float dt)
	{
		Animate(dt);
	}

	protected override void OnEditorTick(float dt)
	{
		Animate(dt);
	}

	private void Animate(float dt)
	{
		if (_initialized)
		{
			_elapsed += dt;
			float num = (1f - (float)Math.Cos(_elapsed * Frequency)) * 0.5f;
			float a = (MinAngle + (MaxAngle - MinAngle) * num) * (float)Math.PI / 180f;
			MatrixFrame frame = _restFrame;
			frame.rotation.RotateAboutSide(a);
			base.GameEntity.SetFrame(ref frame);
		}
	}
}
