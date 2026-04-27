// Assets/Scripts/UI/TapIndicator.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.UI
{
    /// <summary>
    /// Self-contained on-screen feedback for taps. Creates its own
    /// Screen Space Overlay canvas at runtime — no scene wiring needed.
    ///
    /// Usage from anywhere:
    ///     TapIndicator.Show(screenPos);
    ///     TapIndicator.Show(screenPos, Color.red);
    ///
    /// Diagnostic intent:
    ///   - Ring appears = the tap reached the script that called Show().
    ///   - Ring does not appear = the tap never made it to that code path.
    ///
    /// Color convention used by TapToPlace:
    ///   white   - tap detected (press began)
    ///   red     - tap blocked by UI (a Selectable was hit)
    ///   orange  - tap blocked by an existing 3D interactable (e.g. a cube)
    ///   yellow  - tap was a "real" tap but no plane was hit by the AR raycast
    ///   green   - placement succeeded
    /// </summary>
    public class TapIndicator : MonoBehaviour
    {
        // ---------- Static facade ----------------------------------------------
        public static TapIndicator Instance { get; private set; }

        public static void Show(Vector2 screenPos, Color? color = null)
        {
            // Disabled per user request — no on-screen ring on tap. Method kept as a
            // no-op so existing TapToPlace.PlaceAt() / Update() call sites compile.
            return;
        }

        private static void EnsureInstance()
        {
            if (Instance != null) return;
            // Don't try to create at editor-time / before any scene loaded.
            if (!Application.isPlaying) return;

            var go = new GameObject("[TapIndicator]");
            DontDestroyOnLoad(go);
            go.AddComponent<TapIndicator>();
        }

        // ---------- Tunables ----------------------------------------------------
        [SerializeField] private float duration  = 0.45f;
        [SerializeField] private float startSize = 80f;
        [SerializeField] private float endSize   = 260f;

        // ---------- Internals ---------------------------------------------------
        private Canvas _canvas;
        private static Sprite s_ring;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32000; // on top of everything

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            // Deliberately do NOT add a GraphicRaycaster — the indicator must not
            // absorb touches, otherwise it would itself be the bug it's diagnosing.
        }

        private void ShowInternal(Vector2 screenPos, Color color)
        {
            StartCoroutine(Animate(screenPos, color));
        }

        private IEnumerator Animate(Vector2 screenPos, Color color)
        {
            var ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(_canvas.transform, false);

            var rt = ringGo.AddComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.zero;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = screenPos;

            var img = ringGo.AddComponent<Image>();
            img.sprite        = GetRingSprite();
            img.raycastTarget = false;
            img.color         = color;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);

                float size = Mathf.Lerp(startSize, endSize, k);
                rt.sizeDelta = new Vector2(size, size);

                var c = color;
                c.a = Mathf.Lerp(1f, 0f, k);
                img.color = c;

                yield return null;
            }
            Destroy(ringGo);
        }

        // Procedural ring sprite. Generated once and cached.
        private static Sprite GetRingSprite()
        {
            if (s_ring != null) return s_ring;

            const int   tex   = 128;
            const float outer = 60f;
            const float inner = 50f;

            var t = new Texture2D(tex, tex, TextureFormat.RGBA32, false)
            {
                wrapMode   = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color[tex * tex];
            float c = tex * 0.5f;
            for (int y = 0; y < tex; y++)
            {
                for (int x = 0; x < tex; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d  = Mathf.Sqrt(dx * dx + dy * dy);
                    // Anti-aliased band between [inner, outer].
                    float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner);
                    px[y * tex + x] = new Color(1f, 1f, 1f, a);
                }
            }
            t.SetPixels(px);
            t.Apply(false, true);

            s_ring = Sprite.Create(t, new Rect(0, 0, tex, tex), new Vector2(0.5f, 0.5f), 100f);
            s_ring.name = "TapIndicatorRing";
            return s_ring;
        }
    }
}
