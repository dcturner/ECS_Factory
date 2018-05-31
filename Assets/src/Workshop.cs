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
    public List<VehiclePart_CHASSIS> workshop_viableChassis;
    public bool workShopHasChassis = false;
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
        Debug.Log(workshopIndex +  " MODE: " + _factoryMode);
	}

	private void OnDrawGizmos()
    {
        if (L2 && L1 && REG)
        {
            GizmoHelpers.DrawRect(Color.cyan, transform.position, width, height, "CORE [" + workshopIndex + "]");
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

    // Perform the task as many times as possible
    void PerformTask()
    {
        if (purgingPartsToSharedStorage)
        {
            DoPurge();
            return;
        }
        if (REG.currentState == StorageState.IDLE)
        {

            // OOP / DOD different ways to determine if REG has a viable chassis
            bool REG_hasChassis = false;
            VehicleChassiRequest _CHASSIS_REQUEST = new VehicleChassiRequest(null,-1,null,null,FactoryMode.OOP);
            switch (factoryMode)
            {
                case FactoryMode.OOP:
                    _CHASSIS_REQUEST = new VehicleChassiRequest(currentTask.design.chassisType.partConfig, currentTask.design.chassisType.partConfig.partVersion, currentTask.requiredParts, REG, FactoryMode.OOP);
                        REG_hasChassis = REG.HasViableChassis(_CHASSIS_REQUEST);
                    break;
                case FactoryMode.DOD:
                    _CHASSIS_REQUEST = new VehicleChassiRequest(null, -1, currentTask.requiredParts, REG, FactoryMode.DOD);
                        REG_hasChassis = REG.HasViableChassis(_CHASSIS_REQUEST);
                    break;
            }

            if (REG_hasChassis)
            {
                workShopHasChassis = true;
                List<VehiclePart_CHASSIS> _VIABLE_CHASSIS =
                    FindChassis_in_storage(REG, _CHASSIS_REQUEST);
                
                // which required parts do I have?
                List<VehiclePart> _viableParts = new List<VehiclePart>();
                List<VehiclePart_Config> _TASK_PARTS = currentTask.requiredParts.Keys.ToList();

                // If part is NOT a chassis and is used in the current TASK - add it to VIABLE_PARTS
                for (int _slotIndex = 0; _slotIndex < REG.lineLength; _slotIndex++)
                {
                    if (REG.storageLines[0].slots[_slotIndex] != null)
                    {
                        
                        var _PART = REG.storageLines[0].slots[_slotIndex];
                        if (_PART.partConfig.partType != Vehicle_PartType.CHASSIS)
                        {

                            if (_TASK_PARTS.Contains(_PART.partConfig))
                            {
                                foreach (VehiclePart_CHASSIS _CHASSIS in _VIABLE_CHASSIS)
                                {
                                    if (!_CHASSIS.partsFitted.ContainsKey(_PART.partConfig))
                                    {
                                        _viableParts.Add(_PART);
                                    }
                                    else if (_CHASSIS.partsFitted[_PART.partConfig] < _CHASSIS.design.quantities[_PART.partConfig])
                                    {
                                        _viableParts.Add(_PART);
                                    }
                                }
                            }
                        }
                    }
                }

                if (_viableParts.Count > 0)
                {
                    Debug.Log(workshopIndex + " viable parts: " + _viableParts.Count);
                    for (int _slotIndex = 0; _slotIndex < REG.lineLength; _slotIndex++)
                    {
                        var _PART = REG.storageLines[0].slots[_slotIndex];
                        if (_viableParts.Contains(_PART))
                        {
                            for (int _chassisIndex = 0; _chassisIndex < _VIABLE_CHASSIS.Count; _chassisIndex++)
                            {
                                if (_VIABLE_CHASSIS[_chassisIndex].AttachPart(_PART.partConfig, _PART.gameObject))
                                {
                                    _viableParts.Remove(_PART);
                                    REG.ClearSlot(0, _slotIndex);
                                    break;
                                }
                            }
                        }
                    }

                    foreach (VehiclePart_CHASSIS _CHASSIS in _VIABLE_CHASSIS)
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
                else
                {
                    // no action can be undertaken - do we have room to load a chassis?
                    if (REG.freeSpace > 0)
                    {
                        foreach (VehiclePart_CHASSIS _CHASSIS in _VIABLE_CHASSIS)
                        {
                            if (REG.currentState == StorageState.IDLE)
                            {
                                //Debug.Log("W_" + workshopIndex + " partREQ");
                                RequestViableParts(_CHASSIS);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        REG.DUMP_fromLine_exceptType(0, Vehicle_PartType.CHASSIS, 1);
                        // no space available - get rid of unwanted parts
                        //switch (factoryMode)
                        //{
                        //    case FactoryMode.OOP:
                        //        // OOP mode keep chassis
                        //        REG.DUMP_fromLine_exceptType(0, Vehicle_PartType.CHASSIS, 1);
                        //        break;
                        //    case FactoryMode.DOD:
                        //        // keep 1 viable chassis
                        //        KeyValuePair<VehiclePart_Config, int> _taskPart = currentTask.requiredParts.First();
                        //        REG.DUMP_fromLine_exceptType(0, _taskPart.Key.partType, _taskPart.Value);
                        //        break;
                        //}
                    }
                }
            }
            else
            {

                // no viable CHASSIS - request some

                bool hasChassis_L1 = L1.HasViableChassis(_CHASSIS_REQUEST);
                bool hasChassis_L2 = L1.HasViableChassis(_CHASSIS_REQUEST);

                workShopHasChassis = (hasChassis_L1 || hasChassis_L2);

                REG.waitingForPartType = _CHASSIS_REQUEST.part;
                REG.ChangeState(StorageState.WAITING);
                L1.RequestChassis(_CHASSIS_REQUEST);
            }
        }
        else if (L2.currentState == StorageState.WAITING && Factory.INSTANCE.L3.currentState == StorageState.IDLE)
        {

            if (L2.current_PART_request != null)
            {
                //Debug.Log(workshopIndex + "_L2 was waiting for L3 - new req sent");
                Factory.INSTANCE.L3.ClearRequests();
                Factory.INSTANCE.RAM.ClearRequests();
                Factory.INSTANCE.HD.ClearRequests();
                Factory.INSTANCE.L3.RequestPart(new VehiclePartRequest(L2.current_PART_request.part, L2));
            }
        }
    }

    public void RequestViableParts(VehiclePart_CHASSIS _chassis)
    {
        foreach (KeyValuePair<VehiclePart_Config, int> _PAIR in currentTask.requiredParts)
        {
            if (_PAIR.Key.partType != Vehicle_PartType.CHASSIS)
            {

                L1.RequestPart(new VehiclePartRequest(_chassis.partsNeeded[0].partConfig, REG));
                REG.waitingForPartType = _chassis.partsNeeded[0].partConfig;
                if (!purgingPartsToSharedStorage)
                {
                    REG.ChangeState(StorageState.WAITING);
                }
                return;

            }
        }
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
    public void ClearRequestsAndIdle(){
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
            if(L2.usedSpace == 0){
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

    public WorkshopTask(VehicleDesign _design, Dictionary<VehiclePart_Config, int> _requiredParts)
    {
        design = _design;
        requiredParts = _requiredParts;
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