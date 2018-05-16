using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Workshop : MonoBehaviour
{

	public int workshopIndex;
	public float width, height;
	public Storage L2, L1, REG;

	private Queue<WorkshopTask> tasklist;
	private WorkshopTask currentTask;

	private void Awake()
	{
		tasklist = new Queue<WorkshopTask>();
	}

	public void AddTask(VehicleDesign _design, VehiclePart_Config[] _vehicleParts, int _iterations, bool _appendOnCompletion)
	{
		tasklist.Enqueue(new WorkshopTask(_design, _vehicleParts, _iterations, _appendOnCompletion));
	}

	private void OnDrawGizmos()
	{
		if (L2 && L1 && REG)
		{
			GizmoHelpers.DrawRect(Color.cyan, transform.position, width, height, "CORE [" + workshopIndex + "]");
		}
	}

	public void NextTask()
	{
		if (tasklist.Count > 0)
		{
			currentTask = tasklist.Dequeue();
			
		}
		else
		{
			Debug.Log("DONE");
		}
	}

	private void Tick()
	{
		if (tasklist.Count > 0)
		{
			// check if REG has the required pieces to perform the task
		}
	}

	private void StartOrder()
	{
	}

	private void Vote_PartsForSharedStorage(VehiclePart_Config[] _parts)
	{
		// Total number of parts per iteration of this task
		int partsPerTask = _parts.Length;	
	
		VehiclePart_Config[] _REQUEST = new VehiclePart_Config[Factory.SHARED_STORAGE_CORE_SHARE];	
		for (int i = 0; i < Factory.SHARED_STORAGE_CORE_SHARE; i++)
		{
			_REQUEST[i] = _parts[i % partsPerTask];
		}
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
