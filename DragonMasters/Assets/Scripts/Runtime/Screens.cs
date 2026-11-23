using UnityEngine;
using UnityEngine.UI;

namespace DragonMasters
{
    /// <summary>Pantallas de interfaz: menú principal y selección de dragón.</summary>
    public static class Screens
    {
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public static GameObject Menu(RectTransform parent)
        {
            var root = UI.Stretch(parent, "MenuScreen");

            // Título
            var title = UI.Label(root, "DRAGON MASTERS", 150, UI.Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1600, 200));
            title.gameObject.AddComponent<Pulse>();
            UI.Label(root, "¡Lanza, acierta y derrota a todos los monstruos!", 46, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -270), new Vector2(1600, 70));

            // Mascota
            var mascot = UI.Img(root, "dragon_mascot/mascot", new Vector2(0, 0), new Vector2(300, 330), new Vector2(480, 580));
            mascot.gameObject.AddComponent<Pulse>().amount = 0.025f;

            // Botones
            int lvl = Save.Level;
            var e = Catalog.Enemies[lvl % Catalog.Enemies.Length];
            UI.Button(root, "ui_buttons/btn_orange", "JUGAR", Center, new Vector2(-180, -10), new Vector2(560, 170), () => GameManager.I.StartBattle(Save.Level), 78);
            UI.Label(root, $"Nivel {lvl + 1}  ·  vs {e.name}", 40, Color.white, TextAnchor.MiddleCenter, Center, new Vector2(-180, -130), new Vector2(700, 60));
            UI.Button(root, "ui_buttons/btn_teal", "DRAGONES", Center, new Vector2(-180, -260), new Vector2(480, 140), () => GameManager.I.ShowSelect(), 60);

            // Monedas y sonido
            CoinPill(root);
            var snd = UI.Button(root, "ui_buttons/icon_sound", null, new Vector2(1, 1), new Vector2(-90, -90), new Vector2(120, 120), null);
            var sndImg = snd.GetComponent<Image>();
            sndImg.color = Save.Muted ? new Color(1, 1, 1, 0.4f) : Color.white;
            snd.onClick.AddListener(() => { Save.Muted = !Save.Muted; sndImg.color = Save.Muted ? new Color(1, 1, 1, 0.4f) : Color.white; });

