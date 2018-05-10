using System;
using System.Collections.Generic;
using UnityEngine;

#region ------------------------- < VEHICLE PARTS

public static class VEHICLE_DESIGNS
{   
    public static List<VehicleDesign> DESIGNS = new List<VehicleDesign>();

    public static void ADD_SALOON()
    {
        string _designName = "SALOON";
        Color _designColour = Color.blue;
        List<VehicleDesign_RequiredPart> _designParts = new List<VehicleDesign_RequiredPart>();
        // chassis
        _designParts.Add(new VehicleDesign_RequiredPart());
    }
}

// Recipe for a vehicle type
public struct VehicleDesign
{
    public string designName;
    public Color chassisColour;
    public List<VehicleDesign_RequiredPart> requiredParts;

    public VehicleDesign(string _designName, Color _chassisColour, List<VehicleDesign_RequiredPart> _requiredParts)
    {
        designName = _designName;
        chassisColour = _chassisColour;
        requiredParts = _requiredParts;
    }

    public string ToString()
    {
        string _STR = designName + " (): ";
        foreach (VehicleDesign_RequiredPart _PART in requiredParts)
        {
            _STR += _PART.part + " |";
        }

        return _STR;
    }
}

// A group of these make up a VehicleDesign - (4 wheels, one engine, 2 seats <-- 7 components)
public struct VehicleDesign_RequiredPart
{
    public VehiclePart part;
    public Vector3 position;
    public Quaternion rotation;

    public VehicleDesign_RequiredPart(VehiclePart _part, Vector3 _position, Quaternion _rotation)
    {
        part = _part;
        position = _position;
        rotation = _rotation;
    }
}

public enum Vehicle_PartType
{
CHASSIS,
    ENGINE,
    WHEEL,
    STEERING,
    DOOR,
    WINDSCREEN,
    EXHAUST
}

public enum Vehicle_ChassisType
{
    CAR_SALOON,
    CAR_COUPE,
    VAN,
    BIKE,
    BUS,
    TRUCK,
    PICKUP
}

// Configure an individual part - example { name: "wheel", slotsOccupied: 1, connectedTo (chassis)}
public struct VehiclePart
{
    public Vehicle_PartType partType;
    public int partVersion;
    public int size;
    public int age, id;

    public VehiclePart(Vehicle_PartType _partType, int _partVersion, int _size, int _id)
    {
        partType = _partType;
        partVersion = _partVersion;
        size = _size;
        age = 0;
        id = _id;
    }
}

// Governs the state of an individual vehicle as it makes its way through the factory
// Stores DESIGN for this vehicle (recipe of required parts in what arrangement),
// chassis - onto which all components are attached
// age - how long has it taken to build this vehicle?
public struct Vehicle
{
    public VehicleDesign vehicleDesign;
    public List<VehiclePart> parts;
    public int age, id;

    public Vehicle(VehicleDesign _vehicleDesign, int _id)
    {
        vehicleDesign = _vehicleDesign;
        parts = new List<VehiclePart>();
        age = 0;
        id = _id;
    }

    public void AttachPart(VehiclePart _newPart, VehicleDesign_RequiredPart _requiredPart, int _index)
    {
        parts[_index] = _newPart;
        Debug.Log(ToString() + " ++ " + _newPart.partType + "  ("+parts.Count+"/"+vehicleDesign.requiredParts.Count+")");
    }

    public string ToString()
    {
        return vehicleDesign.designName + "__" + id + " --> ";
    }
}

#endregion ------------------------ VEHICLE PARTS >

