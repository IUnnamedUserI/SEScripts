//Ка-52 - Система посадки и контекстное меню
/*ToDo:
1. GPS
2. Расширенное управление инструктором
*/

IMyTextSurface LandingSurface, ContextScreen;
IMyCockpit Cockpit;
IMyRadioAntenna Antenna;

Vector2 CenterScreen;
LandingArea CurrentLandingArea;
int Counter = 0;
ContextMenu PrevMenu, CurrentMenu, Control, InstructorMenu, GPSMenu, GPSListMenu;
bool IsLandingChassis = true;
bool IsStels = false;

List<IMyCockpit> CockpitList = new List<IMyCockpit>();
List<LandingArea> LandingAreaList = new List<LandingArea>();
List<LandingArea> GPSList = new List<LandingArea>();
List<ContextMenu> ContextMenuList = new List<ContextMenu>();

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;

    Antenna = (IMyRadioAntenna)GridTerminalSystem.GetBlockWithName("Helicopter Antenna");

    GridTerminalSystem.GetBlocksOfType<IMyCockpit>(CockpitList);

    LandingSurface = Me.GetSurface(0);
    ContextScreen = Me.GetSurface(1);
    CenterScreen = new Vector2(LandingSurface.SurfaceSize.X / 2, LandingSurface.SurfaceSize.Y / 2);

    Init();
    CreateControlScreen();
    //CreateInstructorScreen();
    CreateGPSScreen();

    CurrentMenu = Control;
}

void Init()
{
    /*
    Инициализация дополнительных модулей
    */
    for (int i = 0; Cockpit == null; i++) { if (CockpitList[i].IsFunctional) Cockpit = CockpitList[i]; } // Инициализация кокпита
    LandingAreaList = GetLandingPoints(Cockpit); // Получение статичных точек приземления из кокпита
    Stels(true); // Отключение стелс-режима
    ContextMenuList.Add(Control);
    ContextMenuList.Add(InstructorMenu);
    ContextMenuList.Add(GPSMenu);
}

void Main(string argument)
{
    DrawLandingSystem();
    ShowMenu(CurrentMenu);
    GetMessage();
    if (Counter <= 60) Counter++; else Counter = 0;

    foreach(LandingArea Object_Area in LandingAreaList) if (GetDistance(Object_Area) < 250) CurrentLandingArea = Object_Area;

    if (argument == "ContextMenuUpDown_Up") CurrentMenu.SwitchUp();
    else if (argument == "ContextMenuUpDown_Down") CurrentMenu.SwitchDown();
    else if (argument == "ContextMenuClick") ButtonClick(CurrentMenu.SelectedButton());
}

