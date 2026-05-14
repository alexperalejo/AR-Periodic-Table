using System.Collections.Generic;
using UnityEngine;

namespace PeriodicAR.Orbitals
{
    /// <summary>
    /// Renders a set of orbital lobes as colored point-cloud GameObjects
    /// parented to this transform in world space.
    /// </summary>
    public class OrbitalRenderer : MonoBehaviour
    {
        private readonly List<GameObject> _lobeObjects = new();
        private static readonly Color[] LevelColors =
        {
            new Color(0.5f, 0.8f, 1.0f, 0.6f),  // n=1 s
            new Color(0.4f, 1.0f, 0.6f, 0.6f),  // n=2
            new Color(1.0f, 0.8f, 0.3f, 0.6f),  // n=3
            new Color(1.0f, 0.5f, 0.5f, 0.6f),  // n=4
            new Color(0.8f, 0.4f, 1.0f, 0.5f),  // n=5+
        };

        public void Show(int atomicNumber)
        {
            Clear();
            var config = ElectronConfigCalculator.GetConfig(atomicNumber);
            float yOffset = 0f;
            foreach (var occ in config)
            {
                float scale = 0.04f + occ.n * 0.015f;
                var mesh = OrbitalShapes.BuildOrbitalMesh(occ.l, scale);
                var colorIdx = Mathf.Clamp(occ.n - 1, 0, LevelColors.Length - 1);
                Color c = LevelColors[colorIdx];

                var go = BuildLobe(mesh, c, new Vector3(0f, yOffset, 0f));
                _lobeObjects.Add(go);
                yOffset += scale * 0.3f;
            }
        }

        public void Clear()
        {
            foreach (var go in _lobeObjects) if (go) Destroy(go);
            _lobeObjects.Clear();
        }

        private GameObject BuildLobe(Mesh mesh, Color color, Vector3 localPos)
        {
            var go = new GameObject("Lobe");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            // Use a particle-like shader if available, else default
            var mat = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Standard"));
            mat.color  = color;
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;

            return go;
        }
    }
}
