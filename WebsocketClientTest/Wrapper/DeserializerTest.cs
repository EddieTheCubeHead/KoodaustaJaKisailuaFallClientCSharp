using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
using WebsocketClient.Entities;
using WebsocketClient.Wrapper;

namespace WebsocketClientTest.Wrapper;

public class DeserializerTest
{
    #region Setup
    
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Deserializer _deserializer;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    
    [SetUp]
    public void BeforeEach()
    {
        _deserializer = new Deserializer();
    }
    
    #endregion
    
    #region Tests

    [Test]
    public void ShouldDeserializeCellWithNoDataBasedOnCellType()
    {
        var stateJson = ConstructStateJson(new[]
        {
            ("Empty", "{}"),
            ("OutOfVision", "{}"),
            ("AudioSignature", "{}")
        });
        
        var gameState = _deserializer.DeserializeGameState(stateJson);
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
        var stateJson = ConstructStateJson(new[]
        {
            ("HitBox", "{\"entityId\": \"1\"}")
        });
        
        var gameState = _deserializer.DeserializeGameState(stateJson);
        Assert.Multiple(() =>
        {
            Assert.That(gameState.GameMap[0][0].CellType, Is.EqualTo(CellType.HitBox));
            Assert.That(gameState.GameMap[0][0].HitBoxData?.EntityId, Is.EqualTo("1"));
        });
    }

    #endregion

    #region helpers

    private static string ConstructStateJson(IEnumerable<(string type, string data)> cells)
    {
        var cellStrings = cells
            .Select(cellString => $"{{\"type\": \"{cellString.type}\", \"data\": {cellString.data}}}").ToList();
        return $"{{\"turnNumber\": 1, \"gameMap\": [[{string.Join(", ", cellStrings)}]]}}";
    }
    
    #endregion
}