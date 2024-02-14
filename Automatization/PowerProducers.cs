List<IMyPowerProducer> PowerList = new List<IMyPowerProducer>();
List<IMyTextPanel> TextPanelList = new List<IMyTextPanel>();
List<EnergyValue> currentPowerOutputList = new List<EnergyValue>();

int POWER_INIT_COUNTER = 0;
int UPDATE_COUNTER = 0;
const int MAX_UPDATE_COUNTER = 100;

int UpdateTime = 1;
string LCDPrefix = "NEW_PREFIX";
Color MainColor, GridColor, BackgroundColor, MarksColor;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    if (Me.CustomData == string.Empty) SetDefaultData();
    Init();
}

void Init()
{
    try
    {
        GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(PowerList, (p) => p.IsWorking);
        GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(TextPanelList, (t) => t.DisplayNameText.Contains(LCDPrefix));
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    }
    catch { Echo("Init error"); }
}

public void Main(string argument)
{
    if (argument == "SetDefaultData") SetDefaultData();

    POWER_INIT_COUNTER++;
    UPDATE_COUNTER++;
    if (POWER_INIT_COUNTER == 100)
    {
        Init();
        POWER_INIT_COUNTER = 0;
    }

    //Init params
    int PrevUpdateTime = UpdateTime;
    string[] programData = Me.CustomData.Split('\n');
    LCDPrefix = programData[0].Split('=')[1];
    try { MainColor = ConvertStringToColor(programData[1].Split('=')[1]); } catch { MainColor = new Color(0, 255, 0); }
    try { GridColor = ConvertStringToColor(programData[2].Split('=')[1]); } catch { GridColor = new Color(0, 8, 0); }
    try { BackgroundColor = ConvertStringToColor(programData[3].Split('=')[1]); } catch { BackgroundColor = new Color(0, 0, 0); }
    try { MarksColor = ConvertStringToColor(programData[4].Split('=')[1]); } catch { MarksColor = new Color(255, 255, 0); }
    try { UpdateTime = int.Parse(programData[5].Split('=')[1]); } catch { UpdateTime = 1; }
    
    if (PrevUpdateTime != UpdateTime)
    {
        UPDATE_COUNTER = 0;
        currentPowerOutputList.Clear();
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
        }
    }
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
                    "LCDMainColor=0,255,0\n" +
                    "LCDGridColor=0,8,0\n" +
                    "LCDBackgroundColor=0,0,0\n" +
                    "LCDMarksColor=255,255,0\n"+
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