/*****************************************************************************
 // File Name:         BunnySceneBuilder.cs
 // Author:            Hongseok Kim (with OpenAI Codex assistance)
 // Creation Date:     Nov 1, 2026
 //
 // Brief Description: Creates the Bunny Jump scene and connects imported art,
 //                    sound, and UI.
 *****************************************************************************/
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Builds the assignment scene from the imported assets.
/// </summary>
public static class BunnySceneBuilder
{
    /// <summary>
    /// Creates a separate scene without replacing the original SampleScene.
    /// </summary>
    [MenuItem("Bunny Jump/Build Assignment Scene")]
    public static void BuildScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera),
            typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.65f, 0.84f, 0.95f);
        // Unlit sprites keep the imported colors visible without additional
        // lights.
        Material material = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/BunnyJump/SpriteMaterial.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(material,
                "Assets/BunnyJump/SpriteMaterial.mat");
        }

        for (int i = 1; i <= 4; i++)
        {
            SpriteRenderer background = MakeSprite("Background Layer " + i,
                "Assets/Sprites/Background/bg_layer" + i + ".png", material);
            background.transform.SetParent(camera.transform, false);
            background.transform.localPosition = new Vector3(0f, 0f, 15f +
                i * 0.1f);
            background.sortingOrder = -20 + i;
            Vector2 size = background.sprite.bounds.size;
            background.transform.localScale = new Vector3(17.8f / size.x,
                10f / size.y, 1f);
        }

        BunnyGameController controller = new GameObject(
            "GameController").AddComponent<BunnyGameController>();
        SpriteRenderer bunny = MakeSprite("Bunny",
            "Assets/Sprites/Players/bunny1_jump.png", material);
        bunny.transform.position = new Vector3(0f, -2.5f, 0f);
        bunny.sortingOrder = 5;
        // Give the bunny a consistent visible height regardless of source image
        // size.
        bunny.transform.localScale = Vector3.one * (
            0.85f / bunny.sprite.bounds.size.y);
        Rigidbody2D body = bunny.gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 2.5f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D playerCollider =
            bunny.gameObject.AddComponent<BoxCollider2D>();
        Vector2 bunnySize = bunny.sprite.bounds.size;
        playerCollider.size = new Vector2(bunnySize.x * 0.65f,
            bunnySize.y * 0.75f);
        playerCollider.offset = new Vector2(0f, -bunnySize.y * 0.1f);
        BunnyPlayer player = bunny.gameObject.AddComponent<BunnyPlayer>();
        SetReference(player, "controller", controller);
        SetReference(player, "gameCamera", camera);
        SetReference(player, "jumpSprite", LoadSprite(
            "Assets/Sprites/Players/bunny1_jump.png"));
        SetReference(player, "fallSprite", LoadSprite(
            "Assets/Sprites/Players/bunny1_ready.png"));
        SetReference(controller, "player", player);
        SetReference(controller, "gameCamera", camera);
        BunnyCamera follow = cameraObject.AddComponent<BunnyCamera>();
        SetReference(follow, "player", player.transform);
        SetReference(follow, "controller", controller);
        SpriteRenderer platform = MakeSprite("Grass Platform",
            "Assets/Sprites/Environment/ground_grass.png", material);
        Vector2 platformSize = platform.sprite.bounds.size;
        platform.transform.localScale = new Vector3(2.1f / platformSize.x,
            0.45f / platformSize.y, 1f);
        BoxCollider2D platformCollider =
            platform.gameObject.AddComponent<BoxCollider2D>();
        platformCollider.size = platformSize;
        platformCollider.usedByEffector = true;
        PlatformEffector2D effector =
            platform.gameObject.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 160f;
        platform.gameObject.AddComponent<BunnyPlatform>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(platform.gameObject,
            "Assets/BunnyJump/GrassPlatform.prefab");
        Object.DestroyImmediate(platform.gameObject);
        BunnyPlatformSpawner spawner = new GameObject(
            "PlatformSpawner").AddComponent<BunnyPlatformSpawner>();
        SetReference(spawner, "platformPrefab",
            prefab.GetComponent<BunnyPlatform>());
        SetReference(spawner, "gameCamera", camera);
        SetReference(spawner, "controller", controller);
        SetReference(controller, "effects",
            controller.gameObject.AddComponent<AudioSource>());
        controller.Effects.playOnAwake = false;
        SetReference(controller, "bounceClip",
            AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Music/Audio/footstep_grass_000.ogg"));
        SetReference(controller, "gameOverClip",
            AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/BunnyJump/GameOver.wav"));
        SetReference(controller, "music", new GameObject(
            "Background Music").AddComponent<AudioSource>());
        controller.Music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Music/Next to You.mp3");
        controller.Music.loop = true;
        controller.Music.volume = 0.25f;
        controller.Music.playOnAwake = true;
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode =
            RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform hud = Panel("HUD", canvasObject.transform, new Vector2(
            0.025f, 0.82f), new Vector2(0.28f, 0.965f), new Color(0.08f, 0.16f,
            0.22f, 0.88f));
        Text title = Label("Title", hud, "BUNNY JUMP", 27, new Vector2(0.06f,
            0.58f), new Vector2(0.94f, 0.94f));
        title.color = new Color(1f, 0.84f, 0.4f);
        SetReference(controller, "scoreText", Label("Score", hud, "HEIGHT  0",
            40, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.58f)));
        Text help = Label("Controls", canvasObject.transform,
            "A / D or Arrow Keys    MOVE     |     R" +
            "  RESTART     |     ESC  QUIT", 23, new Vector2(0.12f, 0.015f),
            new Vector2(0.88f, 0.065f));
        help.alignment = TextAnchor.MiddleCenter;
        help.color = new Color(0.1f, 0.18f, 0.22f);
        RectTransform overlay = Panel("Game Over", canvasObject.transform,
            Vector2.zero, Vector2.one, new Color(0.04f, 0.09f, 0.14f, 0.85f));
        Text gameOver = Label("Heading", overlay, "GAME OVER", 80, new Vector2(
            0.2f, 0.56f), new Vector2(0.8f, 0.72f));
        gameOver.alignment = TextAnchor.MiddleCenter;
        SetReference(controller, "finalScoreText", Label("Final Score", overlay,
            "HEIGHT  0", 45, new Vector2(0.2f, 0.43f), new Vector2(0.8f,
            0.55f)));
        controller.FinalScoreText.alignment = TextAnchor.MiddleCenter;
        RectTransform buttonRect = Panel("Restart Button", overlay, new Vector2(
            0.38f, 0.28f), new Vector2(0.62f, 0.38f), new Color(0.96f, 0.73f,
            0.24f));
        Button button = buttonRect.gameObject.AddComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,
            controller.RestartGame);
        Text buttonText = Label("Label", buttonRect, "PLAY AGAIN  [R]", 30,
            Vector2.zero, Vector2.one);
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = new Color(0.08f, 0.16f, 0.22f);
        SetReference(controller, "gameOverPanel", overlay.gameObject);
        overlay.gameObject.SetActive(false);
        new GameObject("EventSystem", typeof(EventSystem), typeof(
            InputSystemUIInputModule));
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.productName = "Bunny Jump";
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/BunnyJump.unity");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/BunnyJump.unity", true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("Bunny Jump scene created. Press Play to start.");
    }

    /// <summary>
    /// Connects a private Inspector reference during scene creation.
    /// </summary>
    /// <param name="target">Component that owns the serialized field.</param>
    /// <param name="fieldName">Name of the existing serialized field.</param>
    /// <param name="value">Asset or scene object to connect.</param>
    private static void SetReference(Object target, string fieldName,
        Object value)
    {
        SerializedObject serializedTarget = new SerializedObject(target);
        serializedTarget.FindProperty(fieldName).objectReferenceValue = value;
        serializedTarget.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Loads a sprite from an imported single or multiple sprite texture.
    /// </summary>
    /// <param name="path">Asset path of the texture.</param>
    /// <returns>The first imported sprite.</returns>
    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
    }

    /// <summary>
    /// Creates an unlit sprite object from an existing asset.
    /// </summary>
    /// <param name="name">Object name.</param>
    /// <param name="path">Texture asset path.</param>
    /// <param name="material">Shared sprite material.</param>
    /// <returns>The new sprite renderer.</returns>
    private static SpriteRenderer MakeSprite(string name, string path,
        Material material)
    {
        SpriteRenderer renderer = new GameObject(
            name).AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(path);
        renderer.sharedMaterial = material;
        return renderer;
    }

    /// <summary>
    /// Creates a colored UI panel using relative screen anchors.
    /// </summary>
    /// <param name="name">Object name.</param>
    /// <param name="parent">UI parent.</param>
    /// <param name="minimum">Lower anchor.</param>
    /// <param name="maximum">Upper anchor.</param>
    /// <param name="color">Panel color.</param>
    /// <returns>The panel rectangle.</returns>
    private static RectTransform Panel(string name, Transform parent,
        Vector2 minimum, Vector2 maximum, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(
            Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = minimum;
        rect.anchorMax = maximum;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = color;
        return rect;
    }

    /// <summary>
    /// Creates a readable UI text label.
    /// </summary>
    /// <param name="name">Object name.</param>
    /// <param name="parent">UI parent.</param>
    /// <param name="content">Displayed text.</param>
    /// <param name="fontSize">Font size at 1920 by 1080.</param>
    /// <param name="minimum">Lower anchor.</param>
    /// <param name="maximum">Upper anchor.</param>
    /// <returns>The new text component.</returns>
    private static Text Label(string name, Transform parent, string content,
        int fontSize, Vector2 minimum, Vector2 maximum)
    {
        GameObject label = new GameObject(name, typeof(RectTransform), typeof(
            Text));
        label.transform.SetParent(parent, false);
        Text text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.text = content;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = minimum;
        rect.anchorMax = maximum;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }
}
