using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace PeriodicAR.AR
{
    public class ARPlaneDetectionWatcher : MonoBehaviour
    {
        [SerializeField] private ARPlaneManager planeManager;

        public bool HasAnyHorizontalPlane { get; private set; }
        public event Action<bool> PlaneAvailabilityChanged;

        private void OnEnable()
        {
            if (planeManager == null) return;
            planeManager.trackablesChanged.AddListener(OnTrackablesChanged);
            Recompute();
        }

        private void OnDisable()
        {
            if (planeManager == null) return;
            planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> _)
        {
            Recompute();
        }

        private void Recompute()
        {
            bool found = false;
            foreach (var plane in planeManager.trackables)
            {
                if (plane == null) continue;
                if (plane.alignment == PlaneAlignment.HorizontalUp || plane.alignment == PlaneAlignment.HorizontalDown)
                {
                    found = true;
                    break;
                }
            }

            if (found != HasAnyHorizontalPlane)
            {
                HasAnyHorizontalPlane = found;
                PlaneAvailabilityChanged?.Invoke(found);
            }
        }
    }
}
