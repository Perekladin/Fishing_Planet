using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ARDrawingManager : MonoBehaviour
{
    [Header("Drawing Settings")]
    public GameObject linePrefab;
    public float brushSize = 0.01f;
    private LineRenderer currentLine;
    private List<Vector3> points;

    [Header("Pond Settings")]
    public Material pondMaterial;
    public ARPlaneManager arPlaneManager;
    public GameObject pondPrefab; // префаб для инспектора (скрыть в Start)

    [Header("Auto Close Settings")]
    public bool autoCloseContour = true;
    public float autoCloseDistance = 0.15f; // Расстояние для автоматического замыкания
    public Color openContourColor = Color.red;
    public Color closedContourColor = Color.green;

    [Header("UI References")]
    public Text messageText;
    public Button generatePondButton;
    public Button closeContourButton;
    public Toggle autoCloseToggle;

    private List<GameObject> drawnLines = new List<GameObject>();
    private GameObject pondObject;

    // Для проверки касаний UI
    private GraphicRaycaster uiRaycaster;
    private PointerEventData pointerData;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();

    // Флаг для отслеживания инициализации
    private bool isEnhancedTouchEnabled = false;

    // Для визуализации замыкания
    private LineRenderer closeIndicatorLine;

    // Класс для сохранения данных пруда
    [System.Serializable]
    public class PondSaveData
    {
        public List<Vector3> points;
        public Vector3 position;
        public Quaternion rotation;
        public float brushSize;
    }

    private void InitializeInputForMobile()
    {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
        // Для мобильного билда
        if (EventSystem.current != null)
        {
            // Удаляем старый InputModule если есть
            StandaloneInputModule oldModule = EventSystem.current.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Destroy(oldModule);
            }
            
            // Добавляем InputSystemUIInputModule
            InputSystemUIInputModule newModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if (newModule == null)
            {
                newModule = EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("InputSystemUIInputModule добавлен для мобильного билда");
            }
            
            // Включаем EnhancedTouch
            if (!EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Enable();
                isEnhancedTouchEnabled = true;
            }
        }
#endif
    }

    void Start()
    {
        InitializeInputForMobile();

        // Активируем EnhancedTouch для мобильных устройств
        if (!EnhancedTouchSupport.enabled)
        {
            try
            {
                EnhancedTouchSupport.Enable();
                isEnhancedTouchEnabled = true;
                Debug.Log("EnhancedTouchSupport включен");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Не удалось включить EnhancedTouchSupport: {e.Message}");
                isEnhancedTouchEnabled = false;
            }
        }
        else
        {
            isEnhancedTouchEnabled = true;
        }

        // Скрываем исходный префаб пруда
        if (pondPrefab != null)
            pondPrefab.SetActive(false);

        // Ищем GraphicRaycaster на Canvas
        FindGraphicRaycaster();

        // Создаем индикатор замыкания
        CreateCloseIndicator();

        // Инициализируем UI
        InitializeUI();

        Debug.Log($"Система рисования инициализирована. EnhancedTouch: {isEnhancedTouchEnabled}");
    }

    void CreateCloseIndicator()
    {
        // Создаем простой индикатор
        GameObject indicator = new GameObject("CloseIndicator");
        closeIndicatorLine = indicator.AddComponent<LineRenderer>();
        closeIndicatorLine.enabled = false;
        closeIndicatorLine.widthMultiplier = brushSize * 1.5f;
        closeIndicatorLine.material = new Material(Shader.Find("Sprites/Default"));
        closeIndicatorLine.material.color = Color.yellow;
        closeIndicatorLine.startColor = Color.yellow;
        closeIndicatorLine.endColor = Color.yellow;
        closeIndicatorLine.positionCount = 2;
    }

    void InitializeUI()
    {
        // Настраиваем кнопки если они есть
        if (generatePondButton != null)
        {
            generatePondButton.onClick.AddListener(GeneratePond);
            generatePondButton.interactable = false;
        }

        if (closeContourButton != null)
        {
            closeContourButton.onClick.AddListener(ForceCloseContour);
        }

        if (autoCloseToggle != null)
        {
            autoCloseToggle.isOn = autoCloseContour;
            autoCloseToggle.onValueChanged.AddListener(ToggleAutoClose);
        }

        if (messageText != null)
        {
            messageText.text = "Нарисуйте контур пруда";
        }
    }

    void OnEnable()
    {
        if (!isEnhancedTouchEnabled)
        {
            try
            {
                EnhancedTouchSupport.Enable();
                isEnhancedTouchEnabled = true;
            }
            catch { }
        }
    }

    void OnDisable()
    {
        if (isEnhancedTouchEnabled && EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Disable();
            isEnhancedTouchEnabled = false;
        }
    }

    void FindGraphicRaycaster()
    {
        // Ищем Canvas и GraphicRaycaster в сцене
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            uiRaycaster = canvas.GetComponent<GraphicRaycaster>();
            if (uiRaycaster == null)
            {
                Debug.LogWarning("GraphicRaycaster не найден на Canvas. Добавьте его для корректной работы UI.");
            }
            else
            {
                Debug.Log($"GraphicRaycaster найден: {uiRaycaster.enabled}");
            }
        }
        else
        {
            Debug.LogWarning("Canvas не найден в сцене!");
        }
    }

    void Update()
    {
        HandleDrawing();
        UpdateCloseIndicator();
        UpdateUIState();
    }

    void UpdateUIState()
    {
        if (generatePondButton != null)
        {
            generatePondButton.interactable = (points != null && points.Count >= 3 && IsClosedShape(points));
        }
    }

    void HandleDrawing()
    {
        Vector3 worldPos = Vector3.zero;
        bool hasInput = false;

        // Проверяем различные источники ввода в правильном порядке
        bool isEditor = false;

#if UNITY_EDITOR
        isEditor = true;
#endif

        // 1. Сначала проверяем мышь в редакторе
#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // Проверяем, не нажимаем ли на UI элемент
            if (IsPointerOverUI(mousePos))
            {
                return;
            }

            if (TryGetTouchPositionOnPlane(mousePos, out worldPos))
            {
                hasInput = true;
            }
        }
