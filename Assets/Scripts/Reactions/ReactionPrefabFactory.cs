using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Reactions
{
    /// <summary>
    /// Builds the reaction sandbox panel at runtime (screen-space overlay).
    /// </summary>
    public static class ReactionPrefabFactory
    {
        private const float RefW  = 1080f;
        private const float RefH  = 1920f;

        private static readonly Color PanelBg    = new Color(0.07f, 0.08f, 0.12f, 0.96f);
        private static readonly Color HeaderBg   = new Color(0.12f, 0.16f, 0.22f, 1f);
        private static readonly Color SlotBg     = new Color(0.14f, 0.22f, 0.14f, 1f);
        private static readonly Color IgniteBg   = new Color(0.85f, 0.45f, 0.10f, 1f);
        private static readonly Color SkipBg     = new Color(0.40f, 0.40f, 0.40f, 1f);
        private static readonly Color CloseBtnBg = new Color(0.65f, 0.12f, 0.12f, 1f);

        public static GameObject CreatePanel()
        {
            var root = new GameObject("ReactionTrayCanvas", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(RefW, RefH);
            scaler.matchWidthOrHeight   = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            var backdrop = MakeRect(root.transform, "Backdrop");
            Stretch(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);

            var panel = MakeRect(root.transform, "Panel");
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot     = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(900f, 1100f);
            panel.gameObject.AddComponent<Image>().color = PanelBg;

            // Header
            var header = MakeRect(panel.transform, "Header");
            header.anchorMin = new Vector2(0f, 1f); header.anchorMax = new Vector2(1f, 1f);
            header.pivot     = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 100f);
            header.gameObject.AddComponent<Image>().color = HeaderBg;
            var hlg = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = true;
            hlg.padding = new RectOffset(20, 12, 0, 0);
            hlg.spacing = 10f;
            var titleTmp = AddLabel(header.transform, "Reaction Sandbox", 44f, TextAlignmentOptions.Left);
            titleTmp.fontStyle = FontStyles.Bold;
            var closeRect = MakeRect(header.transform, "CloseButton");
            closeRect.sizeDelta = new Vector2(80f, 80f);
            closeRect.gameObject.AddComponent<Image>().color = CloseBtnBg;
            var closeBtn = closeRect.gameObject.AddComponent<Button>();
            AddLabel(closeRect.transform, "✕", 36f);
            var closeLe = closeRect.gameObject.AddComponent<LayoutElement>();
            closeLe.preferredWidth = 80f; closeLe.flexibleWidth = 0f;

            // Slot row
            var slotRow = MakeRect(panel.transform, "SlotRow");
            slotRow.anchorMin = new Vector2(0f, 1f); slotRow.anchorMax = new Vector2(1f, 1f);
            slotRow.pivot     = new Vector2(0.5f, 1f);
            slotRow.anchoredPosition = new Vector2(0f, -110f);
            slotRow.sizeDelta = new Vector2(-40f, 220f);
            var slotHlg = slotRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotHlg.childAlignment = TextAnchor.MiddleCenter;
            slotHlg.childForceExpandWidth = true;
            slotHlg.childForceExpandHeight = true;
            slotHlg.spacing = 20f;
            slotHlg.padding = new RectOffset(10, 10, 10, 10);

            var slotARect = MakeRect(slotRow.transform, "SlotA");
            slotARect.gameObject.AddComponent<Image>().color = SlotBg;
            var slotABtn = slotARect.gameObject.AddComponent<Button>();
            var slotALabel = AddLabel(slotARect.transform, "Slot A\n(grab element)", 34f);

            var plusRect = MakeRect(slotRow.transform, "Plus");
            var plusLe = plusRect.gameObject.AddComponent<LayoutElement>();
            plusLe.preferredWidth = 60f; plusLe.flexibleWidth = 0f;
            AddLabel(plusRect.transform, "+", 56f);

            var slotBRect = MakeRect(slotRow.transform, "SlotB");
            slotBRect.gameObject.AddComponent<Image>().color = SlotBg;
            var slotBBtn = slotBRect.gameObject.AddComponent<Button>();
            var slotBLabel = AddLabel(slotBRect.transform, "Slot B\n(grab element)", 34f);

            // Ignite button
            var igniteRect = MakeRect(panel.transform, "IgniteButton");
            igniteRect.anchorMin = new Vector2(0f, 1f); igniteRect.anchorMax = new Vector2(1f, 1f);
            igniteRect.pivot     = new Vector2(0.5f, 1f);
            igniteRect.anchoredPosition = new Vector2(0f, -350f);
            igniteRect.sizeDelta = new Vector2(-80f, 100f);
            igniteRect.gameObject.AddComponent<Image>().color = IgniteBg;
            var igniteBtn = igniteRect.gameObject.AddComponent<Button>();
            var igniteLbl = AddLabel(igniteRect.transform, "Ignite!", 48f);
            igniteLbl.fontStyle = FontStyles.Bold;

            // Skip button (hidden while not animating)
            var skipRect = MakeRect(panel.transform, "SkipButton");
            skipRect.anchorMin = new Vector2(0f, 1f); skipRect.anchorMax = new Vector2(1f, 1f);
            skipRect.pivot     = new Vector2(0.5f, 1f);
            skipRect.anchoredPosition = new Vector2(0f, -470f);
            skipRect.sizeDelta = new Vector2(-80f, 70f);
            skipRect.gameObject.AddComponent<Image>().color = SkipBg;
            var skipBtn = skipRect.gameObject.AddComponent<Button>();
            AddLabel(skipRect.transform, "Skip animation", 32f);
            skipRect.gameObject.SetActive(false);

            // Result text
            var resultRect = MakeRect(panel.transform, "ResultText");
            resultRect.anchorMin = new Vector2(0f, 0f); resultRect.anchorMax = new Vector2(1f, 1f);
            resultRect.offsetMin = new Vector2(20f, 20f);
            resultRect.offsetMax = new Vector2(-20f, -560f);
            var resultTmp = resultRect.gameObject.AddComponent<TextMeshProUGUI>();
            resultTmp.fontSize   = 32f;
            resultTmp.color      = Color.white;
            resultTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            resultTmp.alignment  = TextAlignmentOptions.TopLeft;

            // Wire
            var bindings             = root.AddComponent<ReactionTrayBindings>();
            bindings.panelRoot       = root;
            bindings.slotALabel      = slotALabel;
            bindings.slotBLabel      = slotBLabel;
            bindings.slotAButton     = slotABtn;
            bindings.slotBButton     = slotBBtn;
            bindings.igniteButton    = igniteBtn;
            bindings.closeButton     = closeBtn;
            bindings.resultText      = resultTmp;
            bindings.skipButton      = skipBtn;
            root.AddComponent<ReactionTray>();

            root.SetActive(false);
            return root;
        }

        private static RectTransform MakeRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI AddLabel(Transform parent, string text, float size,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go  = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.alignment = align;
            tmp.color     = Color.white;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            return tmp;
        }
    }
}
