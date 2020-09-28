using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class AidansMovementScript : MonoBehaviour {
    Seeker seeker;
    ABPath path = null;
    Transform transToFollow = null;
    public float speed = 2;
    public float changePointThreshhold = 0.5f;
    public float roundToArrived = 0.5f;
    int currentWaypoint = 0;

    void Start() {
        seeker = GetComponent<Seeker>();
    }

    public void setDestination (Vector3 destination, Transform movingTransform = null) {
        if (movingTransform != null) {
            transToFollow = movingTransform;
        }
        //this IF is needed because the click handler defaults to a destination of 0,0,0 if it doesn't recognise a unit or the ground.
        if (destination != new Vector3(0,0,0)) {
            seeker.StartPath(transform.position, destination, OnPathComplete);
            currentWaypoint = 0;
        }
    }

    void OnPathComplete (Path finishedPath) {
        path = (ABPath) finishedPath;
    }

    void Update() {
        if (path == null) {
            return;
        }
        //The first criteria is just to stop the recalculation from happening every frame.
        if (Time.time % 0.5f <= 0.02 && transToFollow != null && transToFollow.hasChanged == true) {
            setDestination(transToFollow.position);
            currentWaypoint = 0;
        }
        if (Vector2.Distance(transform.position, path.endPoint) < roundToArrived) {
            terminatePathfinding();
            return;
        }
        //if you are within a specified range of the next waypoint
            if (Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]) < changePointThreshhold) {
                //and if the number of the next waypoint would not exceeed the number of waypoints in the path
                if (currentWaypoint + 1 < path.vectorPath.Count - 1) {
                    //increment the currentWaypoint (I think there should be another break here, but it's not in the example)
                    currentWaypoint++;
                }
                else {
                    //end reached
                    terminatePathfinding();
                    return;
                }
            }
        Vector3 dirNew = (path.vectorPath[currentWaypoint] - transform.position).normalized;
        transform.position += dirNew * speed * Time.deltaTime;
        gameObject.transform.hasChanged = true;
    }

    void terminatePathfinding () {
        path = null;
        currentWaypoint = 0;
        transToFollow = null;
        gameObject.transform.hasChanged = false;
    }
}
