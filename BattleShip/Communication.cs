// Корабль - связь
IMyInteriorLight Light;
IMyRadioAntenna Antenna;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    
    Light = (IMyInteriorLight)GridTerminalSystem.GetBlockWithName("Landing Light");
    Antenna = (IMyRadioAntenna)GridTerminalSystem.GetBlockWithName("Antenna");

    if (Antenna.CustomData == string.Empty) SetDefaultData();
}

void Main(string argument)
{
    SendLandingCoords();
}

void SetDefaultData()
{
    Antenna.CustomData = "Имя: Корабль\nЧастота: 100.0";
}

string GetAntennaData(string Data)
{
    string[] AntennaData = Antenna.CustomData.Split('\n');
    switch (Data)
    {
        case "Name": return AntennaData[0].Split(':')[1];
        case "Frequency": return AntennaData[1].Split(':')[1];
    }
    return "Неизвестные данные";
}

void SendLandingCoords()
{
    IGC.SendBroadcastMessage(GetAntennaData("Frequency"), Light.GetPosition(), TransmissionDistance.TransmissionDistanceMax);
}