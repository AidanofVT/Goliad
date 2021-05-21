using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DepotFunction : MonoBehaviour {

    public void SlaughterSheep () {
        foreach (Collider2D contact in Physics2D.OverlapCircleAll(transform.position, 10)) {
            if (contact.gameObject.name.Contains("sheep")) {
                contact.gameObject.GetPhotonView().RPC("Die", RpcTarget.All);
            }
        }
    }

}
