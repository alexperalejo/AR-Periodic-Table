using UnityEngine;

namespace PeriodicAR.Debugging
{
    /// <summary>
    /// Logs every time this GameObject is enabled or disabled. Includes a full
    /// managed stack trace on disable so the script that called SetActive(false)
    /// can be identified. Attach to BohrModelRoot, BohrModelRoot/AtomSystem and
    /// ElementInfoCard.
    ///
    /// All logs are tagged "[HIDE TRACE]" for easy log filtering.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class HideWatcher : MonoBehaviour
    {
        [Tooltip("Also log when the object is enabled/shown (helpful for correlating hide/show pairs).")]
        public bool logEnable = true;

        private void OnEnable()
        {
            if (!logEnable) return;
            Debug.Log($"[HIDE TRACE] {name} ENABLED   activeSelf=true  activeInHierarchy={gameObject.activeInHierarchy}");
        }

        private void OnDisable()
        {
            // OnDisable fires both for direct SetActive(false) calls AND when an
            // ancestor was deactivated AND when the object is being destroyed during
            // shutdown. The stack trace tells you which.
            Debug.Log($"[HIDE TRACE] {name} DISABLED  activeSelf={gameObject.activeSelf}  activeInHierarchy={gameObject.activeInHierarchy}\n{System.Environment.StackTrace}");
        }
    }
}
