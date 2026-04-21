using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridUnit : MonoBehaviour
{
    [Header("Attack Visuals")]
    [SerializeField] private AttackEffectData attackEffectData;
    
    [Header("Turn Rules")]
    [SerializeField] private UnitTurnRulesData turnRules;
    
    [Header("UI")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 2f, 0);

    private WorldHealthBar healthBarInstance;
    
    [Header("Team")]
    [SerializeField] private UnitTeam team = UnitTeam.Player;
    
    [Header("Unit Data")]
    [SerializeField] private UnitData unitData;
    public int MaxClimbHeight => unitData != null ? unitData.maxClimbHeight : 1;
    
    [Header("Unit Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float groundOffset = 0.02f;
    
    
    [Header("Visual Root")]
    [SerializeField] private Transform visualRoot;

    [SerializeField] private Vector3 visualRotationOffsetEuler = Vector3.zero;
    private Quaternion visualRotationOffset;
    
    private GridTile currentTile;
    private bool isMoving;
    private int currentHP;
    private int remainingMovementPoints;
    private InteractablePlacementService interactablePlacementService;
    
    private bool hasMovedThisTurn = false;
    private int attacksUsedThisTurn = 0;

    public bool HasMovedThisTurn => hasMovedThisTurn;
    public bool HasAttackedThisTurn => attacksUsedThisTurn > 0;
    public int AttacksUsedThisTurn => attacksUsedThisTurn;
    public UnitTurnRulesData TurnRules => turnRules;
    
    public int CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0;
    public GridTile CurrentTile => currentTile;
    public bool IsMoving => isMoving;
    public UnitTeam Team => team;
    
    public UnitData UnitData => unitData;
    public string UnitName => unitData != null ? unitData.unitName : "Unnamed Unit";
    public int MaxHP => unitData != null ? unitData.maxHP : 1;
    public int AttackDamage => unitData != null ? unitData.attackPower : 0;
    public int Defense => unitData != null ? unitData.defense : 0;
    public int AttackRange => unitData != null ? unitData.attackRange : 1;
    public int MaxAttacksPerTurn => unitData != null && unitData.canAttackMultipleTimesPerTurn ? Mathf.Max(1, unitData.attacksPerTurn) : 1;
    public int MaxMovementPoints => unitData != null ? unitData.movementPoints : 1;
    public float HiddenMovementMultiplier => unitData != null ? Mathf.Clamp01(unitData.HiddenMovementMultiplier) : 1f;
    public int MovementPointsWhileHidden => GetHiddenMovementPointsFrom(MaxMovementPoints);
    public float MovementSpeedWhileHidden => unitData != null && unitData.movementSpeedWhileHidden > 0f ? unitData.movementSpeedWhileHidden : moveSpeed;
    public AttackType AttackType => unitData != null ? unitData.attackType : AttackType.Melee;
    public ElementType ElementType => unitData != null ? unitData.elementType : ElementType.None;
    public AIType AIType => unitData != null ? unitData.aiType : AIType.None;
    public int VisionRange => unitData != null ? unitData.visionRange : 1;
    public float VisionAngle => unitData != null ? unitData.visionAngle : 90f;
    public bool CanHideInBarrel => unitData == null || unitData.canHideInBarrel;
    public bool CanBackstab => unitData != null && unitData.canBackstab;
    public float BackstabDamageMultiplier => unitData != null ? Mathf.Max(1f, unitData.backstabDamageMultiplier) : 1f;
    public int BackstabBonusDamage => unitData != null ? Mathf.Max(0, unitData.backstabBonusDamage) : 0;
    public bool CanPushAbility => unitData == null || unitData.canPush;
    public bool CanBePushed => unitData == null || unitData.canBePushed;
    public bool UsesPushWeightSystem => unitData != null && unitData.usePushWeightSystem;
    public int PushWeight => unitData != null ? Mathf.Max(0, unitData.pushWeight) : 1;
    public int PushDistancePerWeightDifference => unitData != null ? Mathf.Max(0, unitData.pushDistancePerWeightDifference) : 0;
    public int PushDistance => unitData != null ? Mathf.Max(1, unitData.pushDistance) : 1;
    public int RemainingMovementPoints => remainingMovementPoints;
    
    public System.Action<GridUnit> OnMovementFinished;
    
    public int GetMovementCostForTile(GridTile tile, bool isFinalDestination = false)
    {
        if (tile == null)
            return int.MaxValue;

        return tile.GetTraversalCost(isFinalDestination);
    }
    
    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        visualRotationOffset = Quaternion.Euler(visualRotationOffsetEuler);

        if (unitData != null)
        {
            currentHP = MaxHP;
            remainingMovementPoints = MaxMovementPoints;
        }

        interactablePlacementService = FindFirstObjectByType<InteractablePlacementService>();
        
        if (GetComponent<HiddenStateComponent>() == null)
        {
            gameObject.AddComponent<HiddenStateComponent>();
        }
    }
    
    private void Start()
    {
        if (healthBarPrefab != null)
        {
            GameObject bar = Instantiate(healthBarPrefab, transform);
            bar.transform.localPosition = healthBarOffset;

            healthBarInstance = bar.GetComponent<WorldHealthBar>();
            healthBarInstance.Initialize(this);
        }
    }

    public void PlaceOnTile(GridTile tile)
    {
        if (tile == null)
            return;

        if (currentTile != null)
            currentTile.SetOccupant(null);

        currentTile = tile;
        currentTile.SetOccupant(gameObject);

        transform.position = GetGroundedWorldPosition(tile);

        HiddenStateComponent hiddenState = GetComponent<HiddenStateComponent>();
        if (hiddenState != null && hiddenState.CurrentBarrel != null)
        {
            hiddenState.CurrentBarrel.OnCarrierTileChanged(tile);
            EnemyVisionDetector.RefreshHiddenState(hiddenState);
        }
    }
    
    public bool TryMove(List<GridTile> path)
    {
        if (!CanMoveThisTurn())
            return false;

        if (path == null || path.Count == 0)
            return false;

        if (isMoving)
            return false;

        if (currentTile == null)
            return false;

        if (path[0] != currentTile)
            return false;

        int movementCost = CalculatePathMovementCost(path);
        if (movementCost <= 0)
            return false;

        if (movementCost > remainingMovementPoints)
            return false;

        MarkMovedThisTurn();
        remainingMovementPoints -= movementCost;
        MoveAlongPath(path);
        return true;
    }
    
    public void MoveAlongPath(List<GridTile> path)
    {
        if (path == null || path.Count == 0)
            return;

        if (isMoving)
            return;
        
        //not in use anymore
        //List<GridTile> pathCopy = new List<GridTile>(path);
        
        //////////TurnManager//////////////
        if (TurnManager.Instance != null)
            TurnManager.Instance.SetBusy();
        ///////////////////////////////////
        
        StartCoroutine(MoveRoutine(path));
    }

    private IEnumerator MoveRoutine(List<GridTile> path)
    {
        isMoving = true;

        if (currentTile != null)
            currentTile.SetOccupant(null);

        for (int i = 1; i < path.Count; i++)
        {
            GridTile nextTile = path[i];
            Vector3 targetPosition = GetGroundedWorldPosition(nextTile);

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                Vector3 moveDirection = targetPosition - transform.position;
                moveDirection.y = 0f;

                if (moveDirection.sqrMagnitude > 0.0001f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);

                    Quaternion finalRotation = lookRotation * visualRotationOffset;
                    
                    visualRoot.rotation = Quaternion.Slerp(
                        visualRoot.rotation,
                        finalRotation,
                        rotationSpeed * Time.deltaTime);
                }

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    GetCurrentMoveSpeed() * Time.deltaTime);

                yield return null;
            }

            transform.position = targetPosition;
            currentTile = nextTile;
            ApplyTerrainEntryEffects(currentTile, false);

            HiddenStateComponent hiddenState = GetComponent<HiddenStateComponent>();
            if (hiddenState != null && hiddenState.CurrentBarrel != null)
            {
                hiddenState.CurrentBarrel.OnCarrierTileChanged(currentTile);
                EnemyVisionDetector.RevealHiddenByVisibleBarrelMovement(hiddenState);
                EnemyController.NotifyEnemiesOfVisibleBarrelCarrier(this);
            }

            if (Team == UnitTeam.Player)
                EnemyController.NotifyEnemiesOfVisiblePlayer(this);
        }

        if (currentTile != null)
            currentTile.SetOccupant(gameObject);

        isMoving = false;

        HiddenStateComponent finalHiddenState = GetComponent<HiddenStateComponent>();
        if (finalHiddenState != null && finalHiddenState.CurrentBarrel != null)
            EnemyVisionDetector.RefreshHiddenState(finalHiddenState);

        if (TurnManager.Instance != null && Team == UnitTeam.Player)
        {
            TurnManager.Instance.ReturnToPlayerControl();
        }
        
        OnMovementFinished?.Invoke(this);
    }
    
    private Vector3 GetGroundedWorldPosition(GridTile tile)
    {
        Vector3 tileTopCenter = tile.transform.position + Vector3.up * GetTileTopY(tile);
        float unitBottomOffset = GetUnitBottomOffset();

        return new Vector3(
            tileTopCenter.x,
            tileTopCenter.y + unitBottomOffset + groundOffset,
            tileTopCenter.z);
    }

    private float GetTileTopY(GridTile tile)
    {
        Renderer tileRenderer = tile.GetComponentInChildren<Renderer>();

        if (tileRenderer != null)
            return tileRenderer.bounds.max.y - tile.transform.position.y;

        Collider tileCollider = tile.GetComponent<Collider>();
        if (tileCollider != null)
            return tileCollider.bounds.max.y - tile.transform.position.y;

        return 0f;
    }

    private float GetUnitBottomOffset()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return 0f;

        Bounds combinedBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);

        return transform.position.y - combinedBounds.min.y;
    }
    
    public void TakeDamage(int amount)
    {
        if (IsDead)
            return;

        HiddenStateComponent hiddenState = GetComponent<HiddenStateComponent>();
        if (hiddenState != null && hiddenState.CurrentBarrel != null)
        {
            if (hiddenState.CurrentBarrel.TryAbsorbHitAndBreak(this))
                return;
        }

        if (hiddenState != null && hiddenState.IsHidden)
            hiddenState.ForceReveal();

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0);

        Debug.Log($"{name} took {amount} damage. HP: {currentHP}/{MaxHP}");

        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log($"{name} died.");

        if (currentTile != null)
            currentTile.SetOccupant(null);

        gameObject.SetActive(false);

        if (BattleStateManager.Instance != null)
            BattleStateManager.Instance.NotifyUnitDied(this);
    }
    
    public bool IsTargetInRange(GridUnit target)
    {
        if (target == null || target.CurrentTile == null || CurrentTile == null)
            return false;

        int distance = Mathf.Abs(CurrentTile.X - target.CurrentTile.X) +
                       Mathf.Abs(CurrentTile.Y - target.CurrentTile.Y);

        return distance <= AttackRange;
    }
    
    public bool CanAttack(GridUnit target)
    {
        if (target == null)
            return false;

        if (target == this)
            return false;

        if (target.Team == Team)
            return false;

        return IsTargetInRange(target);
    }

    public bool TryAttack(GridUnit target)
    {
        if (!CanAttackThisTurn())
            return false;

        if (!CanAttack(target))
            return false;

        bool isBackstab = CanPerformBackstabOn(target);
        FaceTarget(target);
        ShowAttackEffect(target);

        int finalDamage = CalculateAttackDamage(target, isBackstab);

        Debug.Log(isBackstab
            ? $"{name} backstabs {target.name} for {finalDamage} damage."
            : $"{name} attacks {target.name} for {finalDamage} damage.");
        target.TakeDamage(finalDamage);

        HiddenStateComponent hiddenState = GetComponent<HiddenStateComponent>();
        if (hiddenState != null && hiddenState.IsHidden)
            hiddenState.ForceReveal();

        if (Team == UnitTeam.Player && target.Team == UnitTeam.Enemy)
            EnemyController.NotifyEnemyHitByAttacker(target, this);

        MarkAttackedThisTurn();
        return true;
    }

    public bool CanPush(GridUnit target, GridManager gridManager)
    {
        if (!CanAttackThisTurn())
            return false;

        if (!CanPushAbility)
            return false;

        if (target == null || target == this || target.IsDead)
            return false;

        if (!target.CanBePushed)
            return false;

        if (!CanPushTargetByWeight(target))
            return false;

        if (target.Team == Team)
            return false;

        if (CurrentTile == null || target.CurrentTile == null || gridManager == null)
            return false;

        Vector2Int pushDirection = GetPushDirectionToTarget(target);
        if (pushDirection == Vector2Int.zero)
            return false;

        return GetPushDestination(target, gridManager, pushDirection, GetFinalPushDistanceAgainst(target)) != null;
    }

    public bool CanPushTarget(GridUnit target)
    {
        if (!CanAttackThisTurn())
            return false;

        if (!CanPushAbility)
            return false;

        if (target == null || target == this || target.IsDead)
            return false;

        if (!target.CanBePushed)
            return false;

        if (!CanPushTargetByWeight(target))
            return false;

        return target.Team != Team;
    }

    public GridTile GetPushDestinationFromTile(GridUnit target, GridManager gridManager, GridTile pusherTile)
    {
        if (!CanPushTarget(target) || target == null || target.CurrentTile == null || pusherTile == null)
            return null;

        Vector2Int pushDirection = target.CurrentTile.GridPosition - pusherTile.GridPosition;
        int manhattanDistance = Mathf.Abs(pushDirection.x) + Mathf.Abs(pushDirection.y);

        if (manhattanDistance != 1)
            return null;

        return GetPushDestination(target, gridManager, pushDirection, GetFinalPushDistanceAgainst(target));
    }

    public int GetFinalPushDistanceAgainstPublic(GridUnit target)
    {
        return GetFinalPushDistanceAgainst(target);
    }

    public bool TryPush(GridUnit target, GridManager gridManager, bool restoreControlAfterPush = true)
    {
        if (!CanPush(target, gridManager))
            return false;

        Vector2Int pushDirection = GetPushDirectionToTarget(target);
        GridTile destinationTile = GetPushDestination(target, gridManager, pushDirection, GetFinalPushDistanceAgainst(target));
        if (destinationTile == null)
            return false;

        FaceTarget(target);

        List<GridTile> pushPath = BuildPushPath(target.CurrentTile, destinationTile, gridManager, pushDirection);
        if (pushPath == null || pushPath.Count <= 1)
            return false;

        target.ForceMoveAlongPath(pushPath, restoreControlAfterPush);

        if (Team == UnitTeam.Player && target.Team == UnitTeam.Enemy)
            EnemyController.NotifyEnemyHitByAttacker(target, this);

        MarkAttackedThisTurn();
        Debug.Log($"{name} pushes {target.name}.");
        return true;
    }
    
    public bool CanMoveThisTurn()
    {
        if (IsDead)
            return false;

        if (remainingMovementPoints <= 0)
            return false;

        if (HasAttackedThisTurn)
        {
            if (turnRules == null)
                return false;

            return turnRules.CanMoveAfterAttacking;
        }

        return true;
    }

    public bool CanAttackThisTurn()
    {
        if (IsDead)
            return false;

        if (attacksUsedThisTurn >= MaxAttacksPerTurn)
            return false;

        if (hasMovedThisTurn)
        {
            if (turnRules == null)
                return false;

            return turnRules.CanAttackAfterMoving;
        }

        return true;
    }

    public void MarkMovedThisTurn()
    {
        hasMovedThisTurn = true;
    }

    public void MarkAttackedThisTurn()
    {
        attacksUsedThisTurn = Mathf.Min(attacksUsedThisTurn + 1, MaxAttacksPerTurn);
    }

    public void ForceMoveAlongPath(List<GridTile> path, bool restorePlayerControlWhenFinished = true)
    {
        if (path == null || path.Count == 0)
            return;

        if (isMoving)
            return;

        if (TurnManager.Instance != null)
            TurnManager.Instance.SetBusy();

        StartCoroutine(ForcedMoveRoutine(path, restorePlayerControlWhenFinished));
    }

    public void ResetTurnState()
    {
        hasMovedThisTurn = false;
        attacksUsedThisTurn = 0;
        remainingMovementPoints = IsInsideBarrel() ? MovementPointsWhileHidden : MaxMovementPoints;
    }

    public void ApplyHiddenMovementEntryModifier()
    {
        remainingMovementPoints = GetHiddenMovementPointsFrom(remainingMovementPoints);
        Debug.Log($"{name} hidden movement adjusted to {remainingMovementPoints} MP.");
    }

    public void ApplyTerrainStartTurnEffects()
    {
        if (currentTile == null || IsDead)
            return;

        TerrainTypeData terrainData = currentTile.CurrentTerrainData;
        if (terrainData == null)
            return;

        ApplyMovementPenalty(terrainData.MovementPenaltyOnStop, "terrain start");

        if (terrainData.DamageOnStop > 0)
        {
            Debug.Log($"{name} takes {terrainData.DamageOnStop} terrain damage for staying on {currentTile.TerrainType}.");
            TakeDamage(terrainData.DamageOnStop);
        }
    }
    
    private void FaceTarget(GridUnit target)
    {
        if (target == null || visualRoot == null)
            return;

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Quaternion finalRotation = lookRotation * visualRotationOffset;

        visualRoot.rotation = finalRotation;
    }
    
    private void ShowAttackEffect(GridUnit target)
    {
        if (attackEffectData == null || attackEffectData.EffectPrefab == null)
            return;

        Vector3 spawnPosition = transform.position + attackEffectData.PositionOffset;

        GameObject effectInstance = Instantiate(
            attackEffectData.EffectPrefab,
            spawnPosition,
            Quaternion.identity
        );

        effectInstance.transform.localScale = Vector3.zero;

        StartCoroutine(AnimateAttackEffect(effectInstance));
    }
    private IEnumerator AnimateAttackEffect(GameObject effectInstance)
    {
        if (effectInstance == null || attackEffectData == null)
            yield break;

        Vector3 startPosition = effectInstance.transform.position;
        Vector3 endPosition = startPosition + Vector3.up * attackEffectData.RiseAmount;

        float elapsed = 0f;
        
        while (elapsed < attackEffectData.PopInDuration)
        {
            if (effectInstance == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / attackEffectData.PopInDuration);

            effectInstance.transform.localScale = Vector3.Lerp(Vector3.zero, attackEffectData.EffectScale, t);
            effectInstance.transform.position = Vector3.Lerp(startPosition, endPosition, t);

            yield return null;
        }

        effectInstance.transform.localScale = attackEffectData.EffectScale;
        effectInstance.transform.position = endPosition;
        
        float remainingTime = Mathf.Max(0f, attackEffectData.Duration - attackEffectData.PopInDuration);
        yield return new WaitForSeconds(remainingTime);

        if (effectInstance != null)
            Destroy(effectInstance);
    }
    public void InitializeFromData(UnitData data)
    {
        if (data == null)
        {
            Debug.LogError($"GridUnit on {gameObject.name} received null UnitData.");
            return;
        }

        unitData = data;
        currentHP = MaxHP;
        remainingMovementPoints = MaxMovementPoints;
    }

    private IEnumerator ForcedMoveRoutine(List<GridTile> path, bool restorePlayerControlWhenFinished)
    {
        isMoving = true;

        if (currentTile != null)
            currentTile.SetOccupant(null);

        for (int i = 1; i < path.Count; i++)
        {
            GridTile nextTile = path[i];
            Vector3 targetPosition = GetGroundedWorldPosition(nextTile);

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                Vector3 moveDirection = targetPosition - transform.position;
                moveDirection.y = 0f;

                if (moveDirection.sqrMagnitude > 0.0001f && visualRoot != null)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
                    Quaternion finalRotation = lookRotation * visualRotationOffset;
                    visualRoot.rotation = Quaternion.Slerp(
                        visualRoot.rotation,
                        finalRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    GetCurrentMoveSpeed() * Time.deltaTime
                );

                yield return null;
            }

            transform.position = targetPosition;
            currentTile = nextTile;
            ApplyTerrainEntryEffects(currentTile, true);
        }

        if (currentTile != null)
            currentTile.SetOccupant(gameObject);

        isMoving = false;

        if (restorePlayerControlWhenFinished && TurnManager.Instance != null)
            TurnManager.Instance.ReturnToPlayerControl();

        OnMovementFinished?.Invoke(this);
    }
    public void RestoreTurnState(bool moved, bool attacked, int restoredMovementPoints)
    {
        RestoreTurnState(moved, attacked ? 1 : 0, restoredMovementPoints);
    }

    public void RestoreTurnState(bool moved, int restoredAttacksUsedThisTurn, int restoredMovementPoints)
    {
        hasMovedThisTurn = moved;
        attacksUsedThisTurn = Mathf.Clamp(restoredAttacksUsedThisTurn, 0, MaxAttacksPerTurn);
        remainingMovementPoints = Mathf.Clamp(restoredMovementPoints, 0, MaxMovementPoints);
    }

    public void RestoreHealth(int hp)
    {
        currentHP = Mathf.Clamp(hp, 0, MaxHP);
    }

    public void RestoreAliveState(bool shouldBeDead)
    {
        gameObject.SetActive(!shouldBeDead);
    }
    
    public Quaternion GetVisualRotation()
    {
        if (visualRoot != null)
            return visualRoot.rotation;

        return transform.rotation;
    }

    public Vector3 GetVisualForward()
    {
        if (visualRoot != null)
            return visualRoot.forward;

        return transform.forward;
    }

    public void RestoreVisualRotation(Quaternion rotation)
    {
        if (visualRoot != null)
            visualRoot.rotation = rotation;
        else
            transform.rotation = rotation;
    }

    private int CalculatePathMovementCost(List<GridTile> path)
    {
        if (path == null || path.Count <= 1)
            return 0;

        int totalCost = 0;

        for (int i = 1; i < path.Count; i++)
        {
            GridTile tile = path[i];
            bool isFinalDestination = i == path.Count - 1;
            totalCost += GetMovementCostForTile(tile, isFinalDestination);
        }

        return totalCost;
    }

    public int EstimateTerrainEntryDamage(GridTile tile)
    {
        TerrainTypeData terrainData = tile != null ? tile.CurrentTerrainData : null;
        return terrainData != null ? Mathf.Max(0, terrainData.DamageOnEnter) : 0;
    }

    public int EstimateTerrainStartTurnDamage(GridTile tile)
    {
        TerrainTypeData terrainData = tile != null ? tile.CurrentTerrainData : null;
        return terrainData != null ? Mathf.Max(0, terrainData.DamageOnStop) : 0;
    }

    private void ApplyTerrainEntryEffects(GridTile tile, bool applyMovementPenalty)
    {
        if (tile == null || IsDead)
            return;

        TerrainTypeData terrainData = tile.CurrentTerrainData;
        if (terrainData == null)
            return;

        if (applyMovementPenalty)
            ApplyMovementPenalty(terrainData.MovementPenaltyOnEntry, "terrain entry");

        if (terrainData.DamageOnEnter > 0)
        {
            Debug.Log($"{name} takes {terrainData.DamageOnEnter} terrain damage entering {tile.TerrainType}.");
            TakeDamage(terrainData.DamageOnEnter);
        }
    }

    private void ApplyMovementPenalty(int penalty, string source)
    {
        if (penalty <= 0)
            return;

        int oldMovement = remainingMovementPoints;
        remainingMovementPoints = Mathf.Max(0, remainingMovementPoints - penalty);

        Debug.Log($"{name} loses {oldMovement - remainingMovementPoints} movement from {source}. Remaining MP: {remainingMovementPoints}.");
    }

    private Vector2Int GetPushDirectionToTarget(GridUnit target)
    {
        if (target == null || CurrentTile == null || target.CurrentTile == null)
            return Vector2Int.zero;

        Vector2Int delta = target.CurrentTile.GridPosition - CurrentTile.GridPosition;
        int manhattanDistance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

        if (manhattanDistance != 1)
            return Vector2Int.zero;

        return delta;
    }

    private GridTile GetPushDestination(GridUnit target, GridManager gridManager, Vector2Int pushDirection, int finalPushDistance)
    {
        if (target == null || target.CurrentTile == null || gridManager == null || pushDirection == Vector2Int.zero)
            return null;

        GridTile lastValidTile = null;
        Vector2Int currentPosition = target.CurrentTile.GridPosition;

        for (int i = 0; i < Mathf.Max(1, finalPushDistance); i++)
        {
            currentPosition += pushDirection;
            GridTile candidateTile = gridManager.GetTileAt(currentPosition);

            if (!CanBePushedIntoTile(candidateTile))
                break;

            lastValidTile = candidateTile;
        }

        return lastValidTile;
    }

    private bool CanPushTargetByWeight(GridUnit target)
    {
        if (target == null)
            return false;

        if (!UsesPushWeightSystem && !target.UsesPushWeightSystem)
            return true;

        return PushWeight > target.PushWeight;
    }

    private int GetFinalPushDistanceAgainst(GridUnit target)
    {
        int finalDistance = PushDistance;

        if (target == null)
            return finalDistance;

        if (!UsesPushWeightSystem && !target.UsesPushWeightSystem)
            return finalDistance;

        int weightDifference = Mathf.Max(0, PushWeight - target.PushWeight);
        int distanceMultiplier = PushDistancePerWeightDifference;

        if (distanceMultiplier <= 0)
            return finalDistance;

        return finalDistance + (weightDifference / distanceMultiplier);
    }

    private List<GridTile> BuildPushPath(GridTile startTile, GridTile destinationTile, GridManager gridManager, Vector2Int pushDirection)
    {
        if (startTile == null || destinationTile == null || gridManager == null || pushDirection == Vector2Int.zero)
            return null;

        List<GridTile> path = new List<GridTile> { startTile };
        Vector2Int currentPosition = startTile.GridPosition;

        while (currentPosition != destinationTile.GridPosition)
        {
            currentPosition += pushDirection;
            GridTile tile = gridManager.GetTileAt(currentPosition);
            if (tile == null)
                return null;

            path.Add(tile);
        }

        return path;
    }

    private bool CanBePushedIntoTile(GridTile tile)
    {
        if (tile == null)
            return false;

        if (!tile.isWalkable)
            return false;

        if (tile.isOccupied)
            return false;

        if (interactablePlacementService == null)
            interactablePlacementService = FindFirstObjectByType<InteractablePlacementService>();

        if (interactablePlacementService != null &&
            interactablePlacementService.GetPlacedInteractableAtTile(tile) != null)
            return false;

        return true;
    }

    private bool IsInsideBarrel()
    {
        HiddenStateComponent hiddenState = GetComponent<HiddenStateComponent>();
        return hiddenState != null && hiddenState.CurrentBarrel != null;
    }

    private float GetCurrentMoveSpeed()
    {
        return IsInsideBarrel() ? MovementSpeedWhileHidden : moveSpeed;
    }

    private int CalculateAttackDamage(GridUnit target, bool isBackstab)
    {
        if (target == null)
            return 0;

        int baseDamage = Mathf.Max(1, AttackDamage - target.Defense);

        if (!isBackstab)
            return baseDamage;

        float multipliedDamage = baseDamage * BackstabDamageMultiplier;
        return Mathf.Max(1, Mathf.CeilToInt(multipliedDamage) + BackstabBonusDamage);
    }

    private bool CanPerformBackstabOn(GridUnit target)
    {
        if (!CanBackstab || target == null || target.CurrentTile == null || CurrentTile == null)
            return false;

        HiddenStateComponent hiddenState = GetComponent<HiddenStateComponent>();
        bool isHiddenOrUntracked =
            (hiddenState != null && hiddenState.IsHidden) ||
            !EnemyController.AreEnemiesAwareOfPlayer(this);

        if (!isHiddenOrUntracked)
            return false;

        Vector3 targetForward = target.GetVisualForward();
        targetForward.y = 0f;

        if (targetForward.sqrMagnitude <= 0.0001f)
            return false;

        Vector3 targetToAttacker = transform.position - target.transform.position;
        targetToAttacker.y = 0f;

        if (targetToAttacker.sqrMagnitude <= 0.0001f)
            return false;

        float dot = Vector3.Dot(targetForward.normalized, targetToAttacker.normalized);
        return dot <= -0.7f;
    }

    private int GetHiddenMovementPointsFrom(int baseMovementPoints)
    {
        if (baseMovementPoints <= 0)
            return 0;

        return Mathf.Max(1, Mathf.CeilToInt(baseMovementPoints * HiddenMovementMultiplier));
    }
}
