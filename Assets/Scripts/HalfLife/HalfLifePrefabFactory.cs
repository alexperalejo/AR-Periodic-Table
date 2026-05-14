using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.HalfLife
{
    public static class HalfLifePrefabFactory
    {
        public static GameObject BuildBench(
            Canvas cv,
            out AtomCloud          cloud,
            out DecayGraph         graph,
            out TextMeshProUGUI    statsLabel,
            out TextMeshProUGUI    storyLabel,
            out Dropdown           isotopeDropdown,
            out Button             runButton,
            out Button             resetButton,
            out Dropdown           speedDropdown)
        {
            float bot = UI.SafeAreaResolver.BottomInset();

            // ---- Root panel --------------------------------------------------------
            var root = new GameObject("HalfLifeBench", typeof(RectTransform));
            root.transform.SetParent(cv.transform, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0f, 0f);
            rootRt.pivot     = new Vector2(0f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, bot + UI.Theme.FabSize + 70f);
            rootRt.sizeDelta = new Vector2(Screen.width > 0 ? Mathf.Min(Screen.width, 480f) : 360f, 560f);

            root.AddComponent<Image>().color = UI.Theme.SurfaceElevated;
            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 6f;
            root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ---- Title -------------------------------------------------------------
            AddLabel(root.transform, "Half-Life Simulator", UI.Theme.TypeSizeM, UI.Theme.OnSurface, bold: true, height: 36f);

            // ---- Isotope selector --------------------------------------------------
            isotopeDropdown = AddDropdown(root.transform, "Isotope", height: 44f);

            // ---- Speed selector ----------------------------------------------------
            speedDropdown = AddDropdown(root.transform, "Speed", height: 44f);
            speedDropdown.AddOptions(new List<string> { "1×", "100×", "1000×", "To Completion" });

            // ---- Run / Reset buttons -----------------------------------------------
            var btnRow = MakeRow(root.transform, 44f);
            runButton   = AddButton(btnRow.transform, "▶  Run",   UI.Theme.Accent,         labelColor: UI.Theme.OnSurface);
            resetButton = AddButton(btnRow.transform, "↺  Reset", UI.Theme.SurfaceGlass,   labelColor: UI.Theme.OnSurface);

            // ---- Stats label -------------------------------------------------------
            statsLabel = AddLabel(root.transform, "Original: 1000  Decayed: 0  Time: 0s",
                UI.Theme.TypeSizeS, UI.Theme.OnSurfaceDim, height: 36f);

            // ---- Atom cloud (world-space proxy; actual spheres built separately) ---
            // The cloud is a world-space object; we add a placeholder entry here.
            cloud = null; // assigned by controller after world placement

            // ---- Decay graph -------------------------------------------------------
            var graphGo = new GameObject("DecayGraph", typeof(RectTransform));
            graphGo.transform.SetParent(root.transform, false);
            var graphRt = graphGo.GetComponent<RectTransform>();
            graphRt.sizeDelta = new Vector2(280f, 160f);
            graphGo.AddComponent<LayoutElement>().preferredHeight = 160f;
            graph = graphGo.AddComponent<DecayGraph>();

            // ---- Use-case / story panel --------------------------------------------
            var storyPanel = new GameObject("StoryPanel", typeof(RectTransform));
            storyPanel.transform.SetParent(root.transform, false);
            storyPanel.AddComponent<Image>().color = UI.Theme.SurfaceGlass;
            var spVlg = storyPanel.AddComponent<VerticalLayoutGroup>();
            spVlg.childAlignment = TextAnchor.UpperLeft;
            spVlg.childForceExpandWidth = true;
            spVlg.childForceExpandHeight = false;
            spVlg.padding = new RectOffset(8, 8, 6, 6);
            spVlg.spacing = 4f;
            storyPanel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            storyPanel.AddComponent<LayoutElement>().preferredHeight = 120f;

            AddLabel(storyPanel.transform, "Use Case", UI.Theme.TypeSizeS, UI.Theme.Accent, bold: true, height: 22f);
            storyLabel = AddLabel(storyPanel.transform, "", UI.Theme.TypeSizeS, UI.Theme.OnSurface, height: 90f, wrap: true);

            root.SetActive(false);
            return root;
        }

        // ---- helpers ---------------------------------------------------------------

        private static TextMeshProUGUI AddLabel(Transform parent, string text, float size,
            Color color, bool bold = false, float height = 30f, bool wrap = false)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            if (wrap) { tmp.alignment = TextAlignmentOptions.TopLeft; tmp.enableWordWrapping = true; }
            return tmp;
        }

        private static Dropdown AddDropdown(Transform parent, string name, float height)
        {
            var go = new GameObject(name + "Dropdown", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = UI.Theme.SurfaceGlass;
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var dd = go.AddComponent<Dropdown>();
            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(go.transform, false);
            var rt = label.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 2); rt.offsetMax = new Vector2(-8, -2);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.color = UI.Theme.OnSurface; tmp.fontSize = UI.Theme.TypeSizeS;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            dd.captionText = null; // TMP-Dropdown needs separate wiring; controller handles display
            return dd;
        }

        private static GameObject MakeRow(Transform parent, float height)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 8f;
            go.AddComponent<LayoutElement>().preferredHeight = height;
            return go;
        }

        private static Button AddButton(Transform parent, string label, Color bg, Color labelColor)
        {
            var go = new GameObject(label.Trim(), typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1f;
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            var lGo = new GameObject("Label", typeof(RectTransform));
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
