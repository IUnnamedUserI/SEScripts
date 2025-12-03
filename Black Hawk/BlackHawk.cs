IMyButtonPanel _pilotPanel;
IMyCockpit _pilotCockpit;
IMySoundBlock _soundBlock;

int _counter = 0;

bool _flagAltitudeWarning = false;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    BlockInit();
}

void Main(string argument)
{
    DrawPilotInterface();
    SoundBlockPlay();
    _counter = _counter == 200 ? 0 : _counter + 1;
    if (_counter == 0) _flagAltitudeWarning = false;
}

void BlockInit()
{
    _pilotPanel = (IMyButtonPanel)GridTerminalSystem.GetBlockWithName("Pilot Button Panel");
    _pilotCockpit = (IMyCockpit)GridTerminalSystem.GetBlockWithName("Pilot Cockpit");
    _soundBlock = (IMySoundBlock)GridTerminalSystem.GetBlockWithName("Sound Block");
}

void DrawPilotInterface()
{
    IMyTextSurface _pilotSurface1 = ((IMyTextSurfaceProvider)_pilotPanel).GetSurface(0);

    using (MySpriteDrawFrame Frame = _pilotSurface1.DrawFrame())
    {
        MySprite circle = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(256f, 256f), new Vector2(201f, 201f), Color.White, "", TextAlignment.CENTER, 0f);
        Frame.Add(circle);
        circle = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(256f, 256f), new Vector2(200f, 200f), Color.Black, "", TextAlignment.CENTER, 0f);
        Frame.Add(circle);
        circle = new MySprite(SpriteType.TEXTURE, "SemiCircle", new Vector2(256f, 256f), new Vector2(200f, 200f), new Color(0, 0, 100), "", TextAlignment.CENTER, GetHorizon());
        Frame.Add(circle);
        circle = new MySprite(SpriteType.TEXTURE, "SemiCircle", new Vector2(256f, 256f), new Vector2(200f, 200f), new Color(92, 44, 6, 100), "", TextAlignment.CENTER, 3.14f + GetHorizon());
        Frame.Add(circle);

        MySprite _horizonLine = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(206f, 256f), new Vector2(50f, 2f), Color.White, "", TextAlignment.CENTER, 0f);
        Frame.Add(_horizonLine);
        _horizonLine = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(306f, 256f), new Vector2(50f, 2f), Color.White, "", TextAlignment.CENTER, 0f);
        Frame.Add(_horizonLine);
        _horizonLine = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(231f, 266f), new Vector2(2f, 20f), Color.White, "", TextAlignment.CENTER, 0f);
        Frame.Add(_horizonLine);
        _horizonLine = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(281f, 266f), new Vector2(2f, 20f), Color.White, "", TextAlignment.CENTER, 0f);
        Frame.Add(_horizonLine);

        MySprite _verticalSpeedBackground = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(70f, 256f), new Vector2(32f, 152f), Color.White, "", TextAlignment.CENTER, 0f);
        Frame.Add(_verticalSpeedBackground);
        _verticalSpeedBackground = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(70f, 256f), new Vector2(30f, 150f), Color.Black, "", TextAlignment.CENTER, 0f);
        Frame.Add(_verticalSpeedBackground);
        _verticalSpeedBackground = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(70f, 293.5f), new Vector2(30f, 75f), Color.Red, "", TextAlignment.CENTER, 0f);
        Frame.Add(_verticalSpeedBackground);
        _verticalSpeedBackground = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(70f, 218.5f), new Vector2(30f, 75f), Color.Green, "", TextAlignment.CENTER, 0f);
        Frame.Add(_verticalSpeedBackground);
        _verticalSpeedBackground = new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(100f, Math.Min(Math.Max(256f + GetAltitudeSpeed(), 181f), 331f)), new Vector2(20f, 20f), Color.White, "", TextAlignment.CENTER, (float)(-90 * Math.PI / 180));
        Frame.Add(_verticalSpeedBackground);
        _verticalSpeedBackground = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(70f, Math.Min(Math.Max(256f + GetAltitudeSpeed(), 181f), 331f)), new Vector2(30f, 2f), Color.White, "", TextAlignment.CENTER, 0f);
        Frame.Add(_verticalSpeedBackground);
    }
}

float GetHorizon()
{
    Vector3D GravityVector = _pilotCockpit.GetNaturalGravity();
    Vector3D GravityNormalize = Vector3D.Normalize(GravityVector);

    double GravityLeft = GravityNormalize.Dot(_pilotCockpit.WorldMatrix.Left);
    double GravityUp = GravityNormalize.Dot(_pilotCockpit.WorldMatrix.Up);

    return (float)Math.Atan2(GravityLeft, -GravityUp);
}

float GetAltitudeSpeed()
{
    return (float)Math.Round(_pilotCockpit.GetShipVelocities().LinearVelocity.Z, 2);
}

bool CheckAltitudeSpeed()
{
    if (GetAltitudeSpeed() >= 20f) return true;
    else
    {
        _flagAltitudeWarning = false;
        return false;
    }
}

void SoundBlockPlay()
{
    if (CheckAltitudeSpeed() && !_flagAltitudeWarning)
    {
        _soundBlock.SelectedSound = "F-16 Altitude";
        _soundBlock.Play();
        _flagAltitudeWarning = true;
    }
}