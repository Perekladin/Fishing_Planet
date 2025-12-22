using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AchievementSystem : MonoBehaviour
{

    // ================== SINGLETON ==================
    public static AchievementSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ================== DATA ==================
    [System.Serializable]
    public class AchievementData
    {
        public string achievementId;
        public string achievementName;
        public string achievementDescription;
        public Sprite achievementIcon;
        public bool isUnlocked;
    }

    [Header("Список достижений")]
    [SerializeField] private List<AchievementData> achievements = new List<AchievementData>();

    // ================== PUBLIC API ==================
    public void UnlockAchievement(string achievementId)
    {
        var achievement = achievements.FirstOrDefault(a => a.achievementId == achievementId);
        if (achievement == null || achievement.isUnlocked)
            return;

        achievement.isUnlocked = true;
        SaveAchievements();

        Debug.Log("[AchievementSystem] Разблокировано: " + achievementId);

        // Обновляем UI, если есть подписанный Canvas
        RefreshAchievementUI();

        // Можно показать уведомление
        ShowNewAchievementNotification(achievement);
    }

    public bool IsAchievementUnlocked(string achievementId)
    {
        return achievements.FirstOrDefault(a => a.achievementId == achievementId)?.isUnlocked ?? false;
    }

    public List<AchievementData> GetAllAchievements()
    {
        return achievements;
    }

    // ================== SAVE / LOAD ==================
    public void SaveAchievements()
    {
        foreach (var achievement in achievements)
            PlayerPrefs.SetInt("Achievement_" + achievement.achievementId, achievement.isUnlocked ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void LoadAchievements()
    {
        foreach (var achievement in achievements)
            achievement.isUnlocked = PlayerPrefs.GetInt("Achievement_" + achievement.achievementId, 0) == 1;

        Debug.Log("[AchievementSystem] Достижения загружены");
    }

    // ================== UI ==================
    [Header("UI Canvas для уведомлений (опционально)")]
    [SerializeField] private GameObject achievementCanvas;
    [SerializeField] private Transform achievementContainer;
    [SerializeField] private GameObject achievementPrefab;

    // Подключение UI Canvas (например на сцене меню)
    public void RegisterUI(GameObject canvas, Transform container, GameObject prefab)
    {
        achievementCanvas = canvas;
        achievementContainer = container;
        achievementPrefab = prefab;

        RefreshAchievementUI();
    }

    // Обновление списка достижений на Canvas
    public void RefreshAchievementUI()
    {
        if (achievementContainer == null || achievementPrefab == null)
            return;

        // Удаляем старые элементы
        for (int i = achievementContainer.childCount - 1; i >= 0; i--)
            Destroy(achievementContainer.GetChild(i).gameObject);

        // Создаём новые элементы
        foreach (var achievement in achievements)
        {
            GameObject element = Instantiate(achievementPrefab, achievementContainer);
            AchievementUI ui = element.GetComponent<AchievementUI>();
            if (ui != null)
                ui.Setup(achievement, achievement.isUnlocked, false);
        }
    }

    // Показ уведомления о новом достижении
    private void ShowNewAchievementNotification(AchievementData achievement)
    {
        if (achievementCanvas == null || achievementPrefab == null)
            return;

        GameObject notification = Instantiate(achievementPrefab, achievementCanvas.transform);
        AchievementUI ui = notification.GetComponentInChildren<AchievementUI>();
        if (ui != null)
            ui.Setup(achievement, true, true);

        StartCoroutine(AnimateNotification(notification));
    }

    private IEnumerator AnimateNotification(GameObject notification)
    {
        CanvasGroup cg = notification.GetComponent<CanvasGroup>();
        if (cg == null) cg = notification.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        notification.transform.localPosition = new Vector3(0, 100, 0);

        float t = 0f;
        const float duration = 0.5f;

        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, t / duration);
            notification.transform.localPosition = Vector3.Lerp(new Vector3(0, 100, 0), Vector3.zero, t / duration);
            yield return null;
        }

        yield return new WaitForSeconds(3f);

        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1, 0, t / duration);
            yield return null;
        }

        Destroy(notification);
    }
}
