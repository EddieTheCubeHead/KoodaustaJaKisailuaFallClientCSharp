using WebsocketClient.Wrapper.Entities;

namespace WebsocketClientTest.Wrapper.Entities;

public class ShootActionDataTest
{
    [Test]
    public void ShouldIncludeSpeedAndMassOnShootActionSerialization()
    {
        var shootActionJson = new ShootActionData { Speed = 3, Mass = 2 }.Serialize();
        
        Assert.AreEqual("{\"speed\": 3,\"mass\": 2}", shootActionJson);
    }
}