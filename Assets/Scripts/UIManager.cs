using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Кнопки")]
    [SerializeField] private Button loadSceneButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private Button fishratingButton;
    [SerializeField] private Button equipmentButton;
    [SerializeField] private Button achievementsButton;

    [Header("Canvas")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject fishratingCanvas;
    [SerializeField] private GameObject equipmentCanvas;
    [SerializeField] private GameObject achievementsCanvas;

    [Header("Achievement UI")]
    [SerializeField] private Transform achievementContainer;  // контейнер для элементов достижений
    [SerializeField] private GameObject achievementPrefab;    // префаб одного элемента

    [Header("Загрузка сцены")]
    [SerializeField] private string sceneToLoad = ""; // Имя сцены через Inspector

    private void Start()
    {
        // Подписываемся на кнопки
        loadSceneButton.onClick.AddListener(LoadNewScene);
        quitGameButton.onClick.AddListener(QuitGame);
        fishratingButton.onClick.AddListener(ShowFishRating);
        equipmentButton.onClick.AddListener(ShowEquipment);
        achievementsButton.onClick.AddListener(ShowAchievements);

        // Скрываем все Canvas кроме главного
        HideAllCanvases();
        mainMenuCanvas.SetActive(true);

        // Регистрируем Canvas и префаб в AchievementSystem
        if (AchievementSystem.Instance != null)
        {
            AchievementSystem.Instance.RegisterUI(
                achievementsCanvas,
                achievementContainer,
                achievementPrefab
            );
        }
    }

    private void HideAllCanvases()
    {
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
        if (fishratingCanvas != null) fishratingCanvas.SetActive(false);
        if (equipmentCanvas != null) equipmentCanvas.SetActive(false);
        if (achievementsCanvas != null) achievementsCanvas.SetActive(false);
    }

    public void LoadNewScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowFishRating()
    {
        HideAllCanvases();
        if (fishratingCanvas != null) fishratingCanvas.SetActive(true);
    }

    public void ShowEquipment()
    {
        HideAllCanvases();
        if (equipmentCanvas != null) equipmentCanvas.SetActive(true);
    }

    public void ShowAchievements()
    {
        HideAllCanvases();
        if (achievementsCanvas != null) achievementsCanvas.SetActive(true);

        // Обновляем UI достижений при открытии Canvas
        if (AchievementSystem.Instance != null)
        {
            AchievementSystem.Instance.RefreshAchievementUI();
        }
    }

    public void BackToMainMenu()
    {
        HideAllCanvases();
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);
    }
}
