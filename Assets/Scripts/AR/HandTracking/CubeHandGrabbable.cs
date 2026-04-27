using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PeriodicAR.AR.HandTracking
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class CubeHandGrabbable : MonoBehaviour
    {
        // Events for outside listeners (e.g. ElementPopupOnGrab) to react to hand-pinch
        // grab/release on this cube.
        public event Action Grabbed;
        public event Action Released;
        [SerializeField] private float respawnDelaySeconds = 1.0f;

        private Vector3 _homeLocalPosition;
        private Quaternion _homeLocalRotation;
        private Transform _homeParent;
        private Rigidbody _rb;
        private XRGrabInteractable _touchGrab;
        private bool _handGrabbed;
        private bool _falling;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _touchGrab = GetComponent<XRGrabInteractable>();
            _homeLocalPosition = transform.localPosition;
            _homeLocalRotation = transform.localRotation;
            _homeParent = transform.parent;
        }

        public bool IsAvailableForHandGrab => !_handGrabbed && !_falling;

        public void BeginHandGrab()
        {
            if (!IsAvailableForHandGrab) return;
            _handGrabbed = true;
            if (_touchGrab != null) _touchGrab.enabled = false;
            _rb.isKinematic = true;
            _rb.useGravity = false;
            transform.SetParent(null, worldPositionStays: true);
            Grabbed?.Invoke();
        }

        public void UpdateHandGrab(Vector3 worldPos)
        {
            if (!_handGrabbed) return;
            // Snappier follow so the cube doesn't visibly lag behind the palm.
            transform.position = Vector3.Lerp(transform.position, worldPos, 0.55f);
        }

        public void ReleaseHandGrab()
        {
            if (!_handGrabbed) return;
            _handGrabbed = false;
            _falling = true;
            _rb.isKinematic = false;
            _rb.useGravity = true;
            Released?.Invoke();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_falling) return;
            if (collision.collider.GetComponentInParent<ARPlane>() == null
                && !collision.gameObject.CompareTag("ARPlane"))
            {
                if (!collision.rigidbody || !collision.rigidbody.isKinematic == false) return;
            }
            StartCoroutine(DespawnAndRespawn());
        }

        private System.Collections.IEnumerator DespawnAndRespawn()
        {
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            _rb.isKinematic = true;
            _rb.useGravity = false;
            yield return new WaitForSeconds(respawnDelaySeconds);

            transform.SetParent(_homeParent, worldPositionStays: false);
            transform.localPosition = _homeLocalPosition;
            transform.localRotation = _homeLocalRotation;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
            _falling = false;
            if (_touchGrab != null) _touchGrab.enabled = true;
        }
    }
}
