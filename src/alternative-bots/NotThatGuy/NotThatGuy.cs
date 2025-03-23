using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;


public class NotThatGuy : Bot
{
    Random random = new Random();
    bool movingForward = true;

    private double enemyX = 0;
    private double enemyY = 0;
    private double enemyDirection = 0;
    private double enemySpeed = 0;

    private bool isRadarLocked = false;
    private bool shoot = false;

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
        while (IsRunning){
            MoveRandomly();
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
        double bulletSpeed = 20 - (3 * GetOptimalFirepower(DistanceTo(enemyX, enemyY)));
        double timeToHit = DistanceTo(enemyX, enemyY) / bulletSpeed;

        double enemyVelocityX = enemySpeed * Math.Cos(DegreesToRadians(enemyDirection));
        double enemyVelocityY = enemySpeed * Math.Sin(DegreesToRadians(enemyDirection));

        double predictedX = enemyX + enemyVelocityX * timeToHit;
        double predictedY = enemyY + enemyVelocityY * timeToHit;

        double firingAngle = GunBearingTo(predictedX, predictedY);
        SetTurnGunLeft(firingAngle);


        double radarOffset = NormalizeAngle(RadarBearingTo(enemyX, enemyY));
        SetTurnRadarLeft(radarOffset);
        isRadarLocked = true;

        double firepower = GetOptimalFirepower(DistanceTo(enemyX, enemyY));
        if (GunHeat == 0 && Math.Abs(GunTurnRemaining) < 3.0)
            SetFire(firepower);
        shoot = false;
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        enemyX = e.X;
        enemyY = e.Y;
        enemyDirection = e.Direction;
        enemySpeed = e.Speed;
        shoot = true;
    }

    private void MoveRandomly()
    
    {
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
