using System.Collections.Generic;

namespace XenoKit.Engine.Pool
{
    public class PoolInstance<T> where T : PooledEntity, new()
    {
        private GameBase GameBase;

        private readonly int PoolSize;
        private readonly List<T> InUse = new List<T>();
        private readonly List<T> Available = new List<T>();

        public int CurrentSize => InUse.Count + Available.Count;

        public PoolInstance(int poolSize, GameBase game)
        {
            PoolSize = poolSize;
            GameBase = game;
        }

        public T GetObject()
        {
            lock (InUse)
            {
                if (Available.Count > 0)
                {
                    T _object = Available[0];
                    Available.RemoveAt(0);
                    InUse.Add(_object);

                    return _object;
                }
                else
                {
                    T _object = new T();
                    _object.SetGameBaseInstance(GameBase);

                    if (InUse.Count + Available.Count < PoolSize)
                        InUse.Add(_object);

                    return _object;
                }
            }
        }

        public void ReleaseObject(T _object)
        {
            _object.ClearObjectState();

            lock (InUse)
            {
                if (InUse.Contains(_object))
                {
                    InUse.Remove(_object);
                    Available.Add(_object);
                }
            }
        }

        public void DelayedUpdate()
        {
            //Reclaim dead objects that were not properly released
            //Method will be run on the delayed update cycle (1 second)

            lock (InUse)
            {
                for (int i = InUse.Count - 1; i >= 0; i--)
                {
                    if (!InUse[i].IsAlive)
                    {
                        T _obj = InUse[i];
                        _obj.ClearObjectState();
                        InUse.RemoveAt(i);
                        Available.Add(_obj);
                    }
                }
            }
        }
    }

    public abstract class PooledEntity : Entity
    {
        public virtual bool IsAlive => true;

        public PooledEntity() { }

        public abstract void ClearObjectState();
    }

}
