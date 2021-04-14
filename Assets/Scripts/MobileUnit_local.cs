using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class MobileUnit_local : Unit_local {
    public AidansMovementScript moveConductor;
    public Rigidbody2D body;

    protected override void dispenseOutranged() {
        if (task.nature == Task.actions.give || task.nature == Task.actions.take) {
            Transform toSeek = task.objectUnit.transform;
            Move(toSeek.position, toSeek.gameObject.GetPhotonView().ViewID, -1, 7);
        }      
    }
    
    public override void ignition () {
        StartForLocals();
        moveConductor = GetComponent<AidansMovementScript>();
        body = GetComponent<Rigidbody2D>();
        StartCoroutine("AllignRemotes");
    }

    public override void Move (Vector2 goTo, int leaderID = -1, float speed = -1, float arrivalThreshholdOverride = -1) {
        Vector2 nowGoing = body.velocity;
        Vector2 nowAt = transform.position;
        double whenToStart = PhotonNetwork.Time + PhotonNetwork.GetPing() * 0.0015;
        Transform leader = null;
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
        int noiseStartPoint = Random.Range(0, 100);
        photonView.RPC("Move", RpcTarget.Others, whenToStart, nowGoing.x, nowGoing.y, nowAt.x, nowAt.y, goTo.x, goTo.y, noiseStartPoint, leaderID, moveConductor.speed, arrivalThreshholdOverride);
        moveConductor.Go(goTo, whenToStart, noiseStartPoint, leader, speed, arrivalThreshhold); 
    }

    public virtual void PathEnded () {
        if (task != null) { 
            if (task.nature != Task.actions.move) {
                if (task.objectUnit.gameObject == null) {
                    task = null;
                }
                else {
                    switch (task.nature.ToString()) {
                        case "give":
                            StartCoroutine("dispense", null);
                            break;
                        case "take":
                            StartCoroutine("dispense", null);
                            break;
                        case "attack":
                            break;
                        default:
                            Debug.Log("a path-ended message was sent while there was no valid task");
                            break;
                    }
                }
            }
            else {
                cohort.taskCompleted(task);
                task = null;
            }
        }
    }

    [PunRPC]
    public override void StopMoving () {
        if (moveConductor.isRunning) {
            moveConductor.terminatePathfinding(false);
        }
    }

    IEnumerator AllignRemotes () {
        Vector2 pastPosition;
        float timeOfMostRecentMotion = 0;
        while (true) {
            Debug.Log("a");
            pastPosition = transform.position;
            yield return new WaitForSeconds(0.5f);            
            float discrepancy = Vector2.Distance(pastPosition, transform.position);
            if (discrepancy != 0) {
                Debug.Log("b");
                timeOfMostRecentMotion = Time.time;
            }
            if (Time.time - timeOfMostRecentMotion <= 2.1f) {
                Debug.Log("c");
                // Debug.Log("Transmitting nudge to " + photonView.ViewID);
                photonView.RPC("AuthoritativeNudge", RpcTarget.Others, transform.position.x, transform.position.y, body.velocity.x, body.velocity.y, PhotonNetwork.ServerTimestamp, moveConductor.currentWaypoint);
            }
        }
    }

}