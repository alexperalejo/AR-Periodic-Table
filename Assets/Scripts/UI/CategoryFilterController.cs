// Assets/Scripts/UI/CategoryFilterController.cs
using System.Collections.Generic;
using PeriodicAR.AR;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.UI
{
    /// <summary>
    /// Lives on the FilterBar. Forwards button clicks to the current pTableGroup instance's
    /// ElementCategoryHighlighter. Auto-wires its child Buttons by GameObject name on
    /// OnEnable, so you don't have to set up UnityEvents manually in the prefab.
    /// </summary>
    public class CategoryFilterController : MonoBehaviour
    {
        [SerializeField] private TapToPlace placer;

        // GameObject name → highlighter button key. Names match the FilterBar's children
        // in ARHudCanvas.prefab.
        private static readonly Dictionary<string, string> s_NameToKey = new()
        {
            { "Alkali_Metals",     "alkali"        },
            { "Alkaline_Earth",    "alkaline"      },
            { "Lanthanoids",       "lanthanoid"    },
            { "Actinoids",         "actinoid"      },
            { "Transition_Metals", "transition"    },
            { "PostTrans_Metals",  "posttransition"},
            { "Metalloids",        "metalloid"     },
            { "Halogens",          "halogen"       },
            { "Noble_Gases",       "noblegas"      },
            { "Other_Nonmetals",   "othernonmetal" },
        };

        // Active-state visuals — RED when a category is on, restore original when off.
        private static readonly Color ActiveColor = new Color(0.90f, 0.20f, 0.20f, 1.00f);
        private string _activeKey;
        private readonly Dictionary<string, Button> _buttonRefs     = new();
        private readonly Dictionary<string, Color>  _originalColors = new();

        private void OnEnable()
        {
            if (placer == null) placer = FindAnyObjectByType<TapToPlace>();
            AutoWireButtons();
            SyncActiveStateFromHighlighter();
            UpdateButtonVisuals();
        }

        private void AutoWireButtons()
        {
            _buttonRefs.Clear();
            _originalColors.Clear();

            int wired = 0;
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (!s_NameToKey.TryGetValue(btn.name, out string key)) continue;
                string localKey = key;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnButtonPressed(localKey));

                _buttonRefs[key] = btn;
                var img = btn.GetComponent<Image>() ?? btn.targetGraphic as Image;
                if (img != null) _originalColors[key] = img.color;
                wired++;
            }
            Debug.Log($"[CategoryFilterController] Auto-wired {wired} category buttons.");
        }

        private void OnButtonPressed(string key)
        {
            Debug.Log($"[CategoryFilterController] Pressed '{key}'.");
            var highlighter = CurrentHighlighter();
            if (highlighter == null)
            {
                Debug.LogWarning("[CategoryFilterController] No highlighter — table not spawned yet?");
                return;
            }
            highlighter.Highlight(key);                 // toggles internally
            _activeKey = highlighter.ActiveButton;       // null if just toggled off
            UpdateButtonVisuals();
        }

        private void SyncActiveStateFromHighlighter()
        {
            var h = CurrentHighlighter();
            _activeKey = h != null ? h.ActiveButton : null;
        }

        private void UpdateButtonVisuals()
        {
            foreach (var kv in _buttonRefs)
            {
                var btn = kv.Value;
                if (btn == null) continue;
                var img = btn.GetComponent<Image>() ?? btn.targetGraphic as Image;
                if (img == null) continue;

                bool isActive = kv.Key == _activeKey;
                if (isActive)
                {
                    img.color = ActiveColor;
                }
                else if (_originalColors.TryGetValue(kv.Key, out var c))
                {
                    img.color = c;
                }
            }
        }

        private ElementCategoryHighlighter CurrentHighlighter()
        {
            if (placer == null) placer = FindAnyObjectByType<TapToPlace>();
            if (placer == null || placer.CurrentInstance == null) return null;
            return placer.CurrentInstance.GetComponent<ElementCategoryHighlighter>()
                ?? placer.CurrentInstance.AddComponent<ElementCategoryHighlighter>();
        }

        // Public methods kept for back-compat in case anyone still has them wired in
        // an Inspector UnityEvent. AutoWireButtons replaces them at runtime.
        public void OnAlkaliMetals()        => CurrentHighlighter()?.Highlight("alkali");
        public void OnAlkalineEarthMetals() => CurrentHighlighter()?.Highlight("alkaline");
        public void OnLanthanoids()         => CurrentHighlighter()?.Highlight("lanthanoid");
        public void OnActinoids()           => CurrentHighlighter()?.Highlight("actinoid");
        public void OnTransitionMetals()    => CurrentHighlighter()?.Highlight("transition");
        public void OnPostTransitionMetals()=> CurrentHighlighter()?.Highlight("posttransition");
        public void OnMetalloid()           => CurrentHighlighter()?.Highlight("metalloid");
        public void OnHalogens()            => CurrentHighlighter()?.Highlight("halogen");
        public void OnNobleGases()          => CurrentHighlighter()?.Highlight("noblegas");
        public void OnOtherNonMetals()      => CurrentHighlighter()?.Highlight("othernonmetal");
        public void OnAll()                 => CurrentHighlighter()?.ClearHighlight();
    }
}
