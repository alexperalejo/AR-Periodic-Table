using UnityEngine;

namespace PeriodicAR.HalfLife
{
    /// <summary>
    /// Manages 1000 instanced spheres representing radioactive atoms.
    /// One loop per frame — no per-atom coroutines.
    /// </summary>
    public class AtomCloud : MonoBehaviour
    {
        private const int AtomCount = 1000;

        private GameObject[]  _atoms;
        private bool[]        _decayed;
        private Renderer[]    _renderers;
        private MaterialPropertyBlock _mpb;
        private static readonly int s_ColorId = Shader.PropertyToID("_BaseColor");

        private Color _parentColor;
        private Color _daughterColor;
        private double _halfLifeSeconds;

        private bool   _running;
        private float  _timeMultiplier = 1f;
        private double _simulatedSeconds;
        private int    _decayedCount;

        // Callbacks
        public System.Action<int, int, double> OnStatsChanged; // parent, decayed, simSeconds

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        public void Init(IsotopeData data, float timeMultiplier)
        {
            _halfLifeSeconds = data.halfLifeSeconds;
            _timeMultiplier  = timeMultiplier;
            _simulatedSeconds = 0;
            _decayedCount = 0;

            ColorUtility.TryParseHtmlString(data.parentColor,   out _parentColor);
            ColorUtility.TryParseHtmlString(data.daughterColor, out _daughterColor);

            BuildAtoms();
            SetAllColor(_parentColor);
        }

        public void SetTimeMultiplier(float mult) => _timeMultiplier = mult;

        public void StartSimulation()  => _running = true;
        public void PauseSimulation()  => _running = false;
        public void ResetSimulation()
        {
            _running = false;
            _simulatedSeconds = 0;
            _decayedCount = 0;
            for (int i = 0; i < AtomCount; i++) _decayed[i] = false;
            SetAllColor(_parentColor);
            OnStatsChanged?.Invoke(AtomCount, 0, 0);
        }

        public void JumpToFraction(float parentFraction)
        {
            _running = false;
            int targetDecayed = Mathf.RoundToInt((1f - parentFraction) * AtomCount);
            int current = _decayedCount;

            if (targetDecayed > current)
            {
                int toDecay = targetDecayed - current;
                int decayed = 0;
                for (int i = 0; i < AtomCount && decayed < toDecay; i++)
                {
                    if (!_decayed[i])
                    {
                        _decayed[i] = true;
                        SetAtomColor(i, _daughterColor);
                        decayed++;
                    }
                }
                _decayedCount = targetDecayed;
            }

            if (_halfLifeSeconds > 0 && parentFraction > 0)
                _simulatedSeconds = -System.Math.Log(parentFraction) / (System.Math.Log(2) / _halfLifeSeconds);

            OnStatsChanged?.Invoke(AtomCount - _decayedCount, _decayedCount, _simulatedSeconds);
        }

        private void Update()
        {
            if (!_running || _decayedCount >= AtomCount) return;

            double dt = (double)Time.deltaTime * _timeMultiplier;
            double decayProb = dt * 0.693147 / _halfLifeSeconds;
            decayProb = System.Math.Min(decayProb, 1.0);

            _simulatedSeconds += dt;

            for (int i = 0; i < AtomCount; i++)
            {
                if (_decayed[i]) continue;
                if (Random.value < (float)decayProb)
                {
                    _decayed[i] = true;
                    SetAtomColor(i, _daughterColor);
                    _decayedCount++;
                }
            }

            OnStatsChanged?.Invoke(AtomCount - _decayedCount, _decayedCount, _simulatedSeconds);

            if (_decayedCount >= AtomCount) _running = false;
        }

        private void BuildAtoms()
        {
            if (_atoms != null)
            {
                foreach (var a in _atoms)
                    if (a != null) Destroy(a);
            }

            _atoms     = new GameObject[AtomCount];
            _decayed   = new bool[AtomCount];
            _renderers = new Renderer[AtomCount];

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var sharedMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            var sharedMat  = primitive.GetComponent<Renderer>().sharedMaterial;
            Destroy(primitive);

            // 10x10x10 grid, ~2cm spacing
            int idx = 0;
            for (int x = 0; x < 10; x++)
            for (int y = 0; y < 10; y++)
            for (int z = 0; z < 10; z++)
            {
                var go = new GameObject($"Atom_{idx}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3((x - 4.5f) * 0.02f,
                                                          (y - 4.5f) * 0.02f,
                                                          (z - 4.5f) * 0.02f);
                go.transform.localScale = Vector3.one * 0.012f;

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = sharedMesh;
                var rend = go.AddComponent<MeshRenderer>();
                rend.sharedMaterial = sharedMat;

                _atoms[idx]     = go;
                _decayed[idx]   = false;
                _renderers[idx] = rend;
                idx++;
            }
        }

        private void SetAllColor(Color c)
        {
            for (int i = 0; i < AtomCount; i++)
                SetAtomColor(i, c);
        }

        private void SetAtomColor(int i, Color c)
        {
            if (_renderers[i] == null) return;
            _renderers[i].GetPropertyBlock(_mpb);
            _mpb.SetColor(s_ColorId, c);
            _renderers[i].SetPropertyBlock(_mpb);
        }
    }
}
