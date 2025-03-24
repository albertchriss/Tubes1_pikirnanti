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
    Random random = new Random();
    bool isRadarLocked = false;
    private bool shoot = false;

    private ScannedBotEvent enemy;

    public static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Botol.json");

        var config = builder.Build();
        var botInfo = BotInfo.FromConfiguration(config);

        new Botol(botInfo).Start();
    }

    public Botol(BotInfo botInfo) : base(botInfo){}

    public override void Run()
    {
        BodyColor = Color.Red;
        TurretColor = Color.Red;
        RadarColor = Color.Red;
        ScanColor = Color.Red;

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
        double bulletSpeed = 20 - (3 * GetOptimalFirepower(DistanceTo(enemy.X, enemy.Y), enemy.Energy));
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
            double angleToEnemy = BearingTo(enemy.X, enemy.Y); 

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

        if (enemyEnergy < 4)
            return Math.Min(3, Math.Max(enemyEnergy, 0.1)); 

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
