namespace PofyTools.Pool
{
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>
	/// Resource pool. Creates a dictionary with individual pools for each resource prefab
	/// </summary>
	public class ResourcePool<T>: IDictionary<string,Pool<T>> where T:Component
	{
		private Dictionary<string,Pool<T>> _pools = null;
		private string _resourcePath;

		public ResourcePool (string resourcePath) : this (resourcePath, -1, true)
		{

		}

		public ResourcePool (string resourcePath, int initialCount, bool trackActiveComponents = true)
		{
			this._resourcePath = resourcePath;
			this._pools = new Dictionary<string, Pool<T>> ();
			PreloadPools (initialCount, trackActiveComponents);
		}

		#region Add/Preload/Get Pool

		void PreloadPools (int initialCount, bool trackActiveComponents)
		{
			T[] resources = Resources.LoadAll<T> (this._resourcePath);
			foreach (var resource in resources) {
				string id = resource.name;

				IIdentifiable identifiable = resource as IIdentifiable;
				if (identifiable != null)
					id = identifiable.id;

				AddPool (resource, id, initialCount, trackActiveComponents);
			}
		}

		//Create pool for loaded resource
		public Pool<T> AddPool (T resource, string id, int count, bool trackActiveComponents = true)
		{
			Pool<T> pool = null;
			if (!this._pools.TryGetValue (id, out pool)) {
				pool = new Pool<T> (resource, count, trackActiveComponents);
				this._pools [id] = pool;
			}
			return pool;
		}

		//Load resource and create pool
		public Pool<T> AddPool (string id, int count = 0, bool trackActiveComponent = true)
		{
			T resource = Resources.Load<T> (this._resourcePath + id);

			return AddPool (resource, id, count, trackActiveComponent);
		}

		public Pool<T> GetPool (string key)
		{
			Pool<T> pool = null;
			this._pools.TryGetValue (key, out pool);
			return pool;
		}

		#endregion

		#region FREE

		public void FreeToPool (T component, string id)
		{
			this._pools [id].Free (component);
		}

		public void FreeToPool (T component)
		{
			string id = component.name;
			IIdentifiable identifiable = component as IIdentifiable;

			if (identifiable != null) {
				id = identifiable.id;
			}

			FreeToPool (component, id);
		}

		public void FreeAll ()
		{
			foreach (var pool in this._pools.Values) {
				pool.FreeAll ();
			}
		}

		public void ReleaseAll (bool destroyActiveComponents = false)
		{
			foreach (var pool in this._pools.Values) {
				pool.Release (destroyActiveComponents);
			}
		}

		#endregion

		#region Obtain

		public T ObtainFromPool (string id)
		{
			Pool<T> pool = null;
			if (!this._pools.TryGetValue (id, out pool))
				pool = AddPool (id, 1);

			return 	pool.Obtain ();
		}

		public T ObtainFromPool (T source)
		{
			string sourceId = source.name;
			IIdentifiable identifiable = source as IIdentifiable;

			if (identifiable != null) {
				sourceId = identifiable.id;
			}

			return ObtainFromPool (sourceId);
		}

		#endregion

		#region Overrides

		//		public override string ToString ()
		//		{
		//			string result = string.Format ("Resource Path: {0}, ID Count: {1}.\nIds and Count:", this._resourcePath, this._pools.Count);
		//
		//			foreach (var key in this._pools)
		//				result += string.Format ("\n{0} - {1}", key.Key, key.Value.Head);
		//
		//			return result;
		//		}

		#endregion


		#region IDictionary implementation

		public bool ContainsKey (string key)
		{
			return this._pools.ContainsKey (key);
		}

		public void Add (string key, Pool<T> value)
		{
			this._pools.Add (key, value);
		}

		public bool Remove (string key)
		{
			return this._pools.Remove (key);
		}

		public bool TryGetValue (string key, out Pool<T> value)
		{
			return this._pools.TryGetValue (key, out value);
		}

		public Pool<T> this [string index] {
			get {
				return this._pools [index];
			}
			set {
				this._pools [index] = value;
			}
		}

		public ICollection<string> Keys {
			get {
				return this._pools.Keys;
			}
		}

		public ICollection<Pool<T>> Values {
			get {
				return this._pools.Values;
			}
		}

		#endregion

		#region ICollection implementation

		public void Add (KeyValuePair<string, Pool<T>> item)
		{
			throw new System.NotImplementedException ();
		}

		public void Clear ()
		{
			this._pools.Clear ();
		}

		public bool Contains (KeyValuePair<string, Pool<T>> item)
		{
			throw new System.NotImplementedException ();
		}

		public void CopyTo (KeyValuePair<string, Pool<T>>[] array, int arrayIndex)
		{
			throw new System.NotImplementedException ();
		}

		public bool Remove (KeyValuePair<string, Pool<T>> item)
		{
			throw new System.NotImplementedException ();
		}

		public int Count {
			get {
				return this._pools.Count;
			}
		}

		public bool IsReadOnly {
			get {
				throw new System.NotImplementedException ();
			}
		}

		#endregion

		#region IEnumerable implementation

		public IEnumerator<KeyValuePair<string, Pool<T>>> GetEnumerator ()
		{
			return this._pools.GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return this._pools.GetEnumerator ();
		}

		#endregion
	}
}
