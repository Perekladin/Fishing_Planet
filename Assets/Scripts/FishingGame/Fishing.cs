using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

public class Fishing : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARPlaneManager arPlaneManager;
    [SerializeField] private GameObject pondPrefab;

    [Header("Кнопки")]
    [SerializeField] private Button castButton;
    [SerializeField] private Button resetPondButton;

    [Header("Заброс")]
    [SerializeField] private GameObject floatPrefab;
    [SerializeField] private Transform rodTip;

    [Header("Мини-игра")]
    [SerializeField] private GameObject miniGamePanel;
    [SerializeField] private Slider controlSlider;
    [SerializeField] private Image successZoneImage;
    [SerializeField] private float baseHoldTime = 3f;

    [Header("Результат")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI fishNameText;
    [SerializeField] private TextMeshProUGUI fishWeightText;
    [SerializeField] private TextMeshProUGUI fishRarityText;

    [Header("Система рейтинга")]
    [SerializeField] private FishRatingSystem ratingSystem;

    private GameObject currentPond;
    private GameObject currentFloat;

    private bool pondPlaced;
    private bool isFishing;
    private bool miniGameActive;
    private bool floatAlreadyInPond;

    private float requiredHoldTime;
    private float currentHoldTime;

    private FishData currentFish;
    private int totalFishCaught;

    private bool waitingForPlane;
    private bool pondEverPlaced;

    private float zoneCenterNormalized = 0.5f;
    private float zoneWidthNormalized = 0.2f;

    // ================== START ==================
    private void Start()
    {
        castButton.onClick.AddListener(OnCastButton);
        resetPondButton.onClick.AddListener(ResetPond);

        castButton.interactable = false;
        miniGamePanel.SetActive(false);
        resultPanel.SetActive(false);

        waitingForPlane = true;
        pondEverPlaced = false;

        totalFishCaught = PlayerPrefs.GetInt("TotalFishCaught", 0);
        Debug.Log("[Fishing] Ожидание первой горизонтальной плоскости");
    }

    private void Update()
    {
        if (!pondPlaced && waitingForPlane)
            TryAutoPlacePond();

        if (miniGameActive)
            UpdateMiniGame();
    }

    // ================== ПРУД ==================
    private void TryAutoPlacePond()
    {
        foreach (ARPlane plane in arPlaneManager.trackables)
        {
            if (plane == null) continue;
            if (plane.alignment != PlaneAlignment.HorizontalUp) continue;

            PlacePond(plane);
            break;
        }
    }

    private void PlacePond(ARPlane plane)
    {
        if (currentPond != null) return;

        currentPond = Instantiate(pondPrefab, plane.center, Quaternion.identity);

        pondPlaced = true;
        waitingForPlane = false;
        pondEverPlaced = true;

        castButton.interactable = true;

        foreach (ARPlane p in arPlaneManager.trackables)
            if (p != null) p.gameObject.SetActive(false);

        Debug.Log("[Fishing] Пруд создан автоматически");
    }

    public void ResetPond()
    {
        if (currentPond != null)
            Destroy(currentPond);

        ResetFishing();

        pondPlaced = false;
        castButton.interactable = false;

        // Включаем PlaneManager заново
        arPlaneManager.enabled = false;
        arPlaneManager.enabled = true;

        foreach (ARPlane plane in arPlaneManager.trackables)
            if (plane != null)
                plane.gameObject.SetActive(true);

        waitingForPlane = true;

        Debug.Log("[Fishing] Пруд сброшен, ожидаем новую плоскость");
    }

    // ================== ЗАБРОС ==================
    private void OnCastButton()
    {
        if (!pondPlaced || isFishing) return;

        isFishing = true;
        floatAlreadyInPond = false;
        castButton.interactable = false;

        currentFloat = Instantiate(floatPrefab, rodTip.position, Quaternion.identity);
        Rigidbody rb = currentFloat.GetComponent<Rigidbody>();
        rb.AddForce((currentPond.transform.position - rodTip.position).normalized * 6f + Vector3.up * 3f, ForceMode.Impulse);

        Debug.Log("[Fishing] Поплавок заброшен");

        StartCoroutine(FailSafeTimeout());
    }

    public void OnFloatEnteredPond(GameObject floatObject)
    {
        if (!isFishing || floatAlreadyInPond) return;
        if (!floatObject.CompareTag("Float")) return;

        floatAlreadyInPond = true;

        Debug.Log("[Fishing] Поплавок попал в пруд");

        StartCoroutine(DelayedMiniGame());
    }

    private IEnumerator DelayedMiniGame()
    {
        yield return new WaitForSeconds(Random.Range(1.5f, 3.5f));
        if (isFishing) StartMiniGame();
    }

    // ================== МИНИ-ИГРА ==================
    private void StartMiniGame()
    {
        currentFish = GenerateFish();
        requiredHoldTime = baseHoldTime + currentFish.weight * 0.3f + currentFish.rarity * 0.5f;
        currentHoldTime = 0f;
        controlSlider.value = 0.5f;

        miniGameActive = true;
        miniGamePanel.SetActive(true);

        UpdateZoneVisual();

        Debug.Log($"[Fishing] Мини-игра начата: {currentFish.fishName}, вес {currentFish.weight:F1}, редкость {currentFish.rarity}, время удержания {requiredHoldTime:F1}");
    }

    private void UpdateMiniGame()
    {
        if (!miniGameActive) return;

        bool isTouching = false;

        // Проверяем касание через Input System
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            isTouching = true;
            // Двигаем ползунок только при касании
            controlSlider.value = Mathf.Clamp01(controlSlider.value + Time.deltaTime * 0.5f);
        }

        // Двигаем зону
        UpdateZoneVisual();

        // Проверка попадания ползунка в зону
        bool inZone = Mathf.Abs(controlSlider.value - zoneCenterNormalized) <= zoneWidthNormalized / 2f;

        if (inZone && isTouching)
            currentHoldTime += Time.deltaTime;
        // Убираем уменьшение таймера при удержании пальца вне зоны
        // currentHoldTime -= Time.deltaTime не вызываем

        // Победа
        if (currentHoldTime >= requiredHoldTime)
            FinishFishing(true);

        // Проигрыш можно делать только после окончания времени мини-игры
        // или после отпускания пальца без попадания в зону
    }


    private void UpdateZoneVisual()
    {
        if (successZoneImage == null || controlSlider == null) return;

        RectTransform sliderRect = controlSlider.GetComponent<RectTransform>();
        RectTransform zoneRect = successZoneImage.rectTransform;

        zoneCenterNormalized = Mathf.PingPong(Time.time * 0.2f, 1f - zoneWidthNormalized) + zoneWidthNormalized / 2f;

        float sliderWidth = sliderRect.rect.width;
        float zoneX = (zoneCenterNormalized - 0.5f) * sliderWidth;

        zoneRect.sizeDelta = new Vector2(zoneWidthNormalized * sliderWidth, zoneRect.sizeDelta.y);
        zoneRect.anchoredPosition = new Vector2(zoneX, 0);
    }

    // ================== ФИНАЛ ==================
    private void FinishFishing(bool success)
    {
        miniGameActive = false;
        miniGamePanel.SetActive(false);

        if (success)
        {
            Debug.Log($"[Fishing] Рыба поймана: {currentFish.fishName}, {currentFish.weight:F1} кг, редкость {currentFish.rarity}");

            ShowResult(currentFish);

            totalFishCaught++;
            PlayerPrefs.SetInt("TotalFishCaught", totalFishCaught);
            PlayerPrefs.Save();

            // --- Достижения ---
            if (AchievementSystem.Instance != null)
            {
                AchievementSystem.Instance.UnlockAchievement("first_fish");
                if (totalFishCaught >= 10) AchievementSystem.Instance.UnlockAchievement("fisherman");
                if (currentFish.weight > 3f) AchievementSystem.Instance.UnlockAchievement("big_fish");
            }

            // --- Рейтинг ---
            if (ratingSystem != null)
            {
                var ratingFish = new FishRatingData
                {
                    fishName = currentFish.fishName,
                    weight = currentFish.weight,
                    rarity = currentFish.rarity,
                    fishSprite = null,
                    description = currentFish.description
                };
                ratingSystem.AddFish(ratingFish);
            }
        }
        else
        {
            Debug.Log("[Fishing] Рыба сорвалась");
        }

        ResetFishing();
    }

    private IEnumerator FailSafeTimeout()
    {
        yield return new WaitForSeconds(3f);
        if (isFishing && !floatAlreadyInPond)
        {
            Debug.Log("[Fishing] Поплавок не попал в пруд, сброс");
            ResetFishing();
        }
    }

    private void ResetFishing()
    {
        if (currentFloat != null) Destroy(currentFloat);

        isFishing = false;
        miniGameActive = false;
        floatAlreadyInPond = false;

        castButton.interactable = pondPlaced;
    }

    private void ShowResult(FishData fish)
    {
        resultPanel.SetActive(true);
        fishNameText.text = fish.fishName;
        fishWeightText.text = $"Вес: {fish.weight:F1} кг";
        fishRarityText.text = $"Редкость: {fish.rarity}";
    }

    // ================== РЫБЫ ==================
    private FishData GenerateFish()
    {
        string[] names = { "Окунь", "Карп", "Щука", "Сом", "Форель" };
        string fishName = names[Random.Range(0, names.Length)];
        string description = GetFishDescription(fishName);

        return new FishData
        {
            fishName = fishName,
            weight = Random.Range(0.5f, 8f),
            rarity = Random.Range(1, 6),
            description = description
        };
    }

    private string GetFishDescription(string name)
    {
        switch (name)
        {
            case "Окунь": return "Окунь — мелкая хищная рыба, водится в реках и озёрах с пресной водой.";
            case "Карп": return "Карп — пресноводная рыба, предпочитает тихие озера и пруды с растительностью.";
            case "Щука": return "Щука — хищная рыба, обитает в реках и заросших озёрах, любит прятаться среди растений.";
            case "Сом": return "Сом — крупная донная рыба, встречается в глубоких реках и озёрах с песчаным или илистым дном.";
            case "Форель": return "Форель — рыба холодных рек и горных озёр, предпочитает чистую воду и быстрые потоки.";
            default: return "Описание отсутствует.";
        }
    }

    [System.Serializable]
    public class FishData
    {
        public string fishName;
        public float weight;
        public int rarity;
        public string description;
    }
}
