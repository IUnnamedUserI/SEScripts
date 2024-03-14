List<IMyPowerProducer> PowerList = new List<IMyPowerProducer>();
List<IMyTextPanel> TextPanelList = new List<IMyTextPanel>();
List<EnergyValue> currentPowerOutputList = new List<EnergyValue>();
List<Button> buttonList = new List<Button>();

IMyCockpit ControlCockpit;
Cursor cursor;
Form AdditionalForm;

int POWER_INIT_COUNTER = 0;
int UPDATE_COUNTER = 0;
const int MAX_UPDATE_COUNTER = 100;
const float CURSOR_SENSIVITY = 0.5f;
const string PB_NAME = "[UU's ECA] Program";

int UpdateTime = 1;
string LCDPrefix = "NEW_PREFIX";
string CockpitPrefix = "COCKPIT_PREFIX";
Color MainColor = new Color(0, 0, 0),
        GridColor = new Color(0, 0, 0),
        BackgroundColor = new Color(0, 0, 0),
        MarksColor = new Color(0, 0, 0),
        CursorColor = new Color(0, 0, 0);
Vector2 CursorPosition = new Vector2(256, 256);

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    Me.CustomName = PB_NAME;
    if (Me.CustomData == string.Empty) SetDefaultData();

    Init();
    cursor = new Cursor(Color.Red, new Vector2(10f, 10f));
    
    buttonList.Add(new Button(new Vector2(474.5f, 15f), new Vector2(75f, 30f), BackgroundColor, MainColor, "Details", 0.75f, Color.White, "ShowDetails"));
}

void Init()
{
    try
    {
        GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(PowerList, (p) => p.IsWorking);
        GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(TextPanelList, (t) => t.DisplayNameText.Contains(LCDPrefix));
        
        int PrevUpdateTime = UpdateTime;
        string[] programData = Me.CustomData.Split('\n');
        LCDPrefix = programData[0].Split('=')[1];
        CockpitPrefix = programData[1].Split('=')[1];

        try { MainColor = ConvertStringToColor(programData[2].Split('=')[1]); } catch { MainColor = new Color(0, 255, 0); }
        try { GridColor = ConvertStringToColor(programData[3].Split('=')[1]); } catch { GridColor = new Color(0, 8, 0); }
        try { BackgroundColor = ConvertStringToColor(programData[4].Split('=')[1]); } catch { BackgroundColor = new Color(0, 0, 0); }
        try { MarksColor = ConvertStringToColor(programData[5].Split('=')[1]); } catch { MarksColor = new Color(255, 255, 0); }
        try { CursorColor = ConvertStringToColor(programData[6].Split('=')[1]); } catch { CursorColor = new Color(255, 0, 0); }
        try { UpdateTime = int.Parse(programData[7].Split('=')[1]); } catch { UpdateTime = 1; }

        if (PrevUpdateTime != UpdateTime)
        {
            UPDATE_COUNTER = 0;
            currentPowerOutputList.Clear();
        }
    }
    catch { Echo("Init error"); }
    
    try{
        List<IMyCockpit> tempCockpitList = new List<IMyCockpit>();
        GridTerminalSystem.GetBlocksOfType<IMyCockpit>(tempCockpitList, (c) => c.DisplayNameText.Contains(CockpitPrefix));
        if (tempCockpitList.Count > 0) ControlCockpit = tempCockpitList[0];
        else ControlCockpit = null;
    }
}

void UpdateElementsColor()
{
    if (buttonList.Count > 0)
        buttonList[0].ChangeColor(BackgroundColor, MainColor, Color.White);
    if (AdditionalForm != null) AdditionalForm.ChangeColors(MainColor, BackgroundColor);
}

