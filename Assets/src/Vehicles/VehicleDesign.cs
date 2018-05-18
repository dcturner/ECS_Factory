using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteInEditMode]
[CreateAssetMenu(fileName = "VehicleDesign_X", menuName = "Factory/Vehicle Design")]
public class VehicleDesign : ScriptableObject
{
    [InlineEditor(InlineEditorModes.LargePreview), Required]
    public GameObject designPrefab;

    public string designName;
    public Color chassisColour;
    public VehiclePart_CHASSIS chassisType;
    public List<VehiclePart_Assignment> requiredParts;
    public Dictionary<VehiclePart_Config, int> quantities;

    private void OnValidate()
    {
        quantities = new Dictionary<VehiclePart_Config, int>();
        if (designPrefab != null)
        {
            designName = designPrefab.name;
            requiredParts = new List<VehiclePart_Assignment>();
            foreach (VehiclePart _PART in designPrefab.GetComponentsInChildren<VehiclePart>())
            {
                VehiclePart_Config _CONFIG = _PART.partConfig;
                VehiclePart_Assignment _temp = new VehiclePart_Assignment(_PART.name, _CONFIG,
                    _PART.transform.localPosition, _PART.transform.localRotation);
                requiredParts.Add(_temp);
                if (quantities.ContainsKey(_CONFIG))
                {
                    quantities[_CONFIG]++;
                }
                else
                {
                    quantities.Add(_CONFIG, 1);
                }
            }
        }
    }

    public string Log()
    {
        string _STR = designName + " (): ";
        foreach (VehiclePart_Assignment _PART in requiredParts)
        {
            _STR += _PART.partConfig + " |";
        }

        return _STR;
    }
}