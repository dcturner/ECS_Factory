using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Workshop : MonoBehaviour
{

	public int workshopIndex;
	public float width, height;
	public Storage L2, L1, REG;

	private List<WorkshopTask> tasklist;
	private WorkshopTask currentTask;

	private void Awake()
	{
		tasklist = new List<WorkshopTask>();
	}

	public void AddTask(VehicleDesign _design, VehiclePart_Config[] _vehicleParts, int _iterations, bool _appendOnCompletion)
	{
		tasklist.Add(new WorkshopTask(_design, _vehicleParts, _iterations, _appendOnCompletion));
		Debug.Log("workshop_" + workshopIndex + ", added: " + tasklist[tasklist.Count-1].Log());
		currentTask = tasklist[0];
	}

	private void OnDrawGizmos()
	{
		if (L2 && L1 && REG)
		{
			GizmoHelpers.DrawRect(Color.cyan, transform.position, width, height, "CORE [" + workshopIndex + "]");
		}
	}

	private void NextTask()
	{
		
	}

	private void Tick()
	{
		if (tasklist.Count > 0)
		{
			
		}
	}

	private void RequestPart()
	{
	}
}

public struct WorkshopTask
{
	public VehicleDesign design;
	public VehiclePart_Config[] parts;
	public bool appendOnCompletion;
	public int iterations;

	public WorkshopTask(VehicleDesign _design, VehiclePart_Config[] _parts, int _iterations, bool _appendOnCompletion)
	{
		design = _design;
		parts = _parts;
		iterations = _iterations;
		appendOnCompletion = _appendOnCompletion;
	}

	public string Log()
	{
		string _PART_LIST = "";
		foreach (VehiclePart_Config _PART in parts)
		{
			_PART_LIST += _PART.name + " | ";
		}
		return design.designName + ": " + iterations + " x  >>> " + _PART_LIST;
	}
}
