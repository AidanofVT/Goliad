using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Task {

    public GameObject subjectUnit;
    public GameObject objectUnit;
    public enum actions {give, take, move, attack, help};
    public actions nature;
    public int quantity;

    public Task (GameObject doneBy, GameObject doneTo, actions doWhat, int howMuch = 0) {
        subjectUnit = doneBy;
        objectUnit = doneTo;
        nature = doWhat;
        quantity = howMuch;
    }

}