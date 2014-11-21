using UnityEngine;
using System.Collections;

public class StaticComponents : MonoBehaviour {

	public static int score;

	void OnGUI()
	{
		GetComponent<GUIText>().text = score.ToString();
	}
}
