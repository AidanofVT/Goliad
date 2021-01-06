﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Photon.Pun;

public class AidansMovementScript : MonoBehaviourPun {
    Seeker seeker;
    ABPath path = null;
    Vector3 placetoGo;
    Transform transToFollow = null;
    Rigidbody2D body;
    Collider2D selfToIgnore;
    Unit thisUnit;
//this boolean isn't used by this script, but it is needed for other scripts to register what's going on. toggling path to null and back doesn't work: a new path is spontaneously created for some reason
    public bool isRunning = false;
    public float speed;
    public float changePointThreshhold;
    public float roundToArrived = 0.1f;
    int currentWaypoint = 0;

    void Start() {
//The seeker is the script that branches into all the A* pathfinding stuff
        seeker = GetComponent<Seeker>();
        speed = GetComponent<UnitBlueprint>().speed;
        body = GetComponent<Rigidbody2D>();
        selfToIgnore = GetComponent<Collider2D>();
        thisUnit = GetComponent<Unit>();
        if (changePointThreshhold != 0) {
            changePointThreshhold = GetComponent<CircleCollider2D>().radius;
        }
    }

    public void setDestination (Vector3 destination, Transform movingTransform = null, float acceptableDistance = 0.1f) {
        placetoGo = destination;
        transToFollow = movingTransform;
        roundToArrived = acceptableDistance;
        InvokeRepeating("setRoute", 0, 2);       
        StartCoroutine("stuckCheck");
        photonView.RPC("startTurning", RpcTarget.All);
    }

    void setRoute () {
        if (transToFollow != null) {
            seeker.StartPath(transform.position, transToFollow.position, OnPathComplete);
        }
        else {
            seeker.StartPath(transform.position, placetoGo, OnPathComplete);
        }
        currentWaypoint = 0;
    }

//This function is needed because the seeker's startpath() function can only deliver it's output via a backwards-parameter, or whatever it's called.
//The name of the intermediary function, in this case OnePathComplete, is put as a parameter for StartPath (see lines 33 and 36), and the resulting path gets passed here as a parameter.
    void OnPathComplete (Path finishedPath) {
        path = (ABPath) finishedPath;
        InvokeRepeating("moveAlong", 0, .05f);
        isRunning = true;
    }

    void moveAlong() {
        if (Vector2.Distance(transform.position, path.endPoint) < roundToArrived) {
            terminatePathfinding();
            return;
        }
            try {
//if you are within a specified range of the next waypoint
                if (Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]) < changePointThreshhold) {
    //and if the number of the next waypoint would not exceeed the number of waypoints in the path
                    if (currentWaypoint + 1 <= path.vectorPath.Count - 1) {
        //increment the currentWaypoint
                        currentWaypoint++;
                    }
                    else {
                        //end reached
                        terminatePathfinding();
                        return;
                    }
                }
            }
            catch {
                Debug.Log("CAUGHT IT. Tried to access index " + currentWaypoint + " when the size of the path is " + path.vectorPath.Count + " entries long.");
            }
        Vector2 dirNew = (path.vectorPath[currentWaypoint] - transform.position).normalized * speed;
        if (Mathf.Sqrt(Mathf.Pow(body.velocity.x, 2) + Mathf.Pow(body.velocity.y, 2)) <= speed) {
            body.AddForce(neededPush(dirNew));
        }
        else {
            body.AddForce(body.velocity * -0.5f);
        }
    }

    IEnumerator stuckCheck () {
        Vector3 positionOneSecondAgo = transform.position;
        yield return new WaitForSeconds(1);
        while (true) {
            Vector3 change = transform.position - positionOneSecondAgo;
            if (change.magnitude < 0.1f) {
                // Debug.Log("jerking because this unit has moved " + change.magnitude + " in the last second.");
                Vector2 swayWay = (path.vectorPath[currentWaypoint] - transform.position).normalized;
                float temp  = swayWay.x;
                swayWay.x = swayWay.y * -1;
                swayWay.y = temp;
                if (Random.value < 0.5) {
                    swayWay *= -1;
                }
                body.AddForce(swayWay * 100);
            }
            positionOneSecondAgo = transform.position;
            yield return new WaitForSeconds(1);
        }
    }

    Vector2 neededPush (Vector2 desiredCourse) {
        return (desiredCourse - body.velocity);
    }

    public bool isNavigable (Vector3 where) {
        float girth = GetComponent<CircleCollider2D>().radius * transform.localScale.magnitude * 1.1f;
        Collider2D[] occupants = Physics2D.OverlapCircleAll(where, girth);
        List<Collider2D> listFormat = new List<Collider2D>(occupants);
        foreach (Collider2D contact in listFormat) {
            if (contact.tag == "unit" || contact.tag == "obstacle" || contact.tag == "out of bounds") {
                if (contact != selfToIgnore) {
                    //Debug.Log("Point obstructed by " + contact.name + ".");
                    return false;
                }
            }
        }
        return true;
    }

    public void terminatePathfinding (bool passUpward = true) {
        isRunning = false;
        CancelInvoke("moveAlong");
        StopCoroutine("stuckCheck");
        CancelInvoke("setRoute");
        photonView.SendMessage("stopTurning");
        roundToArrived = 0.1f;
        path = null;
        currentWaypoint = 0;
        transToFollow = null;
        body.velocity = new Vector2(0, 0);
        if (passUpward) {
            SendMessage("pathEnded");
        }
    }

}
