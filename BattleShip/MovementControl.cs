// Управление кораблём
IMyCockpit cockpit;

List<IMyMotorStator> PropellerRotorList = new List<IMyMotorStator>();
List<IMyThrust> ThrustBackwardList = new List<IMyThrust>();
List<IMyThrust> ThrustForwardList = new List<IMyThrust>();
List<IMyGyro> GyroList = new List<IMyGyro>();
List<TextButton> TextButtonList = new List<TextButton>();

//int SelectedButtonID = 1;
Vector2 FirstButtonPosition = new Vector2(25f, 70f);

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;

    cockpit = (IMyCockpit)GridTerminalSystem.GetBlockWithName("Control Seat");

    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(PropellerRotorList, (r) => r.DisplayNameText.Contains("(Propeller)"));
    GridTerminalSystem.GetBlocksOfType<IMyThrust>(ThrustBackwardList, (t) => t.DisplayNameText.Contains("Backward"));
    GridTerminalSystem.GetBlocksOfType<IMyThrust>(ThrustForwardList, (t) => t.DisplayNameText.Contains("Forward"));
    GridTerminalSystem.GetBlocksOfType<IMyGyro>(GyroList);

    TextButtonList.Add(new TextButton(0, "Стелс", FirstButtonPosition));
}

void Main(string argument)
{
    float _keyX = cockpit.MoveIndicator.X;
    foreach (IMyMotorStator Rotor in PropellerRotorList)
    {
        if (_keyX < 0)
        {
            Rotor.TargetVelocityRPM = -5f;
            Rotor.LowerLimitDeg = -50;
        }
        else if (_keyX > 0)
        {
            Rotor.TargetVelocityRPM = 5f;
            Rotor.UpperLimitDeg = 50;
        }
        else if (_keyX == 0)
        {
            if (Rotor.Angle > 0)
            {
                Rotor.TargetVelocityRPM = -5f;
                Rotor.LowerLimitDeg = 0;
            }
            else
            {
                Rotor.TargetVelocityRPM = 5f;
                Rotor.UpperLimitDeg = 0;
            }
        }
    }

    float _keyZ = cockpit.MoveIndicator.Z;
    if (_keyZ < 0)
    {
        if (ThrustBackwardList[0].CurrentThrustPercentage == 0)
            foreach (IMyThrust object_Thrust in ThrustForwardList) object_Thrust.ThrustOverridePercentage += 0.005f;
        else
        {
            if (ThrustBackwardList[0].CurrentThrustPercentage != 0f)
                foreach (IMyThrust object_Thrust in ThrustBackwardList) object_Thrust.ThrustOverridePercentage -= 0.005f;
            else foreach (IMyThrust object_Thrust in ThrustBackwardList) object_Thrust.ThrustOverride = 0.000001f;
        }
    }
    else if (_keyZ > 0)
    {
        if (ThrustForwardList[0].CurrentThrustPercentage == 0)
            foreach (IMyThrust object_Thrust in ThrustBackwardList) object_Thrust.ThrustOverridePercentage += 0.005f;
        else
        {
            if (ThrustForwardList[0].CurrentThrustPercentage != 0)
                foreach (IMyThrust object_Thrust in ThrustForwardList) object_Thrust.ThrustOverridePercentage -= 0.005f;
            else foreach (IMyThrust object_Thrust in ThrustForwardList) object_Thrust.ThrustOverride = 0.000001f;
        }
    }

    float _keyY = cockpit.MoveIndicator.Y;
    if (_keyY < 0)
    {
        foreach (IMyThrust object_Thrust in ThrustBackwardList) object_Thrust.ThrustOverride = 0.000001f;
        foreach (IMyThrust object_Thrust in ThrustForwardList) object_Thrust.ThrustOverride = 0.000001f;
    }

    PrintContextMenu();
}

void PrintContextMenu()
{
    IMyTextSurface contextSurface = cockpit.GetSurface(1);
    contextSurface.ContentType = ContentType.NONE; contextSurface.ContentType = ContentType.SCRIPT;

    Vector2 CenterScreen = new Vector2(contextSurface.SurfaceSize.X / 2, contextSurface.SurfaceSize.Y / 2);
    
    using (MySpriteDrawFrame Frame = contextSurface.DrawFrame())
    {
        MySprite Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 130f), new Vector2(230f, 150f), Color.Red, "", TextAlignment.CENTER, 0f);
        Frame.Add(Border);
        MySprite Background = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 130f), new Vector2(220f, 140f), Color.Black, "", TextAlignment.CENTER, 0f);
        Frame.Add(Background);
        MySprite TitleBackground = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 60f), new Vector2(80f, 10f), Color.Black, "", TextAlignment.CENTER, 0f);
        Frame.Add(TitleBackground);
        MySprite Title = new MySprite(SpriteType.TEXT, "М Е Н Ю", new Vector2(CenterScreen.X, 48f), null, Color.Red, "Debug", TextAlignment.CENTER, 0.6f);
        Frame.Add(Title);
        
        foreach (TextButton object_Button in TextButtonList)
        {
            object_Button.DrawButton();
        }
    }
}

class TextButton
{
    private Vector2 Position;
    private string Text;
    private int ID;
    private MySprite ButtonText, ButtonSelectedBackground;
    private List<MySprite> SpriteList = new List<MySprite>();

    public TextButton(int ID, string Text, Vector2 Position)
    {
        this.Text = Text;
        this.Position = Position;
        this.ID = ID;
        this.ButtonText = new MySprite(SpriteType.TEXT, Text, Position, null, Color.Red, "Debug", TextAlignment.LEFT, 0.5f);
        this.ButtonSelectedBackground = new MySprite(SpriteType.TEXTURE, "SquareSimple", Position, new Vector2(100f, 10f), Color.Black, "", TextAlignment.LEFT, 0f);
    }

    public List<MySprite> DrawButton()
    {
        SpriteList.Add(ButtonText);
        SpriteList.Add(ButtonSelectedBackground);
        return SpriteList;
    }

    public void ChangeSelectedButton(int SelectedID)
    {
        if (SelectedID == ID)
        {
            ButtonText = new MySprite(SpriteType.TEXT, Text, Position, null, Color.Black, "Debug", TextAlignment.LEFT, 0.5f);
            ButtonSelectedBackground = new MySprite(SpriteType.TEXTURE, "SquareSimple", Position, new Vector2(100f, 10f), Color.Red, "", TextAlignment.LEFT, 0f);
        }
        else
        {
            ButtonText = new MySprite(SpriteType.TEXT, Text, Position, null, Color.Red, "Debug", TextAlignment.LEFT, 0.5f);
            ButtonSelectedBackground = new MySprite(SpriteType.TEXTURE, "SquareSimple", Position, new Vector2(100f, 10f), Color.Black, "", TextAlignment.LEFT, 0f);
        }
    }
}