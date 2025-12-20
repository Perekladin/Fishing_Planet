using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class Fishing : MonoBehaviour
{
    private enum FishingState
    {
        Idle,
        WaitingForFloatHit,
        MiniGame
    }

    [Header("UI")]
    [SerializeField] private Button castButton;
    [SerializeField] private GameObject catchResultPanel;
    [SerializeField] private TextMeshProUGUI resultFishNameText;
    [SerializeField] private TextMeshProUGUI resultFishWeightText;
    [SerializeField] private TextMeshProUGUI resultFishRarityText; // новый текст для редкости
    [SerializeField] private Image resultFishImage;

    [Header("MiniGame UI")]
    [SerializeField] private GameObject miniGamePanel;
    [SerializeField] private Slider miniGameSlider;
    [SerializeField] private RectTransform targetZone;

    [Header("Fishing Settings")]
    [SerializeField] private float fishingTimeMin = 3f;
    [SerializeField] private float fishingTimeMax = 8f;
    [SerializeField] private float floatHitTimeout = 3f;
    [SerializeField] private float miniGameBaseTime = 3f;
    [SerializeField] private float miniGameWeightFactor = 0.2f;
    [SerializeField] private float miniGameRarityFactor = 0.3f;
    [SerializeField] private float resultShowTime = 5f;
    [SerializeField] private Animator rodAnimator;
    [SerializeField] private string pullAnimation = "PullRod";

    [Header("Float")]
    [SerializeField] private GameObject floatPrefab;
    [SerializeField] private Transform rodTip;
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float throwUpForce = 8f;

    [Header("Pond")]
    [SerializeField] private GameObject pondPrefab;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private float pondYOffset = 0.01f;

    [Header("Fish Generator")]
    [SerializeField] private int maxFishCount = 20;

    [Header("Catch Settings")]
    [SerializeField][Range(0f, 1f)] private float fishEscapeChance = 0.2f;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip hitWaterClip;
    [SerializeField] private AudioClip castClip;
    [SerializeField] private AudioClip fishCaughtClip;
    [SerializeField] private AudioClip fishEscapedClip;
    [SerializeField] private AudioClip backgroundMusicClip;

    private AudioSource audioSource;

    private GameObject currentPond;
    private GameObject currentFloat;
    private Vector3 pondPosition;

    private bool pondSpawned = false;
    private FishingState currentState = FishingState.Idle;

    private Coroutine floatTimeoutCoroutine;
    private Coroutine miniGameCoroutine;
    private Coroutine resultHideCoroutine;

    private List<FishData> fishDatabase = new();

    private int totalFishCaught = 0;
    private FishData currentFish;

    private bool firstFishUnlocked = false;
    private bool tenFishUnlocked = false;
    private bool bigFishUnlocked = false;

    // -------------------- START --------------------

    private void Start()
    {
        castButton.onClick.AddListener(OnCastButton);
        castButton.interactable = false;
        catchResultPanel.SetActive(false);
        miniGamePanel.SetActive(false);

        totalFishCaught = PlayerPrefs.GetInt("TotalFishCaught", 0);
        GenerateFishDatabase();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;

        if (backgroundMusicClip != null)
        {
            audioSource.clip = backgroundMusicClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        Debug.Log("Игра запущена. Всего поймано ранее: " + totalFishCaught);
        PrintAchievementsStatus();
    }

    private void Update()
    {
        if (!pondSpawned)
        {
            TryPlacePond();
        }
    }

    // -------------------- POND --------------------

    private void TryPlacePond()
    {
        if (pondSpawned) return;

        Vector2 center = new(Screen.width / 2f, Screen.height / 2f);
        List<ARRaycastHit> hits = new();

        if (arRaycastManager.Raycast(center, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            currentPond = Instantiate(pondPrefab, hitPose.position + Vector3.up * pondYOffset, hitPose.rotation);
            pondPosition = hitPose.position;
            pondSpawned = true;
            castButton.interactable = true;

            PondTrigger trigger = currentPond.GetComponentInChildren<PondTrigger>();
            if (trigger != null)
                trigger.fishing = this;

            Debug.Log("Пруд создан и зафиксирован");
        }
    }

    // -------------------- CAST --------------------

    private void OnCastButton()
    {
        if (currentState != FishingState.Idle) return;

        currentState = FishingState.WaitingForFloatHit;
        castButton.interactable = false;

        PlaySound(castClip);
        ThrowFloat();

        floatTimeoutCoroutine = StartCoroutine(FloatHitTimeout());
    }

    private void ThrowFloat()
    {
        if (currentFloat != null) Destroy(currentFloat);

        currentFloat = Instantiate(floatPrefab, rodTip.position, rodTip.rotation);

        Rigidbody rb = currentFloat.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = (pondPosition - rodTip.position).normalized;
            rb.AddForce(dir * throwForce + Vector3.up * throwUpForce, ForceMode.Impulse);
        }

        Debug.Log("Поплавок заброшен");
    }

    // -------------------- TRIGGER --------------------

    public void OnFloatEnteredPond(GameObject floatObject)
    {
        if (currentState != FishingState.WaitingForFloatHit) return;
        if (floatObject != currentFloat) return;

        if (floatTimeoutCoroutine != null) StopCoroutine(floatTimeoutCoroutine);

        PlaySound(hitWaterClip);

        currentFish = fishDatabase[Random.Range(0, fishDatabase.Count)];
        currentState = FishingState.MiniGame;

        float delay = Random.Range(0.5f, 2f);
        miniGameCoroutine = StartCoroutine(MiniGameWithDelay(delay));
    }

    private IEnumerator FloatHitTimeout()
    {
        yield return new WaitForSeconds(floatHitTimeout);
        if (currentState == FishingState.WaitingForFloatHit)
        {
            Debug.Log("Поплавок не попал в пруд. Попытка отменена.");
            CleanupFishing();
        }
    }

    // -------------------- MINI-GAME --------------------

    private IEnumerator MiniGameWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return MiniGameCoroutine();
    }

    private IEnumerator MiniGameCoroutine()
    {
        miniGamePanel.SetActive(true);

        // длительность мини-игры зависит от веса и редкости
        float gameDuration = miniGameBaseTime + currentFish.weight * miniGameWeightFactor + currentFish.rarity * miniGameRarityFactor;
        float elapsed = 0f;
        bool success = false;

        while (elapsed < gameDuration)
        {
            float center = Mathf.PingPong(Time.time * 0.2f, 0.7f - 0.3f) + 0.3f;
            targetZone.anchorMin = new Vector2(center - 0.05f, targetZone.anchorMin.y);
            targetZone.anchorMax = new Vector2(center + 0.05f, targetZone.anchorMax.y);

            float value = miniGameSlider.value;
            success = value >= targetZone.anchorMin.x && value <= targetZone.anchorMax.x;

            elapsed += Time.deltaTime;
            yield return null;
        }

        miniGamePanel.SetActive(false);

        if (success)
        {
            Debug.Log("Мини-игра пройдена. Рыба поймана.");
            currentState = FishingState.Idle;
            CatchFishAfterMiniGame();
        }
        else
        {
            Debug.Log("Рыба сорвалась. Не удалось удержать ползунок.");
            PlaySound(fishEscapedClip);
            CleanupFishing();
        }
    }

    // -------------------- ПОЙМАТЬ РЫБУ --------------------

    private void CatchFishAfterMiniGame()
    {
        FishData fish = currentFish;
        totalFishCaught++;

        PlayerPrefs.SetInt("TotalFishCaught", totalFishCaught);
        PlayerPrefs.Save();

        Debug.Log("Рыба поймана: " + fish.fishName + ", вес " + fish.weight.ToString("F1") + " кг, редкость " + fish.rarity);
        Debug.Log("Всего поймано рыб: " + totalFishCaught);

        PlaySound(fishCaughtClip);

        CheckAchievements(fish);
        PrintAchievementsStatus();

        ShowCatchResult(fish);
        CleanupFishing();
    }

    // -------------------- CLEANUP --------------------

    private void CleanupFishing()
    {
        if (floatTimeoutCoroutine != null)
        {
            StopCoroutine(floatTimeoutCoroutine);
            floatTimeoutCoroutine = null;
        }

        if (miniGameCoroutine != null)
        {
            StopCoroutine(miniGameCoroutine);
            miniGameCoroutine = null;
        }

        if (currentFloat != null) Destroy(currentFloat);

        currentState = FishingState.Idle;
        castButton.interactable = true;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    private void CheckAchievements(FishData fish)
    {
        if (!firstFishUnlocked && totalFishCaught >= 1)
        {
            firstFishUnlocked = true;
            Debug.Log("Достижение открыто: Первая рыба");
        }

        if (!tenFishUnlocked && totalFishCaught >= 10)
        {
            tenFishUnlocked = true;
            Debug.Log("Достижение открыто: 10 рыб");
        }

        if (!bigFishUnlocked && fish.weight >= 8f)
        {
            bigFishUnlocked = true;
            Debug.Log("Достижение открыто: Крупная рыба");
        }
    }

    private void PrintAchievementsStatus()
    {
        Debug.Log("Статус достижений:");
        Debug.Log("- Первая рыба: " + (firstFishUnlocked ? "Открыто" : "Не выполнено"));
        Debug.Log("- 10 рыб: " + (tenFishUnlocked ? "Открыто" : "Не выполнено"));
        Debug.Log("- Крупная рыба (?8 кг): " + (bigFishUnlocked ? "Открыто" : "Не выполнено"));
    }

    private void ShowCatchResult(FishData fish)
    {
        catchResultPanel.SetActive(true);
        resultFishNameText.text = fish.fishName;
        resultFishWeightText.text = "Вес: " + fish.weight.ToString("F1") + " кг";
        resultFishRarityText.text = "Редкость: " + fish.rarity;

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

        resultHideCoroutine = StartCoroutine(HideResult());
    }

    private IEnumerator HideResult()
    {
        yield return new WaitForSeconds(resultShowTime);
        catchResultPanel.SetActive(false);
    }

    private void GenerateFishDatabase()
    {
        string[] names = { "Окунь", "Карп", "Щука", "Сом", "Форель" };

        for (int i = 0; i < maxFishCount; i++)
        {
            fishDatabase.Add(new FishData
            {
                fishName = names[Random.Range(0, names.Length)],
                weight = Random.Range(0.5f, 10f),
                rarity = Random.Range(1, 5) // добавляем редкость
            });
        }
    }

    [System.Serializable]
    public class FishData
    {
        public string fishName;
        public float weight;
        public int rarity; // добавлено
        public Sprite fishSprite;
    }
}