void DrawLandingSystem()
{
    /*
    Отрисовка посадочной системы
    */

    LandingSurface.ContentType = ContentType.NONE; LandingSurface.ContentType = ContentType.SCRIPT;
    using (MySpriteDrawFrame Frame = LandingSurface.DrawFrame())
    {
        if (CurrentLandingArea != null)
        {
            if (GetDistance(CurrentLandingArea.LandingVector) < 50) // Дистанция до точки менее 50 метров
            {
                if (Counter < 30)
                {
                    MySprite Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(450f, 450f), Color.Yellow, "", TextAlignment.CENTER, 0f);
                    Frame.Add(Border);
                    Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(440f, 440f), Color.Black, "", TextAlignment.CENTER, 0f);
                    Frame.Add(Border);
                    Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(80f, 512f), Color.Black, "", TextAlignment.CENTER, 0f);
                    Frame.Add(Border);
                    Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(512f, 80f), Color.Black, "", TextAlignment.CENTER, 0f);
                    Frame.Add(Border);

                    Vector2 CircleSize = new Vector2(30 * (float)(GetDistance(CurrentLandingArea.LandingVector) * 0.25));

                    MySprite LandingCircle = new MySprite(SpriteType.TEXTURE, "Circle", CenterScreen, CircleSize, Color.Yellow, "", TextAlignment.CENTER, 0f);
                    Frame.Add(LandingCircle);
                    LandingCircle = new MySprite(SpriteType.TEXTURE, "Circle", CenterScreen, new Vector2(CircleSize.X - 2, CircleSize.Y - 2), Color.Black, "", TextAlignment.CENTER, 0f);
                    Frame.Add(LandingCircle);

                    MySprite CenterMarker = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(40f, 1f), Color.Yellow, "", TextAlignment.CENTER, 0f);
                    Frame.Add(CenterMarker);
                    CenterMarker = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(1f, 40f), Color.Yellow, "", TextAlignment.CENTER, 0f);
                    Frame.Add(CenterMarker);
                    CenterMarker = new MySprite(SpriteType.TEXTURE, "Circle", CenterScreen, new Vector2(26f, 26f), Color.Yellow, "", TextAlignment.CENTER, 0f);
                    Frame.Add(CenterMarker);
                    CenterMarker = new MySprite(SpriteType.TEXTURE, "Circle", CenterScreen, new Vector2(24f, 24f), Color.Black, "", TextAlignment.CENTER, 0f);
                    Frame.Add(CenterMarker);
                }
            }
            else if (GetDistance(CurrentLandingArea.LandingVector) >= 50 && GetDistance(CurrentLandingArea.LandingVector) < 250)// Точка находится на промеждутке от 50 до 250 метров
            {
                MySprite Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(450f, 450f), Color.Green, "", TextAlignment.CENTER, 0f);
                Frame.Add(Border);
                Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(440f, 440f), Color.Black, "", TextAlignment.CENTER, 0f);
                Frame.Add(Border);
                Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(80f, 512f), Color.Black, "", TextAlignment.CENTER, 0f);
                Frame.Add(Border);
                Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(512f, 80f), Color.Black, "", TextAlignment.CENTER, 0f);
                Frame.Add(Border);

                double Angle = Math.Round(GetAngleBetweenVectors(Cockpit.WorldMatrix.Forward, Cockpit.WorldMatrix.Right, Vector3D.Normalize(CurrentLandingArea.LandingVector - Cockpit.GetPosition())));

                MySprite Arrow = new MySprite(SpriteType.TEXTURE, "Triangle", CenterScreen, new Vector2(75f, 150), Color.Green, "", TextAlignment.CENTER, (float)(Angle * Math.PI / 180));
                Frame.Add(Arrow);
                Arrow = new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(CenterScreen.X, CenterScreen.Y), new Vector2(65f, 140f), Color.Black, "", TextAlignment.CENTER, (float)(Angle * Math.PI / 180));
                Frame.Add(Arrow);

                MySprite LandingAreaName = new MySprite(SpriteType.TEXT, CurrentLandingArea.AreaName, new Vector2(CenterScreen.X, 80f), null, Color.Green, "Debug", TextAlignment.CENTER, 1.5f);
                Frame.Add(LandingAreaName);
            }
        }
        else
        {
            MySprite NoLandingPoint = new MySprite(SpriteType.TEXT, "Точка посадки отсутствует", CenterScreen, null, Color.Green, "Debug", TextAlignment.CENTER, 1.5f);
            Frame.Add(NoLandingPoint);
        }
    }
}

string GetAntennaData(string Data)
{
    /*
    Получение данных с антенны - имя и частота
    */
    string[] AntennaData = Antenna.CustomData.Split('\n');
    switch (Data)
    {
        case "Name": return AntennaData[0].Split(':')[1];
        case "Frequency": return AntennaData[1].Split(':')[1];
    }
    return "Data not found";
}

double GetDistance(Vector3D Point) { return (Cockpit.CenterOfMass - Point).Length(); }

double GetDistance(LandingArea Area) { return (Cockpit.CenterOfMass - Area.LandingVector).Length(); }

double GetAngleBetweenVectors(Vector3D ForwardVector, Vector3D RightVector, Vector3D EntityVector)
{
    double Cos = EntityVector.Dot(ForwardVector);
    double Sin = EntityVector.Dot(RightVector);
    return Math.Atan2(Sin, Cos) / Math.PI * 180;
}

void GetMessage()
{
    Me.CustomData = string.Empty;
    MyIGCMessage Message = new MyIGCMessage();
    IMyBroadcastListener Listener = IGC.RegisterBroadcastListener(GetAntennaData("Frequency"));
    if (Listener.HasPendingMessage)
    {
        Message = Listener.AcceptMessage();
        string[] AcceptedMessage = Message.Data.ToString().Split(' ');
        CurrentLandingArea = new LandingArea("Точка посадки", new Vector3D(Convert.ToDouble(AcceptedMessage[0].Split(':')[1]), Convert.ToDouble(AcceptedMessage[1].Split(':')[1]), Convert.ToDouble(AcceptedMessage[2].Split(':')[1])));
    }
}

