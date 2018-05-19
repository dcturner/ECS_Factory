﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Drawers;
using UnityEngine;

public enum FactoryMode
{
    OOP,
    DOD
}

public class Factory : SerializedMonoBehaviour
{
    public static Factory INSTANCE;
    public FactoryMode factoryMode;
    public Storage PartsDeliveredTo;
    private int tick = 0;
    [PropertyRange(0.0000001f, 1f)] public float tickRate = 0.1f;
    private float t;

    public Storage HD, RAM, L3;
    private Storage[] storage;
    private List<Workshop> workshops;
    private List<WorkshopTask> workshopTasks;

    private int storageCount;
    public static int SHARED_STORAGE_CAPACITY;
    public static int SHARED_STORAGE_CORE_SHARE;

    // ORDER
    // Quantity of each design to be built
    public Dictionary<VehicleDesign, int> vehicleOrder;
    public Dictionary<VehicleDesign, int> CompletedVehicles;
    private Dictionary<VehiclePart_Config, int> requiredParts;
    public bool orderComplete;

    private void Awake()
    {
        INSTANCE = this;
        t = tickRate;
        storage = FindObjectsOfType<Storage>();
        workshops = FindObjectsOfType<Workshop>().OrderBy(m => m.workshopIndex).ToList();
        storageCount = storage.Length;
        requiredParts = new Dictionary<VehiclePart_Config, int>();
        CompletedVehicles = new Dictionary<VehicleDesign, int>();
        orderComplete = false;
    }

    private void Start()
    {
        // set up SHARED STORAGE statics (L3)
        SHARED_STORAGE_CAPACITY = L3.capacity;
        SHARED_STORAGE_CORE_SHARE = SHARED_STORAGE_CAPACITY / workshops.Count;
        Debug.Log("WORKSHOPS GET [" + SHARED_STORAGE_CORE_SHARE + "] of [" + SHARED_STORAGE_CAPACITY + "]");
        Get_RequiredParts();
        ScheduleTasks();
    }

    private void Get_RequiredParts()
    {
        // get total number of parts required
        foreach (KeyValuePair<VehicleDesign, int> _DESIGN_PAIR in vehicleOrder)
        {
            VehicleDesign _DESIGN = _DESIGN_PAIR.Key;
            int _TOTAL = _DESIGN_PAIR.Value;

            // Set number of completed vehicles to zero
            CompletedVehicles[_DESIGN] = 0;

            Debug.Log(_DESIGN.designName + " -- " + _TOTAL);
            foreach (KeyValuePair<VehiclePart_Config, int> _PartCount in _DESIGN.quantities)
            {
                VehiclePart_Config _partType = _PartCount.Key;
                if (requiredParts.ContainsKey(_partType))
                {
                    requiredParts[_partType] += _DESIGN.quantities[_partType] * _TOTAL;
                }
                else
                {
                    requiredParts.Add(_partType, _DESIGN.quantities[_partType] * _TOTAL);
                }
            }
        }

        // summarise order and sling it into RAM
        Debug.Log("Part totals...");
        foreach (KeyValuePair<VehiclePart_Config, int> _PAIR in requiredParts)
        {
            VehiclePart_Config _PART = _PAIR.Key;
            int _TOTAL = _PAIR.Value;
            GameObject _PART_PREFAB = _PART.prefab_part;
            Debug.Log(_PART.name + " x " + _TOTAL);
            List<VehiclePart_CHASSIS> _LIST_CHASSIS = new List<VehiclePart_CHASSIS>();
            List<VehiclePart> _LIST_PARTS = new List<VehiclePart>();
            for (int i = 0; i < _TOTAL; i++)
            {
                GameObject _part_OBJ =
                    (GameObject) Instantiate(_PART_PREFAB, Vector3.zero, Quaternion.identity);
                if (_PART.partType == Vehicle_PartType.CHASSIS)
                {
                    _LIST_CHASSIS.Add(_part_OBJ.GetComponent<VehiclePart_CHASSIS>());
                }
                else
                {
                _LIST_PARTS.Add(_part_OBJ.GetComponent<VehiclePart>());
                }
            }
            
            // parts are instantiated - now lets force_quickSave them into "PartsDeliveredTo" (usually RAM)
            PartsDeliveredTo.Force_QuickSave(_LIST_CHASSIS.ToArray());
            PartsDeliveredTo.Force_QuickSave(_LIST_PARTS.ToArray());
        }
    }

    private void ScheduleTasks()
    {
        
        // STEP ONE - order all the required parts
        VehicleDesign _DESIGN = vehicleOrder.Keys.First();
        List<VehiclePart_Assignment> _PARTS = new List<VehiclePart_Assignment>();
        Debug.Log(_DESIGN.designName);
        foreach (VehiclePart_Assignment _REQUIRED_PART in _DESIGN.requiredParts)
        {
            _PARTS.Add(_REQUIRED_PART);
        }
        workshopTasks = new List<WorkshopTask>();

        // STEP TWO - Depending on the approach (OOP / DOD), setup workshop tasks
        switch (factoryMode)
        {
            case FactoryMode.OOP:
                // for each design - make a single workshop task to tackle it
                foreach (VehicleDesign _VEHICLE_DESIGN in vehicleOrder.Keys)
                {
                    workshopTasks.Add(new WorkshopTask(_VEHICLE_DESIGN, _VEHICLE_DESIGN.quantities));
                }
                break;
            case FactoryMode.DOD:
                break;
        }

        workshops[0].currentTask = workshopTasks[0];
    }

    private void Update()
    {
        if (Timer.TimerReachedZero(ref t))
        {
            Tick();
        }
    }

    private void Tick()
    {
        if (!orderComplete)
        {
            t = tickRate;
            tick++;

            for (int i = 0; i < storageCount; i++)
            {
                storage[i].Tick();
            }

            for (int i = 0; i < workshops.Count; i++)
            {
                workshops[i].Tick();
            }
        }
    }

    public void VehicleComplete(VehiclePart_CHASSIS _chassis)
    {
        vehicleOrder[_chassis.design]--;
        if (vehicleOrder[_chassis.design] == 0)
        {
            bool ordersStillPending = false;
            foreach (int _REMAINING in vehicleOrder.Values)
            {
                if (_REMAINING > 0)
                {
                    ordersStillPending = true;
                    break;
                }
            }

            if (!ordersStillPending)
            {
                orderComplete = true;
                Debug.Log("ORDER COMPLETE");
            }
        }
    }

    #region ------------------------- < MOVE PARTS BETWEEN STORAGE

    public void OrderParts(Storage _target)
    {
    }

    #endregion ------------------------ MOVE PARTS BETWEEN STORAGE >


    #region ------------------------- < GIZMOS METHODS

    [Button("Toggle Storage Cell Gizmos")]
    public void GIZMOS_storage_showCells()
    {
        Storage.GIZMOS_DRAW_CELLS = !Storage.GIZMOS_DRAW_CELLS;
    }

    #endregion ------------------------ GIZMOS METHODS >
}