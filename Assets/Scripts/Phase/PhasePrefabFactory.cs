using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace PeriodicAR.Phase
{
    public static class PhasePrefabFactory
    {
        public static GameObject BuildChamber(Canvas cv, out PhaseChamber chamber)
        {
            float bot = UI.SafeAreaResolver.BottomInset();

            var root = new GameObject("PhaseChamber", typeof(RectTransform));
            root.transform.SetParent(cv.transform, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot     = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, bot + UI.Theme.FabSize + 70f);
            rootRt.sizeDelta = new Vector2(340f, 540f);

            root.AddComponent<Image>().color = UI.Theme.SurfaceElevated;
            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 6f;
            root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header
            var headerRow = new GameObject("Header", typeof(RectTransform));
            headerRow.transform.SetParent(root.transform, false);
            var hhlg = headerRow.AddComponent<HorizontalLayoutGroup>();
            hhlg.childForceExpandWidth = true; hhlg.childForceExpandHeight = false;
            headerRow.AddComponent<LayoutElement>().preferredHeight = 36f;
            AddLabel(headerRow.transform, "State of Matter", UI.Theme.TypeSizeM, UI.Theme.OnSurface, bold: true);
            var closeBtn = AddButton(headerRow.transform, "✕", UI.Theme.SurfaceGlass, UI.Theme.OnSurface, 36f);

            // Substance dropdown
            var subDd = AddDropdown(root.transform, "Substance", 44f);

            // Simulation container
            var simContainer = new GameObject("SimContainer", typeof(RectTransform));
            simContainer.transform.SetParent(root.transform, false);
            var scLe = simContainer.AddComponent<LayoutElement>();
            scLe.preferredWidth = 300f; scLe.preferredHeight = 200f;
            simContainer.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.8f);

            // Phase label
            var phaseLabel    = AddLabel(root.transform, "Liquid · 25 °C · 1 atm", UI.Theme.TypeSizeS, UI.Theme.Accent, height: 28f);
            var tempLabel     = AddLabel(root.transform, "T = 298 K (25 °C)", UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim, height: 22f);
            var pressureLabel = AddLabel(root.transform, "P = 1.00 atm", UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim, height: 22f);

            // Temperature slider
            AddLabel(root.transform, "Temperature →", UI.Theme.TypeSizeS - 2f, UI.Theme.OnSurfaceDim, height: 18f);
            var tempSlider = AddSlider(root.transform, 1f, 500f, 298f);

            // Pressure slider
            AddLabel(root.transform, "Pressure →", UI.Theme.TypeSizeS - 2f, UI.Theme.OnSurfaceDim, height: 18f);
            var pressSlider = AddSlider(root.transform, 0.001f, 1000f, 1f);

            // Annotation
            var annotGo = new GameObject("Annotation", typeof(RectTransform));
            annotGo.transform.SetParent(root.transform, false);
            annotGo.AddComponent<LayoutElement>().preferredHeight = 40f;
            var annotTmp = annotGo.AddComponent<TextMeshProUGUI>();
            annotTmp.text = ""; annotTmp.fontSize = UI.Theme.TypeSizeS;
            annotTmp.color = UI.Theme.Warning; annotTmp.alignment = TextAlignmentOptions.Center;
            annotTmp.enableWordWrapping = true;
            annotGo.SetActive(false);

            // Wire bindings
            var bindings = root.AddComponent<PhaseChamberBindings>();
            bindings.simContainer      = simContainer.GetComponent<RectTransform>();
            bindings.phaseLabel        = phaseLabel;
            bindings.tempLabel         = tempLabel;
            bindings.pressureLabel     = pressureLabel;
            bindings.tempSlider        = tempSlider;
            bindings.pressureSlider    = pressSlider;
            bindings.substanceDropdown = subDd;
            bindings.annotationText    = annotTmp;
            bindings.closeButton       = closeBtn;

            chamber = root.AddComponent<PhaseChamber>();

            root.SetActive(false);
            return root;
        }

        private static TextMeshProUGUI AddLabel(Transform parent, string text, float size, Color color,
            bool bold = false, float height = 30f)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

        private static Dropdown AddDropdown(Transform parent, string name, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = UI.Theme.SurfaceGlass;
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var dd = go.AddComponent<Dropdown>();
            var lGo = new GameObject("L", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lrt = lGo.GetComponent<RectTransform>(); lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8, 2); lrt.offsetMax = new Vector2(-8, -2);
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.color = UI.Theme.OnSurface; tmp.fontSize = UI.Theme.TypeSizeS; tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return dd;
        }

        private static Slider AddSlider(Transform parent, float min, float max, float val)
        {
            var go = new GameObject("Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = UI.Theme.SurfaceGlass;
            go.AddComponent<LayoutElement>().preferredHeight = 32f;
            var sl = go.AddComponent<Slider>();
            sl.minValue = min; sl.maxValue = max; sl.value = val;
            var handleGo = new GameObject("H", typeof(RectTransform));
            handleGo.transform.SetParent(go.transform, false);
            handleGo.AddComponent<Image>().color = UI.Theme.Accent;
            var hrt = handleGo.GetComponent<RectTransform>(); hrt.sizeDelta = new Vector2(20f, 20f);
            sl.handleRect = hrt; sl.fillRect = null;
            return sl;
        }

        private static Button AddButton(Transform parent, string label, Color bg, Color labelColor, float width = -1f)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            if (width > 0) le.preferredWidth = width; else le.flexibleWidth = 1f;
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            var lGo = new GameObject("L", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lrt = lGo.GetComponent<RectTransform>(); lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = UI.Theme.TypeSizeS; tmp.alignment = TextAlignmentOptions.Center; tmp.color = labelColor;
            return btn;
        }
    }
}
