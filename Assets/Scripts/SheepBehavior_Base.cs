using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SheepBehavior_Base : MonoBehaviourPun {
    
    protected GameState gameState;
    protected MapManager mapManager;
    protected Unit thisSheep;

    void Start() {
        if (photonView.IsMine && this.GetType() == typeof(SheepBehavior_Base)) {
// Meat bars shouldn't be visible on sheep, even if they're local:
            transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().sprite = null;
            gameObject.AddComponent<SheepBehavior_Local>();
            DestroyImmediate(this);
        }
        else {
            GameObject Goliad = GameObject.Find("Goliad");
            gameState = Goliad.GetComponent<GameState>();
            mapManager = Goliad.GetComponent<MapManager>();
            StartCoroutine("Start2");
        }
    }

    IEnumerator Start2 () {
        yield return new WaitForSeconds(0);
        thisSheep = GetComponent<Unit>();
        ChangeFaction(photonView.OwnerActorNr);
    }

    [PunRPC]
    public virtual void ChangeFaction (int factionNumber) {
        thisSheep.stats.factionNumber = factionNumber;
        if (factionNumber == 1) {
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_white");
            transform.GetChild(4).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_white_icon");
        }
        else if (factionNumber == 2) {
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_orange");
            transform.GetChild(4).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_orange_icon");
        }        
    }

}
