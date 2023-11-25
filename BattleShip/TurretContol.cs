IMyShipWelder LMBDetector;
IMyTextSurface MainSurface;
IMyCockpit Cockpit;

Color ContentColor = Color.FromNonPremultiplied(0, 90, 75, 255);

List<IMySmallMissileLauncher> MainFirstTurretList = new List<IMySmallMissileLauncher>();
List<IMySmallMissileLauncher> MainSecondTurretList = new List<IMySmallMissileLauncher>();
List<IMySmallMissileLauncher> MainThirdTurretList = new List<IMySmallMissileLauncher>();
List<IMyCockpit> CockpitList = new List<IMyCockpit>();
List<IMyMotorStator> MotorStatorList1 = new List<IMyMotorStator>();
List<IMyMotorStator> MotorStatorList2 = new List<IMyMotorStator>();
List<IMyMotorStator> MotorStatorList3 = new List<IMyMotorStator>();

List<MainTurret> MainTurretList = new List<MainTurret>();
MainTurret Turret1, Turret2, Turret3;

public const float X_SENSIVITY = 0.1f;
public const float Y_SENSIVITY = 0.0125f;

public enum SpriteForm
{
    MainTurretInactive,
    MainTurretActive,
    Square
}

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;

    LMBDetector = (IMyShipWelder)GridTerminalSystem.GetBlockWithName("Welder (LMB-Detector)");

    GridTerminalSystem.GetBlocksOfType<IMySmallMissileLauncher>(MainFirstTurretList, (t) => t.DisplayNameText.Contains("[Turret 1]"));
    GridTerminalSystem.GetBlocksOfType<IMySmallMissileLauncher>(MainSecondTurretList, (t) => t.DisplayNameText.Contains("[Turret 2]"));
    GridTerminalSystem.GetBlocksOfType<IMySmallMissileLauncher>(MainThirdTurretList, (t) => t.DisplayNameText.Contains("[Turret 3]"));
    GridTerminalSystem.GetBlocksOfType<IMyCockpit>(CockpitList, (c) => c.IsMainCockpit);
    foreach (IMyCockpit object_Cockpit in CockpitList) if (object_Cockpit.IsMainCockpit) Cockpit = object_Cockpit;
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(MotorStatorList1, (m) => m.DisplayNameText.Contains("[Turret 1]"));
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(MotorStatorList2, (m) => m.DisplayNameText.Contains("[Turret 2]"));
    GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(MotorStatorList3, (m) => m.DisplayNameText.Contains("[Turret 3]"));

    Turret1 = new MainTurret(MainFirstTurretList, 60, MotorStatorList1, this);
    MainTurretList.Add(Turret1);
    Turret2 = new MainTurret(MainSecondTurretList, 60, MotorStatorList2, this);
    MainTurretList.Add(Turret2);
    Turret3 = new MainTurret(MainThirdTurretList, 60, MotorStatorList3, this);
    MainTurretList.Add(Turret3);
    foreach (MainTurret object_Turret in MainTurretList) object_Turret.InitMovement();

    MainSurface = Cockpit.GetSurface(0);
}

public void Main(string argument)
{
    if (argument == "Shoot1") Turret1.IsReadyToShoot = true;
    else if (argument == "Shoot2") Turret2.IsReadyToShoot = true;
    else if (argument == "Shoot3") Turret3.IsReadyToShoot = true;
    else if (argument == "ChangeControl1") Turret1.IsUnderControl = !Turret1.IsUnderControl;
    else if (argument == "ChangeControl2") Turret2.IsUnderControl = !Turret2.IsUnderControl;
    else if (argument == "ChangeControl3") Turret3.IsUnderControl = !Turret3.IsUnderControl;

    foreach (MainTurret Turret in MainTurretList) Turret.Shoot("BurstMode");
    foreach (MainTurret object_Turret in MainTurretList) object_Turret.MoveTurret(Cockpit.RotationIndicator.X, Cockpit.RotationIndicator.Y);

    DrawIndicators();
}

