using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace PeriodicAR.AcidBase
{
    public static class AcidsPrefabFactory
    {
        public static GameObject BuildPanel(Canvas cv, out Beaker beaker)
        {
            float bot = UI.SafeAreaResolver.BottomInset();

            var root = new GameObject("AcidBasePanel", typeof(RectTransform));
            root.transform.SetParent(cv.transform, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot     = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, bot + UI.Theme.FabSize + 70f);
            rootRt.sizeDelta = new Vector2(320f, 520f);

            root.AddComponent<Image>().color = UI.Theme.SurfaceElevated;
            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 6f;
            root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddLabel(root.transform, "pH / Acid-Base Visualizer", UI.Theme.TypeSizeM, UI.Theme.OnSurface, bold: true, height: 36f);

            // Substance picker
            var subDd = AddDropdown(root.transform, "Substance", 44f);

            // Indicator picker
            var indDd = AddDropdown(root.transform, "Indicator", 44f);

            // Beaker visual
            var beakerFrame = new GameObject("BeakerFrame", typeof(RectTransform));
            beakerFrame.transform.SetParent(root.transform, false);
            beakerFrame.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.35f, 0.6f);
            beakerFrame.AddComponent<LayoutElement>().preferredHeight = 110f;

            // Liquid fill image inside beaker
            var liquid = new GameObject("Liquid", typeof(RectTransform));
            liquid.transform.SetParent(beakerFrame.transform, false);
            var liqRt = liquid.GetComponent<RectTransform>();
            liqRt.anchorMin = Vector2.zero; liqRt.anchorMax = new Vector2(1f, 0.85f);
            liqRt.offsetMin = new Vector2(4f, 4f); liqRt.offsetMax = new Vector2(-4f, 0f);
            var liqImg = liquid.AddComponent<Image>();
            liqImg.color = new Color(0.5f, 0.8f, 1f, 0.75f);

            // pH display inside beaker
            var phGo = new GameObject("PhDisplay", typeof(RectTransform));
            phGo.transform.SetParent(beakerFrame.transform, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = phRt.anchorMax = new Vector2(0.5f, 0.5f);
            phRt.sizeDelta = new Vector2(120f, 50f);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            phTmp.text = "pH 7.0\nneutral"; phTmp.fontSize = UI.Theme.TypeSizeL;
            phTmp.alignment = TextAlignmentOptions.Center; phTmp.color = Color.white;
            phTmp.fontStyle = FontStyles.Bold;

            // pH color bar
            var barGo = new GameObject("PhBar", typeof(RectTransform));
            barGo.transform.SetParent(root.transform, false);
            barGo.AddComponent<LayoutElement>().preferredHeight = 16f;
            var barImg = barGo.AddComponent<Image>();
            barImg.type = Image.Type.Filled;
            barImg.fillMethod = Image.FillMethod.Horizontal;
            barImg.fillAmount = 0.5f;
            barImg.color = new Color(0.4f, 0.8f, 0.4f, 1f);

            // Slider
            var sliderGo = new GameObject("PhSlider", typeof(RectTransform));
            sliderGo.transform.SetParent(root.transform, false);
            sliderGo.AddComponent<LayoutElement>().preferredHeight = 36f;
            var slider = sliderGo.AddComponent<Slider>();
            var slBg = sliderGo.AddComponent<Image>(); slBg.color = UI.Theme.SurfaceGlass;
            var handleGo = new GameObject("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(sliderGo.transform, false);
            var handleImg = handleGo.AddComponent<Image>(); handleImg.color = UI.Theme.Accent;
            var handleRt = handleGo.GetComponent<RectTransform>(); handleRt.sizeDelta = new Vector2(20f, 20f);
            slider.handleRect = handleRt;
            slider.fillRect   = null;

            // Substance label + description
            var subLabelGo = new GameObject("SubLabel", typeof(RectTransform));
            subLabelGo.transform.SetParent(root.transform, false);
            subLabelGo.AddComponent<LayoutElement>().preferredHeight = 24f;
            var subLabelTmp = subLabelGo.AddComponent<TextMeshProUGUI>();
            subLabelTmp.text = ""; subLabelTmp.fontSize = UI.Theme.TypeSizeS;
            subLabelTmp.color = UI.Theme.Accent; subLabelTmp.alignment = TextAlignmentOptions.Center;

            var descGo = new GameObject("Desc", typeof(RectTransform));
            descGo.transform.SetParent(root.transform, false);
            descGo.AddComponent<LayoutElement>().preferredHeight = 70f;
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = ""; descTmp.fontSize = UI.Theme.TypeSizeS;
            descTmp.color = UI.Theme.OnSurfaceDim;
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.enableWordWrapping = true;

            // Buttons
            var btnRow = new GameObject("Buttons", typeof(RectTransform));
            btnRow.transform.SetParent(root.transform, false);
            var bhlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            bhlg.childAlignment = TextAnchor.MiddleCenter;
            bhlg.childForceExpandWidth = true; bhlg.childForceExpandHeight = false;
            bhlg.spacing = 8f;
            btnRow.AddComponent<LayoutElement>().preferredHeight = 44f;
            var neutralBtn = AddButton(btnRow.transform, "⚖ Neutralize", UI.Theme.Accent, UI.Theme.OnSurface);
            var clearBtn   = AddButton(btnRow.transform, "↺ Reset",       UI.Theme.SurfaceGlass, UI.Theme.OnSurface);

            // Wire bindings
            var bindings = root.AddComponent<BeakerBindings>();
            bindings.liquidImage       = liqImg;
            bindings.phDisplay         = phTmp;
            bindings.substanceLabel    = subLabelTmp;
            bindings.descriptionText   = descTmp;
            bindings.phSlider          = slider;
            bindings.substanceDropdown = subDd;
            bindings.indicatorDropdown = indDd;
            bindings.neutralizeButton  = neutralBtn;
            bindings.clearButton       = clearBtn;
            bindings.phBar             = barImg;

            beaker = root.AddComponent<Beaker>();

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
            var go = new GameObject(name + "Dd", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = UI.Theme.SurfaceGlass;
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var dd = go.AddComponent<Dropdown>();
            var lGo = new GameObject("Label", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var rt = lGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 2); rt.offsetMax = new Vector2(-8, -2);
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.color = UI.Theme.OnSurface; tmp.fontSize = UI.Theme.TypeSizeS;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return dd;
        }

        private static Button AddButton(Transform parent, string label, Color bg, Color labelColor)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1f;
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            var lGo = new GameObject("L", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lrt = lGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = UI.Theme.TypeSizeS;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = labelColor;
            return btn;
        }
    }
}
