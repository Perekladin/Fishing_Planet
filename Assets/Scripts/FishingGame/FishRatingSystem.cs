using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class FishRatingData
{
    public string fishName;
    public float weight;
    public int rarity;
    public float ratingScore;
    public string description;
    public Sprite fishSprite;
}

public class FishRatingSystem : MonoBehaviour
{
    public static FishRatingSystem Instance { get; private set; }
    private List<FishRatingData> catchHistory = new List<FishRatingData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadCatchHistory();
    }

    public void AddFish(FishRatingData fish)
    {
        fish.ratingScore = CalculateRatingScore(fish);
        catchHistory.Add(fish);
        SaveCatchHistory();
        Debug.Log($"Рыба добавлена: {fish.fishName} | Рейтинг: {fish.ratingScore:F1}");
    }

    public FishRatingData GetBestFish(string fishName)
    {
        return catchHistory
            .Where(f => f.fishName == fishName)
            .OrderByDescending(f => f.ratingScore)
            .FirstOrDefault();
    }

    public List<FishRatingData> GetAllFish() => catchHistory;

    private float CalculateRatingScore(FishRatingData fish)
    {
        float weightScore = (fish.weight / 15f) * 50f;
        float rarityScore = (fish.rarity / 5f) * 50f;
        return Mathf.Clamp(weightScore + rarityScore, 0f, 100f);
    }

    private void SaveCatchHistory()
    {
        PlayerPrefs.SetInt("FishHistoryCount", catchHistory.Count);
        for (int i = 0; i < catchHistory.Count; i++)
        {
            string key = "FishHistory_" + i;
            PlayerPrefs.SetString(key + "_Name", catchHistory[i].fishName);
            PlayerPrefs.SetFloat(key + "_Weight", catchHistory[i].weight);
            PlayerPrefs.SetInt(key + "_Rarity", catchHistory[i].rarity);
            PlayerPrefs.SetFloat(key + "_Score", catchHistory[i].ratingScore);
            PlayerPrefs.SetString(key + "_Description", catchHistory[i].description ?? "");
        }
        PlayerPrefs.Save();
    }

    public void LoadCatchHistory()
    {
        catchHistory.Clear();
        int count = PlayerPrefs.GetInt("FishHistoryCount", 0);
        for (int i = 0; i < count; i++)
        {
            string key = "FishHistory_" + i;
            if (PlayerPrefs.HasKey(key + "_Name"))
            {
                FishRatingData fish = new FishRatingData
                {
                    fishName = PlayerPrefs.GetString(key + "_Name"),
                    weight = PlayerPrefs.GetFloat(key + "_Weight"),
                    rarity = PlayerPrefs.GetInt(key + "_Rarity"),
                    ratingScore = PlayerPrefs.GetFloat(key + "_Score"),
                    description = PlayerPrefs.GetString(key + "_Description")
                };
                catchHistory.Add(fish);
            }
        }
    }
}
