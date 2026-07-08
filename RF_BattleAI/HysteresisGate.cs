namespace RF_BattleAI;

/// <summary>
/// Threshold check with hysteresis for tactic state machines. RemainingPowerRatio
/// fluctuates both ways during combat; a bare comparison flips the state every
/// tick while the value dances around the threshold. A gate opens at the
/// threshold but only closes again once the value has moved a full band past it,
/// so state transitions commit instead of flapping.
/// </summary>
public sealed class HysteresisGate
{
    private readonly float _enterThreshold;
    private readonly float _exitThreshold;
    private readonly bool _openWhenAbove;
    private bool _isOpen;

    private HysteresisGate(float enterThreshold, float exitThreshold, bool openWhenAbove)
    {
        _enterThreshold = enterThreshold;
        _exitThreshold = exitThreshold;
        _openWhenAbove = openWhenAbove;
    }

    /// <summary>Opens when the value reaches the threshold; closes only once it drops a band below it.</summary>
    public static HysteresisGate RisesAbove(float threshold, float band = 0.05f)
    {
        return new HysteresisGate(threshold, threshold - band, openWhenAbove: true);
    }

    /// <summary>Opens when the value falls to the threshold; closes only once it climbs a band above it.</summary>
    public static HysteresisGate FallsBelow(float threshold, float band = 0.05f)
    {
        return new HysteresisGate(threshold, threshold + band, openWhenAbove: false);
    }

    public bool Evaluate(float value)
    {
        if (_openWhenAbove)
        {
            if (_isOpen)
            {
                if (value < _exitThreshold)
                {
                    _isOpen = false;
                }
            }
            else if (value >= _enterThreshold)
            {
                _isOpen = true;
            }
        }
        else
        {
            if (_isOpen)
            {
                if (value > _exitThreshold)
                {
                    _isOpen = false;
                }
            }
            else if (value <= _enterThreshold)
            {
                _isOpen = true;
            }
        }

        return _isOpen;
    }
}
