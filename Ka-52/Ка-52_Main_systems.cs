//Ка-52 - Основные системы
/*ToDo:
1. Адаптивная индикация повреждений (-)
2. Адаптивная глобальная система (-)
3. !!! ДОДЕЛАТЬ РАБОТУ С ГЛОБАЛЬНЫМИ ДАННЫМИ !!! (-)
4. Сопровождение цели камерой (-)
5. Наведение вооружения (-)
*/

IMyTextSurface Surface1;
IMyCockpit PilotCockpit, CoPilotCockpit;
IMyRadioAntenna Antenna;
IMyCameraBlock Camera;
IMySoundBlock Sound;
IMyPowerProducer LeftEngine, RightEngine;
IMyThrust PropellerUp, PropellerLow;
IMyGyro Gyroscope;
IMyMotorStator ChainElevation, ChainAzimuth, CameraElevation, CameraAzimuth;
IMyMotorStator LeftDoor, RightDoor;
IMyTextPanel FighterLCD;

IMySmallMissileLauncher MissileLauncher1, MissileLauncher2;
IMySmallMissileLauncher Bomb1, Bomb2;
IMySmallMissileLauncher Missile1, Missile2;
IMySmallGatlingGun ChainGun;

List<IMyCockpit> CockpitList = new List<IMyCockpit>();
List<IMyGasTank> GasTankList = new List<IMyGasTank>();
List<IMyMotorStator> WheelHingeList = new List<IMyMotorStator>();
List<IMyThrust> PropellerList = new List<IMyThrust>();
List<IMyTerminalBlock> BlockList = new List<IMyTerminalBlock>();

Vector2 CenterScreen;
Vector3D SeaAltitude = new Vector3D(55131.6884012993, 28965.3612758834, -4376.76599661899);
Vector3D PlanetVector = new Vector3D(0.5, 0.5, 0.5);
bool Instructor;
bool Horizon;
bool SpeedDumpeners;
bool AimAssist;

const float CAMERA_SENSIVITY = 0.5f;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;

    Surface1 = Me.GetSurface(0);
    Antenna = (IMyRadioAntenna)GridTerminalSystem.GetBlockWithName("Helicopter Antenna");
    Camera = (IMyCameraBlock)GridTerminalSystem.GetBlockWithName("FLIR Camera System (IRNV)");
    Camera.EnableRaycast = true;
    Sound = (IMySoundBlock)GridTerminalSystem.GetBlockWithName("Sound Block");
    RightEngine = (IMyPowerProducer)GridTerminalSystem.GetBlockWithName("Jet Hydrogen Engine Right");
    LeftEngine = (IMyPowerProducer)GridTerminalSystem.GetBlockWithName("Jet Hydrogen Engine Left");
    PropellerUp = (IMyThrust)GridTerminalSystem.GetBlockWithName("Helicopter Propeller Upper");
    PropellerLow = (IMyThrust)GridTerminalSystem.GetBlockWithName("Helicopter Propeller Lower");
    Gyroscope = (IMyGyro)GridTerminalSystem.GetBlockWithName("Flight Stick (Gyro) Override");
    CameraElevation = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Hinge (FLIR)");
    CameraAzimuth = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Rotor (FLIR)");
    Camera = (IMyCameraBlock)GridTerminalSystem.GetBlockWithName("FLIR Camera System (IRNV)");
    Camera.EnableRaycast = true;

    MissileLauncher1 = (IMySmallMissileLauncher)GridTerminalSystem.GetBlockWithName("Hydra Rocket Pod 2");
    MissileLauncher2 = (IMySmallMissileLauncher)GridTerminalSystem.GetBlockWithName("Hydra Rocket Pod 3");
    Bomb1 = (IMySmallMissileLauncher)GridTerminalSystem.GetBlockWithName("1000lb Bomb Mount");
    Bomb2 = (IMySmallMissileLauncher)GridTerminalSystem.GetBlockWithName("1000lb Bomb Mount 2");
    Missile1 = (IMySmallMissileLauncher)GridTerminalSystem.GetBlockWithName("AIM-54 Mount");
    Missile2 = (IMySmallMissileLauncher)GridTerminalSystem.GetBlockWithName("AIM-54 Mount 2");
    ChainGun = (IMySmallGatlingGun)GridTerminalSystem.GetBlockWithName("30mm Chain Gun");

    LeftDoor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Left Door Hinge");
    RightDoor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Right Door Hinge");
    FighterLCD = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Fighter HUD LCD");

    GridTerminalSystem.GetBlocksOfType<IMyCockpit>(CockpitList);
    GridTerminalSystem.GetBlocksOfType<IMyGasTank>(GasTankList);
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(WheelHingeList, (w) => w.DisplayNameText.Contains("Wheel Rotor"));
    GridTerminalSystem.GetBlocksOfType<IMyThrust>(PropellerList, (p) => p.DisplayNameText.Contains("Propeller"));

    CenterScreen = new Vector2(Surface1.SurfaceSize.X / 2, Surface1.SurfaceSize.Y / 2);
    Instructor = false;
    Horizon = true;
    SpeedDumpeners = false;
    AimAssist = true;

    Init();
    if (Antenna.CustomData == string.Empty) SetAntennaData();
}

