using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OrbBehavior_Base : MonoBehaviourPun {

    protected Rigidbody2D body;

    void Awake () {
        if (photonView.IsMine && this.GetType() == typeof(OrbBehavior_Base)) {
            gameObject.AddComponent<OrbBehavior_Local>();
            Destroy(this);
        }
    }

    protected void parachute () {
        body.velocity = new Vector3(0,0,0);
        Destroy(body);
        CircleCollider2D localCollider = GetComponent<CircleCollider2D>();
        localCollider.isTrigger = true;
        localCollider.radius = 10;
    }
}
