IMyTextPanel Monitor;

List<IMyPowerProducer> PowerList = new List<IMyPowerProducer>();
List<EnergyValue> currentPowerOutputList = new List<EnergyValue>();

int POWER_INIT_COUNTER = 0;

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
    }
    catch { Echo("Init error"); }
}

public void Main(string argument)
{
    POWER_INIT_COUNTER++;
    if (POWER_INIT_COUNTER == 100)
    {
        Init();
        POWER_INIT_COUNTER = 0;
    }

    Monitor.ContentType = ContentType.NONE;
    Monitor.ContentType = ContentType.SCRIPT;

    EnergyValue maxOutput = new EnergyValue();
    foreach (IMyPowerProducer powerProducer in PowerList) maxOutput.Value += powerProducer.MaxOutput;
    maxOutput = ConvertValue(maxOutput);

    EnergyValue currentOutput = new EnergyValue();
    foreach (IMyPowerProducer powerProducer in PowerList) currentOutput.Value += powerProducer.CurrentOutput;
    currentOutput = ConvertValue(currentOutput);
    currentPowerOutputList = AddValue(currentPowerOutputList, currentOutput);

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
        
        MySprite VerticalLine = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(40f, 230f), new Vector2(1f, 445f), Color.Green, "", TextAlignment.CENTER, 0f);
        Frame.Add(VerticalLine);
        MySprite HorizontalLine = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(40f, 451.75f), new Vector2(445f, 1f), Color.Green, "", TextAlignment.LEFT, 0f);
        Frame.Add(HorizontalLine);

        for (int i = 0; i < 21; i++)
        {
            int timeValue = i * 3;
            float width = 45f + i * 22f;
            MySprite CurrentOutput = new MySprite(SpriteType.TEXT, timeValue.ToString(), new Vector2(width, 453f), null, Color.Green, "Debug", TextAlignment.LEFT, 0.5f);
            Frame.Add(CurrentOutput);
        }

        for (int i = 0; i < 20; i++)
        {
            try
            {
                float width = 45f + i * 22f;
                float height = (float)(512 - 445 * Decemial(currentPowerOutputList[i], maxOutput));
                MySprite Mark = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(width, height), new Vector2(10f, 4f), Color.Yellow, "", TextAlignment.CENTER, 0f);
                Frame.Add(Mark);
            }
            catch {}
        }

        MySprite DebugSprite = new MySprite(SpriteType.TEXT, $"{Math.Round(currentOutput.Value).ToString()} {currentOutput.Magnitude}", new Vector2(100f, 100f), null, Color.Red, "Debug", TextAlignment.CENTER, 0.5f);
        Frame.Add(DebugSprite);
    }
}

List<EnergyValue> AddValue(List<EnergyValue> OutputList, EnergyValue newEnergyValue)
{
    if (OutputList.Count == 20)
        for (int i = 1; i < OutputList.Count; i++) OutputList[i - 1] = OutputList[i];
    OutputList.Add(newEnergyValue);
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

double Decemial(EnergyValue energy1, EnergyValue energy2)
{
    if (energy1.Magnitude == "MW") energy1.DownMagnitude();
    else if (energy1.Magnitude == "W") energy1.UpMagnitude();
    if (energy2.Magnitude == "MW") energy2.DownMagnitude();
    else if (energy2.Magnitude == "W") energy2.UpMagnitude();
    return energy1.Value / energy2.Value;
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