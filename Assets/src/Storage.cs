using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
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
    public int clusters_x, clusters_y, clusters_z;

    // How many cells make up a cluster?  XYZ
    [TabGroup("ClusterSize")] [PropertyRange(1, 100)]
    public int cluster_width, cluster_height, cluster_depth;

    // How much space goes between clusters? XYZ
    [TabGroup("Gutters")] [PropertyRange(0, 10)]
    public int gutterX, gutterY, gutterZ;

    private int width, height, depth;
    [ReadOnly] public int usedSpace, freeSpace, capacity, clusterCapacity, taskStep;
    [HideInInspector] public StorageState currentState;

    [HideInInspector] public List<Vector3> storageLocations;
    [HideInInspector] public List<Vector3> aisleSpaces;
    [HideInInspector] public float factor;
    [HideInInspector] public List<VehiclePart> contents;
    [HideInEditorMode] public VehiclePart[] pending_STORE, pending_SEND;
    [HideInInspector] public VehiclePartRequest pending_REQUEST;

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
        aisleSpaces = new List<Vector3>();
        Vector3 _POS = transform.position;

        Vector3 cellSize = new Vector3(0.75f, 0.75f, 0.75f);
        clusterCapacity = cluster_width * cluster_height * cluster_depth;
        capacity = clusterCapacity * (clusters_x * clusters_y * clusters_z);
        freeSpace = capacity;
        usedSpace = 0;

        width = (clusters_x * cluster_width) + ((clusters_x - 1) * gutterX);
        height = (clusters_y * cluster_height) + ((clusters_y - 1) * gutterY);
        depth = (clusters_z * cluster_depth) + ((clusters_z - 1) * gutterZ);

        int wrapX = cluster_width + (gutterX);
        int wrapY = cluster_height + (gutterY);
        int wrapZ = cluster_depth + (gutterZ);

        for (int cellX = 0; cellX < width; cellX++)
        {
            for (int cellY = 0; cellY < height; cellY++)
            {
                for (int cellZ = 0; cellZ < depth; cellZ++)
                {
                    Vector3 _CELL_POS = _POS + new Vector3(cellX, cellY, cellZ);
                    if (cellX % wrapX < cluster_width && cellY % wrapY < cluster_height &&
                        cellZ % wrapZ < cluster_depth)
                    {
                        storageLocations.Add(_CELL_POS);
                    }
                    else
                    {
                        aisleSpaces.Add(_CELL_POS);
                    }
                }
            }
        }

        Debug.Log(storageName + " locations: " + storageLocations.Count);
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
                    if (pending_REQUEST != null)
                    {
                        RequestPart(pending_REQUEST);
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
        int index = 0;
        while (freeSpace > 0 && index < _parts.Length)
        {
            VehiclePart _PART = _parts[index];
            contents.Add(_PART);
            _PART.transform.position = storageLocations[usedSpace];
            index++;
            freeSpace--;
            usedSpace++;
        }
    }

    public void RequestPart(VehiclePartRequest _request)
    {
        if (currentState == StorageState.IDLE)
        {
            Debug.Log(storageName + " sourcing: " + _request + " " + _request.part.name + " for " +
                      _request.deliverTo.storageName);
            sendingPartsTo = _request.deliverTo;
            VehiclePart[] _partsFound = contents.Where(p => p.partConfig == _request.part).ToArray();
            int _totalFound = _partsFound.Length;
            if (_totalFound > 0)
            {
                Debug.Log(storageName + " found: " + _totalFound + " " + _request.part.name);
                if (_totalFound > _request.maxParts)
                {
                    Debug.Log("too many, trimming down to " + _request.maxParts);
                    Array.Resize(ref _partsFound, _request.maxParts);
                }

                pending_SEND = _partsFound;
                pending_REQUEST = null;
                Debug.Log("preparing to send " + _partsFound.Length + " parts to " + _request.deliverTo.storageName);
                ChangeState(StorageState.FETCHING_REQUESTED_ITEMS);
            }
            else
            {
                pending_REQUEST = _request;
                ChangeState(StorageState.WAITING_FOR_DELIVERY);
                getsPartsFrom.RequestPart(new VehiclePartRequest(_request.part, this, _request.maxParts*2));
            }
        }
    }

    private int CurrentStorageCell()
    {
        return contents.Count - 1;
    }

    private Vector3 CurrentStorageLocation()
    {
        return storageLocations[CurrentStorageCell()];
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
        Debug.Log(storageName + " SENDING ITEMS");
        taskStep = 0;
        foreach (VehiclePart _PART in pending_SEND)
        {
            contents.Remove(_PART);
            freeSpace++;
            usedSpace--;
        }
        sendingPartsTo.Recieve_Parts(pending_SEND);
        pending_SEND = null;
        ChangeState(StorageState.IDLE);
    }

    public void Recieve_Parts(VehiclePart[] _parts)
    {
        if (currentState != StorageState.FETCHING_REQUESTED_ITEMS)
        {
            // store
//            Debug.Log(storageName + " Recieved " + _parts.Length + " x " + _parts[0].partConfig.name);
            AttemptStore(_parts);
            ChangeState(StorageState.IDLE);
        }
        else
        {
            Debug.Log(storageName + " Waiting to Store " + _parts.Length + " x " + _parts[0].partConfig.name);
            pending_STORE = _parts;
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 _POS = transform.position;
        Gizmos.color = colour;
        width = (clusters_x * cluster_width) + ((clusters_x - 1) * gutterX);
        height = (clusters_y * cluster_height) + ((clusters_y - 1) * gutterY);
        depth = (clusters_z * cluster_depth) + ((clusters_z - 1) * gutterZ);
        Vector3 _SIZE = new Vector3(width, 0f, depth);
        GizmoHelpers.DrawRect(colour, _POS, width, depth, Log());

        Vector3 _START = _POS + new Vector3(0f, 0f, depth * factor);
        GizmoHelpers.DrawLine(Color.white, _START, _START + new Vector3(width, 0f, 0f));

        // flash on action?
        if (taskStep == 0)
        {
        }

        if (GIZMOS_DRAW_CELLS)
        {
            Vector3 cellSize = new Vector3(0.75f, 0.75f, 0.75f);
            if (storageLocations.Count == 0)
            {
                int wrapX = cluster_width + (gutterX);
                int wrapY = cluster_height + (gutterY);
                int wrapZ = cluster_depth + (gutterZ);

                for (int cellX = 0; cellX < width; cellX++)
                {
                    for (int cellY = 0; cellY < height; cellY++)
                    {
                        for (int cellZ = 0; cellZ < depth; cellZ++)
                        {
                            if (cellX % wrapX < cluster_width && cellY % wrapY < cluster_height &&
                                cellZ % wrapZ < cluster_depth)
                            {
                                Gizmos_DrawCell(_POS + new Vector3(cellX, cellY, cellZ), cellSize);
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
                    Gizmos_DrawCell(_STORAGE_LOCATION, cellSize);
                }
            }

            clusterCapacity = cluster_width * cluster_height * cluster_depth;
            capacity = clusterCapacity * (clusters_x * clusters_y * clusters_z);
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