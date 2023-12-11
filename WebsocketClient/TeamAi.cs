using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;
using WebsocketClient.Wrapper.Entities;

namespace WebsocketClient;

/// <summary>
/// The class that is responsible for handling a team's AI logic for each tick
/// </summary>
public class TeamAi
{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    /// <summary>
    /// The persistent context maintained between ticks
    /// </summary>
    public TeamAiContext Context = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

    /// <summary>
    /// You can use this logger to track the behaviour of your bot. 
    ///
    /// This is preferred to calling print("msg") as it offers better configuration (see README.md in root)
    /// </summary>
    ///
    /// <example>
    /// _logger.LogDebug("A message that is not important but helps understand the code during problem solving.")
    /// _logger.LogInfo("A message that you want to see to know the state of the bot during normal operation.")
    /// _logger.LogWarning("A message that demands attention, but is not yet causing problems.")
    /// _logger.LogError("A message about the bot reaching an erroneous state")
    /// _logger.LogCritical("A message about a critical exception, usually causing a premature shutdown")
    /// </example>
    private readonly ILogger _logger;

    private int[,] _currentMap;
    private SafetyValues[,] _currentSafetyMap;
    private ulong[,] _currentSafetyMapIsSetOnRound;
    private ulong _currentRound;
    private ShipData? _currentMyShipState;
    private ShipData? _currentEnemyShipState;
    private Vector2 _currentEnemyNormalizedDirectionBySound;
    private Vector2 _currentEnemyPositionBySound;
    private List<ProjectileData> _liveProjectiles;
    private List<Vector2> _currentOurHitBoxLocations;
    private List<Vector2> _currentEnemyHitBoxLocations;
    private Vector2 _currentEnemyPositionVector;
    private Vector2 _currentEnemyDirectionVector;
    private Vector2 _currentNormalizedEnemyDirectionVector;
    private Vector2 _currentOurDirectionVector;
    private Vector2 _currentNormalizedOurDirectionVector;
    private Vector2 _currentOurPosition;

    private Vector2 _lastEnemySoundPosition;

    private const int MaxHeat = 25;
    private const int MaxHeatLimit = 20;
    private const int MapWidth = 30;
    private const int MapHeight = 30;
    private const int MaxMoveDistance = 3;
    private const int MaxSpeed = 3;
    private const int MaxMass = 4;
    private const int MaxTurns = 1;

    private readonly int[,] _speedMassHeatMap;
    private ImmutableSortedSet<int> _possibleSortedHeatValues;
    private ImmutableDictionary<int, ShootActionData> _heatValueToMaxSpeedShootData;

    private static SafetyValues[] ProjectileCalculationDontOverrideValues = new SafetyValues[] { SafetyValues.Enemy, SafetyValues.Sound, SafetyValues.MyShip };

    private readonly string _ourBotId;

    public TeamAi(ILoggerFactory loggerFactory, string token, string botName)
    {
        _logger = loggerFactory.CreateLogger<TeamAi>();
        _ourBotId = $"ship:{token}:{botName}";
        _currentMap = new int[30, 30];
        _currentSafetyMap = new SafetyValues[30, 30];
        _currentSafetyMapIsSetOnRound = new ulong[30, 30];
        _currentRound = 0;
        InitSafetyMap();

        // Pre-calculate shooting heat values
        _speedMassHeatMap = new int[MaxSpeed + 1, MaxMass + 1];
        PreCalculateShootingHeatGeneration(_speedMassHeatMap, MaxSpeed, MaxMass, ref _possibleSortedHeatValues, ref _heatValueToMaxSpeedShootData);

        _liveProjectiles = new List<ProjectileData>(20);
        _currentMyShipState = null;
        _currentEnemyShipState = null;
        _currentEnemyNormalizedDirectionBySound = new Vector2(0, 0);
        _currentEnemyPositionBySound = new Vector2(0, 0);
        _currentOurHitBoxLocations = new List<Vector2>(8);
        _currentEnemyHitBoxLocations = new List<Vector2>(8);
    }

    private void PreCalculateShootingHeatGeneration(
        int[,] speedMassHeatMap, int maxSpeed, int maxMass,
        ref ImmutableSortedSet<int> possibleSortedHeatValues,
        ref ImmutableDictionary<int, ShootActionData> heatToMaxSpeedShootData)
    {
        var heatValueToMaxSpeedMassMap = new Dictionary<int, ShootActionData>();
        var possibleHeatValues = new SortedSet<int>();

        for (int speed = 0; speed < maxSpeed; speed++)
        {
            for (int mass = 0; mass < maxMass; mass++)
            {
                int heat = mass * speed;
                speedMassHeatMap[speed, mass] = heat;
                possibleHeatValues.Add(heat);
                ShootActionData existingShootingData = null;
                if (heatValueToMaxSpeedMassMap.TryGetValue(heat, out existingShootingData))
                {
                    if (speed > existingShootingData.Speed)
                    {
                        heatValueToMaxSpeedMassMap[heat] = new ShootActionData
                        {
                            Speed = speed,
                            Mass = mass
                        };
                    }
                }
                else
                {
                    heatValueToMaxSpeedMassMap[heat] = new ShootActionData
                    {
                        Speed = speed,
                        Mass = mass
                    };
                }
            }
        }

        possibleSortedHeatValues = possibleHeatValues.ToImmutableSortedSet();
        heatToMaxSpeedShootData = heatValueToMaxSpeedMassMap.ToImmutableDictionary();
    }

