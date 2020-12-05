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

    public virtual void activate () {
        gameState.activateUnit(gameObject);
        transform.GetChild(3).gameObject.SetActive(true);
        //transform.GetChild(3).GetChild(1).GetComponent<RectTransform>().localScale = new Vector3(1,1,1) * (Camera.main.orthographicSize / 5);
    }

    public void attack (GameObject target) {
        weapon.engage(target);
    }

    public void changeCohort (Cohort newCohort = null) {
        cohort.removeMember(this);
        if (newCohort == null) {
            cohort = soloCohort;
        }
        else {
            newCohort.addMember(this);
            cohort = newCohort;
        }
    }
 
    public virtual void deactivate () {
        transform.GetChild(3).gameObject.SetActive(false);
        gameState.deactivateUnit(gameObject);
    }

    [PunRPC]
    public override void die () {
        spindown();
    }

    protected virtual IEnumerator dispense () {
        GameObject to;
        GameObject from;
        if (task.nature == Task.actions.give) {
            to = task.objectUnit;
            from = task.subjectUnit;
        }
        else {
            to = task.subjectUnit;
            from = task.objectUnit;
        }
        Transform toTrans = to.transform;
        Transform fromTrans = from.transform;
        int dispensed = 0;
        while (from.GetComponent<Unit>().meat > 0 && dispensed < task.quantity && to.GetComponent<Unit>().roomForMeat() > 0) {
            if (Vector2.Distance(transform.position, task.objectUnit.transform.position) > 10) {
                task.quantity -= dispensed;
                if (task.nature == Task.actions.give) {
                    dispenseOutranged();
                }
                yield return null;
            }
            from.GetComponent<Unit>().deductMeat(1);
            Vector3 startOut = (toTrans.position - fromTrans.position).normalized * (from.GetComponent<CircleCollider2D>().radius + 1) + fromTrans.position;
            GameObject newOrb = PhotonNetwork.Instantiate("Orb", startOut, Quaternion.identity);
            newOrb.GetComponent<Rigidbody2D>().velocity = (toTrans.position - fromTrans.position).normalized * 4;
            StartCoroutine(dispenseThrow(newOrb, to));
            ++dispensed;
            yield return new WaitForSeconds(0.15f);
        }
        task = null;
        yield return null;
    }

    protected virtual void dispenseOutranged () {
        CircleCollider2D newCollider = gameObject.AddComponent<CircleCollider2D>();
        newCollider.isTrigger = true;
        newCollider.radius = 10;
    }

    protected virtual IEnumerator dispenseThrow (GameObject thrownOrb, GameObject recipient) {
        yield return new WaitForSeconds(0);
        thrownOrb.GetComponent<OrbBehavior_Local>().embark(recipient);
        yield return null;
    }

    public virtual void give (GameObject toWho, int howMuch) {
        Debug.Log("initiating give");
        task  = new Task(gameObject, toWho, Task.actions.give, howMuch);
        if (Vector2.Distance(transform.position, toWho.transform.position) < 10) {
            StartCoroutine(dispense());
        }
        else {
            dispenseOutranged();
        }
    }

    void OnTriggerEnter2D(Collider2D contact) {
        if (contact.isTrigger == false && contact.gameObject == task.objectUnit) {
            StartCoroutine(dispense());
            Destroy(gameObject.GetComponents<CircleCollider2D>()[1]);
        }
    }

    void OnMouseExit() {
        viewManager.removeFromPalette(this);
    }

    protected void OnMouseEnter() {
        viewManager.addToPalette(this);
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

    public virtual void take (GameObject fromWho, int howMuch) {
        Debug.Log("initiating take");
        task = new Task(gameObject, fromWho, Task.actions.take, howMuch);
        if (Vector2.Distance(transform.position, fromWho.transform.position) < 10) {
            StartCoroutine(dispense());
        }
        else {
            dispenseOutranged();
        }
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
