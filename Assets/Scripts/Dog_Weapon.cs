using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class dog_Weapon : Weapon {

    public override void doIt () {
        StartCoroutine("lerpDrumstick");
        target.GetComponent<PhotonView>().RPC("takeHit", RpcTarget.Others, power);
    }

    IEnumerator lerpDrumstick () {        
        GameObject drumstick = Instantiate((GameObject) Resources.Load("drumstick"), (transform.position + target.transform.position) / 2, Quaternion.identity);
        Vector3 startValue = transform.localScale;
        Vector3 endValue = new Vector3(2, 2, 1);
        float startTime = Time.time;
        float progress = 0;
        while (progress < 1) {
            progress = (Time.time - startTime);
            drumstick.transform.localScale = Vector3.Lerp(startValue, endValue, progress);
            Color drumstickColor = drumstick.GetComponent<Renderer>().material.color;
            drumstickColor.a = 1 - progress;
            drumstick.GetComponent<Renderer>().material.color = drumstickColor;
            yield return new WaitForSeconds(0);
        }
        Destroy(drumstick);
        yield return null;
    }

}
