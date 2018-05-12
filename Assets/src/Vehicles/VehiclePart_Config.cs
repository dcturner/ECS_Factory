using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Drawers;

[CreateAssetMenu(fileName = "VehiclePart_X", menuName = "Factory/Vehicle Part")]
public class VehiclePart_Config : ScriptableObject
{
    
    [InlineEditor(InlineEditorModes.LargePreview), Required]
    public GameObject prefab_part;
    public Vehicle_PartType partType;
    public int partVersion;
    [PropertyRange(1,10)]
    public int size;
}