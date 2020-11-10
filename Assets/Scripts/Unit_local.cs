using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Unit_local : Unit {

    void Awake () {
        stats = GetComponent<UnitBlueprint>();
        if (this.GetType() == typeof(Unit_local)) {
            if (stats.isMobile == true) {
                gameObject.AddComponent<MobileUnit_local>();
                DestroyImmediate(this);
            }
        }
    }

    void Start() {
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        gameState.enlivenUnit(gameObject);
        meat = stats.startingMeat;
        transform.GetChild(1).gameObject.SetActive(true);
        statusBar = transform.GetChild(1).GetComponent<BarManager>();
        int radius = Mathf.CeilToInt(GetComponent<CircleCollider2D>().radius);
        AstarPath.active.UpdateGraphs(new Bounds(transform.position, new Vector3 (radius, radius, 1)));
    }

    public void activate () {
        gameState.activateUnit(gameObject);
        gameObject.transform.GetChild(0).gameObject.SetActive(true);
        transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().localScale = new Vector3(1,1,1) * (Camera.main.orthographicSize / 5);
    }

    public void deactivate () {
        gameObject.transform.GetChild(0).gameObject.SetActive(false);
        gameState.deactivateUnit(gameObject);
    }

    [PunRPC]
    public override void takeHit (int power) {
        int roll = Random.Range(0, stats.toughness);
        if (roll + power >= stats.toughness) {
            photonView.RPC("deductStrike", RpcTarget.All);
            takeHit(roll + power - stats.toughness);
        }    
    }

    public void attack (GameObject target) {
        Debug.Log("unit attacking");
        weapon.engage(target);
    }

    [PunRPC]
    public override void die () {
        spindown();
    } 

    void spindown () {
        GameObject orb = (GameObject)Resources.Load("Orb");
        List <OrbBehavior> createdOrbs = new List<OrbBehavior>();
        float quantityMultiplier = (float) (meat / 20) + 1;
        for (; meat > 0; --meat) {
            Vector3 place = new Vector3(transform.position.x + Random.Range(-.5f, 0.5f) * quantityMultiplier, transform.position.y + Random.Range(-.5f, 0.5f) * quantityMultiplier, -.2f);
            GameObject lastOrb = PhotonNetwork.Instantiate("orb", place, Quaternion.identity);
            createdOrbs.Add(lastOrb.GetComponent<OrbBehavior>());
            lastOrb.GetComponent<Rigidbody2D>().AddForce((lastOrb.transform.position - transform.position).normalized * Random.Range(0, 2) * quantityMultiplier);
            lastOrb.AddComponent<OrbBehavior>();
        }
        gameState.deadenUnit(gameObject);
        PhotonNetwork.Destroy(gameObject);
    }

}
