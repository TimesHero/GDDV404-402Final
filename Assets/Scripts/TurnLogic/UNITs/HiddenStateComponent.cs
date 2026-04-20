using UnityEngine;

public class HiddenStateComponent : MonoBehaviour
{
    [Header("Runtime State")]
    [SerializeField] private bool isHidden;
    [SerializeField] private BarrelInteractable currentBarrel;

    private GridUnit ownerUnit;

    public bool IsHidden => isHidden;
    public BarrelInteractable CurrentBarrel => currentBarrel;

    private void Awake()
    {
        ownerUnit = GetComponent<GridUnit>();
    }

    public bool CanHide()
    {
        return ownerUnit != null && ownerUnit.Team == UnitTeam.Player && !ownerUnit.IsDead;
    }

    public void EnterBarrel(BarrelInteractable barrel)
    {
        if (!CanHide() || barrel == null)
            return;

        isHidden = true;
        currentBarrel = barrel;
    }

    public void ExitBarrel()
    {
        isHidden = false;
        currentBarrel = null;
    }

    public void ForceReveal()
    {
        if (currentBarrel != null)
            currentBarrel.ForceOpenBarrel();

        ExitBarrel();
    }
}