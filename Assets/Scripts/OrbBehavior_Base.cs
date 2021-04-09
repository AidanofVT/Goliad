using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OrbBehavior_Base : MonoBehaviourPun {

    public int meat;
    protected Rigidbody2D body;
    protected CircleCollider2D localCollider;
    public bool available = false;

    void Awake () {
        if (photonView.IsMine && this.GetType() == typeof(OrbBehavior_Base)) {
            gameObject.AddComponent<OrbBehavior_Local>();
            DestroyImmediate(this);
        }
        else {
            body = GetComponent<Rigidbody2D>();
            localCollider = GetComponent<CircleCollider2D>();
            fill((int) photonView.InstantiationData[0]);
            int spawnerID = (int) photonView.InstantiationData[1];
            if (spawnerID != -1) {
                PhotonNetwork.GetPhotonView(spawnerID).GetComponent<Unit>().deductMeat(meat);
            }
        }
    }

// note that fill() can also be used to shrink a bulb
    [PunRPC]
    public void fill (int howMuchMeat) {
        float orbScale = Mathf.PI * howMuchMeat; 
        orbScale = Mathf.Sqrt(orbScale / Mathf.PI);
        transform.localScale = new Vector3(orbScale, orbScale, 1);
        meat = howMuchMeat;
    }

    [PunRPC]
    protected void seekStage () {
        StopCoroutine("launchStage");
        if (body != null) {
            Destroy(body);
        }
        localCollider.isTrigger = true;
        localCollider.radius = 10;
    }

    [PunRPC]
    public void setAvailable () {
        available = true;
    }

    [PunRPC]
    public void setUnavailable () {
        available = false;
    }

}
