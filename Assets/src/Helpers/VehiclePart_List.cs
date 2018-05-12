using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "VehicleParts", menuName = "Factory/PartList")]
public class VehiclePart_List : ScriptableObject
{   
    [AssetList, InlineEditor(PreviewWidth = 50)]
    public VehiclePart_Config parts;
}
