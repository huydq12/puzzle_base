#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupUIManagerForGameScene
{
    private const string ScenePath = "Assets/Scenes/_Game/Scene/Game.unity";

    [MenuItem("Tools/UI/Setup UIManager In Game Scene")]
    public static void Setup()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Canvas canvas = FindMainCanvas();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(canvas.gameObject);

        UIManager manager = canvas.GetComponent<UIManager>();
        if (manager == null)
        {
            manager = Undo.AddComponent<UIManager>(canvas.gameObject);
        }

        manager.cScreen = GetOrCreateContainer(canvas.transform, "Screen");
        manager.cPopup = GetOrCreateContainer(canvas.transform, "Popup");
        manager.cNotify = GetOrCreateContainer(canvas.transform, "Notify");
        manager.cOverlap = GetOrCreateContainer(canvas.transform, "Overlay");

        manager.cScreen.SetSiblingIndex(0);
        manager.cPopup.SetSiblingIndex(1);
        manager.cNotify.SetSiblingIndex(2);
        manager.cOverlap.SetSiblingIndex(3);

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupUIManagerForGameScene] UIManager setup complete in Game scene.");
    }

    private static Canvas FindMainCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas fallback = null;

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null) continue;
            if (!canvas.gameObject.scene.IsValid()) continue;
            if (fallback == null) fallback = canvas;

            bool isRootCanvas = canvas.transform.parent == null || canvas.transform.GetComponentInParent<Canvas>() == canvas;
            if (isRootCanvas && canvas.gameObject.name == "Canvas")
            {
                return canvas;
            }
        }

        return fallback;
    }

    private static Transform GetOrCreateContainer(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Stretch(existing as RectTransform);
            return existing;
        }

        GameObject container = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(container, $"Create {name}");
        Transform transform = container.transform;
        transform.SetParent(parent, false);
        Stretch(container.GetComponent<RectTransform>());
        return transform;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        if (rectTransform == null) return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localPosition = Vector3.zero;
    }
}
#endif
