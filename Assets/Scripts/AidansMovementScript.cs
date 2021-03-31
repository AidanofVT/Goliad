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
//this boolean isn't used by this script, but it is needed for other scripts to register what's going on. toggling path to null and back doesn't work: a new path is spontaneously created for some reason
    public bool isRunning = false;
    float baseSpeed;
    float girth;
    public float speed;
    public float changePointThreshhold;
    public float roundToArrived = 0.15f;
    int currentWaypoint = 0;

    void Start() {
        seeker = GetComponent<Seeker>();
        baseSpeed = GetComponent<UnitBlueprint>().speed;
        speed = baseSpeed;
        body = GetComponent<Rigidbody2D>();
        selfToIgnore = GetComponent<CircleCollider2D>();
        girth = selfToIgnore.radius;
        thisUnit = GetComponent<Unit>();
        changePointThreshhold = girth;
    }

    IEnumerator GuideRemotes () {
        while (true) {
            int upcomingNode = Mathf.Clamp(currentWaypoint + (int) (speed * 3f), 0, path.vectorPath.Count - 1);
            Vector2 shortHop = path.vectorPath[upcomingNode];
            photonView.RPC("SmallMove", RpcTarget.Others, shortHop.x, shortHop.y);
            yield return new WaitForSeconds(1.5f);
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
        Debug.Log("lightRecalculate");
        transToFollow = null;
        CancelInvoke("setRoute");
        placetoGo = destination;
        InvokeRepeating("setRoute", 0, 2);
    }

    IEnumerator MoveAlong() {
        while (Vector2.Distance(transform.position, path.endPoint) > roundToArrived) {
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
            Vector2 dirNew = (path.vectorPath[currentWaypoint] - transform.position).normalized * speed;
            if (body.velocity.magnitude <= speed) {
                body.AddForce(neededPush(dirNew));
            }
// Remote instances should be able to exceed a unit's given speed in order to catch up, and we want a little grace-zone to smooth things out.
            else if (body.velocity.magnitude > speed * 1.2f) {
                body.AddForce(body.velocity * -0.5f);
            }
            yield return new WaitForSeconds(0.05f);
        }
        if (transToFollow == null || transToFollow.GetComponent<AidansMovementScript>() == null || transToFollow.GetComponent<AidansMovementScript>().isRunning == false) {
            body.drag = 10;
            yield return new WaitForSeconds(0.5f);
            body.drag = 0.5f;
            terminatePathfinding(true);
        }
        else {
// This is the case of a unit following a moving unit that hasn't stopped yet.
            StopCoroutine("moveAlong");
        }
        yield return null;
    }

    Vector2 neededPush (Vector2 desiredCourse) {
        return (desiredCourse - body.velocity);
    }

//This function is needed because the seeker's startpath() function can only deliver it's output via a backwards-parameter, or whatever it's called.
//The intermediary function, in this case OnePathComplete, is put as a parameter for StartPath (see lines 33 and 36), and the resulting path gets passed here as a parameter.
    void OnPathComplete (Path finishedPath) {
        path = (ABPath) finishedPath;
        currentWaypoint = 0;
        if (photonView.IsMine && isRunning == false){      
            StartCoroutine("stuckCheck");
            StartCoroutine("GuideRemotes");
        }
        StartCoroutine("MoveAlong");
        isRunning = true;
    }

    public void setDestination (Vector2 destination, Transform movingTransform = null, float giddyup = -1, float acceptableDistance = -1) {
        // Debug.Log("Moving from " + transform.position + " to " + (Vector2) destination);
        StopCoroutine("MoveAlong");
        StopCoroutine("stuckCheck");
        CancelInvoke("setRoute");
        photonView.RPC("stopTurning", RpcTarget.All);
        placetoGo = new Vector3(destination.x, destination.y, transform.position.z);
        transToFollow = movingTransform;
        if (acceptableDistance != -1){
            roundToArrived = acceptableDistance;
        }
        InvokeRepeating("setRoute", 0, 2);
        photonView.RPC("startTurning", RpcTarget.All);
        if (giddyup != -1) {
            speed = giddyup;
        }
    }

    void setRoute () {
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
        Vector3 positionOneSecondAgo = transform.position;
// This reduces instances where units that were made together check and jerk at the same time, making for stickier traffic jams. The extra second is to allow the path to finish computing.
        yield return new WaitForSeconds(1 + (Mathf.Abs(transform.position.x) % 10) / 5);
        while (true) {
            if (body.velocity.magnitude < 0.04f) {
                // Debug.Log("jerking because this unit has moved " + body.velocity.magnitude + " in the last second.");
                try {
                    Vector2 swayWay = (path.vectorPath[currentWaypoint] - transform.position).normalized;
                    float temp  = swayWay.x;
                    swayWay.x = swayWay.y * -1;
                    swayWay.y = temp;
                    if (Random.value < 0.5) {
                        swayWay *= -1;
                    }
                    // Debug.Log("jerk");
                    body.AddForce(swayWay * Random.Range(0, 100));
                }
                catch {
                }
            }
            positionOneSecondAgo = transform.position;
            yield return new WaitForSeconds(1);
        }
    }

    public void terminatePathfinding (bool passUpward = true, bool hardStop = false) {
        Debug.Log("terminatePathfinding at " + transform.position);
        StopCoroutine("MoveAlong");
        StopCoroutine("stuckCheck");
        StopCoroutine("GuideRemotes");
        CancelInvoke("setRoute");
        photonView.RPC("stopTurning", RpcTarget.All);
        if (hardStop == true) {
            body.velocity = new Vector2(0, 0);
        }
        isRunning = false;
        roundToArrived = 0.15f;
        speed = baseSpeed;
        path = null;
        currentWaypoint = 0;
        transToFollow = null;
        if (passUpward) {
            SendMessage("PathEnded");
        }
    }

}
