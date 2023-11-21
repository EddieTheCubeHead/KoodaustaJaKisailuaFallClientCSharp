using WebsocketClient.Wrapper.Entities;

namespace WebsocketClient;

public static class Helpers
{
    public static Coordinates GetCoordinateDifference(Coordinates origin, Coordinates target)
    {
        return new Coordinates
        {
            X = target.X - origin.X,
            Y = target.Y - origin.Y
        };
    }

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

    public static Coordinates GetEntityCoordinates(string entityId, Cell[][] map)
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

        throw new Exception($"Could not find entity with id {entityId} in map.");
    }

    public static CompassDirection GetPartialTurn(CompassDirection startingDirection, CompassDirection targetDirection,
        TeamAiContext context)
    {
        var initialTurn = (targetDirection - startingDirection) % 8;
        initialTurn = initialTurn < 0 ? initialTurn + 8 : initialTurn; // Again, no modulo operator

        if (initialTurn <= 4) // turning clockwise
        {
            return (CompassDirection)((int)startingDirection + int.Min(initialTurn, context.TurnRate));
        }

        // turning counter-clockwise
        initialTurn -= 8;
        return (CompassDirection)((int)startingDirection + int.Max(initialTurn, -context.TurnRate));
    }
}