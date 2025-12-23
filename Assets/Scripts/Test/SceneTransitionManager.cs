using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Настройки")]
    public bool autoFixOnStart = true;
    public bool logDebugInfo = true;

    [Header("Input System")]
    public bool useNewInputSystem = true;

    private Canvas canvas;
    private GraphicRaycaster raycaster;

    void Start()
    {
        if (autoFixOnStart)
        {
            StartCoroutine(FixCanvasDelayed());
        }
    }

    private IEnumerator FixCanvasDelayed()
    {
        // Ждем один кадр, чтобы все компоненты успели инициализироваться
        yield return null;
        FixCanvas();
    }

    public void FixCanvas()
    {
        if (logDebugInfo) Debug.Log("=== CanvasFixer: Начинаем исправление ===");

        // Получаем Canvas
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("CanvasFixer: Canvas не найден в сцене!");
                CreateNewCanvas();
                return;
            }
        }

        if (logDebugInfo) Debug.Log($"Canvas найден: {canvas.name}");

        // 1. Настраиваем Canvas
        FixCanvasSettings();

        // 2. Настраиваем GraphicRaycaster
        FixGraphicRaycaster();

        // 3. Настраиваем EventSystem
        FixEventSystem();

        // 4. Настраиваем Input System
        FixInputSystem();

        // 5. Активируем все кнопки
        FixAllButtons();

        // 6. Проверяем настройки рендера
        FixRenderSettings();

        if (logDebugInfo) Debug.Log("=== CanvasFixer: Исправление завершено ===");
    }

    private void FixCanvasSettings()
    {
        // Проверяем и настраиваем Canvas
        if (canvas.worldCamera == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                canvas.worldCamera = mainCamera;
                if (logDebugInfo) Debug.Log($"Назначена камера: {mainCamera.name}");
            }
        }

        // Для AR сцены лучше использовать Screen Space - Camera
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.planeDistance = 1;

        // Включаем Canvas
        canvas.enabled = true;

        // Добавляем CanvasScaler если нет
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (logDebugInfo) Debug.Log("Добавлен CanvasScaler");
        }
    }

    private void FixGraphicRaycaster()
    {
        raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            if (logDebugInfo) Debug.Log("Добавлен GraphicRaycaster");
        }

        raycaster.enabled = true;
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.All;

        // Добавляем PhysicsRaycaster для AR
        Camera canvasCamera = canvas.worldCamera;
        if (canvasCamera != null)
        {
            PhysicsRaycaster physicsRaycaster = canvasCamera.GetComponent<PhysicsRaycaster>();
            if (physicsRaycaster == null)
            {
                physicsRaycaster = canvasCamera.gameObject.AddComponent<PhysicsRaycaster>();
                if (logDebugInfo) Debug.Log("Добавлен PhysicsRaycaster на камеру");
            }
        }
    }

    private void FixEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();

        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            if (logDebugInfo) Debug.Log("Создан новый EventSystem");
        }
        else
        {
            if (logDebugInfo) Debug.Log($"EventSystem найден: {eventSystem.name}");
        }

        // Убедимся, что есть модуль ввода
        StandaloneInputModule oldInputModule = eventSystem.GetComponent<StandaloneInputModule>();

        if (useNewInputSystem)
        {
            // Используем Input System UI Input Module
            InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputSystemModule == null)
            {
                // Удаляем старый модуль если есть
                if (oldInputModule != null)
                {
                    Destroy(oldInputModule);
                }

                inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                if (logDebugInfo) Debug.Log("Добавлен InputSystemUIInputModule");
            }

            // Настраиваем модуль
            inputSystemModule.enabled = true;
            eventSystem.sendNavigationEvents = true;
        }
        else
        {
            // Используем старый StandaloneInputModule
            if (oldInputModule == null)
            {
                oldInputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                if (logDebugInfo) Debug.Log("Добавлен StandaloneInputModule");
            }

            oldInputModule.enabled = true;
        }

        // Активируем EventSystem
        eventSystem.enabled = true;

        // Устанавливаем первую выбранную кнопку
        Button firstButton = canvas.GetComponentInChildren<Button>();
        if (firstButton != null)
        {
            eventSystem.firstSelectedGameObject = firstButton.gameObject;
            if (logDebugInfo) Debug.Log($"Установлена первая кнопка: {firstButton.name}");
        }
    }

    private void FixInputSystem()
    {
        // Включаем Enhanced Touch для мобильных устройств
        if (useNewInputSystem && !EnhancedTouchSupport.enabled)
        {
            try
            {
                EnhancedTouchSupport.Enable();
                if (logDebugInfo) Debug.Log("EnhancedTouchSupport включен");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Не удалось включить EnhancedTouchSupport: {e.Message}");
            }
        }

        // Проверяем наличие Input Action Asset
        GameObject inputSystemObj = GameObject.Find("InputSystem");
        if (inputSystemObj == null)
        {
            inputSystemObj = new GameObject("InputSystem");
            DontDestroyOnLoad(inputSystemObj);

            // Добавляем PlayerInput для глобального управления
            PlayerInput playerInput = inputSystemObj.AddComponent<PlayerInput>();
            playerInput.neverAutoSwitchControlSchemes = true;

            if (logDebugInfo) Debug.Log("Создан глобальный InputSystem объект");
        }
    }

    private void FixAllButtons()
    {
        // Находим ВСЕ кнопки в сцене, включая неактивные
        Button[] allButtons = canvas.GetComponentsInChildren<Button>(true);

        if (logDebugInfo) Debug.Log($"Найдено кнопок: {allButtons.Length}");

        int fixedCount = 0;
        foreach (Button button in allButtons)
        {
            // Включаем кнопку
            button.enabled = true;
            button.interactable = true;

            // Проверяем и исправляем Navigation
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None; // Отключаем навигацию если не нужно
            button.navigation = navigation;

            // Добавляем компонент звука если нет
            AudioSource audioSource = button.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = button.gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Добавляем аниматор если нет
            Animator animator = button.GetComponent<Animator>();
            if (animator == null && button.GetComponent<Animation>() == null)
            {
                // Можно добавить простой аниматор для feedback
            }

            // Проверяем RectTransform
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Убедимся, что кнопка имеет ненулевой размер
                if (rectTransform.sizeDelta.x < 10 || rectTransform.sizeDelta.y < 10)
                {
                    rectTransform.sizeDelta = new Vector2(160, 60);
                    if (logDebugInfo) Debug.Log($"Исправлен размер кнопки: {button.name}");
                }
            }

            // Логируем информацию о кнопке
            if (logDebugInfo)
            {
                Debug.Log($"Кнопка: {button.name}, " +
                         $"Parent: {button.transform.parent?.name}, " +
                         $"Active: {button.gameObject.activeSelf}, " +
                         $"Interactable: {button.interactable}");
            }

            fixedCount++;
        }

        if (logDebugInfo) Debug.Log($"Активировано кнопок: {fixedCount}");
    }

    private void FixRenderSettings()
    {
        // Проверяем настройки рендера для мобильных устройств
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            // Для мобильных устройств
            canvas.pixelPerfect = false;

            // Проверяем, что Canvas рендерится поверх всего
            canvas.sortingOrder = 100;

            // Проверяем, что нет других Canvas с таким же order
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (Canvas otherCanvas in allCanvases)
            {
                if (otherCanvas != canvas && otherCanvas.sortingOrder >= canvas.sortingOrder)
                {
                    otherCanvas.sortingOrder = canvas.sortingOrder - 1;
                }
            }
        }

        // Проверяем, что UI элементы на правильном слое
        Transform[] allUI = canvas.GetComponentsInChildren<Transform>(true);
        foreach (Transform uiElement in allUI)
        {
            if (uiElement.gameObject.layer != LayerMask.NameToLayer("UI"))
            {
                uiElement.gameObject.layer = LayerMask.NameToLayer("UI");
            }
        }
    }

    private void CreateNewCanvas()
    {
        if (logDebugInfo) Debug.Log("Создаем новый Canvas...");

        GameObject newCanvasObj = new GameObject("FixedCanvas");
        canvas = newCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            canvas.worldCamera = mainCamera;
        }

        canvas.planeDistance = 1;

        // Добавляем CanvasScaler
        CanvasScaler scaler = newCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Добавляем GraphicRaycaster
        raycaster = newCanvasObj.AddComponent<GraphicRaycaster>();

        if (logDebugInfo) Debug.Log("Создан новый Canvas");
    }

    // Метод для принудительного обновления
    public void ForceUpdateUI()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());

        if (logDebugInfo) Debug.Log("UI принудительно обновлен");
    }

    // Метод для проверки состояния UI
    public void CheckUIStatus()
    {
        StringBuilder status = new StringBuilder();
        status.AppendLine("=== UI Status Report ===");

        // Canvas
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            status.AppendLine($"Canvas: {canvas.name}");
            status.AppendLine($"- Enabled: {canvas.enabled}");
            status.AppendLine($"- Render Mode: {canvas.renderMode}");
            status.AppendLine($"- Camera: {(canvas.worldCamera != null ? canvas.worldCamera.name : "None")}");
        }

        // GraphicRaycaster
        GraphicRaycaster gr = GetComponent<GraphicRaycaster>();
        if (gr != null)
        {
            status.AppendLine($"GraphicRaycaster: Enabled={gr.enabled}");
        }

        // EventSystem
        EventSystem es = FindObjectOfType<EventSystem>();
        if (es != null)
        {
            status.AppendLine($"EventSystem: {es.name}");
            status.AppendLine($"- Enabled: {es.enabled}");

            // Input Module
            Component inputModule = es.GetComponent<InputSystemUIInputModule>();
            if (inputModule != null)
            {
                status.AppendLine($"- Input Module: InputSystemUIInputModule, Enabled={((Behaviour)inputModule).enabled}");
            }
            else
            {
                inputModule = es.GetComponent<StandaloneInputModule>();
                if (inputModule != null)
                {
                    status.AppendLine($"- Input Module: StandaloneInputModule, Enabled={((Behaviour)inputModule).enabled}");
                }
            }
        }

        // Buttons
        Button[] buttons = GetComponentsInChildren<Button>(true);
        status.AppendLine($"Buttons: {buttons.Length}");

        foreach (Button btn in buttons)
        {
            status.AppendLine($"- {btn.name}: Interactable={btn.interactable}, Enabled={btn.enabled}");
        }

        Debug.Log(status.ToString());
    }

    // Вызывается при включении объекта
    void OnEnable()
    {
        // Повторно включаем EnhancedTouch при активации
        if (useNewInputSystem)
        {
            try
            {
                EnhancedTouchSupport.Enable();
            }
            catch { }
        }
    }

    // Вызывается при выключении объекта
    void OnDisable()
    {
        if (useNewInputSystem && EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Disable();
        }
    }
}