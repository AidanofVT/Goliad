using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SheepBehavior_Base : MonoBehaviourPun {

    void Start() {
        changeFaction(photonView.OwnerActorNr);
        if (photonView.IsMine && this.GetType() == typeof(SheepBehavior_Base)) {
            gameObject.AddComponent<SheepBehavior_Local>();
            Debug.Log("changing to SheepBehavior_Local");
            DestroyImmediate(this);
        }
    }

    [PunRPC]
    public virtual void changeFaction (int factionNumber) {
        Debug.Log("changing color to " + factionNumber);
        if (factionNumber == 1) {
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_white");
        }
        else if (factionNumber == 2) {
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_orange");
        }        
    }

    [PunRPC]
    public virtual void hearChime (int chimerPhotonID) {

    }

}
