using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Tutor
{
    /// <summary>
    /// Screen-space chat panel for the LLM tutor. Paired with TutorPanelBindings.
    /// </summary>
    [RequireComponent(typeof(TutorPanelBindings))]
    public class TutorPanel : MonoBehaviour
    {
        private static readonly Color UserBubbleBg      = new Color(0.15f, 0.40f, 0.80f, 1f);
        private static readonly Color AssistantBubbleBg = new Color(0.16f, 0.20f, 0.22f, 1f);
        private static readonly Color StatusTextColor   = new Color(0.55f, 0.55f, 0.55f, 1f);

        private TutorPanelBindings _b;
        private TextMeshProUGUI    _thinkingBubble;

        private void Awake() => _b = GetComponent<TutorPanelBindings>();

        public void Open()
        {
            if (_b.panelRoot != null) _b.panelRoot.SetActive(true);
            if (_b.inputField != null)
            {
                _b.inputField.text = string.Empty;
                _b.inputField.ActivateInputField();
            }
        }

        public void Close()
        {
            if (_b.panelRoot != null) _b.panelRoot.SetActive(false);
        }

        public bool IsOpen => _b.panelRoot != null && _b.panelRoot.activeSelf;

        public void AppendUserMessage(string text)
        {
            ClearThinking();
            AddBubble(text, UserBubbleBg, TextAlignmentOptions.Right, isRight: true);
            ScrollToBottom();
        }

        public void AppendAssistantMessage(string text)
        {
            ClearThinking();
            AddBubble(text, AssistantBubbleBg, TextAlignmentOptions.Left, isRight: false);
            ScrollToBottom();
        }

        public void AppendToolStatus(string text)
        {
            ClearThinking();
            var go = new GameObject("ToolStatus", typeof(RectTransform));
            go.transform.SetParent(_b.messageContainer, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text       = $"*{text}*";
            tmp.fontSize   = 24f;
            tmp.color      = StatusTextColor;
            tmp.fontStyle  = FontStyles.Italic;
            tmp.alignment  = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollToBottom();
        }

        public void SetThinking(bool thinking)
        {
            if (thinking)
            {
                if (_thinkingBubble != null) return;
                var go = new GameObject("Thinking", typeof(RectTransform));
                go.transform.SetParent(_b.messageContainer, false);

                var bg  = go.AddComponent<Image>();
                bg.color = AssistantBubbleBg;

                var rt  = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(120f, 50f);

                var tmp = new GameObject("Dots", typeof(RectTransform));
                tmp.transform.SetParent(go.transform, false);
                var drt = tmp.GetComponent<RectTransform>();
                drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
                drt.offsetMin = new Vector2(10, 5); drt.offsetMax = new Vector2(-10, -5);
                _thinkingBubble = tmp.AddComponent<TextMeshProUGUI>();
                _thinkingBubble.text     = "…";
                _thinkingBubble.fontSize = 28f;
                _thinkingBubble.color    = Color.white;
                _thinkingBubble.alignment = TextAlignmentOptions.Left;

                StartCoroutine(AnimateDots(_thinkingBubble));
                ScrollToBottom();
            }
            else
            {
                ClearThinking();
            }
        }

        // ---- Internals ----------------------------------------------------------

        private void ClearThinking()
        {
            if (_thinkingBubble != null)
            {
                Destroy(_thinkingBubble.transform.parent.gameObject);
                _thinkingBubble = null;
            }
        }

        private void AddBubble(string text, Color bg, TextAlignmentOptions alignment, bool isRight)
        {
            var bubble = new GameObject("Bubble", typeof(RectTransform));
            bubble.transform.SetParent(_b.messageContainer, false);

            var csf = bubble.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var hlg = bubble.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment        = isRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = true;
            hlg.padding               = new RectOffset(10, 10, 8, 8);

            var img = bubble.AddComponent<Image>();
            img.color = bg;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(bubble.transform, false);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text             = text;
            tmp.fontSize         = 28f;
            tmp.color            = Color.white;
            tmp.alignment        = alignment;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            var txtCsf = textGo.AddComponent<ContentSizeFitter>();
            txtCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void ScrollToBottom()
        {
            if (_b.chatScroll == null) return;
            // Rebuild layout before scrolling so the content height is current.
            Canvas.ForceUpdateCanvases();
            _b.chatScroll.normalizedPosition = new Vector2(0f, 0f);
        }

        private static IEnumerator AnimateDots(TextMeshProUGUI label)
        {
            string[] frames = { ".", "..", "…" };
            int i = 0;
            while (label != null)
            {
                if (label != null) label.text = frames[i % frames.Length];
                i++;
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