private void DrawIndicators()
{
    MainSurface.ContentType = ContentType.NONE; MainSurface.ContentType = ContentType.SCRIPT;
    Vector2 CenterScreen = new Vector2(MainSurface.SurfaceSize.X / 2, MainSurface.SurfaceSize.Y / 2);

    using (MySpriteDrawFrame Frame = MainSurface.DrawFrame())
    {
        VisualIndicator Turret1VI = new VisualIndicator(new Vector2(CenterScreen.X + 50f, CenterScreen.Y), ContentColor, Turret1.ReloadTime);
        Frame.AddRange(Turret1VI.GetSprite(Turret1.GetIsUnderControl()));
        VisualIndicator Turret2VI = new VisualIndicator(CenterScreen, ContentColor, Turret2.ReloadTime);
        Frame.AddRange(Turret2VI.GetSprite(Turret2.GetIsUnderControl()));
        VisualIndicator Turret3VI = new VisualIndicator(new Vector2(CenterScreen.X - 100f, CenterScreen.Y), ContentColor, Turret3.ReloadTime);
        Frame.AddRange(Turret3VI.GetSprite(Turret3.GetIsUnderControl()));

        MySprite TextBorder = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(50f, 30f), new Vector2(80f, 100f), Color.Green, "", TextAlignment.CENTER, 0f);
        Frame.Add(TextBorder);
        MySprite TextSprite = new MySprite(SpriteType.TEXT, "ИНФО", new Vector2(40f, 15f), null, Color.Blue, "Monospace", TextAlignment.CENTER, 0.4f);
        Frame.Add(TextSprite);
    }
}

//inactive_TurretGun = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(Position.X + 5, Position.Y + 6), new Vector2(30f, 1f), Color, "", TextAlignment.LEFT, 0f);

class MainTurret
{
    Program ExecutingProgram;
    private List<IMySmallMissileLauncher> GunList;
    private List<IMyMotorStator> MotorList;
    private List<IMyMotorStator> ElevationHingeList;
    public IMyMotorStator AzimuthRotor;
    private string ShootMode;
    private int TickCount, CurrentTickCount;
    public double ReloadTime;
    public bool IsReadyToShoot;
    public bool IsUnderControl;

    public MainTurret(List<IMySmallMissileLauncher> GunList, int TickCount, List<IMyMotorStator> MotorList, Program ExecutingProgram)
    {
        this.GunList = GunList;
        ShootMode = "None";
        this.MotorList = MotorList;
        this.ElevationHingeList = new List<IMyMotorStator>();
        this.ExecutingProgram = ExecutingProgram;
        this.TickCount = TickCount;
        this.CurrentTickCount = TickCount;
        this.ReloadTime = 0;
        this.IsReadyToShoot = false;
        this.IsUnderControl = false;
    }

    public void InitMovement()
    {
        foreach (IMyMotorStator object_MotorStator in MotorList)
        {
            if (object_MotorStator.DisplayNameText.Contains("Azimuth")) AzimuthRotor = object_MotorStator;
            else ElevationHingeList.Add(object_MotorStator);
        }
    }

    public void MoveTurret(float RotationIndicator_X, float RotationIndicator_Y)
    {
        if (this.IsUnderControl)
        {
            foreach (IMyMotorStator object_MotorStator in MotorList) object_MotorStator.RotorLock = false;
            AzimuthRotor.TargetVelocityRPM = RotationIndicator_Y * Program.X_SENSIVITY;
            foreach (IMyMotorStator object_ElevationHinge in ElevationHingeList) object_ElevationHinge.TargetVelocityRPM = -RotationIndicator_X * Y_SENSIVITY;
        }
        else foreach (IMyMotorStator object_MotorStator in MotorList) object_MotorStator.RotorLock = true;
    }