public void Main(string argument)
{
    if (argument == "SetDefaultData") SetDefaultData();
    UpdateElementsColor();

    POWER_INIT_COUNTER++;
    UPDATE_COUNTER++;
    if (POWER_INIT_COUNTER == 100)
    {
        Init();
        POWER_INIT_COUNTER = 0;
    }

    foreach (IMyTextPanel textPanel in TextPanelList)
    {
        textPanel.ContentType = ContentType.NONE;
        textPanel.ContentType = ContentType.SCRIPT;
        textPanel.ScriptBackgroundColor = BackgroundColor;
    }

    EnergyValue maxOutput = new EnergyValue();
    foreach (IMyPowerProducer powerProducer in PowerList) maxOutput.Value += powerProducer.MaxOutput;
    if (currentPowerOutputList.Count > 0)
        foreach (EnergyValue energyValue in currentPowerOutputList)
            if (ConvertValue(ConvertValue(energyValue), "kW").Value > ConvertValue(ConvertValue(maxOutput), "kW").Value) maxOutput = ConvertValue(energyValue);
    maxOutput = ConvertValue(maxOutput);

    EnergyValue currentOutput = new EnergyValue();
    foreach (IMyPowerProducer powerProducer in PowerList) currentOutput.Value += powerProducer.CurrentOutput;
    currentOutput = ConvertValue(currentOutput);

    if (UPDATE_COUNTER == MAX_UPDATE_COUNTER * UpdateTime)
    {
        currentPowerOutputList = AddValue(currentPowerOutputList, currentOutput);
        if (currentPowerOutputList.Count > 22) currentPowerOutputList.RemoveAt(22);

        UPDATE_COUNTER = 0;
    }

    foreach (IMyTextPanel textPanel in TextPanelList)
    {
        float ScreenResolutionX = (float)(textPanel.TextureSize.X / 512);
        using (MySpriteDrawFrame Frame = textPanel.DrawFrame())
        {
            double value = 0;
            for (int i = 10; i >= 0; i--) // Vertical values
            {
                float height = 15f + i * 42f;
                MySprite MaxOutput = new MySprite(SpriteType.TEXT, value.ToString(), new Vector2(35f, height), null, MainColor, "Debug", TextAlignment.RIGHT, 0.5f);
                Frame.Add(MaxOutput);
                value += Math.Round(maxOutput.Value / 10, 1);
            }

            for (int w = 0; w < 22; w++) // Background grid
                for (int h = 0; h < 11; h++)
                {
                    MySprite Square = new MySprite(SpriteType.TEXTURE, "SquareHollow", new Vector2(40f + w * 22.5f * ScreenResolutionX, 430 - h * 45f), new Vector2(22.5f * ScreenResolutionX, 45), GridColor, "", TextAlignment.LEFT, 0f);
                    Frame.Add(Square);
                }

            MySprite MaxMagnitudeSprite = new MySprite(SpriteType.TEXT, maxOutput.Magnitude, new Vector2(20f, 5f), null, MainColor, "Debug", TextAlignment.LEFT, 0.4f);
            Frame.Add(MaxMagnitudeSprite);
            MySprite UpdateTimeSprite = new MySprite(SpriteType.TEXT, $"Update: {UpdateTime.ToString()} sec", new Vector2(256f * ScreenResolutionX, 470f), null, MainColor, "Debug", TextAlignment.CENTER, 0.4f);
            Frame.Add(UpdateTimeSprite);

            MySprite VerticalLine = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(40f, 230f), new Vector2(1f, 445f), MainColor, "", TextAlignment.CENTER, 0f);
            Frame.Add(VerticalLine);
            MySprite HorizontalLine = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(40f, 451.75f), new Vector2(470f * ScreenResolutionX, 1f), MainColor, "", TextAlignment.LEFT, 0f);
            Frame.Add(HorizontalLine);

            for (int i = 0; i < 21 * UpdateTime; i++) // Horizontal values
            {
                int timeValue = i * UpdateTime;
                float width = 40f + i * 22.5f * ScreenResolutionX;
                MySprite CurrentOutput = new MySprite(SpriteType.TEXT, timeValue.ToString(), new Vector2(width, 453f), null, MainColor, "Debug", TextAlignment.CENTER, 0.5f);
                Frame.Add(CurrentOutput);
            }

            int plankCount = 0;

            foreach (EnergyValue energyValue in currentPowerOutputList) // Energy marks
            {
                float width = 40f + plankCount * 22.5f * ScreenResolutionX;
                float height = (float)(450 - 435 * (Decemial(ConvertValue(new EnergyValue(energyValue.Value * 100, energyValue.Magnitude)), maxOutput)) / 100);
                MySprite Mark = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(width, height), new Vector2(1f, 1f), MarksColor, "", TextAlignment.CENTER, 0f);
                Frame.Add(Mark);
                if (plankCount > 0)
                {
                    int heightPrev = (int)Math.Round(450 - 435 * (Decemial(ConvertValue(new EnergyValue(currentPowerOutputList[plankCount - 1].Value * 100, currentPowerOutputList[plankCount - 1].Magnitude)), maxOutput)) / 100);
                    MySprite Line = DrawLine(new Vector2((int)Math.Round(width), (int)Math.Round(height)), new Vector2(40 + (plankCount - 1) * 22.5f * ScreenResolutionX, heightPrev), 1f, MarksColor);
                    Frame.Add(Line);
                }
                plankCount++;
            }

            // New functions
            if (ControlCockpit != null)
            {
                foreach (Button btn in buttonList) Frame.AddRange(btn.CreateButtonS());

                if (AdditionalForm != null && AdditionalForm.IsVisible)
                {
                    Frame.AddRange(AdditionalForm.CreateForm());
                    
                    foreach (Button btn in AdditionalForm.ButtonList)
                    {
                        if (!IsButtonListContainsTag(btn))
                        {
                            buttonList.Add(btn);
                        }
                        Frame.AddRange(btn.CreateButtonS());
                    }

                    string infoString = string.Empty;
                    List<IMyTerminalBlock> tempTerminalBlockList = new List<IMyTerminalBlock>();
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(tempTerminalBlockList);
                    foreach (IMyTerminalBlock block in tempTerminalBlockList)
                    {
                        string input = GetPowerInputS(block);
                        if (input != "-1") infoString += $"{block.DisplayNameText} -{input}\n";
                    }
                    
                    Label powerLabel = new Label(infoString, new Vector2(10f, 40f), 0.65f, MainColor);
                    AdditionalForm.ClearContent();
                    AdditionalForm.AddContent(powerLabel);
                }
                
                cursor.MoveCursor(ControlCockpit.RotationIndicator, buttonList);
                if (ControlCockpit.RollIndicator > 0)
                {
                    cursor.Click(true); // Press 'E'
                    cursor.IsBlocked = true;
                }
                else
                {
                    cursor.Click(false);
                    cursor.IsBlocked = false;
                }

                if (cursor.CursorClicked)
                {
                    cursor.Click(false);
                    // Get clicked button
                    Button tempButton = null;
                    foreach (Button btn in buttonList) if (cursor.IsCursorOnButton(btn)) { tempButton = btn; break; }
                    if (tempButton != null) onButtonClick(tempButton);
                }
                
                if (ControlCockpit != null && ControlCockpit.IsUnderControl)
                    Frame.Add(cursor.CreateCursor());
            }
        }
    }
}

