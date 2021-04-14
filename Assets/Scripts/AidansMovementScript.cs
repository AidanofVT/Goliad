using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Photon.Pun;

public class AidansMovementScript : MonoBehaviourPun {
    Seeker seeker;
    ABPath path = null;
    public Vector3 placetoGo;
    Transform transToFollow = null;
    Rigidbody2D body;
    CircleCollider2D selfToIgnore;
    Unit thisUnit;
    double scheduledStart;
//this boolean isn't used by this script, but it is needed for other scripts to register what's going on. toggling path to null and back doesn't work: a new path is spontaneously created for some reason
    public bool isRunning = false;
    float baseSpeed;
    float girth;
    public float speed;
    public float changePointThreshhold;
    public float roundToArrived = 0.15f;
    public int currentWaypoint = 0;
    int noisePoint;

    void Start() {
        seeker = GetComponent<Seeker>();
        baseSpeed = GetComponent<UnitBlueprint>().speed;
        speed = baseSpeed;
        body = GetComponent<Rigidbody2D>();
        selfToIgnore = GetComponent<CircleCollider2D>();
        girth = selfToIgnore.radius;
        thisUnit = GetComponent<Unit>();
        changePointThreshhold = girth * 1.5f;
    }
    
    public void Go (Vector2 destination, double startWhen, int noiseStart, Transform movingTransform = null, float giddyup = -1, float acceptableDistance = -1) {
        // Debug.Log("Moving from " + transform.position + " to " + (Vector2) destination + ". Scheduled to start at " + startWhen);
        scheduledStart = startWhen;
        noisePoint = noiseStart;
        // Debug.Log("Should start at " + scheduledStart);
        placetoGo = new Vector3(destination.x, destination.y, transform.position.z);
        transToFollow = movingTransform;
        if (acceptableDistance != -1){
            roundToArrived = acceptableDistance;
        }
        else if (transToFollow != null) {
            roundToArrived = changePointThreshhold;
        }
        if  (isRunning == false) {
            InvokeRepeating("SetRoute", 0, 2);
        }
        else {
            SetRoute();
        }
        if (giddyup != -1) {
            speed = giddyup;
        }
    }

    public bool isNavigable (Vector3 where, bool ignoreMobileUnits = false) {        
        Collider2D[] occupants = Physics2D.OverlapCircleAll(where, girth);
        List<Collider2D> listFormat = new List<Collider2D>(occupants);
        foreach (Collider2D contact in listFormat) {
            if (contact.tag == "obstacle" || contact.tag == "out of bounds" || (ignoreMobileUnits == false && contact.tag == "unit")) {
                if (contact != selfToIgnore) {
                    //Debug.Log("Point obstructed by " + contact.name + ".");
                    return false;
                }
            }
        }
        return true;
    }

// Only for use without a leading transform:
    public void LightRecalculate (Vector2 destination) {
        // Debug.Log("lightRecalculate");
        transToFollow = null;
        placetoGo = destination;
        SetRoute();
    }

    private IEnumerator MoveAlong() {
        int synchronicityItterator = 0;
        double nextMoveTime = scheduledStart;
        yield return new WaitUntil(() => PhotonNetwork.Time >= nextMoveTime);
        while (true) {
            ++synchronicityItterator;
            // Debug.Log("Moving along at " + PhotonNetwork.Time + ". The ultimate goal is " + path.endPoint);
            try {
//if you are within a specified range of the next waypoint
                if (Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]) < changePointThreshhold) {
    //and if the number of the next waypoint would not exceeed the number of waypoints in the path
                    if (currentWaypoint + 1 <= path.vectorPath.Count - 1) {
        //increment the currentWaypoint
                        currentWaypoint++;                        
                    }
                }
            }
            catch {
                Debug.Log("CAUGHT IT. Tried to access index " + currentWaypoint + " when the size of the path is " + path.vectorPath.Count + " entries long.");
            }
            if (Vector2.Distance(transform.position, path.endPoint) > roundToArrived) {
                if (Vector2.Distance(transform.position, path.endPoint) < speed && transToFollow == null) {
                    body.drag = Mathf.Clamp(body.velocity.magnitude, 0.5f, 20);
                }
                else {
                    body.drag = 0.5f;
                }
                if (body.velocity.magnitude <= speed) {
                    Vector2 idealCourse = ((Vector2)path.vectorPath[currentWaypoint] - (Vector2)transform.position).normalized * speed;
                    Vector2 hit = idealCourse - body.velocity;
                    // Debug.Log("hitting it with a " + hit + ".  Position is now " + body.position);
                    body.AddForce(hit);
                    
                }
                nextMoveTime = (scheduledStart + 0.1 * synchronicityItterator); //% 4294967.295;
                yield return new WaitUntil(() => PhotonNetwork.Time >= nextMoveTime);
            }
            else if (transToFollow != null && transToFollow.GetComponent<AidansMovementScript>() != null && transToFollow.GetComponent<AidansMovementScript>().isRunning == true) {
// This is the case of a unit catching up to a followed unit that hasn't stopped yet.
                // Debug.Log("Caught up.");
                nextMoveTime = (scheduledStart + 0.3 * synchronicityItterator); // % 4294967.295;
                yield return new WaitUntil(() => PhotonNetwork.Time >= nextMoveTime);
                CancelInvoke("SetRoute");
                InvokeRepeating("SetRoute", 0, 2);
            }
            else {
                break;
            }
        }
        body.drag = 10;
        yield return new WaitForSeconds(0.5f);
        terminatePathfinding(true);  
        yield return null;
    }

