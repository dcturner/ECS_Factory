using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

public class Workshop : MonoBehaviour
{
    public FactoryMode factoryMode;
    public int workshopIndex;
    public float width, height;
    public Storage L2, L1, REG;
    public int freeSpace, usedSpace, completedVehicles;

    private Queue<WorkshopTask> tasklist;
    public WorkshopTask currentTask = null;
    public VehiclePart_Config currentTaskPart;
    public List<VehiclePart_CHASSIS> REG_viableChassis;
    public List<VehiclePart_CHASSIS> workshop_viableChassis;
    public List<VehiclePart> REG_viableParts;
    public bool purgingPartsToSharedStorage = false;

    private void Awake()
    {
        completedVehicles = 0;
        tasklist = new Queue<WorkshopTask>();
        workshop_viableChassis = new List<VehiclePart_CHASSIS>();
    }

    public void Init(FactoryMode _factoryMode)
    {
        factoryMode = _factoryMode;
        Debug.Log(workshopIndex + " MODE: " + _factoryMode);
    }

    private void OnDrawGizmos()
    {
        if (L2 && L1 && REG)
        {
            string myName = "CORE [" + workshopIndex + "]";
            if (factoryMode == FactoryMode.DOD)
            {
                if (currentTask != null)
                {
                    myName += " >> " + currentTask.requiredParts.First().Key.partType;
                }
            }
            GizmoHelpers.DrawRect(Color.cyan, transform.position, width, height, myName);
        }
    }

    public void Tick()
    {
        if (currentTask != null)
        {

            freeSpace = REG.freeSpace + L1.freeSpace + L2.freeSpace;
            usedSpace = REG.usedSpace + L1.usedSpace + L2.usedSpace;
            PerformTask();
        }
    }

    public void SetCurrentTask(WorkshopTask _task)
    {
        currentTask = _task;
        if (factoryMode == FactoryMode.DOD)
        {
            currentTaskPart = currentTask.requiredParts.First().Key;
        }
    }

    // - - - - - - - - - - - - - - - - - PERFORM WORKSHOP TASK
    void PerformTask()
    {
        if (purgingPartsToSharedStorage)
        {
            DoPurge();
            return;
        }

        VehicleChassiRequest _CHASSIS_REQUEST = new VehicleChassiRequest(
            currentTask.design.chassisType.partConfig,
            currentTask.design.chassisType.partConfig.partVersion,
            currentTask.requiredParts,
            REG,
            factoryMode);

        if (
            REG.currentState == StorageState.IDLE ||
            (REG.currentState == StorageState.WAITING && L1.currentState == StorageState.IDLE))
        { 
            // REG is able to request
            REG_viableChassis = FindChassis_in_storage(REG, _CHASSIS_REQUEST);

            if (REG_viableChassis.Count > 0)
            { 
                // HAS CHASSIS
                Update_REG_ViableParts();

                if (REG_viableParts.Count > 0)
                { 
                    // HAS PARTS
                    AttachParts();
                    RemoveCompletedChassis();
                }
                else
                { 
                    // NO PARTS
                    bool REG_IS_FULL = REG.freeSpace == 0;
                    switch (factoryMode)
                    {

                        case FactoryMode.OOP:

                            if (REG_IS_FULL)
                            {
                                REG.DUMP_fromLine_exceptType(0, Vehicle_PartType.CHASSIS, 1);
                            }
                            else
                            {
                                RequestViableParts(REG_viableChassis[0]);
                            }

                            break;

                        case FactoryMode.DOD:

                            if (REG_IS_FULL)
                            {
                                REG.DUMP_nonViable_CHASSIS(_CHASSIS_REQUEST, 0);
                            }
                            else
                            {
                                RequestTaskPart();
                            }
                            break;
                    }
                }
            }
            else
            { // NO CHASSIS

                if (REG.freeSpace > 0)
                {
                    REG.waitingForPartType = _CHASSIS_REQUEST.part;
                    REG.ChangeState(StorageState.WAITING);
                    L1.RequestChassis(_CHASSIS_REQUEST);
                }
                else
                { // NO ROOM, DUMP
                    REG.DUMP_fromLine_exceptType(0, Vehicle_PartType.CHASSIS, 1);
                }
            }
        }

        //else if (L2.currentState == StorageState.WAITING && Factory.INSTANCE.L3.currentState == StorageState.IDLE)
        //{

        //    if (L2.current_PART_request != null)
        //    {
        //        //Debug.Log(workshopIndex + "_L2 was waiting for L3 - new req sent");
        //        Factory.INSTANCE.L3.ClearRequests();
        //        Factory.INSTANCE.RAM.ClearRequests();
        //        Factory.INSTANCE.HD.ClearRequests();
        //        Factory.INSTANCE.L3.RequestPart(new VehiclePartRequest(L2.current_PART_request.part, L2));
        //    }
        //    else if (L2.current_CHASSIS_request != null)
        //    {
        //        //Debug.Log(workshopIndex + "_L2 was waiting for L3 - new req sent");
        //        Factory.INSTANCE.L3.ClearRequests();
        //        Factory.INSTANCE.RAM.ClearRequests();
        //        Factory.INSTANCE.HD.ClearRequests();
        //        Factory.INSTANCE.L3.RequestChassis(new VehicleChassiRequest(_CHASSIS_REQUEST.part, _CHASSIS_REQUEST.chassisVersion, _CHASSIS_REQUEST.requiredParts, L2, _CHASSIS_REQUEST.factoryMode));
        //    }
        //}

        //if (L1.currentState == StorageState.IDLE)
        //{

        //    if (L1.HasNonViableChassis(_CHASSIS_REQUEST))
        //    {
        //        L1.DUMP_first_nonViable_CHASSIS(_CHASSIS_REQUEST);
        //    }
        //}
        //else if (L2.currentState == StorageState.IDLE)
        //{
        //    if (L2.HasNonViableChassis(_CHASSIS_REQUEST))
        //    {
        //        L2.DUMP_first_nonViable_CHASSIS(_CHASSIS_REQUEST);
        //    }
        //}
    }

