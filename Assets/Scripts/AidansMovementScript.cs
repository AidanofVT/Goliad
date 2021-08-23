﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Photon.Pun;

// NOTICE: Much of the functionality herein is designed to facilitate a future change to synchronized simulations using deterministic physics. It may seem unusual or unwieldy, but it's
// working fine with photontransformviews, so let's keep it like this.

public class AidansMovementScript : MonoBehaviourPun {
// WARNING: keep everything possible private because allowing any variable to be changed by means OTHER THAN a Go() RPC call will likely cause desynchronization.
    Seeker seeker;
    ABPath path = null;
    Vector3 placetoGo;
    Transform transToFollow = null;
    Rigidbody2D body;
    CircleCollider2D selfCollider;
    Unit thisUnit;
// The purpose of scheduledStart is to add some delay embarking on all instances, so that they can all start at the same PhotonNetwork.Time time.
    double scheduledStart;
// This boolean isn't used by this script, but it is needed for other scripts to register what's going on. toggling path to null and back doesn't work: a new path is spontaneously created for some reason
    [SerializeField]
    bool amRunning = false;
    Coroutine alongMover = null;
    float baseSpeed;
    [SerializeField]
    float speed;
    [SerializeField]
    float changePointThreshhold;
    [SerializeField]
    float roundToArrived = 0.15f;
    float pushInterval = 0.1f;
    int currentWaypoint = 0;
    int noisePoint;

    void Start() {
        seeker = GetComponent<Seeker>();
        baseSpeed = GetComponent<UnitBlueprint>().speed;
        speed = baseSpeed;
        body = GetComponent<Rigidbody2D>();
        selfCollider = GetComponent<CircleCollider2D>();
        thisUnit = GetComponent<Unit>();
        changePointThreshhold = baseSpeed * pushInterval * 3;
    }

    IEnumerator Brake () {
        body.drag = 10;
        yield return new WaitForSeconds(0.5f);
        body.drag = 0.5f;
    }

    float deviation () {
        if (currentWaypoint <= 0) {
            throw new System.Exception ("Tried to find deviation from path before starting on path.");
        }
        float sideALength = Vector2.Distance(path.vectorPath[currentWaypoint], path.vectorPath[currentWaypoint - 1]);
        float sideBLength = Vector2.Distance(path.vectorPath[currentWaypoint], transform.position);
        float sideCLength = Vector2.Distance(path.vectorPath[currentWaypoint - 1], transform.position);
        float halfPerimeter = (sideALength + sideBLength + sideCLength) / 2;
// This is called "Heron's formula."
        float areaOfImaginaryTriangle = Mathf.Sqrt(halfPerimeter * (halfPerimeter - sideALength) * (halfPerimeter - sideBLength) * (halfPerimeter - sideCLength));
// Imagine a rectangle with the same base as the triangle (the current leg of the path) and twice the area. It must have the same height as the triangle.
// Divide by the length of the base to find the height.
        float height = areaOfImaginaryTriangle * 2 / sideALength;
        return height;
    }

    public void Go (Vector2 destination, double startWhen, int noiseStart, int destinationTransformPID, float giddyup = -1, float acceptableDistance = -1) {
        // Debug.Log("Moving from " + transform.position + " to " + (Vector2) destination + ". Scheduled to start at " + startWhen);
        scheduledStart = startWhen;
        noisePoint = noiseStart;
        placetoGo = new Vector3(destination.x, destination.y, transform.position.z);
        if (destinationTransformPID != -1) {
            transToFollow = PhotonNetwork.GetPhotonView(destinationTransformPID).transform;
        }
        if (acceptableDistance != -1){
            roundToArrived = acceptableDistance;
        }
        CancelInvoke("SetRoute");
        body.drag = 1000;
        SetRoute();
        if (giddyup != -1) {
            speed = giddyup;
        }
        amRunning = true;
    }

    public float GetArrivalThreshold () {
        return roundToArrived;
    }

    public bool GetRunningState() {
        return amRunning;
    }

    public float GetSpeed () {
        return speed;
    }

    public bool IsNavigable (Vector3 where, bool ignoreMobileUnits = false) {      
        Collider2D[] occupants = Physics2D.OverlapCircleAll(where, selfCollider.radius);
        List<Collider2D> listFormat = new List<Collider2D>(occupants);
        foreach (Collider2D contact in listFormat) {
            if (contact.tag == "obstacle" || contact.tag == "out of bounds" || (ignoreMobileUnits == false && contact.tag == "unit")) {
                if (contact != selfCollider) {
                    //Debug.Log("Point obstructed by " + contact.name + ".");
                    return false;
                }
            }
        }
        return true;
    }

