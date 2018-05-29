using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehiclePart : MonoBehaviour
{

	public VehiclePart_Config partConfig;

	public int age;
	public int id;
    public int temp_score;
}


public enum Vehicle_PartType
{
	CHASSIS,
	ENGINE,
	WHEEL,
	STEERING,
	SEAT,
	DOOR,
	GLASS,
	EXHAUST
}

// A group of these make up a VehicleDesign - (4 wheels, one engine, 2 seats <-- 7 components)
public class VehiclePart_Assignment
{
	public string name;
	public VehiclePart_Config partConfig;
	public Vector3 position;
	public Quaternion rotation;

	public VehiclePart_Assignment (string _name, VehiclePart_Config _partConfig, Vector3 _position, Quaternion _rotation)
	{
		name = _name;
		partConfig = _partConfig;
		position = _position;
		rotation = _rotation;
	}
}