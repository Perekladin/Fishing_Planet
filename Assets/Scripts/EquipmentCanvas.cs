using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class EquipmentCanvas : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button takeButton;
    [SerializeField] private Text equipmentNameText;

    [Header("Список снаряжения")]
    [SerializeField] private EquipmentItem[] equipmentList;

    [Header("Настройки вращения")]
    [SerializeField] private float rotationSpeed = 50f;

    [Header("События")]
    [SerializeField] private UnityEvent onEquipmentTaken;

    private int currentIndex = 0;
    private string selectedEquipmentId = "";

    private void Awake()
    {
        if (leftButton != null) leftButton.onClick.AddListener(ShowPrevious);
        if (rightButton != null) rightButton.onClick.AddListener(ShowNext);
        if (takeButton != null) takeButton.onClick.AddListener(TakeEquipment);

        UpdateCurrentEquipment();
    }

    private void Update()
    {
        //  ИСПРАВЛЕНО: Вращаем ТЕКУЩИЙ активный объект снаряжения
        if (currentIndex < equipmentList.Length && equipmentList[currentIndex].equipmentObject != null)
        {
            Transform currentEquipment = equipmentList[currentIndex].equipmentObject.transform;
            currentEquipment.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }
    }

    private void ShowPrevious()
    {
        currentIndex = (currentIndex - 1 + equipmentList.Length) % equipmentList.Length;
        UpdateCurrentEquipment();
    }

    private void ShowNext()
    {
        currentIndex = (currentIndex + 1) % equipmentList.Length;
        UpdateCurrentEquipment();
    }

    private void UpdateCurrentEquipment()
    {
        if (equipmentList.Length == 0) return;

        //  Активируем только текущий объект
        for (int i = 0; i < equipmentList.Length; i++)
        {
            if (equipmentList[i].equipmentObject != null)
            {
                equipmentList[i].equipmentObject.SetActive(i == currentIndex);
            }
        }

        //  Обновляем название
        if (equipmentNameText != null && currentIndex < equipmentList.Length)
        {
            equipmentNameText.text = equipmentList[currentIndex].equipmentName;
        }
    }

    private void TakeEquipment()
    {
        if (currentIndex < equipmentList.Length && !string.IsNullOrEmpty(equipmentList[currentIndex].equipmentId))
        {
            selectedEquipmentId = equipmentList[currentIndex].equipmentId;

            PlayerPrefs.SetString("SelectedEquipmentId", selectedEquipmentId);
            PlayerPrefs.Save();

            Debug.Log($"Взято снаряжение: {equipmentList[currentIndex].equipmentName} (ID: {selectedEquipmentId})");

            onEquipmentTaken?.Invoke();
        }
    }

    public static string GetSelectedEquipmentId()
    {
        return PlayerPrefs.GetString("SelectedEquipmentId", "");
    }

    private void OnDestroy()
    {
        if (leftButton != null) leftButton.onClick.RemoveListener(ShowPrevious);
        if (rightButton != null) rightButton.onClick.RemoveListener(ShowNext);
        if (takeButton != null) takeButton.onClick.RemoveListener(TakeEquipment);
    }

    [System.Serializable]
    public class EquipmentItem
    {
        [SerializeField] public string equipmentId;
        [SerializeField] public string equipmentName;
        [SerializeField] public GameObject equipmentObject;
    }
}
