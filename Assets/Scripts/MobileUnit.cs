using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileUnit : Unit {
    protected AidansMovementScript moveConductor;

    void Awake () {
        string prefab = gameObject.name;
        prefab = prefab.Remove(prefab.IndexOf("("));
        prefab = "Sprites/" + prefab;
        if (photonView.Owner.ActorNumber == 1) {
            GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(prefab + "_white");
        }
        else if (photonView.Owner.ActorNumber == 2) {
            GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(prefab + "_orange");
        }
        if (this.GetType() == typeof(MobileUnit)) {
            Unit replacement = null;
            if (photonView.IsMine) {
                replacement = gameObject.AddComponent<MobileUnit_local>();

            }
            else {
                replacement = gameObject.AddComponent<MobileUnit_remote>();
            }
            Destroy(this);            
        }
    }

    private void Start() {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        moveConductor = gameObject.GetComponent<AidansMovementScript>();        
    }
    
    public virtual void move (Vector3 target, Transform movingTransform = null) {
        moveConductor.setDestination(target, movingTransform);
    }

}