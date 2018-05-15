using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Workshop : MonoBehaviour
{

	public int workshopIndex;
	public float width, height;
	public Storage L2, L1, REG;

	private List<WorkshopTask> tasklist;

	private void Awake()
	{
		tasklist = new List<WorkshopTask>();
	}

	public void AddTask(VehicleDesign _design, VehiclePart_Config _vehiclePart, int _iterations, bool _appendOnCompletion)
	{
		List<VehicleDesign_RequiredPart> _partList = new List<VehicleDesign_RequiredPart>();
		foreach (VehicleDesign_RequiredPart _PART in _design.requiredParts)
		{
			if (_PART.partConfig == _vehiclePart)
			{
				_partList.Add(_PART);
			}
		}
		tasklist.Add(new WorkshopTask(_design, _partList.ToArray(),_iterations, _appendOnCompletion));
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
	}

	private void RequestPart()
	{
	}
}

public struct WorkshopTask
{
	public VehicleDesign design;
	public VehicleDesign_RequiredPart[] requiredParts;
	public bool appendOnCompletion;
	public int iterations;

	public WorkshopTask(VehicleDesign _design, VehicleDesign_RequiredPart[] _requiredParts, int _iterations, bool _appendOnCompletion)
	{
		design = _design;
		requiredParts = _requiredParts;
		iterations = _iterations;
		appendOnCompletion = _appendOnCompletion;
	}
}
