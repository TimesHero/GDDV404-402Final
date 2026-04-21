using UnityEngine;

public class BarrelInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private PlacedInteractable placedInteractable;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private InteractableRegistry interactableRegistry;

    [Header("Barrel Visual States")]
    [SerializeField] private Vector3 raisedLocalOffset = new Vector3(0f, 0.75f, 0f);
    [SerializeField] private Vector3 loweredLocalOffset = Vector3.zero;

    [Header("Runtime")]
    [SerializeField] private GridUnit hiddenUnit;

    private Vector3 initialLocalPosition;
    private Transform originalParent;

    public bool HasHiddenUnit => hiddenUnit != null;
    public GridUnit HiddenUnit => hiddenUnit;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (interactableRegistry == null)
            interactableRegistry = FindFirstObjectByType<InteractableRegistry>();

        originalParent = transform.parent;
        initialLocalPosition = visualRoot.localPosition;
        SetBarrelLoweredVisual();
        Debug.Log($"Barrel Awake -> placedInteractable: {placedInteractable != null}, gridManager: {gridManager != null}");
    }
    private void Start()
    {
        if (placedInteractable == null)
            placedInteractable = GetComponent<PlacedInteractable>();
    }

    public bool TryInteract(GridUnit unit)
    {
        if (unit == null || unit.Team != UnitTeam.Player)
            return false;

        HiddenStateComponent hiddenState = unit.GetComponent<HiddenStateComponent>();
        if (hiddenState == null)
        {
            Debug.LogWarning($"{unit.name} is missing HiddenStateComponent.");
            return false;
        }

        if (hiddenUnit == null)
            return TryHideUnit(unit, hiddenState);

        if (hiddenUnit == unit)
            return TryExitUnit(unit, hiddenState);

        return false;
    }

    public void ForceOpenBarrel()
    {
        hiddenUnit = null;
        DetachFromCarrier(GetBarrelTile());
        SetBarrelRaisedVisual();
    }

    public bool TryAbsorbHitAndBreak(GridUnit protectedUnit)
    {
        if (protectedUnit == null)
            return false;

        HiddenStateComponent hiddenState = protectedUnit.GetComponent<HiddenStateComponent>();
        if (hiddenState != null && hiddenState.CurrentBarrel == this)
            hiddenState.ExitBarrel();

        hiddenUnit = null;
        UnregisterFromRegistry();

        Destroy(gameObject);
        Debug.Log($"{protectedUnit.name}'s barrel absorbed the hit and broke.");
        return true;
    }

    public bool BreakOpenByEnemySearch()
    {
        GridUnit releasedUnit = hiddenUnit;

        if (releasedUnit != null)
        {
            HiddenStateComponent hiddenState = releasedUnit.GetComponent<HiddenStateComponent>();
            if (hiddenState != null && hiddenState.CurrentBarrel == this)
                hiddenState.ExitBarrel();
        }

        hiddenUnit = null;
        UnregisterFromRegistry();
        Destroy(gameObject);

        Debug.Log(releasedUnit != null
            ? $"{releasedUnit.name} was forced out of a searched barrel."
            : "Enemy broke an empty barrel while searching.");

        return releasedUnit != null;
    }

    public bool RemoveByPlayer(GridUnit unit)
    {
        if (unit == null || hiddenUnit != unit)
            return false;

        HiddenStateComponent hiddenState = unit.GetComponent<HiddenStateComponent>();
        if (hiddenState != null && hiddenState.CurrentBarrel == this)
            hiddenState.ExitBarrel();

        hiddenUnit = null;
        UnregisterFromRegistry();
        Destroy(gameObject);

        Debug.Log($"{unit.name} removed and destroyed their barrel.");
        return true;
    }
    
    public bool CanUnitHideHere(GridUnit unit)
    {
        if (unit == null || unit.Team != UnitTeam.Player)
            return false;

        if (hiddenUnit != null)
            return false;

        HiddenStateComponent hiddenState = unit.GetComponent<HiddenStateComponent>();
        if (hiddenState == null || !hiddenState.CanHide())
            return false;

        GridTile barrelTile = GetBarrelTile();
        if (barrelTile == null)
            return false;

        return true;
    }

    public GridTile GetBarrelTilePublic()
    {
        return GetBarrelTile();
    }

    public void PrepareForUnitEntering()
    {
        DetachFromCarrier(GetBarrelTile());
        SetBarrelRaisedVisual();
    }

    public bool CompleteHideAfterMove(GridUnit unit, bool wasSeenEntering)
    {
        if (unit == null || unit.Team != UnitTeam.Player)
            return false;

        HiddenStateComponent hiddenState = unit.GetComponent<HiddenStateComponent>();
        if (hiddenState == null || !hiddenState.CanHide())
            return false;

        GridTile barrelTile = GetBarrelTile();
        if (barrelTile == null || unit.CurrentTile != barrelTile)
            return false;

        hiddenUnit = unit;
        hiddenState.EnterBarrel(this, !wasSeenEntering, wasSeenEntering);
        unit.ApplyHiddenMovementEntryModifier();

        AttachToCarrier(unit);
        UpdatePlacedInteractableTile(unit.CurrentTile);

        SetBarrelLoweredVisual();
        EnemyVisionDetector.RefreshHiddenState(hiddenState);

        Debug.Log($"{unit.name} entered barrel.");
        return true;
    }

    public void OnCarrierTileChanged(GridTile tile)
    {
        if (hiddenUnit == null || tile == null)
            return;

        UpdatePlacedInteractableTile(tile);
    }

    private bool TryHideUnit(GridUnit unit, HiddenStateComponent hiddenState)
    {
        if (hiddenState == null || !hiddenState.CanHide())
            return false;

        GridTile barrelTile = GetBarrelTile();
        if (barrelTile == null)
            return false;

        if (unit.CurrentTile != barrelTile)
            return false;

        bool wasSeenEntering =
            EnemyVisionDetector.CanAnyEnemySeeUnit(unit) ||
            EnemyVisionDetector.CanAnyEnemySeeBarrel(this);

        hiddenUnit = unit;
        hiddenState.EnterBarrel(this, !wasSeenEntering, wasSeenEntering);
        unit.ApplyHiddenMovementEntryModifier();

        AttachToCarrier(unit);
        UpdatePlacedInteractableTile(unit.CurrentTile);

        SetBarrelLoweredVisual();
        EnemyVisionDetector.RefreshHiddenState(hiddenState);

        Debug.Log($"{unit.name} entered barrel.");
        return true;
    }

    private bool TryExitUnit(GridUnit unit, HiddenStateComponent hiddenState)
    {
        if (hiddenUnit != unit)
            return false;

        hiddenUnit = null;

        if (hiddenState != null)
            hiddenState.ExitBarrel();

        DetachFromCarrier(unit.CurrentTile);
        UpdatePlacedInteractableTile(unit.CurrentTile);
        SetBarrelRaisedVisual();

        Debug.Log($"{unit.name} exited barrel.");
        return true;
    }

    private void AttachToCarrier(GridUnit unit)
    {
        if (unit == null)
            return;

        transform.SetParent(unit.transform, true);
        transform.localRotation = Quaternion.identity;
        transform.localPosition = Vector3.zero;
    }

    private void DetachFromCarrier(GridTile tile)
    {
        transform.SetParent(originalParent, true);

        if (tile != null)
            SnapToTile(tile);
    }

    private GridTile GetBarrelTile()
    {
        if (placedInteractable == null)
            placedInteractable = GetComponent<PlacedInteractable>();

        if (placedInteractable == null || gridManager == null)
            return null;

        return gridManager.GetTileAt(placedInteractable.Origin);
    }

    private bool IsUnitAdjacentOrOnBarrel(GridUnit unit, GridTile barrelTile)
    {
        if (unit == null || unit.CurrentTile == null || barrelTile == null)
            return false;

        int dx = Mathf.Abs(unit.CurrentTile.X - barrelTile.X);
        int dy = Mathf.Abs(unit.CurrentTile.Y - barrelTile.Y);

        return (dx + dy) <= 1;
    }

    private void UpdatePlacedInteractableTile(GridTile tile)
    {
        if (placedInteractable == null || tile == null)
            return;

        placedInteractable.Origin = tile.GridPosition;
        placedInteractable.OccupiedGridPositions.Clear();
        placedInteractable.OccupiedGridPositions.Add(tile.GridPosition);
    }

    private void SnapToTile(GridTile tile)
    {
        if (tile == null)
            return;

        if (placedInteractable != null)
            UpdatePlacedInteractableTile(tile);

        Vector3 worldPosition = GetTileTopCenter(tile);

        if (placedInteractable != null && placedInteractable.Data != null)
        {
            worldPosition += placedInteractable.Data.GetVisualOffsetForRotation(placedInteractable.RotationY);
            transform.rotation = Quaternion.Euler(
                placedInteractable.Data.GetVisualRotationEulerForRotation(placedInteractable.RotationY)
            );
            transform.localScale = placedInteractable.Data.GetVisualScaleForRotation(placedInteractable.RotationY);
        }

        transform.position = worldPosition;
    }

    private Vector3 GetTileTopCenter(GridTile tile)
    {
        if (tile == null)
            return Vector3.zero;

        Renderer topRenderer = tile.GetTopRenderer();
        if (topRenderer != null)
        {
            return new Vector3(
                topRenderer.bounds.center.x,
                topRenderer.bounds.max.y,
                topRenderer.bounds.center.z
            );
        }

        return tile.transform.position;
    }

    private void SetBarrelRaisedVisual()
    {
        if (visualRoot == null)
            return;

        visualRoot.localPosition = initialLocalPosition + raisedLocalOffset;
    }

    private void SetBarrelLoweredVisual()
    {
        if (visualRoot == null)
            return;

        visualRoot.localPosition = initialLocalPosition + loweredLocalOffset;
    }

    private void OnDestroy()
    {
        UnregisterFromRegistry();
    }

    private void UnregisterFromRegistry()
    {
        if (placedInteractable == null)
            placedInteractable = GetComponent<PlacedInteractable>();

        if (interactableRegistry == null)
            interactableRegistry = FindFirstObjectByType<InteractableRegistry>();

        if (interactableRegistry != null && placedInteractable != null)
            interactableRegistry.Unregister(placedInteractable);
    }
}
