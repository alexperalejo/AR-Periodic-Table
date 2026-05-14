using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Reactions
{
    /// <summary>
    /// Screen-space reaction sandbox panel. Two slots that capture the currently
    /// held element from AppStateProvider, plus an Ignite button.
    /// </summary>
    [RequireComponent(typeof(ReactionTrayBindings))]
    public class ReactionTray : MonoBehaviour
    {
        private ReactionTrayBindings _b;

        public string SlotA { get; private set; }
        public string SlotB { get; private set; }
        public bool IsOpen  => _b.panelRoot != null && _b.panelRoot.activeSelf;

        public System.Action          OnIgniteRequested;
        public System.Action          OnClose;
        public System.Action          OnSkipRequested;

        private void Awake() => _b = GetComponent<ReactionTrayBindings>();

        private void Start()
        {
            _b.slotAButton.onClick.AddListener(CaptureSlotA);
            _b.slotBButton.onClick.AddListener(CaptureSlotB);
            _b.igniteButton.onClick.AddListener(() => OnIgniteRequested?.Invoke());
            _b.closeButton.onClick.AddListener(() => { Close(); OnClose?.Invoke(); });
            _b.skipButton.onClick.AddListener(() => OnSkipRequested?.Invoke());
            RefreshLabels();
        }

        public void Open()
        {
            if (_b.panelRoot != null) _b.panelRoot.SetActive(true);
            ClearResult();
            RefreshLabels();
        }

        public void Close()
        {
            if (_b.panelRoot != null) _b.panelRoot.SetActive(false);
        }

        public void ShowResult(string text)
        {
            if (_b.resultText != null) _b.resultText.text = text;
        }

        public void ClearResult()
        {
            if (_b.resultText != null) _b.resultText.text = string.Empty;
        }

        public void SetAnimating(bool animating)
        {
            if (_b.igniteButton != null) _b.igniteButton.interactable = !animating;
            if (_b.skipButton   != null) _b.skipButton.gameObject.SetActive(animating);
        }

        private void CaptureSlotA()
        {
            var sym = Tutor.AppStateProvider.Instance?.heldElementSymbol;
            if (!string.IsNullOrEmpty(sym)) { SlotA = sym; RefreshLabels(); ClearResult(); }
        }

        private void CaptureSlotB()
        {
            var sym = Tutor.AppStateProvider.Instance?.heldElementSymbol;
            if (!string.IsNullOrEmpty(sym)) { SlotB = sym; RefreshLabels(); ClearResult(); }
        }

        private void RefreshLabels()
        {
            if (_b.slotALabel != null) _b.slotALabel.text = string.IsNullOrEmpty(SlotA) ? "Slot A\n(grab element)" : $"A: {SlotA}";
            if (_b.slotBLabel != null) _b.slotBLabel.text = string.IsNullOrEmpty(SlotB) ? "Slot B\n(grab element)" : $"B: {SlotB}";
            if (_b.igniteButton != null) _b.igniteButton.interactable = !string.IsNullOrEmpty(SlotA) && !string.IsNullOrEmpty(SlotB);
        }
    }
}
