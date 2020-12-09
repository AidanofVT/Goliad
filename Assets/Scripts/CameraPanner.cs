using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPanner : MonoBehaviour
{
    GameObject Goliad;
    ViewManager vManage;
//changes to panMultiplier and zoomMultiplier change the magnitude of these inputs by a static, linear amount.
    public float panMultiplier = 0.05f;
    public float zoomMultiplier = 9f;
//zoomExponent determines how exponentially greater zoom inputs should become as camera size increases
    public float zoomExponent = 1.1f;
//zoomMultiplier simply compensates for camera size, allowing the movement of things across the screen to seem static regardless of zoom 
    float distanceMultiplier;
    enum buttonState {up, upLeft, left, leftDown, down, downRight, right, rightUp}

    private void Awake() {
        distanceMultiplier = Mathf.Pow(Camera.main.orthographicSize, zoomExponent) / 5;
        Goliad = GameObject.Find("Goliad");
        vManage = transform.parent.GetComponent<ViewManager>();
    }

    void Update()
    {
        obeyCameraPanInputs();
        obeyCameraZoomInputs();
        distanceMultiplier = Mathf.Pow(Camera.main.orthographicSize, zoomExponent) / 10;   
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
        if (Input.GetAxis("zoom") != 0) {
            float oldSize = Camera.main.orthographicSize;
            Camera.main.orthographicSize -= Input.GetAxis("zoom") * zoomMultiplier * distanceMultiplier;
            float newSize = Camera.main.orthographicSize;
            vManage.resizeUIs(newSize / oldSize);
        }
    }

}
