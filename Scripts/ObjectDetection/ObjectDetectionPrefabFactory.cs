// Assets/Scripts/ObjectDetection/ObjectDetectionPrefabFactory.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Builds a basic world-space DetectionPopup at runtime. This means the
    /// scan feature works as soon as the scripts are compiled — no manual
    /// prefab/canvas setup required. If you later want a polished design,
    /// drop a prefab reference into ScanController.popupPrefab and this
    /// factory is bypassed.
    /// </summary>
    public static class ObjectDetectionPrefabFactory
    {
        // Final on-screen size at spawnDistance (~0.6 m). Tuned to feel like
        // a phone-sized card hovering in front of the user.
        private const float CanvasWorldWidth  = 0.32f;
        private const float CanvasWorldHeight = 0.22f;
        // World-space canvases use a 1px-per-unit ratio by default, which would
        // make a 32cm canvas only 32 pixels wide. Scale up so text renders
        // crisply, then shrink the whole transform back down.
        private const float CanvasPixelScale  = 1000f;

        public static DetectionPopup CreatePopup()
        {
            var go = new GameObject("DetectionPopup");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(CanvasWorldWidth * CanvasPixelScale,
                                        CanvasWorldHeight * CanvasPixelScale);
            rt.localScale = Vector3.one / CanvasPixelScale;

            // ---- background panel -------------------------------------------------
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.05f, 0.07f, 0.10f, 0.92f);

            // ---- title label ------------------------------------------------------
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(go.transform, false);
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot     = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -16f);
            titleRt.sizeDelta = new Vector2(-32f, 56f);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "Object";
            title.fontSize = 36f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1f, 0.9f, 0.4f, 1f);

            // ---- body label -------------------------------------------------------
            var bodyGo = new GameObject("Body", typeof(RectTransform));
            bodyGo.transform.SetParent(go.transform, false);
            var bodyRt = (RectTransform)bodyGo.transform;
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(20f, 80f);
            bodyRt.offsetMax = new Vector2(-20f, -76f);
            var body = bodyGo.AddComponent<TextMeshProUGUI>();
            body.text = "";
            body.fontSize = 26f;
            body.alignment = TextAlignmentOptions.TopLeft;
            body.color = Color.white;
            body.enableWordWrapping = true;
            body.richText = true;

            // ---- highlight button -------------------------------------------------
            var highlightBtn = MakeButton(go.transform, "HighlightButton", "Highlight on Table");
            var hbRt = (RectTransform)highlightBtn.transform;
            hbRt.anchorMin = new Vector2(0f, 0f);
            hbRt.anchorMax = new Vector2(0.65f, 0f);
            hbRt.pivot     = new Vector2(0.5f, 0f);
            hbRt.anchoredPosition = new Vector2(0f, 16f);
            hbRt.sizeDelta = new Vector2(-12f, 56f);

            // ---- close button -----------------------------------------------------
            var closeBtn = MakeButton(go.transform, "CloseButton", "✕");
            var cbRt = (RectTransform)closeBtn.transform;
            cbRt.anchorMin = new Vector2(0.7f, 0f);
            cbRt.anchorMax = new Vector2(1f, 0f);
            cbRt.pivot     = new Vector2(0.5f, 0f);
            cbRt.anchoredPosition = new Vector2(0f, 16f);
            cbRt.sizeDelta = new Vector2(-12f, 56f);

            var popup = go.AddComponent<DetectionPopup>();

            // Reach back into the freshly added component and wire its
            // serialized fields by reflection-free assignment via SerializedObject
            // is editor-only, so we expose setters indirectly here:
            DetectionPopupBindings.Bind(popup, title, body, highlightBtn, closeBtn);

            return popup;
        }

        private static Button MakeButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.45f, 0.85f, 1f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var lblRt = (RectTransform)labelGo.transform;
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;
            var txt = labelGo.AddComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 24f;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            return go.GetComponent<Button>();
        }
    }
}
