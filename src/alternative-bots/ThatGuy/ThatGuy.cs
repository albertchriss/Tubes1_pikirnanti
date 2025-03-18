using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class ThatGuy : Bot
{
    Random random = new Random();
    bool movingForward = true; // Tracks movement direction

    // GuessFactor Targeting Variables
    private Dictionary<int, double> guessFactors = new Dictionary<int, double>(); // Stores movement history
    private double enemyLastHeading = 0;

    static void Main()
    {
        new ThatGuy().Start();
    }

    ThatGuy() : base(BotInfo.FromFile("ThatGuy.json")) { }

    public override void Run()
    {
        // Set bot colors
        BodyColor = Color.FromArgb(0x99, 0x99, 0x99);
        TurretColor = Color.FromArgb(0x88, 0x88, 0x88);
        RadarColor = Color.FromArgb(0x66, 0x66, 0x66);

        // Infinite scanning
        SetTurnGunRight(double.PositiveInfinity);

        while (IsRunning)
        {
            MoveRandomly();
        }
    }

    private void MoveRandomly()
    {
        // Avoid walls preemptively
        if (IsNearWall())
        {
            SetTurnRight(random.Next(90, 180)); // Large turn away from wall
            SetForward(150);
            WaitFor(new TurnCompleteCondition(this));
            return;
        }

        // Avoid bots preemptively
        if (IsNearBot())
        {
            SetTurnRight(random.Next(45, 90)); // Quick dodge maneuver
            SetBack(100);
            WaitFor(new TurnCompleteCondition(this));
            return;
        }

        // Random movement strategy
        int moveDistance = random.Next(200, 500); // Larger random movement
        int turnAngle = random.Next(45, 180); // Random turn

        SetForward(moveDistance);
        SetTurnRight(turnAngle);
        WaitFor(new TurnCompleteCondition(this));
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

    public override void OnHitWall(HitWallEvent e)
    {
        ReverseDirection();
    }

    public override void OnHitBot(HitBotEvent e)
    {
        ReverseDirection();
    }

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

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double enemyHeadingChange = e.Direction - enemyLastHeading;
        enemyLastHeading = e.Direction;

        // Normalize enemy movement to a GuessFactor scale (-1 to 1)
        double guessFactor = enemyHeadingChange / 10.0;
        guessFactor = Math.Max(-1, Math.Min(1, guessFactor)); // Ensure within range

        // Store movement history
        int gfIndex = (int)((guessFactor + 1) * 5); // Convert -1 to 1 range into 0-10 index
        if (!guessFactors.ContainsKey(gfIndex))
            guessFactors[gfIndex] = 1;
        else
            guessFactors[gfIndex]++;

        // Find the best guess factor to shoot at
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

        // Compute predicted position
        double bulletSpeed = 20 - (3 * GetOptimalFirepower(DistanceTo(e.X, e.Y)));
        double timeToHit = DistanceTo(e.X, e.Y) / bulletSpeed;
        
        // Convert speed & direction into X/Y velocity
        double enemyVelocityX = e.Speed * Math.Cos(DegreesToRadians(e.Direction));
        double enemyVelocityY = e.Speed * Math.Sin(DegreesToRadians(e.Direction));

        double predictedX = e.X + enemyVelocityX * timeToHit;
        double predictedY = e.Y + enemyVelocityY * timeToHit;
        
        double firingAngle = BearingTo(predictedX, predictedY);

        // Adjust bullet power based on distance
        double firepower = GetOptimalFirepower(DistanceTo(e.X, e.Y));

        // Aim and fire
        SetTurnGunRight(firingAngle);
        Fire(firepower);
    }

    private double GetOptimalFirepower(double distance)
    {
        if (distance < 200)
            return 3; // Close range → Maximum firepower
        else if (distance < 500)
            return 2; // Mid-range → Medium firepower
        else
            return 1; // Long range → Low firepower for accuracy
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}

// Condition that is triggered when the turning is complete
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
