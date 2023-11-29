# Basic WebSocket interface

The bots communicate with the server through a websocket interface. The wrapper fully
realizes this interface, so you unless you want to make manual changes to the wrapper
you do not need to read this document. See
[model reference](MODELS.md) for reference on the
python models.

Each websocket request is encoded as a json object with two fields. The fields are
`eventType` and `data`. Thus, each event looks as follows:

```json
{
    "eventType": str,
    "data": {}
}
```

All the event types and their data packages are as follows:

## Server sent events:

### authAck:
```json
{
    "eventType": "authAck",
    "data": {}
}
```

### startGame:
```json
{
    "eventType": "startGame",
    "data": {}
}
```

### gameTick:
See [GameState](#gamestate) for `GameState` reference.
```json
{
    "eventType": "gameTick",
    "data": GameState
}
```


### endGame:
```json
{
    "eventType": "endGame",
    "data": {}
}
```

## Client sent events:

### auth:
```json
{
    "eventType": "auth",
    "data": {
        "token": str,
        "botName": str
     }
}
```

### startAck:
```json
{
    "eventType": "startAck",
    "data": {}
}
```

### gameAction:
see [ClientActions](#clientactions) for `ClientAction` reference.
```json
{
    "eventType": "gameAction",
    "data": ClientAction
}
```

### endAck:
```json
{
    "eventType": "endAck",
    "data": {}
}
```

# GameState

The `GameState` object houses two fields. `turnNumber` and `gameMap`. Please note that
in the python code, these fields are accessed via pythonic snake_case names.

 - `turnNumber` or `turn_number` is a rolling integer denoting the current turn,
starting from 1.
 - `gameMap` is a matrix (array of arrays) of `Cell` objects. See [Cells](#cells) for
further reference

```json
{
    "turnNumber": int,
    "gameMap": [
        [
            {
                "cellType": str,
                "data": {}
            }
        ]
    ]
}
```

## Cells

Each cell houses a `Cell` object, with fields `cellType` and `data`. Cell type denotes
the cell type and data houses the unique data for each cell type. Possible types and
their data objects are:

### Empty
Empty cell, only space here

```json
{
    "cellType": "empty",
    "data": {}
}
```

### OutOfVision
Cell outside your vision range
```json
{
    "cellType": "outOfVision",
    "data": {}
}
```

### AudioSignature
If an enemy ship is outside your vision range, an out of vision cell at the edge of your
vision is converted into an audio signature cell on the line from your ship towards
the ship that is out of vision.
```json
{
    "cellType": "audioSignature",
    "data": {}
}
```

### HitBox
Entities (ships and projectiles) are not always exactly the size of one cell. In these
cases the middle cell of the entity houses the entity data, while the rest are hit box
cells with a reference to the main entity by entity id.
```json
{
    "cellType": "hitBox",
    "data": {
        "entityId": str
    }
}
```

### Ship
A cell with a ship entity

See [CompassDirection](#compassdirection) for possible compass direction enum values in 
the python model
```json
{
    "cellType": "ship",
    "direction": str
    "data": {
        "id": str,
        "position": {
            "x": int,
            "y": int
        },
        "health": int,
        "heat": int
    }
}
```

### Projectile
A cell with a projectile entity

See [CompassDirection](#compassdirection) for possible compass direction enum values in 
the python model
```json
{
    "cellType": "projectile",
    "direction": str
    "data": {
        "id": str,
        "position": {
            "x": int,
            "y": int
        },
        "speed": int,
        "mass": int
    }
}
```

# ClientActions

The bot sends client action models as a response to server game tick events. Each action
model has an `actionType` field and a `payload` field containing action type specific
data. The models are as follows

### Move

Move straight ahead 0 to 3 cells. Dissipates `distance * 2` heat

```json
{
    "actionType": "move",
    "payload": {
        "distance": int
    }
}
```

### Turn

Turn to a compass direction. Cannot be compass direction at a time than the game max
turn radius. Validated server-side

```json
{
    "actionType": "turn",
    "payload": {
        "direction": str
    }
}
```

### Shoot

Shoot a projectile. Generates `mass * speed` heat, moves at `speed` speed and
does `mass * 2 + speed` damage on impact. Heat exceeding `25` will be converted to
damage on your ship in a ratio of `1:1`


```json
{
    "actionType": "shoot",
    "payload": {
        "mass": int,
        "speed": int
    }
}
```

# CompassDirection
All directions are given as shortened compass directions for websocket communication
```json
North = "n",
NorthEast = "ne",
East = "e",
SouthEast = "se",
South = "s",
SouthWest = "sw",
West = "w",
NorthWest = "nw"
```