using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class hoplite_Weapon : Weapon {
    
    public override void doIt() {
        Vector2 from = transform.position;
        Vector2 to = target.transform.position;
        Vector2 direction = from - to;
        float radAngle = Mathf.Atan2(direction.y, direction.x);
        object[] lengthPerameter = new object[1];
        lengthPerameter[0] = direction.magnitude;
        GameObject beam = PhotonNetwork.Instantiate("beam", (transform.position + target.transform.position) / 2, Quaternion.AxisAngle(Vector3.forward, radAngle), 0, lengthPerameter);
        beam.GetPhotonView().RPC("lerpBeam", RpcTarget.All);
        target.GetComponent<PhotonView>().RPC("takeHit", RpcTarget.Others, power);
    }

}
