using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Builds the detection popup canvas at runtime with no prefab asset.
    /// Returns a world-space Canvas ready to be shown by DetectionPopup.Show().
    /// </summary>
    public static class ObjectDetectionPrefabFactory
    {
        private static readonly Color PanelBg      = new Color(0.06f, 0.06f, 0.10f, 0.88f);
        private static readonly Color SeparatorCol  = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color HighBtnBg     = new Color(0.90f, 0.65f, 0.10f, 1f);
        private static readonly Color CloseBtnBg    = new Color(0.70f, 0.15f, 0.15f, 1f);

        // World-space canvas scale. 0.001 means 1 pixel == 1 mm in world space.
        private const float CanvasScale = 0.001f;

        // Canvas size in UI pixels (~30 cm wide, ~45 cm tall at CanvasScale).
        private const float PanelW = 300f;
        private const float PanelH = 450f;

        public static GameObject CreatePopup()
        {
            // ---- Root: world-space canvas ----------------------------------------
            var root = new GameObject("DetectionPopup", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            var rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(PanelW, PanelH);
            root.transform.localScale = Vector3.one * CanvasScale;

            // ---- Background panel -----------------------------------------------
            var bg = MakeRect(root.transform, "Background");
            Stretch(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = PanelBg;

            // ---- Close button (top-right X) -------------------------------------
            var closeBtn = MakeRect(bg.transform, "CloseButton");
            closeBtn.anchorMin = new Vector2(1f, 1f);
            closeBtn.anchorMax = new Vector2(1f, 1f);
            closeBtn.pivot     = new Vector2(1f, 1f);
            closeBtn.anchoredPosition = new Vector2(-8f, -8f);
            closeBtn.sizeDelta = new Vector2(50f, 50f);
            var closeBtnComp = closeBtn.gameObject.AddComponent<Button>();
            var closeBtnImg  = closeBtn.gameObject.AddComponent<Image>();
            closeBtnImg.color = CloseBtnBg;
            AddLabel(closeBtn.transform, "✕", 26f);

            // ---- Header row: title + score ---------------------------------------
            var header = MakeRect(bg.transform, "Header");
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot     = new Vector2(0.5f, 1f);
            header.anchoredPosition = new Vector2(0f, -8f);
            header.sizeDelta = new Vector2(-20f, 70f);

            var hGroup = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            hGroup.childAlignment        = TextAnchor.MiddleLeft;
            hGroup.childForceExpandWidth = true;
            hGroup.padding               = new RectOffset(8, 8, 4, 4);
            hGroup.spacing               = 8f;

            var titleTmp = AddLabel(header.transform, "Object", 38f, TextAlignmentOptions.Left);
            titleTmp.fontStyle = FontStyles.Bold;

            var scoreTmp = AddLabel(header.transform, "0%", 30f, TextAlignmentOptions.Right);
            scoreTmp.color = new Color(0.7f, 0.9f, 0.7f, 1f);

            // ---- Divider --------------------------------------------------------
            var divider = MakeRect(bg.transform, "Divider");
            divider.anchorMin = new Vector2(0f, 1f);
            divider.anchorMax = new Vector2(1f, 1f);
            divider.pivot     = new Vector2(0.5f, 1f);
            divider.anchoredPosition = new Vector2(0f, -82f);
            divider.sizeDelta = new Vector2(-20f, 2f);
            divider.gameObject.AddComponent<Image>().color = SeparatorCol;

            // ---- Label: "Contains:" -------------------------------------------
            var primaryLabel = MakeRect(bg.transform, "PrimaryLabel");
            primaryLabel.anchorMin = new Vector2(0f, 1f);
            primaryLabel.anchorMax = new Vector2(1f, 1f);
            primaryLabel.pivot     = new Vector2(0f, 1f);
            primaryLabel.anchoredPosition = new Vector2(10f, -90f);
            primaryLabel.sizeDelta = new Vector2(-20f, 36f);
            var pLabelTmp = primaryLabel.gameObject.AddComponent<TextMeshProUGUI>();
            pLabelTmp.text      = "Contains:";
            pLabelTmp.fontSize  = 26f;
            pLabelTmp.color     = new Color(0.7f, 0.7f, 0.7f, 1f);

            // ---- Primary chips container -----------------------------------------
            var primaryContainer = MakeRect(bg.transform, "PrimaryChips");
            primaryContainer.anchorMin = new Vector2(0f, 1f);
            primaryContainer.anchorMax = new Vector2(1f, 1f);
            primaryContainer.pivot     = new Vector2(0f, 1f);
            primaryContainer.anchoredPosition = new Vector2(10f, -130f);
            primaryContainer.sizeDelta = new Vector2(-20f, 70f);

            var pHGroup = primaryContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            pHGroup.childAlignment        = TextAnchor.MiddleLeft;
            pHGroup.childForceExpandWidth = false;
            pHGroup.childForceExpandHeight= true;
            pHGroup.spacing               = 6f;

            // ---- Label: "Trace:" ------------------------------------------------
            var traceLabel = MakeRect(bg.transform, "TraceLabel");
            traceLabel.anchorMin = new Vector2(0f, 1f);
            traceLabel.anchorMax = new Vector2(1f, 1f);
            traceLabel.pivot     = new Vector2(0f, 1f);
            traceLabel.anchoredPosition = new Vector2(10f, -206f);
            traceLabel.sizeDelta = new Vector2(-20f, 36f);
            var tLabelTmp = traceLabel.gameObject.AddComponent<TextMeshProUGUI>();
            tLabelTmp.text     = "Trace:";
            tLabelTmp.fontSize = 24f;
            tLabelTmp.color    = new Color(0.55f, 0.55f, 0.55f, 1f);

            // ---- Trace chips container -------------------------------------------
            var traceContainer = MakeRect(bg.transform, "TraceChips");
            traceContainer.anchorMin = new Vector2(0f, 1f);
            traceContainer.anchorMax = new Vector2(1f, 1f);
            traceContainer.pivot     = new Vector2(0f, 1f);
            traceContainer.anchoredPosition = new Vector2(10f, -246f);
            traceContainer.sizeDelta = new Vector2(-20f, 60f);

            var tHGroup = traceContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            tHGroup.childAlignment        = TextAnchor.MiddleLeft;
            tHGroup.childForceExpandWidth = false;
            tHGroup.childForceExpandHeight= true;
            tHGroup.spacing               = 5f;

            // ---- Note text ------------------------------------------------------
            var noteRect = MakeRect(bg.transform, "NoteText");
            noteRect.anchorMin = new Vector2(0f, 1f);
            noteRect.anchorMax = new Vector2(1f, 1f);
            noteRect.pivot     = new Vector2(0f, 1f);
            noteRect.anchoredPosition = new Vector2(10f, -312f);
            noteRect.sizeDelta = new Vector2(-20f, 80f);
            var noteTmp = noteRect.gameObject.AddComponent<TextMeshProUGUI>();
            noteTmp.fontSize         = 22f;
            noteTmp.color            = new Color(0.75f, 0.75f, 0.75f, 1f);
            noteTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // ---- Highlight button -----------------------------------------------
            var highlightBtnRect = MakeRect(bg.transform, "HighlightButton");
            highlightBtnRect.anchorMin = new Vector2(0f, 0f);
            highlightBtnRect.anchorMax = new Vector2(1f, 0f);
            highlightBtnRect.pivot     = new Vector2(0.5f, 0f);
            highlightBtnRect.anchoredPosition = new Vector2(0f, 12f);
            highlightBtnRect.sizeDelta = new Vector2(-20f, 64f);
            var highlightBtnComp = highlightBtnRect.gameObject.AddComponent<Button>();
            var highlightBtnImg  = highlightBtnRect.gameObject.AddComponent<Image>();
            highlightBtnImg.color = HighBtnBg;
            AddLabel(highlightBtnRect.transform, "Highlight on Table", 30f);

            // ---- Wire bindings --------------------------------------------------
            var popup    = root.AddComponent<DetectionPopup>();
            var bindings = root.AddComponent<DetectionPopupBindings>();
            bindings.titleText         = titleTmp;
            bindings.scoreText         = scoreTmp;
            bindings.primaryContainer  = primaryContainer;
            bindings.traceContainer    = traceContainer;
            bindings.noteText          = noteTmp;
            bindings.highlightButton   = highlightBtnComp;
            bindings.closeButton       = closeBtnComp;

            root.SetActive(false);
            return root;
        }

        // ---- Helpers ------------------------------------------------------------

        private static RectTransform MakeRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI AddLabel(
            Transform parent,
            string text,
            float fontSize,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go  = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.alignment = alignment;
            tmp.color     = Color.white;
            return tmp;
        }
    }
}
