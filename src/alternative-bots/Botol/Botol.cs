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
    int currIdx = 0;
    int turnCount = 0;
    bool isRadarLocked = false;
    private bool shoot = false, foundTarget = false;

    private Dictionary<int, int> botIdx = new Dictionary<int, int>();
    private Dictionary<int, ScannedBotEvent> botDict = new Dictionary<int, ScannedBotEvent>();

    private ScannedBotEvent enemy, targettedEnemy = null;

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
            if (isRadarLocked && Math.Abs(RadarTurnRemaining) < 0.01){
                isRadarLocked = false;
                SetTurnRadarRight(double.PositiveInfinity);
            }
            Go();
        }
    }

    private void handleShoot(){
        double bulletSpeed = 20 - (3 * GetOptimalFirepower(DistanceTo(enemy.X, enemy.Y)));
        double timeToHit = DistanceTo(enemy.X, enemy.Y) / bulletSpeed;

        double enemyVelocityX = enemy.Speed * Math.Cos(ToRadians(enemy.Direction));
        double enemyVelocityY = enemy.Speed * Math.Sin(ToRadians(enemy.Direction));

        double predictedX = enemy.X + enemyVelocityX * timeToHit;
        double predictedY = enemy.Y + enemyVelocityY * timeToHit;

        double firingAngle = GunBearingTo(predictedX, predictedY);
        SetTurnGunLeft(firingAngle);


        double radarOffset = NormalizeAngle(RadarBearingTo(enemy.X, enemy.Y));
        SetTurnRadarLeft(radarOffset);
        isRadarLocked = true;

        double firepower = GetOptimalFirepower(DistanceTo(enemy.X, enemy.Y), enemy.Energy);
        if (GunHeat == 0 && Math.Abs(GunTurnRemaining) < 3.0)
            SetFire(firepower);
        shoot = false;
    }
    public override void OnScannedBot(ScannedBotEvent e)
    {
        enemy = e;
        shoot = true;
    }

    private void Movement()
    {
        if (shoot){
            double angleToEnemy = BearingTo(enemy.X, enemy.Y); // radians

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
        if (Energy < 20)
            return 1; // Save energy when low

        if (distance < 50)
            return 3;
        else if (distance < 300)
            return 2;
        else if (distance < 600)
            return 1.5;
        else
            return 1;
    }

    private double GetOptimalFirepower(double distance, double enemyEnergy)
    {
        double basePower = GetOptimalFirepower(distance);

        // If enemy is almost dead, finish them
        if (enemyEnergy < 4)
            return Math.Min(3, enemyEnergy); // Don't waste power

        return basePower;
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