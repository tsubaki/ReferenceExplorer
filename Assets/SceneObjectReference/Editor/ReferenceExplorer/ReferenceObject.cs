using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ReferenceExplorer
{
	public class ReferenceObject  
	{
		public object value;
		public Component referenceComponent;
		public string referenceMemberName;
	}

	public class ReferenceObjectItem
	{
		public System.Type componentType;
		public bool isDisplay = true;
	}
}