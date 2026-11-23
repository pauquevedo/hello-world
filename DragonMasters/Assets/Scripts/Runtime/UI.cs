using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonMasters
{
    /// <summary>Ayudantes para construir la interfaz (uGUI) por código usando los botones del Brand 01.</summary>
    public static class UI
    {
        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                }
                return font;
            }
        }

        public static readonly Color Cream = new Color(1f, 0.97f, 0.88f);
        public static readonly Color Brown = new Color(0.35f, 0.2f, 0.1f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.2f);

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Img(Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, bool preserveAspect = true)
        {
            var rt = Rect(parent, sprite != null ? sprite.name : "img", anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = preserveAspect;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero && !preserveAspect)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1.6f; // esquinas algo más pequeñas
            }
            return img;
        }

        public static Image Img(Transform parent, string path, Vector2 anchor, Vector2 pos, Vector2 size, bool preserveAspect = true)
            => Img(parent, Art.Get(path), anchor, pos, size, preserveAspect);

        public static Image Panel(Transform parent, Color color)
        {
            var rt = Stretch(parent, "Dim");
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Art.White; img.color = color;
            img.raycastTarget = true; // bloquea toques al juego
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 box)
        {
            var rt = Rect(parent, "Text", anchor, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.25f, 0.12f, 0.05f, 1f);
            o.effectDistance = new Vector2(Mathf.Max(2, size / 14f), -Mathf.Max(2, size / 14f));
            return t;
        }

        public static Button Button(Transform parent, string spritePath, string label, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, int fontSize = 52)
        {
            var img = Img(parent, spritePath, anchor, pos, size, false);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { Sfx.Play("click"); onClick?.Invoke(); });
            img.gameObject.AddComponent<ButtonPop>();
            if (!string.IsNullOrEmpty(label))
            {
                var t = Label(img.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 6), size);
                t.name = "Label";
            }
            return b;
        }

        /// <summary>Barra de vida: fondo + barra blanca "retardada" + barra de color.</summary>
        public static HpBar HpBar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, bool fromRight)
        {
            var root = Rect(parent, "HpBar", anchor, pos, size);
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = Art.White; bg.color = new Color(0.2f, 0.1f, 0.05f, 0.85f); bg.raycastTarget = false;
            Func<string, Color, Image> Fill = (n, c) =>
            {
                var rt = Stretch(root, n);
                rt.offsetMin = new Vector2(6, 6); rt.offsetMax = new Vector2(-6, -6);
                var im = rt.gameObject.AddComponent<Image>();
                im.sprite = Art.White; im.color = c; im.type = Image.Type.Filled;
                im.fillMethod = Image.FillMethod.Horizontal;
                im.fillOrigin = fromRight ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
                im.raycastTarget = false;
                return im;
            };
            var bar = root.gameObject.AddComponent<HpBar>();
            bar.lag = Fill("Lag", new Color(1f, 0.95f, 0.8f));
            bar.fill = Fill("Fill", color);
            return bar;
        }
    }

    public class HpBar : MonoBehaviour
    {
        public Image fill, lag;
        float value = 1f, lagValue = 1f, hold;

        public void Set(float v)
        {
            value = Mathf.Clamp01(v);
            fill.fillAmount = value;
            hold = 0.4f;
        }

        void Update()
        {
            hold -= Time.deltaTime;
            if (hold <= 0) lagValue = Mathf.MoveTowards(lagValue, value, Time.deltaTime * 0.8f);
            lag.fillAmount = lagValue;
        }
    }

    /// <summary>Efecto de "pulsar" en los botones.</summary>
    public class ButtonPop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        float target = 1f, cur = 1f;
        public void OnPointerDown(PointerEventData e) => target = 0.9f;
        public void OnPointerUp(PointerEventData e) => target = 1f;
        public void OnPointerExit(PointerEventData e) => target = 1f;
        void Update()
        {
            cur = Mathf.Lerp(cur, target, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
            transform.localScale = Vector3.one * cur;
        }
    }

    /// <summary>Ajusta el contenido a la zona segura del iPhone (notch / Dynamic Island).</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        Vector2Int lastRes;
        public CanvasScaler scaler;

        void Update()
        {
            var sa = Screen.safeArea;
            var res = new Vector2Int(Screen.width, Screen.height);
            if (sa == last && res == lastRes) return;
            last = sa; lastRes = res;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (scaler != null)
                scaler.matchWidthOrHeight = (float)Screen.width / Screen.height < 1.7f ? 0f : 1f; // iPad: ajustar por ancho
        }
    }
}
