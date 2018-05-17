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
            // check if REG has the required pieces to perform the task
        }
    }

    // Perform the task as many times as possible
    void PerformTask()
    {
        VehiclePart_CHASSIS requiredChassis = currentTask.design.chassisType;
        
        // Do I have a suitable chassis?
        List<VehiclePart_CHASSIS> viableChassis =
            REG.FindChassis(requiredChassis.partConfig.partVersion, currentTask.requiredParts);
        
        if (viableChassis.Count > 0)
        {
            Debug.Log(workshopIndex  + " chassis: " + viableChassis.Count);
            // which required parts do I have?
            List<VehiclePart> viableParts = new List<VehiclePart>();
            foreach (VehiclePart _PART in REG.contents)
            {
                foreach (VehiclePart_Config _REQUIRED in currentTask.requiredParts.Keys)
                {
                    if (_PART.partConfig == _REQUIRED)
                    {
                        viableParts.Add(_PART);
                    }
                }
            }

            if (viableParts.Count > 0)
            {
                Debug.Log(workshopIndex  + " viableParts: " + viableParts.Count);
            }
            else
            {
                Debug.Log(workshopIndex + " needs PARTS");;
            }

            // Can I perform the assigned task?
            // do I have any required parts AND a suitable chassis? - if so, DO IT

            // if not, order the parts for the task
        }
        else
        {
//            Debug.Log(workshopIndex  + " needs CHASSIS");
// no viable CHASSIS - request some
            L1.RequestChassis(new VehicleChassiRequest(requiredChassis.partConfig, requiredChassis.partConfig.partVersion, currentTask.requiredParts, REG, REG.capacity));
        }
    }
}

public class WorkshopTask
{
    public VehicleDesign design;
    public Dictionary<VehiclePart_Config, int> requiredParts;

    public WorkshopTask(VehicleDesign _design, Dictionary<VehiclePart_Config, int> _requiredParts)
    {
        design = _design;
        requiredParts = _requiredParts;
    }
}