using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TapToPlace : MonoBehaviour
{
    //debug / test
    void Start()
    {
        Debug.Log("TapToPlace STARTED");
    }
    [SerializeField] GameObject placePrefab;

    ARRaycastManager raycastManager;
    GameObject spawnedObject;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // debug / test
    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        if (raycastManager == null)
            Debug.LogError("TapToPlace: No ARRaycastManager found on this GameObject!");
        if (placePrefab == null)
            Debug.LogError("TapToPlace: Place Prefab is NULL!");
    }

    void Update()
    {
        //debug / test
        if (Input.touchCount > 0)
            Debug.Log("Touch detected: " + Input.GetTouch(0).phase);

        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            Debug.Log("TapToPlace hit plane. Placing: " + (placePrefab ? placePrefab.name : "NULL"));
            Pose hitPose = hits[0].pose;

            if (spawnedObject == null)
            {
                spawnedObject = Instantiate(placePrefab, hitPose.position, hitPose.rotation);
            }
            else
            {
                spawnedObject.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
            }
        }
    }
}