List<LandingArea> GetLandingPoints(IMyTerminalBlock Block)
{
    List<LandingArea> AreaList = new List<LandingArea>();
    if (Block.CustomData != string.Empty)
    {
        string[] PointList = Block.CustomData.Split('\n');
        foreach (string Object_String in PointList)
        {
            string[] PointData = Object_String.Split(':');
            AreaList.Add(new LandingArea(PointData[0], new Vector3D(Convert.ToDouble(PointData[1]), Convert.ToDouble(PointData[2]), Convert.ToDouble(PointData[3]))));
        }
    }
    return AreaList;
}

void ButtonClick(Button Object_Button)
{
    string Tag = Object_Button.Tag;
    switch (Tag)
    {
        case "Back":
            CurrentMenu = PrevMenu;
            PrevMenu = null;
            break;

        case "LandingGears":
            List<IMyMotorStator> LandingGearList = new List<IMyMotorStator>();
            GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(LandingGearList, (l) => l.DisplayNameText.Contains("Wheel Rotor"));
            foreach (IMyMotorStator Object_Rotor in LandingGearList) Object_Rotor.ApplyAction("Reverse");
            if (LandingGearList[0].TargetVelocityRPM < 0) {IsLandingChassis = false; }
            else { IsLandingChassis = true; }
            
            List<IMyInteriorLight> LandingLightList = new List<IMyInteriorLight>();
            GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(LandingLightList, (l) => l.DisplayNameText.Contains("Landing Light"));
            if (IsStels) foreach (IMyInteriorLight Object_Light in LandingLightList) Object_Light.Enabled = IsLandingChassis;
            else foreach (IMyInteriorLight Object_Light in LandingLightList) Object_Light.Enabled = false;
            break;

        case "Stels":
            Stels(!IsStels);
            break;

        case "InstructorMenu":
            CreateInstructorScreen();
            CurrentMenu = InstructorMenu;
            PrevMenu = Control;
            break;

        case "GPSMenu":
            CurrentMenu = GPSMenu;
            PrevMenu = Control;
            break;

        case "Instructor":
            ChangeParam("Instructor");
            break;
        
        case "Horizon":
            ChangeParam("Horizon");
            break;
        
        case "SpeedDumpeners":
            ChangeParam("SpeedDumpeners");
            break;

        case "AimAssist":
            ChangeParam("Помощь прицеливания");
            break;

        case "CreateNewLandingArea":
            GPSList.Add(new LandingArea("Новая точка", Cockpit.GetPosition()));
            break;
    }
    for (int i = 1; i < CurrentMenu.ButtonList.Count; i++) try { CurrentMenu.ChangeButtonColor(CurrentMenu.ButtonList[i].Tag, GetParam(CurrentMenu.ButtonList[i].Tag)); } catch {}
}

void ShowMenu(ContextMenu Menu)
{
    ContextScreen.ContentType = ContentType.NONE; ContextScreen.ContentType = ContentType.SCRIPT;
    using (MySpriteDrawFrame Frame = ContextScreen.DrawFrame()) { Frame.AddRange(Menu.DrawMenu()); }
}

void DrawContextMenu(string ControlName)
{
    switch (ControlName)
    {
        case "Управление": ShowMenu(Control); break;
        case "Инструктор": ShowMenu(InstructorMenu); break;
        case "GPS": ShowMenu(GPSMenu); break;
        default: ShowMenu(Control); break;
    }
    ContextMenu Menu = new ContextMenu();
    foreach (ContextMenu Object_ContextMenu in ContextMenuList) if (Object_ContextMenu.Title == ControlName) Menu = Object_ContextMenu;
    ShowMenu(Menu);
}

void CreateControlScreen()
{
    Control = new ContextMenu("Управление");
    Control.CreateButton("Шасси", "LandingGears");
    Control.CreateButton("Стелс", "Stels");
    Control.CreateButton("Инструктор", "InstructorMenu");
    Control.CreateButton("GSP", "GPSMenu");
}

void CreateInstructorScreen()
{
    InstructorMenu = new ContextMenu("Инструктор");
    InstructorMenu.CreateButton("< Назад", "Back");
    InstructorMenu.CreateButton("Вкл/выкл Инструктор", "Instructor", GetParam("Инструктор"));
    InstructorMenu.CreateButton("Удержание горизонта", "Horizon", GetParam("Удержание горизонта"));
    InstructorMenu.CreateButton("Гаситель скорости", "SpeedDumpeners", GetParam("Гаситель скорости"));
    InstructorMenu.CreateButton("Помощ. прицеливания", "AimAssist", GetParam("Помощь прицеливания"));
}

