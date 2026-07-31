using TaleWorlds.Library;

namespace SOTOR.AbilitySystem;

public class SeekerController
{
	private readonly SotorTarget _target;

	private readonly SeekerParameters _parameters;

	private Vec3 _prevError;

	private bool _enabled = true;

	public SeekerController(SotorTarget target, SeekerParameters parameters)
	{
		_target = target;
		_parameters = parameters;
		_prevError = Vec3.Zero;
	}

	public MatrixFrame CalculateRotatedFrame(MatrixFrame globalFrame, float dt)
	{
		if (!_enabled || _target == null || !_target.IsValid)
		{
			return globalFrame;
		}
		Vec3 vec = globalFrame.origin + globalFrame.rotation.f.NormalizedCopy();
		Vec3 vec2 = _target.GetPosition() - vec;
		float length = vec2.Length;
		if (length < _parameters.DisableDistance)
		{
			_enabled = false;
			return globalFrame;
		}
		if (length < _parameters.MaxDistance && length > _parameters.MinDistance)
		{
			Vec3 vec3 = vec2 * _parameters.Proportional + (vec2 - _prevError) * _parameters.Derivative;
			globalFrame.rotation = Mat3.CreateMat3WithForward(vec + vec3 * dt - globalFrame.origin);
		}
		_prevError = vec2;
		return globalFrame;
	}
}