void Init()
{
    for (int i = 0; PilotCockpit == null; i++) { if (CockpitList[i].IsFunctional) PilotCockpit = CockpitList[i]; } // Поиск кокпита первого пилота
    for (int i = 0; CoPilotCockpit == null; i++) { if (CockpitList[i].IsFunctional && CockpitList[i] != PilotCockpit) CoPilotCockpit = CockpitList[i]; }
    List<IMyMotorStator> RotorList = new List<IMyMotorStator>(); // v--Поиск роторов пушки--v
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(RotorList, (r) => r.DisplayNameText.Contains("Chain Rotor"));
    for (int i = 0; i < RotorList.Count; i++) if (RotorList[i].DisplayNameText.Contains("Elevation")) ChainElevation = RotorList[i]; else ChainAzimuth = RotorList[i]; // ^--Поиск роторов пушки--^

    if (Me.CustomData == string.Empty) SetDefaultSystemData(); // Установка параметров на стандартные значения
}

void Main(string argument)
{ 
    if (argument == "SwitchInstructor") { Instructor = !Instructor; } // Переключение инструктора
    else if (argument == "SwitchHorizon") { Horizon = !Horizon; } // Переключение удержания горизонта
    else if (argument == "SwitchSpeedDumpeners") { SpeedDumpeners = !SpeedDumpeners; } // Переключение гасителя скорости
    
    if (!PilotCockpit.IsUnderControl)
    {
        if (argument == "OpenLeftDoor") { LeftDoor.TargetVelocityRPM = 5f; }
        else if (argument == "CloseLeftDoor") LeftDoor.TargetVelocityRPM = -5f;
    } else LeftDoor.TargetVelocityRPM = -5f;

    DrawInfo();
    GetSystemData();
    DrawHUD();

    try
    {
        if (Instructor)
        {
            float KeyWS = PilotCockpit.MoveIndicator.Z;
            float KeySpaceC = PilotCockpit.MoveIndicator.Y;

            if (GetAltitude() < 3) { foreach (IMyThrust Object_Propeller in PropellerList) Object_Propeller.ThrustOverride = (float)(PilotCockpit.CalculateShipMass().TotalMass * PilotCockpit.GetNaturalGravity().Length() / PropellerList.Count); }
            else { foreach (IMyThrust Object_Propeller in PropellerList) Object_Propeller.ThrustOverride = 0f; }
            
            if (Horizon) SetHorizon();
            if (SpeedDumpeners) Dumpeners();
        }
        else
        {
            Gyroscope.Yaw = 0f;
            Gyroscope.Pitch = 0f;
            Gyroscope.Roll = 0f;
            PropellerUp.ThrustOverride = 0f;
            PropellerLow.ThrustOverride = 0f;
        }

        if (!PilotCockpit.IsUnderControl && PropellerList[0].ThrustOverride != 0) Gyroscope.Enabled = false;
        else Gyroscope.Enabled = true;

        //Co-Pilot
        CameraElevation.TargetVelocityRPM = -CoPilotCockpit.RotationIndicator.X * CAMERA_SENSIVITY;
        CameraAzimuth.TargetVelocityRPM = -CoPilotCockpit.RotationIndicator.Y * CAMERA_SENSIVITY;
    }
    catch { Echo("Main() - Error"); }
}

