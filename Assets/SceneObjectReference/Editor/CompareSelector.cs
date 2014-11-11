using System;
using System.Collections.Generic;

namespace ReferenceViewer
{
	public class CompareSelector<T, TKey> : IEqualityComparer<T>
	{
		private Func<T, TKey> selector;
		
		public CompareSelector(Func<T, TKey> selector)
		{
			this.selector = selector;
		}
		
		public bool Equals(T x, T y)
		{
			return selector(x).Equals(selector(y));
		}
		
		public int GetHashCode(T obj)
		{
			return selector(obj).GetHashCode();
		}
	}
}