#endif

        // 2. Проверяем старый Input.Touch для совместимости
        if (!isEditor || (isEditor && !hasInput))
        {
            // Используем полное имя UnityEngine.Touch для устранения неоднозначности
            if (Input.touchCount > 0)
            {
                UnityEngine.Touch touch = Input.GetTouch(0);

                // Проверяем, не нажимаем ли на UI элемент
                if (IsPointerOverUI(touch.position))
                {
                    return;
                }

                // Используем полное имя UnityEngine.TouchPhase
                if (touch.phase == UnityEngine.TouchPhase.Began ||
                    touch.phase == UnityEngine.TouchPhase.Moved ||
                    touch.phase == UnityEngine.TouchPhase.Stationary)
                {
                    if (TryGetTouchPositionOnPlane(touch.position, out worldPos))
                    {
                        hasInput = true;
                    }
                }
            }

            // 3. Проверяем Enhanced Touch (новый Input System)
            if (isEnhancedTouchEnabled && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
            {
                // Явно указываем полное имя для Enhanced Touch
                UnityEngine.InputSystem.EnhancedTouch.Touch enhancedTouch =
                    UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];

                // Проверяем, не нажимаем ли на UI элемент
                if (IsPointerOverUI(enhancedTouch.screenPosition))
                {
                    return;
                }

                if (enhancedTouch.isInProgress && !enhancedTouch.ended)
                {
                    if (TryGetTouchPositionOnPlane(enhancedTouch.screenPosition, out worldPos))
                    {
                        hasInput = true;
                    }
                }
            }
        }

        if (hasInput)
        {
            if (currentLine == null)
            {
                StartLine(worldPos);
            }
            else
            {
                AddPoint(worldPos);
            }
        }
        else
        {
            if (currentLine != null)
            {
                // Проверяем возможность автоматического замыкания
                if (autoCloseContour && points != null && points.Count >= 3)
                {
                    float distanceToStart = Vector3.Distance(points[points.Count - 1], points[0]);
                    if (distanceToStart <= autoCloseDistance)
                    {
                        // Автоматически замыкаем контур
                        AutoCloseContour();
                        ShowMessage($"Контур автоматически замкнут!", 2f);
                    }
                }

                currentLine = null; // заканчиваем линию при отпускании
                closeIndicatorLine.enabled = false;
            }
        }
    }

    void UpdateCloseIndicator()
    {
        if (currentLine != null && points != null && points.Count >= 3)
        {
            float distanceToStart = Vector3.Distance(points[points.Count - 1], points[0]);

            if (distanceToStart <= autoCloseDistance * 2f) // Показываем индикатор заранее
            {
                closeIndicatorLine.enabled = true;
                closeIndicatorLine.SetPosition(0, points[points.Count - 1]);
                closeIndicatorLine.SetPosition(1, points[0]);

                // Меняем цвет в зависимости от расстояния
                float t = Mathf.Clamp01(1f - (distanceToStart / autoCloseDistance));
                Color indicatorColor = Color.Lerp(Color.red, Color.green, t);
                closeIndicatorLine.startColor = indicatorColor;
                closeIndicatorLine.endColor = indicatorColor;
            }
            else
            {
                closeIndicatorLine.enabled = false;
            }
        }
        else
        {
            closeIndicatorLine.enabled = false;
        }
    }

    void AutoCloseContour()
    {
        if (points == null || points.Count < 3 || currentLine == null) return;

        // Добавляем первую точку в конец для замыкания
        points.Add(points[0]);
        currentLine.positionCount = points.Count;
        currentLine.SetPositions(points.ToArray());

        // Меняем цвет линии на "замкнутый"
        currentLine.startColor = closedContourColor;
        currentLine.endColor = closedContourColor;

        Debug.Log($"Контур замкнут автоматически. Всего точек: {points.Count}");
    }

    // Метод для проверки, находится ли указатель над UI элементом
    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        // Если нет EventSystem или GraphicRaycaster, пропускаем проверку
        if (EventSystem.current == null || uiRaycaster == null)
        {
            return false;
        }

        // Создаем PointerEventData для raycast
        if (pointerData == null)
            pointerData = new PointerEventData(EventSystem.current);

        pointerData.position = screenPosition;

        // Очищаем предыдущие результаты
        raycastResults.Clear();

        // Выполняем raycast
        uiRaycaster.Raycast(pointerData, raycastResults);

        // Если есть результаты, значит указатель над UI
        return raycastResults.Count > 0;
    }

    bool TryGetTouchPositionOnPlane(Vector2 screenPos, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        // Проверяем, существует ли ARRaycastManager
        ARRaycastManager raycastManager = null;
        if (arPlaneManager != null)
        {
            raycastManager = arPlaneManager.GetComponent<ARRaycastManager>();
        }

        if (raycastManager == null)
        {
            // В редакторе это нормально
#if !UNITY_EDITOR
            Debug.LogError("ARRaycastManager не найден. Добавьте его на тот же объект, что и ARPlaneManager.");
#endif
            return false;
        }

        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            worldPos = hits[0].pose.position;
            return true;
        }

        // Дополнительная проверка для дебага в редакторе
