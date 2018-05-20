using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

public class Storage : MonoBehaviour
{
    public static bool GIZMOS_DRAW_CELLS = false;

    public string storageName;
    public Storage getsPartsFrom;
    private Storage sendingPartsTo;
    public Color colour;
    [PropertyRange(1, 1000)] public int taskDuration;
    // Total number of clusters along XYZ
    [TabGroup("Total")] [PropertyRange(1, 100)]
    public int linesX, linesY, linesZ;

    // How many cells make up a cluster?  XYZ
    [TabGroup("GroupBy")] [PropertyRange(1, 100)]
    public int lineLength, lines_groupBy_Y, lines_groupBy_Z;

    // How much space goes between clusters? XYZ
    [TabGroup("Gutters")] [PropertyRange(0, 10)]
    public int gutterX, gutterY, gutterZ;

    private float width, height, depth;
    [ReadOnly] public int usedSpace, freeSpace, capacity, clusterCapacity, taskStep;
    [HideInInspector] public StorageState currentState;

    [HideInInspector] public List<Vector3> storageLocations;
    [HideInInspector] public List<Vector3> aisleSpaces;
    [HideInInspector] public float factor;
    [HideInInspector] public List<VehiclePart> contents;
    [HideInInspector] public List<StorageLine> storageLines;
    [HideInEditorMode] public VehiclePart[] pending_STORE, pending_SEND;

    // Storage GRID layout
    private int cellsX, cellsY, cellsZ;

    private void Awake()
    {
        DefineStorageLayout();
    }

    private string Log()
    {
        return storageName + " \n(" + usedSpace + "/" + capacity + ") ";
    }

    private void DefineStorageLayout()
    {
        storageLocations = new List<Vector3>();
        storageLines = new List<StorageLine>();
        aisleSpaces = new List<Vector3>();
        Vector3 _POS = transform.position;

        clusterCapacity = lineLength * lines_groupBy_Y * lines_groupBy_Z;
        capacity = clusterCapacity * (linesX * linesY * linesZ);
        freeSpace = capacity;
        usedSpace = 0;


        int lineIndex = 0;
        for (int stackY = 0; stackY < linesY; stackY++)
        {
            for (int stackZ = 0; stackZ < linesZ; stackZ++)
            {
                for (int lineX = 0; lineX < linesX; lineX++)
                {
                    AddStorageLine(_POS + new Vector3((lineX * lineLength) + (gutterX * lineX), stackY + (gutterY * stackY),
                        stackZ + (gutterZ * stackZ)) * Factory.INSTANCE.storageCellSize);
                    lineIndex++;
                }
            }
        }
    }

    void AddStorageLine(Vector3 _pos)
    {
        storageLines.Add(new StorageLine(lineLength, _pos));
        capacity += lineLength;
        freeSpace = capacity;
    }


    public void ChangeState(StorageState _newState)
    {
        if (currentState != _newState)
        {
            currentState = _newState;
//            Debug.Log(ToString() + currentState);
            switch (currentState)
            {
                case StorageState.IDLE:
                    if (pending_STORE != null)
                    {
                        AttemptStore(pending_STORE);
                    }

                    break;
                case StorageState.WAITING_FOR_DELIVERY:
                    break;
                case StorageState.FETCHING_REQUESTED_ITEMS:
                    break;
            }
        }
    }

    public void Force_QuickSave(VehiclePart[] _parts)
    {
        AttemptStore(_parts);
    }

    private void AttemptStore(VehiclePart[] _parts)
    {
        
        int partIndex = 0;
        int lineIndex = 0;
        while (partIndex < _parts.Length-1 && lineIndex < storageLines.Count)
        {
            StorageLine _LINE = storageLines[lineIndex];
            for (int slotIndex = 0; slotIndex < lineLength; slotIndex++)
            {
                VehiclePart _PART = (_parts[partIndex].partConfig.partType == Vehicle_PartType.CHASSIS)
                    ? _parts[partIndex] as VehiclePart_CHASSIS
                    : _parts[partIndex];
                if (_LINE.slots[slotIndex] == null)
                {
                    _LINE.slots[slotIndex] = _PART;
                    SetPartTransform(_PART.transform, lineIndex, slotIndex);
                    freeSpace--;
                    usedSpace++;
                    partIndex++;
                    if (partIndex == _parts.Length)
                    {
                        return;
                    }
                }
            }

            lineIndex++;
        }
    }

    public VehiclePart[] FetchLine(int _index)
    {
        return storageLines[_index].slots;
    }

    private void SetPartTransform(Transform _partTransform, int _lineIndex, int _slotIndex)
    {
        _partTransform.position = storageLines[_lineIndex].slotPositions[_slotIndex];
    }

