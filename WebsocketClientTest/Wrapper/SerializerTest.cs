using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
using Newtonsoft.Json;
using WebsocketClient.Entities;
using WebsocketClient.Wrapper;
using WebsocketClient.Wrapper.Entities;

namespace WebsocketClientTest.Wrapper;

[TestFixture]
public class SerializerTest
{
    #region Setup
    
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Serializer _serializer;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    
    private record TestActionData : IActionData
    {
        public string Serialize()
        {
            return "{\"testPayload\": \"test\"}";
        }
    }
    
    [SetUp]
    public void BeforeEach()
    {
        _serializer = new Serializer();
    }
    
    #endregion
    
    #region Tests

    [Test]
    public void ShouldSerializeCommandToTypeAndInterfaceSerializeCallResult()
    {
        var command = new Command{Action = ActionType.Shoot, Payload = new TestActionData()};

        var commandJson = _serializer.SerializeCommand(command);

        Assert.AreEqual("{\"action\": \"shoot\", \"payload\": {\"testPayload\": \"test\"}}", commandJson);
    }

    [Test]
    public void ShouldDeserializeCellWithNoDataBasedOnCellType()
    {
        var partlyDeserializedState = ConstructStateJson(new[]
        {
            ("Empty", "{}"),
            ("OutOfVision", "{}"),
            ("AudioSignature", "{}")
        });
        
        var gameState = _serializer.DeserializeGameState(partlyDeserializedState);
        Assert.Multiple(() =>
        {
            Assert.That(gameState.GameMap[0][0].CellType, Is.EqualTo(CellType.Empty));
            Assert.That(gameState.GameMap[0][1].CellType, Is.EqualTo(CellType.OutOfVision));
            Assert.That(gameState.GameMap[0][2].CellType, Is.EqualTo(CellType.AudioSignature));
        });
        
        Assert.Multiple(() =>
        {
            foreach (var cell in gameState.GameMap[0])
            {
                Assert.That(cell.HitBoxData, Is.Null);
                Assert.That(cell.ShipData, Is.Null);
                Assert.That(cell.ProjectileData, Is.Null);
            }
        });
    }

    [Test]
    public void ShouldSetEntityIdOnHitBoxDeserialization()
    {
        const string hitBoxEntityId = "myShipId";
        var partlyDeserializedState = ConstructStateJson(new[]
        {
            ("HitBox", $"{{\"entityId\": \"{hitBoxEntityId}\"}}")
        });
        
        var gameState = _serializer.DeserializeGameState(partlyDeserializedState);
        Assert.Multiple(() =>
        {
            Assert.That(gameState.GameMap[0][0].CellType, Is.EqualTo(CellType.HitBox));
            Assert.That(gameState.GameMap[0][0].HitBoxData?.EntityId, Is.EqualTo(hitBoxEntityId));
        });
    }

    [Test]
    public void ShouldSetShipDataOnShipDeserialization()
    {
        const string shipEntityId = "myShipId";
        const int xCoordinate = 1;
        const int yCoordinate = 2;
        const int shipHealth = 10;
        const int shipHeat = 3;
        var shipData =
            $"{{\"id\": \"{shipEntityId}\", " +
            $"\"position\": {{\"x\": {xCoordinate}, \"y\": {yCoordinate}}}, " +
            $"\"direction\": \"ne\", " +
            $"\"health\": {shipHealth}, " +
            $"\"heat\": {shipHeat}}}";
        var partlyDeserializedState = ConstructStateJson(new[]
        {
            ("ship", shipData)
        });
        
        var gameState = _serializer.DeserializeGameState(partlyDeserializedState);
        
        Assert.Multiple(() =>
        {
            Assert.That(gameState.GameMap[0][0].CellType, Is.EqualTo(CellType.Ship));
            Assert.That(gameState.GameMap[0][0].ShipData?.Id, Is.EqualTo(shipEntityId));
            Assert.That(gameState.GameMap[0][0].ShipData?.Direction, Is.EqualTo(CompassDirection.NorthEast));
            Assert.That(gameState.GameMap[0][0].ShipData?.Position.X, Is.EqualTo(xCoordinate));
            Assert.That(gameState.GameMap[0][0].ShipData?.Position.Y, Is.EqualTo(yCoordinate));
            Assert.That(gameState.GameMap[0][0].ShipData?.Health, Is.EqualTo(shipHealth));
            Assert.That(gameState.GameMap[0][0].ShipData?.Heat, Is.EqualTo(shipHeat));
        });
    }

