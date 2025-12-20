using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementSystem : MonoBehaviour
{
    [System.Serializable]
    public class AchievementData
    {
        public string achievementId;
        public string achievementName;
        public string achievementDescription;
        public Sprite achievementIcon;
        public bool isUnlocked;
    }

    [Header("UI Элементы")]
    [SerializeField] private GameObject achievementCanvas;
    [SerializeField] private Transform achievementContainer;  // Grid Layout Group!
    [SerializeField] private GameObject achievementPrefab;

    [Header("Список достижений")]
    [SerializeField] private List<AchievementData> achievements = new List<AchievementData>();

    [Header("Настройки")]
    [SerializeField] private bool loadAchievementsOnStart = true;

    private void Start()
    {
        if (loadAchievementsOnStart)
        {
            // Загружаем достижения из PlayerPrefs при старте сцены
            LoadAchievements();
            RefreshAchievementUI();
        }
    }

    public void UnlockAchievement(string achievementId)
    {
        AchievementData achievement = achievements.FirstOrDefault(a => a.achievementId == achievementId);
        if (achievement != null)
        {
            if (!achievement.isUnlocked)
            {
                achievement.isUnlocked = true;
                SaveAchievements(); // сохраняем прогресс
                ShowNewAchievement(achievement);
                RefreshAchievementUI();
            }
        }
        else
        {
            Debug.LogError("Достижение не найдено: " + achievementId);
        }
    }

    private void ShowNewAchievement(AchievementData achievement)
    {
        GameObject notification = Instantiate(achievementPrefab, achievementCanvas.transform);
        notification.transform.SetAsLastSibling();

        AchievementUI[] uiElements = notification.GetComponentsInChildren<AchievementUI>();
        if (uiElements.Length > 0)
        {
            uiElements[0].Setup(achievement, true, true);
        }

        StartCoroutine(AnimateNotification(notification));
    }

    private IEnumerator AnimateNotification(GameObject notification)
    {
        CanvasGroup cg = notification.GetComponent<CanvasGroup>();
        if (cg == null) cg = notification.AddComponent<CanvasGroup>();

        cg.alpha = 0;
        notification.transform.localPosition = new Vector3(0, 100, 0);

        float duration = 0.5f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            notification.transform.localPosition = Vector3.Lerp(new Vector3(0, 100, 0), Vector3.zero, elapsed / duration);
            yield return null;
        }

        yield return new WaitForSeconds(3f);

        elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1, 0, elapsed / duration);
            notification.transform.localPosition = Vector3.Lerp(Vector3.zero, new Vector3(0, -100, 0), elapsed / duration);
            yield return null;
        }

        Destroy(notification);
    }

    public void ToggleAchievementCanvas()
    {
        achievementCanvas.SetActive(!achievementCanvas.activeSelf);
    }

    private void RefreshAchievementUI()
    {
        // удаляем старые элементы UI
        for (int i = achievementContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = achievementContainer.GetChild(i);
            if (child.GetComponent<AchievementUI>() != null)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // создаем новые элементы UI
        for (int i = 0; i < achievements.Count; i++)
        {
            var achievement = achievements[i];
            CreateAchievementUIElement(achievement, achievement.isUnlocked, i);
        }
    }

    private void CreateAchievementUIElement(AchievementData achievement, bool isUnlocked, int index)
    {
        GameObject achievementElement = Instantiate(achievementPrefab, achievementContainer);
        achievementElement.name = "Achievement_" + achievement.achievementId;

        AchievementUI[] uiElements = achievementElement.GetComponentsInChildren<AchievementUI>();
        if (uiElements.Length > 0)
        {
            uiElements[0].Setup(achievement, isUnlocked, false);
        }
    }

    private void SaveAchievements()
    {
        foreach (var achievement in achievements)
        {
            PlayerPrefs.SetInt("Achievement_" + achievement.achievementId, achievement.isUnlocked ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    private void LoadAchievements()
    {
        foreach (var achievement in achievements)
        {
            achievement.isUnlocked = PlayerPrefs.GetInt("Achievement_" + achievement.achievementId, 0) == 1;
        }
    }

    public bool IsAchievementUnlocked(string achievementId)
    {
        return achievements.FirstOrDefault(a => a.achievementId == achievementId)?.isUnlocked ?? false;
    }

    public int GetUnlockedCount()
    {
        return achievements.Count(a => a.isUnlocked);
    }
}