    private IEnumerator MoveAlong() {
// Similar to scheduledStart, these two values are used to ensure that all instances apply impulses with as much synchronicity as possible.
        int synchronicityItterator = 0;
        double nextMoveTime = scheduledStart;
        while (true) {
// This allows the script to itterate more than one waypoint per cycle if the unit is close enough to a later waypoint, smoothing movement in some cases.
            for (int i = Mathf.Clamp(currentWaypoint + 2, 0, path.vectorPath.Count - 2); i >= currentWaypoint; --i) { 
                if (Vector2.Distance(transform.position, path.vectorPath[i]) < changePointThreshhold) {                    
                    currentWaypoint = i + 1;
                    break;
                }
            }
            if (deviation() > Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]) || (transToFollow != null && currentWaypoint == path.vectorPath.Count - 1)) {
                SetRoute();
            }
            yield return new WaitUntil(() => PhotonNetwork.Time >= nextMoveTime);                   
            if (Vector2.Distance(transform.position, path.endPoint) > roundToArrived) {
                float distanceToEnd = Vector2.Distance(transform.position, path.endPoint);
                if (distanceToEnd < speed && transToFollow == null) {
                    body.drag = Mathf.Clamp(speed - distanceToEnd, 0.5f, 100);
                }
                else {
                    body.drag = 0.5f;
                }
                if (body.velocity.magnitude <= speed) {
                    Vector2 idealCourse = ((Vector2)path.vectorPath[currentWaypoint] - (Vector2)transform.position).normalized * speed;
                    Vector2 hit = (idealCourse - body.velocity) * 10;
                    // Debug.Log("hitting it with a " + hit + ".  Position is now " + body.position);
                    // if (name.Contains("dog")) {
                    //     Debug.Log((Vector2)path.vectorPath[currentWaypoint] - (Vector2)transform.position);
                    // }
                    body.AddForce(hit);                    
                }
                // Debug.Log("Velocity = " + body.velocity.magnitude);
            }
            else if (transToFollow != null && transToFollow.GetComponent<AidansMovementScript>() != null && transToFollow.GetComponent<AidansMovementScript>().amRunning == true) {
// This is the case of a unit catching up to a followed unit that hasn't stopped yet.
                SetRoute();
                synchronicityItterator += 5;
            }
            else {
                break;
            }
            nextMoveTime = (scheduledStart + pushInterval * ++synchronicityItterator); //% 4294967.295;
        }
        TerminatePathfinding(true, true);  
        yield return null;
    }

// This function is needed because the seeker's startpath() function can only deliver it's output via a backwards-parameter, or whatever it's called.
// The intermediary function, in this case OnePathComplete, is put as a parameter for StartPath (see lines 33 and 36), and the resulting path gets passed here as a parameter.
    void OnPathComplete (Path finishedPath) {
// This IF is necessary because sometimes TerminatePathfinding() will be called while a path is being computed, resulting in the path being delivered when the unit shouldn't be moving.
        if (amRunning == true) {
            path = (ABPath) finishedPath;
            if (alongMover == null) {
                currentWaypoint = 0;
                photonView.RPC("StartTurning", RpcTarget.All);
                StopCoroutine("MoveAlong");
                alongMover = StartCoroutine("MoveAlong");
                StopCoroutine("Brake");
                StartCoroutine("StuckCheck");
            }
            else {
                currentWaypoint = 1;
            }
        }
    }

    void SetRoute () {
        // Debug.Log("Calculating route...");
        if (transToFollow != null) {
            placetoGo = transToFollow.position;
            Vector3 offset = placetoGo - transform.position;
// This just accounts for the minimum proximity afforded by the two units' colliders.
            placetoGo = transform.position + offset * ((offset.magnitude - transToFollow.GetComponent<CircleCollider2D>().radius - thisUnit.bodyCircle.radius) / offset.magnitude);
        }
        seeker.StartPath(transform.position, placetoGo, OnPathComplete);
    }

    IEnumerator StuckCheck () {
        int synchronicityItterator = 0;
// This reduces instances where units that were made together check and jerk at the same time, making for stickier traffic jams.
        double nextCheckTime = PhotonNetwork.Time + (Mathf.Abs(transform.position.x) % 10) / 5;
        while (true) {
            yield return new WaitUntil(() => PhotonNetwork.Time >= nextCheckTime);
            if (body.velocity.magnitude < 0.04f) {
                Vector2 swayWay = new Vector2(Mathf.PerlinNoise(noisePoint + 0.5f, 1234), Mathf.PerlinNoise(noisePoint + 0.5f, 5678)) * (Mathf.PerlinNoise(1, noisePoint + 0.5f) - 0.5f) * 50; // = (path.vectorPath[currentWaypoint] - transform.position).normalized;
                // Debug.Log("Jerking " + swayWay + " because this unit has moved " + body.velocity.magnitude + " in the last second.");
                body.AddForce(swayWay);
                noisePoint += 1;
            }
            ++synchronicityItterator;
            nextCheckTime = (scheduledStart + synchronicityItterator); // % 4294967.295;            
        }
    }

    public void TerminatePathfinding (bool passUpward, bool brakeStop) {
        // Debug.Log("unit " + photonView.ViewID + " terminatePathfinding at " + transform.position);
        StopCoroutine("MoveAlong");
        StopCoroutine("StuckCheck");
        CancelInvoke("SetRoute");
        photonView.RPC("StopTurning", RpcTarget.All);
        amRunning = false;
        roundToArrived = 0.15f;
        speed = baseSpeed;
        path = null;
        alongMover = null;
        currentWaypoint = 0;
        transToFollow = null;
// If movement is being stopped by logic outside of this script, the calling location should take care of everything related to stopping. Sending the "pathended" message might cause infinite loops.
        if (passUpward) {
            SendMessage("PathEnded");
        }
        if (brakeStop) {
            StartCoroutine("Brake");
        }
    }

}
