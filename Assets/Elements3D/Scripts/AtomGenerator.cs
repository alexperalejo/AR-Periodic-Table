//This script will spawn
//proton/neutron nucleus
//orbit rings
//electrons on each ring

using System.Collections.Generic;
using UnityEngine;

public class AtomGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject protonPrefab;
    public GameObject neutronPrefab;
    public GameObject electronPrefab;
    public GameObject orbitRingPrefab;

    [Header("Element Data")] //Hardcode first to test
    public string elementName = "Magnesium";
    public int protonCount = 12;
    public int neutronCount = 12;
    public int[] electronsPerShell = new int[] { 2, 8, 2 };

    [Header("Layout Settings")]
    public float nucleusRadius = 0.12f;
    public float shellSpacing = 0.18f;
    public float electronScale = 1f;
    public float ringYOffset = 0f;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        GenerateAtom();
    }

    [ContextMenu("Generate Atom")]
    public void GenerateAtom()
    {
        ClearAtom();

        GenerateNucleus();
        GenerateShellsAndElectrons();
    }

    [ContextMenu("Clear Atom")]
    public void ClearAtom()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
                DestroyImmediate(spawnedObjects[i]);
        }

        spawnedObjects.Clear();
    }

    void GenerateNucleus()
    {
        SpawnNucleusParticles(protonPrefab, protonCount);
        SpawnNucleusParticles(neutronPrefab, neutronCount);
    }

    void SpawnNucleusParticles(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * nucleusRadius;
            GameObject particle = Instantiate(prefab, transform);
            particle.transform.localPosition = randomOffset;
            particle.transform.localRotation = Quaternion.identity;

            spawnedObjects.Add(particle);
        }
    }

    void GenerateShellsAndElectrons()
    {
        if (orbitRingPrefab == null || electronPrefab == null || electronsPerShell == null) return;

        // Predefined tilt angles so rings look like the video (varied 3D orientations)
        Vector3[] tiltAngles = new Vector3[]
        {
        new Vector3(90f, 0f, 0f),       // flat / equatorial
        new Vector3(20f, 45f, 0f),      // tilted
        new Vector3(60f, 90f, 30f),     // more tilted
        new Vector3(10f, 135f, 60f),    // near vertical
        };

        Vector3[] rotationAxes = new Vector3[]
        {
        new Vector3(0f, 1f, 0.3f).normalized,
        new Vector3(0.5f, 1f, 0f).normalized,
        new Vector3(1f, 0.2f, 0.5f).normalized,
        new Vector3(0.3f, 0.8f, 1f).normalized,
        };

        for (int shellIndex = 0; shellIndex < electronsPerShell.Length; shellIndex++)
        {
            float radius = shellSpacing * (shellIndex + 1);

            GameObject ring = Instantiate(orbitRingPrefab, transform);
            ring.transform.localPosition = new Vector3(0f, ringYOffset, 0f);

            // Apply unique tilt per shell
            int tiltIdx = shellIndex % tiltAngles.Length;
            ring.transform.localRotation = Quaternion.Euler(tiltAngles[tiltIdx]);

            OrbitRing orbitScript = ring.GetComponent<OrbitRing>();
            if (orbitScript != null)
            {
                orbitScript.radius = radius;
                orbitScript.rotationAxis = rotationAxes[tiltIdx];
                orbitScript.rotationSpeed = 10f + shellIndex * 5f; // outer rings spin slower
            }

            spawnedObjects.Add(ring);

            int electronCount = electronsPerShell[shellIndex];
            for (int e = 0; e < electronCount; e++)
            {
                float angle = (360f / electronCount) * e;

                GameObject electron = Instantiate(electronPrefab, transform);
                electron.transform.localRotation = Quaternion.identity;
                electron.transform.localScale *= electronScale;

                ElectronOrbit orbit = electron.GetComponent<ElectronOrbit>();
                if (orbit == null)
                    orbit = electron.AddComponent<ElectronOrbit>();

                orbit.center = transform;
                orbit.ringTransform = ring.transform; // follow ring rotation
                orbit.radius = radius;
                orbit.yOffset = ringYOffset;
                orbit.angleOffset = angle;
                orbit.speed = 20f + shellIndex * 5f;

                spawnedObjects.Add(electron);
            }
        }
    }
}