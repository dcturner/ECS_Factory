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

    #region < FIELDS
    public static bool GIZMOS_DRAW_CELLS = false;

    public string storageName;
    public Storage getsPartsFrom;
    public Storage sendingLineTo;
    public Color colour;

    [PropertyRange(1, 1000)] public int taskDuration;

    // Total number of clusters along XYZ
    [TabGroup("Total")]
    [PropertyRange(1, 100)]
    public int linesX, linesY, linesZ;

    // How many cells make up a cluster?  XYZ
    [TabGroup("GroupBy")]
    [PropertyRange(1, 100)]
    public int lineLength, lines_groupBy_Y, lines_groupBy_Z;

    // How much space goes between clusters? XYZ
    [TabGroup("Gutters")]
    [PropertyRange(0, 10)]
    public int gutterX, gutterY, gutterZ;

    private float width, height, depth;
    [ReadOnly] public int usedSpace, freeSpace, capacity, clusterCapacity, taskStep;
    [HideInInspector] public StorageState currentState;

    [HideInInspector] public List<Vector3> storageLocations;
    [HideInInspector] public List<Vector3> aisleSpaces;
    [HideInInspector] public float factor;
    [HideInInspector] public List<StorageLine> storageLines;
    [HideInInspector] public VehiclePart[] parts_IN, parts_OUT;
    [HideInInspector] public StorageLine pendingLineSend;
    [HideInInspector] public VehiclePartRequest pending_PART_request;
    [HideInInspector] public VehicleChassiRequest pending_CHASSIS_request;

    // Storage GRID layout
    private int cellsX, cellsY, cellsZ;
    #endregion FIELDS >
    #region < INIT
    public void Init()
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
                    AddStorageLine(lineIndex, _POS + new Vector3((lineX * lineLength) + (gutterX * lineX),
                                                  stackY + (gutterY * stackY),
                                                  stackZ + (gutterZ * stackZ)) * Factory.INSTANCE.storageCellSize);
                    lineIndex++;
                }
            }
        }
    }

    void AddStorageLine(int _index, Vector3 _pos)
    {
        storageLines.Add(new StorageLine(_index, lineLength, _pos));
    }
    #endregion INIT >
    #region < State / Update
    public void ChangeState(StorageState _newState)
    {
        if (currentState != _newState)
        {
            currentState = _newState;
            //            Debug.Log(ToString() + currentState);
            switch (currentState)
            {
                case StorageState.IDLE:
                    if (parts_IN != null)
                    {
                        AttemptStore(parts_IN);
                    }

                    break;
                case StorageState.WAITING:
                    break;
                case StorageState.FETCHING:
                    break;
            }
        }
    }
    public void Tick()
    {
        switch (currentState)
        {
            case StorageState.IDLE:
                break;
            case StorageState.WAITING:
                break;
            case StorageState.FETCHING:
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

        factor = (float)taskStep / (float)taskDuration;
    }
    #endregion State / Update >
    #region < Send / Recieve
    // Check within CONTENTS for chassis that meet the required criteria (PART TYPE MISSING)
    // e.g. find chassis that still need wheels
    public void RequestChassis(VehicleChassiRequest _request)
    {
        if (currentState == StorageState.IDLE)
        {
            sendingLineTo = _request.deliverTo;
            int lineContainingChassis =
                FindLineContainingChassis(_request.chassisVersion, _request.requiredParts);
            if (lineContainingChassis != -1)
            {
                parts_OUT = storageLines[lineContainingChassis].slots.ToArray();
                ChangeState(StorageState.FETCHING);
            }
            else
            {
                getsPartsFrom.RequestChassis(new VehicleChassiRequest(_request.part, _request.chassisVersion,
                    _request.requiredParts, this));
            }
        }
    }

    public int FindLineContainingChassis(int _chassisVersion,
        Dictionary<VehiclePart_Config, int> _requiredParts)
    {
        // Iterate through LINES
        for (int lineIndex = 0; lineIndex < storageLines.Count; lineIndex++)
        {
            StorageLine _LINE = storageLines[lineIndex];

            // iterate through SLOTS
            for (int slotIndex = 0; slotIndex < lineLength; slotIndex++)
            {
                if (_LINE.slots[slotIndex] != null)
                {
                    VehiclePart _SLOT_PART = _LINE.slots[slotIndex];

                    VehiclePart_Config _PART_CONFIG = _SLOT_PART.partConfig;

                    // Proceed only if the current SLOT PART is the right type of chassis
                    if (_PART_CONFIG.partType == Vehicle_PartType.CHASSIS &&
                        _PART_CONFIG.partVersion == _chassisVersion)
                    {
                        VehiclePart_CHASSIS _CHASSIS = _SLOT_PART as VehiclePart_CHASSIS;

                        // Proceed only if the chassis still needs parts O_o
                        if (_CHASSIS.partsNeeded.Count > 0)
                        {
                            var _PARTS_FITTED = _CHASSIS.partsFitted;

                            // If chassis has less a defecit of our required parts, grab it
                            foreach (KeyValuePair<VehiclePart_Config, int> _PAIR in _requiredParts)
                            {
                                VehiclePart_Config _REQ_PART = _PAIR.Key;
                                int _QUANTITY = _PAIR.Value;
                                if (_CHASSIS.partsFitted.ContainsKey(_REQ_PART))
                                {
                                    if (_CHASSIS.partsFitted[_REQ_PART] < _QUANTITY)
                                    {
                                        Debug.Log(storageName + "chassis found on line " + lineIndex);
                                        return lineIndex;
                                    }
                                }
                                else
                                {
                                    return lineIndex;
                                }
                            }
                        }
                    }
                }
            }
        }

        return -1;
    }

    public List<VehiclePart_CHASSIS> GetViableChassis(int _chassisVersion,
        Dictionary<VehiclePart_Config, int> _requiredParts)
    {
        List<VehiclePart_CHASSIS> _result = new List<VehiclePart_CHASSIS>();
        // Iterate through LINES
        for (int lineIndex = 0; lineIndex < storageLines.Count; lineIndex++)
        {
            StorageLine _LINE = storageLines[lineIndex];

            // iterate through SLOTS
            for (int slotIndex = 0; slotIndex < lineLength; slotIndex++)
            {
                if (_LINE.slots[slotIndex] != null)
                {
                    VehiclePart _SLOT_PART = _LINE.slots[slotIndex];

                    VehiclePart_Config _PART_CONFIG = _SLOT_PART.partConfig;

                    // Proceed only if the current SLOT PART is the right type of chassis
                    if (_PART_CONFIG.partType == Vehicle_PartType.CHASSIS &&
                        _PART_CONFIG.partVersion == _chassisVersion)
                    {
                        VehiclePart_CHASSIS _CHASSIS = _SLOT_PART as VehiclePart_CHASSIS;
                        // Proceed only if the chassis still needs parts O_o
                        if (_CHASSIS.partsNeeded.Count > 0)
                        {
                            var _PARTS_FITTED = _CHASSIS.partsFitted;
                            int criteriaMet = 0;

                            // If chassis has less a defecit of our required parts, grab it
                            foreach (KeyValuePair<VehiclePart_Config, int> _PAIR in _requiredParts)
                            {
                                VehiclePart_Config _REQ_PART = _PAIR.Key;
                                int _QUANTITY = _PAIR.Value;
                                if (_CHASSIS.partsFitted.ContainsKey(_REQ_PART))
                                {
                                    if (_CHASSIS.partsFitted[_REQ_PART] < _QUANTITY)
                                    {
                                        _result.Add(_CHASSIS);
                                    }
                                }
                                else
                                {
                                    _result.Add(_CHASSIS);
                                }
                            }
                        }
                    }
                }
            }
        }

        return _result;
    }
    public void Force_QuickSave(VehiclePart[] _parts)
    {
        AttemptStore(_parts);
    }

    private void READY_TO_DISPATCH()
    {
        for (int slotIndex = 0; slotIndex < lineLength; slotIndex++)
        {
            ClearSlot(pendingLineSend.index, slotIndex);
        }

        storageLines.Remove(pendingLineSend);
        sendingLineTo.RecieveParts(parts_OUT);
        ChangeState(StorageState.IDLE);
    }

    public void RecieveParts(VehiclePart[] _parts)
    {
        //Debug.Log(storageName +  " receieved " + _parts.Length);
        if (currentState == StorageState.IDLE || currentState == StorageState.WAITING)
        {
            if (freeSpace > 0)
            {
                // we have some room, try to store
                AttemptStore(_parts);
                ChangeState(StorageState.IDLE);
            }
            else
            {
                // no room available - ditch line zero
                List<VehiclePart> _PARTS_TO_SEND = new List<VehiclePart>();
                for (int i = 0; i < lineLength; i++)
                {
                    _PARTS_TO_SEND.Add(storageLines[0].slots[i]);
                    ClearSlot(0, i);
                }

                parts_OUT = _PARTS_TO_SEND.ToArray();
                sendingLineTo = getsPartsFrom;
                ChangeState(StorageState.FETCHING);
            }
        }
        else
        {
            parts_IN = _parts;
        }
    }
    #endregion Send / Recieve >
    #region < Slot Management
    private void AttemptStore(VehiclePart[] _parts)
    {
        int partIndex = 0;
        int lineIndex = 0;
        while (partIndex < _parts.Length - 1 && lineIndex < storageLines.Count)
        {
            StorageLine _LINE = storageLines[lineIndex];
            for (int slotIndex = 0; slotIndex < lineLength; slotIndex++)
            {
                if (_LINE.slots[slotIndex] == null)
                {
                    if (_parts[slotIndex] != null)
                    {
                        VehiclePart _PART = (_parts[partIndex].partConfig.partType == Vehicle_PartType.CHASSIS)
                            ? _parts[partIndex] as VehiclePart_CHASSIS
                            : _parts[partIndex];
                        SetPartTransform(_PART.transform, lineIndex, slotIndex);
                        FillSlot(lineIndex, slotIndex, _PART);
                        partIndex++;
                        if (partIndex == _parts.Length)
                        {
                            return;
                        }
                    }
                }
            }

            lineIndex++;
        }
    }
    public void ClearSlot(int _lineIndex, int _slotIndex)
    {
        if (storageLines[_lineIndex].slots[_slotIndex] != null)
        {
            storageLines[_lineIndex].slots[_slotIndex] = null;
            freeSpace++;
            usedSpace--;
        }
    }

    private void FillSlot(int _lineIndex, int _slotIndex, VehiclePart _part)
    {
        if (storageLines[_lineIndex].slots[_slotIndex] == null)
        {
            storageLines[_lineIndex].slots[_slotIndex] = _part;
            freeSpace--;
            usedSpace++;
        }
    }
    #endregion Slot Management >

    private void SetPartTransform(Transform _partTransform, int _lineIndex, int _slotIndex)
    {
        _partTransform.position = storageLines[_lineIndex].slotPositions[_slotIndex];
    }

    public void RequestPart(VehiclePartRequest _request)
    {
        if (currentState == StorageState.IDLE)
        { // I am free to take orders

            if (freeSpace >= lineLength)
            { // do I have space for a new line?
                BeginRequest(_request);
            }
            else
            { // no room, DUMP a line
                pending_PART_request = _request;
                Dump_LINE();
            }
        }
    }

    // Send the first line you encounter with a matching part
    private void BeginRequest(VehiclePartRequest _request)
    {
        pending_PART_request = null;
        sendingLineTo = _request.deliverTo;
        for (int _lineIndex = 0; _lineIndex < storageLines.Count; _lineIndex++)
        {
            StorageLine _LINE = storageLines[_lineIndex];
            for (int _slotIndex = 0; _slotIndex < lineLength; _slotIndex++)
            {
                VehiclePart[] _SLOTS = _LINE.slots.ToArray();
                if (_SLOTS[_slotIndex] != null)
                {
                    if (_SLOTS[_slotIndex].partConfig == _request.part)
                    {
                        parts_OUT = _LINE.slots.ToArray();
                        pendingLineSend = _LINE;
                        ChangeState(StorageState.FETCHING);
                        return;
                    }
                }
            }
        }

        // IF YOU REACH THIS POINT - YOU DONT HAVE THE PARTS, request from the next storage in chain :)
        ChangeState(StorageState.WAITING);
        getsPartsFrom.RequestPart(new VehiclePartRequest(_request.part, this));
    }

    public void Dump_LINE(int _lineIndex = 0)
    {
        Dump_SLOTS(lineLength, _lineIndex);
    }
    public void Dump_SLOTS(int _count, int _lineIndex = 0)
    {

        if (currentState == StorageState.IDLE)
        {
            ChangeState(StorageState.FETCHING);
            List<VehiclePart> dumpList = new List<VehiclePart>();
            StorageLine _LINE = storageLines[_lineIndex];
            for (int _slotIndex = 0; _slotIndex < _count; _slotIndex++)
            {
                if (_LINE.slots[_slotIndex] != null)
                {
                    dumpList.Add(_LINE.slots[_slotIndex]);
                    ClearSlot(_lineIndex, _slotIndex);
                }
            }
        }
    }



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
                                                        stackY + (stackY * gutterY),
                                                        stackZ + (stackZ * gutterZ)) * cellSize;
                                Gizmos_DrawCell(_LINE_POS + new Vector3(slotIndex * cellSize, 0f, 0f),
                                    cell_drawSize);
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
    WAITING,
    FETCHING
}

