using System;
using System.Collections;
using UnityEngine;

namespace PeriodicAR.Reactions
{
    /// <summary>
    /// Plays a Bohr-model reaction animation at a world-space position.
    /// Spawns two atoms, runs the chosen effect, then calls onComplete.
    /// Skippable by setting Skip = true.
    /// </summary>
    public class ReactionAnimator : MonoBehaviour
    {
        public bool Skip { get; set; }

        private const float AnimDuration = 3.5f;
        private const float Separation   = 0.25f; // metres between atom centres

        public IEnumerator Play(string symbolA, string symbolB, string animationType,
                                Vector3 worldCenter, Action onComplete)
        {
            Skip = false;

            // Spawn both atoms.
            Vector3 posA = worldCenter + Vector3.left  * Separation;
            Vector3 posB = worldCenter + Vector3.right * Separation;

            var atomA = BohrModelFactory.Spawn(symbolA, posA);
            var atomB = BohrModelFactory.Spawn(symbolB, posB);

            // Scale down to AR-friendly size.
            float scale = 0.15f;
            if (atomA != null) atomA.transform.localScale = Vector3.one * scale;
            if (atomB != null) atomB.transform.localScale = Vector3.one * scale;

            yield return animationType switch
            {
                "electron_transfer" => PlayElectronTransfer(atomA, atomB),
                "electron_sharing"  => PlayElectronSharing(atomA, atomB),
                _                   => PlayNoReaction(atomA, atomB),
            };

            BohrModelFactory.Despawn(atomA);
            BohrModelFactory.Despawn(atomB);

            onComplete?.Invoke();
        }

        // ---- Animation types -------------------------------------------------

        private IEnumerator PlayElectronTransfer(GameObject atomA, GameObject atomB)
        {
            float elapsed = 0f;

            // After 1 s, fire a "spark" sphere from A to B.
            while (!Skip && elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Create a simple spark GO.
            var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(spark.GetComponent<Collider>());
            spark.transform.localScale = Vector3.one * 0.015f;
            var sparkMpb = new MaterialPropertyBlock();
            sparkMpb.SetColor("_BaseColor", new Color(1f, 0.9f, 0.2f));
            var sparkRend = spark.GetComponent<Renderer>();
            sparkRend?.SetPropertyBlock(sparkMpb);

            Vector3 startPos = atomA != null ? atomA.transform.position : Vector3.zero;
            Vector3 endPos   = atomB != null ? atomB.transform.position : Vector3.zero;

            float t = 0f;
            float travelTime = 0.8f;
            while (!Skip && t < 1f)
            {
                t += Time.deltaTime / travelTime;
                spark.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
                // Arc up slightly.
                spark.transform.position += Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.06f;
                yield return null;
            }

            Destroy(spark);

            // Hold on result.
            float hold = 0f;
            while (!Skip && hold < 1.5f) { hold += Time.deltaTime; yield return null; }
        }

        private IEnumerator PlayElectronSharing(GameObject atomA, GameObject atomB)
        {
            // Slide atoms together over 1 s.
            if (atomA == null || atomB == null) { yield return new WaitForSeconds(AnimDuration); yield break; }

            Vector3 startA = atomA.transform.position;
            Vector3 startB = atomB.transform.position;
            Vector3 mid    = (startA + startB) * 0.5f;
            Vector3 closeA = mid + Vector3.left  * Separation * 0.4f;
            Vector3 closeB = mid + Vector3.right * Separation * 0.4f;

            float t = 0f;
            while (!Skip && t < 1f)
            {
                t += Time.deltaTime / 1.2f;
                atomA.transform.position = Vector3.Lerp(startA, closeA, Mathf.SmoothStep(0f, 1f, t));
                atomB.transform.position = Vector3.Lerp(startB, closeB, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            // Hold close together.
            float hold = 0f;
            while (!Skip && hold < 2f) { hold += Time.deltaTime; yield return null; }

            // Slide back.
            t = 0f;
            while (!Skip && t < 1f)
            {
                t += Time.deltaTime / 0.6f;
                atomA.transform.position = Vector3.Lerp(closeA, startA, Mathf.SmoothStep(0f, 1f, t));
                atomB.transform.position = Vector3.Lerp(closeB, startB, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
        }

        private IEnumerator PlayNoReaction(GameObject atomA, GameObject atomB)
        {
            if (atomA == null || atomB == null) { yield return new WaitForSeconds(AnimDuration); yield break; }

            Vector3 startA = atomA.transform.position;
            Vector3 startB = atomB.transform.position;
            Vector3 mid    = (startA + startB) * 0.5f;

            // Move toward each other.
            float t = 0f;
            while (!Skip && t < 1f)
            {
                t += Time.deltaTime / 0.8f;
                float s = Mathf.SmoothStep(0f, 1f, t);
                atomA.transform.position = Vector3.Lerp(startA, mid + Vector3.left  * 0.05f, s);
                atomB.transform.position = Vector3.Lerp(startB, mid + Vector3.right * 0.05f, s);
                yield return null;
            }

            // Bounce back.
            t = 0f;
            while (!Skip && t < 1f)
            {
                t += Time.deltaTime / 0.5f;
                float s = Mathf.SmoothStep(0f, 1f, t);
                atomA.transform.position = Vector3.Lerp(mid + Vector3.left  * 0.05f, startA + Vector3.left  * 0.04f, s);
                atomB.transform.position = Vector3.Lerp(mid + Vector3.right * 0.05f, startB + Vector3.right * 0.04f, s);
                yield return null;
            }

            float hold = 0f;
            while (!Skip && hold < 1f) { hold += Time.deltaTime; yield return null; }
        }
    }
}
