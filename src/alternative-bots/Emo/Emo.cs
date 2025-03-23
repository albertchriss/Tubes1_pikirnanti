using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class Emo : Bot
{
    private readonly static double safeDistance = 25;
    private readonly static double minMoveRange = 100;
    private readonly static double maxMoveRange = 200;
    private readonly static double stopDistance = 5;
    private readonly static int angleDivision = 36;

    private double targetX, targetY;
    private Random randomizer = new Random();
    private List<OpponentInfo> opponentList = new List<OpponentInfo>();
    private Dictionary<int, double> guessFactors = new Dictionary<int, double>();
    private double enemyLastHeading = 0;

    static void Main()
    {
        new Emo().Start();
    }

    Emo() : base(BotInfo.FromFile("Emo.json")) { }

    public override void Run()
    {
        BodyColor = Color.Black;
        TurretColor = Color.Black;
        RadarColor = Color.Black;

        AdjustRadarForBodyTurn = true;
        AdjustRadarForGunTurn = true;
        AdjustGunForBodyTurn = true;

        SetTurnRadarRight(double.PositiveInfinity);
    }

    public override void OnTick(TickEvent e)
    {
        if (DistanceRemaining < stopDistance)
        {
            double optimalX = X, optimalY = Y;
            double minThreat = double.MaxValue;

            for (int i = 0; i < angleDivision; i++)
            {
                double angle = (2 * Math.PI / angleDivision) * i;
                double moveRange = randomizer.NextDouble() * (maxMoveRange - minMoveRange) + minMoveRange;
                double posX = X + moveRange * Math.Cos(angle);
                double posY = Y + moveRange * Math.Sin(angle);

                if (posX < safeDistance || posX > ArenaWidth - safeDistance ||
                    posY < safeDistance || posY > ArenaHeight - safeDistance)
                {
                    continue;
                }

                double threatLevel = EvaluateThreat(posX, posY);
                if (threatLevel < minThreat)
                {
                    minThreat = threatLevel;
                    optimalX = posX;
                    optimalY = posY;
                }
            }
            targetX = optimalX;
            targetY = optimalY;
        }
        double adjustAngle = BearingTo(targetX, targetY) * Math.PI / 180;
        SetTurnLeft(180 / Math.PI * Math.Tan(adjustAngle));
        SetForward(DistanceTo(targetX, targetY) * Math.Cos(adjustAngle));
        SetTurnRadarRight(double.PositiveInfinity);
    }

    private double EvaluateThreat(double candidateX, double candidateY)
    {
        double threatLevel = 0;
        foreach (OpponentInfo opponent in opponentList)
        {
            if (opponent.IsActive)
            {
                double distSquared = Math.Pow(candidateX - opponent.LastX, 2) + Math.Pow(candidateY - opponent.LastY, 2);
                double energyFactor = opponent.LastPower / 100.0;
                double distanceFactor = 1.0 / Math.Max(distSquared, 1e-6);
                double threat = energyFactor * distanceFactor;

                if (distSquared < 10000)
                    threat *= 2;

                threatLevel += threat;
            }
        }
        return threatLevel;
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

        double bestGuessFactor = (bestFactorIndex / 5.0) - 1;

        double bulletSpeed = 20 - (3 * GetOptimalFirepower(DistanceTo(e.X, e.Y)));
        double timeToHit = DistanceTo(e.X, e.Y) / bulletSpeed;

        double enemyVelocityX = e.Speed * Math.Cos(DegreesToRadians(e.Direction));
    double enemyVelocityY = e.Speed * Math.Sin(DegreesToRadians(e.Direction));

    double maxEscapeAngle = Math.Asin(8.0 / bulletSpeed);
    double firingAngleOffset = bestGuessFactor * maxEscapeAngle;

    double predictedX = e.X + (enemyVelocityX * timeToHit) + (Math.Sin(firingAngleOffset) * e.Speed * timeToHit);
    double predictedY = e.Y + (enemyVelocityY * timeToHit) + (Math.Cos(firingAngleOffset) * e.Speed * timeToHit);

    predictedX = Math.Max(25, Math.Min(ArenaWidth - 25, predictedX));
    predictedY = Math.Max(25, Math.Min(ArenaHeight - 25, predictedY));

    double firingAngle = GunBearingTo(predictedX, predictedY);
    SetTurnGunLeft(firingAngle);

    double radarOffset = NormalizeAngle(RadarBearingTo(e.X, e.Y));
    SetTurnRadarLeft(radarOffset);

    double firepower = GetOptimalFirepower(DistanceTo(e.X, e.Y));
    if (GunHeat == 0 && Math.Abs(GunTurnRemaining) < 3.0)
        Fire(firepower);
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

    private double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    private double NormalizeAngle(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
}

public class OpponentInfo
{
    public int BotId { get; set; }
    public double LastX { get; set; }
    public double LastY { get; set; }
    public double LastSpeed { get; set; }
    public double LastHeading { get; set; }
    public double LastPower { get; set; }
    public bool IsActive { get; set; } = true;
    private List<double> movementHistory = new List<double>();

    public void Update(double x, double y, double speed, double heading, double energy)
    {
        LastX = x;
        LastY = y;
        LastSpeed = speed;
        LastHeading = heading;
        LastPower = energy;
        IsActive = true;

        double angleChange = NormalizeAngle(heading - LastHeading);
        movementHistory.Add(angleChange);
        if (movementHistory.Count > 100) movementHistory.RemoveAt(0);
    }

    public double GetGuessFactor()
    {
        if (movementHistory.Count == 0) return 0;
        
        double sum = 0;
        foreach (double move in movementHistory)
            sum += move;

        return Math.Max(-1, Math.Min(1, sum / movementHistory.Count));
    }

    private double NormalizeAngle(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
}