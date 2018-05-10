using System;
using System.Collections.Generic;
using UnityEngine;



#region ------------------------- < STORAGE

public enum StorageState
{
    IDLE,
    STORE,
    FETCH
}

public struct StorageTask
{
    public StorageState state;
}

public struct Storage
{
    public string name;
    public int capacity, usedSpace, freeSpace;
    public StorageState currentState;
    public int taskDuration;
    public int[] contents;

    public Storage(string _name, int _capacity, int _taskDuration)
    {
        name = _name;
        capacity = _capacity;
        usedSpace = 0;
        freeSpace = capacity;
        currentState = StorageState.IDLE;
        taskDuration = _taskDuration;
        contents = new int[capacity];
    }

    private string ToString()
    {
        return name + "(" + usedSpace + "/" + freeSpace + ") --> ";
    }

    private void ChangeState(StorageState _newState)
    {
        if (currentState != _newState)
        {
            currentState = _newState;
            Debug.Log(ToString() + currentState);

            switch (currentState)
            {
                case StorageState.IDLE:
                    break;
                case StorageState.STORE:
                    break;
                case StorageState.FETCH:
                    break;
            }
        }
    }


    public void Update()
    {
        switch (currentState)
        {
            case StorageState.IDLE:
                break;
            case StorageState.STORE:
                break;
            case StorageState.FETCH:
                break;
        }
    }
}

#endregion ------------------------ STORAGE >
#region ------------------------- < VEHICLE PARTS

// A group of these make up a VehicleDesign - (4 wheels, one engine, 2 seats <-- 7 components)
public struct VehicleDesign_Component
{
    public VehiclePart part;
    public Vector3 position;
    public Quaternion rotation;

    public VehicleDesign_Component(VehiclePart _part, Vector3 _position, Quaternion _rotation)
    {
        part = _part;
        position = _position;
        rotation = _rotation;
    }
}

// Recipe for a vehicle type
public struct VehicleDesign
{
    public string designName;
    public Color chassisColour;
    public VehicleDesign_Component[] designComponents;

    public VehicleDesign(string _designName, Color _chassisColour, VehicleDesign_Component[] _designComponents)
    {
        designName = _designName;
        chassisColour = _chassisColour;
        designComponents = _designComponents;
    }
}

// All vehicle parts are attached ONTO the chassis
public struct VehicleChassis
{
    private int chassisType;
    private int slotsOccupied;
    private Color chassisColour;
    private List<VehiclePart> connectedParts;

    public VehicleChassis(int _chassisType, int _slotsOccupied, Color _chassisColour)
    {
        chassisType = _chassisType;
        slotsOccupied = _slotsOccupied;
        chassisColour = _chassisColour;
        connectedParts = new List<VehiclePart>();
    }
}

// Configure an individual part - example { name: "wheel", slotsOccupied: 1, connectedTo (chassis)}
public struct VehiclePart
{
    public int partType;
    public int size;
    private List<VehiclePart> connectedTo;

    public VehiclePart(int _partType, int _size)
    {
        partType = _partType;
        size = _size;
        connectedTo = new List<VehiclePart>();
    }
}

// Governs the state of an individual vehicle as it makes its way through the factory
// Stores DESIGN for this vehicle (recipe of required parts in what arrangement),
// chassis - onto which all components are attached
// age - how long has it taken to build this vehicle?
public struct Vehicle
{
    public VehicleDesign vehicleDesign;
    public VehiclePart[] parts;
    public int age;

    public Vehicle(VehicleDesign _vehicleDesign)
    {
        vehicleDesign = _vehicleDesign;
        chassis = _chassis;
        age = 0;
    }
}

#endregion ------------------------ VEHICLE PARTS >