    private void Update_REG_ViableParts()
    {

        REG_viableParts = new List<VehiclePart>();
        List<VehiclePart_Config> _TASK_PARTS = currentTask.requiredParts.Keys.ToList();

        // If part is NOT a chassis and is used in the current TASK - add it to VIABLE_PARTS
        for (int _slotIndex = 0; _slotIndex < REG.lineLength; _slotIndex++)
        {
            var _PART = REG.GetSlot(0, _slotIndex);
            if (_PART != null)
            {
                if (_PART.partConfig.partType != Vehicle_PartType.CHASSIS)
                {
                    if (_TASK_PARTS.Contains(_PART.partConfig))
                    {
                        foreach (VehiclePart_CHASSIS _CHASSIS in REG_viableChassis)
                        {
                            if (!_CHASSIS.partsFitted.ContainsKey(_PART.partConfig))
                            {
                                REG_viableParts.Add(_PART);
                            }
                            else if (_CHASSIS.partsFitted[_PART.partConfig] < _CHASSIS.design.quantities[_PART.partConfig])
                            {
                                REG_viableParts.Add(_PART);
                            }
                        }
                    }
                }
            }
        }
    }

    void AttachParts()
    {
        for (int _slotIndex = 0; _slotIndex < REG.lineLength; _slotIndex++)
        {
            var _PART = REG.storageLines[0].slots[_slotIndex];
            if (REG_viableParts.Contains(_PART))
            {
                for (int _chassisIndex = 0; _chassisIndex < REG_viableChassis.Count; _chassisIndex++)
                {
                    if (REG_viableChassis[_chassisIndex].AttachPart(_PART.partConfig, _PART.gameObject))
                    {
                        REG_viableParts.Remove(_PART);
                        Factory.INSTANCE.PartAttached(_PART.partConfig, this);
                        REG.ClearSlot(0, _slotIndex);
                        break;
                    }
                }
            }
        }
    }
    void RemoveCompletedChassis()
    {
        foreach (VehiclePart_CHASSIS _CHASSIS in REG_viableChassis)
        {
            if (_CHASSIS.vehicleIsComplete)
            {
                int indexOfCompletedChassis = REG.storageLines[0].slots.IndexOf(_CHASSIS);
                REG.ClearSlot(0, indexOfCompletedChassis);
                Factory.INSTANCE.VehicleComplete(_CHASSIS);
                completedVehicles++;

                _CHASSIS.transform.position = transform.position + new Vector3(completedVehicles % 10, 0f, -1 - Mathf.FloorToInt(completedVehicles / 10));
            }
        }
    }

