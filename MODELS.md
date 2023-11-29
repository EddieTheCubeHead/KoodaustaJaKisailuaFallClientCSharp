# TeamAiContext

The `TeamAiContext` object is available in the team ai class as the private `Context`
attribute. It is reset at the start of a game and can be used to store persistent data
between ticks. It also contains data about the game sent by the server at game start.
This data is the tick length of the game in milliseconds and the maximum turn rate for
the game, in 1/8ths of a circle, or compass direction steps.

To edit the data you can store in the team ai context, you can edit the class in 
`WebsocketClient/Wrapper/Entities/TeamAiContext.cs` to add the fields you need. The basic 
context class is as follows:

```csharp
public class TeamAiContext {
    public int TickLength { get; init; }
    public int TurnRate {get; init; }
}
```

# GameState

The `GameState` record houses two fields. `turnNumber` and `gameMap`.

 - `turnNumber` is a rolling integer denoting the current turn,
starting from 1.
 - `gameMap` is a matrix (array of arrays) of `Cell` objects. See [Cells](#cells) for
further reference

```csharp
public record GameState {
    public required int TurnNumber { get; init; }
    public required List<List<Cell>> GameMap { get; init; }
}
```

## Cells

Each map cell houses a `Cell` record, with fields `CellType` and a nullable data field for each 
cell type with data. Cell type denotes the cell type and the data fields house the unique data 
for each cell type. 

```csharp
    public required CellType CellType { get; init; }
    public ProjectileData? ProjectileData { get; init; }
    public ShipData? ShipData { get; init; }
    public HitBoxData? HitBoxData { get; init; }
}
```

Possible cell types and their data models are:

### Empty
Empty cell, only space here. No data

### OutOfVision
Cell outside your vision range. No data


### AudioSignature
If an enemy ship is outside your vision range, an out of vision cell at the edge of your
vision is converted into an audio signature cell on the line from your ship towards
the ship that is out of vision. No data

### HitBox
Entities (ships and projectiles) are not always exactly the size of one cell. In these
cases the middle cell of the entity houses the entity data, while the rest are hit box
cells with a reference to the main entity by entity id.
```csharp
public record HitBoxData
{
    public required string EntityId { get; init; }
}
```

### Ship
A cell with a ship entity

See [CompassDirection](#compassdirection) for possible compass direction values.
```csharp
public record ShipData
{
    public string Id { get; init; }
    public Coordinates Position { get; init; }
    public CompassDirection Direction { get; init; }
    public int? Health { get; init; }
    public int? Heat { get; init; }
}
```

### Projectile
A cell with a projectile entity

See [CompassDirection](#compassdirection) for possible compass direction values.
```csharp
public record ProjectileData
{
    public string Id { get; init; }
    public Coordinates Position { get; init; }
    public CompassDirection Direction { get; init; }
    public int? Mass { get; init; }
    public int? Velocity { get; init; }
}
```

# Command

The bot sends command models as a response to server game tick events. Each command
model has an `ActionType` field and a `Payload` field containing action type specific
data. `IActionData` exposes the `Serialize` method which returns a JSON string, but should
not be relevant to the usage of the wrapper unless you want to implement custom functionality.

```csharp
public enum ActionType
{
    Move,
    Turn,
    Shoot
}

...

public record Command
{
    public required ActionType Action { get; init; }
    public required IActionData Payload { get; init; }
}
```

The action data models are as follows:

### Move

Move straight ahead 0 to 3 cells. Dissipates `distance * 2` heat

```csharp
public record MoveActionData : IActionData
{
    public required int Distance { get; init; }
    public string Serialize() ...
}
```

### Turn

Turn to a compass direction. Cannot be compass direction at a time than the game max
turn radius. Validated server-side

```csharp
public record TurnActionData : IActionData
{
    public required CompassDirection Direction { get; init; }
    public string Serialize() ...
}
```

### Shoot

Shoot a projectile. Generates `mass * speed` heat, moves at `speed` speed and
does `mass * 2 + speed` damage on impact. Heat exceeding `25` will be converted to
damage on your ship in a ratio of `1:1`


```csharp
public record ShootActionData : IActionData
{
    public required int Mass { get; init; }
    public required int Speed { get; init; }
    public string Serialize() ...
}
```

# CompassDirection
Enum holding compass direction values
```csharp
public enum CompassDirection
{
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest
}
```