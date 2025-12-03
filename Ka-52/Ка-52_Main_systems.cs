// Ка-52 - Основные системы
// Правки 02.12.2025
/*ToDo:
[ ] Отображение горизонтальной скорости
[ ] Глобальная система регистрации повреждений
    [ ] Шасси
    [ ] Винты
    [ ] Двигатели

[ ] Перевод на кнопочные панели
[ ] Индикация точки прицеливания авиапушки на дисплее пилота
*/

IMyTextSurface Surface1, Surface3;
IMyCockpit PilotCockpit, CoPilotCockpit;
IMyRadioAntenna Antenna;
IMyCameraBlock Camera;
IMySoundBlock Sound;
IMyPowerProducer LeftEngine, RightEngine;
IMyThrust PropellerUp, PropellerLow;
IMyGyro Gyroscope;
IMyMotorStator ChainElevation, ChainAzimuth, CameraElevation, CameraAzimuth;
IMyMotorStator LeftCockpit, RightDoor, LeftDoor;
IMyTextPanel FighterLCD;

IMyConveyorSorter ChainGun;

List<IMyCockpit> CockpitList = new List<IMyCockpit>();
List<IMyGasTank> GasTankList = new List<IMyGasTank>();
List<IMyMotorStator> WheelHingeList = new List<IMyMotorStator>();
List<IMyThrust> PropellerList = new List<IMyThrust>();
List<IMyConveyorSorter> DecoyList = new List<IMyConveyorSorter>();
List<IMyConveyorSorter> AdditionalWeapon = new List<IMyConveyorSorter>();
List<IMyTerminalBlock> BlockList = new List<IMyTerminalBlock>();

Vector2 CenterScreen;
Vector3D _hudPosition;
Vector3D _targetPosition = new Vector3D(10577.64, 384.53, -59721.03);
Vector3D SeaAltitude = new Vector3D(55131.6884012993, 28965.3612758834, -4376.76599661899);
Vector3D PlanetVector = new Vector3D(0.5, 0.5, 0.5);
Vector3D EnemyVector = new Vector3D();
bool Instructor;
bool Horizon;
bool SpeedDumpeners;
bool AimAssist;
bool TargetLocked;

const float CAMERA_SENSIVITY = 0.5f;

float _triangleCounter = 0f;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;

    Surface1 = Me.GetSurface(0);
    Surface3 = Me.GetSurface(2);
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

    ChainGun = (IMyConveyorSorter)GridTerminalSystem.GetBlockWithName("30mm Chain Gun");

    LeftCockpit = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Left Cockpit Hinge");
    LeftDoor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Left Door Hinge");
    RightDoor = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("Right Door Hinge");
    FighterLCD = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Fighter HUD LCD");

    GridTerminalSystem.GetBlocksOfType<IMyCockpit>(CockpitList);
    GridTerminalSystem.GetBlocksOfType<IMyGasTank>(GasTankList);
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(WheelHingeList, (w) => w.DisplayNameText.Contains("Wheel Rotor"));
    GridTerminalSystem.GetBlocksOfType<IMyThrust>(PropellerList, (p) => p.DisplayNameText.Contains("Propeller"));
    GridTerminalSystem.GetBlocksOfType<IMyConveyorSorter>(DecoyList, (d) => d.DisplayNameText.Contains("Dispenser"));

    CenterScreen = new Vector2(Surface1.SurfaceSize.X / 2, Surface1.SurfaceSize.Y / 2);
    Instructor = false;
    Horizon = true;
    SpeedDumpeners = false;
    AimAssist = true;
    TargetLocked = false;

    Init();
    if (Antenna.CustomData == string.Empty) SetAntennaData();
}

