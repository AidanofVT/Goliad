﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Dog_Attack_Visualizer : WeaponVisualizer {
    
    public override void Show () {
        Instantiate((GameObject) Resources.Load("bite"), (transform.position + thisWeapon.target.transform.position) / 2, Quaternion.identity);
    }

}
