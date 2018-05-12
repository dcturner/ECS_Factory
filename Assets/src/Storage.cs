using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class Storage : MonoBehaviour
{
    public string storageName;
    public Color colour;
    public int storageWidth, storageHeight;
    public int capacity, usedSpace, freeSpace;
    [HideInInspector] public StorageState currentState;
    public int taskDuration;
    public int taskStep;

    [HideInInspector] public List<Vector3> storageLocations;
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
        
        for (int columnIndex = 0; columnIndex < storageWidth; columnIndex++)
        {
            float x = (columnIndex * 1f);
            if (columnIndex % 8 > 0)
            {
                for (int rowIndex = 0; rowIndex < storageHeight; rowIndex++)
                {
                    float z = rowIndex * 1f;
                    if (rowIndex % 3 > 0)
                    {
                        storageLocations.Add(transform.position + new Vector3(x + 0.5f, 0f, z + 0.5f));
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
                case StorageState.IDLE:
                    break;
                case StorageState.STORE:
                    break;
                case StorageState.FETCH:
                    break;
            }
        }
    }

    public void Request_STORE(VehiclePart[] _parts)
    {
    }

    public void Request_SEND(List<VehiclePart> _parts, Storage _destination)
    {
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
        GizmoHelpers.DrawRect(colour, _POS, storageWidth, storageHeight, Log());

        Vector3 _START = _POS + new Vector3(0f, 0f, storageHeight * factor);
        GizmoHelpers.DrawLine(Color.white, _START, _START + new Vector3(storageWidth, 0f, 0f));

        // flash on action?
        if (taskStep == 0)
        {
        }

        // draw storage grid
//        Gizmos.color = new Color(0.5f, 0.5f, 0.5f);
        for (int columnIndex = 0; columnIndex < storageWidth; columnIndex++)
        {
            float x = (columnIndex * 1f);
            if (columnIndex % 8 > 0)
            {
                for (int rowIndex = 0; rowIndex < storageHeight; rowIndex++)
                {
                    float z = rowIndex * 1f;
                    if (rowIndex % 3 > 0)
                    {
                        Gizmos.DrawCube(_POS + new Vector3(x + 0.5f, 0f, z + 0.5f), new Vector3(0.75f, 0.01f,0.75f));
                    }
                }
            }
        }
    }
}

public enum StorageState
{
    IDLE,
    STORE,
    FETCH
}