using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [SerializeField] private AchievementSystem achievementSystem;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (achievementSystem == null)
                achievementSystem = FindObjectOfType<AchievementSystem>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UnlockAchievement(string id)
    {
        achievementSystem.UnlockAchievement(id);
    }
}
