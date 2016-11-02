namespace PofyTools.Pool
{
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>
	/// Poolable object descriptor.
	/// </summary>
	public class PoolableObjectDescriptor<T> where T: Component
	{
		T _component = null;

		public T component {
			get {
				return _component;
			}
			set {
				_component = value;
			}
		}

		public PoolableObjectDescriptor (T component)
		{
			this._component = component;
		}
	}
}