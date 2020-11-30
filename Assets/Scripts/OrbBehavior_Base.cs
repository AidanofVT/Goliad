using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OrbBehavior_Base : MonoBehaviourPun {

    protected Rigidbody2D body;
    protected CircleCollider2D localCollider;

    void Awake () {
        if (photonView.IsMine && this.GetType() == typeof(OrbBehavior_Base)) {
            gameObject.AddComponent<OrbBehavior_Local>();
            Destroy(this);
        }
    }

    void Start () {
        body = GetComponent<Rigidbody2D>();
        localCollider = GetComponent<CircleCollider2D>();
    }

    [PunRPC]
    protected void seekStage () {
        StopCoroutine("launchStage");
//note: sometimes this throws a null reference error. is it possible that sometimes, if the orb is instantiated with little or no speed, this line is reached before the rigidbody exists, or before body is assigned a value?
        if (body != null) {
            Destroy(body);
        }
        localCollider.isTrigger = true;
        localCollider.radius = 10;
    }
}
