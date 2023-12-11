using System.Numerics;
using Microsoft.Extensions.Configuration;
using WebsocketClient.Wrapper.Entities;

namespace WebsocketClient;

public static class Helpers
{
    private static IConfigurationRoot config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", true, true)
        .AddEnvironmentVariables().Build();
    
    /// <summary>
    /// Get the difference between two coordinates
    /// </summary>
    /// <param name="origin">the origin (source) coordinates for the calculation</param>
    /// <param name="target">the target coordinates for the calculation</param>
    /// <returns>a vector representing the difference from origin to target</returns>
    public static Coordinates GetCoordinateDifference(Coordinates origin, Coordinates target)
    {
        return new Coordinates
        {
            X = target.X - origin.X,
            Y = target.Y - origin.Y
        };
    }

    /// <summary>
    /// Get a compass direction most closely representing the given vector
    /// </summary>
    /// <param name="vector">the vector which should be converted to approximate compass direction</param>
    /// <returns>the compass direction closest to the vector</returns>
    /// <exception cref="Exception">If you give a vector that breaks 2d space an exception is raised</exception>
    public static CompassDirection GetApproximateDirection(Coordinates vector)
    {
        var angle = GetAngleDegrees(vector);
        const float cutoff = 22.5F;
        return angle switch
        {
            >= 15 * cutoff or < cutoff => CompassDirection.North,
            >= cutoff and < 3 * cutoff => CompassDirection.NorthEast,
            >= 3 * cutoff and < 5 * cutoff => CompassDirection.East,
            >= 5 * cutoff and < 7 * cutoff => CompassDirection.SouthEast,
            >= 7 * cutoff and < 9 * cutoff => CompassDirection.South,
            >= 9 * cutoff and < 11 * cutoff => CompassDirection.SouthWest,
            >= 11 * cutoff and < 13 * cutoff => CompassDirection.West,
            >= 13 * cutoff and < 15 * cutoff => CompassDirection.NorthWest,
            _ => throw new Exception($"Could not determine direction for vector {vector}")
        };
    }
    
    private static double GetAngleDegrees(Coordinates vector)
    {
        var angleRadians = Math.Atan2(vector.Y, -vector.X);
        var angleDegrees = angleRadians * 180 / Math.PI;
        // C# doesn't have a modulo operator, % is remainder, we have to do this to ensure degrees is between 0 and 360
        return angleDegrees < 0 ? angleDegrees + 360 : angleDegrees;
    }

    /// <summary>
    /// Get a compass direction most closely representing the given vector
    /// </summary>
    /// <param name="vector">the vector which should be converted to approximate compass direction</param>
    /// <returns>the compass direction closest to the vector</returns>
    /// <exception cref="Exception">If you give a vector that breaks 2d space an exception is raised</exception>
    public static CompassDirection GetApproximateDirection(Vector2 vector)
    {
        Vector2 East = new Vector2(1, 0);
        double dot = (double)Vector2.Dot(vector, East);
        double det = (double)(vector.X * East.Y - vector.Y * East.X);
        double angle = (double)Math.Atan2((double)det, (double)dot);
        var angleDegrees = angle * 180 / Math.PI;       
        angleDegrees = angleDegrees < 0 ? angleDegrees + 360 : angleDegrees;
        const float cutoff = 22.5F;

        return angleDegrees switch
        {
            >= 15 * cutoff or < cutoff => CompassDirection.East,
            >= cutoff and < 3 * cutoff => CompassDirection.NorthEast,
            >= 3 * cutoff and < 5 * cutoff => CompassDirection.North,
            >= 5 * cutoff and < 7 * cutoff => CompassDirection.NorthWest,
            >= 7 * cutoff and < 9 * cutoff => CompassDirection.West,
            >= 9 * cutoff and < 11 * cutoff => CompassDirection.SouthWest,
            >= 11 * cutoff and < 13 * cutoff => CompassDirection.South,
            >= 13 * cutoff and < 15 * cutoff => CompassDirection.SouthEast,
            _ => throw new Exception($"Could not determine direction for vector {vector}")
        };
    }

    /// <summary>
    /// Get coordinates for a given entity from the given game map
    /// </summary>
    /// <param name="entityId">the id of the entity to search for in the map</param>
    /// <param name="map">the game map to search for the entity in</param>
    /// <returns>the entity coordinates if the entity exists, otherwise null</returns>
    public static Coordinates? GetEntityCoordinates(string entityId, Cell[][] map)
    {
        for (var y = 0; y < map.Length; y++)
        {
            for (var x = 0; x < map[y].Length; x++)
            {
                var cell = map[y][x];
                if (cell.ShipData?.Id == entityId)
                {
                    return new Coordinates { X = x, Y = y };
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get the compass direction that is the furthest one you are allowed to turn towards from the given starting
    /// direction, given the turn rate.
    /// </summary>
    /// <param name="startingDirection">the starting direction for the turn</param>
    /// <param name="targetDirection">the target direction for the turn</param>
    /// <param name="turnRate">the turn rate for the game, see ClientContext.turnRate</param>
    /// <returns>the furthest direction between starting and target directions allowed by the turn rate</returns>
    /// <remarks>If performing a 180-degree turn, the function will always perform the partial turn clockwise</remarks>
    public static CompassDirection GetPartialTurn(CompassDirection startingDirection, CompassDirection targetDirection,
        int turnRate)
    {
        var initialTurn = (targetDirection - startingDirection) % 8;
        initialTurn = initialTurn < 0 ? initialTurn + 8 : initialTurn; // Again, no modulo operator

        if (initialTurn <= 4) // turning clockwise
        {
            return (CompassDirection)((int)startingDirection + int.Min(initialTurn, turnRate));
        }

        // turning counter-clockwise
        initialTurn -= 8;
        return (CompassDirection)((int)startingDirection + int.Max(initialTurn, -turnRate));
    }

    /// <summary>
    /// Get the id of your ship
    /// </summary>
    /// <returns>The ship id as string</returns>
    public static string GetOwnShipId()
    {
        return $"ship:{config["Client:Token"]}:{config["Client:BotName"]}";
    }

    public static double RadiansToDegrees(double radians)
    {
        double degrees = (180 / Math.PI) * radians;
        return (degrees);
    }
}