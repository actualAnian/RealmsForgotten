using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SOTOR.AbilitySystem.SFX;

public class SotorSpinner : ScriptComponentBehavior
{
	public float RotationSpeed = 100f;

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
	}

	private void Rotate(float dt)
	{
		WeakGameEntity gameEntity = base.GameEntity;
		if (!(gameEntity == null))
		{
			float a = RotationSpeed * 0.001f * dt;
			MatrixFrame frame = gameEntity.GetFrame();
			frame.rotation.RotateAboutUp(a);
			gameEntity.SetFrame(ref frame);
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.TickParallel | base.GetTickRequirement();
	}

	protected override void OnTickParallel(float dt)
	{
		Rotate(dt);
	}

	protected override void OnEditorTick(float dt)
	{
		Rotate(dt);
	}
}
