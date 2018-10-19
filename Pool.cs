namespace PofyTools.Pool
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    public class Pool<T> : IPool<T> where T : new()
    {
        private Stack<T> _stack = null;

        #region Constructor

        public Pool()
        {
            this._stack = new Stack<T>();
        }

        public Pool(int capacity)
        {
            this._stack = new Stack<T>(capacity);
        }

        #endregion

        public T Obtain()
        {
            if (this._stack.Count > 0)
            {
                return this._stack.Pop();
            }

            return new T();
        }

        public bool TryObtain(out T outValue)
        {
            outValue = default(T);
            if (this._stack.Count > 0)
            {
                outValue = this._stack.Pop();
                return true;
            }
            return false;
        }

        public void Free(T instance)
        {
            if (instance != null)
            {
                this._stack.Push(instance);
            }
        }

        public void Release()
        {
            this._stack.Clear();
        }
    }

    /// <summary>
    /// Pool for components of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
	public class ComponentPool<T> : IPool<T>, IList<T> where T : Component
    {
        public static int DEFAULT_INSTANCE_COUNT = 32;

        T _resource = null;
        private List<T> _componentList = null;
        private int _head = -1;
        public int Head
        {
            get
            {
                return _head;
            }
        }

        private bool _trackActiveComponent;

        private List<T> _activeInstances = null;

        /// <summary>
        /// Constructs an object pool with default parameters
        /// </summary>
        /// <param name="resource"></param>
        public ComponentPool(T resource) : this(resource, -1, true)
        {
        }

        /// <summary>
        /// Constructs an object pool with parameters
        /// </summary>
        /// <param name="resource">component to use as blueprint</param>
        /// <param name="count">number of prewarmed instances</param>
        /// <param name="trackActiveComponents">should active components be tracked.</param>
		public ComponentPool(T resource, int count, bool trackActiveComponents = true)
        {
            this._trackActiveComponent = trackActiveComponents;

            if (this._trackActiveComponent)
                this._activeInstances = new List<T>(Mathf.Max(count, 0));

            if (count < 0)
                count = DEFAULT_INSTANCE_COUNT;

            this._resource = resource;
            this._componentList = new List<T>(count);

            if (count > 0)
            {
                while (count > 0)
                {

                    this._componentList.Add(InstantiateNewComponent());

                    ++this._head;
                    --count;
                }
            }
            else
            {
                this._componentList.Add(null);
                this._head = -1;
            }
        }

        //EX BUFFER
        private T InstantiateNewComponent()
        {
            T newInstance = GameObject.Instantiate<T>(this._resource);

            newInstance.name = this._resource.name;
            newInstance.gameObject.SetActive(false);

            IPoolableComponent<T> poolable = newInstance as IPoolableComponent<T>;
            if (poolable != null)
            {
                poolable.Pool = this;
            }

            return newInstance;
        }

        public void Free(T component)
        {
            //deactivate and unparent component's game object
            component.gameObject.SetActive(false);
            component.transform.SetParent(null);

            ++this._head;

            //extend the descriptor list if limit is reached
            if (this._head == this._componentList.Count)
            {
                Debug.LogWarningFormat("POOL: Expanding Pool for {0}. Pool size is now: {1}.", component.name, (this._head + 1));
                this._componentList.Add(component);
            }
            else
            {
                this._componentList[this._head] = component;
            }

            if (this._trackActiveComponent)
                this._activeInstances.Remove(component);
        }

        public void FreeAll()
        {
            if (this._trackActiveComponent)
            {
                int count = this._activeInstances.Count;
                for (int i = count - 1; i >= 0; --i)
                    Free(this._activeInstances[i]);
            }
        }

        /// <summary>
        /// Obtain an instance from pool.
        /// </summary>
        /// <returns></returns>
		public T Obtain()
        {
            T instance = null;

            //if head is at zero we create an immidiatlly return created instance
            if (this._head < 0)
            {
                //Debug.LogWarningFormat ("POOL: No instancies available for {0}! All {1} preloaded instances in use. Instantiating new one...", this._resource.name, this.descriptorList.Count);
                this._head = 0;
                this._componentList[this._head] = InstantiateNewComponent();
            }

            instance = this._componentList[this._head];
            --this._head;

            if (this._trackActiveComponent)
                this._activeInstances.Add(instance);

            return instance;
        }

        /// <summary>
        /// Releases all instances and GC them.
        /// </summary>
        /// <param name="destroyActiveComponents"></param>
		public void Release(bool destroyActiveComponents)
        {
            int count = 0;
            if (destroyActiveComponents && this._trackActiveComponent)
            {

                count = this._activeInstances.Count;
                for (int i = count - 1; i >= 0; --i)
                    GameObject.Destroy(this._activeInstances[i].gameObject);

                this._activeInstances.Clear();

            }

            if (this._trackActiveComponent)
                this._activeInstances.Clear();

            count = this._componentList.Count;

            for (int i = count - 1; i >= 0; --i)
            {
                T component = this._componentList[i];
                if (component != null)
                {
                    GameObject.Destroy(component.gameObject);
                }
                if (i > 0)
                    this._componentList.RemoveAt(i);
            }
            this._head = -1;

            System.GC.Collect();
        }

        public void Release()
        {
            Release(false);
        }

        #region IList implementation

        public int IndexOf(T item)
        {
            return this._activeInstances.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this._activeInstances.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            this._activeInstances.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return this._activeInstances[index];
            }
            set
            {
                this._activeInstances[index] = value;
            }
        }

        #endregion

        #region ICollection implementation

        public void Add(T item)
        {
            this._activeInstances.Add(item);
        }

        public void Clear()
        {
            this._activeInstances.Clear();
        }

        public bool Contains(T item)
        {
            return this._activeInstances.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this._activeInstances.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return this._activeInstances.Remove(item);
        }

        public int Count
        {
            get
            {
                return this._activeInstances.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region IEnumerable implementation

        public IEnumerator<T> GetEnumerator()
        {
            return this._activeInstances.GetEnumerator();
        }

        #endregion

        #region IEnumerable implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._activeInstances.GetEnumerator();
        }

        #endregion
    }
}