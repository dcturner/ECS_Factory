﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorageBot : MonoBehaviour {

    Storage parent;
    public Vector3 Destination = Vector3.zero;
    public Transform[] slots;
    private Transform t;

    public void Init(Storage _parent){
        parent = _parent;
        t = transform;
        Destination = t.position;
    }

    private void Update()
    {
        // move towards Dest
        t.position += (Destination - t.position) / 2;

        // rotate: look at dest
        t.LookAt(Destination);
    }
}
