using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MobileUnit_remote : Unit_remote {
    AidansMovementScript moveConductor;
    Rigidbody2D body;

    public override void ignition () {
        moveConductor = GetComponent<AidansMovementScript>();
        statusBar.GetComponent<SpriteRenderer>().enabled = false;
        body = GetComponent<Rigidbody2D>();
    }

    [PunRPC]
    public void Move (float toX, float toY, int leaderID = -1, float speed = -1, float arrivalThreshholdOverride = -1) {
        Vector2 destination = new Vector2 (toX, toY);
        Transform leader = null;
        // float cutoff = AstarPath.active.data.gridGraph.nodeSize * 10;
        // if (Vector2.Distance(destination, transform.position) > cutoff) {
        //     destination = (Vector2) transform.position + (destination - (Vector2) transform.position).normalized * cutoff;
        // }
        if (leaderID != -1) {
            leader = PhotonNetwork.GetPhotonView(leaderID).transform;
        }
        float arrivalThreshhold;
        if (arrivalThreshholdOverride != -1) {
            arrivalThreshhold = bodyCircle.radius;
        }
        else {
            arrivalThreshhold = arrivalThreshholdOverride;
        }
        moveConductor.setDestination(destination, leader, speed, arrivalThreshhold);            
    }

    public void PathEnded() {}

    [PunRPC]
    void SmallMove (float toX, float toY) {
        gameState.smallMoveCount += 1;
        if (moveConductor.isRunning == true) {
            Vector2 goTo = new Vector2(toX, toY);
            moveConductor.LightRecalculate(goTo);
        }
        else {
            Move(toX, toY, -1, -1f, bodyCircle.radius);
        } 
    }

    [PunRPC]
    public virtual void StopMoving () {
        Debug.Log("StopMoving.");
// This is unconditional, because we want the velocity to be zeroed regardless of whether the legs are moving or not.
        moveConductor.terminatePathfinding(false, true);
    }

    [PunRPC]
    public void AuthoritativeNudge (float posX, float posY, float velX, float velY, int timeSent) {
        // Debug.Log("Recieved nudge for photonview #" + photonView.ViewID + ". Current velocity: " + body.velocity.magnitude);
        float secondsTranspired = (float) (PhotonNetwork.ServerTimestamp - timeSent) / 1000;
        Vector2 pastAuthorityPosition = new Vector2(posX, posY);
        Vector2 pastAuthorityVelocity = new Vector2(velX, velY);
        Vector2 estimatedAuthorityPosition = pastAuthorityPosition + (pastAuthorityVelocity * secondsTranspired);
        Vector2 offset = (Vector2) transform.position - estimatedAuthorityPosition;
        if (offset.magnitude < 0.05f) {
            body.position = estimatedAuthorityPosition;
        }
        else if (offset.magnitude > 2) {
            body.position = estimatedAuthorityPosition;
            body.velocity = pastAuthorityVelocity;
            if (moveConductor.isRunning) {
                moveConductor.LightRecalculate(moveConductor.placetoGo);
            }
        }
        else {
            Vector2 impulse = offset * -10;
            body.AddForce(impulse);
        }
        // Debug.Log("added " + impulse.ToString() + " to " + gameObject.name + ", based on a time difference of "
        //              + secondsTranspired + " and and estimated authority position " + Vector2.Distance(estimatedAuthorityPosition, transform.position) + " units away.");
    }

}