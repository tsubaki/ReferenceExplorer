using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceToPlayer : MonoBehaviour
{
	private GameObject player = null;

	[SerializeField]
	private UnityEngine.UI.Text label = null;

	void Start ()
	{
		player = GameObject.FindWithTag ("Player");
	}

	void Update ()
	{
		label.text = string.Format("distance to player : {0:F3}", Vector3.Distance (player.transform.localPosition, transform.localPosition));
	}
}