string GetPowerInputS(IMyTerminalBlock block)
{
    string detailedInfo = block.DetailedInfo;
    string[] splitedDetailedInfo = detailedInfo.Split('\n');
    string test = string.Empty;

    foreach (string str in splitedDetailedInfo)
    {
        if ((str.Contains("Required Input") || str.Contains("Current Input")) && !str.Contains("Max"))
        {
            string[] split = str.Split(':');
            if (float.Parse(split[1].Split(' ')[1]) > 0) return split[1];
            
        }
    }
    return "-1";
}

void onButtonClick(Button btn)
{
    switch (btn.Tag)
    {
        case "ShowDetails":
            CreateAdditionalForm();
            AdditionalForm.IsVisible = true;
            break;
        case "CloseAdditionalForm":
            buttonList.Remove(btn);
            AdditionalForm = null;
            break;
    }
}

bool IsButtonListContainsTag(Button btn)
{
    foreach (Button _btn in buttonList) if (btn.Tag == _btn.Tag) return true;
    return false;
}

void CreateAdditionalForm()
{
    AdditionalForm = new Form("Power consumption", new Vector2(280f, 230f), new Vector2(350f, 350f), MainColor, BackgroundColor);
    AdditionalForm.AddButton(new Button(new Vector2(333f, 17f), new Vector2(30f, 30f), Color.Red, Color.Green, "X", 0.65f, Color.White, "CloseAdditionalForm"));
}