void CameraLock(Vector3D TargetPosition) // Сделать слежение камерой
{
    Vector3D MyPosition = Camera.GetPosition();
}

void DrawInfo()
{
    Surface1.ContentType = ContentType.NONE; Surface1.ContentType = ContentType.SCRIPT;
    
    using (MySpriteDrawFrame Frame = Surface1.DrawFrame())
    {
        //v-----РАМКА-----v
        MySprite Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(470f, 450f), Color.Green, "", TextAlignment.CENTER, 0f);
        Frame.Add(Border);
        Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(460f, 440f), Color.Black, "", TextAlignment.CENTER, 0f);
        Frame.Add(Border);
        MySprite TitleBackground = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 40f), new Vector2(100, 30), Color.Black, "", TextAlignment.CENTER, 0f);
        Frame.Add(TitleBackground);
        MySprite Title = new MySprite(SpriteType.TEXT, "Ка-52", new Vector2(CenterScreen.X, 20f), null, Color.Green, "Debug", TextAlignment.CENTER, 1f);
        Frame.Add(Title);

        //v-----ИМЯ-----v
        MySprite HelicopterName = new MySprite(SpriteType.TEXT, "Позывной:", new Vector2(40f, 60f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(HelicopterName);
        HelicopterName = new MySprite(SpriteType.TEXT, GetAntennaData("Name"), new Vector2(470f, 60f), null, Color.Green, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(HelicopterName);
        
        //v-----ЧАСТОТА-----v
        MySprite Frequency = new MySprite(SpriteType.TEXT, "Частота вещания:", new Vector2(40f, 90f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(Frequency);
        Frequency = new MySprite(SpriteType.TEXT, GetAntennaData("Frequency"), new Vector2(470f, 90f), null, Color.Green, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(Frequency);

        //v-----СПИДОМЕТР-----v
        string Speed = Math.Round(PilotCockpit.GetShipSpeed() * 3.6).ToString();
        MySprite Velocity = new MySprite(SpriteType.TEXT, "Скорость:", new Vector2(40f, 120f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(Velocity);
        Velocity = new MySprite(SpriteType.TEXT, Speed + "км/ч", new Vector2(470f, 120f), null, Color.Green, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(Velocity);

        //v-----АЛЬТМЕТР-----v
        MySprite Altitude = new MySprite(SpriteType.TEXT, "Высота:", new Vector2(40f, 150f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(Altitude);
        Altitude = new MySprite(SpriteType.TEXT, GetAltitude().ToString() + "м", new Vector2(470f, 150f), null, Color.Green, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(Altitude);

        //v-----ТОПЛИВО-----v
        MySprite Hydrogen = new MySprite(SpriteType.TEXT, "Топливо:", new Vector2(40f, 180f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(Hydrogen);
        Hydrogen = new MySprite(SpriteType.TEXT, Math.Round(GetFuelPrecentage(), 1) + "%", new Vector2(470f, 180f), null, Color.Green, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(Hydrogen);

        //v-----ИНСТРУКТОР-----v
        MySprite InstructorSprite = new MySprite(SpriteType.TEXT, "Инструктор:", new Vector2(40f, 210f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(InstructorSprite);
        Color InstructorColor = Color.Maroon;
        string InstructorStatus; if (Instructor) { InstructorStatus = "Вкл."; InstructorColor = Color.Green; } else InstructorStatus = "Выкл.";
        InstructorSprite = new MySprite(SpriteType.TEXT, InstructorStatus, new Vector2(470f, 210f), null, InstructorColor, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(InstructorSprite);

        //v-----ИНДИКАЦИЯ ПОВРЕЖДЕНИЙ-----v
        MySprite Helicopter = new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(CenterScreen.X - 30, 350f), new Vector2(45f, 75f), Color.Green, "", TextAlignment.CENTER, 0f);
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(CenterScreen.X + 30, 350f), new Vector2(45f, 75f), Color.Green, "", TextAlignment.CENTER, 0f);
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 352.5f), new Vector2(60f, 70f), Color.Green, "", TextAlignment.CENTER, 0f);
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 390f), new Vector2(230f, 10f), Color.Green, "", TextAlignment.CENTER, 0f);
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(CenterScreen.X, 375f), new Vector2(220f, 20f), Color.Green, "", TextAlignment.CENTER, 0f);
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(CenterScreen.X - 25, 415f), new Vector2(30f, 45f), Color.Green, "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(CenterScreen.X + 25, 415f), new Vector2(30f, 45f), Color.Green, "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 407.5f), new Vector2(47.5f, 45f), Color.Green, "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 390f), new Vector2(20f, 20f), Color.Black, "", TextAlignment.CENTER, 0f);
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 310f), new Vector2(7.5f, 45f), Color.Green, "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);

        //v-----Винты-----v
        Helicopter = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 285f), new Vector2(300f, 5f), GetBlockEnabled(PropellerUp), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 300f), new Vector2(300f, 5f), GetBlockEnabled(PropellerLow), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);

        //v-----Двигатели-----v
        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X - 35, 330f), new Vector2(30f, 30f), Color.Black, "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X - 35, 330f), new Vector2(24f, 24f), GetBlockEnabled(LeftEngine), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X + 35, 330f), new Vector2(30f, 30f), Color.Black, "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X + 35, 330f), new Vector2(24f, 24f), GetBlockEnabled(RightEngine), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);

        //v-----Подвесное вооружение-----v
        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X - 60, 405f), new Vector2(20f, 20f), GetBlockEnabled(Missile1), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXT, GetInventoryCount(Missile1).ToString(), new Vector2((float)(CenterScreen.X - 62.5), 415f), null, GetBlockEnabled(Missile1), "Debug", TextAlignment.CENTER, 0.675f);
        Frame.Add(Helicopter);

        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X + 60, 405f), new Vector2(20f, 20f), GetBlockEnabled(Missile2), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXT, GetInventoryCount(Missile2).ToString(), new Vector2((float)(CenterScreen.X + 62.5), 415f), null,GetBlockEnabled(Missile2), "Debug", TextAlignment.CENTER, 0.675f);
        Frame.Add(Helicopter);

        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X - 80, 405f), new Vector2(20f, 20f), GetBlockEnabled(Bomb1), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXT, GetInventoryCount(Bomb1).ToString(), new Vector2((float)(CenterScreen.X - 82.5), 415f), null, GetBlockEnabled(Bomb1), "Debug", TextAlignment.CENTER, 0.675f);
        Frame.Add(Helicopter);

        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X + 80, 405f), new Vector2(20f, 20f), GetBlockEnabled(Bomb2), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXT, GetInventoryCount(Bomb2).ToString(), new Vector2((float)(CenterScreen.X + 82.5), 415f), null, GetBlockEnabled(Bomb2), "Debug", TextAlignment.CENTER, 0.675f);
        Frame.Add(Helicopter);

        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X - 100, 405f), new Vector2(20f, 20f), GetBlockEnabled(MissileLauncher1), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXT, GetInventoryCount(MissileLauncher1).ToString(), new Vector2((float)(CenterScreen.X - 102.5), 415f), null, GetBlockEnabled(MissileLauncher1), "Debug", TextAlignment.CENTER, 0.675f);
        Frame.Add(Helicopter);

        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X + 100, 405f), new Vector2(20f, 20f), GetBlockEnabled(MissileLauncher2), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXT, GetInventoryCount(MissileLauncher2).ToString(), new Vector2((float)(CenterScreen.X + 102.5), 415f), null, GetBlockEnabled(MissileLauncher2), "Debug", TextAlignment.CENTER, 0.675f);
        Frame.Add(Helicopter);

        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X + 35, 425f), new Vector2(25f, 40f), Color.Black, "", TextAlignment.CENTER, 0f);
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X + 30, 420f), new Vector2(10f, 10f), GetBlockEnabled(ChainGun), "", TextAlignment.CENTER, 0f);
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXT, GetInventoryCount(ChainGun).ToString(), new Vector2(CenterScreen.X, 410f), null, Color.Black, "Debug", TextAlignment.CENTER, 0.7f);
        Frame.Add(Helicopter);

        //v-----Шасси-----v
        if (WheelHingeList[0].TargetVelocityRPM > 0)
        {
            Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X, 450f), new Vector2(10f, 30f), GetBlockEnabled("Forward Wheel Rotor"), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
            Frame.Add(Helicopter);
            Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X - 30, 445f), new Vector2(10f, 25f), GetBlockEnabled("Left Wheel Rotor"), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
            Frame.Add(Helicopter);
            Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X + 30, 445f), new Vector2(10f, 25f), GetBlockEnabled("Right Wheel Rotor"), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
            Frame.Add(Helicopter);
        }
    }   
}

