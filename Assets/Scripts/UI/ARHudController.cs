using PeriodicAR.AR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.UI
{
    public class ARHudController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TapToPlace tapToPlace;
        [SerializeField] private ARPlaneDetectionWatcher planeWatcher;

        [Header("Place / Remove button")]
        [SerializeField] private Button placeButton;
        [SerializeField] private TMP_Text placeButtonLabel;
        [Tooltip("Label when a press will arm placement.")]
        [SerializeField] private string placeLabel = "Place";
        [Tooltip("Label when a press will remove the table.")]
        [SerializeField] private string removeLabel = "X";

        [Header("Instruction overlay")]
        [SerializeField] private GameObject instructionRoot;
        [SerializeField] private TMP_Text instructionLabel;
        [SerializeField] private string instructionText = "Tap a detected horizontal plane to place the table";

        private void OnEnable()
        {
            // The HUD prefab can be dropped into a scene without inspector wiring.
            // Auto-discover the AR rig components so the Place button and instruction
            // overlay still work even if the references were not set in the Inspector.
            if (tapToPlace == null) tapToPlace = FindAnyObjectByType<TapToPlace>();
            if (planeWatcher == null) planeWatcher = FindAnyObjectByType<ARPlaneDetectionWatcher>();
            if (tapToPlace == null) Debug.LogWarning("[ARHudController] No TapToPlace found in scene — Place button will be inert.");
            if (planeWatcher == null) Debug.LogWarning("[ARHudController] No ARPlaneDetectionWatcher found in scene — Place button will not gate on plane availability.");

            if (placeButton != null) placeButton.onClick.AddListener(OnPlaceButtonClicked);
            if (tapToPlace != null) tapToPlace.StateChanged += OnTapToPlaceStateChanged;
            if (planeWatcher != null) planeWatcher.PlaneAvailabilityChanged += OnPlaneAvailabilityChanged;
            RefreshHud();
        }

        private void OnDisable()
        {
            if (placeButton != null) placeButton.onClick.RemoveListener(OnPlaceButtonClicked);
            if (tapToPlace != null) tapToPlace.StateChanged -= OnTapToPlaceStateChanged;
            if (planeWatcher != null) planeWatcher.PlaneAvailabilityChanged -= OnPlaneAvailabilityChanged;
        }

        private void OnPlaceButtonClicked()
        {
            if (tapToPlace == null) return;
            switch (tapToPlace.CurrentState)
            {
                case TapToPlace.State.Idle:
                    tapToPlace.ArmPlacement();
                    break;
                case TapToPlace.State.Armed:
                    tapToPlace.CancelPlacement();
                    break;
                case TapToPlace.State.Placed:
                    tapToPlace.RemoveTable();
                    break;
            }
        }

        private void OnTapToPlaceStateChanged(TapToPlace.State _) => RefreshHud();
        private void OnPlaneAvailabilityChanged(bool _) => RefreshHud();

        private void RefreshHud()
        {
            var state = tapToPlace != null ? tapToPlace.CurrentState : TapToPlace.State.Idle;
            bool planeAvailable = planeWatcher == null || planeWatcher.HasAnyHorizontalPlane;

            if (placeButton != null)
            {
                bool interactable = state switch
                {
                    TapToPlace.State.Idle   => planeAvailable,
                    TapToPlace.State.Armed  => true,
                    TapToPlace.State.Placed => true,
                    _ => false,
                };
                placeButton.interactable = interactable;
            }

            if (placeButtonLabel != null)
            {
                placeButtonLabel.text = state == TapToPlace.State.Placed ? removeLabel : placeLabel;
            }

            if (instructionRoot != null)
            {
                instructionRoot.SetActive(state == TapToPlace.State.Armed);
            }
            if (instructionLabel != null)
            {
                instructionLabel.text = instructionText;
            }
        }
    }
}
