using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconManager : MonoBehaviour {
    List <Transform> transforms = new List<Transform>();
    Camera mainCamera;
    bool power = false;

    void Start () {
        transforms = GameObject.Find("Goliad").GetComponent<GameState>().allIconTransforms;
        mainCamera = Camera.main;
    }

    void Update () {
        if (mainCamera.orthographicSize < 40) {
            if (power == true) {
                foreach (Transform key in transforms) {
                    key.gameObject.SetActive(false);
                }
                power = false;
            }
        }
        else {
            if (power == false) {
                foreach (Transform key in transforms) {
                    key.gameObject.SetActive(true);
                }
                power = true;
            }
            Vector3 scaleNeutral = new Vector3 (1, 1, 1); 
            foreach (Transform key in transforms) {                
                key.localScale = scaleNeutral * mainCamera.orthographicSize / 30;
            }
        }
    }

}
