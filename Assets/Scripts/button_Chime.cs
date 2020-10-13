using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class button_Chime : MonoBehaviour {
    public void onPress () {
        transform.parent.parent.parent.gameObject.GetComponent<ShepherdFunction>().chime();
        Debug.Log("button pressed");
    }
}