List<EnergyValue> AddValue(List<EnergyValue> OutputList, EnergyValue newEnergyValue)
{
    OutputList.Insert(0, newEnergyValue);
    return OutputList;
}

EnergyValue ConvertValue(EnergyValue energyValue)
{
    double Value = energyValue.Value;

    if (Value < 1 || Value >= 1000)
    {
        if (Value < 1)
        {
            energyValue.DownMagnitude();
            ConvertValue(energyValue);
        }
        if (Value >= 1000)
        {
            energyValue.UpMagnitude();
            ConvertValue(energyValue);
        }
    }
    return energyValue;
}

EnergyValue ConvertValue(EnergyValue energyValue, string Magnitude)
{
    string currentMagnitude = energyValue.Magnitude;
    
    energyValue = ConvertValue(energyValue);

    if (currentMagnitude == Magnitude) return energyValue;
    else if (currentMagnitude == "W")
    {
        if (Magnitude == "kW") energyValue.Value /= 1000;
        else if (Magnitude == "MW") energyValue.Value /= 1000000;
    }
    else if (currentMagnitude == "kW")
    {
        if (Magnitude == "W") energyValue.Value *= 1000;
        else if (Magnitude == "MW") energyValue.Value /= 1000;
    }
    else if (currentMagnitude == "MW")
    {
        if (Magnitude == "kW") energyValue.Value *= 1000;
        else if (Magnitude == "W") energyValue.Value *= 1000000;
    }
    energyValue.Magnitude = Magnitude;

    return energyValue;
}

double Decemial(EnergyValue energy1, EnergyValue energy2)
{
    if (energy1.Magnitude == "MW") energy1.DownMagnitude();
    else if (energy1.Magnitude == "W") energy1.UpMagnitude();
    if (energy2.Magnitude == "MW") energy2.DownMagnitude();
    else if (energy2.Magnitude == "W") energy2.UpMagnitude();
    return energy1.Value / energy2.Value;
}

MySprite DrawLine(Vector2 Point1, Vector2 Point2, float Width, Color Color)
{
    Vector2 CenterVector = (Point1 + Point2) / 2;
    return new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterVector, new Vector2((Point1 - Point2).Length(), Width), Color, "", TextAlignment.CENTER, (float)Math.Atan((Point2.Y - Point1.Y) / (Point2.X - Point1.X)));
}

void SetDefaultData()
{
    Me.CustomData = "LCDPrefix=<EnergyLCD>\n" +
                    "CockpitPrefix=<EnergyControl>\n" +
                    "LCDMainColor=0, 255, 0\n" +
                    "LCDGridColor=0, 8, 0\n" +
                    "LCDBackgroundColor=0, 0, 0\n" +
                    "LCDMarksColor=255, 255, 0\n"+
                    "CursorColor=255, 0, 0\n" +
                    "UpdateTime=1";
}

Color ConvertStringToColor(string StringFormatColor)
{
    string[] colors = StringFormatColor.Split(',');
    return new Color(int.Parse(colors[0]), int.Parse(colors[1]), int.Parse(colors[2]));
}

class EnergyValue
{
    public double Value;
    public string Magnitude;

    public EnergyValue(double Value, string Magnitude)
    {
        this.Value = Value;
        this.Magnitude = Magnitude;
    }

    public EnergyValue()
    {
        Value = 0;
        Magnitude = "MW";
    }

    public void DownMagnitude()
    {
        switch (Magnitude)
        {
            case "MW":
                Magnitude = "kW";
                Value *= 1000;
                break;
            
            case "kW":
                Magnitude = "W";
                Value *= 1000;
                break;

            default:
                Magnitude = "Error";
                Value = 0;
                break;
        }
    }

