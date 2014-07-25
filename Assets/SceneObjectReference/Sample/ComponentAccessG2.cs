using UnityEngine;
using System.Collections;

public class ComponentAccessG2 : MonoBehaviour
{
	
	public void Start ()
	{
		GameObject.FindObjectOfType<ComponentAccessG1>().act += Callback;
	}
	
	public void Callback ()
	{
	}
}
