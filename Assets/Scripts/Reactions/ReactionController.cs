using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Reactions
{
    /// <summary>
    /// Orchestrates the Reaction Sandbox. Self-bootstrapping singleton.
    /// Adds a "Mix" button to the HUD at position (-640, -40).
    /// </summary>
    public class ReactionController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<ReactionController>() != null) return;
            var go = new GameObject("[ReactionController]");
            DontDestroyOnLoad(go);
            go.AddComponent<ReactionController>();
        }

        public static ReactionController Instance { get; private set; }

        private Button          _mixButton;
        private ReactionTray    _tray;
        private ReactionAnimator _animator;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (UI.AppShellController.Instance != null)
            {
                UI.AppShellController.Instance.RegisterWheelItem(new UI.WheelItem
                {
                    id    = "mix",
                    label = "Mix",
                    iconName = "⚗",
                    page  = 2,
                    onTap = ToggleMixPanel,
                });
            }
            else
            {
                CreateHudButton();
            }

            var panelGo = ReactionPrefabFactory.CreatePanel();
            DontDestroyOnLoad(panelGo);
            _tray = panelGo.GetComponent<ReactionTray>();
            _tray.OnIgniteRequested = OnIgnite;
            _tray.OnClose           = () => { };
            _tray.OnSkipRequested   = OnSkip;

            _animator = gameObject.AddComponent<ReactionAnimator>();
        }

        private void CreateHudButton()
        {
            Canvas hudCanvas = FindHudCanvas();
            if (hudCanvas == null) return;

            var btnGo = new GameObject("MixButton", typeof(RectTransform));
            btnGo.transform.SetParent(hudCanvas.transform, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-640f, -40f);
            rt.sizeDelta        = new Vector2(180f, 180f);

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.20f, 0.10f, 0.22f, 0.85f);
            _mixButton = btnGo.AddComponent<Button>();
            _mixButton.onClick.AddListener(OnMixPressed);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(btnGo.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4, 30); lrt.offsetMax = new Vector2(-4, -4);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "Mix";
            tmp.fontSize  = 40f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
        }

        public void ToggleMixPanel()
        {
            if (_tray == null) return;
            if (_tray.IsOpen) _tray.Close();
            else              _tray.Open();
        }

        private void OnMixPressed() => ToggleMixPanel();

        private void OnIgnite()
        {
            if (_tray == null) return;
            string a = _tray.SlotA;
            string b = _tray.SlotB;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return;
            StartCoroutine(RunReaction(a, b));
        }

        private void OnSkip()
        {
            if (_animator != null) _animator.Skip = true;
        }

        private IEnumerator RunReaction(string a, string b)
        {
            _tray.SetAnimating(true);
            _tray.ClearResult();

            ReactionCatalog.TryFind(a, b, out Reaction reaction);
            string animType = reaction?.animationType ?? "no_reaction";

            // Position animation in front of camera.
            var cam  = Camera.main;
            Vector3 center = cam != null
                ? cam.transform.position + cam.transform.forward * 0.5f
                : Vector3.zero;

            bool done = false;
            yield return _animator.Play(a, b, animType, center, () => done = true);
            while (!done) yield return null;

            // Show result.
            var sb = new StringBuilder();
            if (reaction == null)
            {
                sb.AppendLine($"<b>{a} + {b} → ?</b>");
                sb.AppendLine("No reaction found in the catalog for this pair.");
            }
            else if (reaction.animationType == "no_reaction")
            {
                sb.AppendLine($"<b>{a} + {b} → No reaction</b>");
                sb.AppendLine(reaction.description);
            }
            else
            {
                sb.AppendLine($"<b>{a} + {b} → {reaction.productFormula}</b>");
                sb.AppendLine(reaction.productName);
                sb.AppendLine();
                sb.AppendLine(reaction.description);
                string label = reaction.energy == "exothermic" ? "🔥 Exothermic (releases energy)"
                             : reaction.energy == "endothermic" ? "❄ Endothermic (absorbs energy)"
                             : "";
                if (!string.IsNullOrEmpty(label)) sb.AppendLine(label);
            }

            _tray.ShowResult(sb.ToString());
            _tray.SetAnimating(false);

            // Update AppStateProvider.
            if (Tutor.AppStateProvider.Instance != null && reaction != null)
                Tutor.AppStateProvider.Instance.lastReactionProduct = reaction.productFormula ?? $"no reaction ({a}+{b})";
        }

        private static Canvas FindHudCanvas()
        {
            var hudController = FindAnyObjectByType<UI.ARHudController>();
            Canvas c = hudController != null ? hudController.GetComponentInParent<Canvas>() : null;
            return c != null ? c : FindFirstObjectByType<Canvas>();
        }
    }
}