void Init()
{
    for (int i = 0; PilotCockpit == null; i++) { if (CockpitList[i].IsFunctional) PilotCockpit = CockpitList[i]; } // Поиск кокпита первого пилота
    for (int i = 0; CoPilotCockpit == null; i++) { if (CockpitList[i].IsFunctional && CockpitList[i] != PilotCockpit) CoPilotCockpit = CockpitList[i]; }
    List<IMyMotorStator> RotorList = new List<IMyMotorStator>(); // v--Поиск роторов пушки--v
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(RotorList, (r) => r.DisplayNameText.Contains("Rotor (ChainGun)"));
    for (int i = 0; i < RotorList.Count; i++) if (RotorList[i].DisplayNameText.Contains("Elevation")) ChainElevation = RotorList[i]; else ChainAzimuth = RotorList[i]; // ^--Поиск роторов пушки--^

    if (Me.CustomData == string.Empty) SetDefaultSystemData(); // Установка параметров на стандартные значения

    _hudPosition = FighterLCD.GetPosition();
}

void ReInit() {
    GridTerminalSystem.GetBlocksOfType<IMyConveyorSorter>(AdditionalWeapon, (a) => a.Position.Y == 5 && a.Position.Z == -6);
}

void Main(string argument)
{
    ReInit();
    if (_triangleCounter == 360f) _triangleCounter = 0f;
    else _triangleCounter++;

    if (argument == "SwitchInstructor") { Instructor = !Instructor; } // Переключение инструктора
    else if (argument == "SwitchHorizon") { Horizon = !Horizon; } // Переключение удержания горизонта
    else if (argument == "SwitchSpeedDumpeners") { SpeedDumpeners = !SpeedDumpeners; } // Переключение гасителя скорости
    else if (argument == "TargetLock") { TargetLocked = true; EnemyVector = CameraLock(); }
    else if (argument == "UnlockCamera") { TargetLocked = false; ResetAngles(); }

    
    if (!PilotCockpit.IsUnderControl)
    {
        if (argument == "OpenLeftDoor") { LeftCockpit.TargetVelocityRPM = 5f; LeftDoor.TargetVelocityRPM = 10f; }
        else if (argument == "CloseLeftDoor") { LeftCockpit.TargetVelocityRPM = -5f; LeftDoor.TargetVelocityRPM = -10f; }
    }
    else {
        LeftCockpit.TargetVelocityRPM = -5f;
        LeftDoor.TargetVelocityRPM = -10f;
    }

    SetDefaultAnglesToChainGun();
    DrawInfo();
    GetSystemData();
    DrawHUD();
    if (TargetLocked) CameraTracking(EnemyVector);

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

        ChainElevation.TargetVelocityRPM = CoPilotCockpit.RotationIndicator.X * CAMERA_SENSIVITY;
        ChainAzimuth.TargetVelocityRPM = CoPilotCockpit.RotationIndicator.Y * CAMERA_SENSIVITY;
    }
    catch { Echo("Main() - Error"); }
}

void SetDefaultAnglesToChainGun()
{
    if (CoPilotCockpit.IsUnderControl) return;
    else
    {
        ChainAzimuth.TargetVelocityRPM = (float)(0 - ChainAzimuth.Angle * 180 / Math.PI);
        ChainElevation.TargetVelocityRPM = (float)(0 - ChainElevation.Angle * 180 / Math.PI);
    }
}

