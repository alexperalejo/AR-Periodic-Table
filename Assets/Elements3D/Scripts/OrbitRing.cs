using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(LineRenderer))]
public class OrbitRing : MonoBehaviour
{
    [Header("Ring Shape")]
    public int segments = 64;
    public float radius = 0.45f;

    [Header("Line Style")]
    public float lineWidth = 0.007f;

    [Header("Compatibility With Existing Generator")]
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 0f;

    private LineRenderer lr;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        DrawRing();
    }

    private void OnValidate()
    {
        segments = Mathf.Max(8, segments);
        radius = Mathf.Max(0.01f, radius);
        lineWidth = Mathf.Max(0.0005f, lineWidth);

        if (lr == null)
        {
            lr = GetComponent<LineRenderer>();
        }

        if (lr != null)
        {
            ConfigureLineRenderer();
            DrawRing();
        }
    }

    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0.01f, newRadius);
        DrawRing();
    }

    public float GetRadius()
    {
        return radius;
    }

    public void SetColor(Color color)
    {
        if (lr == null)
        {
            lr = GetComponent<LineRenderer>();
        }

        if (lr == null)
        {
            return;
        }

        lr.startColor = color;
        lr.endColor = color;

        if (lr.material != null)
        {
            if (lr.material.HasProperty("_BaseColor"))
            {
                lr.material.SetColor("_BaseColor", color);
            }

            if (lr.material.HasProperty("_Color"))
            {
                lr.material.SetColor("_Color", color);
            }

            if (lr.material.HasProperty("_EmissionColor"))
            {
                lr.material.EnableKeyword("_EMISSION");
                lr.material.SetColor("_EmissionColor", color * 0.35f);
            }
        }
    }

    private void Initialize()
    {
        if (lr == null)
        {
            lr = GetComponent<LineRenderer>();
        }

        ConfigureLineRenderer();
        DrawRing();
    }

    private void ConfigureLineRenderer()
    {
        if (lr == null) return;

        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = segments;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }

    private void DrawRing()
    {
        if (lr == null) return;

        lr.positionCount = segments;

        float angleStep = 2f * Mathf.PI / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0f, z));
        }
    }
}