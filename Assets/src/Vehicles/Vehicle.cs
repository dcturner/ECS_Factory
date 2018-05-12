using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MonoBehaviour {

	public VehicleDesign vehicleDesign;
	public VehiclePart chassis;
	public List<VehiclePart_Config> parts;
	public int age, id;

	public void AttachPart(VehiclePart_Config _newPartConfig, VehicleDesign_RequiredPart _requiredPart,
		int _index)
	{
		parts[_index] = _newPartConfig;
		Debug.Log(ToString() + " ++ " + _newPartConfig.partType + "  (" + parts.Count + "/" +
		          vehicleDesign.requiredParts.Count + ")");
	}

	public string Log()
	{
		return vehicleDesign.designName + "__" + id + " --> ";
	}
}
