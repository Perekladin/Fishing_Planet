using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button takeButton;
    [SerializeField] private TextMeshProUGUI rodNameText;

    [Header("3D Preview")]
    [SerializeField] private Transform previewRoot; // EquipmentPreviewRoot
    [SerializeField] private GameObject[] rodPrefabs;
    [SerializeField] private string[] rodNames;
    [SerializeField] private float rotationSpeed = 40f;

    private int currentIndex;
    private GameObject currentRod;

    private void Start()
    {
        leftButton.onClick.AddListener(PrevRod);
        rightButton.onClick.AddListener(NextRod);
        takeButton.onClick.AddListener(ConfirmRod);

        currentIndex = EquipmentSystem.Instance != null
            ? (int)EquipmentSystem.Instance.GetSelectedRod()
            : 0;

        SpawnRod();
        UpdateUI();
    }

    private void Update()
    {
        RotateRod();
    }

    // ================== ROTATION ==================

    private void RotateRod()
    {
        if (currentRod == null)
            return;

        currentRod.transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime,
            Space.Self
        );
    }

    // ================== SWITCH ==================

    private void PrevRod()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = rodPrefabs.Length - 1;

        SpawnRod();
        UpdateUI();
    }

    private void NextRod()
    {
        currentIndex++;
        if (currentIndex >= rodPrefabs.Length)
            currentIndex = 0;

        SpawnRod();
        UpdateUI();
    }

    // ================== CONFIRM ==================

    private void ConfirmRod()
    {
        if (EquipmentSystem.Instance == null)
            return;

        EquipmentSystem.Instance.ConfirmRod(
            (EquipmentSystem.RodType)currentIndex
        );

        Debug.Log("[EquipmentUI] Выбрана удочка: " + rodNames[currentIndex]);
    }

    // ================== SPAWN ==================

    private void SpawnRod()
    {
        if (currentRod != null)
            Destroy(currentRod);

        currentRod = Instantiate(
            rodPrefabs[currentIndex],
            previewRoot
        );

        currentRod.transform.localPosition = Vector3.zero;
        currentRod.transform.localRotation = Quaternion.identity;
        currentRod.transform.localScale = Vector3.one;
    }

    // ================== UI ==================

    private void UpdateUI()
    {
        rodNameText.text = rodNames[currentIndex];
    }
}
