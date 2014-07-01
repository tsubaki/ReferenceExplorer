using UnityEngine;
using System.Collections;

public class ComponentAccessE : MonoBehaviour {

	Camera cam {get{return c;
		}
		set{
			c = value;
		}
	}
	Camera c;

	void Start()
	{
		cam = Camera.main;
	}

}
