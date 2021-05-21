using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Unit_local : Unit {
    public Task task;
    public Task temporaryOverrideTask;
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

    public override void Ignition () {
        StartForLocals();
        int radius = Mathf.CeilToInt(bodyCircle.radius);
// Remember, if something here isn't being overridden in a MobileUnit script, it's for stationary units:
        AstarPath.active.UpdateGraphs(new Bounds(transform.position, new Vector3 (radius, radius, 1)));
    }

    protected virtual void StartForLocals () {
        soloCohort = new Cohort(new List<Unit_local>{this});
        cohort = soloCohort;
        AddMeat(stats.startingMeat);
    }


    public virtual void Activate () {
// This should only ever be called by the unit's cohort:
        if (blueCircle.activeInHierarchy == false) {
            gameState.ActivateUnit(this);
            blueCircle.SetActive(true);
            icon.sprite = highlightedIcon;
        }
    }

    public void ChangeCohort (Cohort newCohort = null) {
        if (cohort != soloCohort) {
            cohort.RemoveMember(this);
            if (task != null && (task.nature == Task.actions.give || task.nature == Task.actions.take)) {
                cohort.TaskAbandoned(task, dispensed);
            }
            Stop();
        }
        if (newCohort == null || newCohort.Equals(soloCohort)) {
            newCohort = soloCohort;
        }
        else {
            newCohort.AddMember(this);
        }
        cohort = newCohort;
    }
 
    public virtual void Deactivate () {
// This should only ever be called by the unit's cohort:
        blueCircle.SetActive(false);
        icon.sprite = defaultIcon;
        gameState.DeactivateUnit(this);
    }

    [PunRPC]
    public override IEnumerator Die () {
        if (deathThrows == false) {
            deathThrows = true;
            SendMessage("DeathProtocal", null, SendMessageOptions.DontRequireReceiver);
            yield return StartCoroutine(Spindown());
            cohort.RemoveMember(this);
            gameState.DeadenUnit(gameObject);
            PhotonNetwork.Destroy(gameObject);
        }
    }

    public virtual IEnumerator Dispense () {
        GameObject to;
        GameObject from;
        Task transaction = null;
        if (temporaryOverrideTask != null && (temporaryOverrideTask.nature == Task.actions.give || temporaryOverrideTask.nature == Task.actions.take)) {
            transaction = temporaryOverrideTask;
        }
        else if (task.nature == Task.actions.give || task.nature == Task.actions.take) {
            transaction = task;
        }
        else {
            throw new System.Exception("Problem: called Dispense() on a unit without its task or its temporaryOVerRideTask being a transaction.");
        }
        if (transaction.nature == Task.actions.give) {
            to = transaction.objectUnit.gameObject;
            from = transaction.subjectUnit.gameObject;
        }
        else {
            to = transaction.subjectUnit.gameObject;
            from = transaction.objectUnit.gameObject;
        }
        Transform toTrans = to.transform;
        Transform fromTrans = from.transform;
        while (true) {
            if (dispensed < transaction.quantity && from.GetComponent<Unit>().meat > 0 && to.GetComponent<Unit>().RoomForMeat() > 0) {
                if (Vector2.Distance(transform.position, transaction.objectUnit.transform.position) > 10) {
                    if (transaction != temporaryOverrideTask) {
                        task.quantity -= dispensed;
                        DispenseOutranged();
                        StopCoroutine("Dispense");
                    }
                    else {
// For now, temporaryOverrideTask is only used to represent a ranged unit giving bulbs to an allied ranged unit attacking the same target. We don't want the giver to interupt its
// attack just to move closer to the reciever, so in the case of either hoplite moving out of range, we'll just quit the transaction.
                        temporaryOverrideTask = null;
                    }
                    yield return null;
                }
                Vector3 startOut = (Vector3) ((Vector2) toTrans.position - (Vector2) fromTrans.position).normalized * (from.GetComponent<CircleCollider2D>().radius + 0.5f) + fromTrans.position;
                int leftToDispense = transaction.quantity - dispensed;
// Meat deduction and "dispensed" incrementation are done in SpawnOrb().
                GameObject newOrb = SpawnOrb(startOut, leftToDispense, from.GetComponent<Unit_local>());
                StartCoroutine(PassOrb(to, newOrb));
            }
            else {
                StopMoving(false); // photonView.RPC("StopMoving", RpcTarget.All, false);
                break;
            }
            yield return new WaitForSeconds(0.2f);
        }
// This condition is here because dispense can be called from places other than the cohort, like when an attacking ranged unit refills itself.
        if (transaction == task) {
// This has to be this way because if the taskCompleted() call comes before the null assignment, then task will be set to null while the next dispense coroutine is working on it.
            Task taskRecord = task;
            task = null;
            cohort.TaskCompleted(taskRecord);
            dispensed = 0;
        }
        yield return null;
    }

    protected virtual void DispenseOutranged () {
// Note that this is for use by stationary units. Mobile units use a different mechanism to determine when a target has reentered range: PathEnded().
        CircleCollider2D newCollider = gameObject.AddComponent<CircleCollider2D>();
        newCollider.isTrigger = true;
        newCollider.radius = 10;
    }
  
    protected override void NotifyCohortOfMeatChange(int difference) {
        cohort.orbs += difference;
    }

    public IEnumerator PassOrb (GameObject to, GameObject newOrb) {
        yield return new WaitForSeconds(0);
        newOrb.GetComponent<OrbBehavior_Local>().Embark(to);
        yield return null;
    }

    void OnTriggerEnter2D(Collider2D contact) {
// Note that this is for use by stationary units. Mobile units use a different mechanism to determine when a target has reentered range: PathEnded().
        if (contact.isTrigger == false && task != null) {
            if (task.nature == Task.actions.give || task.nature == Task.actions.take) {
                if (contact.gameObject == task.objectUnit) {
                    StartCoroutine("Dispense");
                    Destroy(gameObject.GetComponents<CircleCollider2D>()[1]);
                }
            }
        } 
    }

    public void OnMouseEnter() {
        viewManager.AttendTo(this);
    }

    public void OnMouseExit() {
        viewManager.Disregard();
    }

    GameObject SpawnOrb (Vector3 where, int poolSize, Unit_local pullFrom) {
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
        GameObject newOrb = PhotonNetwork.Instantiate("Orb", where, Quaternion.identity, 0, new object[]{payload});
        DeductMeat(payload);
        dispensed += payload;
        return newOrb;
    }
  
    IEnumerator Spindown () {
        int remainingToDrop = meat + (int) (stats.costToBuild * Random.Range(0.2f, 0.6f));
        float quantityFactor = (float) (remainingToDrop / 70f) + 1;
// This is to spread it out over two physics computation cycles, reducing frame-time spikes and making a nicer explosion.
        int dropsPerFrame = remainingToDrop / 2;
        for (int i = 1; i > 0; --i){
            while (remainingToDrop > dropsPerFrame * i) {
                Vector3 place = new Vector3(transform.position.x + Random.Range(-.5f, 0.5f) * quantityFactor, transform.position.y + Random.Range(-.5f, 0.5f) * quantityFactor, -.2f);
                GameObject lastOrb = SpawnOrb(place, remainingToDrop, this);
                Vector2 explosiveForce = (lastOrb.transform.position - transform.position).normalized * Random.value * quantityFactor * 3;
                Debug.Log(explosiveForce);
                lastOrb.GetComponent<Rigidbody2D>().AddForce(explosiveForce);
                remainingToDrop -= lastOrb.GetComponent<OrbBehavior_Base>().meat;
            }
            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }

    public void Stop () {
        StopAllCoroutines();
        CircleCollider2D[] circles = gameObject.GetComponents<CircleCollider2D>();
        if (circles.Length > 1) {
            Destroy(circles[1]);
        }
        StopMoving(false); //photonView.RPC("StopMoving", RpcTarget.All, false);
        if (stats.isArmed && task != null && task.nature == Task.actions.attack) {
            weapon.photonView.RPC("Disengage", RpcTarget.All);
        }
        dispensed = 0;
        task = null;
    }

    [PunRPC]
    public override void TakeHit (int power) {
        if (deathThrows == false) {
            int landedStrikes = 0;
            while (power > 0) {
                int roll = Random.Range(0, stats.toughness);
                if (roll + power >= stats.toughness) {
                    ++landedStrikes;
                }
                power = roll + power - stats.toughness;
            }
            photonView.RPC("DeductStrikes", RpcTarget.All, landedStrikes);  
        }
    }

    public virtual void Work (Task newTask) {
        Stop();
        task = newTask;
        if (task.nature == Task.actions.give || task.nature == Task.actions.take) {
            if (Vector2.Distance(transform.position, task.objectUnit.transform.position) < 10) {
                StartCoroutine("Dispense");
            }
            else {
                DispenseOutranged();
            }
        }
        else if (task.nature == Task.actions.attack) {
            weapon.photonView.RPC("Engage", RpcTarget.All, task.objectUnit.photonView.ViewID);
        }
        else if (task.nature == Task.actions.move) {
            int leaderID = -1;
            if (task.objectUnit != null) {
                leaderID = task.objectUnit.photonView.ViewID;
            }
            float speed = -1;
            if (newTask.dataA != 0) {
                speed = newTask.dataA;
            }
            float arrivalThreshold = -1;
            if (cohort.members.Count > 1 && cohort.masterTask.nature == Task.actions.move) {
                arrivalThreshold = bodyCircle.radius;
            }
            Move(newTask.center, leaderID, speed, arrivalThreshold);
        }
    }
}
