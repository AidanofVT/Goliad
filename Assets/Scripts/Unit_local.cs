using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Unit_local : Unit {
    protected ViewManager viewManager;
    protected Task task;

    void Awake () {
//can't this be moved to Unit.Start()?
        stats = GetComponent<UnitBlueprint>();
        if (this.GetType() == typeof(Unit_local)) {
            if (stats.isMobile == true) {
                gameObject.AddComponent<MobileUnit_local>();
                DestroyImmediate(this);
            }
        }
    }

    protected virtual void StartForLocals () {
        List<Unit_local> listOfOne = new List<Unit_local>();
        listOfOne.Add(this);
        soloCohort = new Cohort(listOfOne);
        cohort = soloCohort;
        gameState = GameObject.Find("Goliad").GetComponent<GameState>();
        gameState.enlivenUnit(gameObject);
        viewManager = GameObject.Find("Player Perspective").GetComponent<ViewManager>();
    }

    public override void ignition () {
        StartForLocals();
        int radius = Mathf.CeilToInt(GetComponent<CircleCollider2D>().radius);
        AstarPath.active.UpdateGraphs(new Bounds(transform.position, new Vector3 (radius, radius, 1)));
    }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public virtual void activate (bool activateOthersInCohort) {
        gameState.activateUnit(gameObject);
        gameObject.transform.GetChild(0).gameObject.SetActive(true);
        transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().localScale = new Vector3(1,1,1) * (Camera.main.orthographicSize / 5);
        if (cohort != null && activateOthersInCohort == true) {
            cohort.activate(this);
        }
    }

    public void attack (GameObject target) {
        weapon.engage(target);
    }
 
    public virtual void deactivate () {
        gameObject.transform.GetChild(0).gameObject.SetActive(false);
        gameState.deactivateUnit(gameObject);
    }

    [PunRPC]
    public override void die () {
        spindown();
    }

    public virtual void give (GameObject toWho, int howMuch) {
        task  = new Task(gameObject, toWho, Task.actions.give, howMuch);
        if (Vector2.Distance(transform.position, toWho.transform.position) < 10) {
            StartCoroutine(dispense());
        }
        else {
            CircleCollider2D newCollider = gameObject.AddComponent<CircleCollider2D>();
            newCollider.isTrigger = true;
            newCollider.radius = 10;
        }
    }

    protected virtual IEnumerator dispense () {
        int dispensed = 0;
        while (meat > 0 && dispensed < task.quantity && task.objectUnit.GetComponent<Unit>().roomForMeat() > 0) {
            if (Vector2.Distance(transform.position, task.objectUnit.transform.position) > 10) {
                task.quantity -= dispensed;
                dispenseOutranged();
                yield return null;
            }
            deductMeat(1);
            Vector3 startOut = (task.objectUnit.transform.position - transform.position).normalized * (GetComponent<CircleCollider2D>().radius + 1) + transform.position;
            GameObject newOrb = PhotonNetwork.Instantiate("Orb", startOut, Quaternion.identity);
            newOrb.GetComponent<Rigidbody2D>().velocity = (task.objectUnit.transform.position - transform.position).normalized * 4;
            StartCoroutine(dispenseThrow(newOrb, task.objectUnit));
            ++dispensed;
            yield return new WaitForSeconds(0.15f);
        }
        task = null;
        yield return null;
    }

    protected virtual void dispenseOutranged () { }

    protected virtual IEnumerator dispenseThrow (GameObject thrownOrb, GameObject recipient) {
        yield return new WaitForSeconds(0);
        thrownOrb.GetComponent<OrbBehavior_Local>().embark(recipient);
        yield return null;
    }

    void OnTriggerEnter2D(Collider2D contact) {
        if (contact.isTrigger == false && contact.gameObject == task.objectUnit) {
            StartCoroutine(dispense());
            Destroy(gameObject.GetComponents<CircleCollider2D>()[1]);
        }
    }

    void OnMouseExit() {
        viewManager.unpaintCohort(cohort);
    }

    protected void OnMouseOver() {
        if (transform.GetChild(0).gameObject.activeInHierarchy == false) {
            viewManager.paintCohort(cohort);
        }
    }

    void spindown () {
        GameObject orb = (GameObject)Resources.Load("Orb");
        float quantityMultiplier = (float) (meat / 20) + 1;
        for (; meat > 0; --meat) {
            Vector3 place = new Vector3(transform.position.x + Random.Range(-.5f, 0.5f) * quantityMultiplier, transform.position.y + Random.Range(-.5f, 0.5f) * quantityMultiplier, -.2f);
            GameObject lastOrb = PhotonNetwork.Instantiate("orb", place, Quaternion.identity);
            lastOrb.GetComponent<Rigidbody2D>().AddForce((lastOrb.transform.position - transform.position).normalized * Random.Range(0, 2) * quantityMultiplier);
        }
        gameState.deadenUnit(gameObject);
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    public override void takeHit (int power) {
        int roll = Random.Range(0, stats.toughness);
        if (roll + power >= stats.toughness) {
            photonView.RPC("deductStrike", RpcTarget.All);
            takeHit(roll + power - stats.toughness);
        }    
    }

}