    public void RequestPart(VehiclePartRequest _request)
    {
        if (currentState == StorageState.IDLE)
        {
            sendingPartsTo = _request.deliverTo;
            VehiclePart[] _partsFound = contents.Where(p => p.partConfig == _request.part).ToArray();
            int _totalFound = _partsFound.Length;
            if (_totalFound > 0)
            {
                if (_totalFound > _request.maxParts)
                {
                    Array.Resize(ref _partsFound, _request.maxParts);
                }

                pending_SEND = _partsFound;

                ChangeState(StorageState.FETCHING_REQUESTED_ITEMS);
            }
        }
    }

    // Check within CONTENTS for chassis that meet the required criteria (PART TYPE MISSING)
    // e.g. find chassis that still need wheels
    public void RequestChassis(VehicleChassiRequest _request)
    {
        if (currentState == StorageState.IDLE)
        {
            sendingPartsTo = _request.deliverTo;

            VehiclePart_CHASSIS[] _chassisFound =
                FindChassis(_request.chassisVersion, _request.requiredParts).ToArray();
            int _totalFound = _chassisFound.Length;
            if (_totalFound > 0)
            {
                if (_totalFound > _request.maxParts)
                {
                    Array.Resize(ref _chassisFound, _request.maxParts);
                }

                pending_SEND = _chassisFound;
                ChangeState(StorageState.FETCHING_REQUESTED_ITEMS);
            }
        }
    }

    public List<VehiclePart_CHASSIS> FindChassis(int _chassisVersion,
        Dictionary<VehiclePart_Config, int> _requiredParts)
    {
        List<VehiclePart_CHASSIS> _CHASSIS_LIST = new List<VehiclePart_CHASSIS>();
        foreach (VehiclePart _PART in contents)
        {
            VehiclePart_Config _PART_CONFIG = _PART.partConfig;
            if (_PART_CONFIG.partType == Vehicle_PartType.CHASSIS && _PART_CONFIG.partVersion == _chassisVersion)
            {
                VehiclePart_CHASSIS _CHASSIS = _PART as VehiclePart_CHASSIS;
                if (_CHASSIS.partsNeeded.Count > 0)
                {
                    var _PARTS_FITTED = _CHASSIS.partsFitted;
                    int criteriaMet = 0;

                    // for each part missing, criteriaMet ++
                    foreach (KeyValuePair<VehiclePart_Config, int> _PAIR in _requiredParts)
                    {
                        VehiclePart_Config _REQ_PART = _PAIR.Key;
                        int _QUANTITY = _PAIR.Value;
                        if (_PARTS_FITTED.ContainsKey(_REQ_PART))
                        {
                            criteriaMet += _QUANTITY - _PARTS_FITTED[_REQ_PART];
                        }
                        else
                        {
                            criteriaMet += _QUANTITY;
                        }
                    }


                    if (criteriaMet > 0)
                    {
                        _CHASSIS.tempCriteriaMet = criteriaMet;
                        _CHASSIS_LIST.Add(_CHASSIS);
                    }
                }
            }
        }

        List<VehiclePart_CHASSIS> _CHASSIS_SORTED = _CHASSIS_LIST.OrderBy(c => c.tempCriteriaMet).ToList();
        return _CHASSIS_SORTED;
    }

    public void Tick()
    {
        switch (currentState)
        {
            case StorageState.IDLE:
                break;
            case StorageState.WAITING_FOR_DELIVERY:
                break;
            case StorageState.FETCHING_REQUESTED_ITEMS:
                if (taskStep == taskDuration)
                {
                    READY_TO_DISPATCH();
                }
                else
                {
                    taskStep++;
                }

                break;
        }

        factor = (float) taskStep / (float) taskDuration;
    }

    private void READY_TO_DISPATCH()
    {
        taskStep = 0;
        foreach (VehiclePart _PART in pending_SEND)
        {
            contents.Remove(_PART);
            freeSpace++;
            usedSpace--;
        }

//        RefactorStorage();
        sendingPartsTo.Recieve_Parts(pending_SEND);
        pending_SEND = null;
        ChangeState(StorageState.IDLE);
    }

    public void Recieve_Parts(VehiclePart[] _parts)
    {
//        RefactorStorage();
        if (currentState == StorageState.IDLE || currentState == StorageState.WAITING_FOR_DELIVERY)
        {
            // store
//            Debug.Log(storageName + " Recieved " + _parts.Length + " x " + _parts[0].partConfig.name);
            AttemptStore(_parts);
            ChangeState(StorageState.IDLE);
        }
        else
        {
        }
    }

//    public void RefactorStorage()
//    {
//        contents = contents.Where(vp => vp != null).Distinct().ToList();
//        usedSpace = contents.Count;
//        freeSpace = capacity - usedSpace;
//        for (int i = 0; i < usedSpace; i++)
//        {
//            SetPartTransform(contents[i].transform, i);
//        }
//    }


