using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldHealthBar : MonoBehaviour
{
    [Header("Health Colors")]
    [SerializeField] private Gradient healthGradient;
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hpText;

    private Camera mainCamera;
    private GridUnit targetUnit;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void Initialize(GridUnit unit)
    {
        targetUnit = unit;
        UpdateBar();
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.forward = mainCamera.transform.forward;
        }

        if (targetUnit != null)
        {
            UpdateBar();
        }
    }

    private void UpdateBar()
    {
        if (targetUnit == null)
            return;

        float ratio = (float)targetUnit.CurrentHP / targetUnit.MaxHP;

        fillImage.fillAmount = ratio;
        fillImage.color = healthGradient.Evaluate(ratio);

        if (hpText != null)
            hpText.text = $"HP: {targetUnit.CurrentHP}/{targetUnit.MaxHP}";
    }
}