void SetHorizon() // Расчёт и установка грида относительно горизонта
{
    Vector3D GravityVector = PilotCockpit.GetNaturalGravity();
    Vector3D GravityNormalize = Vector3D.Normalize(GravityVector);

    double GravityForward = GravityNormalize.Dot(PilotCockpit.WorldMatrix.Forward);
    double GravityLeft = GravityNormalize.Dot(PilotCockpit.WorldMatrix.Left);
    double GravityUp = GravityNormalize.Dot(PilotCockpit.WorldMatrix.Up);

    float RollInput = (float)Math.Atan2(GravityLeft, -GravityUp);
    float PitchInput = -(float)Math.Atan2(GravityForward, -GravityUp);

    Gyroscope.Roll = RollInput;
    Gyroscope.Pitch = PitchInput;
}

float GetHorizon()
{
    Vector3D GravityVector = PilotCockpit.GetNaturalGravity();
    Vector3D GravityNormalize = Vector3D.Normalize(GravityVector);

    double GravityLeft = GravityNormalize.Dot(PilotCockpit.WorldMatrix.Left);
    double GravityUp = GravityNormalize.Dot(PilotCockpit.WorldMatrix.Up);

    return (float)Math.Atan2(GravityLeft, -GravityUp);
}

void Dumpeners() // Расчёт и наклон грида для погашения боковой скорости
{
    Vector3D SpeedVector = PilotCockpit.GetShipVelocities().LinearVelocity;
    Vector3D GravityNormalize = Vector3D.Normalize(PilotCockpit.GetNaturalGravity());
    Vector3D VProf = Vector3D.ProjectOnPlane(ref SpeedVector, ref GravityNormalize);
    IMyTextSurface Surface3 = Me.GetSurface(2);
    Surface3.WriteText("X: " + Math.Round(VProf.X, 5).ToString() + "\n" +
                        "Y: " + Math.Round(VProf.Y, 5).ToString() + "\n" +
                        "Z: " + Math.Round(VProf.Z, 5).ToString());

    float GyroMultiply = 0.1f;
    Gyroscope.Pitch = -(float)VProf.Dot(PilotCockpit.WorldMatrix.Forward) * GyroMultiply;
    Gyroscope.Roll = -(float)VProf.Dot(PilotCockpit.WorldMatrix.Right) * 0.01f;
}

