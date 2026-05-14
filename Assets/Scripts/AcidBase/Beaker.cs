using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.AcidBase
{
    [RequireComponent(typeof(BeakerBindings))]
    public class Beaker : MonoBehaviour
    {
        private BeakerBindings _b;
        private float          _currentPh = 7f;
        private string         _currentIndicator = "Universal";
        private SubstanceData  _currentSubstance;

        private void Awake() => _b = GetComponent<BeakerBindings>();

        public void Init()
        {
            var substances = AcidBaseCatalog.GetSubstances();
            var indicators = AcidBaseCatalog.GetIndicators();

            if (_b.substanceDropdown)
            {
                var names = new List<string>();
                foreach (var s in substances) names.Add(s.name);
                _b.substanceDropdown.ClearOptions();
                _b.substanceDropdown.AddOptions(names);
                _b.substanceDropdown.onValueChanged.AddListener(OnSubstanceChanged);
            }

            if (_b.indicatorDropdown)
            {
                var names = new List<string>();
                foreach (var i in indicators) names.Add(i.name);
                _b.indicatorDropdown.ClearOptions();
                _b.indicatorDropdown.AddOptions(names);
                _b.indicatorDropdown.onValueChanged.AddListener(OnIndicatorChanged);
            }

            if (_b.phSlider)
            {
                _b.phSlider.minValue = 0f;
                _b.phSlider.maxValue = 14f;
                _b.phSlider.value    = 7f;
                _b.phSlider.onValueChanged.AddListener(SetPh);
            }

            if (_b.neutralizeButton) _b.neutralizeButton.onClick.AddListener(Neutralize);
            if (_b.clearButton)      _b.clearButton.onClick.AddListener(() => SetPh(7f));

            SetPh(7f);
        }

        public void SetSubstance(SubstanceData sub)
        {
            _currentSubstance = sub;
            SetPh(sub.ph);
            if (_b.substanceLabel)    _b.substanceLabel.text    = sub.formula;
            if (_b.descriptionText)   _b.descriptionText.text   = sub.description;
        }

        public void SetPh(float ph)
        {
            _currentPh = Mathf.Clamp(ph, 0f, 14f);
            if (_b.phSlider) _b.phSlider.SetValueWithoutNotify(_currentPh);
            UpdateVisuals();
            PushState();
        }

        private void OnSubstanceChanged(int idx)
        {
            var list = AcidBaseCatalog.GetSubstances();
            if (idx < list.Count) SetSubstance(list[idx]);
        }

        private void OnIndicatorChanged(int idx)
        {
            var list = AcidBaseCatalog.GetIndicators();
            if (idx < list.Count) _currentIndicator = list[idx].name;
            UpdateVisuals();
        }

        private void Neutralize()
        {
            StopAllCoroutines();
            StartCoroutine(AnimateToNeutral());
        }

        private IEnumerator AnimateToNeutral()
        {
            float start = _currentPh;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.6f;
                SetPh(Mathf.Lerp(start, 7f, t));
                yield return null;
            }
            SetPh(7f);
        }

        private void UpdateVisuals()
        {
            Color c = AcidBaseCatalog.GetBeakerColor(_currentPh, _currentIndicator);

            if (_b.liquidImage) _b.liquidImage.color = new Color(c.r, c.g, c.b, 0.75f);

            if (_b.phDisplay)
                _b.phDisplay.text = $"pH {_currentPh:F1}\n<size=70%>{AcidBaseCatalog.ClassifyPh(_currentPh)}</size>";

            if (_b.phBar)
            {
                // Show a color strip — map pH 0-14 to the full bar
                float t = _currentPh / 14f;
                _b.phBar.fillAmount = t;
                _b.phBar.color = c;
            }
        }

        private void PushState()
        {
            if (Tutor.AppStateProvider.Instance == null) return;
            var sp = Tutor.AppStateProvider.Instance;
            sp.beakerState = AcidBaseCatalog.ClassifyPh(_currentPh);
            sp.currentPh   = _currentPh;
        }
    }
}
