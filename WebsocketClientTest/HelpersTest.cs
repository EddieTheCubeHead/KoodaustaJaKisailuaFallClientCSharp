﻿using WebsocketClient;
using WebsocketClient.Wrapper.Entities;

namespace WebsocketClientTest;

[TestFixture]
public class GetCoordinateDifferenceTest
{
    [Test]
    public void ShouldReturnZeroVectorIfOriginAndTargetAreTheSame()
    {
        var origin = new Coordinates { X = 3, Y = -5 };

        var difference = Helpers.GetCoordinateDifference(origin, origin);

        Assert.That(difference, Is.EqualTo(new Coordinates { X = 0, Y = 0 }));
    }

    [Test]
    public void ShouldReturnVectorPointingFromOriginToTargetIfNotSameCoordinates()
    {
        var origin = new Coordinates { X = -3, Y = 4 };
        var target = new Coordinates { X = -5, Y = -2 };

        var difference = Helpers.GetCoordinateDifference(origin, target);

        Assert.That(difference, Is.EqualTo(new Coordinates { X = -2, Y = -6 }));
    }
}

[TestFixture]
public class GetApproximateDirectionTest
{
    [TestCase(-1, 0, CompassDirection.North)]
    [TestCase(-1, 1, CompassDirection.NorthEast)]
    [TestCase(0, 1, CompassDirection.East)]
    [TestCase(1, 1, CompassDirection.SouthEast)]
    [TestCase(1, 0, CompassDirection.South)]
    [TestCase(1, -1, CompassDirection.SouthWest)]
    [TestCase(0, -1, CompassDirection.West)]
    [TestCase(-1, -1, CompassDirection.NorthWest)]
    public void ShouldReturnTheCorrectCompassDirectionForExactDirections(int x, int y, 
        CompassDirection expectedDirection)
    {
        var direction = Helpers.GetApproximateDirection(new Coordinates { X = x, Y = y });

        Assert.That(direction, Is.EqualTo(expectedDirection));
    }
    
    [TestCase(-5, 1, CompassDirection.North)]
    [TestCase(-5, -1, CompassDirection.North)]
    [TestCase(-4, 5, CompassDirection.NorthEast)]
    [TestCase(-5, 4, CompassDirection.NorthEast)]
    [TestCase(-1, 5, CompassDirection.East)]
    [TestCase(1, 5, CompassDirection.East)]
    [TestCase(5, 4, CompassDirection.SouthEast)]
    [TestCase(4, 5, CompassDirection.SouthEast)]
    [TestCase(5, 1, CompassDirection.South)]
    [TestCase(5, -1, CompassDirection.South)]
    [TestCase(5, -4, CompassDirection.SouthWest)]
    [TestCase(4, -5, CompassDirection.SouthWest)]
    [TestCase(-1, -5, CompassDirection.West)]
    [TestCase(1, -5, CompassDirection.West)]
    [TestCase(-5, -4, CompassDirection.NorthWest)]
    [TestCase(-4, -5, CompassDirection.NorthWest)]
    public void ShouldGiveClosestCompassDirectionIfNotExact(int x, int y, 
        CompassDirection expectedDirection)
    {
        var direction = Helpers.GetApproximateDirection(new Coordinates { X = x, Y = y });

        Assert.That(direction, Is.EqualTo(expectedDirection));
    }
}

[TestFixture]
public class GetEntityCoordinatesTest
{
    [Test]
    public void ShouldGiveEntityCoordinatesForSingleCellEntity()
    {
        var emptyCell = new Cell { CellType = CellType.Empty };
        var shipCell = new Cell
        {
            CellType = CellType.Ship,
            ShipData = new ShipData { Id = "entity", Position = new Coordinates { X = 1, Y = 2 } }
        };
        var gameMap = new[]
        {
            new[] { emptyCell, emptyCell, emptyCell, emptyCell }, 
            new[] { emptyCell, emptyCell, emptyCell, emptyCell }, 
            new[] { emptyCell, shipCell,  emptyCell, emptyCell },
            new[] { emptyCell, emptyCell, emptyCell, emptyCell }
        };
        
        var actualCoordinates = Helpers.GetEntityCoordinates("entity", gameMap);
        
        Assert.That(actualCoordinates, Is.EqualTo(new Coordinates { X = 1, Y = 2 }));
    }

