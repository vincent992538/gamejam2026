using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using HorseBetting.Core;
using HorseBetting.Config;
using HorseBetting.UI;

namespace HorseBetting.Editor
{
    /// <summary>
    /// Editor script to automatically set up the game scene with all required GameObjects,
    /// components, and references. Run from menu: HorseBetting > Setup Scene.
    /// </summary>
    public static class SceneSetup
    {
        [MenuItem("HorseBetting/Setup Scene")]
        public static void SetupScene()
        {
            // ─── 0. Create PanelSettings if it doesn't exist ────────────────────
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                AssetDatabase.CreateAsset(panelSettings, "Assets/UI/PanelSettings.asset");
                AssetDatabase.SaveAssets();
                Debug.Log("[SceneSetup] Created PanelSettings asset at Assets/UI/PanelSettings.asset");
            }

            // ─── 1. GameEngine ──────────────────────────────────────────────────
            var gameEngineObj = new GameObject("GameEngine");
            var gameEngine = gameEngineObj.AddComponent<GameEngine>();

            // Load and assign all 8 ScriptableObject configs
            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/Config/GameConfig.asset");
            var oddsConfig = AssetDatabase.LoadAssetAtPath<OddsConfig>("Assets/Resources/Config/OddsConfig.asset");
            var messageCardConfig = AssetDatabase.LoadAssetAtPath<MessageCardConfig>("Assets/Resources/Config/MessageCardConfig.asset");
            var trackConfig = AssetDatabase.LoadAssetAtPath<TrackConfig>("Assets/Resources/Config/TrackConfig.asset");
            var analystConfig = AssetDatabase.LoadAssetAtPath<AnalystConfig>("Assets/Resources/Config/AnalystConfig.asset");
            var eventConfig = AssetDatabase.LoadAssetAtPath<EventConfig>("Assets/Resources/Config/EventConfig.asset");
            var shopConfig = AssetDatabase.LoadAssetAtPath<ShopConfig>("Assets/Resources/Config/ShopConfig.asset");
            var bettingConfig = AssetDatabase.LoadAssetAtPath<BettingConfig>("Assets/Resources/Config/BettingConfig.asset");

            // Use SerializedObject to set private [SerializeField] fields
            var so = new SerializedObject(gameEngine);
            so.FindProperty("gameConfig").objectReferenceValue = gameConfig;
            so.FindProperty("oddsConfig").objectReferenceValue = oddsConfig;
            so.FindProperty("messageCardConfig").objectReferenceValue = messageCardConfig;
            so.FindProperty("trackConfig").objectReferenceValue = trackConfig;
            so.FindProperty("analystConfig").objectReferenceValue = analystConfig;
            so.FindProperty("eventConfig").objectReferenceValue = eventConfig;
            so.FindProperty("shopConfig").objectReferenceValue = shopConfig;
            so.FindProperty("bettingConfig").objectReferenceValue = bettingConfig;
            so.ApplyModifiedProperties();

            // ─── 2. UI Documents (with PanelSettings) ───────────────────────────
            var mainUIObj = CreateUIDocument("MainUI", "Assets/UI/MainView.uxml", panelSettings);
            var bettingUIObj = CreateUIDocument("BettingUI", "Assets/UI/BettingView.uxml", panelSettings);
            var settlementUIObj = CreateUIDocument("SettlementUI", "Assets/UI/SettlementView.uxml", panelSettings);
            var shopUIObj = CreateUIDocument("ShopUI", "Assets/UI/ShopView.uxml", panelSettings);
            var analystUIObj = CreateUIDocument("AnalystUI", "Assets/UI/AnalystView.uxml", panelSettings);

            // ─── 3. RaceScene ───────────────────────────────────────────────────
            var raceSceneObj = new GameObject("RaceScene");
            var raceView = raceSceneObj.AddComponent<RaceView>();

            // Add a background SpriteRenderer
            var bgObj = new GameObject("TrackBackground");
            bgObj.transform.SetParent(raceSceneObj.transform);
            bgObj.transform.localPosition = Vector3.zero;
            var bgSprite = bgObj.AddComponent<SpriteRenderer>();
            bgSprite.color = new Color(0.3f, 0.6f, 0.2f, 1f); // default grass color
            bgSprite.sortingOrder = -10;