#if UNITY_EDITOR
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        // Создаем тестовую плоскость если нет AR
        if (!arPlaneManager || arPlaneManager.trackables.count == 0)
        {
            // Используем плоскость на y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (groundPlane.Raycast(ray, out distance))
            {
                worldPos = ray.GetPoint(distance);
                return true;
            }
        }

        // Проверяем физические коллайдеры
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            worldPos = hit.point;
            return true;
        }
#endif

        return false;
    }

    void StartLine(Vector3 startPos)
    {
        GameObject lineObj = Instantiate(linePrefab);
        currentLine = lineObj.GetComponent<LineRenderer>();

        if (currentLine == null)
        {
            Debug.LogError("LinePrefab не содержит компонент LineRenderer!");
            Destroy(lineObj);
            return;
        }

        // Настраиваем линию
        currentLine.widthMultiplier = brushSize;
        currentLine.useWorldSpace = true;
        currentLine.positionCount = 0;
        currentLine.startColor = openContourColor;
        currentLine.endColor = openContourColor;

        points = new List<Vector3>();
        AddPoint(startPos);
        drawnLines.Add(lineObj);

        ShowMessage("Начинайте рисовать контур пруда", 2f);
    }

    void AddPoint(Vector3 point)
    {
        if (points == null || currentLine == null) return;

        // Проверяем расстояние до последней точки, чтобы избежать слишком частых точек
        if (points.Count > 0)
        {
            float distance = Vector3.Distance(points[points.Count - 1], point);
            if (distance < 0.001f) return; // Минимальное расстояние между точками
        }

        points.Add(point);
        currentLine.positionCount = points.Count;
        currentLine.SetPositions(points.ToArray());

        // Обновляем цвет в зависимости от замкнутости
        UpdateLineColor();

        // Обновляем сообщение
        if (points.Count == 3)
        {
            ShowMessage("Хорошо! Продолжайте рисовать или замкните контур", 2f);
        }
    }

    void UpdateLineColor()
    {
        if (currentLine == null || points == null || points.Count < 3) return;

        float distanceToStart = Vector3.Distance(points[points.Count - 1], points[0]);
        bool isClosed = distanceToStart < 0.1f;

        if (isClosed)
        {
            currentLine.startColor = closedContourColor;
            currentLine.endColor = closedContourColor;
            ShowMessage("Контур замкнут! Теперь можно создать пруд", 3f);
        }
        else
        {
            currentLine.startColor = openContourColor;
            currentLine.endColor = openContourColor;
        }
    }

    // Методы для кнопок UI
    public void IncreaseBrush()
    {
        brushSize += 0.005f;
        if (brushSize > 0.2f) brushSize = 0.2f;
        if (currentLine != null)
            currentLine.widthMultiplier = brushSize;

        ShowMessage($"Размер кисти: {brushSize:F3}", 1f);
    }

    public void DecreaseBrush()
    {
        brushSize -= 0.005f;
        if (brushSize < 0.001f) brushSize = 0.001f;
        if (currentLine != null)
            currentLine.widthMultiplier = brushSize;

        ShowMessage($"Размер кисти: {brushSize:F3}", 1f);
    }

    public void ClearDrawing()
    {
        Debug.Log($"Очистка рисунка: линий {drawnLines.Count}");

        foreach (var line in drawnLines)
            if (line != null) Destroy(line);

        drawnLines.Clear();

        if (pondObject != null)
        {
            Destroy(pondObject);
            Debug.Log("Пруд удален");
        }

        currentLine = null;
        points = null;
        closeIndicatorLine.enabled = false;

        ShowMessage("Рисунок очищен. Нарисуйте новый контур", 2f);

        Debug.Log("Рисунок очищен");
    }

    public void GeneratePond()
    {
        if (points == null || points.Count < 3)
        {
            ShowMessage("Нужно нарисовать хотя бы три точки!", 2f);
            return;
        }

        // Проверяем, замкнут ли контур
        List<Vector3> pondPoints = new List<Vector3>(points);

        if (!IsClosedShape(pondPoints))
        {
            if (autoCloseContour)
            {
                // Автоматически замыкаем контур
                pondPoints.Add(pondPoints[0]);
                ShowMessage("Контур автоматически замкнут!", 2f);

                // Обновляем визуализацию
                if (currentLine != null)
                {
                    currentLine.positionCount = pondPoints.Count;
                    currentLine.SetPositions(pondPoints.ToArray());
                    currentLine.startColor = closedContourColor;
                    currentLine.endColor = closedContourColor;
                }
            }
            else
            {
                ShowMessage("Контур должен быть замкнут!", 2f);
                return;
            }
        }

        // Удаляем старый пруд, если он существует
        if (pondObject != null)
            Destroy(pondObject);

        // Создаем новый пруд
        pondObject = CreatePondMesh(pondPoints);

        // Сохраняем данные пруда
        SavePondData(pondPoints, pondObject.transform.position, pondObject.transform.rotation);

        ShowMessage("Пруд создан! Загружаем сцену рыбалки...", 1f);

        // Загружаем сцену рыбалки
        StartCoroutine(LoadFishingScene());
    }

    private GameObject CreatePondMesh(List<Vector3> pondPoints)
    {
        GameObject pond = new GameObject("PlayerPond");
        MeshFilter mf = pond.AddComponent<MeshFilter>();
        MeshRenderer mr = pond.AddComponent<MeshRenderer>();
        mr.material = pondMaterial;

        Vector3[] vertices2D = new Vector3[pondPoints.Count];

        // Высота по первой AR плоскости
        float yPos = 0f;
        if (arPlaneManager != null)
        {
            foreach (var plane in arPlaneManager.trackables)
            {
                yPos = plane.transform.position.y;
                break;
            }
        }

        for (int i = 0; i < pondPoints.Count; i++)
            vertices2D[i] = new Vector3(pondPoints[i].x, yPos, pondPoints[i].z);

        Triangulator tr = new Triangulator(vertices2D);
        int[] indices = tr.Triangulate();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices2D;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;

        MeshCollider col = pond.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;
        col.convex = true;
        col.isTrigger = true;

        // Добавляем компонент для идентификации
        PlayerPondIdentifier identifier = pond.AddComponent<PlayerPondIdentifier>();
        identifier.isPlayerCreated = true;

        Debug.Log($"Пруд создан! Вершин: {vertices2D.Length}, Треугольников: {indices.Length / 3}");

        return pond;
    }

    private void SavePondData(List<Vector3> pondPoints, Vector3 position, Quaternion rotation)
    {
        // Создаем объект для хранения данных пруда
        GameObject dataManagerObj = GameObject.Find("PondDataManager");
        if (dataManagerObj == null)
        {
            dataManagerObj = new GameObject("PondDataManager");
            DontDestroyOnLoad(dataManagerObj);
        }

        PondDataContainer dataContainer = dataManagerObj.GetComponent<PondDataContainer>();
        if (dataContainer == null)
        {
            dataContainer = dataManagerObj.AddComponent<PondDataContainer>();
        }

        // Сохраняем данные
        dataContainer.pondData = new PondSaveData
        {
            points = pondPoints,
            position = position,
            rotation = rotation,
            brushSize = brushSize
        };

        Debug.Log($"Данные пруда сохранены: {pondPoints.Count} точек");
    }

    private bool IsClosedShape(List<Vector3> shapePoints)
    {
        if (shapePoints.Count < 3) return false;

        // Проверяем, близка ли первая точка к последней
        float distance = Vector3.Distance(shapePoints[0], shapePoints[shapePoints.Count - 1]);
        bool isClosed = distance < 0.1f; // Порог для закрытия контура

        return isClosed;
    }

    // Метод для принудительного замыкания контура
    public void ForceCloseContour()
    {
        if (currentLine != null && points != null && points.Count >= 3)
        {
            if (!IsClosedShape(points))
            {
                AutoCloseContour();
                ShowMessage("Контур принудительно замкнут!", 2f);
            }
            else
            {
                ShowMessage("Контур уже замкнут", 1f);
            }
        }
        else
        {
            ShowMessage("Недостаточно точек для замыкания", 2f);
        }
    }

    // Метод для включения/выключения авто-замыкания
    public void ToggleAutoClose(bool value)
    {
        autoCloseContour = value;
        string message = autoCloseContour ?
            "Авто-замыкание ВКЛ" : "Авто-замыкание ВЫКЛ";

        ShowMessage(message, 1.5f);
    }

    private void ShowMessage(string message, float duration)
    {
        Debug.Log(message);

        if (messageText != null)
        {
            StartCoroutine(ShowMessageCoroutine(message, duration));
        }
    }

    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            messageText.gameObject.SetActive(false);
        }
    }

    private IEnumerator LoadFishingScene()
    {
        yield return new WaitForSeconds(1f);

        // Проверяем существование сцены
        bool sceneExists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneName == "FishingScene" || sceneName == "TestPlay")
            {
                sceneExists = true;
                string sceneToLoad = sceneName;
                Debug.Log($"Загрузка сцены: {sceneToLoad}");
                SceneManager.LoadScene(sceneToLoad);
                break;
            }
        }

        if (!sceneExists)
        {
            Debug.LogError("Сцена рыбалки не найдена в Build Settings!");
            ShowMessage("Ошибка: сцена рыбалки не найдена", 3f);
        }
    }

    public void OnMenuButtonPressed()
    {
        Debug.Log("Возврат в меню...");
        ClearDrawing();

        // Проверяем существование сцены меню
        bool sceneExists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneName == "Menu")
            {
                sceneExists = true;
                break;
            }
        }

        if (sceneExists)
        {
            SceneManager.LoadScene("Menu");
        }
        else
        {
            Debug.LogWarning("Сцена 'Menu' не найдена, загрузка по индексу 0");
            SceneManager.LoadScene(0);
        }
    }

    // Метод для отладки
    public void DebugDrawingSystem()
    {
        Debug.Log("=== ДИАГНОСТИКА СИСТЕМЫ РИСОВАНИЯ ===");
        Debug.Log($"1. EnhancedTouch: {isEnhancedTouchEnabled}");
        Debug.Log($"2. ARPlaneManager: {(arPlaneManager != null ? "есть" : "нет")}");
        Debug.Log($"3. ARRaycastManager: {(arPlaneManager?.GetComponent<ARRaycastManager>() != null ? "есть" : "нет")}");
        Debug.Log($"4. LinePrefab: {(linePrefab != null ? "есть" : "нет")}");
        Debug.Log($"5. Canvas/GraphicRaycaster: {(uiRaycaster != null ? "есть" : "нет")}");
        Debug.Log($"6. EventSystem: {(EventSystem.current != null ? "есть" : "нет")}");
        Debug.Log($"7. Текущая линия: {(currentLine != null ? "есть" : "нет")}");
        Debug.Log($"8. Точки: {points?.Count ?? 0}");
        Debug.Log($"9. Кисть: {brushSize}");
        Debug.Log($"10. Авто-замыкание: {autoCloseContour}");
        Debug.Log($"11. Расстояние замыкания: {autoCloseDistance}");
        Debug.Log("================================");
    }

    // Класс для хранения данных пруда
    public class PondDataContainer : MonoBehaviour
    {
        public PondSaveData pondData;
    }

    // Компонент для идентификации пруда игрока
    public class PlayerPondIdentifier : MonoBehaviour
    {
        public bool isPlayerCreated = true;
    }

    // --- TRIANGULATOR (ИСПРАВЛЕННАЯ ВЕРСИЯ) ---
    public class Triangulator
    {
        private List<Vector2> m_points = new List<Vector2>();

        public Triangulator(Vector3[] points)
        {
            foreach (var p in points)
                m_points.Add(new Vector2(p.x, p.z)); // Для триангуляции используем x и z координаты
        }

        public int[] Triangulate()
        {
            List<int> indices = new List<int>();
            int n = m_points.Count;
            if (n < 3) return indices.ToArray();

            int[] V = new int[n];
            if (Area() > 0)
                for (int v = 0; v < n; v++) V[v] = v;
            else
                for (int v = 0; v < n; v++) V[v] = (n - 1) - v;

            int nv = n;
            int count = 2 * nv;
            for (int m = 0, v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0) return indices.ToArray();
                int u = v;
                if (nv <= u) u = 0;
                v = u + 1;
                if (nv <= v) v = 0;
                int w = v + 1;
                if (nv <= w) w = 0;

                if (Snip(u, v, w, nv, V))
                {
                    int a = V[u], b = V[v], c = V[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);

                    for (int s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];

                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        private float Area()
        {
            int n = m_points.Count;
            float A = 0.0f;

            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = m_points[p];
                Vector2 qval = m_points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }

            return (A * 0.5f);
        }

        private bool Snip(int u, int v, int w, int n, int[] V)
        {
            Vector2 A = m_points[V[u]];
            Vector2 B = m_points[V[v]];
            Vector2 C = m_points[V[w]];

            if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
                return false;

            for (int p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                    continue;

                Vector2 P = m_points[V[p]];

                if (InsideTriangle(A, B, C, P))
                    return false;
            }

            return true;
        }

        private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x; ay = C.y - B.y;
            bx = A.x - C.x; by = A.y - C.y;
            cx = B.x - A.x; cy = B.y - A.y;
            apx = P.x - A.x; apy = P.y - A.y;
            bpx = P.x - B.x; bpy = P.y - B.y;
            cpx = P.x - C.x; cpy = P.y - C.y;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }
}