    public void RequestViableParts(VehiclePart_CHASSIS _chassis)
    {
        foreach (KeyValuePair<VehiclePart_Config, int> _PAIR in currentTask.requiredParts)
        {
            if (_PAIR.Key.partType != Vehicle_PartType.CHASSIS)
            {
                if (currentTask.requiredParts.ContainsKey(_PAIR.Key))
                {
                    L1.RequestPart(new VehiclePartRequest(_PAIR.Key, REG));
                    REG.waitingForPartType = _chassis.partsNeeded[0].partConfig;
                    if (!purgingPartsToSharedStorage)
                    {
                        REG.ChangeState(StorageState.WAITING);
                    }
                    return;
                }
            }
        }
    }
    public void RequestTaskPart()
    {
        REG.ChangeState(StorageState.WAITING);

        Debug.Log(workshopIndex + " L1 req: " + currentTaskPart);
        L1.RequestPart(new VehiclePartRequest(currentTaskPart, REG));
    }

    public List<VehiclePart_CHASSIS> FindChassis_in_storage(Storage _storage, VehicleChassiRequest _request)
    {
        List<VehiclePart_CHASSIS> _result = new List<VehiclePart_CHASSIS>();
        // Iterate through LINES

        for (int _slotIndex = 0; _slotIndex < _storage.lineLength; _slotIndex++)
        {
            if (_storage.IsChassisViable(0, _slotIndex, _request))
            {
                _result.Add(_storage.storageLines[0].slots[_slotIndex] as VehiclePart_CHASSIS);
            }
        }
        return _result;
    }

    public void PurgePartsToSharedStorage()
    {
        purgingPartsToSharedStorage = true;
        Debug.Log("PURGE WORKSHOP: " + workshopIndex);
        Factory.INSTANCE.L3.AWAIT_PURGED_DATA();
        L2.AWAIT_PURGED_DATA();
        L1.AWAIT_PURGED_DATA();
        REG.AWAIT_PURGED_DATA();

    }
    public void CancelPurge()
    {
        purgingPartsToSharedStorage = false;
        Debug.Log("CANCEL PURGE: " + workshopIndex);
        Factory.INSTANCE.L3.ChangeState(StorageState.IDLE);
        L2.ChangeState(StorageState.IDLE);
        L1.ChangeState(StorageState.IDLE);
        REG.ChangeState(StorageState.IDLE);

    }
    public void ClearRequestsAndIdle()
    {
        L2.ClearRequests();
        L1.ClearRequests();
        REG.ClearRequests();

        REG.ChangeState(StorageState.IDLE);
        L1.ChangeState(StorageState.IDLE);
        L2.ChangeState(StorageState.IDLE);
    }

    public void DoPurge()
    {

        if (REG.usedSpace > 0)
        {
            REG.Dump_LINE();
        }
        else if (L1.usedSpace > 0)
        {
            Factory.INSTANCE.L3.AWAIT_PURGED_DATA();
            L1.Dump_earliestLineWithData();

        }
        else if (L2.usedSpace > 0)
        {
            Factory.INSTANCE.L3.AWAIT_PURGED_DATA();
            L2.Dump_earliestLineWithData();
            if (L2.usedSpace == 0)
            {
                // purge complete
                // reset all workshops - start searching fresh for parts
                foreach (var _WORKSHOP in Factory.INSTANCE.workshops)
                {
                    _WORKSHOP.ClearRequestsAndIdle();
                }
            }
        }
    }
}

public class WorkshopTask
{
    public VehicleDesign design;
    public Dictionary<VehiclePart_Config, int> requiredParts;
    public float ratio_chassis_to_parts;
    public int totalRequired;
    public int tasksCompleted;

    public WorkshopTask(VehicleDesign _design, Dictionary<VehiclePart_Config, int> _requiredParts, int _totalRequired = 0)
    {
        design = _design;
        requiredParts = _requiredParts;
        totalRequired = _totalRequired;
        tasksCompleted = 0;
        int partCount = 0;
        foreach (KeyValuePair<VehiclePart_Config, int> _PAIR in requiredParts)
        {
            if (_PAIR.Key.partType != Vehicle_PartType.CHASSIS)
            {
                partCount++;
            }
        }

        ratio_chassis_to_parts = 1 / (float)partCount;
    }
}