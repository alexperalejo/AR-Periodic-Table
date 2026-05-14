using UnityEngine;

namespace PeriodicAR.Orbitals
{
    /// <summary>
    /// Generates point-cloud meshes approximating s, p, d orbital lobes.
    /// Used by OrbitalRenderer to visualise electron probability density.
    /// </summary>
    public static class OrbitalShapes
    {
        private const int Points = 600;

        /// <summary>Creates a mesh whose vertices approximate the orbital's probability cloud.</summary>
        public static Mesh BuildOrbitalMesh(string lType, float scale)
        {
            var vertices = new Vector3[Points];
            var indices  = new int[Points];

            System.Random rng = new System.Random(42);

            for (int i = 0; i < Points; i++)
            {
                Vector3 p = SampleOrbital(lType, rng, scale);
                vertices[i] = p;
                indices[i]  = i;
            }

            var mesh = new Mesh();
            mesh.vertices  = vertices;
            mesh.SetIndices(indices, MeshTopology.Points, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 SampleOrbital(string lType, System.Random rng, float scale)
        {
            switch (lType)
            {
                case "s": return SampleS(rng, scale);
                case "p": return SampleP(rng, scale);
                case "d": return SampleD(rng, scale);
                case "f": return SampleF(rng, scale);
                default:  return SampleS(rng, scale);
            }
        }

        // ---- s orbital: spherical distribution -----------------------------------
        private static Vector3 SampleS(System.Random rng, float scale)
        {
            // Radial: r ~ r² * exp(-2r/a₀), approximate with rejection from sphere
            float r = SampleExponential(rng, 0.4f) * scale;
            return RandomOnSphere(rng) * r;
        }

        // ---- p orbital: dumbbell lobes along Z ----------------------------------
        private static Vector3 SampleP(System.Random rng, float scale)
        {
            float r     = SampleExponential(rng, 0.45f) * scale;
            float theta = (float)(rng.NextDouble() * Mathf.PI);
            float phi   = (float)(rng.NextDouble() * 2 * Mathf.PI);
            // ψ² ∝ cos²(θ) → weight by cos²
            float w = Mathf.Abs(Mathf.Cos(theta));
            if ((float)rng.NextDouble() > w) theta = Mathf.PI - theta;

            return new Vector3(
                r * Mathf.Sin(theta) * Mathf.Cos(phi),
                r * Mathf.Sin(theta) * Mathf.Sin(phi),
                r * Mathf.Cos(theta));
        }

        // ---- d orbital: four-lobed cloverleaf in XY plane ----------------------
        private static Vector3 SampleD(System.Random rng, float scale)
        {
            float r   = SampleExponential(rng, 0.35f) * scale;
            float phi = (float)(rng.NextDouble() * 2 * Mathf.PI);
            // ψ² ∝ cos²(2φ)
            float w = Mathf.Abs(Mathf.Cos(2f * phi));
            if ((float)rng.NextDouble() > w * 0.8f) phi += Mathf.PI * 0.5f;

            float theta = (float)(rng.NextDouble() * Mathf.PI);
            return new Vector3(
                r * Mathf.Sin(theta) * Mathf.Cos(phi),
                r * Mathf.Cos(theta) * 0.4f,
                r * Mathf.Sin(theta) * Mathf.Sin(phi));
        }

        // ---- f orbital: complex, approximate as elongated d --------------------
        private static Vector3 SampleF(System.Random rng, float scale)
        {
            float r   = SampleExponential(rng, 0.30f) * scale;
            float phi = (float)(rng.NextDouble() * 2 * Mathf.PI);
            float w   = Mathf.Abs(Mathf.Cos(3f * phi));
            if ((float)rng.NextDouble() > w * 0.7f) phi += Mathf.PI / 3f;
            float theta = (float)(rng.NextDouble() * Mathf.PI);
            return new Vector3(
                r * Mathf.Sin(theta) * Mathf.Cos(phi),
                r * Mathf.Cos(theta) * 0.3f,
                r * Mathf.Sin(theta) * Mathf.Sin(phi));
        }

        private static float SampleExponential(System.Random rng, float lambda)
        {
            double u = rng.NextDouble();
            if (u <= 0) u = 1e-10;
            return (float)(-System.Math.Log(u) / lambda);
        }

        private static Vector3 RandomOnSphere(System.Random rng)
        {
            float u = (float)(rng.NextDouble() * 2 - 1);
            float phi = (float)(rng.NextDouble() * 2 * Mathf.PI);
            float r = Mathf.Sqrt(1f - u * u);
            return new Vector3(r * Mathf.Cos(phi), r * Mathf.Sin(phi), u);
        }
    }
}
