IMyTextPanel Monitor;

List<IMyPowerProducer> PowerList = new List<IMyPowerProducer>();
List<EnergyValue> currentPowerOutputList = new List<EnergyValue>();

int POWER_INIT_COUNTER = 0;
int UPDATE_COUNTER = 0;
const int MAX_UPDATE_COUNTER = 100;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    Init();
}

void Init()
{
    try
    {
        Monitor = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Computer Monitor");
        GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(PowerList, (p) => p.IsWorking);
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    }
    catch { Echo("Init error"); }
}

public void Main(string argument)
{
    POWER_INIT_COUNTER++;
    UPDATE_COUNTER++;
    if (POWER_INIT_COUNTER == 100)
    {
        Init();
        POWER_INIT_COUNTER = 0;
    }
    if (Monitor == null)
    {
        Echo("Monitor not found");
        Runtime.UpdateFrequency = UpdateFrequency.None;
    }

    Monitor.ContentType = ContentType.NONE;
    Monitor.ContentType = ContentType.SCRIPT;
    Monitor.ScriptBackgroundColor = Color.Black;

    EnergyValue maxOutput = new EnergyValue();
    foreach (IMyPowerProducer powerProducer in PowerList) maxOutput.Value += powerProducer.MaxOutput;
    if (currentPowerOutputList.Count > 0)
        foreach (EnergyValue energyValue in currentPowerOutputList)
            if (ConvertValue(ConvertValue(energyValue), "kW").Value > ConvertValue(ConvertValue(maxOutput), "kW").Value) maxOutput = ConvertValue(energyValue);
    maxOutput = ConvertValue(maxOutput);

    EnergyValue currentOutput = new EnergyValue();
    foreach (IMyPowerProducer powerProducer in PowerList) currentOutput.Value += powerProducer.CurrentOutput;
    currentOutput = ConvertValue(currentOutput);

    if (UPDATE_COUNTER == MAX_UPDATE_COUNTER)
    {
        currentPowerOutputList = AddValue(currentPowerOutputList, currentOutput);
        if (currentPowerOutputList.Count > 22) currentPowerOutputList.RemoveAt(22);
        UPDATE_COUNTER = 0;
    }

    Vector2 MonitorSize = Monitor.TextureSize;

    using (MySpriteDrawFrame Frame = Monitor.DrawFrame())
    {
        double value = 0;
        for (int i = 10; i >= 0; i--)
        {
            float height = 15f + i * 42f;
            MySprite MaxOutput = new MySprite(SpriteType.TEXT, value.ToString(), new Vector2(35f, height), null, Color.Green, "Debug", TextAlignment.RIGHT, 0.5f);
            Frame.Add(MaxOutput);
            value += Math.Round(maxOutput.Value / 10, 1);
        }

        for (int w = 0; w < 22; w++)
            for (int h = 0; h < 11; h++)
            {
                MySprite Square = new MySprite(SpriteType.TEXTURE, "SquareHollow", new Vector2(50f + w * 22.5f, 430 - h * 45f), new Vector2(22.5f, 45), new Color(0, 8, 0), "", TextAlignment.CENTER, 0f);
                Frame.Add(Square);
            }

        MySprite MaxMagnitudeSprite = new MySprite(SpriteType.TEXT, maxOutput.Magnitude, new Vector2(20f, 5f), null, Color.Green, "Debug", TextAlignment.LEFT, 0.4f);
        Frame.Add(MaxMagnitudeSprite);
        
        MySprite VerticalLine = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(40f, 230f), new Vector2(1f, 445f), Color.Green, "", TextAlignment.CENTER, 0f);
        Frame.Add(VerticalLine);
        MySprite HorizontalLine = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(40f, 451.75f), new Vector2(470f, 1f), Color.Green, "", TextAlignment.LEFT, 0f);
        Frame.Add(HorizontalLine);

        for (int i = 0; i < 21; i++)
        {
            int timeValue = i;
            float width = 40f + i * 22f;
            MySprite CurrentOutput = new MySprite(SpriteType.TEXT, timeValue.ToString(), new Vector2(width, 453f), null, Color.Green, "Debug", TextAlignment.LEFT, 0.5f);
            Frame.Add(CurrentOutput);
        }

        int plankCount = 0;
        foreach (EnergyValue energyValue in currentPowerOutputList)
        {
            try
            {
                float width = 40f + plankCount * 22f;
                float height = (float)(450 - 435 * (Decemial(ConvertValue(new EnergyValue(energyValue.Value * 100, energyValue.Magnitude)), maxOutput)) / 100);
                MySprite Mark = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(width, height), new Vector2(1f, 1f), Color.Red, "", TextAlignment.CENTER, 0f);
                Frame.Add(Mark);
                if (plankCount > 0)
                {
                    int heightPrev = (int)Math.Round(450 - 435 * (Decemial(ConvertValue(new EnergyValue(currentPowerOutputList[plankCount - 1].Value * 100, currentPowerOutputList[plankCount - 1].Magnitude)), maxOutput)) / 100);
                    MySprite Line = DrawLine(new Vector2((int)Math.Round(width), (int)Math.Round(height)), new Vector2(40 + (plankCount - 1) * 22, heightPrev), 1f, Color.Yellow);
                    Frame.Add(Line);
                }
                plankCount++;
            }
            catch {}
        }

        //MySprite DebugSprite = new MySprite(SpriteType.TEXT, currentPowerOutputList.Count.ToString(), new Vector2(100f, 100f), null, Color.Red, "Debug", TextAlignment.CENTER, 0.5f);
        //Frame.Add(DebugSprite);
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