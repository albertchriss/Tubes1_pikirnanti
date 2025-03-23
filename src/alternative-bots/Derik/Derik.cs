using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

public class Derik : Bot
{
    Random random = new Random();
    bool isRadarLocked = false;
    private bool shoot = false;

    private bool maju = true;
    private Dictionary<int, ScannedBotEvent> botDict = new Dictionary<int, ScannedBotEvent>();

    private ScannedBotEvent enemy;

    public static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Derik.json");

        var config = builder.Build();
        var botInfo = BotInfo.FromConfiguration(config);

        new Derik(botInfo).Start();
    }

    public Derik(BotInfo botInfo) : base(botInfo) {}

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
        SetForward(100);
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
    private double preferredDist = 25, offset = 10;

    int wait = 50;

    public override void OnTick(TickEvent e){
        if (wait > 0)
            wait--;
        
        if (wait <= 0){
            maju = !maju;
            wait = random.Next(30, 75);
        }
    }

    private void Movement()
    {   
        if (IsNearWall() && wait <= 0){
            wait = 50;
            maju = !maju;
        }

        if (maju) SetForward(100);
        else SetBack(100);

        if (shoot){
            double angleToEnemy = BearingTo(enemy.X, enemy.Y);
            if (!maju) angleToEnemy += 180;

            int a;
            if (DistanceTo(enemy.X, enemy.Y) < preferredDist - offset ){
                a = 150;
            }
            else if (DistanceTo(enemy.X, enemy.Y) > preferredDist + offset){
                a = 60;
            }
            else{
                a = 90;
            }
            if (angleToEnemy > 0)
                angleToEnemy -= a;
            else 
                angleToEnemy += a;
            MoveInDirection(angleToEnemy);
        }
    }

    public override void OnHitWall(HitWallEvent e)
    {
        maju = !maju;
        wait = 50;
    }

    private bool IsNearWall()
    {
        int batas = 50;
        return X < batas || X > ArenaWidth - batas || Y < batas || Y > ArenaHeight - batas;
    }

    private void MoveInDirection(double angle)
    {
        double turnAngle = NormalizeAngle(angle); 
        // if (maju){
        SetTurnLeft(turnAngle);
        // }
        // else{
        //     SetTurnRight(turnAngle);
        // }
        // SetForward(distance);
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
            return Math.Min(3, Math.Max(enemyEnergy, 0.1)); // Don't waste power

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