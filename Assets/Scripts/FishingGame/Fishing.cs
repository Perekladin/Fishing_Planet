using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Fishing : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARPlaneManager arPlaneManager;
    [SerializeField] private GameObject defaultPondPrefab; // Префаб по умолчанию

    [Header("Пруд игрока")]
    [SerializeField] private Material playerPondMaterial; // Материал для пруда игрока

    [Header("Кнопки")]
    [SerializeField] private Button castButton;
    [SerializeField] private Button resetPondButton;
    [SerializeField] private Button backToDrawingButton;

    [Header("Заброс")]
    [SerializeField] private GameObject floatPrefab;
    [SerializeField] private Transform rodTip;
    [SerializeField] private float castForce = 8f;
    [SerializeField] private float castHeight = 4f;

    [Header("Мини-игра")]
    [SerializeField] private GameObject miniGamePanel;
    [SerializeField] private Slider controlSlider;
    [SerializeField] private Image successZoneImage;
    [SerializeField] private float baseHoldTime = 3f;
    [SerializeField] private float zoneSpeed = 0.25f;
    [SerializeField] private float zoneWidth = 0.25f;

    [Header("Результат")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI fishNameText;
    [SerializeField] private TextMeshProUGUI fishWeightText;
    [SerializeField] private TextMeshProUGUI fishRarityText;
    [SerializeField] private TextMeshProUGUI fishDescriptionText;
    [SerializeField] private Button closeResultButton;

    [Header("Интерфейс")]
    [SerializeField] private TextMeshProUGUI pondInfoText;
    [SerializeField] private GameObject waitingForPondPanel;

    // Ссылки на объекты
    private GameObject currentPond;
    private GameObject currentFloat;
    private PlayerPondIdentifier playerPondComponent;

    // Состояния
    private bool pondPlaced;
    private bool isFishing;
    private bool miniGameActive;
    private bool floatAlreadyInPond;
    private bool isPlayerPond = false;

    // Таймеры
    private float requiredHoldTime;
    private float currentHoldTime;
    private float zoneCenter = 0.5f;

    // Данные рыбы
    private FishData currentFish;

    // Оборудование
    private string currentRod = "Basic";

    // Данные пруда
    private List<Vector3> pondPoints;
    private float pondBrushSize;

    // ================== ОПИСАНИЕ РЫБ ==================
    private readonly Dictionary<string, string> fishDescriptions = new Dictionary<string, string>()
    {
        { "Окунь", "Окунь — мелкая хищная рыба, водится в реках и озёрах с пресной водой." },
        { "Карп", "Карп — пресноводная рыба, предпочитает тихие озёры и пруды с растительностью." },
        { "Щука", "Щука — хищная рыба, обитает в реках и заросших озёрах, любит прятаться среди растений." },
        { "Сом", "Сом — крупная донная рыба, встречается в глубоких реках и озёрах с песчаным или илистым дном." },
        { "Форель", "Форель — рыба холодных рек и горных озёр, предпочитает чистую воду и быстрые потоки." },
        { "Карась", "Карась — неприхотливая рыба, живет в прудах и озерах с илистым дном." },
        { "Лещ", "Лещ — стайная рыба, предпочитает глубокие места с медленным течением." },
        { "Язь", "Язь — всеядная рыба, обитает в реках с умеренным течением и чистой водой." },
        { "Судак", "Судак — хищник, любит глубокие места с каменистым дном." },
        { "Плотва", "Плотва — небольшая рыба, встречается в реках и озерах по всей территории." }
    };

    private readonly Dictionary<string, List<string>> rodFishMap = new Dictionary<string, List<string>>()
    {
        { "Basic", new List<string> { "Окунь", "Карась", "Плотва" } },
        { "Advanced", new List<string> { "Карп", "Лещ", "Язь", "Щука" } },
        { "Pro", new List<string> { "Сом", "Форель", "Судак" } }
    };

    // ================== START ==================
    private void Start()
    {
        InitializeUI();
        LoadEquipment();

        // Пытаемся создать пруд игрока
        CreatePlayerPond();

        // Если пруд игрока не создан, используем стандартный
        if (!pondPlaced)
        {
            StartCoroutine(WaitForARPlane());
        }

        UpdatePondInfo();
    }

    private void InitializeUI()
    {
        if (castButton != null)
            castButton.onClick.AddListener(OnCastButton);

        if (resetPondButton != null)
            resetPondButton.onClick.AddListener(ResetPond);

        if (backToDrawingButton != null)
            backToDrawingButton.onClick.AddListener(BackToDrawing);

        if (closeResultButton != null)
            closeResultButton.onClick.AddListener(() => resultPanel.SetActive(false));

        if (castButton != null)
            castButton.interactable = false;

        if (miniGamePanel != null)
            miniGamePanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (waitingForPondPanel != null)
            waitingForPondPanel.SetActive(false);
    }

    private void LoadEquipment()
    {
        // Если у вас есть EquipmentSystem
        // if (EquipmentSystem.Instance != null)
        // {
        //     currentRod = EquipmentSystem.Instance.GetSelectedRod().ToString();
        // }

        Debug.Log($"Текущая удочка: {currentRod}");
    }

    // ================== СОЗДАНИЕ ПРУДА ИГРОКА ==================
    private void CreatePlayerPond()
    {
        // Ищем контейнер с данными пруда
        GameObject dataManagerObj = GameObject.Find("PondDataManager");
        if (dataManagerObj != null)
        {
            ARDrawingManager.PondDataContainer dataContainer = dataManagerObj.GetComponent<ARDrawingManager.PondDataContainer>();

            if (dataContainer != null && dataContainer.pondData != null)
            {
                ARDrawingManager.PondSaveData pondData = dataContainer.pondData;
                pondPoints = pondData.points;
                pondBrushSize = pondData.brushSize;

                // Создаем меш пруда
                currentPond = CreatePondFromData(pondData);

                if (currentPond != null)
                {
                    playerPondComponent = currentPond.AddComponent<PlayerPondIdentifier>();
                    playerPondComponent.isPlayerCreated = true;

                    pondPlaced = true;
                    isPlayerPond = true;

                    if (castButton != null)
                        castButton.interactable = true;

                    // Отключаем AR плоскости
                    if (arPlaneManager != null)
                    {
                        foreach (ARPlane plane in arPlaneManager.trackables)
                        {
                            plane.gameObject.SetActive(false);
                        }
                        arPlaneManager.enabled = false;
                    }

                    Debug.Log($"Пруд игрока создан! Точек: {pondPoints.Count}, Размер: {pondBrushSize}");
                    return;
                }
            }
        }

        Debug.Log("Пруд игрока не найден, будет использован стандартный");
        isPlayerPond = false;
    }

    private GameObject CreatePondFromData(ARDrawingManager.PondSaveData pondData)
    {
        if (pondData == null || pondData.points == null || pondData.points.Count < 3)
        {
            Debug.LogError("Недостаточно данных для создания пруда");
            return null;
        }

        GameObject pond = new GameObject("PlayerPond_InFishing");
        pond.transform.position = pondData.position;
        pond.transform.rotation = pondData.rotation;

        // Создаем меш
        MeshFilter mf = pond.AddComponent<MeshFilter>();
        MeshRenderer mr = pond.AddComponent<MeshRenderer>();

        if (playerPondMaterial != null)
        {
            mr.material = playerPondMaterial;
        }
        else
        {
            mr.material = new Material(Shader.Find("Standard"));
            mr.material.color = new Color(0, 0.5f, 1f, 0.7f);
            mr.material.SetFloat("_Mode", 3); // Transparent mode
            mr.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mr.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mr.material.SetInt("_ZWrite", 0);
            mr.material.DisableKeyword("_ALPHATEST_ON");
            mr.material.EnableKeyword("_ALPHABLEND_ON");
            mr.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mr.material.renderQueue = 3000;
        }

        // Подготавливаем вершины
        Vector3[] vertices2D = new Vector3[pondData.points.Count];
        for (int i = 0; i < pondData.points.Count; i++)
        {
            vertices2D[i] = new Vector3(pondData.points[i].x, pond.transform.position.y, pondData.points[i].z);
        }

        // Триангуляция (используем вложенный класс из ARDrawingManager)
        ARDrawingManager.Triangulator tr = new ARDrawingManager.Triangulator(vertices2D);
        int[] indices = tr.Triangulate();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices2D;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;

        // Добавляем коллайдер для взаимодействия
        MeshCollider collider = pond.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = true;
        collider.isTrigger = true;

        // Добавляем триггер для поплавка
        PondTrigger trigger = pond.AddComponent<PondTrigger>();
        trigger.SetFishingManager(this);

        return pond;
    }

    // ================== СТАНДАРТНЫЙ ПРУД ==================
    private IEnumerator WaitForARPlane()
    {
        if (waitingForPondPanel != null)
            waitingForPondPanel.SetActive(true);

        if (pondInfoText != null)
            pondInfoText.text = "Ищем поверхность для пруда...";

        yield return new WaitForSeconds(1f);

        while (!pondPlaced)
        {
            TryPlaceDefaultPond();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void TryPlaceDefaultPond()
    {
        if (arPlaneManager == null || defaultPondPrefab == null) return;

        foreach (ARPlane plane in arPlaneManager.trackables)
        {
            if (plane.alignment == PlaneAlignment.HorizontalUp && plane.extents.x > 0.5f)
            {
                // Размещаем стандартный пруд
                currentPond = Instantiate(defaultPondPrefab, plane.center + Vector3.up * 0.01f, Quaternion.identity);
                currentPond.transform.localScale = new Vector3(0.8f, 0.01f, 0.8f);

                pondPlaced = true;
                isPlayerPond = false;

                if (castButton != null)
                    castButton.interactable = true;

                // Отключаем остальные плоскости
                foreach (ARPlane p in arPlaneManager.trackables)
                {
                    p.gameObject.SetActive(false);
                }

                if (waitingForPondPanel != null)
                    waitingForPondPanel.SetActive(false);

                UpdatePondInfo();

                Debug.Log("Стандартный пруд размещен");
                break;
            }
        }
    }

    // ================== ОБНОВЛЕНИЕ ИНТЕРФЕЙСА ==================
    private void UpdatePondInfo()
    {
        if (pondInfoText != null)
        {
            if (isPlayerPond)
            {
                pondInfoText.text = $"Пруд игрока\nТочек: {pondPoints?.Count ?? 0}\nУдочка: {currentRod}";
            }
            else
            {
                pondInfoText.text = $"Стандартный пруд\nУдочка: {currentRod}";
            }
        }
    }

    // ================== ОСНОВНОЙ UPDATE ==================
    private void Update()
    {
        if (miniGameActive)
        {
            UpdateMiniGame();
        }

        // Обновляем позицию зоны успеха
        if (miniGamePanel != null && miniGamePanel.activeSelf)
        {
            UpdateZonePosition();
        }
    }

    // ================== РЫБАЛКА ==================
    private void OnCastButton()
    {
        if (!pondPlaced || isFishing) return;

        isFishing = true;
        floatAlreadyInPond = false;

        if (castButton != null)
            castButton.interactable = false;

        // Создаем поплавок
        if (floatPrefab != null && rodTip != null)
        {
            currentFloat = Instantiate(floatPrefab, rodTip.position, Quaternion.identity);
            Rigidbody rb = currentFloat.GetComponent<Rigidbody>();

            if (rb != null && currentPond != null)
            {
                // Направление заброса (в сторону центра пруда)
                Vector3 pondCenter = currentPond.transform.position;
                Vector3 castDirection = (pondCenter - rodTip.position).normalized;

                rb.AddForce(castDirection * castForce + Vector3.up * castHeight, ForceMode.Impulse);
            }
        }

        // Таймаут на случай если поплавок не попадет в пруд
        StartCoroutine(FailSafeTimeout());

        Debug.Log("Заброс выполнен");
    }

    // Вызывается PondTrigger когда поплавок попадает в пруд
    public void OnFloatEnteredPond(GameObject floatObject)
    {
        if (!isFishing || floatAlreadyInPond) return;
        if (floatObject == null || !floatObject.CompareTag("Float")) return;

        floatAlreadyInPond = true;
        StopCoroutine(nameof(FailSafeTimeout));

        // Начинаем мини-игру через случайную задержку
        float delay = Random.Range(1.5f, 4f);
        StartCoroutine(StartMiniGameAfterDelay(delay));

        Debug.Log("Поплавок в пруду! Ожидаем поклевку...");
    }

    private IEnumerator StartMiniGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isFishing && floatAlreadyInPond)
        {
            StartMiniGame();
        }
    }

    // ================== МИНИ-ИГРА ==================
    private void StartMiniGame()
    {
        // Генерируем рыбу
        currentFish = GenerateFish();

        // Корректируем рыбу в зависимости от удочки
        if (rodFishMap.ContainsKey(currentRod) && !rodFishMap[currentRod].Contains(currentFish.fishName))
        {
            List<string> allowedFish = rodFishMap[currentRod];
            if (allowedFish.Count > 0)
            {
                string fishName = allowedFish[Random.Range(0, allowedFish.Count)];

                currentFish = new FishData
                {
                    fishName = fishName,
                    weight = Random.Range(0.5f, 8f),
                    rarity = Random.Range(1, 6),
                    description = fishDescriptions.ContainsKey(fishName) ? fishDescriptions[fishName] : "Неизвестная рыба"
                };
            }
        }

        // Рассчитываем время удержания
        requiredHoldTime = baseHoldTime + currentFish.weight * 0.3f + currentFish.rarity * 0.5f;
        currentHoldTime = 0f;

        if (controlSlider != null)
            controlSlider.value = 0.5f;

        // Активируем мини-игру
        miniGameActive = true;

        if (miniGamePanel != null)
            miniGamePanel.SetActive(true);

        Debug.Log($"Мини-игра: {currentFish.fishName}, Вес: {currentFish.weight}, Сложность: {requiredHoldTime:F1}с");
    }

    private void UpdateMiniGame()
    {
        // Проверяем ввод
        bool isHolding = false;

        // Сенсорный ввод
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            isHolding = true;
        }

        // Ввод в редакторе
#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isHolding = true;
        }
