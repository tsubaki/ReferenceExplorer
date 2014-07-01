using UnityEngine;
using System.Collections;

public class ComponentAccessD : MonoBehaviour {

	Hoge h = new Hoge();

	// Use this for initialization
	void Start () {
		h.p = GameObject.FindObjectOfType<ParticleSystem>();
	}
	
	public class Hoge
	{
		public ParticleSystem p;
	}
}
