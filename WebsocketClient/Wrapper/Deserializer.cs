using System.Text.Json.Serialization;
using Newtonsoft.Json;
using WebsocketClient.Entities;

namespace WebsocketClient.Wrapper;

public class Deserializer
{
    public GameState DeserializeGameState(string json)
    {
        dynamic? partlyDeserializedState = JsonConvert.DeserializeObject(json);
        if (partlyDeserializedState is null)
        {
            throw new JsonException($"Deserializing string form json '{json}' yielded null.");
        }

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
                // ProjectileData = this.DeserializeProjectileData(partlyDeserializedCell)
            },
            CellType.Ship => new Cell
            {
                CellType = cellType,
                // ShipData = this.DeserializeShipData(partlyDeserializedCell)
            },
            _ => new Cell { CellType = cellType }
        };
    }

    private HitBoxData DeserializeHitBoxData(dynamic partlyDeserializedCellData)
    {
        return new HitBoxData { EntityId = partlyDeserializedCellData.entityId };
    }
}