using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#region ------------------------- < VEHICLE PARTS

// Recipe for a vehicle type


public enum Vehicle_PartType
{
    CHASSIS,
    ENGINE,
    WHEEL,
    STEERING,
    SEAT,
    DOOR,
    GLASS,
    EXHAUST
}

// A group of these make up a VehicleDesign - (4 wheels, one engine, 2 seats <-- 7 components)
public struct VehicleDesign_RequiredPart
{
    public string name;
    public VehiclePart_Config partConfig;
    public Vector3 position;
    public Quaternion rotation;

    public VehicleDesign_RequiredPart (string _name, VehiclePart_Config _partConfig, Vector3 _position, Quaternion _rotation)
    {
        name = _name;
        partConfig = _partConfig;
        position = _position;
        rotation = _rotation;
    }
}


public struct Vehicle
{
    public VehicleDesign vehicleDesign;
    public List<VehiclePart_Config> parts;
    public int age, id;

    public Vehicle(VehicleDesign _vehicleDesign, int _id)
    {
        vehicleDesign = _vehicleDesign;
        parts = new List<VehiclePart_Config>();
        age = 0;
        id = _id;
    }

    public void AttachPart(VehiclePart_Config _newPartConfig, VehicleDesign_RequiredPart _requiredPart,
        int _index)
    {
        parts[_index] = _newPartConfig;
        Debug.Log(ToString() + " ++ " + _newPartConfig.partType + "  (" + parts.Count + "/" +
                  vehicleDesign.requiredParts.Count + ")");
    }

    public string ToString()
    {
        return vehicleDesign.designName + "__" + id + " --> ";
    }
}

#endregion ------------------------ VEHICLE PARTS >