    [Test]
    public void ShouldIgnoreHitBoxCoordinatesAndReturnActualEntity()
    {
        var emptyCell = new Cell { CellType = CellType.Empty };
        var hitBoxCell = new Cell
        {
            CellType = CellType.HitBox,
            HitBoxData = new HitBoxData { EntityId = "entity" }
        };
        var shipCell = new Cell
        {
            CellType = CellType.Ship,
            ShipData = new ShipData { Id = "entity", Position = new Coordinates { X = 3, Y = 1 } }
        };
        var gameMap = new[]
        {
            new[] { emptyCell, emptyCell, hitBoxCell, hitBoxCell, hitBoxCell }, 
            new[] { emptyCell, emptyCell, hitBoxCell, shipCell,   hitBoxCell }, 
            new[] { emptyCell, emptyCell, hitBoxCell, hitBoxCell, hitBoxCell }, 
            new[] { emptyCell, emptyCell, emptyCell,  emptyCell,  emptyCell },
            new[] { emptyCell, emptyCell, emptyCell,  emptyCell,  emptyCell }
        };
        
        var actualCoordinates = Helpers.GetEntityCoordinates("entity", gameMap);
        
        Assert.That(actualCoordinates, Is.EqualTo(new Coordinates { X = 3, Y = 1 }));
    }
}

[TestFixture]
public class GetPartialTurnTest
{
    [Test]
    public void ShouldReturnGivenDirectionIfTurnIsNotTooSharp()
    {
        var direction = Helpers.GetPartialTurn(CompassDirection.North, CompassDirection.East, 2);

        Assert.That(direction, Is.EqualTo(CompassDirection.East));
    }

    [Test]
    public void ShouldReturnLessSharpTurnIfTurnIsTooSharp()
    {
        var direction = Helpers.GetPartialTurn(CompassDirection.North, CompassDirection.SouthEast, 2);
        
        Assert.That(direction, Is.EqualTo(CompassDirection.East));
    }

    [Test]
    public void ShouldTurnClockwiseIfHalfCircleTurnRequired()
    {
        var direction = Helpers.GetPartialTurn(CompassDirection.NorthEast, CompassDirection.SouthWest, 2);
        
        Assert.That(direction, Is.EqualTo(CompassDirection.SouthEast));
    }
    
    [Test]
    public void ShouldFunctionCorrectlyForCounterClockWiseTurns()
    {
        var direction = Helpers.GetPartialTurn(CompassDirection.NorthEast, CompassDirection.SouthWest, 2);

        Assert.That(direction, Is.EqualTo(CompassDirection.North));
    }
}

[TestFixture]
public class GetNearestDirectionWeCanTurnToTest
{
    [Test]
    public void ShouldReturnGivenDirectionIfTurnIsNotTooSharp()
    {
        var direction = TeamAi.GetNearestDirectionWeCanTurnTo(CompassDirection.North, CompassDirection.East, 1);

        Assert.That(direction, Is.EqualTo(CompassDirection.NorthEast));
    }

    [Test]
    public void ShouldReturnLessSharpTurnIfTurnIsTooSharp()
    {
        var direction = TeamAi.GetNearestDirectionWeCanTurnTo(CompassDirection.North, CompassDirection.SouthEast, 1);
        
        Assert.That(direction, Is.EqualTo(CompassDirection.NorthEast));
    }

    [Test]
    public void ShouldTurnClockwiseIfHalfCircleTurnRequired()
    {
        var direction = TeamAi.GetNearestDirectionWeCanTurnTo(CompassDirection.NorthEast, CompassDirection.SouthWest, 1);
        
        Assert.That(direction, Is.EqualTo(CompassDirection.East));
    }
    
    [Test]
    public void ShouldFunctionCorrectlyForCounterClockWiseTurns()
    {
        var direction = TeamAi.GetNearestDirectionWeCanTurnTo(CompassDirection.NorthEast, CompassDirection.West, 1);
        
        Assert.That(direction, Is.EqualTo(CompassDirection.North));
    }   
    
    [TestCase(CompassDirection.North, CompassDirection.West, 2, CompassDirection.West)]
    [TestCase(CompassDirection.North, CompassDirection.SouthWest, 2, CompassDirection.West)]
    [TestCase(CompassDirection.NorthWest, CompassDirection.SouthEast, 3, CompassDirection.SouthEast)]
    [TestCase(CompassDirection.NorthWest, CompassDirection.SouthEast, 4, CompassDirection.SouthEast)]
    [TestCase(CompassDirection.North, CompassDirection.SouthEast, 4, CompassDirection.SouthEast)]
    [TestCase(CompassDirection.North, CompassDirection.SouthEast, 2, CompassDirection.East)]
    [TestCase(CompassDirection.NorthWest, CompassDirection.NorthEast, 2, CompassDirection.NorthEast)]
    public void ShouldFunctionCorrectlyForCounterClockWiseTurns(
        CompassDirection shipDirection, CompassDirection toTurnTo, int maxTurns, CompassDirection expected)
    {
        var direction = TeamAi.GetNearestDirectionWeCanTurnTo(shipDirection, toTurnTo, maxTurns);
        
        Assert.That(direction, Is.EqualTo(expected));
    }

}