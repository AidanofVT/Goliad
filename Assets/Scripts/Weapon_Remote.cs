﻿using System;
using System.Collections;
using UnityEngine;
using Photon.Pun;

public class Weapon_Remote : Weapon {

    [PunRPC]
    public override void Engage (int PhotonID) {
        target = PhotonNetwork.GetPhotonView(PhotonID).gameObject;
    }

    public override IEnumerator Fire () {
        while (target != null) {
            if (thisUnit.meat >= shotCost) {
                visualizer.Show();
                target.GetComponent<Unit>().TakeHit(power);
                if (shotCost > 0) {
                    thisUnit.photonView.RPC("DeductMeat", RpcTarget.All, shotCost);
                }
                yield return new WaitForSeconds(reloadTime);
            }
            else {
                yield return new WaitForSeconds(0);
            }
        }
        Disengage();
    }

    protected override void OnTriggerEnter2D(Collider2D other) { }

    protected override void OnTriggerExit2D(Collider2D other) { }


}
