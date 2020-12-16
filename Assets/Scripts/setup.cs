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
        Physics2D.IgnoreLayerCollision(10, 5);
        Physics2D.IgnoreLayerCollision(10, 8);
        Physics2D.IgnoreLayerCollision(5, 8);
        Physics2D.IgnoreLayerCollision(5, 11);
        Physics2D.queriesHitTriggers = false;
    }

    void Start () {
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
        int me = PhotonNetwork.LocalPlayer.ActorNumber ;
        int distanceFromCenter = (int) (0.3f * (float) mapSize);
        Vector3 startPlace = Vector3.zero;
        if (me == 1) {
            startPlace = new Vector3 (-distanceFromCenter, distanceFromCenter, -.2f);
        }
        else if (me == 2) {
            startPlace = new Vector3 (distanceFromCenter, -distanceFromCenter, -.2f);
        }
        GameObject home = PhotonNetwork.Instantiate("Units/homebase", startPlace, Quaternion.identity);
        //StartCoroutine("step2", home);
    }

    IEnumerator step2 (GameObject home) {
        factory_functions maker = home.GetComponent<factory_functions>();
        yield return new WaitForSeconds(0);
        AstarPath.active.UpdateGraphs(new Bounds(Vector3.zero, new Vector3 (4, 4, 1)));
        GameObject hoplite = maker.makeUnit("Hoplite");
        hoplite.transform.position = home.transform.position / 4;
        GameObject dog1 = maker.makeUnit("dog");
        dog1.transform.position = new Vector2(3, 3);
        GameObject dog2 = maker.makeUnit("dog");
        dog2.transform.position = new Vector2(3, -3);
        GameObject dog3 = maker.makeUnit("dog");
        dog3.transform.position = new Vector2(-3, -3);
        GameObject dog4 = maker.makeUnit("dog");
        dog4.transform.position = new Vector2(-3, 3);
        GameObject courier = maker.makeUnit("courier");
        courier.transform.position = Vector2.zero;
        yield return new WaitForSeconds(0);
        Unit_local[] dogsMembers = {dog1.GetComponent<Unit_local>(), dog2.GetComponent<Unit_local>(), dog3.GetComponent<Unit_local>(), dog4.GetComponent<Unit_local>()};
        Cohort dogs = new Cohort(new List<Unit_local>(dogsMembers));
        courier.GetComponent<Unit_local>().addMeat(30);
        courier.GetComponent<Unit_local>().cohort.commenceTransact(new Task(courier, dog1, Task.actions.give));
    }

    private void OnPlayerConnected() {
        Debug.Log("Someone joined.");
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {
        Debug.Log("Randomly connecting to a room failed. MESSAGE: " + message);
    }

}