void CreateGPSScreen()
{
    GPSMenu = new ContextMenu("GPS");
    GPSMenu.CreateButton("< Назад", "Back");
    GPSMenu.CreateButton("Список точек", "StaticLandingAreaList");
    GPSMenu.CreateButton("Добавить точку", "CreateNewLandingArea");
}

void CreateGPSListScreen()
{
    GPSListMenu = new ContextMenu("Список точек");
    GPSListMenu.CreateButton("< Назад", "Back");
    foreach (LandingArea GPSPoint in GPSList)
        GPSListMenu.CreateGPSButton(GPSPoint.AreaName, GPSPoint.LandingVector);
}

//CurrentLandingArea

void Stels(bool StelsEnabled)
{
    IsStels = StelsEnabled;
    List<IMyInteriorLight> InteriorLightList = new List<IMyInteriorLight>();
    GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(InteriorLightList, (l) => !l.DisplayNameText.Contains("Landing"));
    foreach (IMyInteriorLight Object_Light in InteriorLightList) Object_Light.Enabled = StelsEnabled;

    List<IMyRadioAntenna> AntennaList = new List<IMyRadioAntenna>();
    GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(AntennaList);
    foreach (IMyRadioAntenna Object_Antenna in AntennaList) Object_Antenna.Enabled = StelsEnabled;

    if (IsLandingChassis)
    {
        List<IMyInteriorLight> LandingIntLightList = new List<IMyInteriorLight>();
        GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(LandingIntLightList, (l) => l.DisplayNameText.Contains("Landing Light"));
        foreach (IMyInteriorLight Object_Light in LandingIntLightList) Object_Light.Enabled = StelsEnabled;
    }
}

void ChangeParam(string Param)
{
    IMyProgrammableBlock MainPB = (IMyProgrammableBlock)GridTerminalSystem.GetBlockWithName("Program - Main Systems");
    string PBData = MainPB.CustomData;
    string[] Data = PBData.Split('\n');
    for (int i = 0; i < Data.Count(); i++)
        if (Data[i].Contains(Param))
        {
            string[] ParamSplit = Data[i].Split(':');
            if (ParamSplit[1] == "Вкл.") ParamSplit[1] = "Выкл."; else ParamSplit[1] = "Вкл.";
            Data[i] = ParamSplit[0] + ":" + ParamSplit[1];
        }
    PBData = string.Empty;
    for (int i = 0; i < Data.Count(); i++) if (i == 0) PBData = Data[0]; else PBData += "\n" + Data[i];
    MainPB.CustomData = PBData;
}

public Color GetParam(string Param)
{
    IMyProgrammableBlock MainPB = (IMyProgrammableBlock)GridTerminalSystem.GetBlockWithName("Program - Main Systems");
    string PBData = MainPB.CustomData;
    string[] Data = PBData.Split('\n');
    for (int i = 0; i < Data.Count(); i++)
        if (Data[i].Contains(Param))
        {
            string[] ParamSplit = Data[i].Split(':');
            if (ParamSplit[1] == "Вкл.") return Color.Green; else return Color.Maroon;
        }
    return Color.Yellow;
}

class LandingArea
{
    //Формат точек: Название:X:Y:Z
    public string AreaName;
    public Vector3D LandingVector;

    public LandingArea(string PointName, Vector3D LandingPoint)
    {
        AreaName = PointName;
        LandingVector = LandingPoint;
    }
}

class ContextMenu
{
    public string Title;
    public List<Button> ButtonList;
    private int SelectedButtonID;

    Vector2 CenterScreen = new Vector2(256f, 256f);

    public ContextMenu()
    {
        Title = "Меню";
        ButtonList = new List<Button>();
        SelectedButtonID = 0;
    }

    public ContextMenu(string Title)
    {
        this.Title = Title;
        ButtonList = new List<Button>();
        SelectedButtonID = 0;
    }

