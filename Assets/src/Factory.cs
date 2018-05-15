using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Drawers;
using UnityEngine;

public class Factory : SerializedMonoBehaviour
{
	public static Factory INSTANCE;
	private int tick = 0;
	[PropertyRange(0.0000001f, 1f)]
	public float tickRate = 0.1f;
	private float t;
	
	public Storage HD, RAM, L3;
	private Storage[] storage;
	private int storageCount;
	
	// ORDER
	// Quantity of each design to be built
	public Dictionary<VehicleDesign, int> vehicleOrder;
	public Dictionary<VehicleDesign, int> CompletedVehicles;
	private Dictionary<VehiclePart_Config, int> requiredParts;
	private Dictionary<VehiclePart_Config, List<VehiclePart>> parts;

	private void Awake()
	{
		INSTANCE = this;
		t = tickRate;
		storage = FindObjectsOfType<Storage>();
		storageCount = storage.Length;
		requiredParts = new Dictionary<VehiclePart_Config, int>();
		parts = new Dictionary<VehiclePart_Config, List<VehiclePart>>();
		CompletedVehicles = new Dictionary<VehicleDesign, int>();
	}

	private void Start()
	{
		// get total number of parts required
		foreach (KeyValuePair<VehicleDesign,int> _DESIGN_PAIR in vehicleOrder)
		{
			VehicleDesign _DESIGN = _DESIGN_PAIR.Key;
			int _TOTAL = _DESIGN_PAIR.Value;
			
			// Set number of completed vehicles to zero
			CompletedVehicles[_DESIGN] = 0;
			
			Debug.Log(_DESIGN.designName +" -- " + _TOTAL);
			foreach (KeyValuePair<VehiclePart_Config,int> _PartCount in _DESIGN.quantities)
			{
				VehiclePart_Config _partType = _PartCount.Key;
				if (requiredParts.ContainsKey(_partType))
				{
					requiredParts[_partType]+= _DESIGN.quantities[_partType] * _TOTAL;
				}
				else
				{
					requiredParts.Add(_partType, _DESIGN.quantities[_partType] * _TOTAL);
				}
			}
		}
		
		// summarise order and sling it into RAM
		Debug.Log("Part totals...");
		int index = 0;
		foreach (KeyValuePair<VehiclePart_Config,int> _PAIR in requiredParts)
		{
			VehiclePart_Config _PART = _PAIR.Key;
			int _TOTAL = _PAIR.Value;
			GameObject _PART_PREFAB = _PART.prefab_part;
			parts.Add(_PART, new List<VehiclePart>());
			List<VehiclePart> _LIST = parts[_PART];
			Debug.Log(_PART.name + " x " + _TOTAL);

			
			for (int i = 0; i < _TOTAL; i++)
			{
				GameObject _part_OBJ = (GameObject) Instantiate(_PART_PREFAB, new Vector3(i*0.05f, 0f, 0f), Quaternion.identity);
				_LIST.Add( _part_OBJ.GetComponent<VehiclePart>());
				if (i < RAM.capacity)
				{
					_part_OBJ.transform.position = RAM.storageLocations[index];
					index++;
				}
			}
		}
	}

	private void Update()
	{
		if(Timer.TimerReachedZero(ref t))
		{
			Tick();
		}
	}

	private void Tick()
	{
		t = tickRate;
		tick++;

		for (int i = 0; i < storageCount; i++)
		{
			storage[i].Tick();
		}
	}
	
	#region ------------------------- < GIZMOS METHODS

	[Button("Toggle Storage Cell Gizmos")]
	public void GIZMOS_storage_showCells()
	{
		Storage.GIZMOS_DRAW_CELLS = !Storage.GIZMOS_DRAW_CELLS;
	}

	#endregion ------------------------ GIZMOS METHODS >

}
