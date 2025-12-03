// Artillery Core (14.09.2025)

IMyCockpit _cockpit;

List<IMyMotorSuspension> _wheelList = new List<IMyMotorSuspension>();
List<IMyMotorStator> _hingeList = new List<IMyMotorStator>();
List<IMyMotorStator> _hingeSubList = new List<IMyMotorStator>();

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    Init();
}

void Init()
{
    _cockpit = (IMyCockpit)GridTerminalSystem.GetBlockWithName("Tank Seat Higher (Driver)");

    GridTerminalSystem.GetBlocksOfType<IMyMotorSuspension>(_wheelList, (w) => w.DisplayNameText.Contains("Wheel"));
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(_hingeList, (h) => !h.DisplayNameText.Contains("Sub") && h.DisplayNameText.Contains("Hinge"));
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(_hingeSubList, (h) => h.DisplayNameText.Contains("Sub") && h.DisplayNameText.Contains("Hinge"));
}

public void Main(string argument, UpdateType updateSource)
{
    if (_cockpit.MoveIndicator.Z < 0) // Нажатие W
    {
        foreach (IMyMotorSuspension _wheel in _wheelList)
        {
            _wheel.PropulsionOverride = 0.5f;
        }
    }
    else if (_cockpit.MoveIndicator.Z > 0) // Нажатие S
    {
        foreach (IMyMotorSuspension _wheel in _wheelList)
        {
            _wheel.PropulsionOverride = -1f;
        }
    }
    else if (_cockpit.MoveIndicator.Z == 0)
    {
        foreach (IMyMotorSuspension _wheel in _wheelList)
        {
            _wheel.PropulsionOverride = 0f;
        }
    }

    if (_cockpit.MoveIndicator.X > 0)
    {
        foreach (IMyMotorSuspension _wheel in _wheelList)
        {
            _wheel.SteeringOverride = 1f;
        }
    }
    else if (_cockpit.MoveIndicator.X < 0)
    {
        foreach (IMyMotorSuspension _wheel in _wheelList)
        {
            _wheel.SteeringOverride = -1f;
        }
    }
    else if (_cockpit.MoveIndicator.X == 0)
    {
        foreach (IMyMotorSuspension _wheel in _wheelList)
        {
            _wheel.SteeringOverride = 0f;
        }
    }

    foreach (IMyMotorStator _hinge in _hingeList)
    {
        foreach (IMyMotorStator _hingeSub in _hingeSubList)
        {
            if (_hingeSub.DisplayNameText.Contains(_hinge.DisplayNameText))
            {
                _hingeSub.TargetVelocityRPM = 0f - (float)(_hinge.Angle * 180f / Math.PI) - (float)(_hingeSub.Angle * 180f / Math.PI);
                //if (ConvertToDegrees(_hingeSub.Angle) + ConvertToDegrees(_hinge.Angle) > 5) _hingeSub.TargetVelocityRPM *= -1;
            }
        }
    }
}

float ConvertToDegrees(float Angle)
{
    return (float)(Angle * 180 / Math.PI);
}