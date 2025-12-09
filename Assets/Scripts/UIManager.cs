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
    [SerializeField] private GameObject mainMenuCanvas;        // ГЛАВНЫЙ Canvas
    [SerializeField] private GameObject fishratingCanvas;
    [SerializeField] private GameObject equipmentCanvas;
    [SerializeField] private GameObject achievementsCanvas;

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
    }

    private void HideAllCanvases()
    {
        mainMenuCanvas.SetActive(false);
        fishratingCanvas.SetActive(false);
        equipmentCanvas.SetActive(false);
        achievementsCanvas.SetActive(false);
    }

    public void LoadNewScene()
    {
        // Загрузка через Inspector
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
        fishratingCanvas.SetActive(true);
    }

    public void ShowEquipment()
    {
        HideAllCanvases(); 
        equipmentCanvas.SetActive(true);
    }

    public void ShowAchievements()
    {
        HideAllCanvases();          
        achievementsCanvas.SetActive(true);
    }

    // Дополнительно: кнопка "Назад" для всех Canvas
    public void BackToMainMenu()
    {
        HideAllCanvases();
        mainMenuCanvas.SetActive(true);  // ПОКАЗЫВАЕМ ГЛАВНЫЙ
    }
}
