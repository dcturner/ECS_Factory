using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Factory : MonoBehaviour
{
	private int tick = 0;
	public float tickRate = 0.2f;
	private float t;

	private Storage[] storage;
	private int storageCount;

	private void Awake()
	{
		t = tickRate;
		storage = FindObjectsOfType<Storage>();
		storageCount = storage.Length;
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
}
