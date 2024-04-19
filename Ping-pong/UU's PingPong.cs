const string _programName = "[UU's PingPong] Program";
const string _lcdNamePrefix = "<PingPong LCD>";
const string _cockpitNamePrefix = "<PingPong Cockpit>";

List<IMyTextPanel> LCDList = new List<IMyTextPanel>();
IMyCockpit player1, player2;
Platform platform1, platform2;
Ball ball;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    if (Me.CustomName != _programName) Me.CustomName = _programName;

    platform1 = new Platform(new Vector2(20f, 300f), new Vector2(12.5f, 75f), Color.Green, 0.5f);
    platform2 = new Platform(new Vector2(492f, 300f), new Vector2(12.5f, 75f), Color.Red, 0.5f);
    ball = new Ball(new Vector2(256f, 300f), new Vector2(10f, 10f), Color.Yellow, 5f);

    Init();
    StartGame();
}

void Init()
{
    GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(LCDList, (l) => l.DisplayNameText.Contains(_lcdNamePrefix));
    foreach (IMyTextPanel LCD in LCDList)
    {
        LCD.ContentType = ContentType.NONE;
        LCD.ContentType = ContentType.SCRIPT;
        LCD.ScriptBackgroundColor = Color.Black;
    }

    List<IMyCockpit> tempCockpitList = new List<IMyCockpit>();
    GridTerminalSystem.GetBlocksOfType<IMyCockpit>(tempCockpitList, (c) => c.DisplayNameText.Contains(_cockpitNamePrefix));
    if (tempCockpitList.Count == 2)
    {
        player1 = tempCockpitList[0];
        player2 = tempCockpitList[1];
    }
    else
    {
        player1 = null;
        player2 = null;
    }
}

void Main()
{
    Init();

    foreach (IMyTextPanel LCD in LCDList)
    {
        using (MySpriteDrawFrame Frame = LCD.DrawFrame())
        {
            MySprite Field = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 300f), new Vector2(500f, 400f), Color.DarkGray, "", TextAlignment.CENTER, 0f);
            Frame.Add(Field);
            Field = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 300f), new Vector2(498f, 398f), Color.Black, "", TextAlignment.CENTER, 0f);
            Frame.Add(Field);
            Frame.Add(platform1.DrawPlatform());
            Frame.Add(platform2.DrawPlatform());
            Frame.Add(ball.DrawBall());

            if (player1 == null || player2 == null)
            {
                MySprite Error = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 50f), new Vector2(200f, 30f), Color.Red, "", TextAlignment.CENTER, 0f);
                Frame.Add(Error);
                Error = new MySprite(SpriteType.TEXT, "Cockpits not found", new Vector2(256, (float)(50 * 0.75)), null, Color.White, "Debug", TextAlignment.CENTER, 0.75f);
                Frame.Add(Error);
            }
            else
            {
                MySprite ScoreSprite = new MySprite(SpriteType.TEXT, $"{platform1.Score} : {platform2.Score}", new Vector2(256f, 50f), null, Color.White, "Monospace", TextAlignment.CENTER, 1f);
                Frame.Add(ScoreSprite);
                //platform1.Move(player1.RotationIndicator.X);
                platform1.Position.Y = ball.GetPosition().Y;
                platform2.Move(player2.RotationIndicator.X);
                ball.Move(platform1, platform2);
                
                Vector2 BallPos = ball.GetPosition();
                if (BallPos.X < 0 || BallPos.X > 512)
                {
                    if (BallPos.X < 0) platform2.Score++;
                    else if (BallPos.X > 512) platform1.Score++;
                    StartGame();
                }
            }
        }
    }
}

void StartGame()
{
    Random rnd = new Random();
    Vector2 RandomVector = new Vector2((float)rnd.Next(40, 90), (float)rnd.Next(10, 60));
    int temp = rnd.Next(1, 10);
    if (temp % 2 == 0) RandomVector.Y *= -1;
    ball.GoVector = RandomVector;
    ball.ResetPosition();
}

class Platform
{
    public Vector2 Position;
    public Vector2 Size;
    private Color PlatformColor;
    private float SpeedModifier;
    public int Score;

    public Platform(Vector2 Position, Vector2 Size, Color PlatformColor, float SpeedModifier)
    {
        this.Position = Position;
        this.Size = Size;
        this.PlatformColor = PlatformColor;
        this.SpeedModifier = SpeedModifier;
        this.Score = 0;
    }

    public MySprite DrawPlatform()
    {
        return new MySprite(SpriteType.TEXTURE, "SquareSimple", Position, Size, PlatformColor, "", TextAlignment.CENTER, 0f);
    }

    public void Move(float VectorY)
    {
        float prevTopPositionY = (Position.Y - Size.Y / 2);
        float prevBottomPositionY = (Position.Y + Size.Y / 2);
        if (prevTopPositionY + VectorY > 110f && prevBottomPositionY + VectorY < 490)
        {
            Position.Y += VectorY * SpeedModifier;
        }
    }
}

class Ball
{
    private Vector2 Position;
    private Vector2 Size;
    public Vector2 GoVector;
    private Color BallColor;
    private float BallSpeed;

    public Ball(Vector2 Position, Vector2 Size, Color BallColor, float BallSpeed)
    {
        this.Position = Position;
        this.Size = Size;
        this.GoVector = new Vector2(0f, 0f);
        this.BallColor = BallColor;
        this.BallSpeed = BallSpeed;
    }

    public MySprite DrawBall()
    {
        return new MySprite(SpriteType.TEXTURE, "Circle", Position, Size, BallColor, "", TextAlignment.CENTER, 0f);
    }

    public void Move(Platform player1, Platform player2)
    {
        Vector2 tempVector = GoVector;
        tempVector.Normalize();
        Vector2 prevVector = new Vector2(tempVector.X * BallSpeed, tempVector.Y * BallSpeed); // 110 - 490
        
        if (Position.Y + Size.Y >= 500f || Position.Y - Size.Y < 100f)
        {
            GoVector.Y *= -1;
            prevVector.Y *= -1;
        }
        else
        {
            if ((Position.X + Size.X / 2 >= player2.Position.X - player2.Size.X / 2) &&
                ((Position.Y >= player2.Position.Y - player2.Size.Y / 2) && (Position.Y <= player2.Position.Y + player2.Size.Y / 2))
                ||
                (Position.X - Size.X / 2 <= player1.Position.X + player1.Size.X / 2) &&
                ((Position.Y >= player1.Position.Y - player1.Size.Y / 2) && (Position.Y <= player1.Position.Y + player1.Size.Y / 2))) // Coallision
            {
                GoVector.X *= -1;
                prevVector.X *= -1;
            }
        }
        Position += prevVector;
    }

    public Vector2 GetPosition() { return Position; }

    public void ResetPosition() { Position = new Vector2(256f, 300f); }
}