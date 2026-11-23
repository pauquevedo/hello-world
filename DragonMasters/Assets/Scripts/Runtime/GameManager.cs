using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonMasters
{
    /// <summary>
    /// Punto de entrada. Crea cámara, interfaz y cambia entre pantallas (menú, selección de dragón, combate).
    /// Se crea solo al darle a Play aunque la escena esté vacía.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public CameraRig Rig { get; private set; }
        public Canvas Canvas { get; private set; }
        public RectTransform UiRoot { get; private set; }

        GameObject world, screen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureExists()
        {
            if (FindAnyObjectByType<GameManager>() == null)
                new GameObject("DragonMasters").AddComponent<GameManager>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = false;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            Sfx.Init(gameObject);
            AudioListener.volume = Save.Muted ? 0 : 1;

            // Cámara
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.8f, 0.95f);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 100f;
            cam.transform.position = new Vector3(0, 3, -10);
            DontDestroyOnLoad(cam.gameObject);
            Rig = cam.GetComponent<CameraRig>();
            if (Rig == null) Rig = cam.gameObject.AddComponent<CameraRig>();

            // Interfaz
            var cgo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(cgo);
            Canvas = cgo.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            UiRoot = UI.Stretch(cgo.transform, "SafeArea");
            UiRoot.gameObject.AddComponent<SafeArea>().scaler = scaler;

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(es);
            }
        }

        void Start() => ShowMenu();

        public void RegisterScreen(GameObject go)
        {
            if (screen != null && screen != go) Destroy(screen);
            screen = go;
        }

        void Clear()
        {
            Time.timeScale = 1;
            if (world != null) Destroy(world);
            if (screen != null) Destroy(screen);
            world = screen = null;
            Rig.StopFollow();
        }

        public void ShowMenu()
        {
            Clear();
            world = new GameObject("MenuWorld");
            int lvl = Save.Level % Catalog.Enemies.Length;
            Scenery.Build(world.transform, Catalog.Enemies[lvl].theme, -12, 12, 99);
            // el dragón elegido y el próximo rival posan en el escenario
            var hero = Catalog.Hero(Save.Hero);
            var h = Art.Make("Hero", hero.sprite, world.transform, new Vector3(4f, 0, 0), 2.6f, Scenery.OrderFighters);
            h.gameObject.AddComponent<Bob>().amp = 0.05f;
            var e = Catalog.Enemies[lvl];
            var en = Art.Make("Rival", e.sprite, world.transform, new Vector3(7.6f, e.hover, 0), e.height * 0.9f, Scenery.OrderFighters);
            en.flipX = true;
            en.gameObject.AddComponent<Bob>().amp = 0.05f;
            Rig.SetImmediate(new Vector2(1f, 3.2f), 5.2f);
            RegisterScreen(Screens.Menu(UiRoot));
        }

        public void ShowSelect()
        {
            if (screen != null) Destroy(screen);
            RegisterScreen(Screens.CharacterSelect(UiRoot));
        }

        public void StartBattle(int level)
        {
            Clear();
            world = new GameObject("Battle");
            var b = world.AddComponent<Battle>();
            b.Init(level);
        }
    }
}