    private void InitSafetyMap()
    {
        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < 30; y++)
            {
                if (IsBorder((x, y)))
                {
                    _currentSafetyMap[x, y] = SafetyValues.InstantDanger;
                }
            }
        }
    }

    public void CreateContext(StartGameData gameData)
    {
        Context = new TeamAiContext(gameData.TickLength, gameData.TurnRate);
    }

    /// <summary>
    /// Main function defining the behaviour of the AI of the team
    /// </summary>
    /// <param name="gameState">the current state of the game</param>
    /// <returns>A Command instance containing the type and data of the command to be executed on the
    /// tick. Returning None tells server to move 0 steps forward.</returns>
    /// <remarks>You can get tick time in milliseconds from `context.tick_length_ms` and ship turn rate
    /// in 1/8th circles from `context.turn_rate`.
    ///
    /// If your function takes longer than the max tick length the function is cancelled and None is
    /// returned.
    /// </remarks>
    public Command? ProcessTick(GameState gameState)
    {
        _logger.LogDebug("Processing tick.");

        _currentRound++;

        // Your code goes here

        _liveProjectiles.Clear();
        _currentEnemyHitBoxLocations.Clear();

        bool isEnemyVisible = false;

        for (int x = 1; x < MapWidth - 1; x++)
        {
            for (int y = 1; y < MapHeight - 1; y++)
            {
                Cell cell = gameState.GameMap[y][x];

                Vector2 currentCoordinate = new Vector2(x, y);

                _currentMap[x, y] = (int)cell.CellType;

                switch (cell.CellType)
                {
                    case CellType.AudioSignature:
                        //_logger.LogDebug("Found Audio signature at" + " at: x=" + x + " y=" + y);
                        _currentEnemyPositionBySound.X = x;
                        _currentEnemyPositionBySound.Y = y;
                        _currentSafetyMap[x, y] = SafetyValues.Sound;
                        break;
                    case CellType.HitBox:
                        if (cell.HitBoxData.EntityId.Equals(_ourBotId, StringComparison.InvariantCultureIgnoreCase))
                        {
                            //_logger.LogDebug("Found our hitbox at: x=" + x + " y=" + y);
                            _currentOurHitBoxLocations.Add(currentCoordinate);
                            _currentSafetyMap[x, y] = SafetyValues.MyShip;
                        }
                        else
                        {
                            //_logger.LogDebug("Found enemy hitbox at: x=" + x + " y=" + y);

                            _currentEnemyHitBoxLocations.Add(currentCoordinate);
                            // Add 1 cell (our hitbox) padding to safety map around checkbox so we can instantly check if we can move there:

                            PadSafetyMapAroundCellAndSetSafetyMapOnRound(currentCoordinate, _currentSafetyMap, SafetyValues.Enemy, _currentSafetyMapIsSetOnRound, _currentRound);
                        }
                        break;
                    case CellType.Projectile:
                        _currentSafetyMap[x, y] = SafetyValues.InstantDanger;
                        _liveProjectiles.Add(cell.ProjectileData);
                        break;
                    case CellType.Ship:
                        if (cell.ShipData.Id.Equals(_ourBotId, StringComparison.InvariantCultureIgnoreCase))
                        {
                            //_logger.LogDebug("Found our bot: " + cell.ShipData.Id + " at: x=" + x + " y=" + y);
                            _currentMyShipState = cell.ShipData;
                            _currentSafetyMap[x, y] = SafetyValues.MyShip;
                            _currentOurHitBoxLocations.Add(new Vector2(x, y));
                            _currentOurDirectionVector = GetDirectionVectorFromDirection(cell.ShipData.Direction);
                            _currentNormalizedOurDirectionVector = Vector2.Normalize(_currentOurDirectionVector);
                            _currentOurPosition = currentCoordinate;
                        }
                        else
                        {
                            //_logger.LogDebug("Found enemy bot: " + cell.ShipData.Id + " at: x=" + x + " y=" + y);
                            _currentEnemyShipState = cell.ShipData;
                            _currentSafetyMap[x, y] = SafetyValues.Enemy;
                            _currentEnemyHitBoxLocations.Add(currentCoordinate);
                            _currentEnemyDirectionVector = GetDirectionVectorFromDirection(cell.ShipData.Direction);
                            _currentNormalizedEnemyDirectionVector = Vector2.Normalize(_currentEnemyDirectionVector);
                            _currentEnemyPositionVector = currentCoordinate;
                            isEnemyVisible = true;
                        }
                        break;
                    case CellType.Empty:
                        if (_currentSafetyMapIsSetOnRound[x, y] != _currentRound)
                            _currentSafetyMap[x, y] = SafetyValues.Safe;
                        break;

                        break;
                    case CellType.OutOfVision:
                        if (_currentSafetyMapIsSetOnRound[x, y] != _currentRound)
                            _currentSafetyMap[x, y] = SafetyValues.Unknown;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("invalid cell type");
                }
                _currentSafetyMapIsSetOnRound[x, y] = _currentRound;

            }
        }

        _lastEnemySoundPosition = _currentEnemyPositionBySound;
        _currentEnemyNormalizedDirectionBySound = Vector2.Normalize(Vector2.Subtract(_currentEnemyPositionBySound, _currentOurPosition));

        foreach (ProjectileData liveProjectile in _liveProjectiles)
        {
            Vector2 directionVector = GetDirectionVectorFromDirection(liveProjectile.Direction);

            int projectileSpeed = liveProjectile.Speed ?? 1;
            for (int i = 0; i <= projectileSpeed; i++)
            {

                Vector2 projectileTrajectoryCoordinate = new Vector2(liveProjectile.Position.X + (directionVector.X * i),
                    liveProjectile.Position.Y + (directionVector.Y * i));
                AddToSafetyMapIfValidCoordinatesAndDoesNotOverrideGivenValues(
                    projectileTrajectoryCoordinate, SafetyValues.InstantDanger,
                    _currentSafetyMap,
                    ProjectileCalculationDontOverrideValues);

                PadSafetyMapAroundCellDontOverride(projectileTrajectoryCoordinate, _currentSafetyMap, SafetyValues.InstantDanger, ProjectileCalculationDontOverrideValues);
            }

            Vector2 expectedProjectileCoordinates = new Vector2(
                liveProjectile.Position.X + (directionVector.X * projectileSpeed) + directionVector.X,
                liveProjectile.Position.Y + (directionVector.Y * projectileSpeed) + directionVector.Y);
            while (0 <= expectedProjectileCoordinates.X && expectedProjectileCoordinates.X < MapWidth &&
                   0 <= expectedProjectileCoordinates.Y && expectedProjectileCoordinates.Y < MapHeight)
            {
                AddToSafetyMapIfValidCoordinates(expectedProjectileCoordinates, SafetyValues.InstantDanger, _currentSafetyMap);
                PadSafetyMapAroundCellDontOverride(expectedProjectileCoordinates, _currentSafetyMap, SafetyValues.InstantDanger, ProjectileCalculationDontOverrideValues);

                expectedProjectileCoordinates = Vector2.Add(expectedProjectileCoordinates, directionVector);
            }
        }

        PrettyLogCurrentSafetyMap();

        bool areWeInDanger = false;
        bool areWeInFutureDanger = false;
        foreach (Vector2 ourHitBoxLocation in _currentOurHitBoxLocations)
        {
            switch (_currentSafetyMap[(int)ourHitBoxLocation.X, (int)ourHitBoxLocation.Y])
            {
                case SafetyValues.InstantDanger:
                    areWeInDanger = true;
                    break;
                case SafetyValues.FutureDanger:
                    areWeInFutureDanger = true;
                    break;
            }
        }

        Command? command = null;

        int currentMyHeat = (int)_currentMyShipState.Heat;

        bool doWeHaveTooMuchHeat = currentMyHeat >= MaxHeatLimit;
        if (areWeInDanger || areWeInFutureDanger)
        {
            List<Vector2> futureDangerCoordinates = new List<Vector2>();
            List<Vector2> safeCoordinates = FindSafeCoordinatesAroundUs(futureDangerCoordinates);

            if (safeCoordinates.Count == 0)
            {
                safeCoordinates = futureDangerCoordinates;
            }

            if (safeCoordinates.Count == 0)
            {
                // Return fire!
                Vector2 coordinateToShootTowards = isEnemyVisible ? _currentEnemyPositionVector : _currentEnemyPositionBySound;


                ShootActionData? shootData = GetSpeedGreedyShootActionDataIfWeDontHaveTooMuchHeat(0, currentMyHeat, MaxHeat, MaxHeatLimit);

                if (shootData != null)
                {
                    Vector2 normalizedDirectionToShootTowards = Vector2.Normalize(Vector2.Subtract(coordinateToShootTowards, _currentOurPosition));
                    CompassDirection directionToShoot = Helpers.GetApproximateDirection(normalizedDirectionToShootTowards);

                    Vector2 normalizedExactDirectionToShootTowards = Vector2.Normalize(GetDirectionVectorFromDirection(directionToShoot));

                    if (IsDirectionInOurSector(normalizedDirectionToShootTowards,
                            normalizedExactDirectionToShootTowards, 0.15d))
                    {
                        return ShootIfPointingTowardsDirectionOrTurnTowardsIt(directionToShoot,
                            _currentMyShipState.Direction, shootData);
                    }
                }
            }

            // coordinates sorted from closest to furthest
            // if we have heat, we want to move more so we start trying from the furthest
            int i = doWeHaveTooMuchHeat ? safeCoordinates.Count - 1 : 0;

            while (0 <= i && i < safeCoordinates.Count)
            {
                Vector2 coordinateToMoveTowards = safeCoordinates[i];

                if (doWeHaveTooMuchHeat)
                {
                    i--;
                }
                else
                {
                    i++;
                }

                command = MoveTowardsPointOrTurn(coordinateToMoveTowards, _currentOurPosition, _currentMyShipState.Direction, _logger);
                if (command != null)
                {
                    return command;
                }
                _logger.LogDebug($"Could not move to: {coordinateToMoveTowards.X}, {coordinateToMoveTowards.Y}. Trying another safe spot.");
            }

        }
        else
        {

            if (isEnemyVisible)
            {
                int distanceToEnemy = (int)Vector2.Distance(_currentOurPosition, _currentEnemyPositionVector);

                if (distanceToEnemy > 7)
                {
                    ShootActionData? shootData = GetSpeedGreedyShootActionDataIfWeDontHaveTooMuchHeat(0, currentMyHeat, MaxHeat, MaxHeatLimit);

                    if (shootData != null)
                    {
                        Vector2 normalizedDirectionToShootTowards = Vector2.Normalize(Vector2.Subtract(_currentEnemyPositionBySound, _currentOurPosition));
                        CompassDirection directionToShoot =
                            Helpers.GetApproximateDirection(normalizedDirectionToShootTowards);

                        Vector2 normalizedExactDirectionToShootTowards = Vector2.Normalize(GetDirectionVectorFromDirection(directionToShoot));

                        if (IsDirectionInOurSector(normalizedDirectionToShootTowards,
                                normalizedExactDirectionToShootTowards, 0.15d))
                        {
                            return ShootIfPointingTowardsDirectionOrTurnTowardsIt(directionToShoot,
                                _currentMyShipState.Direction, shootData);
                        }
                    }
                }
                command = MoveAwayFromPosition(_currentEnemyPositionVector, _currentOurPosition, _currentMyShipState.Direction, _currentSafetyMap, _logger);
                if (command != null)
                {
                    return command;
                }
                command = MoveAwayFromPositionToAnyCell(_currentEnemyPositionBySound, _currentOurPosition, _currentMyShipState.Direction, _logger);
                if (command != null)
                {
                    return command;
                }
            }
            else
            {

                ShootActionData? shootData = GetSpeedGreedyShootActionDataIfWeDontHaveTooMuchHeat(0, currentMyHeat, MaxHeat, MaxHeatLimit);

                if (shootData != null)
                {
                    Vector2 normalizedDirectionToShootTowards = Vector2.Normalize(Vector2.Subtract(_currentEnemyPositionBySound, _currentOurPosition));
                    CompassDirection directionToShoot =
                        Helpers.GetApproximateDirection(normalizedDirectionToShootTowards);

                    Vector2 normalizedExactDirectionToShootTowards = Vector2.Normalize(GetDirectionVectorFromDirection(directionToShoot));

                    if (IsDirectionInOurSector(normalizedDirectionToShootTowards,
                            normalizedExactDirectionToShootTowards, 0.15d))
                    {
                        return ShootIfPointingTowardsDirectionOrTurnTowardsIt(directionToShoot,
                            _currentMyShipState.Direction, shootData);
                    }

                }

                command = MoveOrthogonallyToSafePositionIfPossible(_currentEnemyPositionBySound, _currentOurPosition,
                    _currentMyShipState.Direction, _logger);

                if (command != null)
                {
                    return command;
                }

                List<Vector2> futureDangerCoordinates = new List<Vector2>();
                List<Vector2> safeCoordinates = FindSafeCoordinatesAroundUs(futureDangerCoordinates);

                if (safeCoordinates.Count == 0)
                {
                    safeCoordinates = futureDangerCoordinates;
                }

                if (safeCoordinates.Count == 0)
                {
                    command = MoveAwayFromPositionToAnyCell(_currentEnemyPositionBySound, _currentOurPosition, _currentMyShipState.Direction, _logger);
                    if (command != null)
                    {
                        return command;
                    }
                }

                // coordinates sorted from closest to furthest
                // if we have heat, we want to move more so we start trying from the furthest
                int i = doWeHaveTooMuchHeat ? safeCoordinates.Count - 1 : 0;

                while (0 <= i && i < safeCoordinates.Count)
                {
                    Vector2 coordinateToMoveTowards = safeCoordinates[i];

                    if (doWeHaveTooMuchHeat)
                    {
                        i--;
                    }
                    else
                    {
                        i++;
                    }

                    command = MoveTowardsPointOrTurn(coordinateToMoveTowards, _currentOurPosition, _currentMyShipState.Direction, _logger);
                    if (command != null)
                    {
                        return command;
                    }
                    _logger.LogDebug($"Could not move to: {coordinateToMoveTowards.X}, {coordinateToMoveTowards.Y}. Trying another safe spot.");
                }
            }
        }

        if (command == null)
        {
            Vector2 currentEnemyDirection = isEnemyVisible ? _currentNormalizedEnemyDirectionVector : _currentEnemyNormalizedDirectionBySound;
            _logger.LogDebug("Could not find a command to execute. Returning random action!");
            command = GetRandomCommand(currentMyHeat, currentEnemyDirection, _currentMyShipState.Direction);
        }

        return command;
    }

    private bool CouldWeFaceEnemyDirectlyFromThisPosition(
        Vector2 normalizedDirectionToShootTowards)
    {
        Vector2 unitVectorAlongXAxis = new Vector2(1, 0);

        double vectorDotProduct = Math.Abs((double)Vector2.Dot(normalizedDirectionToShootTowards, unitVectorAlongXAxis));

        double radianAngleBetweenVectors = Math.Acos(vectorDotProduct);

        var angleDegrees = Math.Round(radianAngleBetweenVectors * 180 / Math.PI, 0);

        _logger.LogDebug($"Angle to enemy from current position: {angleDegrees}. Can we get direct impact: {angleDegrees % 45}");
        return angleDegrees % 45 == 0;
    }

    private ShootActionData? GetSpeedGreedyShootActionDataIfWeDontHaveTooMuchHeat(
        int distanceToEnemy, int currentHeat, int maxHeat, int heatLimit)
    {

        if (currentHeat >= heatLimit)
        {
            return null;
        }

        // We can try max out heat
        int currentHeatLeft = maxHeat - currentHeat;

        // We want to shoot with as much speed as possible
        // Shooting generates heat with: mass * speed
        // Moving dissipates heat with: distance * 2

        //If item is not found and is less than one or more elements in this set,
        //this method returns a negative number that is the bitwise complement of the index of the first element that is larger than value.
        //If item is not found and is greater than any of the elements in the set,
        //this method returns a negative number that is the bitwise complement of the index of the last element plus 1.
        int maxPossibleHeatIndex = _possibleSortedHeatValues.IndexOf(currentHeatLeft);

        if (0 > maxPossibleHeatIndex)
        {

            maxPossibleHeatIndex = (~maxPossibleHeatIndex) - 1;

            if (0 > maxPossibleHeatIndex)
            {
                return null;
            }
        }

        int maxPossibleHeat = _possibleSortedHeatValues[maxPossibleHeatIndex];

        if (maxPossibleHeat == 0)
        {
            return null;
        }

        return _heatValueToMaxSpeedShootData[maxPossibleHeat];
    }

    private void PadSafetyMapAroundCellAndSetSafetyMapOnRound(Vector2 coordinates, SafetyValues[,]? currentSafetyMap, SafetyValues value, ulong[,] currentSafetyMapIsSetOnRound, ulong currentRound)
    {
        int x = (int)coordinates.X;
        int y = (int)coordinates.Y;

        // Top Right
        AddToSafetyMapIfValidCoordinates(new Vector2(x + 1, y - 1), value, currentSafetyMap);
        AddToSafetyMapIsSetOnRoundIfValidCoordinates(new Vector2(x + 1, y - 1), value, currentSafetyMapIsSetOnRound, currentRound);
        // Right
        AddToSafetyMapIfValidCoordinates(new Vector2(x + 1, y), value, currentSafetyMap);
        AddToSafetyMapIsSetOnRoundIfValidCoordinates(new Vector2(x + 1, y), value, currentSafetyMapIsSetOnRound, currentRound);
        // Bottom Right
        AddToSafetyMapIfValidCoordinates(new Vector2(x + 1, y + 1), value, currentSafetyMap);
        AddToSafetyMapIsSetOnRoundIfValidCoordinates(new Vector2(x + 1, y + 1), value, currentSafetyMapIsSetOnRound, currentRound);
        // Bottom
        AddToSafetyMapIfValidCoordinates(new Vector2(x, y + 1), value, currentSafetyMap);
        AddToSafetyMapIsSetOnRoundIfValidCoordinates(new Vector2(x, y + 1), value, currentSafetyMapIsSetOnRound, currentRound);

        // Top
        AddToSafetyMapIfValidCoordinates(new Vector2(x, y - 1), value, currentSafetyMap);
        AddToSafetyMapIsSetOnRoundIfValidCoordinates(new Vector2(x, y - 1), value, currentSafetyMapIsSetOnRound, currentRound);
        // Middle
        AddToSafetyMapIfValidCoordinates(new Vector2(x, y), value, currentSafetyMap);
        AddToSafetyMapIsSetOnRoundIfValidCoordinates(new Vector2(x, y), value, currentSafetyMapIsSetOnRound, currentRound);
        // Bottom Left
        AddToSafetyMapIfValidCoordinates(new Vector2(x - 1, y + 1), value, currentSafetyMap);
        AddToSafetyMapIsSetOnRoundIfValidCoordinates(new Vector2(x - 1, y + 1), value, currentSafetyMapIsSetOnRound, currentRound);
        // Left
        AddToSafetyMapIfValidCoordinates(new Vector2(x - 1, y), value, currentSafetyMap);
        AddToSafetyMapIsSetOnRoundIfValidCoordinates(new Vector2(x - 1, y), value, currentSafetyMapIsSetOnRound, currentRound);
        // Top Left
        AddToSafetyMapIfValidCoordinates(new Vector2(x - 1, y - 1), value, currentSafetyMap);
        AddToSafetyMapIsSetOnRoundIfValidCoordinates(new Vector2(x - 1, y - 1), value, currentSafetyMapIsSetOnRound, currentRound);
    }

    private void PadSafetyMapAroundCellDontOverride(
        Vector2 coordinates, SafetyValues[,]? currentSafetyMap, SafetyValues value, SafetyValues[] dontOverride)
    {
        int x = (int)coordinates.X;
        int y = (int)coordinates.Y;

        // Top Right
        AddToSafetyMapIfValidCoordinatesAndDoesNotOverrideGivenValues(
            new Vector2(x + 1, y - 1), value, currentSafetyMap, dontOverride);
        // Right
        AddToSafetyMapIfValidCoordinatesAndDoesNotOverrideGivenValues(
            new Vector2(x + 1, y), value, currentSafetyMap, dontOverride);
        // Bottom Right
        AddToSafetyMapIfValidCoordinatesAndDoesNotOverrideGivenValues(
            new Vector2(x + 1, y + 1), value, currentSafetyMap, dontOverride);
        // Bottom
        AddToSafetyMapIfValidCoordinatesAndDoesNotOverrideGivenValues(
            new Vector2(x, y + 1), value, currentSafetyMap, dontOverride);
        // Top
        AddToSafetyMapIfValidCoordinatesAndDoesNotOverrideGivenValues(
            new Vector2(x, y - 1), value, currentSafetyMap, dontOverride);
        // Middle
        AddToSafetyMapIfValidCoordinatesAndDoesNotOverrideGivenValues(
            new Vector2(x, y), value, currentSafetyMap, dontOverride);
        // Bottom Left
        AddToSafetyMapIfValidCoordinatesAndDoesNotOverrideGivenValues(
            new Vector2(x - 1, y + 1), value, currentSafetyMap, dontOverride);
        // Left
        AddToSafetyMapIfValidCoordinatesAndDoesNotOverrideGivenValues(
            new Vector2(x - 1, y), value, currentSafetyMap, dontOverride);
        // Top Left
        AddToSafetyMapIfValidCoordinatesAndDoesNotOverrideGivenValues(
            new Vector2(x - 1, y - 1), value, currentSafetyMap, dontOverride);
    }

    private Command GetRandomCommand(
        int currentMyHeat,
        Vector2 normalizedDirectionToShootTowards,
        CompassDirection currentMyDirection)
    {
        Command command = null;

        Random random = new Random();

        int randomAction = random.Next(0, 3);

        switch (randomAction)
        {
            case 0:
                command = new Command()
                {
                    Action = ActionType.Move,
                    Payload = new MoveActionData
                    {
                        Distance = 0
                    }
                };
                break;
            case 1:
                CompassDirection randomDirection = (CompassDirection)random.Next(0, 8);
                CompassDirection directionWeCanTurnTo = GetNearestDirectionWeCanTurnTo(currentMyDirection, randomDirection, MaxTurns);
                command = new Command()
                {
                    Action = ActionType.Turn,
                    Payload = new TurnActionData
                    {
                        Direction = directionWeCanTurnTo
                    }
                };
                break;
            case 2:
                ShootActionData? shootData = GetSpeedGreedyShootActionDataIfWeDontHaveTooMuchHeat(0, currentMyHeat, MaxHeat, MaxHeatLimit);

                if (shootData != null)
                {
                    return ShootIfPointingApproximatelyTowardsDirectionOrTurnTowardsIt(normalizedDirectionToShootTowards, _currentNormalizedOurDirectionVector, shootData);
                }

                break;
        }

        return command;
    }

    private Command? MoveAwayFromPosition(
        Vector2 position, Vector2 ourPosition,
        CompassDirection currentOurDirection,
        SafetyValues[,] safetyMap,
        ILogger logger)
    {
        var futureDangerCoordinatesAndCoordinatesNearEnemy = new List<Vector2>();
        var safeCoordinates = FindSafeCoordinatesAwayFromEnemy(ourPosition, position, safetyMap, futureDangerCoordinatesAndCoordinatesNearEnemy);

        if (safeCoordinates.Count == 0)
        {
            safeCoordinates = futureDangerCoordinatesAndCoordinatesNearEnemy;
        }

        if (safeCoordinates.Count == 0)
        {
            _logger.LogDebug("Could not find safe coordinates to move to!");
            return null;
        }

        Command? command = null;
        int i = 0;
        while (i < safeCoordinates.Count)
        {
            Vector2 coordinateToMoveTowards = safeCoordinates[i];

            command = MoveTowardsPointOrTurn(coordinateToMoveTowards, ourPosition, currentOurDirection, logger);
            if (command != null)
            {
                return command;
            }
            _logger.LogDebug($"Could not move to: {coordinateToMoveTowards.X}, {coordinateToMoveTowards.Y}. Trying another safe spot.");
            i++;
        }

        return null;
    }

    private Command? MoveAwayFromPositionToAnyCell(
        Vector2 position, Vector2 ourPosition,
        CompassDirection currentOurDirection,
        ILogger logger)
    {
        var coordinatesNearEnemy = new List<Vector2>();
        var safeCoordinates = FindAnyCoordinatesAwayFromEnemy(ourPosition, position, coordinatesNearEnemy);

        if (safeCoordinates.Count == 0)
        {
            safeCoordinates = coordinatesNearEnemy;
        }

        Command? command = null;
        int i = 0;
        while (i < safeCoordinates.Count)
        {
            Vector2 coordinateToMoveTowards = safeCoordinates[i];

            command = MoveTowardsPointOrTurn(coordinateToMoveTowards, ourPosition, currentOurDirection, logger);
            if (command != null)
            {
                return command;
            }
            _logger.LogDebug($"Could not move to: {coordinateToMoveTowards.X}, {coordinateToMoveTowards.Y}. Trying another safe spot.");
            i++;
        }

        return null;
    }

    private Command? MoveOrthogonallyToSafePositionIfPossible(
        Vector2 position, Vector2 ourPosition,
        CompassDirection currentOurDirection,
        ILogger logger)
    {
        Vector2 normalizedPositionDirectionVector = Vector2.Normalize(Vector2.Subtract(position, ourPosition));

        CompassDirection directionToMoveTowards = Helpers.GetApproximateDirection(normalizedPositionDirectionVector);

        var safeCoordinates = FindSafeCoordinatesPerpendicularToDirection(directionToMoveTowards, ourPosition);

        Command? command = null;
        int i = 0;
        while (i < safeCoordinates.Count)
        {
            Vector2 coordinateToMoveTowards = safeCoordinates[i];

            command = MoveTowardsPointOrTurn(coordinateToMoveTowards, ourPosition, currentOurDirection, logger);
            if (command != null)
            {
                return command;
            }
            _logger.LogDebug($"Could not move to: {coordinateToMoveTowards.X}, {coordinateToMoveTowards.Y}. Trying another safe spot.");
            i++;
        }

        return command;
    }

    private static Command? MoveTowardsPointOrTurn(
        Vector2 coordinateToMoveTowards,
        Vector2 currentOurPosition,
        CompassDirection currentOurDirection,
        ILogger logger)
    {
        Command? command;
        logger.LogDebug($"Trying to move towards: ({coordinateToMoveTowards.X},{coordinateToMoveTowards.Y}) from ({currentOurPosition.X},{currentOurPosition.Y})");
        Vector2 toMoveToDirectionVector = Vector2.Subtract(coordinateToMoveTowards, currentOurPosition);

        bool isDirectionVectorInAllowedDirections =
            toMoveToDirectionVector.X % 1 == 0 && toMoveToDirectionVector.Y % 1 == 0;

        if (!isDirectionVectorInAllowedDirections)
        {
            logger.LogDebug(
                $"Direction not allowed: {toMoveToDirectionVector.X}, {toMoveToDirectionVector.Y}");
            return null;
        }

        logger.LogDebug($"Direction allowed: {toMoveToDirectionVector.X}, {toMoveToDirectionVector.Y}");

        CompassDirection directionToMoveTowards = Helpers.GetApproximateDirection(Vector2.Normalize(toMoveToDirectionVector));

        bool doWeHaveSameDirection = directionToMoveTowards == currentOurDirection;
        if (doWeHaveSameDirection)
        {
            int distanceToMove = Math.Min(MaxMoveDistance, (int)Vector2.Distance(currentOurPosition, coordinateToMoveTowards));
            command = new Command()
            {
                Action = ActionType.Move,
                Payload = new MoveActionData
                {
                    Distance = distanceToMove
                }
            };
            logger.LogDebug($"Direction same: {toMoveToDirectionVector.X}, {toMoveToDirectionVector.Y}. Moving!");
            return command;
        }
        else
        {
            logger.LogDebug($"Direction not same: {toMoveToDirectionVector.X}, {toMoveToDirectionVector.Y}. Turning!");
            command = new Command()
            {
                Action = ActionType.Turn,
                Payload = new TurnActionData
                {
                    Direction = GetNearestDirectionWeCanTurnTo(currentOurDirection, directionToMoveTowards, MaxTurns)
                }
            };
            return command;
        }
    }

    private Command ShootIfPointingApproximatelyTowardsDirectionOrTurnTowardsIt(
        Vector2 normalizedDirectionToShootTowards,
        Vector2 ourNormalizedDirection,
        ShootActionData shootData)
    {
        bool areWeApproxFacingTheDirection = IsDirectionInOurSector(normalizedDirectionToShootTowards, ourNormalizedDirection, 0.15d);

        if (areWeApproxFacingTheDirection)
        {
            return new Command()
            {
                Action = ActionType.Shoot,
                Payload = shootData
            };
        }
        else
        {
            return new Command()
            {
                Action = ActionType.Turn,
                Payload = new TurnActionData
                {
                    Direction = GetNearestDirectionWeCanTurnTo(_currentMyShipState.Direction,
                        Helpers.GetApproximateDirection(normalizedDirectionToShootTowards), MaxTurns)
                }
            };
        }
    }

    private Command ShootIfPointingTowardsDirectionOrTurnTowardsIt(
        CompassDirection directionToShootTowards,
        CompassDirection ourDirection,
        ShootActionData shootData)
    {
        if (directionToShootTowards == ourDirection)
        {
            return new Command()
            {
                Action = ActionType.Shoot,
                Payload = shootData
            };
        }
        else
        {
            return new Command()
            {
                Action = ActionType.Turn,
                Payload = new TurnActionData
                {
                    Direction = GetNearestDirectionWeCanTurnTo(ourDirection, directionToShootTowards, MaxTurns)
                }
            };
        }
    }

    private bool IsDirectionInOurSector(Vector2 toCheckNormalized, Vector2 ourDirectionNormalized, double maxAngleDifferenceInRadians)
    {
        double vectorDotProduct = (double)Vector2.Dot(toCheckNormalized, ourDirectionNormalized);
        _logger.LogDebug($"Dot product: {vectorDotProduct}");

        double radianAngleBetweenVectors = Math.Acos(vectorDotProduct);
        _logger.LogDebug($"Degrees between vectors: {Helpers.RadiansToDegrees(radianAngleBetweenVectors)}");

        _logger.LogDebug($"Radian angle between vectors: {radianAngleBetweenVectors}. To Check: ({toCheckNormalized.X},{toCheckNormalized.Y}), Our Direction: ({ourDirectionNormalized.X},{ourDirectionNormalized.Y})");
        return radianAngleBetweenVectors <= maxAngleDifferenceInRadians;
    }

    private bool IsBorder((int x, int y) coordinate)
    {
        return coordinate.x == 0 || coordinate.x == 29 || coordinate.y == 0 || coordinate.y == 29;
    }

    private bool AddToSafetyMapIfValidCoordinates(
        Vector2 coords, SafetyValues value, SafetyValues[,] safetyValuesArray)
    {
        if (0 <= coords.X && coords.X < MapWidth && 0 <= coords.Y && coords.Y < MapWidth)
        {
            safetyValuesArray[(int)coords.X, (int)coords.Y] = value;
            return true;
        }
        return false;
    }

    private bool AddToSafetyMapIfValidCoordinatesAndDoesNotOverrideGivenValues(
        Vector2 coords, SafetyValues value, SafetyValues[,] safetyValuesArray, SafetyValues[] dontOverride)
    {
        if (0 <= coords.X && coords.X < MapWidth && 0 <= coords.Y && coords.Y < MapWidth)
        {
            SafetyValues currentValue = safetyValuesArray[(int)coords.X, (int)coords.Y];
            if (!dontOverride.Contains(currentValue))
            {
                safetyValuesArray[(int)coords.X, (int)coords.Y] = value;
                return true;
            }
        }
        return false;
    }

    private bool AddToSafetyMapIsSetOnRoundIfValidCoordinates(
        Vector2 coords, SafetyValues value, ulong[,] safetyMapIsSetOnRound, ulong currentRound)
    {
        if (0 <= coords.X && coords.X < MapWidth && 0 <= coords.Y && coords.Y < MapWidth)
        {
            safetyMapIsSetOnRound[(int)coords.X, (int)coords.Y] = currentRound;
            return true;
        }
        return false;
    }

    private List<Vector2> FindSafeCoordinatesAroundUs(List<Vector2> futureDangerCoordinates)
    {
        // Find safe coordinates around us in a widening circle:
        // All dangerous fields have been padded so all safe coordinates are safe within our hitbox.

        List<Vector2> res = new List<Vector2>();
        for (int radius = 1; radius <= MaxMoveDistance; radius++)
        {
            for (int x = _currentMyShipState!.Position.X - radius; x <= _currentMyShipState.Position.X + radius; x++)
            {
                for (int y = _currentMyShipState.Position.Y - radius; y <= _currentMyShipState.Position.Y + radius; y++)
                {
                    if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
                    {
                        continue;
                    }

                    CompassDirection directionToCoordinate = Helpers.GetApproximateDirection(Vector2.Subtract(new Vector2(x, y), _currentOurPosition));

                    if (_currentSafetyMap[x, y] == SafetyValues.Safe)
                    {
                        // Prioritize moving towards the direction we are facing:
                        if (directionToCoordinate == _currentMyShipState.Direction)
                        {
                            if (res.Count > 0)
                            {
                                res.Insert(0, new Vector2(x, y));
                            }
                            else
                            {
                                res.Add(new Vector2(x, y));
                            }
                        }
                        else
                        {
                            res.Add(new Vector2(x, y));
                        }
                    }

                    if (_currentSafetyMap[x, y] == SafetyValues.FutureDanger)
                    {
                        // Prioritize moving towards the direction we are facing:
                        if (directionToCoordinate == _currentMyShipState.Direction)
                        {
                            if (res.Count > 0)
                            {
                                futureDangerCoordinates.Insert(0, new Vector2(x, y));
                            }
                            else
                            {
                                futureDangerCoordinates.Insert(0, new Vector2(x, y));
                            }
                        }
                        else
                        {
                            futureDangerCoordinates.Add(new Vector2(x, y));
                        }
                    }
                }
            }
        }
        return res;
    }

    private List<Vector2> FindSafeCoordinatesAwayFromEnemy(
        Vector2 currentOurPosition,
        Vector2 currentEnemyPosition,
        SafetyValues[,] currentSafetyMap,
        List<Vector2> futureDangerCoordinatesAndCoordinatesNearEnemy)
    {
        // Find safe coordinates around us in a random order but away from the enemy:
        // All dangerous fields have been padded so all safe coordinates are safe within our hitbox.

        List<Vector2> res = new List<Vector2>();

        decimal distanceToEnemy = (decimal)Vector2.Distance(currentOurPosition, currentEnemyPosition);

        int currentOurPositionX = (int)currentOurPosition.X;
        int currentOurPositionY = (int)currentOurPosition.Y;

        for (int x = currentOurPositionX - MaxMoveDistance; x <= currentOurPositionX + MaxMoveDistance; x++)
        {
            for (int y = currentOurPositionY - MaxMoveDistance; y <= currentOurPositionY + MaxMoveDistance; y++)
            {
                if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
                {
                    continue;
                }

                decimal distanceToEnemyFromCoordinate = (decimal)Vector2.Distance(new Vector2(x, y), _currentEnemyPositionVector);

                if (distanceToEnemyFromCoordinate >= distanceToEnemy)
                {

                    if (currentSafetyMap[x, y] == SafetyValues.Safe)
                    {
                        res.Add(new Vector2(x, y));
                    }
                    else if (currentSafetyMap[x, y] == SafetyValues.FutureDanger)
                    {
                        futureDangerCoordinatesAndCoordinatesNearEnemy.Add(new Vector2(x, y));
                    }
                }
                else
                {
                    if (currentSafetyMap[x, y] == SafetyValues.Safe || currentSafetyMap[x, y] == SafetyValues.FutureDanger)
                    {
                        futureDangerCoordinatesAndCoordinatesNearEnemy.Add(new Vector2(x, y));
                    }
                }
            }
        }

        // randomize list order:
        Random rng = new Random();
        int n = res.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Vector2 value = res[k];
            res[k] = res[n];
            res[n] = value;
        }

        // randomize future danger list order:
        n = futureDangerCoordinatesAndCoordinatesNearEnemy.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Vector2 value = futureDangerCoordinatesAndCoordinatesNearEnemy[k];
            futureDangerCoordinatesAndCoordinatesNearEnemy[k] = futureDangerCoordinatesAndCoordinatesNearEnemy[n];
            futureDangerCoordinatesAndCoordinatesNearEnemy[n] = value;
        }

        return res;
    }

    private List<Vector2> FindAnyCoordinatesAwayFromEnemy(
        Vector2 currentOurPosition,
        Vector2 currentEnemyPosition,
        List<Vector2> coordinatesNearEnemy)
    {
        // Find any coordinates, except borders, around us in a random order but away from the enemy. 
        // Fields closer to enemy are put into coordinatesNearEnemy list.

        List<Vector2> res = new List<Vector2>();

        decimal distanceToEnemy = (decimal)Vector2.Distance(currentOurPosition, currentEnemyPosition);

        int currentOurPositionX = (int)currentOurPosition.X;
        int currentOurPositionY = (int)currentOurPosition.Y;

        for (int x = currentOurPositionX - MaxMoveDistance; x <= currentOurPositionX + MaxMoveDistance; x++)
        {
            for (int y = currentOurPositionY - MaxMoveDistance; y <= currentOurPositionY + MaxMoveDistance; y++)
            {
                if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight || IsBorder((x, y)))
                {
                    continue;
                }

                decimal distanceToEnemyFromCoordinate = (decimal)Vector2.Distance(new Vector2(x, y), _currentEnemyPositionVector);

                if (distanceToEnemyFromCoordinate >= distanceToEnemy)
                {
                    res.Add(new Vector2(x, y));
                }
                else
                {
                    coordinatesNearEnemy.Add(new Vector2(x, y));
                }
            }
        }

        // randomize list order:
        Random rng = new Random();
        int n = res.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Vector2 value = res[k];
            res[k] = res[n];
            res[n] = value;
        }

        return res;
    }

    private List<Vector2> FindSafeCoordinatesPerpendicularToDirection(
        CompassDirection direction,
        Vector2 ourPosition)
    {
        List<Vector2> res = new List<Vector2>();

        int ourPositionX = (int)ourPosition.X;
        int ourPositionY = (int)ourPosition.Y;

        ((int x, int y) leftOrthogonal, (int x, int y) rightOrthogonal) orthogonals =
            GetOrthogonalCoordinateDirectionVectorsFromDirection(direction);

        // loop both orthogonal directions for safe cells until max distance:
        for (int i = MaxMoveDistance; i > 0; i--)
        {
            // left orthogonal:
            int x = ourPositionX + (orthogonals.leftOrthogonal.x * i);
            int y = ourPositionY + (orthogonals.leftOrthogonal.y * i);

            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
            {
                break;
            }

            if (_currentSafetyMap[x, y] == SafetyValues.Safe)
            {
                res.Add(new Vector2(x, y));
            }

            // right orthogonal:
            x = ourPositionX + (orthogonals.rightOrthogonal.x * i);
            y = ourPositionY + (orthogonals.rightOrthogonal.y * i);

            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
            {
                break;
            }

            if (_currentSafetyMap[x, y] == SafetyValues.Safe)
            {
                res.Add(new Vector2(x, y));
            }
        }

        return res;
    }

    private void PrettyLogCurrentSafetyMap()
    {
        var stringBuilder = new StringBuilder();
        for (int y = 0; y < 30; y++)
        {
            string line = "";
            for (int x = 0; x < 30; x++)
            {
                line += _currentSafetyMap[x, y] switch
                {
                    SafetyValues.Safe => "0",
                    SafetyValues.InstantDanger => "X",
                    SafetyValues.FutureDanger => "x",
                    SafetyValues.Enemy => "E",
                    SafetyValues.MyShip => "M",
                    SafetyValues.Sound => "S",
                    SafetyValues.Unknown => "?",
                    _ => " "
                };
            }
            stringBuilder.AppendLine(line);
        }
        _logger.LogDebug(stringBuilder.ToString());
    }

    private enum SafetyValues
    {
        Unknown = 0,
        Safe = 1,
        InstantDanger = 2,
        FutureDanger = 3,
        Enemy = 4,
        MyShip = 5,
        Sound = 6
    }

    public static CompassDirection GetNearestDirectionWeCanTurnTo(
        CompassDirection shipDirection,
        CompassDirection directionToTurnTo,
        int maxDegreesToTurn)
    {
        const int degrees = 8;
        const int halfDegrees = degrees / 2;
        int distanceToTurn = (directionToTurnTo - shipDirection) % 8;

        if (distanceToTurn <= halfDegrees)
        {
            return (CompassDirection)((int)shipDirection + Math.Min(distanceToTurn, maxDegreesToTurn));
        }
        else
        {
            distanceToTurn = degrees - distanceToTurn;
            return (CompassDirection)((int)((shipDirection - Math.Min(distanceToTurn, maxDegreesToTurn)) + 8) % 8);
        }
    }

    private Vector2 GetDirectionVectorFromDirection(CompassDirection direction)
    {
        switch (direction)
        {
            case CompassDirection.North:
                return new Vector2(0, -1);
            case CompassDirection.NorthEast:
                return new Vector2(1, -1);
            case CompassDirection.East:
                return new Vector2(1, 0);
            case CompassDirection.SouthEast:
                return new Vector2(1, 1);
            case CompassDirection.South:
                return new Vector2(0, 1);
            case CompassDirection.SouthWest:
                return new Vector2(-1, 1);
            case CompassDirection.West:
                return new Vector2(-1, 0);
            case CompassDirection.NorthWest:
                return new Vector2(-1, -1);
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }

    private ((int x, int y) left, (int x, int y) rights) GetOrthogonalCoordinateDirectionVectorsFromDirection(CompassDirection direction)
    {
        switch (direction)
        {
            case CompassDirection.North:
                return new(new(-1, 0), new(1, 0));
            case CompassDirection.NorthEast:
                return new(new(-1, -1), new(1, -1));
            case CompassDirection.East:
                return new(new(0, -1), new(0, 1));
            case CompassDirection.SouthEast:
                return new(new(1, -1), new(-1, 1));
            case CompassDirection.South:
                return new(new(1, 0), new(-1, 0));
            case CompassDirection.SouthWest:
                return new(new(1, 1), new(-1, -1));
            case CompassDirection.West:
                return new(new(0, 1), new(0, -1));
            case CompassDirection.NorthWest:
                return new(new(-1, 1), new(1, -1));
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }

}