//This function is needed because the seeker's startpath() function can only deliver it's output via a backwards-parameter, or whatever it's called.
//The intermediary function, in this case OnePathComplete, is put as a parameter for StartPath (see lines 33 and 36), and the resulting path gets passed here as a parameter.
    void OnPathComplete (Path finishedPath) {
        path = (ABPath) finishedPath;
        // string debugOut = transform.position + ", ";
        // foreach (Vector3 point in path.vectorPath) {
        //     debugOut += point + ", ";
        // }
        // Debug.Log(debugOut);
        currentWaypoint = 0;
        StopCoroutine("MoveAlong");
        StartCoroutine("MoveAlong");
        if (isRunning == false) {
            thisUnit.startTurning();
            StartCoroutine("stuckCheck");
            isRunning = true;
        }
    }

    void SetRoute () {
        if (transToFollow != null) {
            seeker.StartPath(transform.position, transToFollow.position, OnPathComplete);
            // Debug.Log("moving to follow " + transToFollow.position);
        }
        else {
            seeker.StartPath(transform.position, placetoGo, OnPathComplete);
            // Debug.Log("moving to " + placetoGo);
        }
    }

    IEnumerator stuckCheck () {
        int synchronicityItterator = 0;
        double nextCheckTime = PhotonNetwork.Time + (Mathf.Abs(transform.position.x) % 10) / 5;
// This reduces instances where units that were made together check and jerk at the same time, making for stickier traffic jams. The extra second is to allow the path to finish computing.
        while (true) {
            yield return new WaitUntil(() => PhotonNetwork.Time >= nextCheckTime);
            // Debug.Log("Stuckchecking at " + PhotonNetwork.Time + ". Some time past the target of " + nextCheckTime);
            if (body.velocity.magnitude < 0.04f) {
                try {
                    Vector2 swayWay = new Vector2(Mathf.PerlinNoise(noisePoint + 0.5f, 1234), Mathf.PerlinNoise(noisePoint + 0.5f, 5678)) * (Mathf.PerlinNoise(1, noisePoint + 0.5f) - 0.5f) * 50; // = (path.vectorPath[currentWaypoint] - transform.position).normalized;
                // Debug.Log("Jerking " + swayWay + " because this unit has moved " + body.velocity.magnitude + " in the last second.");
                    body.AddForce(swayWay);
                    noisePoint += 1;
                }
                catch {
                }
            }
            ++synchronicityItterator;
            nextCheckTime = (scheduledStart + synchronicityItterator); // % 4294967.295;            
        }
    }

    [PunRPC]
    void syncNoise (int incomingSeed) {
        noisePoint = incomingSeed;
    }

    public void terminatePathfinding (bool passUpward = true, bool hardStop = false) {
        // Debug.Log("terminatePathfinding at " + transform.position);
        StopCoroutine("MoveAlong");
        StopCoroutine("stuckCheck");
        StopCoroutine("GuideRemotes");
        CancelInvoke("SetRoute");
        thisUnit.stopTurning();
        if (hardStop == true) {
            body.velocity = new Vector2(0, 0);
        }
        isRunning = false;
        roundToArrived = 0.15f;
        speed = baseSpeed;
        path = null;
        currentWaypoint = 0;
        transToFollow = null;
        body.drag = 0.5f;
        if (passUpward) {
            SendMessage("PathEnded");
        }
    }

}
