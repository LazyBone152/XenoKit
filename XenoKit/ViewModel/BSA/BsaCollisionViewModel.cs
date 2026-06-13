using GalaSoft.MvvmLight;
using System;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BSA
{
    public class BsaCollisionViewModel : ObservableObject, IDisposable
    {
        private readonly BSA_Collision collision;

        public EepkType EepkType
        {
            get => collision.EepkType;
            set => SetValue(nameof(collision.EepkType), collision.EepkType, value, "BSA Collision EEPK Type");
        }

        public ushort SkillID
        {
            get => collision.SkillID;
            set => SetValue(nameof(collision.SkillID), collision.SkillID, value, "BSA Collision Skill ID");
        }

        public uint EffectID
        {
            get => collision.EffectID;
            set => SetValue(nameof(collision.EffectID), collision.EffectID, value, "BSA Collision Effect ID");
        }

        public int I_08
        {
            get => collision.I_08;
            set => SetValue(nameof(collision.I_08), collision.I_08, value, "BSA Collision I_08");
        }

        public int I_12
        {
            get => collision.I_12;
            set => SetValue(nameof(collision.I_12), collision.I_12, value, "BSA Collision I_12");
        }

        public int I_16
        {
            get => collision.I_16;
            set => SetValue(nameof(collision.I_16), collision.I_16, value, "BSA Collision I_16");
        }

        public int I_20
        {
            get => collision.I_20;
            set => SetValue(nameof(collision.I_20), collision.I_20, value, "BSA Collision I_20");
        }

        public BsaCollisionViewModel(BSA_Collision collision)
        {
            this.collision = collision;
            UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
        }

        private void UndoManager_UndoOrRedoCalled(object sender, EventArgs e)
        {
            RaisePropertyChanged(string.Empty);
        }

        private void SetValue<T>(string propertyName, T oldValue, T newValue, string undoName)
        {
            if (Equals(oldValue, newValue)) return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propertyName, collision, oldValue, newValue, undoName));
            collision.GetType().GetProperty(propertyName).SetValue(collision, newValue, null);
            RaisePropertyChanged(string.Empty);
        }
    }
}
