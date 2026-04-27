using System.IO;
using PeriodicAR.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.EditorTools
{
    public static class BuildARHud
    {
        private const string PrefabPath = "Assets/Prefabs/UI/ARHudCanvas.prefab";

        [MenuItem("Tools/AR Periodic Table/Build AR HUD")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Prefabs/UI");

            var root = new GameObject("ARHudCanvas", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            var placeButton = CreateSquareButton(root.transform, "PlaceButton", "Place",
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot:     new Vector2(1f, 1f),
                anchoredPos: new Vector2(-40f, -40f),
                size: new Vector2(180f, 180f));
            var placeButtonLabel = placeButton.GetComponentInChildren<TMP_Text>(true);

            var instructionRoot = CreatePanel(root.transform, "InstructionPanel",
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                pivot:     new Vector2(0.5f, 1f),
                anchoredPos: new Vector2(0f, -60f),
                size: new Vector2(820f, 120f),
                bg: new Color(0f, 0f, 0f, 0.55f));
            var instructionLabel = CreateTmp(instructionRoot.transform, "Label",
                "Tap a detected horizontal plane to place the table", 42, TextAlignmentOptions.Center);
            StretchToParent(instructionLabel.rectTransform, 16f);
            instructionRoot.SetActive(false);

            var filterBar = CreatePanel(root.transform, "FilterBar",
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 0f),
                pivot:     new Vector2(0.5f, 0f),
                anchoredPos: new Vector2(0f, 40f),
                size: new Vector2(-80f, 360f),
                bg: new Color(0f, 0f, 0f, 0.35f));

            var grid = filterBar.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(16, 16, 16, 16);
            grid.spacing = new Vector2(12f, 12f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.cellSize = new Vector2(180f, 150f);

            string[] categoryLabels = {
                "Alkali\nMetals",
                "Alkaline\nEarth",
                "Transition\nMetals",
                "Post-Trans.\nMetals",
                "Lanthanoids",
                "Actinoids",
                "Metalloids",
                "Halogens",
                "Noble\nGases",
                "Other\nNonmetals",
            };
            foreach (var label in categoryLabels)
            {
                CreateCategoryButton(filterBar.transform, label);
            }

            var controller = root.AddComponent<ARHudController>();
            WireControllerInternalRefs(controller, placeButton, placeButtonLabel, instructionRoot, instructionLabel);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
            Object.DestroyImmediate(root);

            if (ok)
            {
                Debug.Log($"[BuildARHud] Saved {PrefabPath}. Drag it under the scene root and wire ARHudController.tapToPlace / planeWatcher in the Inspector.");
                EditorGUIUtility.PingObject(prefab);
            }
            else
            {
                Debug.LogError("[BuildARHud] Prefab save failed.");
            }
        }

        private static Button CreateSquareButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            btn.colors = colors;

            var tmp = CreateTmp(go.transform, "Label", label, 56, TextAlignmentOptions.Center);
            StretchToParent(tmp.rectTransform, 8f);
            return btn;
        }

        private static Button CreateCategoryButton(Transform parent, string label)
        {
            var go = new GameObject(label.Replace("\n", "_").Replace(".", "").Replace("-", ""),
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var tmp = CreateTmp(go.transform, "Label", label, 28, TextAlignmentOptions.Center);
            StretchToParent(tmp.rectTransform, 6f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            go.GetComponent<Image>().color = bg;
            return go;
        }

        private static TMP_Text CreateTmp(Transform parent, string name, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private static void StretchToParent(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static void WireControllerInternalRefs(ARHudController controller, Button placeButton,
            TMP_Text placeButtonLabel, GameObject instructionRoot, TMP_Text instructionLabel)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("placeButton").objectReferenceValue = placeButton;
            so.FindProperty("placeButtonLabel").objectReferenceValue = placeButtonLabel;
            so.FindProperty("instructionRoot").objectReferenceValue = instructionRoot;
            so.FindProperty("instructionLabel").objectReferenceValue = instructionLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
