using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class setup : MonoBehaviourPunCallbacks {
    public GameState gameState;
    public int mapSize = 100;

    void Awake() {
        gameState = gameObject.GetComponent<GameState>();
//10 = ground, 5 = UI, 8 = obstacles, 11 = units
        Physics2D.IgnoreLayerCollision(10, 11);
        Physics2D.IgnoreLayerCollision(10, 8);
        Physics2D.IgnoreLayerCollision(10, 5);
        Physics2D.IgnoreLayerCollision(5, 11);
        Physics2D.IgnoreLayerCollision(5, 8);
        Physics2D.IgnoreLayerCollision(5, 5);
        Physics2D.queriesHitTriggers = false;
    }

    void Start () {
        PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 360000;
        // PhotonNetwork.SendRate = 4;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster() {
        Debug.Log("Connected to master. " + PhotonNetwork.CountOfRooms + " rooms open in " + PhotonNetwork.ServerAddress + ". Game version " + PhotonNetwork.AppVersion + ". Player ID = " + PhotonNetwork.LocalPlayer.UserId);
        if (PhotonNetwork.CountOfRooms == 0) {
            PhotonNetwork.CreateRoom(Random.Range(0, 10000).ToString());
        }
        else {
            PhotonNetwork.JoinRandomRoom();
        }
    }

    public override void OnDisconnected(DisconnectCause cause) {
        Debug.Log("PUN connection failure. CAUSE: " + cause);
    }

    public override void OnJoinedRoom() {
        Debug.Log("Joined room " + PhotonNetwork.CurrentRoom.Name + ". Player number " + PhotonNetwork.LocalPlayer.ActorNumber);
        int me = PhotonNetwork.LocalPlayer.ActorNumber;
        gameState.playerNumber = me;
        int distanceFromCenter = (int) (0.05f * (float) mapSize);
        Vector3 startPlace = Vector3.zero;
        if (me == 1) {
            startPlace = new Vector3 (-distanceFromCenter, distanceFromCenter, -.2f);
        }
        else if (me == 2) {
            startPlace = new Vector3 (distanceFromCenter, -distanceFromCenter, -.2f);
        }
        // Camera.main.transform.position = startPlace + new Vector3(0, 0, -9.8f);
        StartCoroutine(step2(startPlace));
    }

    IEnumerator step2 (Vector3 startPlace) {
        GameObject home = PhotonNetwork.Instantiate("Units/depot", startPlace, Quaternion.identity);
        yield return new WaitForSeconds(0);
        home.GetComponent<Unit>().addMeat(500); //(270);
        AstarPath.active.UpdateGraphs(new Bounds(Vector3.zero, new Vector3 (4, 4, 1)));
//      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        Vector3 spot = startPlace * -1 + new Vector3(4, 0, -0.4f);
        GameObject enemy = PhotonNetwork.Instantiate("Units/dog", spot, Quaternion.identity);
        while (true) {
            if (enemy == null || enemy.activeInHierarchy == false) {
                enemy = PhotonNetwork.Instantiate("Units/dog", spot, Quaternion.identity);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnPlayerConnected() {
        Debug.Log("Someone joined.");
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {
        Debug.Log("Randomly connecting to a room failed. MESSAGE: " + message);
    }

    void Update () {
        if (Input.GetButtonDown("debug overlay toggle")) {
            DebugStuff.BuildDebugger thiScript = Camera.main.transform.parent.GetComponent<DebugStuff.BuildDebugger>();
            if (thiScript.enabled == false) {
                thiScript.enabled = true;
            }
            else {
                thiScript.enabled = false;
            }
        }
    }

}
