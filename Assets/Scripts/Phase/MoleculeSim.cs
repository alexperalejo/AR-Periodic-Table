using UnityEngine;

namespace PeriodicAR.Phase
{
    /// <summary>
    /// 50-particle 2D Verlet simulation driving a screen-space canvas "chamber face."
    /// Particles are rendered as colored UI Images.
    /// </summary>
    public class MoleculeSim : MonoBehaviour
    {
        private const int N = 50;

        private Vector2[] _pos;
        private Vector2[] _vel;
        private Vector2[] _anchor; // lattice anchors for solid mode
        private UnityEngine.UI.Image[] _dots;

        private RectTransform _container;
        private Color         _molColor;
        private float         _molRadius;  // in UI units

        public PhaseMode Mode { get; set; } = PhaseMode.Liquid;
        public float Temperature { get; set; } = 298f;
        public float Pressure    { get; set; } = 1f;

        private Vector2 Bounds => _container != null ? _container.rect.size : new Vector2(200f, 200f);

        public void Init(RectTransform container, Color color, float radius)
        {
            _container = container;
            _molColor  = color;
            _molRadius = Mathf.Max(radius, 4f);

            _pos    = new Vector2[N];
            _vel    = new Vector2[N];
            _anchor = new Vector2[N];
            _dots   = new UnityEngine.UI.Image[N];

            Vector2 bounds = Bounds;
            int cols = 8, rows = 7;
            int idx = 0;
            for (int r = 0; r < rows && idx < N; r++)
            for (int c = 0; c < cols && idx < N; c++, idx++)
            {
                float x = (c + 0.5f) * bounds.x / cols;
                float y = (r + 0.5f) * bounds.y / rows;
                _pos[idx]    = new Vector2(x, y);
                _anchor[idx] = new Vector2(x, y);
                _vel[idx]    = Random.insideUnitCircle * 10f;
                _dots[idx]   = CreateDot(idx);
            }
        }

        private UnityEngine.UI.Image CreateDot(int idx)
        {
            var go = new GameObject($"M{idx}", typeof(RectTransform));
            go.transform.SetParent(_container, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = Vector2.one * _molRadius * 2f;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = _molColor;
            return img;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float kT = Temperature * 0.001f;

            switch (Mode)
            {
                case PhaseMode.Solid:       TickSolid(dt, kT);       break;
                case PhaseMode.Liquid:      TickLiquid(dt, kT);      break;
                case PhaseMode.Gas:         TickGas(dt, kT);         break;
                case PhaseMode.Supercritical: TickGas(dt, kT * 1.5f); break;
            }

            UpdateDots();
        }

        // ---- Solid: small vibration around lattice anchor -------------------------
        private void TickSolid(float dt, float kT)
        {
            float vibAmp = Mathf.Sqrt(kT) * 3f;
            for (int i = 0; i < N; i++)
            {
                _vel[i] += (_anchor[i] - _pos[i]) * 20f * dt; // spring
                _vel[i] += Random.insideUnitCircle * vibAmp;
                _vel[i] *= 0.85f;
                _pos[i] += _vel[i] * dt;
            }
        }

        // ---- Liquid: Lennard-Jones-lite + thermal motion -------------------------
        private void TickLiquid(float dt, float kT)
        {
            Vector2 bounds = Bounds;
            float thermalSpeed = Mathf.Sqrt(kT) * 25f;

            for (int i = 0; i < N; i++)
            {
                Vector2 force = Vector2.zero;
                for (int j = 0; j < N; j++)
                {
                    if (i == j) continue;
                    Vector2 d = _pos[i] - _pos[j];
                    float   r = d.magnitude;
                    if (r < 0.001f) continue;
                    float r6  = Mathf.Pow(Mathf.Max(r, 8f), 6);
                    float lj  = 12f / (r6 * r6) - 6f / r6;
                    force += d.normalized * lj * 0.0001f;
                }

                _vel[i] += force;
                _vel[i] += Random.insideUnitCircle * thermalSpeed * dt;
                _vel[i] = Vector2.ClampMagnitude(_vel[i], thermalSpeed * 2f);
                _pos[i] += _vel[i] * dt;
                WallBounce(i, bounds);
            }
        }

        // ---- Gas: straight-line + wall + particle collisions ---------------------
        private void TickGas(float dt, float kT)
        {
            Vector2 bounds = Bounds;
            float speed = Mathf.Sqrt(kT) * 80f;

            for (int i = 0; i < N; i++)
            {
                if (_vel[i].magnitude < 1f)
                    _vel[i] = Random.insideUnitCircle.normalized * speed;

                _pos[i] += _vel[i] * dt;
                WallBounce(i, bounds);
            }

            // Simple elastic pair collisions
            for (int i = 0; i < N - 1; i++)
            for (int j = i + 1; j < N; j++)
            {
                Vector2 d = _pos[i] - _pos[j];
                if (d.magnitude < _molRadius * 2f)
                {
                    Vector2 n  = d.normalized;
                    Vector2 rv = _vel[i] - _vel[j];
                    float  dot = Vector2.Dot(rv, n);
                    if (dot < 0)
                    {
                        _vel[i] -= dot * n;
                        _vel[j] += dot * n;
                    }
                }
            }
        }

        private void WallBounce(int i, Vector2 bounds)
        {
            float r = _molRadius;
            if (_pos[i].x < r)               { _pos[i].x = r;              _vel[i].x = Mathf.Abs(_vel[i].x); }
            if (_pos[i].x > bounds.x - r)    { _pos[i].x = bounds.x - r;  _vel[i].x = -Mathf.Abs(_vel[i].x);}
            if (_pos[i].y < r)               { _pos[i].y = r;              _vel[i].y = Mathf.Abs(_vel[i].y); }
            if (_pos[i].y > bounds.y - r)    { _pos[i].y = bounds.y - r;  _vel[i].y = -Mathf.Abs(_vel[i].y);}
        }

        private void UpdateDots()
        {
            for (int i = 0; i < N; i++)
            {
                if (_dots[i] == null) continue;
                _dots[i].rectTransform.anchoredPosition = _pos[i];
            }
        }

        public void SetColor(Color c)
        {
            _molColor = c;
            for (int i = 0; i < N; i++)
                if (_dots[i]) _dots[i].color = c;
        }
    }
}
