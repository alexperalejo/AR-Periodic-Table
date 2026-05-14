using PeriodicAR.AR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.UI
{
    /// <summary>
    /// Drives the Lock/Unlock button in ARHudCanvas. Toggles
    /// <see cref="TapToPlace.LockTable"/> / <see cref="TapToPlace.UnlockTable"/>
    /// and updates the button label and tint to reflect the current lock state.
    ///
    /// The button is only interactable when a table has been placed. Before a table
    /// exists the button is greyed out so users can't lock an empty placement.
    ///
    /// Wiring: attach this component to the LockButton GameObject (the one with the
    /// UI Button component). Assign <see cref="tapToPlace"/> in the Inspector or it
    /// will be found at runtime via FindAnyObjectByType.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LockButtonController : MonoBehaviour
    {
        [Header("References (auto-discovered if null)")]
        [SerializeField] private TapToPlace tapToPlace;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image buttonImage;

        [Header("Labels")]
        [SerializeField] private string lockedLabel   = "Unlock";
        [SerializeField] private string unlockedLabel = "Lock";

        [Header("Tint")]
        [Tooltip("Tint applied to the button Image when the table is locked.")]
        [SerializeField] private Color lockedColor   = new Color(0.65f, 0.10f, 0.10f, 0.90f);
        [Tooltip("Tint applied to the button Image when the table is unlocked.")]
        [SerializeField] private Color unlockedColor = new Color(0.10f, 0.35f, 0.10f, 0.85f);

        private Button _btn;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            if (label == null)       label       = GetComponentInChildren<TMP_Text>(true);
            if (buttonImage == null) buttonImage = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (tapToPlace == null) tapToPlace = FindAnyObjectByType<TapToPlace>();

            if (_btn != null) _btn.onClick.AddListener(OnLockClicked);
            if (tapToPlace != null) tapToPlace.LockChanged += OnLockChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (_btn != null) _btn.onClick.RemoveListener(OnLockClicked);
            if (tapToPlace != null) tapToPlace.LockChanged -= OnLockChanged;
        }

        private void Update()
        {
            // Cheap polling — TapToPlace doesn't expose a PlacedChanged event, so we
            // refresh interactability whenever HasSpawned changes. The Refresh call is
            // idempotent and only mutates state when necessary.
            Refresh();
        }

        private void OnLockClicked()
        {
            if (tapToPlace == null)
            {
                Debug.LogWarning("[LockButtonController] No TapToPlace reference; ignoring click.");
                return;
            }
            if (!tapToPlace.HasSpawned)
            {
                Debug.LogWarning("[LockButtonController] No table placed yet — Lock button has no effect.");
                return;
            }

            if (tapToPlace.IsLocked) tapToPlace.UnlockTable();
            else                     tapToPlace.LockTable();
            // Refresh fires from LockChanged callback below.
        }

        private void OnLockChanged(bool isLocked)
        {
            Refresh();
            Debug.Log($"[LockButtonController] LockChanged → {(isLocked ? "LOCKED" : "UNLOCKED")}.");
        }

        private void Refresh()
        {
            bool hasTable = tapToPlace != null && tapToPlace.HasSpawned;
            bool isLocked = tapToPlace != null && tapToPlace.IsLocked;

            if (_btn != null && _btn.interactable != hasTable)
                _btn.interactable = hasTable;

            if (label != null)
                label.text = isLocked ? lockedLabel : unlockedLabel;

            if (buttonImage != null)
                buttonImage.color = isLocked ? lockedColor : unlockedColor;
        }
    }
}