void DrawInfo()
{
    // Метод отрисовывает на дисплее графическую информацию о
    // состоянии вертолёта, а также некоторые важные данные.

    Surface1.ContentType = ContentType.NONE; Surface1.ContentType = ContentType.SCRIPT;
    Surface3.ContentType = ContentType.NONE; Surface3.ContentType = ContentType.SCRIPT;
    
    using (MySpriteDrawFrame Frame = Surface1.DrawFrame())
    {
        //v----------------------------- РАМКА -----------------------------v
        MySprite Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(470f, 450f), Color.Green, "", TextAlignment.CENTER, 0f);
        Frame.Add(Border);
        Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(460f, 440f), Color.Black, "", TextAlignment.CENTER, 0f);
        Frame.Add(Border);
        MySprite TitleBackground = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 40f), new Vector2(100, 30), Color.Black, "", TextAlignment.CENTER, 0f);
        Frame.Add(TitleBackground);
        MySprite Title = new MySprite(SpriteType.TEXT, "Ка-52", new Vector2(CenterScreen.X, 20f), null, Color.Green, "Debug", TextAlignment.CENTER, 1f);
        Frame.Add(Title);
        //^----------------------------- РАМКА -----------------------------^

        //v----------------------------- ИМЯ -----------------------------v
        MySprite HelicopterName = new MySprite(SpriteType.TEXT, "Позывной:", new Vector2(40f, 60f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(HelicopterName);
        HelicopterName = new MySprite(SpriteType.TEXT, GetAntennaData("Name"), new Vector2(470f, 60f), null, Color.Green, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(HelicopterName);
        //^----------------------------- ИМЯ -----------------------------^
        
        //v----------------------------- ЧАСТОТА -----------------------------v
        MySprite Frequency = new MySprite(SpriteType.TEXT, "Частота вещания:", new Vector2(40f, 90f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(Frequency);
        Frequency = new MySprite(SpriteType.TEXT, GetAntennaData("Frequency"), new Vector2(470f, 90f), null, Color.Green, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(Frequency);
        //^----------------------------- ЧАСТОТА -----------------------------^

        //v----------------------------- СПИДОМЕТР -----------------------------v
        string Speed = Math.Round(PilotCockpit.GetShipSpeed() * 3.6).ToString();
        MySprite Velocity = new MySprite(SpriteType.TEXT, "Скорость:", new Vector2(40f, 120f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(Velocity);
        Velocity = new MySprite(SpriteType.TEXT, Speed + "км/ч", new Vector2(470f, 120f), null, Color.Green, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(Velocity);
        //^----------------------------- СПИДОМЕТР -----------------------------^

        //v----------------------------- АЛЬТМЕТР -----------------------------v
        MySprite Altitude = new MySprite(SpriteType.TEXT, "Высота:", new Vector2(40f, 150f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(Altitude);
        Altitude = new MySprite(SpriteType.TEXT, GetAltitude().ToString() + "м", new Vector2(470f, 150f), null, Color.Green, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(Altitude);
        //^----------------------------- АЛЬТМЕТР -----------------------------^

        //v----------------------------- ТОПЛИВО -----------------------------v
        MySprite Hydrogen = new MySprite(SpriteType.TEXT, "Топливо:", new Vector2(40f, 180f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(Hydrogen);
        Hydrogen = new MySprite(SpriteType.TEXT, Math.Round(GetFuelPrecentage(), 1) + "%", new Vector2(470f, 180f), null, Color.Green, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(Hydrogen);
        //^----------------------------- ТОПЛИВО -----------------------------^

        //v----------------------------- ИНСТРУКТОР -----------------------------v
        MySprite InstructorSprite = new MySprite(SpriteType.TEXT, "Инструктор:", new Vector2(40f, 210f), null, Color.Green, "Debug", TextAlignment.LEFT, 1f);
        Frame.Add(InstructorSprite);
        Color InstructorColor = Color.Maroon;
        string InstructorStatus; if (Instructor) { InstructorStatus = "Вкл."; InstructorColor = Color.Green; } else InstructorStatus = "Выкл.";
        InstructorSprite = new MySprite(SpriteType.TEXT, InstructorStatus, new Vector2(470f, 210f), null, InstructorColor, "Debug", TextAlignment.RIGHT, 1f);
        Frame.Add(InstructorSprite);
        //^----------------------------- ИНСТРУКТОР -----------------------------^

        //v------------------------- ИНДИКАЦИЯ ПОВРЕЖДЕНИЙ -------------------------v
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

        //v------------------------------- Винты -----------------------------v
        Helicopter = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 285f), new Vector2(300f, 5f), GetBlockEnabled(PropellerUp), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 300f), new Vector2(300f, 5f), GetBlockEnabled(PropellerLow), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        //^------------------------------- Винты -----------------------------^

        //v----------------------------  Двигатели  -----------------------------v
        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X - 35, 330f), new Vector2(30f, 30f), Color.Black, "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X - 35, 330f), new Vector2(24f, 24f), GetBlockEnabled(LeftEngine), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X + 35, 330f), new Vector2(30f, 30f), Color.Black, "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X + 35, 330f), new Vector2(24f, 24f), GetBlockEnabled(RightEngine), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
        Frame.Add(Helicopter);
        //^----------------------------  Двигатели  -----------------------------^

        //v---------------------------- Вооружение -----------------------------v
        foreach (IMyConveyorSorter Object_Weapon in AdditionalWeapon) {
            float temp = (Object_Weapon.Position.X < 0) ? 0 : 4;
            float offsetX = Object_Weapon.Position.X - temp;

            Vector2 Position = new Vector2((CenterScreen.X + offsetX * -25f), 405f);

            MySprite Weapon = new MySprite(SpriteType.TEXTURE, "Circle", Position, new Vector2(20f, 20f), GetBlockEnabled(Object_Weapon), "", TextAlignment.CENTER, 0f);
            Frame.Add(Weapon);
            Weapon = new MySprite(SpriteType.TEXT, GetInventoryCount(Object_Weapon).ToString(), new Vector2(Position.X, 415f), null, GetBlockEnabled(Object_Weapon), "Debug", TextAlignment.CENTER, 0.675f);
            Frame.Add(Weapon);
        }

        Helicopter = new MySprite(SpriteType.TEXT, GetInventoryCount(ChainGun).ToString(), new Vector2(CenterScreen.X, 410f), null, Color.Black, "Debug", TextAlignment.CENTER, 0.7f);
        Frame.Add(Helicopter);

        Helicopter = new MySprite(SpriteType.TEXT, $"ЛТЦ: {GetInventoryRangeCount(DecoyList).ToString()}", new Vector2(40f, 445f), null, Color.Green, "Debug", TextAlignment.LEFT, 0.8f);
        Frame.Add(Helicopter);
        //^--------------------------- Вооружение ---------------------------^

        //v----------------------------- Шасси -----------------------------v
        if (WheelHingeList[0].TargetVelocityRPM > 0)
        {
            Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X, 450f), new Vector2(10f, 30f), GetBlockEnabled("Forward Wheel Rotor"), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
            Frame.Add(Helicopter);
            Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X - 30, 445f), new Vector2(10f, 25f), GetBlockEnabled("Left Wheel Rotor"), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
            Frame.Add(Helicopter);
            Helicopter = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(CenterScreen.X + 30, 445f), new Vector2(10f, 25f), GetBlockEnabled("Right Wheel Rotor"), "", TextAlignment.CENTER, (float)(180 * Math.PI / 180));
            Frame.Add(Helicopter);
        }
        //^----------------------------- Шасси -----------------------------^
        //^---------------------- ИНДИКАЦИЯ ПОВРЕЖДЕНИЙ ----------------------^
    }

    using (MySpriteDrawFrame Frame = Surface3.DrawFrame()) {
        MySprite Info = new MySprite(SpriteType.TEXT, $"Масса: {Math.Round(PilotCockpit.CalculateShipMass().TotalMass)} кг", new Vector2(60f, 60f), null, Color.Green, "Debug", TextAlignment.LEFT, 1.75f);
        Frame.Add(Info);
    }
}

void SetHorizon()
{
    // Метод расчитывает и устанавливает вертолёт в прямое положение относительно горизонта.

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
    // Метод возвращает действительное значение наклона вертолёта по координате Z.

    Vector3D GravityVector = PilotCockpit.GetNaturalGravity();
    Vector3D GravityNormalize = Vector3D.Normalize(GravityVector);

    double GravityLeft = GravityNormalize.Dot(PilotCockpit.WorldMatrix.Left);
    double GravityUp = GravityNormalize.Dot(PilotCockpit.WorldMatrix.Up);

    return (float)Math.Atan2(GravityLeft, -GravityUp);
}

float GetPitch()
{
    // Метод возвращает действительное значение наклона вертолёта по координате Y.

    Vector3D GravityVector = PilotCockpit.GetNaturalGravity();
    Vector3D GravityNormalize = Vector3D.Normalize(GravityVector);

    double GravityForward = GravityNormalize.Dot(PilotCockpit.WorldMatrix.Forward);
    double GravityUp = GravityNormalize.Dot(PilotCockpit.WorldMatrix.Up);

    return (float)Math.Atan2(GravityForward, -GravityUp);
}

float GetAzimuth()
{
    // Метод возвращает действительное значение наклона вертолёта по координате X.

    Vector3D GravityVector = PilotCockpit.GetNaturalGravity();
    Vector3D GravityNormalize = Vector3D.Normalize(GravityVector);

    double GravityUp = GravityNormalize.Dot(PilotCockpit.WorldMatrix.Left);
    double GravityLeft = GravityNormalize.Dot(PilotCockpit.WorldMatrix.Forward);

    return (float)Math.Atan2(GravityUp, -GravityLeft);
}

void Dumpeners()
{
    // Метод расчитывает и применяет угол наклона вертолёта для погашения боковой скорости.

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

void SetAntennaData() { Antenna.CustomData = "Имя: Пилот_Ка-52\nЧастота: 100.0"; } // Установка стандартных данных антенны (Требует переработку)

string GetAntennaData(string Data)
{
    // Метод возвращает данные антенны (имя и частота).

    string[] AntennaData = Antenna.CustomData.Split('\n');
    switch (Data)
    {
        case "Name": return AntennaData[0].Split(':')[1];
        case "Frequency": return AntennaData[1].Split(':')[1];
    }
    return "Неизвестные данные";
}

double GetAltitude()
{
    // Метод возвращает действительное значение, указывающее
    // на высоту вертолёта над уровнем моря (уровень моря задаётся переменной SeaAltitude).

    double Vector = (PilotCockpit.GetPosition() - PlanetVector).Length() - (SeaAltitude - PlanetVector).Length();
    return Math.Round(Vector);
}

double GetFuelPrecentage() // Возврат процентного значения водорода в баках
{
    // Метод, возвращающий действительное значение, указывающее процент
    // остатка водорода в баках без учёта двигаталей.

    double CurrentFuel = 0f;
    foreach (IMyGasTank Object_HydrogenTank in GasTankList) { CurrentFuel += Object_HydrogenTank.FilledRatio * 100; }
    return CurrentFuel / GasTankList.Count;
}

void PlaySound(string SoundName)
{
    // Метод, включающий в динамике указанный звук

    Sound.SelectedSound = SoundName;
    Sound.Play();
}

MyFixedPoint GetInventoryCount(IMyTerminalBlock Block)
{
    // Метод возвращает количественное значение элементов в инвентаре указанного блока.

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

MyFixedPoint GetInventoryRangeCount(List<IMyConveyorSorter> List) {
    MyFixedPoint ResultCount = 0;

    foreach (IMyConveyorSorter Object_Block in List) {
        IMyInventory Inventory = Object_Block.GetInventory(0);
        List<MyInventoryItem> ItemList = new List<MyInventoryItem>();
        Inventory.GetItems(ItemList);
        MyFixedPoint ItemCount = 0;
        foreach (MyInventoryItem Object_Item in ItemList) ItemCount += Object_Item.Amount;
        ResultCount += ItemCount;
    }
    return ResultCount;
}

MySprite DrawLine(Vector2 Point1, Vector2 Point2, float Width, Color Color) // Отрисовка линии
{
    // Метод возвращает спрайт, который из себя представляет линию.
    // Входные данные:
    // Point1 - вектор начальной точки линии (X, Y)
    // Point2 - вектор конечной точки линии (X, Y)
    // Width - ширина линии (в пикселях)

    Vector2 CenterVector = (Point1 + Point2) / 2;
    return new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterVector, new Vector2((Point1 - Point2).Length(), Width), Color, "", TextAlignment.CENTER, (float)Math.Atan((Point2.Y - Point1.Y) / (Point2.X - Point1.X)));
}

MySprite DrawLine(Vector2 Point1, Vector2 Point2, float Width, Color Color, float Rotate)
{
    // Метод возвращает спрайт, который из себя представляет линию.
    // Текущий метод, в отличие от предыдущего, учитывает угол поворота линии (в радианах).
    // Входные данные:
    // Point1 - вектор начальной точки линии (X, Y)
    // Point2 - вектор конечной точки линии (X, Y)
    // Width - ширина линии (в пикселях)

    Vector2 CenterVector = (Point1 + Point2) / 2;
    return new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterVector, new Vector2((Point1 - Point2).Length(), Width), Color, "", TextAlignment.CENTER, Rotate);
}

Color GetBlockEnabled(IMyTerminalBlock Block)
{
    // Получение цвета блока в индикации (по ссылке на блок).
    // Если блок не работает или повреждён до нерабочего состояния - метод возвращает красный цвет.
    // Иначе вернёт зелёный.

    if (Block.IsWorking && Block != null) return Color.Green;
    else return Color.Maroon;
}

Color GetBlockEnabled(string BlockName)
{
    // Получение цвета блока в индикации (по названия блока).
    // Если блок не работает или повреждён до нерабочего состояния - метод возвращает красный цвет.
    // Иначе вернёт зелёный.

    IMyTerminalBlock Block = (IMyTerminalBlock)GridTerminalSystem.GetBlockWithName(BlockName);
    if (Block.IsWorking && Block != null) return Color.Green;
    else return Color.Maroon;
}

void SetDefaultSystemData()
{
    // Метод устанавливает стандартные значения для работы некоторых систем
    // вертолёта, а также для индикации на главной панели.
    // Используется в случае первого запуска, ошибки системы или для "обнуления" данных.

    string Data = "[-----=====|=====-----]" +
                    "\nИнструктор:" + ReturnOnOff(Instructor) +
                    "\nУдержание горизонта:" + ReturnOnOff(Horizon) +
                    "\nГаситель скорости:" + ReturnOnOff(SpeedDumpeners) +
                    "\nПомощь прицеливания:" + ReturnOnOff(AimAssist);
    Me.CustomData = Data;
}

void GetSystemData()
{
    // Метод вытаскивает тектовые данные из текущего программного блока,
    // помещая их значения в соответствующие переменные.

    string[] Data = Me.CustomData.Split('\n');
    Instructor = ReturnOnOff(Data[1].Split(':')[1]);
    Horizon = ReturnOnOff(Data[2].Split(':')[1]);
    SpeedDumpeners = ReturnOnOff(Data[3].Split(':')[1]);
    AimAssist = ReturnOnOff(Data[4].Split(':')[1]);
}

string ReturnOnOff(bool Param)
{
    // Метод возвращает строковое значение типа "Вкл/Выкл", получив булевое значение.

    if (Param) return "Вкл.";
    else return "Выкл.";
}

bool ReturnOnOff(string Param)
{
    // Метод возвращает булевое значение, получив строковое значение типа "Вкл/Выкл".

    if (Param == "Вкл.") return true;
    else return false;
}

void DrawHUD()
{
    // Метод для отрисовки HUD'а на дисплее.

    FighterLCD.ContentType = ContentType.NONE;
    FighterLCD.ContentType = ContentType.SCRIPT;
    FighterLCD.ScriptBackgroundColor = Color.Black;

    float _xPos = 0f;
    float _yPos = 0f;

    using (MySpriteDrawFrame Frame = FighterLCD.DrawFrame())
    {
        if (_targetPosition != new Vector3D())
        {
            Vector3D _tempVector = Vector3.Normalize(_targetPosition - FighterLCD.GetPosition());

            float _tempDistance = (float)(_targetPosition - FighterLCD.GetPosition()).Length();

            float azimuth = (float)Math.Atan2(Vector3D.Dot(_tempVector, FighterLCD.WorldMatrix.Right), Vector3D.Dot(_tempVector, FighterLCD.WorldMatrix.Forward));
            float elevation = (float)Math.Asin(Vector3D.Dot(_tempVector, FighterLCD.WorldMatrix.Up));

            _xPos = 256f + (float)GetAngleBetweenVectors(FighterLCD.WorldMatrix.Forward, FighterLCD.WorldMatrix.Right, _tempVector) * 27f;
            _yPos = 256f + (float)GetAngleBetweenVectors(FighterLCD.WorldMatrix.Forward, FighterLCD.WorldMatrix.Up, _tempVector) * -37;
        }

        Frame.Add(DrawLine(new Vector2(140f, 276f), new Vector2(372f, 276f), 2f, Color.Green, 0f));
        Frame.Add(DrawLine(new Vector2(140f, 276f), new Vector2(372f, 276f), 2f, Color.Green, 0.5236f));
        Frame.Add(DrawLine(new Vector2(140f, 276f), new Vector2(372f, 276f), 2f, Color.Green, -0.5236f));
        Frame.Add(DrawLine(new Vector2(140f, 276f), new Vector2(372f, 276f), 2f, Color.Green, 1.0472f));
        Frame.Add(DrawLine(new Vector2(140f, 276f), new Vector2(372f, 276f), 2f, Color.Green, -1.0472f));
        
        Frame.Add(DrawLine(new Vector2(155f, 276f), new Vector2(357f, 276f), 1f, Color.Green, 0.2618f));
        Frame.Add(DrawLine(new Vector2(155f, 276f), new Vector2(357f, 276f), 1f, Color.Green, -0.2618f));
        Frame.Add(DrawLine(new Vector2(155f, 276f), new Vector2(357f, 276f), 1f, Color.Green, 0.7854f));
        Frame.Add(DrawLine(new Vector2(155f, 276f), new Vector2(357f, 276f), 1f, Color.Green, -0.7854f));

        Frame.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(256f, 256f), new Vector2(170f, 170f), Color.Black, "", TextAlignment.CENTER, 0f));
        Frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 127f), new Vector2(512f, 254f), Color.Black, "", TextAlignment.CENTER, 0f));

        Frame.Add(DrawLine(new Vector2(190f, 276f), new Vector2(322f, 276f), 2f, Color.Green, GetHorizon()));
        Frame.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(256f, 256f), new Vector2(50f, 50f), Color.Black, "", TextAlignment.CENTER, 0f));

        Frame.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(256f, 276f), new Vector2(3f, 3f), Color.Green, "", TextAlignment.CENTER, 0f));

        for (int i = -90; i < 90; i += 10)
        {
            float spriteHeight = (float)(-GetPitch() * 180 / Math.PI) * 6 + (256f + i * 6f);
            Frame.Add(new MySprite(SpriteType.TEXT, (-i).ToString(), new Vector2(60f, spriteHeight), null, Color.Green, "Debug", TextAlignment.RIGHT, 1.15f));
            if (i == 0) Frame.Add(DrawLine(new Vector2(60f * 1.15f + 5, spriteHeight + 15f), new Vector2(100f * 1.15f + 5, spriteHeight + 15f), 1f, Color.Green));
            else if (-i > 0)
            {
                Frame.Add(DrawLine(new Vector2(60f * 1.15f + 5, spriteHeight + 15f), new Vector2(100f * 1.15f + 5, spriteHeight + 15f), 1f, Color.Green));
                Frame.Add(DrawLine(new Vector2(60f * 1.15f + 5, spriteHeight + 15f), new Vector2(60f * 1.15f + 5, spriteHeight + 25f), 1f, Color.Green));
                Frame.Add(DrawLine(new Vector2(70f * 1.15f + 5, spriteHeight + 15f), new Vector2(76f * 1.15f + 5, spriteHeight + 15f), 1f, Color.Black));
                Frame.Add(DrawLine(new Vector2(86f * 1.15f + 5, spriteHeight + 15f), new Vector2(92f * 1.15f + 5, spriteHeight + 15f), 1f, Color.Black));
            }
            else if (-i < 0)
            {
                Frame.Add(DrawLine(new Vector2(60f * 1.15f + 5, spriteHeight + 15f), new Vector2(100f * 1.15f + 5, spriteHeight + 15f), 1f, Color.Green));
                Frame.Add(DrawLine(new Vector2(60f * 1.15f + 5, spriteHeight + 15f), new Vector2(60f * 1.15f + 5, spriteHeight + 5f), 1f, Color.Green));
            }
        }
        Frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(60f, 0f), new Vector2(200f, 217.5f), Color.Black, "", TextAlignment.CENTER, 0f));
        Frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(60f, 512f), new Vector2(200f, 217.5f), Color.Black, "", TextAlignment.CENTER, 0f));

        Frame.Add(DrawLine(new Vector2(100f, 30f), new Vector2(412f, 30f), 1f, Color.Green));

        int Azimuth = (int)(180 + Math.Round(GetAzimuth() * 180 / Math.PI));

        if (_targetPosition != new Vector3D())
        {
            Frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(_xPos, _yPos), new Vector2(50f, 50f), Color.Red, "", TextAlignment.CENTER, (float)(_triangleCounter * Math.PI / 360)));
            Frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(_xPos, _yPos), new Vector2(48f, 48f), Color.Black, "", TextAlignment.CENTER, (float)(_triangleCounter * Math.PI / 360)));
        }
    }
}

