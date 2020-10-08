using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class AidansMovementScript : MonoBehaviour {
    Seeker seeker;
    ABPath path = null;
    Transform transToFollow = null;
    Rigidbody2D body;
//for some reason, paths don't stay null when they are set to null, so this is needed
    public bool isRunning = false;
    public float speed = 2;
    public float changePointThreshhold = 0.5f;
    public float roundToArrived = 0.1f;
    int currentWaypoint = 0;

    void Start() {
        seeker = GetComponent<Seeker>();
        body = GetComponent<Rigidbody2D>();
    }

    public void setDestination (Vector3 destination, Transform movingTransform = null) {
        if (movingTransform != null) {
            transToFollow = movingTransform;
        }
        //MAYBE OBSOLETE??? this IF is needed because the click handler defaults to a destination of 0,0,0 if it doesn't recognise a unit or the ground.
        if (destination != new Vector3(0,0,0)) {
            seeker.StartPath(transform.position, destination, OnPathComplete);
            currentWaypoint = 0;
        }
        InvokeRepeating("moveAlong", 0.5f, 0);
        isRunning = true;
    }

    void OnPathComplete (Path finishedPath) {
        path = (ABPath) finishedPath;
    }

    void moveAlong() {
        //The first criteria is just to stop the recalculation from happening every frame.
        if (transToFollow!= null) {
            setDestination(transToFollow.position);
        }
        else {
            setDestination(path.vectorPath[path.vectorPath.Count - 1]);
        }
        //currentWaypoint = 0;
        if (Vector2.Distance(transform.position, path.endPoint) < roundToArrived) {
            terminatePathfinding();
            return;
        }
//if you are within a specified range of the next waypoint
            if (Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]) < changePointThreshhold) {
//and if the number of the next waypoint would not exceeed the number of waypoints in the path
                if (currentWaypoint + 1 <= path.vectorPath.Count - 1) {
//increment the currentWaypoint (I think there should be another break here, but it's not in the example)
                    currentWaypoint++;
                }
                else {
                    //end reached
                    terminatePathfinding();
                    return;
                }
            }
        Vector2 dirNew = (path.vectorPath[currentWaypoint] - transform.position).normalized;
        if (Mathf.Sqrt(Mathf.Pow(body.velocity.x, 2) + Mathf.Pow(body.velocity.y, 2)) <= speed) {
            body.AddForce(neededPush(dirNew) * 10);
        }
        //transform.position += dirNew * speed * Time.deltaTime;
        gameObject.transform.hasChanged = true;
    }

    Vector2 neededPush (Vector2 desiredCourse) {
        return (desiredCourse - body.velocity);
    }

    void terminatePathfinding () {
        CancelInvoke("moveAlong");
        isRunning = false;
        path = null;
        currentWaypoint = 0;
        transToFollow = null;
        gameObject.transform.hasChanged = false;
        body.velocity = new Vector2(0, 0);
    }

}
