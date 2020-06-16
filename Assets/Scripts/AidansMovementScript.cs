using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class AidansMovementScript : MonoBehaviour {
    Seeker seeker;
    ABPath path = null;
    public float speed = 2;
    public float changePointThreshhold = 0.5f;
    public float roundToArrived = 0.5f;
    int currentWaypoint = 0;

    void Start() {
        seeker = GetComponent<Seeker>();
    }

    public void setDestination (Vector3 destination) {
        seeker.StartPath(transform.position, destination, OnPathComplete);
        Debug.Log("Destination set."); 
    }

    void OnPathComplete (Path finishedPath) {
        Debug.Log("Yay, we got a path back. Did it have an error? " + finishedPath.error);
        path = (ABPath) finishedPath;
    }

    void Update() {
        if (path == null) {
            return;
        }
        if (Vector2.Distance(transform.position, path.endPoint) < roundToArrived) {
            path = null;
            Debug.Log("Destination reached. Path null.");
            return;
        }
        for (int i = 0; i < 1000; i++) {
            //if you are within a specified range of the next waypoint
            if (Vector2.Distance(transform.position, path.vectorPath[currentWaypoint]) < changePointThreshhold) {
                //and if the number of the next waypoint would not exceeed the number of waypoints in the path
                if (currentWaypoint + 1 < path.vectorPath.Count) {
                    //increment the currentWaypoint (I think there should be another break here, but it's not in the example)
                    currentWaypoint++;
                }
                else {
                    //end reached
                    path = null;
                    Debug.Log("Destination reached exactly. Path null.");
                    break;
                }
            }
            else {
                //no incrementing needed yet
                break;
            }
        }
        Vector3 dirNew = (path.vectorPath[currentWaypoint] - transform.position).normalized;
        transform.position += dirNew * speed * Time.deltaTime;
    }
}
