using UnityEngine;

public class HiddenStateComponent : MonoBehaviour
{
    [Header("Runtime State")]
    [SerializeField] private bool isHidden;
    [SerializeField] private BarrelInteractable currentBarrel;
    [SerializeField] private bool barrelKnownToEnemies;

    private GridUnit ownerUnit;

    public bool IsHidden => isHidden;
    public BarrelInteractable CurrentBarrel => currentBarrel;
    public bool BarrelKnownToEnemies => barrelKnownToEnemies;

    private void Awake()
    {
        ownerUnit = GetComponent<GridUnit>();
    }

    public bool CanHide()
    {
        return ownerUnit != null &&
               ownerUnit.Team == UnitTeam.Player &&
               !ownerUnit.IsDead &&
               ownerUnit.CanHideInBarrel;
    }

    public void EnterBarrel(BarrelInteractable barrel, bool startHidden = true, bool knownToEnemies = false)
    {
        if (!CanHide() || barrel == null)
            return;

        isHidden = startHidden;
        currentBarrel = barrel;
        barrelKnownToEnemies = knownToEnemies;
    }

    public void ExitBarrel()
    {
        isHidden = false;
        currentBarrel = null;
        barrelKnownToEnemies = false;
    }

    public void SetHiddenState(bool hidden, bool knownToEnemies = false)
    {
        isHidden = hidden;
        barrelKnownToEnemies = knownToEnemies;
    }

    public void ForceReveal()
    {
        isHidden = false;
        barrelKnownToEnemies = currentBarrel != null;
    }
}
