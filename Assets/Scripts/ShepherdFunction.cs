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

    public void Chime () {
        foreach (Collider2D contact in Physics2D.OverlapCircleAll(transform.position, 20)) {
            if (contact.name.Contains("sheep") == true && flock.Contains(contact.gameObject) == false) {
                flock.Add(contact.gameObject);
            }
        }
        foreach (GameObject ward in flock) {
            PhotonView inQuestion = ward.GetPhotonView();
            inQuestion.RPC("HearChime", inQuestion.Owner, photonView.ViewID);
        }
    }

    void DeathProtocal () {
        foreach (GameObject ward in flock) {
            PhotonView inQuestion = ward.GetPhotonView();
            inQuestion.RPC("ShepherdDied", inQuestion.Owner);
        }
    }

    [PunRPC]
    void SheepDeparts (int pView) {
        flock.Remove(PhotonNetwork.GetPhotonView(pView).gameObject);
    }

}
