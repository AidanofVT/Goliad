using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// DELETE THIS SCRIPT IF NOTHING BREAKS WHEN IT IS COMMENTED OUT.

public class IconManager : MonoBehaviour {
    List <GameObject> icons = new List<GameObject>();
    Camera mainCamera;
    bool power = false;

    void Start () {
        icons = GameObject.Find("Goliad").GetComponent<GameState>().allIcons;
        mainCamera = Camera.main;
    }

    void Update () {
        // if (mainCamera.orthographicSize < 40) {
        //     if (power == true) {
        //         foreach (GameObject key in icons) {
        //             key.SetActive(false);
        //         }
        //         power = false;
        //     }
        // }
        // else {
        //     if (power == false) {
        //         foreach (GameObject key in icons) {
        //             key.SetActive(true);
        //         }
        //         power = true;
        //     }
        //     Vector3 scaleNeutral = new Vector3 (1, 1, 1); 
        //     foreach (GameObject key in icons) {                
        //         key.transform.localScale = scaleNeutral * mainCamera.orthographicSize / 30;
        //     }
        // }
    }

}
