using WebsocketClient.Wrapper.Entities;

namespace WebsocketClientTest.Wrapper.Entities;

public class MoveActionDataTest
{
    [Test]
    public void ShouldIncludeDistanceOnMoveActionSerialization()
    {
        var moveActionJson = new MoveActionData { Distance = 3 }.Serialize();
        
        Assert.AreEqual("{\"distance\":3}", moveActionJson);
    }
}