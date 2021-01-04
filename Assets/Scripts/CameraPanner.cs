using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPanner : MonoBehaviour
{
    GameObject Goliad;
    ViewManager vManage;
//changes to panMultiplier and zoomMultiplier change the magnitude of these inputs by a static, linear amount.
    public float panMultiplier = 0.05f;
    public float followMouseMultiplier = 1f;
    public float zoomMultiplier = 9f;
//zoomExponent determines how exponentially greater zoom inputs should become as camera size increases
    public float zoomExponent = 1.1f;
//zoomMultiplier simply compensates for camera size, allowing the movement of things across the screen to seem static regardless of zoom 
    float distanceMultiplier;
    int mapExtent;
    float screenRatio;
    float cameraBaseZ;
    Vector3 cameraPos = Vector3.zero;
    float cameraZoom;

    private void Awake() {
        distanceMultiplier = Mathf.Pow(Camera.main.orthographicSize, zoomExponent) / 5;
        Goliad = GameObject.Find("Goliad");
        vManage = transform.parent.GetComponent<ViewManager>();
        mapExtent = Goliad.GetComponent<setup>().mapSize / 2;
        screenRatio = (float) Screen.width / (float) Screen.height;
        cameraBaseZ = Camera.main.transform.position.z;
    }

    void Update() {
        bool pan = Input.GetButton("panRight") || Input.GetButton("panLeft") || Input.GetButton("panUp") || Input.GetButton("panDown");
        bool zoom = Input.GetAxis("zoom") != 0;
        if (pan || zoom) {
            cameraPos = Camera.main.transform.position;
            if (zoom) {
                obeyCameraZoomInputs();
                cameraZoom = Mathf.Clamp(cameraZoom * screenRatio, mapExtent * -1, mapExtent) / screenRatio;
                cameraZoom = Mathf.Clamp(cameraZoom, mapExtent * -1, mapExtent);
                Camera.main.orthographicSize = cameraZoom;
            }
            if (pan) {
                obeyCameraPanInputs();
            }
            cameraPos = new Vector3(
                Mathf.Clamp(cameraPos.x, mapExtent * -1 + cameraZoom * screenRatio, mapExtent - cameraZoom * screenRatio),
                Mathf.Clamp(cameraPos.y, mapExtent * -1 + cameraZoom, mapExtent - cameraZoom),
                cameraBaseZ);
            Camera.main.transform.position = cameraPos;
            vManage.resizeUIs();
            distanceMultiplier = Mathf.Pow(Camera.main.orthographicSize, zoomExponent) / 10;
        }
    }

    void obeyCameraPanInputs () {
        float deltaX = 0;
        float deltaY = 0;
        if (Input.GetButton("panRight") == true) {
            deltaX = panMultiplier * distanceMultiplier;
        }
        if (Input.GetButton("panLeft") == true) {
            deltaX = panMultiplier * distanceMultiplier * -1;
        }
        if (Input.GetButton("panUp") == true) {
            deltaY = panMultiplier * distanceMultiplier;
        }
        if (Input.GetButton("panDown") == true) {
            deltaY = panMultiplier * distanceMultiplier * -1;
        }
        cameraPos += new Vector3 (deltaX, deltaY, 0);     
    }

    void obeyCameraZoomInputs () {
        float inputThisFrame = Input.GetAxis("zoom");
        cameraZoom = Camera.main.orthographicSize - inputThisFrame * zoomMultiplier * distanceMultiplier;
        Vector2 mouseOffset = Input.mousePosition;
        Vector2 screenCartesian = new Vector2(Screen.width, Screen.height);
        mouseOffset -= screenCartesian / 2;
        mouseOffset /= screenCartesian;
        if (inputThisFrame < 0) {
            mouseOffset *= -1;
        }
        cameraPos += (Vector3) mouseOffset * followMouseMultiplier * distanceMultiplier;
    }

}
