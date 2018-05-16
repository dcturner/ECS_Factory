using System;
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
  
    private int storageCount;
    public static int SHARED_STORAGE_CAPACITY;
    public static int SHARED_STORAGE_CORE_SHARE;

    // ORDER
    // Quantity of each design to be built
    public Dictionary<VehicleDesign, int> vehicleOrder;
    public Dictionary<VehicleDesign, int> CompletedVehicles;
    private Dictionary<VehiclePart_Config, int> requiredParts;
    private Dictionary<VehiclePart_Config, List<VehiclePart>> parts;

    private void Awake()
    {
        INSTANCE = this;
        t = tickRate;
        storage = FindObjectsOfType<Storage>();
        workshops = FindObjectsOfType<Workshop>().OrderBy(m => m.workshopIndex).ToList();
        storageCount = storage.Length;
        requiredParts = new Dictionary<VehiclePart_Config, int>();
        parts = new Dictionary<VehiclePart_Config, List<VehiclePart>>();
        CompletedVehicles = new Dictionary<VehicleDesign, int>();
    }

    private void Start()
    {
        // set up SHARED STORAGE statics (L3)
        SHARED_STORAGE_CAPACITY = L3.capacity;
        SHARED_STORAGE_CORE_SHARE = SHARED_STORAGE_CAPACITY / workshops.Count;
        Debug.Log("WORKSHOPS GET ["+SHARED_STORAGE_CORE_SHARE+"] of ["+SHARED_STORAGE_CAPACITY+"]");
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
            parts.Add(_PART, new List<VehiclePart>());
            List<VehiclePart> _LIST = parts[_PART];
            Debug.Log(_PART.name + " x " + _TOTAL);
            for (int i = 0; i < _TOTAL; i++)
            {
                GameObject _part_OBJ =
                    (GameObject) Instantiate(_PART_PREFAB, Vector3.zero, Quaternion.identity);
                _LIST.Add(_part_OBJ.GetComponent<VehiclePart>());
        // parts are instantiated - now lets force_quickSave them into "PartsDeliveredTo" (usually RAM)
            }
        PartsDeliveredTo.Force_QuickSave(_LIST.ToArray());
        }
    
    }

    private void ScheduleTasks()
    {
        switch (factoryMode)
        {
            case FactoryMode.OOP:
                VehicleDesign _DESIGN = vehicleOrder.Keys.First();
                List<VehiclePart_Config> _PARTS = new List<VehiclePart_Config>();
                Debug.Log(_DESIGN.designName);
                foreach (VehicleDesign_RequiredPart _PART in _DESIGN.requiredParts)
                {
                    _PARTS.Add(_PART.partConfig);
                }
                    workshops[0].AddTask(_DESIGN, _PARTS.ToArray(), vehicleOrder[_DESIGN], true);
                workshops[0].L1.RequestPart(new VehiclePartRequest(_PARTS[0],workshops[0].REG, 8));
                break;
            case FactoryMode.DOD:
                break;
        }
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
        t = tickRate;
        tick++;

        for (int i = 0; i < storageCount; i++)
        {
            storage[i].Tick();
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