#endif

        // Обновляем слайдер
        if (isHolding && controlSlider != null)
        {
            controlSlider.value = Mathf.Clamp01(controlSlider.value + Time.deltaTime * 0.5f);
        }

        // Проверяем находится ли ползунок в зоне успеха
        bool inSuccessZone = controlSlider != null && Mathf.Abs(controlSlider.value - zoneCenter) <= zoneWidth / 2f;

        // Обновляем время удержания
        if (isHolding && inSuccessZone)
        {
            currentHoldTime += Time.deltaTime;
        }
        else
        {
            currentHoldTime = Mathf.Max(0f, currentHoldTime - Time.deltaTime * 0.5f);
        }

        // Проверяем завершение
        if (currentHoldTime >= requiredHoldTime)
        {
            FinishFishing(true);
        }
    }

    private void UpdateZonePosition()
    {
        // Двигаем зону успеха
        zoneCenter = Mathf.PingPong(Time.time * zoneSpeed, 1f - zoneWidth) + zoneWidth / 2f;

        // Обновляем позицию зоны на слайдере
        if (controlSlider != null && successZoneImage != null)
        {
            RectTransform sliderRect = controlSlider.GetComponent<RectTransform>();
            RectTransform zoneRect = successZoneImage.rectTransform;

            if (sliderRect != null && zoneRect != null)
            {
                float sliderWidth = sliderRect.rect.width;
                zoneRect.sizeDelta = new Vector2(sliderWidth * zoneWidth, zoneRect.sizeDelta.y);
                zoneRect.anchoredPosition = new Vector2((zoneCenter - 0.5f) * sliderWidth, 0);
            }
        }
    }

    private void FinishFishing(bool success)
    {
        miniGameActive = false;

        if (miniGamePanel != null)
            miniGamePanel.SetActive(false);

        if (success)
        {
            // Показываем результат
            ShowResult(currentFish);

            Debug.Log($"Рыба поймана: {currentFish.fishName} ({currentFish.weight:F1}кг)");
        }
        else
        {
            Debug.Log("Рыба сорвалась!");
        }

        // Сбрасываем состояние рыбалки
        ResetFishing();
    }

    private void ShowResult(FishData fish)
    {
        if (resultPanel != null)
        {
            if (fishNameText != null)
                fishNameText.text = fish.fishName;

            if (fishWeightText != null)
                fishWeightText.text = $"Вес: {fish.weight:F1} кг";

            if (fishRarityText != null)
                fishRarityText.text = $"Редкость: {fish.rarity}/5";

            if (fishDescriptionText != null)
                fishDescriptionText.text = fish.description;

            resultPanel.SetActive(true);
        }
    }

    // ================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==================
    private FishData GenerateFish()
    {
        // Список всех возможных рыб
        List<string> allFish = new List<string>(fishDescriptions.Keys);
        string fishName = allFish[Random.Range(0, allFish.Count)];

        // Вес зависит от типа рыбы
        float minWeight = 0.3f;
        float maxWeight = 10f;

        switch (fishName)
        {
            case "Окунь":
            case "Карась":
            case "Плотва":
                maxWeight = 2f;
                break;
            case "Карп":
            case "Лещ":
            case "Язь":
                maxWeight = 5f;
                break;
            case "Щука":
            case "Судак":
                maxWeight = 8f;
                break;
            case "Сом":
                maxWeight = 10f;
                break;
            case "Форель":
                maxWeight = 4f;
                break;
        }

        return new FishData
        {
            fishName = fishName,
            weight = Random.Range(minWeight, maxWeight),
            rarity = Random.Range(1, 6),
            description = fishDescriptions[fishName]
        };
    }

    private IEnumerator FailSafeTimeout()
    {
        yield return new WaitForSeconds(5f);

        if (isFishing && !floatAlreadyInPond)
        {
            Debug.Log("Таймаут: поплавок не попал в пруд");
            ResetFishing();
        }
    }

    private void ResetFishing()
    {
        if (currentFloat != null)
        {
            Destroy(currentFloat);
            currentFloat = null;
        }

        isFishing = false;
        miniGameActive = false;
        floatAlreadyInPond = false;

        if (castButton != null)
            castButton.interactable = pondPlaced;

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    public void ResetPond()
    {
        ResetFishing();

        if (currentPond != null)
        {
            Destroy(currentPond);
            currentPond = null;
        }

        pondPlaced = false;

        if (castButton != null)
            castButton.interactable = false;

        // Включаем AR плоскости
        if (arPlaneManager != null)
        {
            arPlaneManager.enabled = true;
            foreach (ARPlane plane in arPlaneManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
        }

        // Создаем новый пруд
        if (isPlayerPond)
        {
            CreatePlayerPond();
        }
        else
        {
            StartCoroutine(WaitForARPlane());
        }

        UpdatePondInfo();
    }

    private void BackToDrawing()
    {
        // Очищаем данные пруда если нужно
        GameObject dataManagerObj = GameObject.Find("PondDataManager");
        if (dataManagerObj != null)
        {
            ARDrawingManager.PondDataContainer dataContainer = dataManagerObj.GetComponent<ARDrawingManager.PondDataContainer>();
            if (dataContainer != null)
            {
                dataContainer.pondData = null;
            }
        }

        // Загружаем сцену рисования
        SceneManager.LoadScene("ARDrawingScene");
    }

    // ================== КЛАССЫ ДАННЫХ ==================
    [System.Serializable]
    public class FishData
    {
        public string fishName;
        public float weight;
        public int rarity;
        public string description;
    }
}

// Компонент для триггера пруда
public class PondTrigger : MonoBehaviour
{
    private Fishing fishingManager;

    void Start()
    {
        // Автоматически ищем Fishing менеджер если не установлен
        if (fishingManager == null)
        {
            fishingManager = FindObjectOfType<Fishing>();
        }
    }

    public void SetFishingManager(Fishing manager)
    {
        fishingManager = manager;
    }

    void OnTriggerEnter(Collider other)
    {
        if (fishingManager != null && other != null && other.CompareTag("Float"))
        {
            fishingManager.OnFloatEnteredPond(other.gameObject);
        }
    }
}

// Компонент для идентификации пруда игрока
public class PlayerPondIdentifier : MonoBehaviour
{
    public bool isPlayerCreated = true;

    void Start()
    {
        // Можно добавить визуальные отличия для пруда игрока
        if (isPlayerCreated)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                // Немного другой цвет для пруда игрока
                Color playerColor = new Color(0, 0.6f, 1f, 0.8f);
                mr.material.color = playerColor;
            }
        }
    }
}