    private void OnDrawGizmos()
    {
        Vector3 _POS = transform.position;
        if (Factory.INSTANCE == null)
        {
            Factory.INSTANCE = FindObjectOfType<Factory>();
        }

        float cellSize = Factory.INSTANCE.storageCellSize;
        Gizmos.color = colour;
        width = ((linesX * lineLength) + ((linesX - 1) * gutterX)) * cellSize;
        height = ((linesY * lines_groupBy_Y) + ((linesY - 1) * gutterY)) * cellSize;
        depth = ((linesZ * lines_groupBy_Z) + ((linesZ - 1) * gutterZ)) * cellSize;
        Vector3 _SIZE = new Vector3(width, 0f, depth);
        GizmoHelpers.DrawRect(colour, _POS, width, depth, Log());

        Vector3 _START = _POS + new Vector3(0f, 0f, (depth) * factor);
        GizmoHelpers.DrawLine(Color.white, _START, _START + new Vector3(width, 0f, 0f));

        // flash on action?
        if (taskStep == 0)
        {
        }

        if (GIZMOS_DRAW_CELLS)
        {
            Vector3 cell_drawSize = Vector3.one
            * cellSize;
            if (storageLocations.Count == 0)
            {
                for (int stackY = 0; stackY < linesY; stackY++)
                {
                    for (int stackZ = 0; stackZ < linesZ; stackZ++)
                    {
                        for (int lineX = 0; lineX < linesX; lineX++)
                        {
                            for (int slotIndex = 0; slotIndex < lineLength; slotIndex++)
                            {
                                Vector3 _LINE_POS = _POS + new Vector3(lineLength * lineX + (lineX * gutterX),
                                                        stackY + (stackY * gutterY), stackZ + (stackZ * gutterZ)) * cellSize;
                                Gizmos_DrawCell(_LINE_POS + new Vector3(slotIndex*cellSize, 0f, 0f), cell_drawSize);
                            }
                        }
                    }
                }
            }

            else
            {
                Gizmos.color = Color.grey;
                foreach (Vector3 _STORAGE_LOCATION in storageLocations)
                {
                    Gizmos_DrawCell(_STORAGE_LOCATION, cell_drawSize);
                }
            }

            clusterCapacity = lineLength * lines_groupBy_Y * lines_groupBy_Z;
            capacity = clusterCapacity * (linesX * linesY * linesZ);
        }
    }

    void Gizmos_DrawCell(Vector3 _cell_POS, Vector3 _cell_SIZE)
    {
//        Gizmos.DrawCube(_cell_POS + _cell_SIZE * 0.5f, _cell_SIZE);
//        Gizmos.DrawWireCube(_cell_POS, _cell_SIZE );
        GizmoHelpers.DrawRect(colour, _cell_POS, _cell_SIZE.x, _cell_SIZE.y);
    }
}

public enum StorageState
{
    IDLE,
    WAITING_FOR_DELIVERY,
    FETCHING_REQUESTED_ITEMS,
}

public class VehiclePartRequest
{
    public VehiclePart_Config part;
    public Storage deliverTo;
    public int maxParts;

    public VehiclePartRequest(VehiclePart_Config _part, Storage _deliverTo, int _maxParts)
    {
        part = _part;
        deliverTo = _deliverTo;
        maxParts = _maxParts;
    }
}

public class VehicleChassiRequest : VehiclePartRequest
{
    public int chassisVersion;
    public Dictionary<VehiclePart_Config, int> requiredParts;

    public VehicleChassiRequest(VehiclePart_Config _part, int _chassisVersion,
        Dictionary<VehiclePart_Config, int> _requiredParts, Storage _deliverTo, int _maxParts)
        : base(_part, _deliverTo, _maxParts)
    {
        part = _part;
        chassisVersion = _chassisVersion;
        requiredParts = _requiredParts;
        deliverTo = _deliverTo;
        maxParts = _maxParts;
    }
}

public struct StorageLine
{
    public VehiclePart[] slots;
    public Vector3[] slotPositions;
    public int lineLength;
    public bool empty, full;

    public StorageLine(int _lineLength, Vector3 _pos)
    {
        lineLength = _lineLength;
        slots = new VehiclePart[lineLength];
        slotPositions = new Vector3[lineLength];
        empty = true;
        full = false;
        for (int i = 0; i < lineLength; i++)
        {
            slots[i] = null;
            slotPositions[i] = _pos + new Vector3(i*Factory.INSTANCE.storageCellSize, 0f, 0f);
        }
    }
}