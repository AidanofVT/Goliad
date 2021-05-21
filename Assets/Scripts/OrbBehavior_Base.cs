using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OrbBehavior_Base : MonoBehaviourPun {

    public int meat;
    protected Rigidbody2D body;
    public bool available = false;

    void Awake () {
        if (photonView.IsMine && this.GetType() == typeof(OrbBehavior_Base)) {
            gameObject.AddComponent<OrbBehavior_Local>();
            DestroyImmediate(this);
        }
        else {
            body = GetComponent<Rigidbody2D>();
            Fill((int) photonView.InstantiationData[0]);
        }
    }

// Note that fill() can also be used to shrink a bulb.
    [PunRPC]
    public void Fill (int howMuchMeat) {
        float orbScale = Mathf.Sqrt(howMuchMeat);
        transform.localScale = new Vector3(orbScale, orbScale, 1);
        meat = howMuchMeat;
    }

    [PunRPC]
    protected void SeekStage () {
        CircleCollider2D localCollider  = GetComponent<CircleCollider2D>();
        localCollider.isTrigger = true;
        localCollider.radius = 10;
    }

    [PunRPC]
    public void SetAvailable () {
        available = true;
    }

    [PunRPC]
    public void SetUnavailable () {
        available = false;
    }

}
