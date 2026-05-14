using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Spectroscopy
{
    public static class SpectroscopyPrefabFactory
    {
        // ---- Flame (world-space) ---------------------------------------------------

        public static GameObject BuildFlame(out FlameTarget flameTarget)
        {
            var go = new GameObject("[Flame]");

            // Visible sphere representing flame
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(go.transform, false);
            sphere.transform.localScale = Vector3.one * 0.08f;

            // Trigger collider
            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.06f;
            col.isTrigger = true;

            flameTarget = go.AddComponent<FlameTarget>();
            return go;
        }

        // ---- Spectrum strip (screen-space) -----------------------------------------

        public static GameObject BuildStrip(Canvas cv, out SpectrumStrip strip)
        {
            float bot = UI.SafeAreaResolver.BottomInset();

            var root = new GameObject("SpectrumStrip", typeof(RectTransform));
            root.transform.SetParent(cv.transform, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(1f, 0f);
            rootRt.pivot     = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, bot + UI.Theme.FabSize + 6f);
            rootRt.sizeDelta = new Vector2(0f, 160f);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.04f, 0.95f);

            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 4f;

            // Mode label + buttons row
            var row = new GameObject("ControlRow", typeof(RectTransform));
            row.transform.SetParent(root.transform, false);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 8f;
            row.AddComponent<LayoutElement>().preferredHeight = 30f;

            var modeLabel = AddLabel(row.transform, "Emission", UI.Theme.TypeSizeS, UI.Theme.OnSurface);
            var emBtn  = AddSmallButton(row.transform, "Emission");
            var absBtn = AddSmallButton(row.transform, "Absorption");
            var starBtn= AddSmallButton(row.transform, "Stars ✦");

            // Strip image area
            var stripContainer = new GameObject("StripContainer", typeof(RectTransform));
            stripContainer.transform.SetParent(root.transform, false);
            var scImg = stripContainer.AddComponent<Image>();
            scImg.color = new Color(0.02f, 0.02f, 0.04f, 1f);
            var scLe = stripContainer.AddComponent<LayoutElement>();
            scLe.preferredHeight = 70f;
            var scRt = stripContainer.GetComponent<RectTransform>();

            var linesContainer = new GameObject("Lines", typeof(RectTransform));
            linesContainer.transform.SetParent(stripContainer.transform, false);
            var lcRt = linesContainer.GetComponent<RectTransform>();
            lcRt.anchorMin = Vector2.zero; lcRt.anchorMax = Vector2.one;
            lcRt.offsetMin = lcRt.offsetMax = Vector2.zero;

            // Line popup
            var popup = new GameObject("LinePopup", typeof(RectTransform));
            popup.transform.SetParent(root.transform, false);
            popup.AddComponent<Image>().color = UI.Theme.SurfaceElevated;
            var popRt = popup.GetComponent<RectTransform>();
            popRt.anchorMin = popRt.anchorMax = new Vector2(0.5f, 0f);
            popRt.pivot = new Vector2(0.5f, 0f);
            popRt.sizeDelta = new Vector2(260f, 70f);
            popRt.anchoredPosition = new Vector2(0f, 160f);
            var popTmp = popup.AddComponent<TextMeshProUGUI>();
            popTmp.fontSize = UI.Theme.TypeSizeS;
            popTmp.color = UI.Theme.OnSurface;
            popTmp.alignment = TextAlignmentOptions.Center;
            var popClose = popup.AddComponent<Button>();
            popup.SetActive(false);

            // Star overlay placeholder
            var starOverlay = new GameObject("StarOverlay", typeof(RectTransform));
            starOverlay.transform.SetParent(root.transform, false);
            starOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0.1f, 0.8f);
            var soLe = starOverlay.AddComponent<LayoutElement>();
            soLe.preferredHeight = 40f;
            AddLabel(starOverlay.transform, "Sun ☀  Vega ★  Betelgeuse ★ (tap to match)", UI.Theme.TypeSizeS - 2f, UI.Theme.OnSurfaceDim);
            starOverlay.SetActive(false);

            // Wire bindings
            var bindings = root.AddComponent<SpectrumStripBindings>();
            bindings.stripRoot       = rootRt;
            bindings.linesContainer  = lcRt;
            bindings.background      = scImg;
            bindings.modeLabel       = modeLabel;
            bindings.emissionBtn     = emBtn;
            bindings.absorptionBtn   = absBtn;
            bindings.starsBtn        = starBtn;
            bindings.linePopup       = popRt;
            bindings.popupText       = popTmp;
            bindings.popupClose      = popClose;
            bindings.starOverlay     = starOverlay.GetComponent<RectTransform>();

            strip = root.AddComponent<SpectrumStrip>();

            root.SetActive(false);
            return root;
        }

        private static TextMeshProUGUI AddLabel(Transform parent, string text, float size, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static Button AddSmallButton(Transform parent, string label)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = 80f;
            go.AddComponent<Image>().color = UI.Theme.SurfaceGlass;
            var btn = go.AddComponent<Button>();
            var lGo = new GameObject("L", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lrt = lGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 10f;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = UI.Theme.OnSurface;
            return btn;
        }
    }
}
