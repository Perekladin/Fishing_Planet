using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class FishRating : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private TextMeshProUGUI fishNameText;
    [SerializeField] private TextMeshProUGUI fishWeightText;
    [SerializeField] private TextMeshProUGUI fishRarityText;
    [SerializeField] private Image fishImage;
    [SerializeField] private GameObject ratingPanel;
    [SerializeField] private Slider ratingSlider;
    [SerializeField] private TextMeshProUGUI ratingScoreText;
    [SerializeField] private Image ratingBackground;

    [Header("Настройки рейтинга")]
    [SerializeField] private float maxWeight = 15f;
    [SerializeField] private int maxRarity = 5;
    [SerializeField] private Color[] starColors = new Color[6];

    [Header("История улова")]
    [SerializeField] private Transform catchHistoryContainer;
    [SerializeField] private GameObject catchHistoryItemPrefab;
    [SerializeField] private int maxHistoryItems = 10;

    private List<FishRatingData> catchHistory = new List<FishRatingData>();
    private int totalFishCaught = 0;

    [System.Serializable]
    public class FishRatingData
    {
        public string fishName;
        public float weight;
        public Sprite fishSprite;
        public int rarity;
        public float ratingScore;
    }

    private void Start()
    {
        ratingPanel.SetActive(false);
        LoadCatchHistory();
        UpdateHistoryUI();

        // Инициализация цветов звезд
        if (starColors.Length == 0)
        {
            starColors = new Color[]
            {
                new Color(0.5f, 0.5f, 0.5f),  // Серый (0 звезд)
                Color.yellow,                  // Желтый (1)
                Color.yellow,                  // Желтый (2)
                Color.yellow,                  // Желтый (3)
                Color.cyan,                    // Голубой (4)
                Color.magenta                  // Фиолетовый (5)
            };
        }
    }

    //  ПРИНИМАЕТ Fishing.FishData и конвертирует
    public void AddFishToRating(Fishing.FishData fishingFish)
    {
        // Конвертируем Fishing.FishData в наш FishRatingData
        FishRatingData ratingFish = new FishRatingData
        {
            fishName = fishingFish.fishName,
            weight = fishingFish.weight,
            fishSprite = fishingFish.fishSprite,
            rarity = fishingFish.rarity,
            ratingScore = CalculateRatingScore(fishingFish)
        };

        // Добавляем в историю
        catchHistory.Add(ratingFish);
        totalFishCaught++;

        // Ограничиваем историю
        if (catchHistory.Count > maxHistoryItems)
        {
            catchHistory.RemoveAt(0);
        }

        // Обновляем UI
        ShowRatingPanel(ratingFish);
        UpdateHistoryUI();
        SaveCatchHistory();

        Debug.Log($"Рыба добавлена в рейтинг: {ratingFish.fishName} | Рейтинг: {ratingFish.ratingScore:F1}/100");
    }

    private float CalculateRatingScore(Fishing.FishData fish)
    {
        float weightScore = (fish.weight / maxWeight) * 50f;    // 50% за вес
        float rarityScore = (fish.rarity / (float)maxRarity) * 50f; // 50% за редкость

        return Mathf.Clamp(weightScore + rarityScore, 0f, 100f);
    }

    private void ShowRatingPanel(FishRatingData fish)
    {
        ratingPanel.SetActive(true);

        // Заполняем текущую рыбу
        fishNameText.text = fish.fishName;
        fishWeightText.text = "Вес: " + fish.weight.ToString("F2") + " кг";
        fishRarityText.text = "Редкость: " + fish.rarity + "/5";

        if (fish.fishSprite != null)
        {
            fishImage.sprite = fish.fishSprite;
            fishImage.gameObject.SetActive(true);
        }
        else
        {
            fishImage.gameObject.SetActive(false);
        }

        // Рейтинг
        float normalizedScore = fish.ratingScore / 100f;
        ratingSlider.value = normalizedScore;
        ratingScoreText.text = fish.ratingScore.ToString("F1") + "/100";

        // Цвет фона по звездам
        int starRating = Mathf.RoundToInt(normalizedScore * 5f);
        starRating = Mathf.Clamp(starRating, 0, 5);
        if (starRating < starColors.Length)
            ratingBackground.color = starColors[starRating];
    }

    private void UpdateHistoryUI()
    {
        // Очищаем старые элементы
        foreach (Transform child in catchHistoryContainer)
        {
            Destroy(child.gameObject);
        }

        // Создаем новые
        foreach (var fish in catchHistory)
        {
            CreateHistoryItem(fish);
        }
    }

    private void CreateHistoryItem(FishRatingData fish)
    {
        if (catchHistoryItemPrefab == null) return;

        GameObject item = Instantiate(catchHistoryItemPrefab, catchHistoryContainer);

        // Находим компоненты в префабе
        TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
        Image itemImage = item.GetComponentInChildren<Image>();

        if (texts.Length >= 2)
        {
            texts[0].text = fish.fishName;  // Название
            texts[1].text = fish.weight.ToString("F1") + "кг (" + fish.ratingScore.ToString("F0") + ")"; // Вес + рейтинг
        }

        if (itemImage != null && fish.fishSprite != null)
        {
            itemImage.sprite = fish.fishSprite;
        }
    }

    private void SaveCatchHistory()
    {
        for (int i = 0; i < catchHistory.Count; i++)
        {
            var fish = catchHistory[i];
            string key = "FishHistory_" + i;
            PlayerPrefs.SetString(key + "_Name", fish.fishName);
            PlayerPrefs.SetFloat(key + "_Weight", fish.weight);
            PlayerPrefs.SetInt(key + "_Rarity", fish.rarity);
            PlayerPrefs.SetFloat(key + "_Score", fish.ratingScore);
        }
        PlayerPrefs.SetInt("TotalFishCaught", totalFishCaught);
        PlayerPrefs.SetInt("FishHistoryCount", catchHistory.Count);
        PlayerPrefs.Save();
    }

    private void LoadCatchHistory()
    {
        catchHistory.Clear();
        totalFishCaught = PlayerPrefs.GetInt("TotalFishCaught", 0);
        int historyCount = PlayerPrefs.GetInt("FishHistoryCount", 0);

        for (int i = 0; i < historyCount && i < maxHistoryItems; i++)
        {
            string key = "FishHistory_" + i;
            if (PlayerPrefs.HasKey(key + "_Name"))
            {
                FishRatingData fish = new FishRatingData
                {
                    fishName = PlayerPrefs.GetString(key + "_Name"),
                    weight = PlayerPrefs.GetFloat(key + "_Weight"),
                    rarity = PlayerPrefs.GetInt(key + "_Rarity"),
                    ratingScore = PlayerPrefs.GetFloat(key + "_Score")
                };
                catchHistory.Add(fish);
            }
        }
    }

    public FishRatingData GetBestCatch()
    {
        if (catchHistory.Count == 0) return null;
        return catchHistory.OrderByDescending(f => f.ratingScore).First();
    }

    public int GetTotalFish() => totalFishCaught;
    public float GetAverageRating() => catchHistory.Count > 0 ? catchHistory.Average(f => f.ratingScore) : 0f;

    public void CloseRatingPanel()
    {
        ratingPanel.SetActive(false);
    }
}