void SetAntennaData() { Antenna.CustomData = "Имя: Пилот_Ка-52\nЧастота: 100.0"; } // Установка стандартные данных антенны (Требует переработку)

string GetAntennaData(string Data) // Получение данных антенны
{
    string[] AntennaData = Antenna.CustomData.Split('\n');
    switch (Data)
    {
        case "Name": return AntennaData[0].Split(':')[1];
        case "Frequency": return AntennaData[1].Split(':')[1];
    }
    return "Неизвестные данные";
}

double GetAltitude() // Возврат высоты относительно уровня моря
{
    double Vector = (PilotCockpit.GetPosition() - PlanetVector).Length() - (SeaAltitude - PlanetVector).Length();
    return Math.Round(Vector);
}

double GetFuelPrecentage() // Возврат процентного значения водорода в баках
{
    double CurrentFuel = 0f;
    foreach (IMyGasTank Object_HydrogenTank in GasTankList) { CurrentFuel += Object_HydrogenTank.FilledRatio * 100; }
    return CurrentFuel / GasTankList.Count;
}

void PlaySound(string SoundName) // Проигрователь звуков
{
    Sound.SelectedSound = SoundName;
    Sound.Play();
}

MyFixedPoint GetInventoryCount(IMyTerminalBlock Block) // Получение количества предметов в инвентаре блока
{
    if (Block != null)
    {
        IMyInventory Inventory = Block.GetInventory(0);
        List<MyInventoryItem> ItemList = new List<MyInventoryItem>();
        Inventory.GetItems(ItemList);
        MyFixedPoint ItemCount = 0;
        foreach (MyInventoryItem Object_Item in ItemList) ItemCount += Object_Item.Amount;
        return ItemCount;
    }
    return 0;
}

