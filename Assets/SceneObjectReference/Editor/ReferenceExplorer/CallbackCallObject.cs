using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ReferenceExplorer
{
	public class CallbackCallObject  {
		public string method;
		public List<Component> callComponent = new List<Component> ();
		public bool isOpen = true;
	}

	public class ANimationCallbackObject : CallbackCallObject
	{
		public AnimationClip clip;
	}

	public class CallbackObject
	{
		public string method;
		public Type callComponenttype;
	}
}
