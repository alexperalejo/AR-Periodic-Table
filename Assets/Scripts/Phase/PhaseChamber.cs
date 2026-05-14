using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Phase
{
    [RequireComponent(typeof(PhaseChamberBindings))]
    public class PhaseChamber : MonoBehaviour
    {
        private PhaseChamberBindings _b;
        private MoleculeSim          _sim;
        private SubstancePhaseDiagram _substance;
        private float _temperature = 298f;  // Kelvin
        private float _pressure    = 1f;    // atm
        private PhaseMode _currentPhase = PhaseMode.Liquid;

        private void Awake() => _b = GetComponent<PhaseChamberBindings>();

        public void Init()
        {
            var keys = PhaseDiagramData.GetKeys();
            var names = new List<string>();
            foreach (var k in keys) names.Add(k);
            if (_b.substanceDropdown)
            {
                _b.substanceDropdown.ClearOptions();
                _b.substanceDropdown.AddOptions(names);
                _b.substanceDropdown.onValueChanged.AddListener(OnSubstanceChanged);
            }

            if (_b.tempSlider)
            {
                _b.tempSlider.minValue = 1f;
                _b.tempSlider.maxValue = 500f;
                _b.tempSlider.value    = 298f;
                _b.tempSlider.onValueChanged.AddListener(OnTempChanged);
            }

            if (_b.pressureSlider)
            {
                _b.pressureSlider.minValue = 0.001f;
                _b.pressureSlider.maxValue = 1000f;
                _b.pressureSlider.value    = 1f;
                _b.pressureSlider.onValueChanged.AddListener(OnPressureChanged);
            }

            SetSubstance("H2O");
        }

        public void SetSubstance(string key)
        {
            _substance = PhaseDiagramData.Get(key);
            if (_substance == null) return;

            Color color = Color.cyan;
            ColorUtility.TryParseHtmlString(_substance.moleculeColor, out color);

            if (_sim != null) Destroy(_sim.gameObject);
            if (_b.simContainer != null)
            {
                var simGo = new GameObject("MoleculeSim", typeof(RectTransform));
                simGo.transform.SetParent(_b.simContainer, false);
                var simRt = simGo.GetComponent<RectTransform>();
                simRt.anchorMin = Vector2.zero; simRt.anchorMax = Vector2.one;
                simRt.offsetMin = simRt.offsetMax = Vector2.zero;
                _sim = simGo.AddComponent<MoleculeSim>();
                _sim.Init(_b.simContainer, color, _substance.moleculeRadius * 1000f);
            }

            UpdatePhase();
        }

        public void SetConditions(float tempK, float pressureAtm)
        {
            _temperature = tempK;
            _pressure    = pressureAtm;
            if (_b.tempSlider)     _b.tempSlider.SetValueWithoutNotify(tempK);
            if (_b.pressureSlider) _b.pressureSlider.SetValueWithoutNotify(pressureAtm);
            UpdatePhase();
        }

        private void OnSubstanceChanged(int idx)
        {
            var keys = PhaseDiagramData.GetKeys();
            if (idx < keys.Count) SetSubstance(keys[idx]);
        }

        private void OnTempChanged(float val)
        {
            _temperature = val;
            UpdatePhase();
        }

        private void OnPressureChanged(float val)
        {
            _pressure = val;
            UpdatePhase();
        }

        private void UpdatePhase()
        {
            _currentPhase = PhaseDiagramData.Classify(_substance, _temperature, _pressure);

            if (_sim != null)
            {
                _sim.Temperature = _temperature;
                _sim.Pressure    = _pressure;
                _sim.Mode        = _currentPhase;
            }

            float tempC = _temperature - 273.15f;
            if (_b.phaseLabel)    _b.phaseLabel.text    = $"{_currentPhase}  ·  {tempC:F0} °C  ·  {_pressure:F2} atm";
            if (_b.tempLabel)     _b.tempLabel.text     = $"T = {_temperature:F0} K ({tempC:F0} °C)";
            if (_b.pressureLabel) _b.pressureLabel.text = $"P = {_pressure:F2} atm";

            if (_b.annotationText)
            {
                bool showIceFloat = _substance != null && _substance.iceFloats
                    && _currentPhase == PhaseMode.Solid && _temperature < 277f;
                _b.annotationText.text = showIceFloat ? "⬆ Ice is less dense than liquid water — that's why it floats!" : "";
                _b.annotationText.gameObject.SetActive(showIceFloat);
            }

            PushState();
        }

        private void PushState()
        {
            if (Tutor.AppStateProvider.Instance == null) return;
            var sp = Tutor.AppStateProvider.Instance;
            sp.activePhaseSubstance = _substance?.formula;
            sp.currentPhase         = _currentPhase.ToString().ToLower();
        }
    }
}
