using Xv2CoreLib.BAC;

namespace XenoKit.Engine.Scripting.BAC.Simulation
{
    public class BacSimulationObject : Entity
    {
        private readonly bool CanPersist;
        protected readonly IBacType BacType;
        protected readonly BacEntryInstance ParentBacInstance;

        public BacSimulationObject(IBacType bacType, BacEntryInstance bacEntryInstance, bool canPersist, GameBase gameBase) : base(gameBase)
        {
            CanPersist = canPersist;
            BacType = bacType;
            ParentBacInstance = bacEntryInstance;
            ParentBacInstance.ActionStoppedEvent += ParentBacInstance_ActionStoppedEvent;
            ParentBacInstance.SimulationEntities.Add(this);
        }

        protected bool IsValidForCurrentFrame()
        {
            //Since SimulationObjects will only be created when StartTime has been reached, that condition doesn't need to be checked here.
            return BacType.StartTime + BacType.Duration > ParentBacInstance.CurrentFrame && ParentBacInstance.CurrentFrame >= BacType.StartTime && ((ParentBacInstance.InScope && !CanPersist) || CanPersist);
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

        private void ParentBacInstance_ActionStoppedEvent(object source, ActionStoppedEventArgs e)
        {
            ActionStoppedEvent(e.State);
        }

        protected virtual void ActionStoppedEvent(ActionSimulationState state)
        {

        }

        public override void Dispose()
        {
            ParentBacInstance.ActionStoppedEvent -= ParentBacInstance_ActionStoppedEvent;
            _ = ParentBacInstance.SimulationEntities.Remove(this);
        }

    }
}
