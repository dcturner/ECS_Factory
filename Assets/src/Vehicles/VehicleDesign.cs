using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[CreateAssetMenu(fileName = "VehicleDesign_X", menuName = "Factory/Vehicle Design")]
public class VehicleDesign : ScriptableObject
{
	public GameObject designPrefab;
	public string designName;
	public Color chassisColour;
	public List<VehicleDesign_RequiredPart> requiredParts;

	private void OnValidate()
	{
		if (designPrefab != null)
		{
			designName = designPrefab.name;
			requiredParts = new List<VehicleDesign_RequiredPart>();
			Debug.Log(">> Updating vehicleDesign: " + designName);
			foreach (VehiclePart _PART in designPrefab.GetComponentsInChildren<VehiclePart>())
			{
				VehicleDesign_RequiredPart _temp = new VehicleDesign_RequiredPart(_PART.name, _PART.partConfig, _PART.transform.localPosition, _PART.transform.localRotation);
				requiredParts.Add(_temp);
				Debug.Log(requiredParts.Count + ", " + _temp.partConfig.name +", " + _temp.position + ", :" + _temp.rotation);
			}
		}
	}

	public string ToString()
	{
		string _STR = designName + " (): ";
		foreach (VehicleDesign_RequiredPart _PART in requiredParts)
		{
			_STR += _PART.partConfig + " |";
		}

		return _STR;
	}
}