using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPanner : MonoBehaviour
{
    public float panMultiplier = 0.05f;
    public float zoomMultiplier = 9f;
    public float distanceFactor = 1.1f;
    float distanceMultiplier;
    enum buttonState {up, upLeft, left, leftDown, down, downRight, right, rightUp}

    private void Awake() {
        distanceMultiplier = Mathf.Pow(Camera.main.orthographicSize, distanceFactor) / 5;
    }

    void Update()
    {
        obeyCameraPanInputs();
        obeyCameraZoomInputs();
    }

    void obeyCameraPanInputs () {
        float cameraX = Camera.main.transform.position.x;
        float cameraY = Camera.main.transform.position.y;
        if (Input.GetButton("panRight") == true) {
            cameraX += panMultiplier * distanceMultiplier;
        }
        if (Input.GetButton("panLeft") == true) {
            cameraX -= panMultiplier * distanceMultiplier;
        }
        if (Input.GetButton("panUp") == true) {
            cameraY += panMultiplier * distanceMultiplier;
        }
        if (Input.GetButton("panDown") == true) {
            cameraY -= panMultiplier * distanceMultiplier;
        }
        Vector3 newCamPosition = new Vector3 (cameraX, cameraY, Camera.main.transform.position.z);
                Camera.main.transform.position = newCamPosition;     
    }

    void obeyCameraZoomInputs () {
        Camera.main.orthographicSize -= Input.GetAxis("zoom") * zoomMultiplier * distanceMultiplier;
        distanceMultiplier = Mathf.Pow(Camera.main.orthographicSize, distanceFactor) / 10;        
    }
}
