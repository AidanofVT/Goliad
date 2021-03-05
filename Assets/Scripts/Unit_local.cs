using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Unit_local : Unit {
    public Task task;
    public CircleCollider2D bodyCircle;
    int dispensed = 0;

    void Awake () {
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
        bodyCircle = GetComponent<CircleCollider2D>();
        icon.gameObject.AddComponent<IconMouseContactBridge>();
    }

    public override void ignition () {
        StartForLocals();
        int radius = Mathf.CeilToInt(bodyCircle.radius);
        AstarPath.active.UpdateGraphs(new Bounds(transform.position, new Vector3 (radius, radius, 1)));
    }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public virtual void activate () {
// This should only be called by the unit's cohort.
        if (blueCircle.activeInHierarchy == false) {
            gameState.activateUnit(this);
            blueCircle.SetActive(true);
            icon.sprite = highlightedIcon;
        }
    }

    public void changeCohort (Cohort newCohort = null) {
        if (cohort != soloCohort) {
            cohort.removeMember(this);
            if (task != null && (task.nature == Task.actions.give || task.nature == Task.actions.take)) {
                cohort.TaskAbandoned(task, dispensed);
            }
            Stop();
        }
        if (newCohort == null || newCohort.Equals(soloCohort)) {
            newCohort = soloCohort;
        }
        else {
            newCohort.addMember(this);
        }
        cohort = newCohort;
    }
 
    public virtual void deactivate () {
// This should only be called by the unit's cohort.
        blueCircle.SetActive(false);
// if there were ever a case where the unit were deactivated but needed to remain highlighted, like for targeting, this could be a problem 
        icon.sprite = defaultIcon;
        gameState.deactivateUnit(this);
    }

    [PunRPC]
    public override void die () {
        SendMessage("deathProtocal", null, SendMessageOptions.DontRequireReceiver);
        spindown();
        if (cohort != soloCohort) {
            cohort.removeMember(this);
        }
        DeathNotice();
        gameState.deadenUnit(gameObject);
        PhotonNetwork.Destroy(gameObject);
    }

    public virtual IEnumerator dispense (GameObject to = null, GameObject from = null) {
        if (to == null && from == null) {
            if (task.nature == Task.actions.give) {
                to = task.objectUnit.gameObject;
                from = task.subjectUnit.gameObject;
            }
            else {
                to = task.subjectUnit.gameObject;
                from = task.objectUnit.gameObject;
            }
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
//this condition is here because dispense can be called from places other than the cohort, like when an attacking raged unit refills itself.
        if (task.nature == Task.actions.take || task.nature == Task.actions.give) {
//this has to be this way because if the taskcompleted call comes before the null assignment, then task will be set to null while the next dispense coroutine is working on it.
            Task taskRecord = task;
            task = null;
            cohort.taskCompleted(taskRecord);
            dispensed = 0;
        }
        yield return null;
    }

    protected virtual void dispenseOutranged () {
        CircleCollider2D newCollider = gameObject.AddComponent<CircleCollider2D>();
        newCollider.isTrigger = true;
        newCollider.radius = 10;
    }
  
    public IEnumerator passOrb (GameObject to, GameObject newOrb) {
        yield return new WaitForSeconds(0);
        OrbBehavior_Local inQuestion = newOrb.GetComponent<OrbBehavior_Local>();
        inQuestion.embark(to);
        yield return null;
    }

    public virtual void move (Vector2 goTo, Unit toFollow) { }

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

    public void OnMouseEnter() {
        viewManager.addToPalette(this);
    }

    public void OnMouseExit() {
        viewManager.removeFromPalette(this);
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
            weapon.Disengage();
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
        else if (task.nature == Task.actions.attack) {
            weapon.engage(task.objectUnit.gameObject);
        }
        else if (task.nature == Task.actions.move) {
            move(task.center, task.objectUnit);
        }
    }
}
