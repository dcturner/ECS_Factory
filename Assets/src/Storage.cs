using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class Storage : MonoBehaviour
{
    public static bool GIZMOS_DRAW_CELLS = true;
    
    public string storageName;
    public Color colour;
    [PropertyRange(1, 100000)] public int taskDuration;

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
    [HideInInspector] public List<VehiclePart_Config> contents;
    [HideInEditorMode] public List<VehiclePart_Config> pending_STORE, pending_SEND;

    // Storage GRID layout
    private int cellsX, cellsY, cellsZ;

    private void Awake()
    {
        pending_STORE = new List<VehiclePart_Config>();
        pending_SEND = new List<VehiclePart_Config>();
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
    }

    private void ChangeState(StorageState _newState)
    {
        if (currentState != _newState)
        {
            currentState = _newState;
            Debug.Log(ToString() + currentState);

            switch (currentState)
            {
                case StorageState.VOTING:
                    break;
                case StorageState.STORE:
                    break;
                case StorageState.FETCH:
                    break;
            }
        }
    }

    public void Request_STORE(VehiclePart_Config[] _parts)
    {
        int _SLOTS_FREE = contents.Count - pending_SEND.Count;
        for (int i = 0; i < _SLOTS_FREE; i++)
        {
            pending_STORE.Add(_parts[i]);
        }
    }

    public void Request_SEND(List<VehiclePart_Config> _parts, Storage _destination)
    {
        for (int i = 0; i < _parts.Count; i++)
        {
            VehiclePart_Config _PART = _parts[i];
            contents.Remove(_PART);
            pending_SEND.Add(_PART);
        }

        _destination.Request_STORE(_parts.ToArray());
    }

    public void Tick()
    {
        taskStep = (taskStep + 1) % taskDuration;
        factor = (float) taskStep / (float) taskDuration;

        bool ACTION_TICK = taskStep == 0;

        if (ACTION_TICK)
        {
            // Send stock out
            for (int i = 0; i < pending_SEND.Count; i++)
            {
                contents.Remove(pending_SEND[i]);
            }

            pending_SEND.Clear();

            // Get new stock 
            for (int i = 0; i < pending_STORE.Count; i++)
            {
                contents.Add(pending_STORE[i]);
            }

            pending_STORE.Clear();
        }
        else
        {
        }
    }

    private void OnDrawGizmos()
    {
        

            Vector3 _POS = transform.position;
            Gizmos.color = colour;
            width = (clusters_x * cluster_width) + ((clusters_x - 1) * gutterX);
            height = (clusters_y * cluster_height) + ((clusters_y - 1) * gutterY);
            depth = (clusters_z * cluster_depth) + ((clusters_z - 1) * gutterZ);
            Vector3 _SIZE = new Vector3(width, height, depth);
            Gizmos.DrawWireCube(_POS + _SIZE * 0.5f, _SIZE);

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

                Gizmos.color = colour;
                foreach (Vector3 _AISLE_SPACE in aisleSpaces)
                {
                    Gizmos_DrawCell(_AISLE_SPACE, cellSize);
                }
            }

            clusterCapacity = cluster_width * cluster_height * cluster_depth;
            capacity = clusterCapacity * (clusters_x * clusters_y * clusters_z);
        }
    }

    void Gizmos_DrawCell(Vector3 _cell_POS, Vector3 _cell_SIZE)
    {
        Gizmos.DrawCube(_cell_POS + _cell_SIZE * 0.5f, _cell_SIZE);
//        Gizmos.DrawWireCube(_cell_POS, _cell_SIZE );
//        GizmoHelpers.DrawRect(colour, _cell_POS, _cell_SIZE.x, _cell_SIZE.y);
    }
}

public enum StorageState
{
    VOTING,
    STORE,
    FETCH
}