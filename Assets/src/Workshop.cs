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

    private Queue<WorkshopTask> tasklist;
    public WorkshopTask currentTask = null;

    private void Awake()
    {
        tasklist = new Queue<WorkshopTask>();
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
            PerformTask();
        }
    }

    // Perform the task as many times as possible
    void PerformTask()
    {
        if (REG.currentState == StorageState.IDLE)
        {
            VehiclePart_CHASSIS requiredChassis = currentTask.design.chassisType;

            // Does REG have a viable Chassis?
            List<VehiclePart_CHASSIS> _VIABLE_CHASSIS =
                FindChassis_in_REG(requiredChassis.partConfig.partVersion, currentTask.requiredParts);

            if (_VIABLE_CHASSIS.Count > 0)
            {
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
                                //                        Debug.Log("Part IS found in task list ("+ _PART.partConfig.name +")");
                                _viableParts.Add(_PART);
                            }
                        }
                    }
                }

                if (_viableParts.Count > 0)
                {
                    List<VehiclePart> _attachedParts = new List<VehiclePart>();
                    foreach (VehiclePart _VIABLE_PART in _viableParts)
                    {
                        foreach (VehiclePart_CHASSIS _VC in _VIABLE_CHASSIS)
                        {
                            if (_VC.AttachPart(_VIABLE_PART.partConfig, _VIABLE_PART.gameObject))
                            {
                                _attachedParts.Add(_VIABLE_PART);
                            }
                        }
                    }

                    // clean up slots of attached parts
                    for (int _slotIndex = 0; _slotIndex < REG.lineLength; _slotIndex++)
                    {
                        if(_attachedParts.Contains(REG.storageLines[0].slots[_slotIndex])){
                            REG.storageLines[0].slots[_slotIndex] = null;
                            _viableParts.Remove(REG.storageLines[0].slots[_slotIndex]);
                        }
                    }

                    foreach (VehiclePart_CHASSIS _CHASSIS in _VIABLE_CHASSIS)
                    {
                        if (_CHASSIS.vehicleIsComplete)
                        {
                            int indexOfCompletedChassis = REG.storageLines[0].slots.IndexOf(_CHASSIS);
                            REG.storageLines[0].slots[indexOfCompletedChassis] = null;
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
                        Debug.Log("space available in REG");
                        foreach (VehiclePart_CHASSIS _CHASSIS in _VIABLE_CHASSIS)
                        {
                            if (REG.currentState == StorageState.IDLE)
                            {
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

                // Can I perform the assigned task?
                // do I have any required parts AND a suitable chassis? - if so, DO IT

                // if not, order the parts for the task
            }
            else
            {
                // no viable CHASSIS - request some
                //Debug.Log("REG needs chassis");
                L1.RequestChassis(new VehicleChassiRequest(requiredChassis.partConfig,
                    requiredChassis.partConfig.partVersion, currentTask.requiredParts, REG));
            }
        }
    }

    public void RequestViableParts(VehiclePart_CHASSIS _chassis)
    {
        foreach (KeyValuePair<VehiclePart_Config, int> _PAIR in currentTask.requiredParts)
        {
            if (_PAIR.Key.partType != Vehicle_PartType.CHASSIS)
            {
                int partsToRequest = 0;
                VehiclePart_Config _CONFIG = _chassis.partsNeeded[0].partConfig;
                foreach (VehiclePart_Assignment _PART in _chassis.partsNeeded)
                {
                    if (_PART.partConfig == _CONFIG)
                    {
                        partsToRequest++;
                    }
                }

                L1.RequestPart(new VehiclePartRequest(_chassis.partsNeeded[0].partConfig, REG));
                REG.ChangeState(StorageState.WAITING);
                break;
            }
        }
    }

    public List<VehiclePart_CHASSIS> FindChassis_in_REG(int _chassisVersion,
       Dictionary<VehiclePart_Config, int> _requiredParts)
    {
        List<VehiclePart_CHASSIS> _result = new List<VehiclePart_CHASSIS>();
        // Iterate through LINES

        for (int _slotIndex = 0; _slotIndex < REG.lineLength; _slotIndex++)
        {
            if(REG.IsChassisViable(0,_slotIndex,_chassisVersion, _requiredParts)){
                _result.Add(REG.storageLines[0].slots[_slotIndex] as VehiclePart_CHASSIS);
            }
        }
        return _result;
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