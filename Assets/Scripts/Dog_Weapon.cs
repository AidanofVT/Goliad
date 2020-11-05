using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Dog_Weapon : Weapon {

    float timeOfLastFire;
    GameObject target = null;

    public override void doIt () {
        target.GetComponent<PhotonView>().RPC("takeHit", RpcTarget.Others, power);
    }

}
