using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class MobileUnit_local : Unit_local {
    public AidansMovementScript moveConductor;
    Rigidbody2D body;
    Vector2 pastPosition;

    protected override void dispenseOutranged() {
        if (task.nature == Task.actions.give || task.nature == Task.actions.take) {
            Transform toSeek = task.objectUnit.transform;
            moveConductor.setDestination(toSeek.position, toSeek, 7);
        }      
    }
    
    public override void ignition () {
        StartForLocals();
        moveConductor = GetComponent<AidansMovementScript>();
        body = GetComponent<Rigidbody2D>();
        StartCoroutine("allignRemotes");
    }

    [PunRPC]
    public void Move (float toX, float toY, int leaderID = -1, float speed = -1, float arrivalThreshholdOverride = -1) {
        Vector2 destination = new Vector2 (toX, toY);
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
        moveConductor.setDestination(destination, leader, speed, arrivalThreshhold);            
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

    public override void StopMoving () {
        if (moveConductor.isRunning) {
            moveConductor.terminatePathfinding(false);
        }
    }

    IEnumerator allignRemotes () {
        while (true) {
            pastPosition = transform.position;
            yield return new WaitForSeconds(0.5f);
            float discrepancy = Vector2.Distance(pastPosition, transform.position);
            if (discrepancy != 0) {
                photonView.RPC("AuthoritativeNudge", RpcTarget.Others, transform.position.x, transform.position.y, body.velocity.x, body.velocity.y, PhotonNetwork.ServerTimestamp);
            }
        }
    }

}