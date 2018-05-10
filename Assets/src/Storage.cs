using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Storage : MonoBehaviour
{
	public string name;
	public Color colour;
	public int capacity, usedSpace, freeSpace;
	[HideInInspector]
	public StorageState currentState;
	public int taskDuration;
	public int taskStep;
	public float factor;
	[HideInInspector]
	public int[] contents;

	private bool actionStep = false;

	private Transform t;

	private void Awake()
	{
		t = transform;
	}

	public void Init(string _name, int _capacity, int _taskDuration)
	{
		name = _name;
		capacity = _capacity;
		usedSpace = 0;
		freeSpace = capacity;
		currentState = StorageState.IDLE;
		taskDuration = _taskDuration;
		contents = new int[capacity];
	}

	private string ToString()
	{
		return name + " \n(" + usedSpace + "/" + capacity + ") ";
	}

	private void ChangeState(StorageState _newState)
	{
		if (currentState != _newState)
		{
			currentState = _newState;
			Debug.Log(ToString() + currentState);

			switch (currentState)
			{
				case StorageState.IDLE:
					break;
				case StorageState.STORE:
					break;
				case StorageState.FETCH:
					break;
			}
		}
	}


	public void Tick()
	{
		taskStep = (taskStep + 1) % taskDuration;
		factor = (float) taskStep / (float) taskDuration;
		
		switch (currentState)
		{
			case StorageState.IDLE:
				break;
			case StorageState.STORE:
				break;
			case StorageState.FETCH:
				break;
		}
	}

	private void OnDrawGizmos()
	{
		Vector3 _POS = transform.position;
		GizmoHelpers.DrawRect(colour, _POS, transform.localScale.x, transform.localScale.z, ToString());
		
		Vector3 _START = _POS + new Vector3(0f,0f,transform.localScale.z * factor);
		GizmoHelpers.DrawLine(Color.white, _START, _START + new Vector3(transform.localScale.x,0f,0f));
		
		// flash on action?
		if (taskStep == 0)
		{
			
		}
	}
}

public enum StorageState
{
	IDLE,
	STORE,
	FETCH
}
