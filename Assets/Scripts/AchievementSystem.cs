using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class AchievementSystem : MonoBehaviour
{
    [System.Serializable]
    public class AchievementData
    {
        public string achievementId;           // Уникальный ID
        public string achievementName;         // Название
        public string achievementDescription;  // Описание условия
        public Sprite achievementIcon;         // Картинка достижения
        public bool isUnlocked;                // Разблокировано?
    }

    [Header("UI Элементы")]
    [SerializeField] private GameObject achievementCanvas;
    [SerializeField] private Transform achievementContainer;  // Grid Layout Group
    [SerializeField] private GameObject achievementPrefab;     // Префаб элемента достижения

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

    //  РАЗБЛОКИРОВКА ДОСТИЖЕНИЯ (вызывать из других сцен)
    public void UnlockAchievement(string achievementId)
    {
        AchievementData achievement = achievements.Find(a => a.achievementId == achievementId);
        if (achievement != null && !achievement.isUnlocked)
        {
            achievement.isUnlocked = true;
            SaveAchievements();
            RefreshAchievementUI();

            Debug.Log($" Достижение разблокировано: {achievement.achievementName}");
        }
    }

    // Показать/скрыть Canvas достижений
    public void ToggleAchievementCanvas()
    {
        achievementCanvas.SetActive(!achievementCanvas.activeSelf);
    }

    // Обновить UI достижений
    private void RefreshAchievementUI()
    {
        // Очищаем старые элементы
        foreach (Transform child in achievementContainer)
        {
            Destroy(child.gameObject);
        }

        // Создаем новые элементы
        foreach (var achievement in achievements)
        {
            if (achievement.isUnlocked)
            {
                CreateAchievementUIElement(achievement);
            }
        }
    }

    // Создать UI элемент достижения
    private void CreateAchievementUIElement(AchievementData achievement)
    {
        GameObject achievementElement = Instantiate(achievementPrefab, achievementContainer);

        // Находим компоненты в префабе
        Image icon = achievementElement.GetComponentInChildren<Image>();
        TextMeshProUGUI nameText = achievementElement.GetComponentInChildren<TextMeshProUGUI>();
        TextMeshProUGUI descText = achievementElement.GetComponentsInChildren<TextMeshProUGUI>()[1];

        // Заполняем данные
        if (icon != null) icon.sprite = achievement.achievementIcon;
        if (nameText != null) nameText.text = achievement.achievementName;
        if (descText != null) descText.text = achievement.achievementDescription;
    }

    //  СОХРАНЕНИЕ
    private void SaveAchievements()
    {
        for (int i = 0; i < achievements.Count; i++)
        {
            PlayerPrefs.SetInt("Achievement_" + achievements[i].achievementId, achievements[i].isUnlocked ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    //  ЗАГРУЗКА
    private void LoadAchievements()
    {
        foreach (var achievement in achievements)
        {
            achievement.isUnlocked = PlayerPrefs.GetInt("Achievement_" + achievement.achievementId, 0) == 1;
        }
    }

    // Проверка прогресса (вызывать из других скриптов)
    public bool IsAchievementUnlocked(string achievementId)
    {
        return achievements.Find(a => a.achievementId == achievementId)?.isUnlocked ?? false;
    }

    // Получить количество разблокированных достижений
    public int GetUnlockedCount()
    {
        return achievements.FindAll(a => a.isUnlocked).Count;
    }
}
