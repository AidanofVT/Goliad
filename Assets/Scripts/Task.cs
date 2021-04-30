using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Task {

    public Unit_local subjectUnit;
    public Unit objectUnit;
    public enum actions {give, take, move, attack, help, build};
    public actions nature;
    public int quantity;
    public Vector2 center;
    public float dataA;

    public Task (Unit_local doneBy, actions doWhat, Vector2 where, Unit doneTo = null, int howMuch = 0, float extraData = 0) {
        subjectUnit = doneBy;
        nature = doWhat;
        center = where;
        objectUnit = doneTo;
        quantity = howMuch;
        dataA = extraData;
    }

}