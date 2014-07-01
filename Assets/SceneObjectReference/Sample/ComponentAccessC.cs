using UnityEngine;
using System.Collections;

public class ComponentAccessC : MonoBehaviour {

	public SerializedClass serializeClass;
	
	[System.Serializable]
	public class SerializedClass
	{
		public ComponentAccessB accessb;
		public GameObject[] objects;
	}
	
}