    public void UpMagnitude()
    {
        switch (Magnitude)
        {            
            case "W":
                Magnitude = "kW";
                Value /= 1000;
                break;

            case "kW":
                Magnitude = "MW";
                Value /= 1000;
                break;

            default:
                Magnitude = "Error";
                Value = 0;
                break;
        }
    }
}

class Cursor
{
    private Color CursorColor;
    private Vector2 CursorPosition;
    private Vector2 CursorSize;
    public bool CursorClicked;
    public bool IsBlocked;

    public Cursor(Color CursorColor, Vector2 CursorSize)
    {
        this.CursorColor = CursorColor;
        this.CursorPosition = new Vector2(256f, 256f);
        this.CursorSize = CursorSize;
        this.CursorClicked = false;
        this.IsBlocked = false;
    }

    public MySprite CreateCursor()
    {
        return new MySprite(SpriteType.TEXTURE, "CircleHollow", CursorPosition, CursorSize, CursorColor, "", TextAlignment.CENTER, 0f);
    }

    public void MoveCursor(Vector2 RotationIndicator, List<Button> btnList)
    {
        Vector2 CursorPrev = new Vector2(CursorPosition.X + RotationIndicator.Y * CURSOR_SENSIVITY,
                                        CursorPosition.Y + RotationIndicator.X * CURSOR_SENSIVITY);

        if (CursorPrev.X > 0 && CursorPrev.X < 512)
            CursorPosition.X += RotationIndicator.Y * CURSOR_SENSIVITY;
        if (CursorPrev.Y > 0 && CursorPrev.Y < 512)
            CursorPosition.Y += RotationIndicator.X * CURSOR_SENSIVITY;

        foreach (Button btn in btnList)
        {
            if (IsCursorOnButton(btn))
            {
                CursorColor = Color.Green;
                CursorSize = new Vector2(20f, 20f);
                btn.IsActive = true;
            }
            else
            {
                CursorColor = Color.Red;
                CursorSize = new Vector2(10f, 10f);
                btn.IsActive = false;
            }
        }
    }

    public bool IsCursorOnButton(Button btn)
    {
        if (CursorPosition.X > btn.ButtonPosition.X - (btn.ButtonSize.X / 2) &&
            CursorPosition.X < btn.ButtonPosition.X + (btn.ButtonSize.X / 2) &&
            CursorPosition.Y > btn.ButtonPosition.Y - (btn.ButtonSize.Y / 2) &&
            CursorPosition.Y < btn.ButtonPosition.Y + (btn.ButtonSize.Y / 2)) return true;
        else return false;
    }

    public void Click(bool IsClicked) { CursorClicked = IsClicked; }
}

class Button
{
    public Vector2 ButtonPosition;
    public Vector2 ButtonSize;
    public Color ButtonColor;
    public Color ButtonActiveColor;
    public bool IsActive;
    public string Tag;

    public string Text;
    private float FontSize;
    private Color TextColor;

    public Button(Vector2 ButtonPosition, Vector2 ButtonSize, Color ButtonColor, Color ButtonActiveColor, string Text, float FontSize, Color TextColor, string Tag)
    {
        this.ButtonPosition = ButtonPosition;
        this.ButtonSize = ButtonSize;
        this.ButtonColor = ButtonColor;
        this.ButtonActiveColor = ButtonActiveColor;
        this.IsActive = false;
        this.Text = Text;
        this.FontSize = FontSize;
        this.TextColor = TextColor;
        this.Tag = Tag;
    }

    public List<MySprite> CreateButtonS()
    {
        MySprite btnSprite;

        if (!IsActive)
            btnSprite = new MySprite(SpriteType.TEXTURE, "SquareSimple", ButtonPosition, ButtonSize, ButtonColor, "", TextAlignment.CENTER, 0f);
        else
            btnSprite = new MySprite(SpriteType.TEXTURE, "SquareSimple", ButtonPosition, ButtonSize, ButtonActiveColor, "", TextAlignment.CENTER, 0f);
        
        MySprite btnText = new MySprite(SpriteType.TEXT, Text, new Vector2(ButtonPosition.X, ButtonPosition.Y - ButtonSize.Y * (FontSize / 2)), null, TextColor, "Debug", TextAlignment.CENTER, FontSize);
        List<MySprite> list = new List<MySprite>();
        list.Add(btnSprite);
        list.Add(btnText);
        return list;
    }