    [Test]
    public void ShouldSetProjectileDataOnProjectileDeserialization()
    {
        const string projectileEntityId = "projectileId";
        const int xCoordinate = 5;
        const int yCoordinate = 3;
        const int velocity = 4;
        const int mass = 2;
        var projectileData =
            $"{{\"id\": \"{projectileEntityId}\", " +
            $"\"position\": {{\"x\": {xCoordinate}, \"y\": {yCoordinate}}}, " +
            $"\"direction\": \"sw\", " +
            $"\"velocity\": {velocity}, " +
            $"\"mass\": {mass}}}";
        var partlyDeserializedState = ConstructStateJson(new[]
        {
            ("projectile", projectileData)
        });
        
        var gameState = _serializer.DeserializeGameState(partlyDeserializedState);
        
        Assert.Multiple(() =>
        {
            Assert.That(gameState.GameMap[0][0].CellType, Is.EqualTo(CellType.Projectile));
            Assert.That(gameState.GameMap[0][0].ProjectileData?.Id, Is.EqualTo(projectileEntityId));
            Assert.That(gameState.GameMap[0][0].ProjectileData?.Direction, Is.EqualTo(CompassDirection.SouthWest));
            Assert.That(gameState.GameMap[0][0].ProjectileData?.Position.X, Is.EqualTo(xCoordinate));
            Assert.That(gameState.GameMap[0][0].ProjectileData?.Position.Y, Is.EqualTo(yCoordinate));
            Assert.That(gameState.GameMap[0][0].ProjectileData?.Velocity, Is.EqualTo(velocity));
            Assert.That(gameState.GameMap[0][0].ProjectileData?.Mass, Is.EqualTo(mass));
        });
    }

    [Test]
    public void ShouldDeserializeTheWholeMatrixAtATime()
    {
        const string oneCellData = "\"type\": \"Empty\", \"data\": {}";
        var oneRowData = "[" + string.Join(", ", Enumerable.Repeat($"{{{oneCellData}}}", 10)) + "]";
        var mapMatrix = "[" + string.Join(", ", Enumerable.Repeat(oneRowData, 10)) + "]";
        var stateJson = $"{{\"turnNumber\": 1, \"gameMap\": {mapMatrix}}}";
        var partlyDeserializedStateJson = JsonConvert.DeserializeObject(stateJson);
        
        var gameState = _serializer.DeserializeGameState(partlyDeserializedStateJson);

        Assert.Multiple(() =>
        {
            Assert.That(gameState.GameMap, Has.Count.EqualTo(10));
            foreach (var row in gameState.GameMap)
            {
                Assert.That(row, Has.Count.EqualTo(10));
            }
        });
    }

    [Test]
    public void ShouldDeserializeGameStateToTurnNumberAndMap()
    {
        var partlyDeserializedState = ConstructStateJson(new[]
        {
            ("Empty", "{}")
        }, 82);
        
        var gameState = _serializer.DeserializeGameState(partlyDeserializedState);
        
        Assert.Multiple(() =>
        {
            Assert.That(gameState.TurnNumber, Is.EqualTo(82));
            Assert.That(gameState.GameMap, Has.Count.EqualTo(1));
            Assert.That(gameState.GameMap[0], Has.Count.EqualTo(1));
            Assert.That(gameState.GameMap[0][0].CellType, Is.EqualTo(CellType.Empty));
        });
    }

    [Test]
    public void ShouldDeserializeStartGameDataToTickLengthAndTurnRate()
    {
        var startGameJson = "{\"tickLength\": 1000, \"turnRate\": 2}";
        var partlyDeserializedStartGameJson = JsonConvert.DeserializeObject(startGameJson);
        
        var gameData = _serializer.DeserializeStartGameData(partlyDeserializedStartGameJson);

        Assert.Multiple(() =>
        {
            Assert.That(gameData.TickLength, Is.EqualTo(1000));
            Assert.That(gameData.TurnRate, Is.EqualTo(2));
        });
    }

    #endregion

    #region helpers

    private static dynamic? ConstructStateJson(IEnumerable<(string type, string data)> cells, int turnNumber = 1)
    {
        var cellStrings = cells
            .Select(cellString => $"{{\"type\": \"{cellString.type}\", \"data\": {cellString.data}}}").ToList();
        var rawJson = $"{{\"turnNumber\": {turnNumber}, \"gameMap\": [[{string.Join(", ", cellStrings)}]]}}";
        return JsonConvert.DeserializeObject(rawJson);
    }
    
    #endregion
}