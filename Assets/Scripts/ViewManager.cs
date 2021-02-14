using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ViewManager : MonoBehaviour {

    Unit_local unitUnderMouse;
    SelectionRectManager rectManager;
    List <Transform> transforms = new List<Transform>();
    Camera mainCamera;
    bool power = false;

    void Start () {
        transforms = GameObject.Find("Goliad").GetComponent<GameState>().allIconTransforms;
        rectManager = transform.GetChild(1).GetComponent<SelectionRectManager>();
        mainCamera = Camera.main;
    }

    public void addToPalette (Unit_local underMouse) {
        unitUnderMouse = underMouse;
        if (rectManager.rectOn == false) {
            if (Input.GetButton("modifier") == false) {
                unitUnderMouse.cohort.Highlight();
            }
            else {
                unitUnderMouse.Highlight();
            }
        }
    }

    public void removeFromPalette (Unit_local toRem) {
        if (rectManager.rectOn == false) {
            unitUnderMouse.cohort.HighlightOff();
        }
        unitUnderMouse = null;
    }

    void Update () {
        if (unitUnderMouse != null) {
            if (Input.GetButtonDown("modifier") && rectManager.rectOn == false) {
                unitUnderMouse.cohort.HighlightOff();
                unitUnderMouse.Highlight();
            }
            if (Input.GetButtonUp("modifier") && rectManager.rectOn == false) {
                unitUnderMouse.cohort.Highlight();
            }
        }
    }

    public void resizeIcons (GameObject singleIcon = null) {
        if (mainCamera.orthographicSize < 40) {
            if (power == true) {
                foreach (Transform key in transforms) {
                    key.gameObject.SetActive(false);
                }
                power = false;
            }
        }
        else {
            Vector3 neutralScaleVector = new Vector3 (1, 1, 1); 
            if (singleIcon != null) {
                singleIcon.transform.localScale = neutralScaleVector * mainCamera.orthographicSize / 30;
                singleIcon.SetActive(true);
            }
            else {
                if (power == false) {
                    foreach (Transform key in transforms) {
                        key.gameObject.SetActive(true);
                    }
                    power = true;
                }
                foreach (Transform key in transforms) {    
                    key.localScale = neutralScaleVector * mainCamera.orthographicSize / 30;
                }
            }
        }
    }
}
