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
        if (currentTask!=null)
        {
        PerformTask();
        }
    }

    // Perform the task as many times as possible
    void PerformTask()
    {
        VehiclePart_CHASSIS requiredChassis = currentTask.design.chassisType;

        // Does REG have a viable Chassis?
        int _REG_CHASSIS = REG.FindLineContainingChassis(requiredChassis.partConfig.partVersion, currentTask.requiredParts);
        List<VehiclePart_CHASSIS> _VIABLE_CHASSIS =
            REG.GetViableChassis(requiredChassis.partConfig.partVersion, currentTask.requiredParts);

        if (_VIABLE_CHASSIS.Count >0)
        {
            // which required parts do I have?
            List<VehiclePart> _viableParts = new List<VehiclePart>();
            List<VehiclePart_Config> _TASK_PARTS = currentTask.requiredParts.Keys.ToList();
            foreach (VehiclePart _PART in REG.storageLines[0].slots)
            {
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

            // Can I perform the assigned task?
            // do I have any required parts AND a suitable chassis? - if so, DO IT

            // if not, order the parts for the task
        }
        else
        {
            // no viable CHASSIS - request some
            L1.RequestChassis(new VehicleChassiRequest(requiredChassis.partConfig,
                requiredChassis.partConfig.partVersion, currentTask.requiredParts, REG));
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
                REG.ChangeState(StorageState.WAITING_FOR_DELIVERY);
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