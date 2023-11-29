# MustacheCorp Captain Solar

The game is a tick-based 1v1 space battler played on a 2d grid. The grid size is 30
cells by 30 cells. Each ship starts on their respective side (left/right) on the grid 
with a randomized y coordinate. Both ships start facing towards the middle of the grid.

## Game flow

The game is played on long ticks. Tick length is decided by the server and is sent 
to both clients at the start of the game, along with the maximum turn rate for the game.
These can be found in the [client context](MODELS.md#clientcontext).

At the start of a tick the server sends the current [game state](MODELS.md#gamestate) to
both clients. The clients analyze the state and respond with a
[command](MODELS.md#command). The possible commands are:

 - Move 0 to 3 cells into the direction the ship is facing. Note that if your client
fails to provide a command in time, or throws an exception while processing the tick,
"move 0" is sent automatically.
 - Turn to face a compass direction. How many compass direction steps can be turned in
one tick can be found in the [client context](MODELS.md#clientcontext)
 - Shoot a projectile with mass and speed both between 1 and 4. This generates
`mass * speed` [heat](#heat). A projectile hitting a ship deals `mass * 2 + speed`
damage.

## Ship attributes

### Direction

Given as a compass direction, move and shoot commands always happen towards the direction
the ship is facing.

### Health

Health is the amount of damage the ship can sustain. If a ship's health reaches 0 the
ship is destroyed.

Ships take damage from coming into contact with a projectile, another ship, or exceeding
their max heat.

Projectile damage is calculated as `projectile_mass * 2 + projectile_speed`

Ship contact damage is fixed `3` for the ship trying to move to a space with another ship.
The move command is not performed in this case.

If ship generates heat exceeding the max heat amount of `25` the excess heat is converted
to damage in a ratio of `1 heat : 1 damage`

### Heat

Heat is the heat accumulated from firing the ship weaponry. The ships are engaged in the
upper atmosphere of a planet and thus heat is dissipated by moving.

Heat accumulates on shoot actions via the formula `projectile_mass * projectile_speed`.

Heat dissipates on move actions via the formula `distance * 2`

Maximum heat for a ship is 25, excess heat generated is turned into damage with a ratio
of `1 heat : 1 damage`.

### Vision

Ships can only see in a circle with a radius of `10` around them. Cells outside this area
are returned as [out of vision](MODELS.md#outofvision). If there is a ship outside the
vision are, the out of vision cell at the edge of your ship's vision range on a direct
line between the ships is converted into an [audio signature](MODELS.md#audiosignature)
cell.

## Projectile attributes

### Mass

The mass of the projectile between 1 and 4. Main component in projectile damage.

### speed

The speed of the projectile between 1 and 4. Some effect on damage, but also affects
how many cells the projectile moves each tick.

### Damage

Projectile damage is calculated as `mass * 2 + speed`
