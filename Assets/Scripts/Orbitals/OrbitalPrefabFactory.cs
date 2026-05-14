using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Orbitals
{
    public static class OrbitalPrefabFactory
    {
        public static GameObject BuildPanel(Canvas cv,
            out TextMeshProUGUI configLabel,
            out TextMeshProUGUI elementLabel,
            out Button          closeButton)
        {
            float bot = UI.SafeAreaResolver.BottomInset();

            var root = new GameObject("OrbitalPanel", typeof(RectTransform));
            root.transform.SetParent(cv.transform, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(0f, bot + UI.Theme.FabSize + 70f);
            rt.sizeDelta = new Vector2(280f, 180f);

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

            elementLabel = AddTmp(headerRow.transform, "Orbital Viewer", UI.Theme.TypeSizeM, UI.Theme.OnSurface, bold: true);
            closeButton  = AddCloseButton(headerRow.transform);

            configLabel = AddTmp(root.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim);
            var configLe = configLabel.gameObject.AddComponent<LayoutElement>();
            configLe.preferredHeight = 28f;
            configLabel.enableWordWrapping = true;
            configLabel.alignment = TextAlignmentOptions.Center;

            AddTmp(root.transform, "Hold an element cube near the orbital viewer", UI.Theme.TypeSizeS - 2f, UI.Theme.OnSurfaceDim);

            root.SetActive(false);
            return root;
        }

        private static TextMeshProUGUI AddTmp(Transform parent, string text, float size, Color color, bool bold = false)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 28f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

        private static Button AddCloseButton(Transform parent)
        {
            var go = new GameObject("Close", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = 36f;
            go.AddComponent<Image>().color = UI.Theme.SurfaceGlass;
            var btn = go.AddComponent<Button>();
            var lGo = new GameObject("X", typeof(RectTransform));
            lGo.transform.SetParent(go.transform, false);
            var lrt = lGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var tmp = lGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "✕"; tmp.fontSize = UI.Theme.TypeSizeM;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = UI.Theme.OnSurface;
            return btn;
        }
    }
}
