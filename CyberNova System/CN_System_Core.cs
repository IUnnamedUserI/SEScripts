// CyberNova SYSTEM CORE v0.1

const float CURSOR_SENSIVITY = 0.75f;
string MY_PREFIX = string.Empty;

List<IMyTextPanel> DisplayList;
List<Application> appList = new List<Application>();
List<MySprite> SaveCurrentScreen = new List<MySprite>(); // Сохранение текущего окна

IMyCockpit ControlSeat;

Cursor cursor;
Color BackgroundColor = new Color(15, 15, 15);

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    Init();
}

void Init() {
    MY_PREFIX = Me.DisplayNameText.Split('>')[0] + ">";
    cursor = new Cursor();
    DisplayList = new List<IMyTextPanel>();
    try { ControlSeat = (IMyCockpit)GridTerminalSystem.GetBlockWithName("<CN> Control Seat"); } catch { Echo("Control Seat not found"); }
}

void ReInit() {
    List<IMyProgrammableBlock> PBList = new List<IMyProgrammableBlock>();
    appList.Clear();

    GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(DisplayList, (d) => d.DisplayNameText.Contains(MY_PREFIX));
    GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(PBList, (p) => p.DisplayNameText.Contains(MY_PREFIX));

    int count = 0;
    foreach (IMyProgrammableBlock Object_PB in PBList) {
        if (Object_PB != Me)
            appList.Add(new Application(new Vector2(36f + 120f * (float)Math.Floor((float)count / 4f), 36f + 120 * (count % 4)), new List<MySprite>(), Object_PB.DisplayNameText.Substring(MY_PREFIX.Length), Object_PB));
        count++;
    }
}

public void Main(string argument, UpdateType updateSource) {
    ReInit();

    if (argument.Contains("Install('")) {
        string appName = argument.Split("'")[1];
        
        try {
            IMyProgrammableBlock appBlock = (IMyProgrammableBlock)GridTerminalSystem.GetBlockWithName(appName);
            appList.Add(new Application(new Vector2(36f + 120f * (float)Math.Floor((float)appList.Count / 4f), 36f + 120 * (appList.Count % 4)), new List<MySprite>(), appBlock.DisplayNameText, appBlock));
        } catch { Echo($"Программный блок {appName} не был найден"); }
    }

    if (ControlSeat != null) {
        cursor.Move(new Vector2(ControlSeat.RotationIndicator.Y * CURSOR_SENSIVITY, ControlSeat.RotationIndicator.X * CURSOR_SENSIVITY));
        foreach (Application Object_App in appList) Object_App.CheckCursor(cursor.GetPosition());
    }

    foreach (IMyTextPanel Object_Display in DisplayList) {
        Object_Display.ContentType = ContentType.NONE;
        Object_Display.ContentType = ContentType.SCRIPT;
        Object_Display.ScriptBackgroundColor = BackgroundColor;

        using (MySpriteDrawFrame Frame = Object_Display.DrawFrame()) {
            if (ControlSeat.MoveIndicator.Y < 0) { // Key_C
                    // Очистка всех спрайтов дисплея

                    Frame.AddRange(SaveCurrentScreen);
                    SaveCurrentScreen.Clear();
                }

            Frame.Add(new MySprite(SpriteType.TEXT, "Cyber", new Vector2(256f * (Object_Display.SurfaceSize.X / 512), 180f), null, Color.Purple, "Debug", TextAlignment.CENTER, 3f));
            Frame.Add(new MySprite(SpriteType.TEXT, "OS", new Vector2(256f * (Object_Display.SurfaceSize.X / 512), 250f), null, Color.Purple, "Debug", TextAlignment.CENTER, 2f));
            foreach (Application Object_App in appList) Frame.AddRange(Object_App.Create());
            Frame.Add(cursor.DrawCursor(Object_Display.TextureSize));

            Frame.Add(new MySprite(SpriteType.TEXT, cursor.GetPosition().ToString(), new Vector2(400f, 400f), null, Color.White, "Debug", TextAlignment.CENTER, 0.75f));
        }
    }
}

class Cursor {
    private Vector2 Position;
    private Color Color;

    public Cursor() {
        this.Position = new Vector2(256f, 256f);
        this.Color = new Color(255, 255, 255);
    }

    public MySprite DrawCursor(Vector2 SurfaceSize) {
        return new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(Position.X * (float)(SurfaceSize.X / 512), Position.Y), new Vector2(12f, 12f), Color, "", TextAlignment.CENTER, 0f);
    }

    public void Move(Vector2 MoveVector) {
        if (Position.X + MoveVector.X > 0 && Position.X + MoveVector.X < 512) Position.X += MoveVector.X;
        if (Position.Y + MoveVector.Y > 0 && Position.Y + MoveVector.Y < 512) Position.Y += MoveVector.Y;
    }

    public Vector2 GetPosition() {
        return Position;
    }
}

class Application {
    private Vector2 Position;
    private List<MySprite> Icon;
    private string Title;
    private Color BorderColor;
    private IMyProgrammableBlock PB_Parent;

    public Application(Vector2 Position, List<MySprite> Icon, string Title, IMyProgrammableBlock PB_Parent) {
        Vector2 offset = new Vector2(24f, 24f);
        this.Position = Position + offset;
        this.Icon = Icon;
        this.Title = Title;
        this.PB_Parent = PB_Parent;
        this.BorderColor = Color.Gray;
    }

    public List<MySprite> Create() {
        string finalTitle = Title;
        if (Title.Length > 13) finalTitle = Title.Substring(0, 11) + "...";

        List<MySprite> resultSpriteList = new List<MySprite>();
        resultSpriteList.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", Position, new Vector2(96f, 112f), BorderColor, "", TextAlignment.CENTER, 0f));
        resultSpriteList.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", Position, new Vector2(94f, 110f), new Color(15, 15, 15), "", TextAlignment.CENTER, 0f));
        resultSpriteList.Add(new MySprite(SpriteType.TEXT, finalTitle, new Vector2(Position.X, Position.Y + 36f), null, Color.White, "Debug", TextAlignment.CENTER, 0.5f));
        return resultSpriteList;
    }

    public void CheckCursor(Vector2 CursorPosition) {
        if (Position.X - 48f <= CursorPosition.X && Position.X + 48f >= CursorPosition.X &&
            Position.Y - 56f <= CursorPosition.Y && Position.Y + 56f >= CursorPosition.Y) {
                BorderColor = Color.Red;
            }
        else BorderColor = Color.Gray;
    }
}