public class VehiclePartRequest
{
    public VehiclePart_Config part;
    public Storage deliverTo;

    public VehiclePartRequest(VehiclePart_Config _part, Storage _deliverTo)
    {
        part = _part;
        deliverTo = _deliverTo;
    }
}

public class VehicleChassiRequest : VehiclePartRequest
{
    public int chassisVersion;
    public Dictionary<VehiclePart_Config, int> requiredParts;

    public VehicleChassiRequest(VehiclePart_Config _part, int _chassisVersion,
        Dictionary<VehiclePart_Config, int> _requiredParts, Storage _deliverTo)
        : base(_part, _deliverTo)
    {
        part = _part;
        chassisVersion = _chassisVersion;
        requiredParts = _requiredParts;
        deliverTo = _deliverTo;
    }
}

public struct StorageLine
{
    public int index;
    public List<VehiclePart> slots;
    public Vector3[] slotPositions;
    public int lineLength;
    public bool empty, full;

    public StorageLine(int _index, int _lineLength, Vector3 _pos)
    {
        index = _index;
        lineLength = _lineLength;
        slots = new List<VehiclePart>(lineLength);
        slotPositions = new Vector3[lineLength];
        empty = true;
        full = false;
        for (int i = 0; i < lineLength; i++)
        {
            slots.Add(null);
            slotPositions[i] = _pos + new Vector3(i * Factory.INSTANCE.storageCellSize, 0f, 0f);
        }
    }
}