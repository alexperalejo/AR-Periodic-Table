using PeriodicAR.Data;
using UnityEngine;

namespace PeriodicAR
{
    /// <summary>
    /// Spawns standalone Bohr-model atoms at arbitrary world positions.
    /// Borrows prefab references from the scene's existing AtomGenerator.
    /// Other features (Reaction Sandbox, Scale Visualizer) use this instead
    /// of going through ElementLoader's fixed scene atom.
    /// </summary>
    public static class BohrModelFactory
    {
        public static GameObject Spawn(string symbol, Vector3 worldPosition)
        {
            var loader = Object.FindAnyObjectByType<ElementLoader>();
            if (loader == null || loader.Table == null) return null;

            AtomElementData data = null;
            foreach (var el in loader.Table.elements)
            {
                if (string.Equals(el.symbol, symbol, System.StringComparison.OrdinalIgnoreCase))
                { data = el; break; }
            }
            if (data == null) return null;

            int neutrons = Mathf.Max(0, Mathf.RoundToInt(data.atomic_mass) - data.number);
            int[] shells = (data.shells != null && data.shells.Length > 0)
                ? (int[])data.shells.Clone()
                : new int[] { data.number };

            // Create inactive so Start() doesn't fire before we set prefab refs.
            var root = new GameObject($"[Bohr_{symbol}]");
            root.SetActive(false);

            var gen = root.AddComponent<AtomGenerator>();

            // Copy prefab refs from the scene AtomGenerator.
            var template = Object.FindFirstObjectByType<AtomGenerator>();
            if (template != null && template.protonPrefab != null)
            {
                gen.protonPrefab    = template.protonPrefab;
                gen.neutronPrefab   = template.neutronPrefab;
                gen.electronPrefab  = template.electronPrefab;
                gen.orbitRingPrefab = template.orbitRingPrefab;
            }

            gen.elementName       = data.name;
            gen.protonCount       = data.number;
            gen.neutronCount      = neutrons;
            gen.electronsPerShell = shells;

            root.transform.position = worldPosition;
            root.SetActive(true); // Start() fires → GenerateAtom() once
            return root;
        }

        public static void Despawn(GameObject instance)
        {
            if (instance != null) Object.Destroy(instance);
        }
    }
}
