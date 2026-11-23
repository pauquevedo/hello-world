using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace DragonRunner
{
    /// <summary>Interfaz con uGUI construida por código usando los botones de Brand 01.</summary>
    public class GameUI : MonoBehaviour
    {
        public Action OnPlay, OnPause, OnResume, OnHome;

        static readonly Color Cream = Art.Hex("#FFF6E5");
        static readonly Color Brown = Art.Hex("#5A2E12");
        static readonly Color Orange = Art.Hex("#F28A1E");

        Font font;
        RectTransform root, title, hud, pause, gameOver;
        Text distanceText, coinText, overScore, overBest, overDetail, titleBest;
        Image[] hearts;
        RectTransform overDragon;

        public void Build()
        {
            font = LoadFont();
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            root = canvasGo.GetComponent<RectTransform>();

            BuildTitle();
            BuildHud();
            BuildPause();
            BuildGameOver();
        }

        static Font LoadFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            return f;
        }

        // ---------- Pantallas ----------

        void BuildTitle()
        {
            title = Panel("Title", new Color(0f, 0f, 0f, 0f));
            var dragon = Img(title, "dragon/dragon_mascot", new Vector2(0.5f, 0.5f), new Vector2(-430, -20), 620);
            dragon.gameObject.AddComponent<UiBob>();
            Label(title, "DRAGON\nRUNNER", 150, new Vector2(0.5f, 0.5f), new Vector2(330, 210), Orange, true);
            Label(title, "Toca para saltar · toca otra vez para aletear · mantén para planear", 34,
                  new Vector2(0.5f, 0.5f), new Vector2(330, -10), Cream, true, 900);
            var play = IconButton(title, "ui_buttons/play", new Vector2(0.5f, 0.5f), new Vector2(330, -200), 230, () => OnPlay?.Invoke());
            play.gameObject.AddComponent<UiPulse>();
            titleBest = Label(title, "", 44, new Vector2(0.5f, 0.5f), new Vector2(330, -360), Cream, true, 900);
        }

        void BuildHud()
        {
            hud = Panel("HUD", new Color(0f, 0f, 0f, 0f));
            Img(hud, "items_collectibles/coin", new Vector2(0f, 1f), new Vector2(90, -80), 90);
            coinText = Label(hud, "0", 64, new Vector2(0f, 1f), new Vector2(290, -80), Cream, true, 300);
            coinText.alignment = TextAnchor.MiddleLeft;
            distanceText = Label(hud, "0 m", 64, new Vector2(0.5f, 1f), new Vector2(0, -80), Cream, true, 500);
            hearts = new Image[3];
            for (int i = 0; i < 3; i++)
                hearts[i] = Img(hud, "items_collectibles/heart", new Vector2(0f, 1f), new Vector2(90 + i * 95, -185), 80);
            IconButton(hud, "ui_buttons/pause", new Vector2(1f, 1f), new Vector2(-100, -95), 140, () => OnPause?.Invoke());
        }

        void BuildPause()
        {
            pause = Panel("Pause", new Color(0.1f, 0.05f, 0.2f, 0.55f));
            Img(pause, "ui_buttons/panel", new Vector2(0.5f, 0.5f), new Vector2(0, 0), 620, 760);
            Img(pause, "dragon_expressions/sleep", new Vector2(0.5f, 0.5f), new Vector2(0, 110), 230);
            Label(pause, "PAUSA", 90, new Vector2(0.5f, 0.5f), new Vector2(0, 290), Brown, false);
            IconButton(pause, "ui_buttons/play", new Vector2(0.5f, 0.5f), new Vector2(-110, -150), 170, () => OnResume?.Invoke());
            IconButton(pause, "ui_buttons/home", new Vector2(0.5f, 0.5f), new Vector2(110, -150), 170, () => OnHome?.Invoke());
        }

        void BuildGameOver()
        {
            gameOver = Panel("GameOver", new Color(0.1f, 0.05f, 0.2f, 0.55f));
            Img(gameOver, "ui_buttons/panel", new Vector2(0.5f, 0.5f), new Vector2(0, 0), 760, 900);
            overDragon = Img(gameOver, "dragon_expressions/sad", new Vector2(0.5f, 0.5f), new Vector2(0, 180), 260).rectTransform;
            Label(gameOver, "¡OH, NO!", 80, new Vector2(0.5f, 0.5f), new Vector2(0, 365), Brown, false);
            overScore = Label(gameOver, "", 76, new Vector2(0.5f, 0.5f), new Vector2(0, 10), Orange, false, 700);
            overDetail = Label(gameOver, "", 38, new Vector2(0.5f, 0.5f), new Vector2(0, -60), Brown, false, 700);
            overBest = Label(gameOver, "", 38, new Vector2(0.5f, 0.5f), new Vector2(0, -115), Brown, false, 700);
            IconButton(gameOver, "ui_buttons/play", new Vector2(0.5f, 0.5f), new Vector2(-120, -270), 180, () => OnPlay?.Invoke());
            IconButton(gameOver, "ui_buttons/home", new Vector2(0.5f, 0.5f), new Vector2(120, -270), 180, () => OnHome?.Invoke());
        }

        // ---------- API para el GameManager ----------

        public void ShowTitle(int best)
        {
            titleBest.text = best > 0 ? $"Récord: {best}" : "";
            Show(title);
        }

        public void ShowHud() => Show(hud);

        public void ShowPause(bool on)
        {
            pause.gameObject.SetActive(on);
        }

        public void ShowGameOver(int score, int best, int meters, int coins, bool newBest)
        {
            overScore.text = $"{score} puntos";
            overDetail.text = $"{meters} m · {coins} monedas";
            overBest.text = newBest ? "¡NUEVO RÉCORD!" : $"Récord: {best}";
            overDragon.GetComponent<Image>().sprite = Art.Get(newBest ? "dragon_expressions/celebrate" : "dragon_expressions/sad");
            Show(gameOver);
            hud.gameObject.SetActive(true);
        }

        public void SetHud(int meters, int coins, int lives)
        {
            distanceText.text = meters + " m";
            coinText.text = coins.ToString();
            for (int i = 0; i < hearts.Length; i++)
                hearts[i].color = i < lives ? Color.white : new Color(0.2f, 0.2f, 0.2f, 0.45f);
        }

        void Show(RectTransform which)
        {
            foreach (var p in new[] { title, hud, pause, gameOver })
                p.gameObject.SetActive(p == which);
        }

        // ---------- Utilidades de construcción ----------

        RectTransform Panel(string name, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = bg;
            img.raycastTarget = bg.a > 0f;
            return rt;
        }

        Image Img(RectTransform parent, string sprite, Vector2 anchor, Vector2 pos, float height, float width = -1f)
        {
            var go = new GameObject(sprite, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.sprite = Art.Get(sprite);
            img.raycastTarget = false;
            var s = img.sprite.rect.size;
            if (width < 0f) width = height * s.x / s.y;
            else img.type = Image.Type.Simple;
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = pos;
            return img;
        }

        Button IconButton(RectTransform parent, string sprite, Vector2 anchor, Vector2 pos, float size, Action onClick)
        {
            var img = Img(parent, sprite, anchor, pos, size);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            b.colors = colors;
            b.onClick.AddListener(() => { GameManager.Instance.Sfx("click"); onClick(); });
            return b;
        }

        Text Label(RectTransform parent, string text, int size, Vector2 anchor, Vector2 pos, Color color, bool outline, float width = 800f)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(width, size * 2.6f);
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline)
            {
                var o = go.AddComponent<Outline>();
                o.effectColor = Brown;
                o.effectDistance = new Vector2(4, -4);
            }
            return t;
        }
    }

    /// <summary>Flotación suave para el dragón de la portada.</summary>
    public class UiBob : MonoBehaviour
    {
        Vector2 start;
        RectTransform rt;
        void Awake() { rt = (RectTransform)transform; start = rt.anchoredPosition; }
        void Update() => rt.anchoredPosition = start + Vector2.up * Mathf.Sin(Time.unscaledTime * 2f) * 18f;
    }

    /// <summary>Latido para el botón de jugar.</summary>
    public class UiPulse : MonoBehaviour
    {
        void Update() => transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.05f);
    }
}
