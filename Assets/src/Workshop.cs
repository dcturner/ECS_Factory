using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Workshop : MonoBehaviour
{

	public int workshopIndex;
	public float width, height;
	public Storage L2, L1, REG;

	private void OnDrawGizmos()
	{
		if (L2 && L1 && REG)
		{
			GizmoHelpers.DrawRect(Color.cyan, transform.position, width, height, "CORE [" + workshopIndex + "]");
		}
	}
}
