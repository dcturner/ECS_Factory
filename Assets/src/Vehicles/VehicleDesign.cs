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
    public List<VehicleDesign_RequiredPart> requiredParts;
    public Dictionary<VehiclePart_Config, int> quantities;

    private void OnValidate()
    {
        quantities = new Dictionary<VehiclePart_Config, int>();
        if (designPrefab != null)
        {
            designName = designPrefab.name;
            requiredParts = new List<VehicleDesign_RequiredPart>();
            foreach (VehiclePart _PART in designPrefab.GetComponentsInChildren<VehiclePart>())
            {
                VehiclePart_Config _CONFIG = _PART.partConfig;
                VehicleDesign_RequiredPart _temp = new VehicleDesign_RequiredPart(_PART.name, _CONFIG,
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
        foreach (VehicleDesign_RequiredPart _PART in requiredParts)
        {
            _STR += _PART.partConfig + " |";
        }

        return _STR;
    }
}