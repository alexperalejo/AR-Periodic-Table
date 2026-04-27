using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace PeriodicAR.AR.HandTracking
{
    public class HandCapabilityProbe : MonoBehaviour
    {
        [SerializeField] private AROcclusionManager occlusionManager;

        public enum Result { Unknown, Supported, NotSupported }
        public Result Status { get; private set; } = Result.Unknown;
        public event Action<Result> ProbeCompleted;

        private float _timeAccumulated;
        private const float ProbeTimeoutSeconds = 2.0f;

        private void Start()
        {
            if (Application.isEditor)
            {
                FinishProbe(Result.NotSupported);
                return;
            }
            if (occlusionManager == null)
            {
                Debug.LogWarning("[HandCapabilityProbe] No AROcclusionManager assigned; assuming unsupported.");
                FinishProbe(Result.NotSupported);
                return;
            }
        }

        private void Update()
        {
            if (Status != Result.Unknown) return;
            if (occlusionManager == null) return;

            _timeAccumulated += Time.deltaTime;

            var descriptor = occlusionManager.descriptor;
            if (descriptor != null && descriptor.environmentDepthImageSupported == Supported.Supported)
            {
                FinishProbe(Result.Supported);
                return;
            }
            if (descriptor != null && descriptor.environmentDepthImageSupported == Supported.Unsupported)
            {
                FinishProbe(Result.NotSupported);
                return;
            }
            if (_timeAccumulated > ProbeTimeoutSeconds)
            {
                FinishProbe(Result.NotSupported);
            }
        }

        private void FinishProbe(Result r)
        {
            Status = r;
            Debug.Log($"[HandCapabilityProbe] LiDAR environment depth: {r}");
            ProbeCompleted?.Invoke(r);
        }
    }
}
