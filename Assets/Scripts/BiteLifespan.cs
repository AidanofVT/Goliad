using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiteLifespan : MonoBehaviour {
    void Start() {
        StartCoroutine("LerpDrumstick");
    }

    IEnumerator LerpDrumstick () {        
        Vector3 startValue = transform.localScale;
        Vector3 endValue = new Vector3(2, 2, 1);
        float startTime = Time.time;
        float progress = 0;
        while (progress < 1) {
            progress = (Time.time - startTime);
            transform.localScale = Vector3.Lerp(startValue, endValue, progress);
            Color drumstickColor = GetComponent<Renderer>().material.color;
            drumstickColor.a = 1 - progress;
            GetComponent<Renderer>().material.color = drumstickColor;
            yield return new WaitForSeconds(0);
        }
        Destroy(gameObject);
        yield return null;
    }
}
