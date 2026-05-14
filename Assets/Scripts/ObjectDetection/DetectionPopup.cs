using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Logic for the world-space detection popup. Paired with DetectionPopupBindings for widget refs.
    /// Billboard-faces the camera every LateUpdate.
    /// </summary>
    [RequireComponent(typeof(DetectionPopupBindings))]
    public class DetectionPopup : MonoBehaviour
    {
        private DetectionPopupBindings _b;
        private Camera _faceCamera;
        private Action<string[]> _onHighlight;
        private Action           _onClosed;
        private string[]         _allSymbols;

        // Per-chip colours.
        private static readonly Color PrimaryChipBg  = new Color(0.15f, 0.45f, 0.85f, 1f);
        private static readonly Color TraceChipBg    = new Color(0.30f, 0.30f, 0.30f, 1f);
        private static readonly Color HighlightBtnBg = new Color(0.90f, 0.65f, 0.10f, 1f);
        private static readonly Color CloseBtnBg     = new Color(0.70f, 0.15f, 0.15f, 1f);

        private void Awake() => _b = GetComponent<DetectionPopupBindings>();

        public void Show(
            Detection            det,
            ObjectComposition    comp,           // may be null
            Vector3              worldPosition,
            Camera               faceCamera,
            Action<string[]>     onHighlightRequested,
            Action               onClosed)
        {
            transform.position = worldPosition;
            _faceCamera  = faceCamera;
            _onHighlight = onHighlightRequested;
            _onClosed    = onClosed;

            // Title and score.
            if (_b.titleText != null)
                _b.titleText.text = det.label.Length > 0
                    ? char.ToUpper(det.label[0]) + det.label.Substring(1)
                    : det.label;
            if (_b.scoreText != null)
                _b.scoreText.text = $"{det.score:P0}";

            // Element chips.
            PopulateChips(_b.primaryContainer, comp?.primary, PrimaryChipBg);
            PopulateChips(_b.traceContainer,   comp?.trace,   TraceChipBg);

            // Note.
            if (_b.noteText != null)
            {
                _b.noteText.text = comp?.note ?? string.Empty;
                _b.noteText.gameObject.SetActive(!string.IsNullOrEmpty(comp?.note));
            }

            // Collect all symbols for the highlight button.
            var primary = comp?.primary ?? Array.Empty<string>();
            var trace   = comp?.trace   ?? Array.Empty<string>();
            _allSymbols = primary.Concat(trace).ToArray();

            // Buttons.
            if (_b.highlightButton != null)
            {
                _b.highlightButton.gameObject.SetActive(_allSymbols.Length > 0);
                _b.highlightButton.onClick.RemoveAllListeners();
                _b.highlightButton.onClick.AddListener(() => _onHighlight?.Invoke(_allSymbols));
            }
            if (_b.closeButton != null)
            {
                _b.closeButton.onClick.RemoveAllListeners();
                _b.closeButton.onClick.AddListener(() =>
                {
                    _onClosed?.Invoke();
                    Destroy(gameObject);
                });
            }

            gameObject.SetActive(true);
        }

        private void PopulateChips(Transform container, string[] symbols, Color bgColor)
        {
            if (container == null) return;

            // Clear previous chips.
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            if (symbols == null || symbols.Length == 0) return;

            foreach (var sym in symbols)
            {
                var chip    = new GameObject($"Chip_{sym}", typeof(RectTransform));
                chip.transform.SetParent(container, false);

                var img     = chip.AddComponent<Image>();
                img.color   = bgColor;
                img.raycastTarget = false;

                var rt      = chip.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(80f, 60f);

                var label   = new GameObject("Label", typeof(RectTransform));
                label.transform.SetParent(chip.transform, false);
                var tmp     = label.AddComponent<TextMeshProUGUI>();
                tmp.text    = sym;
                tmp.fontSize = 32f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color   = Color.white;
                tmp.raycastTarget = false;

                var lr      = label.GetComponent<RectTransform>();
                lr.anchorMin = Vector2.zero;
                lr.anchorMax = Vector2.one;
                lr.offsetMin = Vector2.zero;
                lr.offsetMax = Vector2.zero;
            }
        }

        private void LateUpdate()
        {
            if (_faceCamera == null) return;
            Vector3 toCamera = transform.position - _faceCamera.transform.position;
            if (toCamera.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
        }
    }
}