Vector3D CameraLock()
{
    MyDetectedEntityInfo entity = Camera.Raycast(2000, 0, 0);
    Vector3D entityPosition = entity.HitPosition.Value;
    return entityPosition;
}

void CameraTracking(Vector3D TargetPosition)
{
    if (TargetLocked)
    {
        Vector3D MyPosition = CameraAzimuth.GetPosition();
        Vector3D DirectionToTarget = Vector3D.Normalize(TargetPosition - MyPosition);

        double RotorRequiredAngle = GetAngleBetweenVectors(CameraAzimuth.WorldMatrix.Forward, CameraAzimuth.WorldMatrix.Right, DirectionToTarget) - 90;
        double HingeRequiredAngle = 90 + GetAngleBetweenVectors(CameraElevation.WorldMatrix.Forward, CameraElevation.WorldMatrix.Right, DirectionToTarget);

        double RotorCurrentAngle = CameraAzimuth.Angle * (180 / Math.PI);
        if (RotorCurrentAngle > RotorRequiredAngle)
        {
            CameraAzimuth.UpperLimitDeg = 180;
            CameraAzimuth.LowerLimitDeg = (float)RotorRequiredAngle;
            CameraAzimuth.TargetVelocityRPM = -20f;
        }
        else if (RotorCurrentAngle < RotorRequiredAngle)
        {
            CameraAzimuth.UpperLimitDeg = (float)RotorRequiredAngle;
            CameraAzimuth.LowerLimitDeg = -180;
            CameraAzimuth.TargetVelocityRPM = 20f;
        }

        double HingeCurrentAngle = CameraElevation.Angle * (180 / Math.PI) + 90;
        if (HingeCurrentAngle > HingeRequiredAngle)
        {
            CameraElevation.UpperLimitDeg = 90;
            CameraElevation.LowerLimitDeg = (float)HingeRequiredAngle;
            CameraElevation.TargetVelocityRPM = -20f;
        }
        else if (HingeCurrentAngle < HingeRequiredAngle)
        {
            CameraElevation.UpperLimitDeg = (float)RotorRequiredAngle;
            CameraElevation.LowerLimitDeg = 0;
            CameraElevation.TargetVelocityRPM = 20f;
        }
    }
}

private double GetAngleBetweenVectors(Vector3D VectorForward, Vector3D VectorRight, Vector3D EntityVector)
{
    double Cos = EntityVector.Dot(VectorForward);
    double Sin = EntityVector.Dot(VectorRight);
    return Math.Atan2(Sin, Cos) / Math.PI * 180;
}

private void ResetAngles()
{
    CameraAzimuth.UpperLimitDeg = 180f;
    CameraAzimuth.LowerLimitDeg = -180f;
    CameraElevation.UpperLimitDeg = 90f;
    CameraElevation.LowerLimitDeg = 0f;
}