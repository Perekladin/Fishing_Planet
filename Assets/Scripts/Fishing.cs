using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Fishing : MonoBehaviour
{
    [Header("Элементы интерфейса")]
    [SerializeField] private Button castButton;
    [SerializeField] private TextMeshProUGUI resultFishNameText;
    [SerializeField] private TextMeshProUGUI resultFishWeightText;
    [SerializeField] private Image resultFishImage;
    [SerializeField] private GameObject catchResultPanel;

    [Header("Настройки рыбалки")]
    [SerializeField] private float fishingTime = 5f;
    [SerializeField] private Animator rodAnimator;
    [SerializeField] private string pullAnimation = "PullRod";
    [SerializeField] private float resultShowTime = 5f;

    [Header("Настройки генератора рыб")]
    [SerializeField] private int maxFishCount = 20;
    [SerializeField] private bool generateFishOnStart = true;

    private List<FishData> fishDatabase = new List<FishData>();
    private bool isFishing = false;
    private Coroutine fishingCoroutine;
    private Coroutine resultHideCoroutine;

    private void Start()
    {
        castButton.onClick.AddListener(StartFishing);
        catchResultPanel.SetActive(false);

        if (generateFishOnStart)
        {
            GenerateFishDatabase();
        }
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
                fishSprite = fishSprites.Length > 0 ? fishSprites[rand.Next(Mathf.Min(fishSprites.Length, 5))] : null,
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

    private void StartFishing()
    {
        if (isFishing || fishDatabase.Count == 0) return;

        isFishing = true;
        castButton.interactable = false;
        Debug.Log("Заброс удочки! Ждите " + fishingTime + " секунд...");

        fishingCoroutine = StartCoroutine(FishingProcess());
    }

    private IEnumerator FishingProcess()
    {
        float timeLeft = fishingTime;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            Debug.Log("Осталось: " + timeLeft.ToString("F1") + "с");
            yield return null;
        }

        Debug.Log("Рыба клюнула! Вылавливаем...");
        yield return StartCoroutine(CatchFish());

        isFishing = false;
        castButton.interactable = true;
    }

    private IEnumerator CatchFish()
    {
        if (rodAnimator != null)
        {
            rodAnimator.SetTrigger(pullAnimation);
        }

        yield return new WaitForSeconds(1f);

        FishData caughtFish = GetRandomFish();
        ShowCatchResult(caughtFish);
    }

    private FishData GetRandomFish()
    {
        if (fishDatabase.Count == 0)
        {
            Debug.LogWarning("База рыб пуста! Сгенерируйте базу.");
            return null;
        }
        return fishDatabase[Random.Range(0, fishDatabase.Count)];
    }

    private void ShowCatchResult(FishData fish)
    {
        catchResultPanel.SetActive(true);

        resultFishNameText.text = fish.fishName;
        resultFishWeightText.text = "Вес: " + fish.weight.ToString("F1") + " кг | Редкость: " + fish.rarity + "/5";

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

    [System.Serializable]
    public class FishData
    {
        public string fishName;
        public float weight;
        public Sprite fishSprite;
        public int rarity;
    }
}