    public void Shoot(string ShootMode)
    {
        this.ShootMode = ShootMode;
        if (IsReadyToShoot)
        {
            switch (this.ShootMode)
            {
                case "None": break;
                case "BurstMode":
                    if (ReloadTime == 0)
                    {
                        if (CurrentTickCount >= 0)
                        {
                            try
                            {
                                foreach (IMySmallMissileLauncher object_Gun in this.GunList)
                                    if (this.GunList[CurrentTickCount / (TickCount / GunList.Count)] == object_Gun)
                                    {
                                        object_Gun.ApplyAction("Override_On");
                                        object_Gun.ApplyAction("Shoot_On");
                                    }
                            }
                            catch { ExecutingProgram.Echo("MainTurret.Shoot() - Error"); }
                            CurrentTickCount--;
                        }
                        else
                        {
                            ReloadTime = 900;
                            foreach (IMySmallMissileLauncher object_Gun in this.GunList) object_Gun.ApplyAction("Shoot_Off");
                            this.IsReadyToShoot = false;
                            this.ShootMode = "None";
                            CurrentTickCount = TickCount;
                        }
                    }
                    break;
            }
        }
        GetReloadTime();
    }

    public SpriteForm GetIsUnderControl()
    {
        SpriteForm Form;
        if (this.IsUnderControl) Form = SpriteForm.MainTurretActive; else Form = SpriteForm.MainTurretInactive;
        return Form;
    }

    private double GetReloadTime()
    {
        if (ReloadTime > 0) ReloadTime--;
        return ReloadTime;
    }
}

class VisualIndicator
{
    private Vector2 Position;
    private Color Color;
    private double ReloadTime;

    public VisualIndicator(Vector2 Position, Color Color, double ReloadTime)
    {
        this.Position = Position;
        this.Color = Color;
        this.ReloadTime = ReloadTime;
    }

    public List<MySprite> GetSprite(SpriteForm SpriteForm)
    {
        List<MySprite> SpriteList = new List<MySprite>();
        if (ReloadTime > 0) Color = Color.Yellow;

        switch (SpriteForm)
        {
            case Program.SpriteForm.MainTurretActive:
                MySprite active_Turret1Barret = new MySprite(SpriteType.TEXTURE, "Circle", Position, new Vector2(20f, 20f), Color, "", TextAlignment.CENTER, 0f);
                SpriteList.Add(active_Turret1Barret);
                MySprite active_TurretGun = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(Position.X, Position.Y - 6), new Vector2(35f, 1f), Color, "", TextAlignment.LEFT, 0f);
                SpriteList.Add(active_TurretGun);
                active_TurretGun = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(Position.X, Position.Y - 3), new Vector2(35f, 1f), Color, "", TextAlignment.LEFT, 0f);
                SpriteList.Add(active_TurretGun);
                active_TurretGun = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(Position.X, Position.Y + 3), new Vector2(35f, 1f), Color, "", TextAlignment.LEFT, 0f);
                SpriteList.Add(active_TurretGun);
                active_TurretGun = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(Position.X, Position.Y + 6), new Vector2(35f, 1f), Color, "", TextAlignment.LEFT, 0f);
                SpriteList.Add(active_TurretGun);
                break;
            case Program.SpriteForm.MainTurretInactive:
            MySprite inactive_Turret1Barret = new MySprite(SpriteType.TEXTURE, "Circle", Position, new Vector2(20f, 20f), Color, "", TextAlignment.CENTER, 0f);
                SpriteList.Add(inactive_Turret1Barret);
                MySprite inactive_Turret1BarretHollow = new MySprite(SpriteType.TEXTURE, "Circle", Position, new Vector2(14f, 14f), Color.Black, "", TextAlignment.CENTER, 0f);
                SpriteList.Add(inactive_Turret1BarretHollow);
                MySprite inactive_TurretGun = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(Position.X + 5, Position.Y - 6), new Vector2(30f, 1f), Color, "", TextAlignment.LEFT, 0f);
                SpriteList.Add(inactive_TurretGun);
                inactive_TurretGun = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(Position.X + 7, Position.Y - 3), new Vector2(28f, 1f), Color, "", TextAlignment.LEFT, 0f);
                SpriteList.Add(inactive_TurretGun);
                inactive_TurretGun = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(Position.X + 7, Position.Y + 3), new Vector2(28f, 1f), Color, "", TextAlignment.LEFT, 0f);
                SpriteList.Add(inactive_TurretGun);
                inactive_TurretGun = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(Position.X + 5, Position.Y + 6), new Vector2(30f, 1f), Color, "", TextAlignment.LEFT, 0f);
                SpriteList.Add(inactive_TurretGun);
                break;
        }

        return SpriteList;
    }
}