using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SheepBehavior_Base : MonoBehaviourPun {

    void Start() {
        if (photonView.Owner.ActorNumber == 1) {
            transform.GetChild(2).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_white");
        }
        else if (photonView.Owner.ActorNumber == 2) {
            transform.GetChild(2).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_orange");
        }
        if (photonView.IsMine && this.GetType() == typeof(SheepBehavior_Base)) {
            gameObject.AddComponent<SheepBehavior_Local>();
            DestroyImmediate(this);
        }
    }

    [PunRPC]
    public void transferOwnership (Player newOwner, int [] flockPhotonViews, int [] farFlockPhotonViews, Vector2 flockCenter, int shepherdphotonView, Vector3 currentMostAppealingPatch) {
        Debug.Log("transferOwnership");
        if (photonView.IsMine) {
            gameObject.AddComponent<SheepBehavior_Base>();
            photonView.TransferOwnership(newOwner);
            DestroyImmediate(this);
            return;
        }
        else if (PhotonNetwork.LocalPlayer == newOwner) {
            SheepBehavior_Local newScript =  gameObject.AddComponent<SheepBehavior_Local>();
            newScript.configure(flockPhotonViews, farFlockPhotonViews, flockCenter, shepherdphotonView, currentMostAppealingPatch);
            DestroyImmediate(this);
        }
    }

    [PunRPC]
    public virtual void hearChime (int chimerPhotonID) {

    }

}
