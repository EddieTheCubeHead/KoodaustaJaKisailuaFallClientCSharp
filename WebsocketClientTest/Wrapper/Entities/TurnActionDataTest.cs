using WebsocketClient.Wrapper.Entities;

namespace WebsocketClientTest.Wrapper.Entities;

public class TurnActionDataTest
{
    [Test]
    public void ShouldIncludeDirectionOnTurnActionSerialization()
    {
        var turnActionJson = new TurnActionData { Direction = CompassDirection.West }.Serialize();

        Assert.AreEqual("{\"direction\":\"west\"}", turnActionJson);
    }

    [Test]
    public void ShouldOnlyLowerCapitalLettersInTheBeginningOfDirectionString()
    {
        var turnActionJson = new TurnActionData { Direction = CompassDirection.NorthWest }.Serialize();

        Assert.AreEqual("{\"direction\":\"northWest\"}", turnActionJson);
    }
}