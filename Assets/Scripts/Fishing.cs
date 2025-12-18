using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Fishing : MonoBehaviour
{
    [Header("Элементы интерфейса")]
    [SerializeField] private Button castButton;
    [SerializeField] private Button resetPondButton;
    [SerializeField] private TextMeshProUGUI resultFishNameText;
    [SerializeField] private TextMeshProUGUI resultFishWeightText;
    [SerializeField] private Image resultFishImage;
    [SerializeField] private GameObject catchResultPanel;

    [Header("Настройки рыбалки")]
    [SerializeField] private float fishingTimeMin = 3f;
    [SerializeField] private float fishingTimeMax = 8f;
    [SerializeField] private Animator rodAnimator;
    [SerializeField] private string pullAnimation = "PullRod";
    [SerializeField] private float resultShowTime = 5f;
    [SerializeField] private float floatMissTime = 3f;

    [Header("Заброс поплавка")]
    [SerializeField] private GameObject floatPrefab;
    [SerializeField] private Transform rodTip;
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float throwUpForce = 8f;

    [Header("Системы")]
    [SerializeField] private PondManager pondManager;
    [SerializeField] private AchievementManager achievementManager;

    [Header("Настройки генератора рыб")]
    [SerializeField] private int maxFishCount = 20;
    [SerializeField] private bool generateFishOnStart = true;

    private List<FishData> fishDatabase = new List<FishData>();
    private bool isFishing = false;
    private GameObject currentFloat;
    private Coroutine fishingCoroutine;
    private Coroutine resultHideCoroutine;
    private int totalFishCaught = 0;

    private void Start()
    {
        castButton.onClick.AddListener(OnCastButton);
        resetPondButton.onClick.AddListener(OnResetPondButton);
        catchResultPanel.SetActive(false);

        totalFishCaught = PlayerPrefs.GetInt("TotalFishCaught", 0);
        Debug.Log("Всего поймано рыб: " + totalFishCaught);

        if (generateFishOnStart)
        {
            GenerateFishDatabase();
        }
    }

    private void Update()
    {
        // Кнопка заброса активна только когда пруд готов
        castButton.interactable = pondManager.IsPondReady && !isFishing;
    }

    private void OnCastButton()
    {
        if (!pondManager.IsPondReady || isFishing)
        {
            Debug.LogWarning("Сначала дождитесь появления пруда!");
            return;
        }

        //  Блокируем создание новых прудов
        pondManager.LockPond();
        isFishing = true;
        castButton.interactable = false;

        Debug.Log("Заброс поплавка!");
        ThrowFloat();
        StartCoroutine(CheckFloatHit());
    }

    private void OnResetPondButton()
    {
        pondManager.ResetPond();
        pondManager.UnlockPond();
    }

    private void ThrowFloat()
    {
        if (currentFloat != null) Destroy(currentFloat);

        currentFloat = Instantiate(floatPrefab, rodTip.position, rodTip.rotation);
        Rigidbody rb = currentFloat.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 directionToPond = (pondManager.PondPosition - rodTip.position).normalized;
            rb.AddForce(directionToPond * throwForce + Vector3.up * throwUpForce, ForceMode.Impulse);
        }
    }

    private IEnumerator CheckFloatHit()
    {
        yield return new WaitForSeconds(2f);

        if (!pondManager.HasPond || currentFloat == null)
        {
            CleanupFishing();
            yield break;
        }

        float distance = Vector3.Distance(currentFloat.transform.position, pondManager.PondPosition);
        if (distance < 1.5f)
        {
            Debug.Log("Поплавок в пруду! Ждем поклевки...");
            float randomWait = Random.Range(fishingTimeMin, fishingTimeMax);
            yield return new WaitForSeconds(randomWait);

            if (isFishing)
            {
                Debug.Log("Рыба клюнула!");
                StartCoroutine(CatchFish());
            }
        }
        else
        {
            Debug.Log("Промах!");
            yield return new WaitForSeconds(floatMissTime);
            CleanupFishing();
        }
    }

    private IEnumerator CatchFish()
    {
        if (rodAnimator != null)
        {
            rodAnimator.SetTrigger(pullAnimation);
        }

        yield return new WaitForSeconds(1f);

        FishData caughtFish = GetRandomFish();
        if (caughtFish != null)
        {
            OnFishCaught(caughtFish);
            ShowCatchResult(caughtFish);
        }

        CleanupFishing();
    }

    private void CleanupFishing()
    {
        if (currentFloat != null) Destroy(currentFloat);
        isFishing = false;
        castButton.interactable = pondManager.IsPondReady;
    }

    // Остальные методы без изменений (OnFishCaught, ShowCatchResult, GenerateFishDatabase...)
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
        if (fishDatabase.Count == 0) return null;
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

        if (resultHideCoroutine != null) StopCoroutine(resultHideCoroutine);
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
        string[] fishNames = { "Окунь", "Карп", "Сом", "Щука", "Судак", "Лещ", "Плотва", "Налим", "Форель", "Угорь", "Красноперка", "Язь", "Голавль", "Хариус", "Сазан" };
        Sprite[] fishSprites = { Resources.Load<Sprite>("Fish/Perch"), Resources.Load<Sprite>("Fish/Carp"), Resources.Load<Sprite>("Fish/Catfish"), Resources.Load<Sprite>("Fish/Pike"), Resources.Load<Sprite>("Fish/Zander") };

        System.Random rand = new System.Random();
        for (int i = 0; i < maxFishCount; i++)
        {
            fishDatabase.Add(new FishData
            {
                fishName = fishNames[rand.Next(fishNames.Length)],
                weight = GenerateRandomWeight(rand),
                fishSprite = fishSprites.Length > 0 ? fishSprites[rand.Next(fishSprites.Length)] : null,
                rarity = rand.Next(1, 6)
            });
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

    [System.Serializable]
    public class FishData
    {
        public string fishName;
        public float weight;
        public Sprite fishSprite;
        public int rarity;
    }
}
