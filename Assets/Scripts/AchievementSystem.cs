using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System.Collections;

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
    [SerializeField] private Transform achievementContainer;
    [SerializeField] private GameObject achievementPrefab;

    [Header("Список достижений")]
    [SerializeField] private List<AchievementData> achievements = new List<AchievementData>();

    [Header("Настройки")]
    [SerializeField] private bool loadAchievementsOnStart = true;

    private void Start()
    {
        if (loadAchievementsOnStart)
        {
            LoadAchievements();
            RefreshAchievementUI();
        }
    }

    //  ИСПРАВЛЕНО: UnlockAchievement
    public void UnlockAchievement(string achievementId)
    {
        Debug.Log($" Ищем достижение: {achievementId}");

        AchievementData achievement = achievements.FirstOrDefault(a => a.achievementId == achievementId);
        if (achievement != null)
        {
            Debug.Log($" Найдено: {achievement.achievementName}, было: {achievement.isUnlocked}");

            if (!achievement.isUnlocked)
            {
                achievement.isUnlocked = true;
                Debug.Log($" РАЗБЛОКИРОВАНО: {achievement.achievementName}");

                SaveAchievements();
                ShowNewAchievement(achievement);
                RefreshAchievementUI();
            }
            else
            {
                Debug.Log($" Уже разблокировано: {achievement.achievementName}");
            }
        }
        else
        {
            Debug.LogError($" Достижение НЕ НАЙДЕНО: {achievementId}");
            Debug.LogError("Доступные ID: " + string.Join(", ", achievements.Select(a => a.achievementId)));
        }
    }

    private void ShowNewAchievement(AchievementData achievement)
    {
        GameObject notification = Instantiate(achievementPrefab, achievementContainer);
        Image[] images = notification.GetComponentsInChildren<Image>();
        TextMeshProUGUI[] texts = notification.GetComponentsInChildren<TextMeshProUGUI>();

        if (images.Length > 0) images[0].sprite = achievement.achievementIcon;
        if (texts.Length > 0) texts[0].text = " " + achievement.achievementName;
        if (texts.Length > 1) texts[1].text = achievement.achievementDescription;

        StartCoroutine(FadeOutNotification(notification));
    }

    private IEnumerator FadeOutNotification(GameObject notification)
    {
        yield return new WaitForSeconds(3f);
        Destroy(notification);
    }

    public void ToggleAchievementCanvas()
    {
        achievementCanvas.SetActive(!achievementCanvas.activeSelf);
    }

    //  ИСПРАВЛЕНО: RefreshAchievementUI
    private void RefreshAchievementUI()
    {
        Debug.Log(" Обновление UI достижений...");

        //  ПРАВИЛЬНО: Удаляем ВСЕ дочерние элементы контейнера
        for (int i = achievementContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(achievementContainer.GetChild(i).gameObject);
        }

        // Создаем элементы для ВСЕХ достижений
        foreach (var achievement in achievements)
        {
            CreateAchievementUIElement(achievement, achievement.isUnlocked);
        }

        Debug.Log($" UI обновлено: {GetUnlockedCount()}/{achievements.Count} разблокировано");
    }

    private void CreateAchievementUIElement(AchievementData achievement, bool isUnlocked)
    {
        GameObject achievementElement = Instantiate(achievementPrefab, achievementContainer);
        Image[] images = achievementElement.GetComponentsInChildren<Image>();
        TextMeshProUGUI[] texts = achievementElement.GetComponentsInChildren<TextMeshProUGUI>();

        Image icon = images.Length > 0 ? images[0] : null;
        TextMeshProUGUI nameText = texts.Length > 0 ? texts[0] : null;
        TextMeshProUGUI descText = texts.Length > 1 ? texts[1] : null;

        if (icon != null)
        {
            icon.sprite = achievement.achievementIcon;
            icon.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        if (nameText != null)
        {
            nameText.text = achievement.achievementName;
            nameText.color = isUnlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
        }
        if (descText != null)
        {
            descText.text = achievement.achievementDescription;
            descText.color = isUnlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
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
