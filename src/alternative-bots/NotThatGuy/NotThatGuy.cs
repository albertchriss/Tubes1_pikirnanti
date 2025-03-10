using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class NotThatGuy : Bot
{
    Random random = new Random();
    bool movingForward = true;

    private Dictionary<int, double> guessFactors = new Dictionary<int, double>();
    private double enemyLastHeading = 0;

    private bool isRadarLocked = false;
    private bool isMoving = false;  

    static void Main()
    {
        new NotThatGuy().Start();
    }

    NotThatGuy() : base(BotInfo.FromFile("NotThatGuy.json")) { }

    public override void Run()
    {
        BodyColor = Color.FromArgb(0x99, 0x99, 0x99);
        TurretColor = Color.FromArgb(0x88, 0x88, 0x88);
        RadarColor = Color.FromArgb(0x66, 0x66, 0x66);

        AdjustGunForBodyTurn = true;
        AdjustRadarForBodyTurn = true;
        AdjustRadarForGunTurn = true;

        SetTurnRadarRight(double.PositiveInfinity); // Continuous radar sweep
        MoveRandomly(); // only issue new movement if not already turning/moving

        // SetForward(10000);
        // SetTurnRight(10000);

        // while (IsRunning)
        // {
        //     if (TurnRemaining == 0 && DistanceRemaining == 0)
        //     {
        //     }
            // Go();
        // }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double enemyHeadingChange = e.Direction - enemyLastHeading;
        enemyLastHeading = e.Direction;

        double guessFactor = enemyHeadingChange / 10.0;
        guessFactor = Math.Max(-1, Math.Min(1, guessFactor));

        int gfIndex = (int)((guessFactor + 1) * 5);
        if (!guessFactors.ContainsKey(gfIndex))
            guessFactors[gfIndex] = 1;
        else
            guessFactors[gfIndex]++;

        int bestFactorIndex = 0;
        double highestFactor = 0;
        foreach (var entry in guessFactors)
        {
            if (entry.Value > highestFactor)
            {
                highestFactor = entry.Value;
                bestFactorIndex = entry.Key;
            }
        }

        double bulletSpeed = 20 - (3 * GetOptimalFirepower(DistanceTo(e.X, e.Y)));
        double timeToHit = DistanceTo(e.X, e.Y) / bulletSpeed;

        double enemyVelocityX = e.Speed * Math.Cos(DegreesToRadians(e.Direction));
        double enemyVelocityY = e.Speed * Math.Sin(DegreesToRadians(e.Direction));

        double predictedX = e.X + enemyVelocityX * timeToHit;
        double predictedY = e.Y + enemyVelocityY * timeToHit;

        double firingAngle = GunBearingTo(predictedX, predictedY);
        TurnGunLeft(firingAngle);

        double radarOffset = NormalizeAngle(RadarBearingTo(e.X, e.Y));
        TurnRadarLeft(radarOffset);
        isRadarLocked = true;

        double firepower = GetOptimalFirepower(DistanceTo(e.X, e.Y));

        if (GunHeat == 0 && Math.Abs(GunTurnRemaining) < 3.0)
            Fire(firepower);
    }

    public override void OnTick(TickEvent e){
        if (isRadarLocked && Math.Abs(RadarTurnRemaining) < 0.01)
        {
            isRadarLocked = false;
            SetTurnRadarRight(double.PositiveInfinity);
        }
        if (!isMoving || (TurnRemaining == 0 && DistanceRemaining == 0))
        {
            MoveRandomly();
        }
    }

    private void MoveRandomly()
    
    {
        isMoving = true;
        if (IsNearWall())
        {
            SetTurnRight(random.Next(90, 180));
            SetForward(150);
            return;
        }

        if (IsNearBot())
        {
            SetTurnRight(random.Next(45, 90));
            SetBack(100);
            return;
        }

        int moveDistance = random.Next(200, 500);
        int turnAngle = random.Next(45, 180);

        SetForward(moveDistance);
        SetTurnRight(turnAngle);
    }

    private bool IsNearWall()
    {
        return X < 100 || X > ArenaWidth - 100 || Y < 100 || Y > ArenaHeight - 100;
    }

    private bool IsNearBot()
    {
        // Assumes a bot is nearby if we are moving and suddenly slow down
        return Speed < 1.5;
    }

    public override void OnHitWall(HitWallEvent e) => ReverseDirection();
    public override void OnHitBot(HitBotEvent e) => ReverseDirection();

    private void ReverseDirection()
    {
        if (movingForward)
        {
            SetBack(40000);
            movingForward = false;
        }
        else
        {
            SetForward(40000);
            movingForward = true;
        }
    }

    private double NormalizeAngle(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    private double GetOptimalFirepower(double distance)
    {
        if (distance < 200)
            return 3;
        else if (distance < 500)
            return 2;
        else
            return 1;
    }

    private double DegreesToRadians(double degrees) => degrees * (Math.PI / 180);
}

public class TurnCompleteCondition : Condition
{
    private readonly Bot bot;

    public TurnCompleteCondition(Bot bot)
    {
        this.bot = bot;
    }

    public override bool Test()
    {
        return bot.TurnRemaining == 0;
    }
}
