using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace PeriodicAR.Offline
{
    /// <summary>
    /// Periodically pings the LLM server to track connectivity.
    /// Writes result to AppStateProvider.offlineMode.
    /// </summary>
    public class NetworkSentinel : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<NetworkSentinel>() != null) return;
            var go = new GameObject("[NetworkSentinel]");
            DontDestroyOnLoad(go);
            go.AddComponent<NetworkSentinel>();
        }

        public static NetworkSentinel Instance { get; private set; }

        private const string PingUrl     = "https://ai.gonzalezerik.com/v1/models";
        private const float  PingInterval = 30f;

        public bool IsOnline { get; private set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start() => StartCoroutine(PingLoop());

        private IEnumerator PingLoop()
        {
            while (true)
            {
                yield return StartCoroutine(Ping());
                yield return new WaitForSeconds(PingInterval);
            }
        }

        private IEnumerator Ping()
        {
            using var req = UnityWebRequest.Head(PingUrl);
            req.timeout = 5;
            yield return req.SendWebRequest();

            bool online = req.result == UnityWebRequest.Result.Success;
            if (online != IsOnline)
            {
                IsOnline = online;
                if (Tutor.AppStateProvider.Instance != null)
                    Tutor.AppStateProvider.Instance.offlineMode = !online;

                string msg = online ? "Connection restored" : "Offline mode — AI features unavailable";
                UI.AppShellController.Instance?.ShowStatus(msg, 4f);
            }
        }

        /// <summary>Force a connectivity check immediately.</summary>
        public void CheckNow() => StartCoroutine(Ping());
    }
}
