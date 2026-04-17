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
    
    private bool hasMovedThisTurn = false;
    private bool hasAttackedThisTurn = false;

    public bool HasMovedThisTurn => hasMovedThisTurn;
    public bool HasAttackedThisTurn => hasAttackedThisTurn;
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
    public int MaxMovementPoints => unitData != null ? unitData.movementPoints : 1;
    public AttackType AttackType => unitData != null ? unitData.attackType : AttackType.Melee;
    public ElementType ElementType => unitData != null ? unitData.elementType : ElementType.None;
    public AIType AIType => unitData != null ? unitData.aiType : AIType.None;
    public int VisionRange => unitData != null ? unitData.visionRange : 1;
    public float VisionAngle => unitData != null ? unitData.visionAngle : 90f;
    
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

        MarkMovedThisTurn();
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
                    moveSpeed * Time.deltaTime);

                yield return null;
            }

            transform.position = targetPosition;
            currentTile = nextTile;
        }

        if (currentTile != null)
            currentTile.SetOccupant(gameObject);

        isMoving = false;

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

        FaceTarget(target);
        ShowAttackEffect(target);

        int finalDamage = Mathf.Max(1, AttackDamage - target.Defense);

        Debug.Log($"{name} attacks {target.name} for {finalDamage} damage.");
        target.TakeDamage(finalDamage);

        MarkAttackedThisTurn();
        return true;
    }
    
    public bool CanMoveThisTurn()
    {
        if (IsDead)
            return false;

        if (hasMovedThisTurn)
            return false;

        if (hasAttackedThisTurn)
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

        if (hasAttackedThisTurn)
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
        hasAttackedThisTurn = true;
    }

    public void ResetTurnState()
    {
        hasMovedThisTurn = false;
        hasAttackedThisTurn = false;
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
    }
    public void RestoreTurnState(bool moved, bool attacked)
    {
        hasMovedThisTurn = moved;
        hasAttackedThisTurn = attacked;
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

    public void RestoreVisualRotation(Quaternion rotation)
    {
        if (visualRoot != null)
            visualRoot.rotation = rotation;
        else
            transform.rotation = rotation;
    }
}