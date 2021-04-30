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
        // StartCoroutine("AllignRemotes");
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
        photonView.RPC("Move", RpcTarget.Others, whenToStart, nowGoing.x, nowGoing.y, nowAt.x, nowAt.y, goTo.x, goTo.y, noiseStartPoint, leaderID, moveConductor.GetSpeed(), arrivalThreshholdOverride);
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
    public override void StopMoving (bool brakeStop) {
        if (moveConductor.getRunningState() == true) {
            moveConductor.terminatePathfinding(false, brakeStop);
        }
    }

}