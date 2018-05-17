using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehiclePart_CHASSIS : VehiclePart
{
    public Vehicle_ChassisType chassisType;
    public int totalpartsFitted, tempCriteriaMet;
    public Dictionary<VehiclePart_Config, int> partsFitted;

    private void Awake()
    {
        totalpartsFitted = 0;
        tempCriteriaMet = 0;
        partsFitted = new Dictionary<VehiclePart_Config, int>();
    }
}

public enum Vehicle_ChassisType
{
    CAR_2_DOOR,
    CAR_4_DOOR,
    BIKE,
    TRUCK,
    VAN
}