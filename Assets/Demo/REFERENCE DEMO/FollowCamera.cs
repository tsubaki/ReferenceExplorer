using UnityEngine;
using System.Collections;

public class FollowCamera : MonoBehaviour 
{
	Camera target = null;

	void Start()
	{
		target = Camera.main;

		StaticComponents.score += 1;
	}
}
