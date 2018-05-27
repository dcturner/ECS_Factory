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
                REG.FindChassis(requiredChassis.partConfig.partVersion, currentTask.requiredParts);

            if (_VIABLE_CHASSIS.Count > 0)
            {
                // which required parts do I have?
                List<VehiclePart> _viableParts = new List<VehiclePart>();
                List<VehiclePart_Config> _TASK_PARTS = currentTask.requiredParts.Keys.ToList();
                for (int SLOT_INDEX = 0; SLOT_INDEX < REG.lineLength; SLOT_INDEX++)
                {
                    if (REG.storageLines[0].slots[SLOT_INDEX] != null)
                    {
                        var _PART = REG.storageLines[0].slots[SLOT_INDEX];
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
                    Debug.Log("Viable parts - " + _viableParts.Count);
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

                    foreach (VehiclePart _ATTACHED_PART in _attachedParts)
                    {
                        _viableParts.Remove(_ATTACHED_PART);
                        REG.storageLines[0].slots.Remove(_ATTACHED_PART);
                    }

                    foreach (VehiclePart_CHASSIS _CHASSIS in _VIABLE_CHASSIS)
                    {
                        if (_CHASSIS.vehicleIsComplete)
                        {
                            REG.storageLines[0].slots.Remove(_CHASSIS);
                            Destroy(_CHASSIS.gameObject);
                            Factory.INSTANCE.VehicleComplete(_CHASSIS);
                        }
                    }

//                REG.RefactorStorage();
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
                        Debug.Log("NO ROOM IN REG - DITCH");
                        List<VehiclePart> _PARTS_TO_DITCH = new List<VehiclePart>();
                        bool firstChassisEncountered = true;
                        for (int SLOT_INDEX = 0; SLOT_INDEX < REG.lineLength; SLOT_INDEX++)
                        {
                            VehiclePart _PART = REG.storageLines[0].slots[SLOT_INDEX];
                            if (firstChassisEncountered && _VIABLE_CHASSIS.Contains(_PART as VehiclePart_CHASSIS))
                            {
                                firstChassisEncountered = false;
                            }
                            else
                            {
                                _PARTS_TO_DITCH.Add(_PART);
                            }
                        }

                        REG.parts_OUT = _PARTS_TO_DITCH.ToArray();
                        REG.sendingLineTo = L1;
                        REG.ChangeState(StorageState.FETCHING);
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

//                if (!_chassis.partsFitted.ContainsKey(_PAIR.Key))
//                {
//                    L1.RequestPart(new VehiclePartRequest(_PAIR.Key, REG, _PAIR.Value));
//                    REG.ChangeState(StorageState.WAITING_FOR_DELIVERY);
//                    break;
//                }
//                else if (_chassis.partsFitted[_PAIR.Key] < _PAIR.Value)
//                {
//                    L1.RequestPart(
//                        new VehiclePartRequest(_PAIR.Key, REG, _PAIR.Value - _chassis.partsFitted[_PAIR.Key]));
//                    REG.ChangeState(StorageState.WAITING_FOR_DELIVERY);
//                    break;
//                }
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

        ratio_chassis_to_parts = 1 / (float) partCount;
    }
}