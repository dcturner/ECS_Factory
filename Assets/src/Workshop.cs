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
    public int workshopIndex;
    public float width, height;
    public Storage L2, L1, REG;
    public int freeSpace, usedSpace;

    private Queue<WorkshopTask> tasklist;
    public WorkshopTask currentTask = null;
    public List<VehiclePart_CHASSIS> workshop_viableChassis;
    public bool workShopHasChassis = false;
    public bool purgingPartsToSharedStorage = false;

    private void Awake()
    {
        tasklist = new Queue<WorkshopTask>();
        workshop_viableChassis = new List<VehiclePart_CHASSIS>();
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
            VehiclePart_CHASSIS requiredChassis = currentTask.design.chassisType;

            int _VIABLE_CHASSIS_VERSION = requiredChassis.partConfig.partVersion;
            Dictionary<VehiclePart_Config, int> _REQUIRED_PARTS = currentTask.requiredParts;

            bool hasChassis_REG = REG.HasViableChassis(_VIABLE_CHASSIS_VERSION, _REQUIRED_PARTS);
            // Does REG have a viable Chassis?


            if (hasChassis_REG)
            {
                workShopHasChassis = true;
                List<VehiclePart_CHASSIS> _VIABLE_CHASSIS =
                FindChassis_in_storage(REG, requiredChassis.partConfig.partVersion, currentTask.requiredParts);
                // which required parts do I have?
                List<VehiclePart> _viableParts = new List<VehiclePart>();
                List<VehiclePart_Config> _TASK_PARTS = currentTask.requiredParts.Keys.ToList();


                // If part is NOT a chassis and is used in the current TASK - add it to VIABLE_PARTS
                for (int _slotIndex = 0; _slotIndex < REG.lineLength; _slotIndex++)
                {
                    if (REG.storageLines[0].slots[_slotIndex] != null)
                    {
                        var _PART = REG.storageLines[0].slots[_slotIndex];
                        //                Debug.Log("Checking part: " + _PART.partConfig.name);
                        if (_PART.partConfig.partType != Vehicle_PartType.CHASSIS)
                        {
                            //                    Debug.Log("not chassis found: " + _PART.partConfig.name);
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
                //Debug.Log("REG - vP: " + _viableParts.Count + ", vC: " + _VIABLE_CHASSIS.Count);
                if (_viableParts.Count > 0)
                {
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
                            Destroy(_CHASSIS.gameObject);
                            Factory.INSTANCE.VehicleComplete(_CHASSIS);
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
                        // no space available - get rid of everything except one viable chassis
                        REG.DUMP_fromLine_exceptType(0, Vehicle_PartType.CHASSIS, 1);
                    }
                }
            }
            else
            {

                // no viable CHASSIS - request some

                bool hasChassis_L1 = L1.HasViableChassis(_VIABLE_CHASSIS_VERSION, _REQUIRED_PARTS);
                bool hasChassis_L2 = L1.HasViableChassis(_VIABLE_CHASSIS_VERSION, _REQUIRED_PARTS);

                workShopHasChassis = (hasChassis_L1 || hasChassis_L2);

                REG.waitingForPartType = requiredChassis.partConfig;
                REG.ChangeState(StorageState.WAITING);
                L1.RequestChassis(new VehicleChassiRequest(requiredChassis.partConfig,
                requiredChassis.partConfig.partVersion, currentTask.requiredParts, REG));
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

    public List<VehiclePart_CHASSIS> FindChassis_in_storage(Storage _storage, int _chassisVersion, Dictionary<VehiclePart_Config, int> _requiredParts)
    {
        List<VehiclePart_CHASSIS> _result = new List<VehiclePart_CHASSIS>();
        // Iterate through LINES

        for (int _slotIndex = 0; _slotIndex < _storage.lineLength; _slotIndex++)
        {
            if (_storage.IsChassisViable(0, _slotIndex, _chassisVersion, _requiredParts))
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