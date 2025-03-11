using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

public class Botol : Bot
{
    List<WaveBullet> waves = new List<WaveBullet>();

    Random random = new Random();
    bool movingForward = true;
    int[][] stats;
    int lateralDirection = 1;

    bool isRadarLocked = false;
    bool isMoving = false;

    public static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Botol.json");

        var config = builder.Build();
        var botInfo = BotInfo.FromConfiguration(config);

        new Botol(botInfo).Start();
    }

    public Botol(BotInfo botInfo) : base(botInfo)
    {
        stats = new int[13][];
        for (int i = 0; i < 13; i++)
        {
            stats[i] = new int[31];
        }
    }

    public override void Run()
    {
        BodyColor = Color.Blue;
        TurretColor = Color.Blue;
        RadarColor = Color.Black;
        ScanColor = Color.Yellow;

        AdjustGunForBodyTurn = true;
        AdjustRadarForBodyTurn = true;
        AdjustRadarForGunTurn = true;

        SetTurnRadarRight(double.PositiveInfinity); // Continuous radar sweep
        MoveRandomly(); // only issue new movement if not already turning/moving

        while (IsRunning)
        {
            TurnRadarLeft(10); // Continuously scan using gun-mounted radar
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        double absBearingDeg = Direction + BearingTo(e.X, e.Y);
        double absBearingRad = ToRadians(absBearingDeg);
        double distance = DistanceTo(e.X, e.Y);

        for (int i = waves.Count - 1; i >= 0; i--)
        {
            if (waves[i].CheckHit(e.X, e.Y, TurnNumber))
            {
                waves.RemoveAt(i);
            }
        }

        double power = GetOptimalFirepower(distance);

        if (e.Speed != 0)
        {
            double lateralVelocity = Math.Sin(ToRadians(e.Direction - absBearingDeg)) * e.Speed;
            lateralDirection = lateralVelocity < 0 ? -1 : 1;
        }

        int[] currentStats = stats[Math.Min((int)(distance / 100), stats.Length - 1)];

        int bestIndex = 15;
        for (int i = 0; i < currentStats.Length; i++)
        {
            if (currentStats[i] > currentStats[bestIndex])
                bestIndex = i;
        }

        double guessFactor = (double)(bestIndex - (currentStats.Length - 1) / 2.0) / ((currentStats.Length - 1) / 2.0);
        WaveBullet wave = new WaveBullet(X, Y, absBearingRad, power, lateralDirection, TurnNumber, currentStats);

        double angleOffset = lateralDirection * guessFactor * wave.MaxEscapeAngle();
        double predictedAngle = absBearingRad + angleOffset;

        double predictedX = X + Math.Cos(predictedAngle) * distance;
        double predictedY = Y + Math.Sin(predictedAngle) * distance;
        Console.WriteLine("Predicted X: " + predictedX + " Predicted Y: " + predictedY);


        double gunTurn = GunBearingTo(predictedX, predictedY);
        TurnGunLeft(gunTurn);

        double radarOffset = NormalizeAngle(RadarBearingTo(e.X, e.Y));
        TurnRadarLeft(radarOffset);
        isRadarLocked = true;

        if (GunHeat == 0 && Math.Abs(GunTurnRemaining) < 2.0)
        {
            Fire(power);
            waves.Add(wave);
        }
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

    private double GetOptimalFirepower(double distance)
    {
        if (distance < 200)
            return 3;
        else if (distance < 500)
            return 2;
        else
            return 1;
    }

    private double NormalizeAngle(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180;
    private double ToDegrees(double radians) => radians * 180 / Math.PI;
}

public class WaveBullet
{
    private double startX, startY, startBearing, power;
    private long fireTime;
    private int direction;
    private int[] returnSegment;

    public WaveBullet(double x, double y, double bearing, double power, int direction, long time, int[] segment)
    {
        startX = x;
        startY = y;
        startBearing = bearing;
        this.power = power;
        this.direction = direction;
        fireTime = time;
        returnSegment = segment;
    }

    public double GetBulletSpeed() => 20 - power * 3;

    public double MaxEscapeAngle() => Math.Asin(8 / GetBulletSpeed());

    public bool CheckHit(double enemyX, double enemyY, long currentTime)
    {
        double traveledDistance = (currentTime - fireTime) * GetBulletSpeed();
        double distance = Math.Sqrt((enemyX - startX) * (enemyX - startX) + (enemyY - startY) * (enemyY - startY));

        if (distance <= traveledDistance)
        {
            double desiredDirection = Math.Atan2(enemyY - startY, enemyX - startX);
            double angleOffset = NormalizeRelativeAngle(desiredDirection - startBearing);
            double guessFactor = Math.Max(-1, Math.Min(1, angleOffset / MaxEscapeAngle())) * direction;
            int index = (int)Math.Round((returnSegment.Length - 1) / 2.0 * (guessFactor + 1));

            returnSegment[index]++;
            return true;
        }
        return false;
    }

    private double NormalizeRelativeAngle(double angle)
    {
        // Force angle into -PI .. +PI range
        while (angle > Math.PI) angle -= 2.0 * Math.PI;
        while (angle < -Math.PI) angle += 2.0 * Math.PI;
        return angle;
    }
    private double ToDegrees(double radians) => radians * 180 / Math.PI;
    private double ToRadians(double degrees) => degrees * Math.PI / 180;
}