using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class AidansMovementScript : MonoBehaviour {
    Seeker seeker;
    ABPath path = null;
    Vector3 placetoGo;
    Transform transToFollow = null;
    Rigidbody2D body;
    Vector3 positionOneSecondAgo;
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
        //MAYBE OBSOLETE??? this IF is needed because the click handler defaults to a destination of 0,0,0 if it doesn't recognise a unit or the ground.
        placetoGo = destination;
        transToFollow = movingTransform;
        InvokeRepeating("setRoute", 0, 2);       
        InvokeRepeating("stuckCheck", 1, 1);
        isRunning = true;
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

    void OnPathComplete (Path finishedPath) {
        path = (ABPath) finishedPath;
        InvokeRepeating("moveAlong", 0, .05f); 
    }

    void moveAlong() {
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
        Vector2 dirNew = (path.vectorPath[currentWaypoint] - transform.position).normalized * speed;
        if (Mathf.Sqrt(Mathf.Pow(body.velocity.x, 2) + Mathf.Pow(body.velocity.y, 2)) <= speed) {
            body.AddForce(neededPush(dirNew));
        }
        else {
            body.AddForce(body.velocity * -0.5f);
        }
        //transform.position += dirNew * speed * Time.deltaTime;
        positionOneSecondAgo = transform.position;
    }

    void stuckCheck () {
        Vector3 change = transform.position - positionOneSecondAgo;
        if (change.magnitude < 0.1) {
            Vector2 swayWay = (path.vectorPath[currentWaypoint] - transform.position).normalized;
            float temp  = swayWay.x;
            swayWay.x = swayWay.y;
            swayWay.y = temp;
            if (Random.value < 0.5) {
                swayWay *= -1;
            }
            body.AddForce(swayWay * 10);
        }
    }

    Vector2 neededPush (Vector2 desiredCourse) {
        return (desiredCourse - body.velocity);
    }

    void terminatePathfinding () {
        CancelInvoke("moveAlong");
        CancelInvoke("stuckCheck");
        CancelInvoke("setRoute");
        isRunning = false;
        path = null;
        currentWaypoint = 0;
        transToFollow = null;
        body.velocity = new Vector2(0, 0);
    }

}
