using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class FishRatingUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform buttonsContainer;       // Контейнер с кнопками рыб
    [SerializeField] private GameObject fishButtonPrefab;      // Префаб кнопки рыбы
    [SerializeField] private GameObject infoPanel;            // Панель с информацией о выбранной рыбе
    [SerializeField] private TextMeshProUGUI fishNameText;
    [SerializeField] private TextMeshProUGUI fishWeightText;
    [SerializeField] private TextMeshProUGUI fishRarityText;
    [SerializeField] private TextMeshProUGUI fishDescriptionText;
    [SerializeField] private Image fishImage;
    [SerializeField] private Button backToListButton;          // Новая кнопка на infoPanel

    private void Start()
    {
        infoPanel.SetActive(false);
        CreateButtons();

        if (backToListButton != null)
            backToListButton.onClick.AddListener(BackToFishList);
    }

    private void CreateButtons()
    {
        if (FishRatingSystem.Instance == null) return;

        var allFish = FishRatingSystem.Instance.GetAllFish();
        HashSet<string> uniqueNames = new HashSet<string>();
        foreach (var fish in allFish)
            uniqueNames.Add(fish.fishName);

        // Удаляем старые кнопки
        foreach (Transform child in buttonsContainer)
            Destroy(child.gameObject);

        // Создаем кнопки
        foreach (string fishName in uniqueNames)
        {
            GameObject btnObj = Instantiate(fishButtonPrefab, buttonsContainer);
            btnObj.transform.localScale = Vector3.one;
            btnObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = fishName;

            Button btn = btnObj.GetComponent<Button>();
            string nameCopy = fishName;
            btn.onClick.AddListener(() => ShowBestFishInfo(nameCopy));
        }
    }

    private void ShowBestFishInfo(string fishName)
    {
        if (FishRatingSystem.Instance == null) return;

        var bestFish = FishRatingSystem.Instance.GetBestFish(fishName);
        if (bestFish == null) return;

        infoPanel.SetActive(true);

        fishNameText.text = bestFish.fishName;
        fishWeightText.text = $"Вес: {bestFish.weight:F1} кг";
        fishRarityText.text = $"Редкость: {bestFish.rarity}/5";
        fishDescriptionText.text = bestFish.description ?? "Описание отсутствует";

        if (bestFish.fishSprite != null)
        {
            fishImage.sprite = bestFish.fishSprite;
            fishImage.gameObject.SetActive(true);
        }
        else
            fishImage.gameObject.SetActive(false);

        // Скрываем кнопки с выбором рыб
        buttonsContainer.gameObject.SetActive(false);
    }

    // Кнопка "Вернуться к списку рыб"
    private void BackToFishList()
    {
        infoPanel.SetActive(false);
        buttonsContainer.gameObject.SetActive(true);
    }
}
