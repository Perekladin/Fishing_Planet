using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementUI : MonoBehaviour
{

    [Header("UI Ёлементы")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;

    public void Setup(AchievementSystem.AchievementData data, bool unlocked, bool isNotification)
    {
        if (icon != null)
        {
            icon.sprite = data.achievementIcon;
            icon.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        if (nameText != null)
        {
            nameText.text = isNotification ? " " + data.achievementName : data.achievementName;
            nameText.color = unlocked ? Color.white : Color.gray;
        }

        if (descText != null)
        {
            descText.text = data.achievementDescription;
            descText.color = unlocked ? Color.white : Color.gray;
        }
    }
}
