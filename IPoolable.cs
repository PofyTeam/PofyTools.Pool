namespace PofyTools.Pool
{
	using UnityEngine;
	using System.Collections;

	public interface IIdentifiable
	{
		string id {
			get;
		}
	}

	public interface IPoolable<T> : IIdentifiable where T:Component
	{
        bool IsActive { get; }
		Pool<T> Pool {
			get;
			set;
		}

		void Free ();

		void ResetFromPool ();
	}
}