using System.Collections.Generic;
using UnityEngine;

public class DistanceToPointComparer : IComparer<Collider>
{
	public Vector3 point;

	public int Compare(Collider a, Collider b)
	{
		return (point - b.transform.position).sqrMagnitude.CompareTo((point - a.transform.position).sqrMagnitude);
	}
}
