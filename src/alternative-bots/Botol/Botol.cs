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
    int[][][] stats;
    int lateralDirection = 1;

    private Dictionary<int, int> botIdx = new Dictionary<int, int>();
    int currIdx = 0;
    bool isRadarLocked = false;

    private bool shoot = false;

    private double enemyX;
    private double enemyY;
    private double enemyDirection;
    private double enemySpeed;

    private int enemyId;

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
        stats = new int[4][][];
        for (int i = 0; i < 4; i++)
        {
            stats[i] = new int[13][];
            for (int j = 0; j < 13; j++)
            {
                stats[i][j] = new int[31];
            }
        }
    }

    public override void Run()
    {
        BodyColor = Color.Black;
        TurretColor = Color.Black;
        RadarColor = Color.Black;
        ScanColor = Color.Black;

        AdjustGunForBodyTurn = true;
        AdjustRadarForBodyTurn = true;
        AdjustRadarForGunTurn = true;

        SetTurnRadarRight(double.PositiveInfinity); // Continuous radar sweep
        while (IsRunning){
            Movement(); 
            if (shoot)
                handleShoot();
            if (isRadarLocked && Math.Abs(RadarTurnRemaining) < 0.01)
            {
                isRadarLocked = false;
                SetTurnRadarRight(double.PositiveInfinity);
            }
            Go();
        }
    }

    private void handleShoot(){
        double absBearingDeg = Direction + BearingTo(enemyX, enemyY);
        double absBearingRad = ToRadians(absBearingDeg);
        double distance = DistanceTo(enemyX, enemyY);

        for (int i = waves.Count - 1; i >= 0; i--)
        {
            if (waves[i].CheckHit(enemyX, enemyY, TurnNumber))
            {
                waves.RemoveAt(i);
            }
        }

        double power = GetOptimalFirepower(distance);

        if (enemySpeed != 0)
        {
            double lateralVelocity = Math.Sin(ToRadians(enemyDirection - absBearingDeg)) * enemySpeed;
            lateralDirection = lateralVelocity < 0 ? -1 : 1;
        }

        int idx;
        if (botIdx.ContainsKey(enemyId))
        {
            idx = botIdx[enemyId];
        }
        else
        {
            idx = currIdx;
            botIdx[enemyId] = currIdx++;
        }


        int[] currentStats = stats[idx][Math.Min((int)(distance / 100), stats.Length - 1)];

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


        double gunTurn = GunBearingTo(predictedX, predictedY);
        SetTurnGunLeft(gunTurn);

        double radarOffset = NormalizeAngle(RadarBearingTo(enemyX, enemyY));
        SetTurnRadarLeft(radarOffset);
        isRadarLocked = true;

        if (GunHeat == 0 && Math.Abs(GunTurnRemaining) < 2.0)
        {
            SetFire(power);
            waves.Add(wave);
        }
        shoot = false;
    }
    public override void OnScannedBot(ScannedBotEvent e)
    {
        enemyX = e.X;
        enemyY = e.Y;
        enemyDirection = e.Direction;
        enemySpeed = e.Speed;
        enemyId = e.ScannedBotId;
        shoot = true;
    }

    private void Movement()
    {
        if (shoot){
            double angleToEnemy = BearingTo(enemyX, enemyY); // radians

            MoveInDirection(angleToEnemy, 120 + random.Next(40));
        }
    }

    private void MoveInDirection(double angle, double distance)
    {
        double turnAngle = NormalizeAngle(angle); 

        SetForward(distance);
        SetTurnLeft(turnAngle);
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