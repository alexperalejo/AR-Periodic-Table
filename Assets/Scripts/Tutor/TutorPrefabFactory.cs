using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Tutor
{
    /// <summary>
    /// Builds the screen-space tutor chat panel at runtime.
    /// Returns the configured GameObject; attach it under an existing screen-space canvas or
    /// let TutorController keep it as a standalone overlay canvas.
    /// </summary>
    public static class TutorPrefabFactory
    {
        // Reference resolution (matches BuildARHud).
        private const float RefW = 1080f;
        private const float RefH = 1920f;

        // Panel covers most of the screen in landscape/portrait.
        private const float PanelW = 950f;
        private const float PanelH = 1400f;

        private static readonly Color PanelBg      = new Color(0.06f, 0.07f, 0.10f, 0.96f);
        private static readonly Color HeaderBg      = new Color(0.10f, 0.14f, 0.20f, 1f);
        private static readonly Color InputBg       = new Color(0.12f, 0.16f, 0.22f, 1f);
        private static readonly Color SendBtnBg     = new Color(0.20f, 0.55f, 0.90f, 1f);
        private static readonly Color CloseBtnBg    = new Color(0.65f, 0.12f, 0.12f, 1f);

        public static GameObject CreatePanel()
        {
            // ---- Root canvas (screen-space overlay) ------------------------------------
            var root = new GameObject("TutorPanelCanvas", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefW, RefH);
            scaler.matchWidthOrHeight  = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            // ---- Semi-transparent backdrop (full screen) for modal feel ----------
            var backdrop = MakeRect(root.transform, "Backdrop");
            Stretch(backdrop);
            var backdropImg = backdrop.gameObject.AddComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.45f);

            // ---- Centred panel -------------------------------------------------
            var panel = MakeRect(root.transform, "Panel");
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot     = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(PanelW, PanelH);
            panel.gameObject.AddComponent<Image>().color = PanelBg;

            // ---- Header --------------------------------------------------------
            var header = MakeRect(panel.transform, "Header");
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot     = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 100f);
            header.gameObject.AddComponent<Image>().color = HeaderBg;

            var headerHlg = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerHlg.childAlignment        = TextAnchor.MiddleLeft;
            headerHlg.childForceExpandWidth = true;
            headerHlg.padding               = new RectOffset(20, 12, 0, 0);
            headerHlg.spacing               = 10f;

            var titleTmp = AddLabel(header.transform, "Tutor", 48f, TextAlignmentOptions.Left);
            titleTmp.fontStyle = FontStyles.Bold;

            var closeBtn = MakeRect(header.transform, "CloseButton");
            closeBtn.sizeDelta = new Vector2(80f, 80f);
            var closeBtnImg  = closeBtn.gameObject.AddComponent<Image>();
            closeBtnImg.color = CloseBtnBg;
            var closeBtnComp = closeBtn.gameObject.AddComponent<Button>();
            AddLabel(closeBtn.transform, "✕", 36f);

            // Force the close button to not expand.
            var closeLe = closeBtn.gameObject.AddComponent<LayoutElement>();
            closeLe.preferredWidth  = 80f;
            closeLe.preferredHeight = 80f;
            closeLe.flexibleWidth   = 0f;

            // ---- Chat scroll view -----------------------------------------------
            var scrollGo = MakeRect(panel.transform, "ChatScroll");
            scrollGo.anchorMin = new Vector2(0f, 0f);
            scrollGo.anchorMax = new Vector2(1f, 1f);
            scrollGo.offsetMin = new Vector2(0f, 110f);  // above input row
            scrollGo.offsetMax = new Vector2(0f, -100f); // below header

            var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            scrollGo.gameObject.AddComponent<Image>().color = Color.clear; // required mask source
            var mask = scrollGo.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.horizontal   = false;
            scroll.vertical     = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            var viewport = MakeRect(scrollGo.transform, "Viewport");
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            // Content
            var content = MakeRect(viewport.transform, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot     = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment        = TextAnchor.UpperLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight= false;
            vlg.padding               = new RectOffset(16, 16, 12, 12);
            vlg.spacing               = 10f;

            var contentCsf = content.gameObject.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;

            // ---- Input row -----------------------------------------------------
            var inputRow = MakeRect(panel.transform, "InputRow");
            inputRow.anchorMin = new Vector2(0f, 0f);
            inputRow.anchorMax = new Vector2(1f, 0f);
            inputRow.pivot     = new Vector2(0.5f, 0f);
            inputRow.anchoredPosition = Vector2.zero;
            inputRow.sizeDelta = new Vector2(0f, 110f);

            var inputHlg = inputRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            inputHlg.childAlignment        = TextAnchor.MiddleCenter;
            inputHlg.childForceExpandWidth = true;
            inputHlg.childForceExpandHeight= true;
            inputHlg.padding               = new RectOffset(12, 12, 10, 10);
            inputHlg.spacing               = 10f;

            // TMP_InputField
            var inputBg = MakeRect(inputRow.transform, "InputBg");
            inputBg.gameObject.AddComponent<Image>().color = InputBg;
            var inputField = inputBg.gameObject.AddComponent<TMP_InputField>();

            var inputTextArea = MakeRect(inputBg.transform, "TextArea");
            Stretch(inputTextArea);
            inputTextArea.offsetMin = new Vector2(10f, 5f);
            inputTextArea.offsetMax = new Vector2(-10f, -5f);
            var inputTmp = inputTextArea.gameObject.AddComponent<TextMeshProUGUI>();
            inputTmp.fontSize    = 32f;
            inputTmp.color       = Color.white;
            inputTmp.richText    = false;

            var placeholder = MakeRect(inputTextArea.transform, "Placeholder");
            Stretch(placeholder);
            var placeholderTmp = placeholder.gameObject.AddComponent<TextMeshProUGUI>();
            placeholderTmp.text      = "Ask a question…";
            placeholderTmp.fontSize  = 32f;
            placeholderTmp.color     = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderTmp.fontStyle = FontStyles.Italic;

            inputField.textComponent  = inputTmp;
            inputField.placeholder    = placeholderTmp;
            inputField.lineType       = TMP_InputField.LineType.MultiLineSubmit;

            // Send button
            var sendRect = MakeRect(inputRow.transform, "SendButton");
            var sendImg  = sendRect.gameObject.AddComponent<Image>();
            sendImg.color = SendBtnBg;
            var sendBtn  = sendRect.gameObject.AddComponent<Button>();
            AddLabel(sendRect.transform, "Send", 30f);

            var sendLe = sendRect.gameObject.AddComponent<LayoutElement>();
            sendLe.preferredWidth = 160f;
            sendLe.flexibleWidth  = 0f;

            // ---- Wire components ------------------------------------------------
            var bindings                = root.AddComponent<TutorPanelBindings>();
            bindings.panelRoot          = root;
            bindings.closeButton        = closeBtnComp;
            bindings.chatScroll         = scroll;
            bindings.messageContainer   = content;
            bindings.inputField         = inputField;
            bindings.sendButton         = sendBtn;

            root.AddComponent<TutorPanel>();

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
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.alignment = alignment;
            tmp.color     = Color.white;
            return tmp;
        }
    }
}
