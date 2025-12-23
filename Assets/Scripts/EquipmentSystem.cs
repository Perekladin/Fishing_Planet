using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    public static EquipmentSystem Instance { get; private set; }

    public enum RodType
    {
        Basic,
        Advanced,
        Pro
    }

    [SerializeField] private RodType selectedRod = RodType.Basic;

    private const string ROD_KEY = "SelectedRod";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadRod();
    }

    // ================== PUBLIC API ==================

    public void ConfirmRod(RodType rod)
    {
        selectedRod = rod;
        PlayerPrefs.SetInt(ROD_KEY, (int)rod);
        PlayerPrefs.Save();

        Debug.Log("[EquipmentSystem] Удочка подтверждена: " + rod);
    }

    public RodType GetSelectedRod()
    {
        return selectedRod;
    }

    // ================== БОНУСЫ ==================

    public float GetWeightBonus()
    {
        switch (selectedRod)
        {
            case RodType.Advanced: return 1.0f;
            case RodType.Pro: return 2.5f;
            default: return 0f;
        }
    }

    public int GetRarityBonus()
    {
        switch (selectedRod)
        {
            case RodType.Advanced: return 1;
            case RodType.Pro: return 2;
            default: return 0;
        }
    }

    // ================== LOAD ==================

    private void LoadRod()
    {
        selectedRod = (RodType)PlayerPrefs.GetInt(ROD_KEY, 0);
        Debug.Log("[EquipmentSystem] Загружена удочка: " + selectedRod);
    }
}
