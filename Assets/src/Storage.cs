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
            taskStep = 0;
            currentState = _newState;
            switch (currentState)
            {
                case StorageState.IDLE:
                    if (parts_IN != null)
                    {
                        AttemptStore(parts_IN);
                    }

                    // grab chassis / part if a request is pending
                    if (pending_CHASSIS_request != null)
                    {
                        RequestChassis(pending_CHASSIS_request);
                    }
                    else if (pending_PART_request != null)
                    {
                        RequestPart(pending_PART_request);
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

    // REQUESTS

    public void RequestPart(VehiclePartRequest _request)
    {
        if (currentState == StorageState.IDLE)

        { // I am free to take orders
            if (freeSpace >= lineLength)
            { // do I have space for a new line?
                Attempt_PART_request(_request);
            }
            else
            { // no room, DUMP a line
                pending_PART_request = _request;
                pending_CHASSIS_request = null;
                Dump_LINE();
            }
        }
    }
    public void RequestChassis(VehicleChassiRequest _request)
    {

        if (currentState == StorageState.IDLE)
        { // I am free to take orders
            if (freeSpace >= lineLength)
            { // do I have space for a new line?
                Attempt_CHASSIS_request(_request);
            }
            else
            { // no room, DUMP a line
                pending_CHASSIS_request = _request;
                pending_PART_request = null;
                Dump_LINE();
            }
        }
    }

    // ATTEMPT TO EXECUTE REQUESTS
    private void Attempt_PART_request(VehiclePartRequest _request)
    {
        pending_PART_request = null;
        sendingLineTo = _request.deliverTo;
        for (int _lineIndex = 0; _lineIndex < storageLines.Count; _lineIndex++)
        {
            StorageLine _LINE = storageLines[_lineIndex];
            var _SLOTS = _LINE.slots;
            for (int _slotIndex = 0; _slotIndex < lineLength; _slotIndex++)
            {
                if (_SLOTS[_slotIndex] != null)
                {
                    if (_SLOTS[_slotIndex].partConfig == _request.part)
                    {
                        Set_parts_OUT(_lineIndex);
                        ChangeState(StorageState.FETCHING);
                        return;
                    }
                }
            }
        }

        // IF YOU REACH THIS POINT - YOU DONT HAVE THE PARTS, request from the next storage in chain :)
        ChangeState(StorageState.WAITING);
        pending_PART_request = _request;
        getsPartsFrom.RequestPart(new VehiclePartRequest(_request.part, this));
    }

    private void Attempt_CHASSIS_request(VehicleChassiRequest _request)
    {
        pending_CHASSIS_request = null;
        sendingLineTo = _request.deliverTo;

        // for StorageLines
        for (int _lineIndex = 0; _lineIndex < storageLines.Count; _lineIndex++)
        {
            for (int _slotIndex = 0; _slotIndex < lineLength; _slotIndex++)
            {
                if (IsChassisViable(_lineIndex, _slotIndex, _request.chassisVersion, _request.requiredParts))
                {
                    Set_parts_OUT(_lineIndex);
                    ChangeState(StorageState.FETCHING);
                    return;
                }
            }
        }

        // IF YOU REACH THIS POINT - YOU DONT HAVE THE PARTS, request from the next storage in chain :)
        ChangeState(StorageState.WAITING);
        pending_CHASSIS_request = _request;
        getsPartsFrom.RequestPart(new VehicleChassiRequest(_request.part, _request.chassisVersion, _request.requiredParts, this));
    }

    private void Set_parts_OUT(int  _lineIndex){
        parts_OUT = new VehiclePart[lineLength];
        var _LINE = storageLines[_lineIndex];
        for (int _slotIndex = 0; _slotIndex < lineLength; _slotIndex++)
        {
            parts_OUT[_slotIndex] = _LINE.slots[_slotIndex];
            ClearSlot(_lineIndex, _slotIndex);
        }
    }

    public bool IsChassisViable(int _lineIndex, int _slotIndex, int _chassisVersion, Dictionary<VehiclePart_Config, int> _requiredParts)
    {
        StorageLine _LINE = storageLines[_lineIndex];
        VehiclePart _SLOT = _LINE.slots[_slotIndex];
        VehiclePart_CHASSIS _CHASSIS = null;
        if (_SLOT != null)
        {
            if (_SLOT.partConfig.partType == Vehicle_PartType.CHASSIS)
            { // part IS a chassis

                if (_SLOT.partConfig.partVersion == _chassisVersion)
                { // is Correct chassis type

                    _CHASSIS = _SLOT as VehiclePart_CHASSIS;
                }
            }
        }

        if (_CHASSIS != null)
        {
            if (_CHASSIS.partsNeeded.Count > 0)
            {
                var _PARTS_FITTED = _CHASSIS.partsFitted;

                // If chassis has less a defecit of our required parts, grab it
                foreach (KeyValuePair<VehiclePart_Config, int> _PAIR in _requiredParts)
                {
                    VehiclePart_Config _REQ_PART = _PAIR.Key;
                    int _QUANTITY = _PAIR.Value;
                    if (_REQ_PART.partType != Vehicle_PartType.CHASSIS)
                    {

                        if (_CHASSIS.partsFitted.ContainsKey(_REQ_PART))
                        {
                            if (_CHASSIS.partsFitted[_REQ_PART] < _QUANTITY)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }



    public void RecieveParts(VehiclePart[] _parts)
    {
        Debug.Log(storageName + " receieved " + _parts.Length);
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
                Set_parts_OUT(0);

                sendingLineTo = getsPartsFrom;
                ChangeState(StorageState.FETCHING);
            }
        }
        else
        {
            parts_IN = _parts;
        }
    }

    private void READY_TO_DISPATCH()
    {
        if (parts_OUT.Length > 0)
        {
            Debug.Log(storageName + " sending to " + sendingLineTo.storageName);
            sendingLineTo.RecieveParts(parts_OUT);
        }
        ChangeState(StorageState.IDLE);
    }
    #endregion Send / Recieve >
    #region < Slot Management
    private void AttemptStore(VehiclePart[] _parts)
    {
        int _partIndex = 0;
        for (int _lineIndex = 0; _lineIndex < storageLines.Count; _lineIndex++)
        {
            StorageLine _LINE = storageLines[_lineIndex];

            for (int _slotIndex = 0; _slotIndex < lineLength; _slotIndex++)
            {
                if (_LINE.slots[_slotIndex] == null)
                {
                    Debug.Log("partIndex: " + _partIndex + " of " + _parts.Length);
                    FillSlot(_lineIndex, _slotIndex, _parts[_partIndex]);
                    SetPartTransform(_parts[_partIndex].transform, _lineIndex, _slotIndex);
                    _partIndex++;


                }
                if (_partIndex >= _parts.Length)
                {
                    return;
                }
            }
            if (_partIndex >= _parts.Length)
            {
                return;
            }
        }
    }
    public void Force_QuickSave(VehiclePart[] _parts)
    {
        AttemptStore(_parts);
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

    public void Dump_LINE(int _lineIndex = 0)
    {
        Dump_SLOTS(lineLength, _lineIndex);
    }
    public void Dump_SLOTS(int _count, int _lineIndex = 0)
    {

        if (currentState == StorageState.IDLE)
        {
            Debug.Log(storageName + " DUMP");
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
            sendingLineTo = getsPartsFrom;
            parts_OUT = dumpList.ToArray();
        }

    }

    public void DUMP_fromLine_exceptType(int _lineIndex, Vehicle_PartType _keepThisPart, int _maxKept)
    {
        if (currentState == StorageState.IDLE)
        {
            int partsKept = 0;
            ChangeState(StorageState.FETCHING);
            List<VehiclePart> dumpList = new List<VehiclePart>();
            StorageLine _LINE = storageLines[_lineIndex];
            for (int _slotIndex = 0; _slotIndex < lineLength; _slotIndex++)
            {
                VehiclePart _PART = _LINE.slots[_slotIndex];

                if (_PART != null)
                {
                    if (_PART.partConfig.partType != _keepThisPart)
                    {
                        dumpList.Add(_PART);
                        ClearSlot(_lineIndex, _slotIndex);
                    }
                    else
                    {
                        if (partsKept < _maxKept)
                        {
                            partsKept++;
                        }
                        else
                        {
                            dumpList.Add(_PART);
                            ClearSlot(_lineIndex, _slotIndex);
                        }
                    }
                }
            }
            sendingLineTo = getsPartsFrom;
            parts_OUT = dumpList.ToArray();
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