using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ShepherdFunction : MonoBehaviourPun {
    public List<GameObject> flock = new List<GameObject>();

    void Start () {
        if (photonView.IsMine == false) {
            Destroy(this);
        }
    }

    public void chime () {
        foreach (Collider2D contact in Physics2D.OverlapCircleAll(transform.position, 20)) {
            if (contact.gameObject.GetComponent<SheepBehavior_Base>() != null) {
                if (flock.Contains(contact.gameObject) == false) {
                    flock.Add(contact.gameObject);
                }             
            }
        }
        foreach (GameObject ward in flock) {
            ward.GetPhotonView().RPC("hearChime", RpcTarget.All, photonView.ViewID);
        }
    }

    void deathProtocal () {
        foreach (GameObject ward in flock) {
                ward.GetComponent<SheepBehavior_Local>().shepherd = null;
        }
    }

    [PunRPC]
    void SheepDeparts (int pView) {
        flock.Remove(PhotonNetwork.GetPhotonView(pView).gameObject);
    }

}
