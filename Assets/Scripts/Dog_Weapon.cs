using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class dog_Weapon : Weapon {

    public override void doIt () {
        PhotonNetwork.Instantiate("bite", (transform.position + target.transform.position) / 2, Quaternion.identity);
        target.GetComponent<PhotonView>().RPC("takeHit", RpcTarget.Others, power);
    }

}
