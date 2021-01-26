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
    public Vector2 center;
    public int radius;

    public Task (GameObject doneBy, actions doWhat, Vector2 where, GameObject doneTo = null, int howMuch = 0, int howWide = 0) {
        subjectUnit = doneBy;
        nature = doWhat;
        center = where;
        objectUnit = doneTo;
        quantity = howMuch;
        radius = howWide;
    }

}