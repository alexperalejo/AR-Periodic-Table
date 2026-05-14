using System.Collections.Generic;
using UnityEngine;

namespace PeriodicAR.BondBuilder
{
    /// <summary>
    /// Renders a molecule in world space as colored spheres (atoms) + cylinders (bonds).
    /// Operates on a normalized scale where 1 Angstrom = 0.05 Unity units.
    /// </summary>
    public class MoleculeRenderer : MonoBehaviour
    {
        private const float Scale = 0.05f;  // Å → Unity units

        private readonly List<GameObject> _objects = new();

        private static readonly Dictionary<string, Color> s_CPKColors = new()
        {
            {"H",  new Color(1f, 1f, 1f)},
            {"C",  new Color(0.2f, 0.2f, 0.2f)},
            {"N",  new Color(0.2f, 0.4f, 1f)},
            {"O",  new Color(1f, 0.2f, 0.2f)},
            {"F",  new Color(0.2f, 0.9f, 0.3f)},
            {"Cl", new Color(0.1f, 0.8f, 0.1f)},
            {"Br", new Color(0.6f, 0.1f, 0.1f)},
            {"S",  new Color(1f, 0.9f, 0.1f)},
            {"P",  new Color(1f, 0.5f, 0f)},
            {"Na", new Color(0.7f, 0.3f, 1f)},
            {"Cl", new Color(0.1f, 0.9f, 0.1f)},
        };

        private static readonly Dictionary<string, float> s_Radii = new()
        {
            {"H", 0.25f}, {"C", 0.35f}, {"N", 0.30f}, {"O", 0.30f},
            {"F", 0.27f}, {"Cl", 0.40f}, {"S", 0.40f}, {"P", 0.38f},
            {"Na", 0.45f}, {"K", 0.55f},
        };

        public void Render(MoleculeRecipe recipe)
        {
            Clear();
            if (recipe == null) return;

            // Atoms
            for (int i = 0; i < recipe.atoms.Count; i++)
            {
                var atom = recipe.atoms[i];
                Vector3 pos = AtomPos(atom.position) * Scale;
                Color   c   = GetColor(atom.symbol);
                float   r   = GetRadius(atom.symbol) * Scale;
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = pos;
                go.transform.localScale    = Vector3.one * r * 2f;
                go.GetComponent<Renderer>().material.color = c;
                Destroy(go.GetComponent<Collider>());
                go.name = atom.symbol + i;
                _objects.Add(go);
            }

            // Bonds
            for (int b = 0; b < recipe.bonds.Count; b++)
            {
                var bond     = recipe.bonds[b];
                if (bond.Length < 2) continue;
                string bType = b < recipe.bondTypes.Count ? recipe.bondTypes[b] : "single";

                Vector3 a = AtomPos(recipe.atoms[bond[0]].position) * Scale;
                Vector3 c = AtomPos(recipe.atoms[bond[1]].position) * Scale;
                DrawBond(a, c, bType);
            }

            // Center the molecule on this transform's origin
            transform.localPosition = Vector3.zero;
            StartCoroutine(RotateSlow());
        }

        public void Clear()
        {
            StopAllCoroutines();
            foreach (var o in _objects) if (o) Destroy(o);
            _objects.Clear();
        }

        private void DrawBond(Vector3 from, Vector3 to, string type)
        {
            int count = type == "double" ? 2 : type == "triple" ? 3 : 1;
            float offset = 0.008f * Scale;

            for (int i = 0; i < count; i++)
            {
                Vector3 dir    = (to - from).normalized;
                Vector3 perp   = Vector3.Cross(dir, Vector3.up).normalized;
                if (perp.sqrMagnitude < 0.001f) perp = Vector3.Cross(dir, Vector3.right).normalized;

                float shift = (i - (count - 1) * 0.5f) * offset;
                Vector3 a   = from + perp * shift;
                Vector3 b   = to   + perp * shift;

                var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cyl.transform.SetParent(transform, false);
                Destroy(cyl.GetComponent<Collider>());

                Vector3 mid    = (a + b) * 0.5f;
                float   length = (b - a).magnitude;
                cyl.transform.localPosition = mid;
                cyl.transform.localScale    = new Vector3(0.02f * Scale, length * 0.5f, 0.02f * Scale);
                cyl.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);

                Color bondColor = type == "ionic" ? new Color(1f, 0.8f, 0f) :
                                  type == "aromatic" ? new Color(0.5f, 0.8f, 1f) :
                                  new Color(0.7f, 0.7f, 0.7f);
                cyl.GetComponent<Renderer>().material.color = bondColor;
                _objects.Add(cyl);
            }
        }

        private System.Collections.IEnumerator RotateSlow()
        {
            while (true)
            {
                transform.Rotate(Vector3.up, 20f * Time.deltaTime, Space.World);
                yield return null;
            }
        }

        private static Vector3 AtomPos(float[] p)
        {
            if (p == null || p.Length < 3) return Vector3.zero;
            return new Vector3(p[0], p[1], p[2]);
        }

        private static Color GetColor(string sym)
        {
            return s_CPKColors.TryGetValue(sym, out var c) ? c : new Color(0.8f, 0.5f, 0.8f);
        }

        private static float GetRadius(string sym)
        {
            return s_Radii.TryGetValue(sym, out var r) ? r : 0.35f;
        }
    }
}
