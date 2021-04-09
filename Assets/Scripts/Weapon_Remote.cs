using System;
using System.Collections;
using UnityEngine;
using Photon.Pun;

public class Weapon_Remote : Weapon {

    public override IEnumerator Fire () {
        while (target != null) {
            if (thisUnit.meat >= shotCost) {
                target.GetComponent<Unit>().takeHit(power);
                visualizer.Show();
                if (shotCost > 0) {
                    thisUnit.photonView.RPC("deductMeat", RpcTarget.All, shotCost);
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
