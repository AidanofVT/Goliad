﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Unit_local : Unit {
    protected ViewManager viewManager;
    public Task task;
    int dispensed = 0;

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
    }

    public void attack (GameObject target) {
        weapon.engage(target);
    }

    public void changeCohort (Cohort newCohort = null) {
        if (cohort != soloCohort) {
            cohort.removeMember(this);
            if (task != null && (task.nature == Task.actions.give || task.nature == Task.actions.take)) {
                cohort.TaskAbandoned(task, dispensed);
                Stop();
            }
        }
        if (newCohort == null) {
            newCohort = soloCohort;
        }
        else {
            newCohort.addMember(this);
        }
        cohort = newCohort;
    }
 
    public virtual void deactivate () {
        transform.GetChild(3).gameObject.SetActive(false);
        gameState.deactivateUnit(gameObject);
    }

    [PunRPC]
    public override void die () {
        SendMessage("deathProtocal", null, SendMessageOptions.DontRequireReceiver);
        spindown();
        if (cohort == soloCohort) {
            gameState.activeCohorts.Remove(soloCohort);
            gameState.activeCohortsChangedFlag = true;
        }
        else {
            cohort.removeMember(this);
        }
        gameState.deadenUnit(gameObject);
        PhotonNetwork.Destroy(gameObject);
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
        while (true) {
            if (from.GetComponent<Unit>().meat > 0 && dispensed < task.quantity && to.GetComponent<Unit>().roomForMeat() > 0) {
                if (Vector2.Distance(transform.position, task.objectUnit.transform.position) > 10) {
                    task.quantity -= dispensed;
                    dispenseOutranged();
                    StopCoroutine(dispense());
                    yield return null;
                }
                Transform toTrans = to.transform;
                Transform fromTrans = from.transform;
                Vector3 startOut = (Vector3) ((Vector2) toTrans.position - (Vector2) fromTrans.position).normalized * (from.GetComponent<CircleCollider2D>().radius + 0.5f) + fromTrans.position;
                int leftToDispense = task.quantity - dispensed;
                GameObject newOrb = spawnOrb(startOut, leftToDispense, from.GetComponent<Unit_local>());
                dispensed += newOrb.GetComponent<OrbMeatContainer>().meat;
                StartCoroutine(passOrb(to, newOrb));
            }
            else {
                StopMoving();
                break;
            }
            yield return new WaitForSeconds(0.2f);
        }
//this has to be this way because if the taskcompleted call comes before the null assignment, then task will be set to null while the next dispense coroutine is working on it.
        Task taskRecord = task;
        task = null;
        cohort.taskCompleted(taskRecord);
        yield return null;
    }

    protected virtual void dispenseOutranged () {
        CircleCollider2D newCollider = gameObject.AddComponent<CircleCollider2D>();
        newCollider.isTrigger = true;
        newCollider.radius = 10;
    }

    GameObject spawnOrb (Vector3 where, int poolSize, Unit_local pullFrom) {
        int payload;
        if (poolSize >= 70) {
            payload = Random.Range(5, 8);
        }
        else if (poolSize >= 20) {
            payload = Random.Range(3, 6);
        }
        else if (poolSize >= 5) {
            payload = Random.Range(2, 5);
        }
        else {
            payload = poolSize;
        }
        GameObject newOrb = PhotonNetwork.Instantiate("Orb", where, Quaternion.identity);
        newOrb.GetPhotonView().RPC("fill", RpcTarget.All, payload);
        if (pullFrom != null) {
            pullFrom.photonView.RPC("deductMeat", RpcTarget.All, payload);
        }
        return newOrb;
    }
    
    public IEnumerator passOrb (GameObject to, GameObject newOrb) {
        yield return new WaitForSeconds(0);
        OrbBehavior_Local inQuestion = newOrb.GetComponent<OrbBehavior_Local>();
        inQuestion.embark(to);
        yield return null;
    }

    public virtual void move (Vector2 goTo, GameObject toFollow) { }

    void OnTriggerEnter2D(Collider2D contact) {
        if (contact.isTrigger == false && task != null) {
            if (task.nature == Task.actions.give || task.nature == Task.actions.take) {
                if (contact.gameObject == task.objectUnit) {
                    StartCoroutine(dispense());
                    Destroy(gameObject.GetComponents<CircleCollider2D>()[1]);
                }
            }
        } 
    }

    void OnMouseExit() {
        viewManager.removeFromPalette(this);
    }

    void OnMouseEnter() {
        viewManager.addToPalette(this);
    }

    void spindown () {
        GameObject orb = (GameObject)Resources.Load("Orb");
        int drop = meat + (int) (stats.costToBuild * Random.Range(0.2f, 0.6f));
        float quantityMultiplier = (float) (drop / 15) + 1;
        while (drop > 0) {
            Vector3 place = new Vector3(transform.position.x + Random.Range(-.5f, 0.5f) * quantityMultiplier, transform.position.y + Random.Range(-.5f, 0.5f) * quantityMultiplier, -.2f);
            GameObject lastOrb = spawnOrb(place, drop, this);
            lastOrb.GetComponent<Rigidbody2D>().AddForce((lastOrb.transform.position - transform.position).normalized * Random.Range(0, 2) * quantityMultiplier);
            drop -= lastOrb.GetComponent<OrbMeatContainer>().meat;
        }
    }

    public void Stop () {
        StopCoroutine("dispense");
        StopAllCoroutines();
        CircleCollider2D[] circles = gameObject.GetComponents<CircleCollider2D>();
        if (circles.Length > 1) {
            Destroy(circles[1]);
        }
        if (stats.isMobile) {
            StopMoving();
        }
        if (stats.isArmed) {
            weapon.disengage();
        }
        dispensed = 0;
        task = null;
    }

    public virtual void StopMoving () {        
    }

    [PunRPC]
    public override void takeHit (int power) {
        int roll = Random.Range(0, stats.toughness);
        if (roll + power >= stats.toughness) {
            photonView.RPC("deductStrike", RpcTarget.All);
            takeHit(roll + power - stats.toughness);
        }    
    }

    public virtual void work (Task newTask) {
        Stop();
        task = newTask;
        if (task.nature == Task.actions.give || task.nature == Task.actions.take) {
            dispensed = 0;
            if (Vector2.Distance(transform.position, task.objectUnit.transform.position) < 10) {
                StartCoroutine("dispense");
            }
            else {
                dispenseOutranged();
            }
        }
        else if (task.nature == Task.actions.move) {
            Debug.Log("moving");
            move(task.center, task.objectUnit);
        }
    }
}
