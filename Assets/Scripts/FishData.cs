using UnityEngine;

[CreateAssetMenu(fileName = "FishData", menuName = "Fishing/FishData")]
[System.Serializable]
public class FishData : ScriptableObject
{
    [Header("Информация о рыбе")]
    public string fishName;
    public float weight;
    public Sprite fishSprite;
    public int rarity;
    [HideInInspector] public float ratingScore;
}
