using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Equilibrium
{
    /// <summary>
    /// Simple canvas-space particle sim showing left (reactant) and right (product) halves
    /// of a reaction chamber. Particle counts reflect concentrations.
    /// </summary>
    public class ParticleSim : MonoBehaviour
    {
        private const int MaxParticles = 80;

        private RectTransform _container;
        private Image[]        _particles;
        private Vector2[]      _vel;
        private int[]          _side;    // 0 = reactant half, 1 = product half

        private int _reactantCount = 40;
        private int _productCount  = 40;

        private Color _reactantColor = Color.blue;
        private Color _productColor  = Color.green;

        public void Init(RectTransform container, Color reactantColor, Color productColor)
        {
            _container      = container;
            _reactantColor  = reactantColor;
            _productColor   = productColor;

            _particles = new Image[MaxParticles];
            _vel       = new Vector2[MaxParticles];
            _side      = new int[MaxParticles];

            Vector2 bounds = container.rect.size;
            for (int i = 0; i < MaxParticles; i++)
            {
                int s = i < MaxParticles / 2 ? 0 : 1;
                _side[i] = s;
                float xRange = bounds.x * 0.5f - 8f;
                float xBase  = s == 0 ? 4f : bounds.x * 0.5f + 4f;
                float x = xBase + Random.Range(0f, xRange);
                float y = Random.Range(4f, bounds.y - 4f);
                _vel[i] = Random.insideUnitCircle * 30f;
                _particles[i] = CreateDot(new Vector2(x, y), s == 0 ? reactantColor : productColor);
            }
            SetCounts(40, 40);
        }

        public void SetCounts(int reactants, int products)
        {
            _reactantCount = Mathf.Clamp(reactants, 0, MaxParticles / 2);
            _productCount  = Mathf.Clamp(products,  0, MaxParticles / 2);

            for (int i = 0; i < MaxParticles / 2; i++)
                if (_particles[i]) _particles[i].gameObject.SetActive(i < _reactantCount);
            for (int i = MaxParticles / 2; i < MaxParticles; i++)
                if (_particles[i]) _particles[i].gameObject.SetActive((i - MaxParticles / 2) < _productCount);
        }

        private void Update()
        {
            if (_container == null) return;
            Vector2 bounds = _container.rect.size;
            float midX = bounds.x * 0.5f;

            for (int i = 0; i < MaxParticles; i++)
            {
                if (_particles[i] == null || !_particles[i].gameObject.activeSelf) continue;
                var rt  = _particles[i].rectTransform;
                var pos = rt.anchoredPosition;
                pos += _vel[i] * Time.deltaTime;

                float minX = _side[i] == 0 ? 4f : midX + 4f;
                float maxX = _side[i] == 0 ? midX - 4f : bounds.x - 4f;

                if (pos.x < minX) { pos.x = minX; _vel[i].x =  Mathf.Abs(_vel[i].x); }
                if (pos.x > maxX) { pos.x = maxX; _vel[i].x = -Mathf.Abs(_vel[i].x); }
                if (pos.y < 4f)   { pos.y = 4f;   _vel[i].y =  Mathf.Abs(_vel[i].y); }
                if (pos.y > bounds.y - 4f) { pos.y = bounds.y - 4f; _vel[i].y = -Mathf.Abs(_vel[i].y); }

                rt.anchoredPosition = pos;
            }
        }

        private Image CreateDot(Vector2 pos, Color color)
        {
            var go = new GameObject("P", typeof(RectTransform));
            go.transform.SetParent(_container, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * 6f;
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }
    }
}
