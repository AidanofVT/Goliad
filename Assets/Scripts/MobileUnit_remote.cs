using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MobileUnit_remote : Unit_remote {
    AidansMovementScript moveConductor;

    public override void Ignition () {
        moveConductor = GetComponent<AidansMovementScript>();
        statusBar.GetComponent<SpriteRenderer>().enabled = false;
        AddMeat(stats.startingMeat);
    }

    [PunRPC]
    public void Move (double scheduledStartTime, float velX, float velY, float fromX, float fromY, float toX, float toY, int jerkSeed,
                        int leaderID = -1, float speed = -1, float arrivalThreshholdOverride = -1) {
        body.position = new Vector2(fromX, fromY);
        body.velocity = new Vector2(velX, velX);
        Vector2 destination = new Vector2 (toX, toY);
        moveConductor.Go(destination, scheduledStartTime, jerkSeed, leaderID, speed, arrivalThreshholdOverride);            
    }

    public void PathEnded() {}

    [PunRPC]
    public override void StopMoving (bool brakeStop) {
// We don't care about whether the unit is under way or not, because we want the velocity to be zeroed regardless of whether the legs are moving or not.
        moveConductor.TerminatePathfinding(false, brakeStop);
    }

    // [PunRPC]
    // public void AuthoritativeNudge (float posX, float posY, float velX, float velY, int timeSent) {
    //     Debug.Log("Recieved nudge for photonview #" + photonView.ViewID + ". Current velocity: " + body.velocity.magnitude);
    //     float secondsTranspired = (float) (PhotonNetwork.ServerTimestamp - timeSent) / 1000;
    //     Vector2 pastAuthorityPosition = new Vector2(posX, posY);
    //     Vector2 pastAuthorityVelocity = new Vector2(velX, velY);
    //     Vector2 estimatedAuthorityPosition = pastAuthorityPosition + (pastAuthorityVelocity * secondsTranspired);
    //     Vector2 offset = estimatedAuthorityPosition - (Vector2) transform.position;
    //     if (offset.magnitude < 0.05f) {
    //         body.position = estimatedAuthorityPosition;
    //     }
    //     else if (offset.magnitude > 2) {
    //         body.position = estimatedAuthorityPosition;
    //         body.velocity = pastAuthorityVelocity;
    //     }
    //     else {
    //         Vector2 impulse = offset * 8 * Mathf.Pow(offset.magnitude, 2.5f);
    //         body.AddForce(impulse);
    //     }
    // }

}