    public void ChangeColor(Color _bgColor, Color _mainColor, Color _textColor)
    {
        if (ButtonColor != _bgColor) ButtonColor = _bgColor;
        if (ButtonActiveColor != _mainColor) ButtonActiveColor = _mainColor;
        if (TextColor != _textColor) TextColor = _textColor;
    }
}

class Label
{
    private string Text;
    public Vector2 Position;
    private float FontSize;
    private Color Color;

    public Label(string Text, Vector2 Position, float FontSize, Color Color)
    {
        this.Text = Text;
        this.Position = Position;
        this.FontSize = FontSize;
        this.Color = Color;
    }

    public MySprite GetSprite()
    {
        return new MySprite(SpriteType.TEXT, Text, Position, null, Color, "Debug", TextAlignment.LEFT, FontSize);
    }
}

class Form
{
    private Vector2 FormPosition;
    private Vector2 FormSize;
    private Color BorderColor;
    private Color BackgroundColor;
    string FormTitle;
    public List<Button> ButtonList = new List<Button>();
    List<MySprite> ContentSpriteList;
    public bool IsVisible;

    public Form(string FormTitle, Vector2 FormPosition, Vector2 FormSize, Color BorderColor, Color BackgroundColor)
    {
        this.FormTitle = FormTitle;
        this.FormPosition = FormPosition;
        this.FormSize = FormSize;
        this.BorderColor = BorderColor;
        this.BackgroundColor = BackgroundColor;
        this.ContentSpriteList = new List<MySprite>();
    }

    public List<MySprite> CreateForm()
    {
        List<MySprite> tempList = new List<MySprite>();
        tempList.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", FormPosition, FormSize, BorderColor, "", TextAlignment.CENTER, 0f));
        tempList.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", FormPosition, FormSize - new Vector2(2f, 2f), BackgroundColor, "", TextAlignment.CENTER, 0f));
        tempList.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(FormPosition.X, 35f + (FormPosition.Y - FormSize.Y / 2)), new Vector2(FormSize.X, 1), BorderColor, "", TextAlignment.CENTER, 0f));
        tempList.Add(new MySprite(SpriteType.TEXT, FormTitle, new Vector2(10f + (FormPosition.X - FormSize.X / 2), 5f + (FormPosition.Y - FormSize.Y / 2)), null, BorderColor, "Debug", TextAlignment.LEFT, 0.75f));

        if (ContentSpriteList != null && ContentSpriteList.Count > 0)
            foreach (MySprite sprite in ContentSpriteList) tempList.Add(sprite);
        
        return tempList;
    }

    public void AddContent(Label label)
    {
        Label tempLabel = label;
        tempLabel.Position = new Vector2(label.Position.X + (FormPosition.X - FormSize.X / 2),
                                        label.Position.Y + (FormPosition.Y - FormSize.Y / 2));
        MySprite tempSprite = tempLabel.GetSprite();
        ContentSpriteList.Add(tempSprite);
    }

    public void ClearContent() { ContentSpriteList.Clear(); }

    public void AddButton(Button btn)
    {
        Button tempButton = btn;
        tempButton.ButtonPosition = new Vector2(btn.ButtonPosition.X + (FormPosition.X - FormSize.X / 2),
                                            btn.ButtonPosition.Y + (FormPosition.Y - FormSize.Y / 2));
        ButtonList.Add(tempButton);
    }

    public void ChangeColors(Color _borderColor, Color _bgColor)
    {
        if (BorderColor != _borderColor) BorderColor = _borderColor;
        if (BackgroundColor != _bgColor) BackgroundColor = _bgColor;
    }
}