    public List<MySprite> DrawMenu()
    {
        List<MySprite> SpriteList = new List<MySprite>();
        MySprite Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(420f, 420f), Color.Green, "", TextAlignment.CENTER, 0f);
        SpriteList.Add(Border);
        Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", CenterScreen, new Vector2(410f, 410f), Color.Black, "", TextAlignment.CENTER, 0f);
        SpriteList.Add(Border);
        Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 50f), new Vector2(270f, 50f), Color.Green, "", TextAlignment.CENTER, 0f);
        SpriteList.Add(Border);
        Border = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(CenterScreen.X, 50f), new Vector2(265f, 45f), Color.Black, "", TextAlignment.CENTER, 0f);
        SpriteList.Add(Border);

        MySprite MenuTitle = new MySprite(SpriteType.TEXT, Title, new Vector2(CenterScreen.X, 20f), null, Color.Green, "DEBUG", TextAlignment.CENTER, 1.75f);
        SpriteList.Add(MenuTitle);

        foreach (Button Object_Button in ButtonList) SpriteList.AddRange(Object_Button.DrawButton());

        return SpriteList;
    }

    public void CreateButton(string Name, string Tag)
    {
        Button Btn = new Button(Name, new Vector2(65f, 100 + 40 * ButtonList.Count), Tag, Color.Green);
        ButtonList.Add(Btn);
        SelectButtonID(SelectedButtonID);
    }

    public void CreateButton(string Name, string Tag, Color ButtonColor)
    {
        Button Btn = new Button(Name, new Vector2(65f, 100 + 40 * ButtonList.Count), Tag, ButtonColor);
        ButtonList.Add(Btn);
        SelectButtonID(SelectedButtonID);
    }

    public void CreateGPSButton(string Name, Vector3D Coords)
    {
        Button Btn = new Button(Name, new Vector2(65f, 100 + 40 * ButtonList.Count), Coords.ToString(), Color.Green);
        ButtonList.Add(Btn);
        SelectButtonID(SelectedButtonID);
    }

    public void SwitchUp()
    {
        if (SelectedButtonID > 0)
        {
            SelectedButtonID--;
            foreach (Button Object_Button in ButtonList) Object_Button.DisselectButton();
            ButtonList[SelectedButtonID].SelectButton();
        }
    }

    public void SwitchDown()
    {
        if (SelectedButtonID < ButtonList.Count - 1)
        {
            SelectedButtonID++;
            foreach (Button Object_Button in ButtonList) Object_Button.DisselectButton();
            ButtonList[SelectedButtonID].SelectButton();
        }
    }

    public void SelectButtonID(int ButtonID)
    {
        SelectedButtonID = ButtonID;
        foreach (Button Object_Button in ButtonList) Object_Button.DisselectButton();
        ButtonList[SelectedButtonID].SelectButton();
    }

    public Button SelectedButton() { return ButtonList[SelectedButtonID]; } // Возврат выбранной кнопки

    public void ChangeButtonColor(string _tag, Color _color) // Смена цвета кнопки
    {
        foreach (Button Object_Button in ButtonList) if (Object_Button.Tag == _tag) Object_Button.ButtonColor = _color;
    }
}

class Button
{
    public string Text;
    private Vector2 Position;
    public string Tag;
    private bool IsSelected;
    public Color ButtonColor;

    public Button(string Text, Vector2 Position, string Tag, Color ButtonColor)
    {
        this.Text = Text;
        this.Position = Position;
        this.Tag = Tag;
        IsSelected = false;
        this.ButtonColor = ButtonColor;
    }

    public List<MySprite> DrawButton()
    {
        Color BorderColor = Color.White;
        if (IsSelected) BorderColor = Color.Green; else BorderColor = Color.Black;
        List<MySprite> ButtonList = new List<MySprite>();

        MySprite ButtonSprite = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(Position.X - 10, Position.Y + 20), new Vector2(400f, 40f), BorderColor, "", TextAlignment.LEFT, 0f);
        ButtonList.Add(ButtonSprite); // Рамка кнопки
        ButtonSprite = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(Position.X - 5, Position.Y + 20), new Vector2(397.5f, 37.5f), Color.Black, "", TextAlignment.LEFT, 0f);
        ButtonList.Add(ButtonSprite); // Фон кнопки
        ButtonSprite = new MySprite(SpriteType.TEXT, Text, Position, null, ButtonColor, "Debug", TextAlignment.LEFT, 1.5f);
        ButtonList.Add(ButtonSprite); // Текст кнопки

        return ButtonList;
    }

    public void SelectButton() { IsSelected = true; }

    public void DisselectButton() { IsSelected = false; }
}