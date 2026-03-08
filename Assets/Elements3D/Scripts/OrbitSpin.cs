//Orbits to spin with electrons

using UnityEngine;

public class OrbitSpin : MonoBehaviour
{
    public Vector3 rotationAxis = new Vector3(0f, 1f, 0f);
    public float spinSpeed = 20f;

    void Update()
    {
        transform.Rotate(rotationAxis * spinSpeed * Time.deltaTime);
    }
}