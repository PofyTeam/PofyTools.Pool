namespace PofyTools.Pool
{
    using UnityEngine;
    using System.Collections;

    public interface IIdentifiable
    {
        string Id
        {
            get;
        }
    }

    public interface IPoolableComponent<T>  where T : Component
    {
        ComponentPool<T> Pool
        {
            get;
            set;
        }

        bool IsActive { get; }
        void Free();
        void ResetFromPool();
    }

    public interface IPool<T>
    {
        T Obtain();

        void Free(T instance);

        void Release();
    }
}