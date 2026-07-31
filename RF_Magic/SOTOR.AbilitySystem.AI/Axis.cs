using System;

namespace SOTOR.AbilitySystem.AI;

public class Axis
{
	private readonly float _min;

	private readonly float _max;

	private readonly float _range;

	private readonly Func<float, float> _outputFunction;

	private readonly Func<Target, float> _parameterFunction;

	private readonly Func<Target, bool> _activationFunction;

	public Axis(float minInput, float maxInput, Func<float, float> outputFunction, Func<Target, float> parameterFunction, Func<Target, bool> activationFunction = null)
	{
		_min = minInput;
		_max = maxInput;
		_range = maxInput - minInput;
		_outputFunction = outputFunction;
		_parameterFunction = parameterFunction;
		_activationFunction = activationFunction;
	}

	public float Evaluate(Target target)
	{
		float val = _parameterFunction(target);
		float num = Math.Max(_min, Math.Min(_max, val));
		float arg = ((_range > 0f) ? ((num - _min) / _range) : 0f);
		float val2 = _outputFunction(arg);
		// SEM piso: o zero de um eixo VETA a opcao, e isso e intencional.
		//
		// Cheguei a por um piso de 0.05 aqui para resolver "o mago nunca conjura".
		// Foi errado: o zero e o mecanismo que faz o mago ESPERAR O MOMENTO. O eixo
		// de buff, por exemplo, e `1-x` sobre a distancia dos inimigos ate o alvo,
		// normalizada em 0..20m — inimigo a mais de 20m zera o eixo e o buff nao sai.
		// Com o piso, os magos lancavam protecao no comeco da batalha, longe de tudo
		// (relatado in-game).
		//
		// A causa real de "nunca conjura" era outra: teto de eixo igual a zero quando
		// o poder total do time e 0, corrigido em PowerScale. Um eixo com TETO zero e
		// dado degenerado; um eixo com VALOR zero e uma decisao.
		return Math.Max(0f, Math.Min(1f, val2));
	}

	public bool IsActive(Target target)
	{
		if (_activationFunction != null)
		{
			return _activationFunction(target);
		}
		return true;
	}
}
