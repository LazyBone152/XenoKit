using Xv2CoreLib.BAC;

namespace XenoKit.Engine.Scripting.BAC.Simulation
{
    public class BacVisualCueObject : Entity
    {
        protected readonly IBacType BacType;
        protected readonly BacEntryInstance ParentBacInstance;

        public BacVisualCueObject(IBacType bacType, BacEntryInstance bacEntryInstance, GameBase gameBase) : base(gameBase)
        {
            BacType = bacType;
            ParentBacInstance = bacEntryInstance;
            ParentBacInstance.VisualSimulationCues.Add(this);
        }

        protected bool IsValidForCurrentFrame()
        {
            //Since BacVisualCueObject will only be created when StartTime has been reached, that condition doesn't need to be checked here.
            return BacType.StartTime + BacType.Duration > ParentBacInstance.CurrentFrame && ParentBacInstance.CurrentFrame >= BacType.StartTime;
        }

        public virtual void Seek(int frame)
        {

        }

        public virtual void Play()
        {

        }

        public virtual void Stop()
        {

        }


        protected virtual bool IsContextValid()
        {
            return true;
        }

        public override void Dispose()
        {
        }

    }
}