MySprite DrawLine(Vector2 Point1, Vector2 Point2, float Width, Color Color) // Отрисовка линии
{
    Vector2 CenterVector = (Point1 + Point2) / 2;
    return new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterVector, new Vector2((Point1 - Point2).Length(), Width), Color, "", TextAlignment.CENTER, (float)Math.Atan((Point2.Y - Point1.Y) / (Point2.X - Point1.X)));
}

MySprite DrawLine(Vector2 Point1, Vector2 Point2, float Width, Color Color, float Rotate) // Отрисовка линии
{
    Vector2 CenterVector = (Point1 + Point2) / 2;
    return new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterVector, new Vector2((Point1 - Point2).Length(), Width), Color, "", TextAlignment.CENTER, Rotate);
}

Color GetBlockEnabled(IMyTerminalBlock Block) // Получение цвета блока в индикации (по ссылке на блок)
{
    if (Block.IsWorking && Block != null) return Color.Green;
    else return Color.Maroon;
}

Color GetBlockEnabled(string BlockName) // Получение цвета блока в индикации (по названия блока)
{
    IMyTerminalBlock Block = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName(BlockName);
    if (Block.IsWorking && Block != null) return Color.Green;
    else return Color.Maroon;
}

void SetDefaultSystemData() // Установка стандартных значений
{
    string Data = "[-----=====|=====-----]" +
                    "\nИнструктор:" + ReturnOnOff(Instructor) +
                    "\nУдержание горизонта:" + ReturnOnOff(Horizon) +
                    "\nГаситель скорости:" + ReturnOnOff(SpeedDumpeners) +
                    "\nПомощь прицеливания:" + ReturnOnOff(AimAssist);
    Me.CustomData = Data;
}

void GetSystemData()
{
    string[] Data = Me.CustomData.Split('\n');
    Instructor = ReturnOnOff(Data[1].Split(':')[1]);
    Horizon = ReturnOnOff(Data[2].Split(':')[1]);
    SpeedDumpeners = ReturnOnOff(Data[3].Split(':')[1]);
    AimAssist = ReturnOnOff(Data[4].Split(':')[1]);
}

string ReturnOnOff(bool Param)
{
    if (Param) return "Вкл.";
    else return "Выкл.";
}

bool ReturnOnOff(string Param)
{
    if (Param == "Вкл.") return true;
    else return false;
}

// MySprite(SpriteType.TEXTURE, "SquareSimple", CenterVector, new Vector2((Point1 - Point2).Length(), Width), Color, "", TextAlignment.CENTER, Rotate);

void DrawHUD()
{
    FighterLCD.ContentType = ContentType.NONE; FighterLCD.ContentType = ContentType.SCRIPT;
    FighterLCD.ScriptBackgroundColor = Color.Black;
    using (MySpriteDrawFrame Frame = FighterLCD.DrawFrame())
    {
        Frame.Add(DrawLine(new Vector2(50f, 256f), new Vector2(462f, 256f), 2f, Color.Green, GetHorizon()));
        MySprite BlackRectangle = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 256f), new Vector2(40f, 10f), Color.Black, "", TextAlignment.CENTER, 0f);
        Frame.Add(BlackRectangle);
    }
}