            if (Save.Level > 0)
            {
                UI.Button(root, "ui_buttons/icon_settings", null, new Vector2(1, 1), new Vector2(-230, -90), new Vector2(120, 120), () =>
                {
                    Save.Level = 0; GameManager.I.ShowMenu();
                });
                UI.Label(root, "reiniciar\nniveles", 22, Color.white, TextAnchor.MiddleCenter, new Vector2(1, 1), new Vector2(-230, -175), new Vector2(160, 60));
            }
            return root.gameObject;
        }

        static void CoinPill(RectTransform root)
        {
            var bg = UI.Img(root, "ui_buttons/btn_purple", new Vector2(1, 1), new Vector2(-470, -90), new Vector2(300, 110), false);
            UI.Img(bg.transform, "items_collectibles/coin", new Vector2(0, 0.5f), new Vector2(55, 4), new Vector2(80, 80));
            UI.Label(bg.transform, Save.Coins.ToString(), 50, Color.white, TextAnchor.MiddleCenter, Center, new Vector2(30, 4), new Vector2(200, 80));
        }

        public static GameObject CharacterSelect(RectTransform parent)
        {
            var dim = UI.Panel(parent, new Color(0.08f, 0.05f, 0.15f, 0.82f));
            var root = (RectTransform)dim.transform;
            root.name = "CharacterSelect";

            UI.Label(root, "ELIGE TU DRAGÓN", 100, UI.Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(1400, 140));
            UI.Button(root, "ui_buttons/icon_home", null, new Vector2(0, 1), new Vector2(100, -95), new Vector2(130, 130), () => GameManager.I.ShowMenu());
            CoinPill(root);

            var toast = UI.Label(root, "", 46, new Color(1f, 0.6f, 0.5f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(1400, 70));

            var heroes = Catalog.Heroes;
            const int cols = 9;
            Vector2 card = new Vector2(196, 270);
            float gap = 14;
            float totalW = cols * card.x + (cols - 1) * gap;
            for (int i = 0; i < heroes.Length; i++)
            {
                var h = heroes[i];
                int r = i / cols, c = i % cols;
                var pos = new Vector2(-totalW / 2 + card.x / 2 + c * (card.x + gap), 120 - r * (card.y + 40));
                bool owned = Save.Owns(h);
                bool selected = Save.Hero == h.id;

                var b = UI.Button(root, "ui_buttons/panel_frame", null, Center, pos, card, null);
                var img = b.GetComponent<Image>();
                if (selected) img.color = new Color(1f, 0.9f, 0.4f);
                var face = UI.Img(b.transform, h.sprite, Center, new Vector2(0, 30), new Vector2(160, 160));
                if (!owned) face.color = new Color(0.25f, 0.25f, 0.3f, 1f);
                UI.Label(b.transform, h.name, 28, Color.white, TextAnchor.MiddleCenter, Center, new Vector2(0, -78), new Vector2(190, 40));
                var weapon = Catalog.Weapon(h.weapon);
                UI.Img(b.transform, weapon.sprite, new Vector2(1, 1), new Vector2(-28, -28), new Vector2(56, 56));

                if (selected)
                    UI.Label(b.transform, "ELEGIDO", 26, UI.Gold, TextAnchor.MiddleCenter, Center, new Vector2(0, -112), new Vector2(190, 36));
                else if (!owned)
                {
                    UI.Img(b.transform, "items_collectibles/coin", Center, new Vector2(-45, -112), new Vector2(38, 38));
                    UI.Label(b.transform, h.price.ToString(), 30, Save.Coins >= h.price ? Color.white : new Color(1f, 0.55f, 0.5f),
                        TextAnchor.MiddleLeft, Center, new Vector2(30, -112), new Vector2(120, 36));
                    UI.Img(b.transform, "items_collectibles/key", Center, new Vector2(0, 40), new Vector2(80, 80));
                }

                b.onClick.AddListener(() =>
                {
                    if (Save.Owns(h))
                    {
                        Save.Hero = h.id;
                        GameManager.I.ShowSelect();
                    }
                    else if (Save.Coins >= h.price)
                    {
                        Save.Coins -= h.price;
                        Save.Unlock(h);
                        Save.Hero = h.id;
                        Sfx.Play("buy");
                        GameManager.I.ShowSelect();
                    }
                    else
                    {
                        toast.text = $"¡Te faltan {h.price - Save.Coins} monedas para {h.name}! Gana combates para conseguirlas.";
                    }
                });
            }

            var cur = Catalog.Hero(Save.Hero);
            var cw = Catalog.Weapon(cur.weapon);
            UI.Label(root, $"{cur.name}  ·  Arma:", 48, Color.white, TextAnchor.MiddleRight, new Vector2(0.5f, 0), new Vector2(-80, 180), new Vector2(800, 70));
            UI.Img(root, cw.sprite, new Vector2(0.5f, 0), new Vector2(380, 180), new Vector2(100, 100));
            return root.gameObject;
        }
    }

    /// <summary>Pantalla de fin de combate.</summary>
    public static class Results
    {
        public static void Show(Transform parent, bool win, int coins, int headshots, int level)
        {
            var dim = UI.Panel(parent, new Color(0, 0, 0, 0.55f));
            var root = dim.transform;
            var c = new Vector2(0.5f, 0.5f);
            var frame = UI.Img(root, "ui_buttons/panel_frame", c, Vector2.zero, new Vector2(1150, 760), false);
            frame.gameObject.AddComponent<Pulse>().amount = 0.008f;

            UI.Label(root, win ? "¡VICTORIA!" : "DERROTA", 120, win ? UI.Gold : new Color(1f, 0.55f, 0.5f), TextAnchor.MiddleCenter, c, new Vector2(0, 270), new Vector2(1000, 160));
            var face = UI.Img(root, win ? "dragon_expressions/celebrating" : "dragon_expressions/sad", c, new Vector2(-330, 10), new Vector2(360, 360));
            face.gameObject.AddComponent<Pulse>().amount = 0.04f;

            UI.Img(root, "items_collectibles/coin", c, new Vector2(-20, 90), new Vector2(90, 90));
            UI.Label(root, $"+{coins}", 80, UI.Gold, TextAnchor.MiddleLeft, c, new Vector2(170, 90), new Vector2(260, 100));
            UI.Img(root, "effects_vfx/star_burst", c, new Vector2(-20, -40), new Vector2(80, 80));
            UI.Label(root, $"Headshots: {headshots}", 54, Color.white, TextAnchor.MiddleLeft, c, new Vector2(200, -40), new Vector2(360, 80));
            if (win)
                UI.Img(root, "items_collectibles/chest", c, new Vector2(380, 40), new Vector2(200, 200)).gameObject.AddComponent<Pulse>().amount = 0.05f;
            else
                UI.Img(root, "dragon_expressions/angry", c, new Vector2(380, 40), new Vector2(170, 170));

            if (win)
            {
                bool lastOfRound = (level + 1) % Catalog.Enemies.Length == 0;
                if (lastOfRound)
                    UI.Label(root, "¡Has vencido a todos! Siguiente ronda: más difícil", 36, Color.white, TextAnchor.MiddleCenter, c, new Vector2(0, -140), new Vector2(1000, 50));
                UI.Button(root, "ui_buttons/btn_orange", "SIGUIENTE", c, new Vector2(190, -255), new Vector2(440, 140), () => GameManager.I.StartBattle(level + 1), 56);
            }
            else
                UI.Button(root, "ui_buttons/btn_orange", "REINTENTAR", c, new Vector2(190, -255), new Vector2(440, 140), () => GameManager.I.StartBattle(level), 56);
            UI.Button(root, "ui_buttons/btn_teal", "MENÚ", c, new Vector2(-260, -255), new Vector2(340, 140), () => GameManager.I.ShowMenu(), 56);
        }
    }

    /// <summary>Latido suave de escala para elementos de interfaz.</summary>
    public class Pulse : MonoBehaviour
    {
        public float amount = 0.03f, speed = 2.5f;
        void Update() => transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * speed) * amount);
    }
}
