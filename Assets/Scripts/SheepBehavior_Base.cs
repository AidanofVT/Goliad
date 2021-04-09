using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SheepBehavior_Base : MonoBehaviourPun {
    
    protected GameObject Goliad;
    protected GameState gameState;
    protected MapManager mapManager;
    protected Unit thisSheep;
    public int alliedFaction = 0;

    void Start() {
        changeFaction(photonView.OwnerActorNr);
        if (photonView.IsMine && this.GetType() == typeof(SheepBehavior_Base)) {
            transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().sprite = null;
            gameObject.AddComponent<SheepBehavior_Local>();
            DestroyImmediate(this);
        }
        else {
            Goliad = GameObject.Find("Goliad");
            gameState = Goliad.GetComponent<GameState>();
            mapManager = Goliad.GetComponent<MapManager>();
            StartCoroutine("Start2");
        }
    }

    IEnumerator Start2 () {
        yield return new WaitForSeconds(0.2f);
        thisSheep = GetComponent<Unit>();

    }

    [PunRPC]
    public virtual void changeFaction (int factionNumber) {
        alliedFaction = factionNumber;
        if (factionNumber == 1) {
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_white");
            transform.GetChild(4).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_white_icon");
        }
        else if (factionNumber == 2) {
            transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_orange");
            transform.GetChild(4).gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/sheep_orange_icon");
        }        
    }

    [PunRPC]
    void Consume (int patchIndexX, int patchIndexY) {
        if (mapManager.exploitPatch(new Vector2Int(patchIndexX, patchIndexY)) == true)  {
            thisSheep.addMeat(1);
        }
    }

    [PunRPC]
    public virtual void hearChime (int chimerPhotonID) {

    }

}
