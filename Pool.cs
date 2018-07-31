namespace PofyTools.Pool
{
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;

	public class Pool<T> : IList<T> where T:Component
	{

		public static int DEFAULT_INSTANCE_COUNT = 32;

		private PoolBuffer _buffer = null;

		private bool _trackActiveComponent;

		private List<T> _activeInstances = null;

		public Pool (T resource) : this (resource, -1, true)
		{
		}

		public Pool (T resource, int count, bool trackActiveComponents = true)
		{
			this._trackActiveComponent = trackActiveComponents;
			this._buffer = PreloadObject (resource, count);
		}

		public PoolBuffer PreloadObject (T resource, int count)
		{

			if (this._trackActiveComponent)
				this._activeInstances = new List<T> (Mathf.Max (count, 0));

			if (count < 0)
				count = DEFAULT_INSTANCE_COUNT;

			PoolBuffer buffer = new PoolBuffer (this, resource, count);

			return buffer;
		}

		public void Free (T component)
		{
			this._buffer.FreeToDescriptor (component);

			if (this._trackActiveComponent)
				this._activeInstances.Remove (component);
		}

		public void FreeAll ()
		{
			if (this._trackActiveComponent) {
				int count = this._activeInstances.Count;
				for (int i = count - 1; i >= 0; --i)
					Free (this._activeInstances [i]);
			}
		}

		public void DestroyActiveComponents ()
		{
			if (this._trackActiveComponent) {
				int count = this._activeInstances.Count;
				for (int i = count - 1; i >= 0; --i)
					GameObject.Destroy (this._activeInstances [i].gameObject);

				this._activeInstances.Clear ();
			}
		}

		public T Obtain ()
		{
			T instance = this._buffer.ObtainDescriptor ().component;

			if (this._trackActiveComponent)
				this._activeInstances.Add (instance);

			return instance;
		}

		public void Release (bool destroyActiveComponents = false)
		{
			if (destroyActiveComponents) {
				DestroyActiveComponents ();
			}
			if (this._trackActiveComponent)
				this._activeInstances.Clear ();

			this._buffer.Release ();
			System.GC.Collect ();
		}

		public class PoolBuffer
		{
			T _resource = null;
			private List<PoolableObjectDescriptor<T>> _descriptorList = null;
			private int _head = -1;
			private Pool<T> _pool;

			public int Head {
				get {
					return _head;
				}
			}

			public PoolBuffer (Pool<T>pool, T resource, int initialCount = 0)
			{
				this._pool = pool;
				this._resource = resource;
				this._descriptorList = new List<PoolableObjectDescriptor<T>> (initialCount);

				if (initialCount > 0) {
					while (initialCount > 0) {

						this._descriptorList.Add (NewPopulatedDescriptor ());

						++this._head;
						--initialCount;
					}
				} else {
					this._descriptorList.Add (new PoolableObjectDescriptor<T> (null));
					this._head = -1;
				}
			}

			public void Release ()
			{
				int count = this._descriptorList.Count;

				for (int i = count - 1; i >= 0; --i) {
					T component = this._descriptorList [i].component;
					if (component != null) {
						GameObject.Destroy (component.gameObject);
					}
					if (i > 0)
						this._descriptorList.RemoveAt (i);
				}
				this._head = -1;
			}

			private T InstantiateNewComponent ()
			{
				T newInstance = GameObject.Instantiate<T> (this._resource);

				newInstance.name = this._resource.name;
				newInstance.gameObject.SetActive (false);

				IPoolable<T> poolable = newInstance as IPoolable<T>;
				if (poolable != null) {
					poolable.Pool = this._pool;
				}

				return newInstance;
			}

			/// <summary>
			/// Creates a new Descriptor and populates it's component field
			/// </summary>
			/// <returns>The populated descriptor.</returns>
			private PoolableObjectDescriptor<T> NewPopulatedDescriptor ()
			{
				T newInstance = InstantiateNewComponent ();

				PoolableObjectDescriptor<T> descriptor = new PoolableObjectDescriptor<T> (newInstance);
				return descriptor;

			}

			/// <summary>
			/// Populates the descriptor.
			/// </summary>
			/// <returns>Populated descriptor.</returns>
			/// <param name="descriptor">Descriptor to be populated with the instance of the resource component.</param>
			private PoolableObjectDescriptor<T> PopulateDescriptor (PoolableObjectDescriptor<T> descriptor)
			{
				T newInstance = InstantiateNewComponent ();

				descriptor.component = newInstance;
				return descriptor;
			}

			/// <summary>
			/// Obtains Descriptor. You must not store returned Descriptor!
			/// </summary>
			public PoolableObjectDescriptor<T> ObtainDescriptor ()
			{
				PoolableObjectDescriptor<T> descriptor = null;

				//if head is at zero we create an immidiatlly return created instance
				if (this._head < 0) {
					//Debug.LogWarningFormat ("POOL: No instancies available for {0}! All {1} preloaded instances in use. Instantiating new one...", this._resource.name, this.descriptorList.Count);
					this._head = 0;
					PopulateDescriptor (this._descriptorList [this._head]);
				}

				descriptor = this._descriptorList [this._head];
				--this._head;

				return descriptor;
			}

			/// <summary>
			/// Frees component to available descriptor or expands the list.
			/// </summary>
			/// <param name="component">Component to be freed.</param>
			public void FreeToDescriptor (T component)
			{
				//deactivate and unparent component's game object
				component.gameObject.SetActive (false);

				//component.transform.parent = null;
				component.transform.SetParent (null);

				++this._head;

				//extend the descriptor list if limit is reached
				if (this._head == this._descriptorList.Count) {
					Debug.LogWarningFormat ("POOL: Expanding Pool for {0}. Pool size is now: {1}.", component.name, (this._head + 1));
					this._descriptorList.Add (new PoolableObjectDescriptor<T> (component));
				} else {
					this._descriptorList [this._head].component = component;
				}
			}
		}

		#region IList implementation

		public int IndexOf (T item)
		{
			return this._activeInstances.IndexOf (item);
		}

		public void Insert (int index, T item)
		{
			this._activeInstances.Insert (index, item);
		}

		public void RemoveAt (int index)
		{
			this._activeInstances.RemoveAt (index);
		}

		public T this [int index] {
			get {
				return this._activeInstances [index];
			}
			set {
				this._activeInstances [index] = value;
			}
		}

		#endregion

		#region ICollection implementation

		public void Add (T item)
		{
			this._activeInstances.Add (item);
		}

		public void Clear ()
		{
			this._activeInstances.Clear ();
		}

		public bool Contains (T item)
		{
			return this._activeInstances.Contains (item);
		}

		public void CopyTo (T[] array, int arrayIndex)
		{
			this._activeInstances.CopyTo (array, arrayIndex);
		}

		public bool Remove (T item)
		{
			return this._activeInstances.Remove (item);
		}

		public int Count {
			get {
				return this._activeInstances.Count;
			}
		}

		public bool IsReadOnly {
			get {
				return true;
			}
		}

		#endregion

		#region IEnumerable implementation

		public IEnumerator<T> GetEnumerator ()
		{
			return this._activeInstances.GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return this._activeInstances.GetEnumerator ();
		}

		#endregion
	}
}