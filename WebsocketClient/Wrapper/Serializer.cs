using Newtonsoft.Json;
using WebsocketClient.Wrapper.Entities;

namespace WebsocketClient.Wrapper;

public class Serializer
{
    private Dictionary<string, CompassDirection> _directionMappings = new()
    {
        { "n", CompassDirection.North },
        { "ne", CompassDirection.NorthEast },
        { "e", CompassDirection.East },
        { "se", CompassDirection.SouthEast },
        { "s", CompassDirection.South },
        { "sw", CompassDirection.SouthWest },
        { "w", CompassDirection.West },
        { "nw", CompassDirection.NorthWest }
    };
    
    public string SerializeCommand(Command command)
    {
        var actionType = command.Action.ToString().ToLower();
        var actionData = command.Payload.Serialize();
        return $"{{\"action\": \"{actionType}\", \"payload\": {actionData}}}";
    }
    
    public StartGameData DeserializeStartGameData(dynamic startGameData)
    {
        return new StartGameData
        {
            TickLength = startGameData.tickLength,
            TurnRate = startGameData.turnRate
        };
    }
    
    public GameState DeserializeGameState(dynamic partlyDeserializedState)
    {
        return new GameState
        {
            TurnNumber = partlyDeserializedState.turnNumber,
            GameMap = this.DeserializeMap(partlyDeserializedState.gameMap)
        };
    }

    private List<List<Cell>> DeserializeMap(dynamic partlyDeserializedMap)
    {
        List<List<Cell>> map = new();
        foreach (var partlyDeserializedRow in partlyDeserializedMap)
        {
            map.Add(this.DeserializeRow(partlyDeserializedRow));
        }

        return map;
    }
    
    private List<Cell> DeserializeRow(dynamic partlyDeserializedRow)
    {
        List<Cell> row = new();
        foreach (var partlyDeserializedCell in partlyDeserializedRow)
        {
            row.Add(this.DeserializeCell(partlyDeserializedCell));
        }

        return row;
    }

    private Cell DeserializeCell(dynamic partlyDeserializedCell)
    {
        if (!Enum.TryParse((string)partlyDeserializedCell.type, true, out CellType cellType))
        {
            throw new JsonException($"Could not parse cell type from '{partlyDeserializedCell.type}'.");
        }

        return cellType switch
        {
            CellType.HitBox => new Cell
            {
                CellType = cellType, HitBoxData = this.DeserializeHitBoxData(partlyDeserializedCell.data)
            },
            CellType.Projectile => new Cell
            {
                CellType = cellType,
                ProjectileData = this.DeserializeProjectileData(partlyDeserializedCell.data)
            },
            CellType.Ship => new Cell
            {
                CellType = cellType,
                ShipData = this.DeserializeShipData(partlyDeserializedCell.data)
            },
            _ => new Cell { CellType = cellType }
        };
    }

    private HitBoxData DeserializeHitBoxData(dynamic partlyDeserializedCellData)
    {
        return new HitBoxData { EntityId = partlyDeserializedCellData.entityId };
    }

    private ShipData DeserializeShipData(dynamic partlyDeserializedShipData)
    {
        (CompassDirection direction, Coordinates position) entityLocationData =
            GetEntityLocationData(partlyDeserializedShipData);
        return new ShipData
        {
            Id = partlyDeserializedShipData.id,
            Direction = entityLocationData.direction,
            Position = entityLocationData.position,
            Health = partlyDeserializedShipData.health,
            Heat = partlyDeserializedShipData.heat
        };
    }

    private ProjectileData DeserializeProjectileData(dynamic partlyDeserializedProjectileData)
    {
        (CompassDirection direction, Coordinates position) entityLocationData =
            GetEntityLocationData(partlyDeserializedProjectileData);
        return new ProjectileData
        {
            Id = partlyDeserializedProjectileData.id,
            Direction = entityLocationData.direction,
            Position = entityLocationData.position,
            Speed = partlyDeserializedProjectileData.velocity,
            Mass = partlyDeserializedProjectileData.mass
        };
    }

    private (CompassDirection, Coordinates) GetEntityLocationData(dynamic partlyDeserializedEntity)
    {
        if (!_directionMappings.ContainsKey((string)partlyDeserializedEntity.direction))
        {
            throw new JsonException(
                $"Could not parse ship direction from '{partlyDeserializedEntity.direction}'.");
        }

        var entityDirection = _directionMappings[(string)partlyDeserializedEntity.direction];

        var entityCoordinates = new Coordinates
            { X = partlyDeserializedEntity.position.x, Y = partlyDeserializedEntity.position.y };

        return (entityDirection, entityCoordinates);
    }
}