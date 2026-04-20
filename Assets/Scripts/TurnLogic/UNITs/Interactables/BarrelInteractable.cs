using UnityEngine;

public class BarrelInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private PlacedInteractable placedInteractable;
    [SerializeField] private GridManager gridManager;

    [Header("Barrel Visual States")]
    [SerializeField] private Vector3 openedLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 hiddenLocalOffset = new Vector3(0f, -0.75f, 0f);

    [Header("Runtime")]
    [SerializeField] private GridUnit hiddenUnit;

    private Vector3 initialLocalPosition;

    public bool HasHiddenUnit => hiddenUnit != null;
    public GridUnit HiddenUnit => hiddenUnit;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        initialLocalPosition = visualRoot.localPosition;
        SetBarrelOpenedVisual();
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
        SetBarrelOpenedVisual();
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
        SetBarrelOpenedVisual();
    }

    public bool CompleteHideAfterMove(GridUnit unit)
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
        hiddenState.EnterBarrel(this);

        SetBarrelHiddenVisual();

        Debug.Log($"{unit.name} entered barrel.");
        return true;
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

        hiddenUnit = unit;
        hiddenState.EnterBarrel(this);

        SetBarrelHiddenVisual();

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

        SetBarrelOpenedVisual();

        Debug.Log($"{unit.name} exited barrel.");
        return true;
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

    private void SetBarrelOpenedVisual()
    {
        if (visualRoot == null)
            return;

        visualRoot.localPosition = initialLocalPosition + openedLocalOffset;
    }

    private void SetBarrelHiddenVisual()
    {
        if (visualRoot == null)
            return;

        visualRoot.localPosition = initialLocalPosition + hiddenLocalOffset;
    }
}