            // Create a simple white texture for the background
            var bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, Color.white);
            bgTexture.Apply();
            bgSprite.sprite = Sprite.Create(bgTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            bgObj.transform.localScale = new Vector3(16f, 12f, 1f);

            // Assign track background to RaceView
            var raceViewSO = new SerializedObject(raceView);
            raceViewSO.FindProperty("_trackBackground").objectReferenceValue = bgSprite;
            raceViewSO.ApplyModifiedProperties();

            // ─── 4. UIManager ───────────────────────────────────────────────────
            var uiManagerObj = new GameObject("UIManager");
            var uiManager = uiManagerObj.AddComponent<UIManager>();

            var uiManagerSO = new SerializedObject(uiManager);
            uiManagerSO.FindProperty("_gameEngine").objectReferenceValue = gameEngine;
            uiManagerSO.FindProperty("_mainUIDocument").objectReferenceValue = mainUIObj.GetComponent<UIDocument>();
            uiManagerSO.FindProperty("_bettingUIDocument").objectReferenceValue = bettingUIObj.GetComponent<UIDocument>();
            uiManagerSO.FindProperty("_settlementUIDocument").objectReferenceValue = settlementUIObj.GetComponent<UIDocument>();
            uiManagerSO.FindProperty("_shopUIDocument").objectReferenceValue = shopUIObj.GetComponent<UIDocument>();
            uiManagerSO.FindProperty("_analystUIDocument").objectReferenceValue = analystUIObj.GetComponent<UIDocument>();
            uiManagerSO.FindProperty("_raceView").objectReferenceValue = raceView;
            uiManagerSO.ApplyModifiedProperties();

            // ─── 5. GameFlowController ──────────────────────────────────────────
            var flowControllerObj = new GameObject("GameFlowController");
            var flowController = flowControllerObj.AddComponent<GameFlowController>();

            var flowSO = new SerializedObject(flowController);
            flowSO.FindProperty("_gameEngine").objectReferenceValue = gameEngine;
            flowSO.FindProperty("_raceView").objectReferenceValue = raceView;
            flowSO.ApplyModifiedProperties();

            // ─── 6. GameBootstrap ───────────────────────────────────────────────
            var bootstrapObj = new GameObject("GameBootstrap");
            var bootstrap = bootstrapObj.AddComponent<GameBootstrap>();

            var bootstrapSO = new SerializedObject(bootstrap);
            bootstrapSO.FindProperty("_gameEngine").objectReferenceValue = gameEngine;
            bootstrapSO.FindProperty("_flowController").objectReferenceValue = flowController;
            bootstrapSO.FindProperty("_uiManager").objectReferenceValue = uiManager;
            bootstrapSO.FindProperty("_mainUIDocument").objectReferenceValue = mainUIObj.GetComponent<UIDocument>();
            bootstrapSO.FindProperty("_bettingUIDocument").objectReferenceValue = bettingUIObj.GetComponent<UIDocument>();
            bootstrapSO.FindProperty("_settlementUIDocument").objectReferenceValue = settlementUIObj.GetComponent<UIDocument>();
            bootstrapSO.FindProperty("_shopUIDocument").objectReferenceValue = shopUIObj.GetComponent<UIDocument>();
            bootstrapSO.FindProperty("_analystUIDocument").objectReferenceValue = analystUIObj.GetComponent<UIDocument>();
            bootstrapSO.ApplyModifiedProperties();

            // ─── 7. Camera Setup ────────────────────────────────────────────────
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 7f;
                mainCamera.transform.position = new Vector3(0f, -3f, -10f);
                mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            }

            // ─── Done ───────────────────────────────────────────────────────────
            Debug.Log("[SceneSetup] Scene setup complete! All GameObjects created and wired.");
            EditorUtility.DisplayDialog("Scene Setup Complete",
                "All game objects have been created:\n" +
                "• GameEngine (with configs)\n" +
                "• 5 UI Documents (with PanelSettings)\n" +
                "• RaceScene (with background)\n" +
                "• UIManager (wired)\n" +
                "• GameFlowController (wired)\n" +
                "• GameBootstrap (wired)\n" +
                "• Camera (orthographic, size 7)\n\n" +
                "Press Play to start the game!",
                "OK");
        }

        private static GameObject CreateUIDocument(string name, string uxmlPath, PanelSettings panelSettings)
        {
            var obj = new GameObject(name);
            var uiDoc = obj.AddComponent<UIDocument>();

            // Assign PanelSettings (required for rendering)
            uiDoc.panelSettings = panelSettings;

            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (uxmlAsset != null)
            {
                uiDoc.visualTreeAsset = uxmlAsset;
            }
            else
            {
                Debug.LogWarning($"[SceneSetup] Could not find UXML at path: {uxmlPath}");
            }

            return obj;
        }
    }
}
