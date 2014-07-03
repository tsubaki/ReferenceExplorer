using UnityEngine;
using System.Collections;

public class ComponnetAccessF : MonoBehaviour {

	public Hoge h{get; set;}

	public void Start()
	{
		h = new Hoge();
		h.oh = Camera.main.gameObject;
	}

	[System.Serializable]
	public class Hoge
	{
		public GameObject oh;
	}
}
