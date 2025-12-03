// MiG-21 Main Code
IMyCockpit _cockpit;
IMyThrust _thrust;
IMyMotorStator _cameraHinge;
IMyTextSurface _surface2;
//IMyTextPanel _buttonPanel;

List<IMyMotorStator> _aileronList = new List<IMyMotorStator>();
List<IMyMotorStator> _stabList = new List<IMyMotorStator>();

float _jetPercent = 0.001f;
int _counter = 0;
Vector2 _cursor = new Vector2(256f, 256f);

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    Init();
}

void Init()
{
    _cockpit = (IMyCockpit)GridTerminalSystem.GetBlockWithName("Pilot Seat");
    _thrust = (IMyThrust)GridTerminalSystem.GetBlockWithName("Jet Engine");
    _cameraHinge = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Hinge (FLIR)");
    _surface2 = ((IMyTextSurfaceProvider)GridTerminalSystem.GetBlockWithName("Button Panel")).GetSurface(1);

    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(_aileronList, (a) => a.DisplayNameText.Contains("Aileron"));
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(_stabList, (a) => a.DisplayNameText.Contains("Stabilator Wing"));
}

public void Main(string argument, UpdateType updateSource)
{
    _counter++;
    if (_counter == 100) _counter = 0;

    float _ad = _cockpit.MoveIndicator.X;
    float _ws = _cockpit.MoveIndicator.Z;
    float _spacec = _cockpit.MoveIndicator.Y;

    _cursor.X += _cockpit.RotationIndicator.Y * 0.1f;
    _cursor.Y += _cockpit.RotationIndicator.X * 0.1f;

    if (_ad > 0)
    {
        foreach (IMyMotorStator _aileron in _aileronList)
        {
            _aileron.TargetVelocityRPM = 5f;
        }
    }
    else if (_ad < 0)
    {
        foreach (IMyMotorStator _aileron in _aileronList)
        {
            _aileron.TargetVelocityRPM = -5f;
        }
    }
    else if (_ad == 0)
    {
        foreach (IMyMotorStator _aileron in _aileronList)
        {
            _aileron.TargetVelocityRPM = 0 - (float)(_aileron.Angle * 180 / Math.PI);
        }
    }

    if (_ws > 0)
    {
        float _mod = 1;
        foreach (IMyMotorStator _stab in _stabList)
        {
            if (_stab.DisplayNameText.Contains("Right")) _mod = -1;
            _stab.TargetVelocityRPM = _mod * -5f;
        }
    }
    else if (_ws < 0)
    {
        float _mod = 1;
        foreach (IMyMotorStator _stab in _stabList)
        {
            if (_stab.DisplayNameText.Contains("Right")) _mod = -1;
            _stab.TargetVelocityRPM = _mod * 5f;
        }
    }
    else if (_ws == 0)
    {
        foreach (IMyMotorStator _stab in _stabList)
        {
            _stab.TargetVelocityRPM = 0 - (float)(_stab.Angle * 180 / Math.PI);
        }
    }

    if (_spacec > 0)
    {
        _jetPercent += 0.01f;
    }
    else if (_spacec < 0)
    {
        if (_thrust.ThrustOverride > 0f) _jetPercent -= 0.01f;
        else _thrust.ThrustOverride = 0.001f;
    }
    _thrust.ThrustOverridePercentage = _jetPercent;

    if (_counter % 5 == 0 && Math.Abs((float)(_cameraHinge.Angle * 180 / Math.PI)) == 40f)
    {
        _cameraHinge.TargetVelocityRPM *= -1;
    }

    using (MySpriteDrawFrame _frame = _surface2.DrawFrame())
    {
        _frame.Add(DrawLine(new Vector2(256f, 512f), 800f, 1, Color.Green, 90 + (float)(_cameraHinge.Angle * 180 / Math.PI)));
        _frame.Add(new MySprite(SpriteType.TEXTURE, "Circle", _cursor, new Vector2(10f, 10f), Color.Red, "", TextAlignment.CENTER, 0f));
    }
}

MySprite DrawLine(Vector2 Point, float Length, float Width, Color Color, float Rotate)
{
    return new MySprite(SpriteType.TEXTURE, "SquareSimple", Point, new Vector2(Length, Width), Color, "", TextAlignment.CENTER, (float)(Rotate / 180f * Math.PI));
}