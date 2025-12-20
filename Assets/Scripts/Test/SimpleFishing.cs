using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SimpleFishing : MonoBehaviour
{
    [Header("Элементы интерфейса")]
    [SerializeField] private Button fishButton;
    [SerializeField] private TextMeshProUGUI resultFishNameText;
    [SerializeField] private TextMeshProUGUI resultFishWeightText;
    [SerializeField] private Image resultFishImage;
    [SerializeField] private GameObject catchResultPanel;

    [Header("Настройки рыбалки")]
    [SerializeField] private float fishingTimeMin = 3f;
    [SerializeField] private float fishingTimeMax = 8f;
    [SerializeField] private float resultShowTime = 5f;

    [Header("Настройки генератора рыб")]
    [SerializeField] private int maxFishCount = 20;
    [SerializeField] private bool generateFishOnStart = true;

    [Header("Достижения и рейтинг")]
    [SerializeField] private AchievementManager achievementManager;

    private List<FishData> fishDatabase = new List<FishData>();
    private bool isFishing = false;
    private Coroutine fishingCoroutine;
    private Coroutine resultHideCoroutine;
    private int totalFishCaught = 0;

    //  ИСПРАВЛЕНО: Совместимый тип FishData для FishRating
    [System.Serializable]
    public class FishData
    {
        public string fishName;
        public float weight;
        public Sprite fishSprite;
        public int rarity;
    }

    private void Start()
    {
        fishButton.onClick.AddListener(StartFishing);
        catchResultPanel.SetActive(false);

        totalFishCaught = PlayerPrefs.GetInt("TotalFishCaught", 0);
        Debug.Log("Всего поймано рыб: " + totalFishCaught);

        if (generateFishOnStart)
        {
            GenerateFishDatabase();
        }
    }

    private void StartFishing()
    {
        if (isFishing) return;

        isFishing = true;
        fishButton.interactable = false;
        fishButton.GetComponentInChildren<TextMeshProUGUI>().text = "Ждем поклевки...";
        Debug.Log("Заброс удочки! Ждем поклевки...");

        fishingCoroutine = StartCoroutine(FishingProcess());
    }

    private IEnumerator FishingProcess()
    {
        float randomWait = Random.Range(fishingTimeMin, fishingTimeMax);
        yield return new WaitForSeconds(randomWait);

        Debug.Log("Рыба клюнула!");
        yield return StartCoroutine(CatchFish());

        isFishing = false;
        fishButton.interactable = true;
        fishButton.GetComponentInChildren<TextMeshProUGUI>().text = "Рыбачить";
    }

    private IEnumerator CatchFish()
    {
        yield return new WaitForSeconds(1f);

        FishData caughtFish = GetRandomFish();
        if (caughtFish != null)
        {
            OnFishCaught(caughtFish);

            FishRating fishRating = FindObjectOfType<FishRating>();
            if (fishRating != null)
            {
                fishRating.AddFishToRating(new Fishing.FishData
                {
                    fishName = caughtFish.fishName,
                    weight = caughtFish.weight,
                    fishSprite = caughtFish.fishSprite,
                    rarity = caughtFish.rarity
                });
            }

            ShowCatchResult(caughtFish);
        }
    }

    private void OnFishCaught(FishData fish)
    {
        totalFishCaught++;
        PlayerPrefs.SetInt("TotalFishCaught", totalFishCaught);
        PlayerPrefs.Save();

        Debug.Log("Поймано рыб: " + totalFishCaught + " | " + fish.fishName + " (" + fish.weight.ToString("F1") + " кг)");

        if (achievementManager != null)
        {
            if (totalFishCaught == 1) achievementManager.UnlockAchievement("first_fish");
            if (fish.weight > 5f) achievementManager.UnlockAchievement("big_fish");
            if (totalFishCaught >= 10) achievementManager.UnlockAchievement("fisherman");
            if (totalFishCaught >= 50) achievementManager.UnlockAchievement("master_fisher");
            if (fish.rarity == 5) achievementManager.UnlockAchievement("rare_fish");
        }
    }

    private FishData GetRandomFish()
    {
        if (fishDatabase.Count == 0)
        {
            Debug.LogWarning("База рыб пуста!");
            return null;
        }
        return fishDatabase[Random.Range(0, fishDatabase.Count)];
    }

    private void ShowCatchResult(FishData fish)
    {
        catchResultPanel.SetActive(true);

        resultFishNameText.text = fish.fishName;
        resultFishWeightText.text = "Вес: " + fish.weight.ToString("F1") + " кг | Редкость: " + fish.rarity + "/5 | Всего: " + totalFishCaught;

        if (fish.fishSprite != null)
        {
            resultFishImage.sprite = fish.fishSprite;
            resultFishImage.gameObject.SetActive(true);
        }
        else
        {
            resultFishImage.gameObject.SetActive(false);
        }

        if (resultHideCoroutine != null)
            StopCoroutine(resultHideCoroutine);
        resultHideCoroutine = StartCoroutine(HideResultAfterDelay());
    }

    private IEnumerator HideResultAfterDelay()
    {
        yield return new WaitForSeconds(resultShowTime);
        catchResultPanel.SetActive(false);
    }

    private void GenerateFishDatabase()
    {
        fishDatabase.Clear();

        string[] fishNames = {
            "Окунь", "Карп", "Сом", "Щука", "Судак",
            "Лещ", "Плотва", "Налим", "Форель", "Угорь",
            "Красноперка", "Язь", "Голавль", "Хариус", "Сазан"
        };

        Sprite[] fishSprites = {
            Resources.Load<Sprite>("Fish/Perch"),
            Resources.Load<Sprite>("Fish/Carp"),
            Resources.Load<Sprite>("Fish/Catfish"),
            Resources.Load<Sprite>("Fish/Pike"),
            Resources.Load<Sprite>("Fish/Zander")
        };

        System.Random rand = new System.Random();

        for (int i = 0; i < maxFishCount; i++)
        {
            FishData fish = new FishData
            {
                fishName = fishNames[rand.Next(fishNames.Length)],
                weight = GenerateRandomWeight(rand),
                fishSprite = fishSprites.Length > 0 ? fishSprites[rand.Next(fishSprites.Length)] : null,
                rarity = rand.Next(1, 6)
            };
            fishDatabase.Add(fish);
        }

        Debug.Log("Сгенерировано " + fishDatabase.Count + " рыб в базу!");
    }

    private float GenerateRandomWeight(System.Random rand)
    {
        float[] weightRanges = { 0.1f, 0.5f, 1.5f, 3.0f, 8.0f, 15.0f };
        int rangeIndex = rand.Next(weightRanges.Length);

        float minWeight = rangeIndex == 0 ? 0.05f : weightRanges[rangeIndex - 1];
        float maxWeight = weightRanges[rangeIndex];

        double value = rand.NextDouble() * (maxWeight - minWeight) + minWeight;
        return Mathf.Round((float)(value * 100f)) / 100f;
    }
}
