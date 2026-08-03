using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            set => SetValue(nameof(collision.EepkType), collision.EepkType, value, v => collision.EepkType = v, "BSA Collision EEPK Type");
        }

        public ushort SkillID
        {
            get => collision.SkillID;
            set => SetValue(nameof(collision.SkillID), collision.SkillID, value, v => collision.SkillID = v, "BSA Collision Skill ID");
        }

        // ushort, matching the model. Declaring this uint boxed the wrong type into the undo record.
        public ushort EffectID
        {
            get => collision.EffectID;
            set => SetValue(nameof(collision.EffectID), collision.EffectID, value, v => collision.EffectID = v, "BSA Collision Effect ID");
        }

        public ushort I_06 { get => collision.I_06; set => SetValue(nameof(collision.I_06), collision.I_06, value, v => collision.I_06 = v, "BSA Collision I_06"); }
        public int I_08 { get => collision.I_08; set => SetValue(nameof(collision.I_08), collision.I_08, value, v => collision.I_08 = v, "BSA Collision I_08"); }
        public int I_12 { get => collision.I_12; set => SetValue(nameof(collision.I_12), collision.I_12, value, v => collision.I_12 = v, "BSA Collision I_12"); }
        public int I_16 { get => collision.I_16; set => SetValue(nameof(collision.I_16), collision.I_16, value, v => collision.I_16 = v, "BSA Collision I_16"); }
        public int I_20 { get => collision.I_20; set => SetValue(nameof(collision.I_20), collision.I_20, value, v => collision.I_20 = v, "BSA Collision I_20"); }

        public BsaCollisionViewModel(BSA_Collision collision)
        {
            this.collision = collision;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
        }

        private void UndoManager_UndoOrRedoCalled(object sender, EventArgs e)
        {
            RaisePropertyChanged(() => EepkType);
            RaisePropertyChanged(() => SkillID);
            RaisePropertyChanged(() => EffectID);
            RaisePropertyChanged(() => I_06);
            RaisePropertyChanged(() => I_08);
            RaisePropertyChanged(() => I_12);
            RaisePropertyChanged(() => I_16);
            RaisePropertyChanged(() => I_20);
        }

        private void SetValue<TValue>(string modelProperty, TValue oldValue, TValue newValue, Action<TValue> assign, string undoName, [CallerMemberName] string viewModelProperty = null)
        {
            if (EqualityComparer<TValue>.Default.Equals(oldValue, newValue)) return;

            UndoManager.Instance.AddUndo(new UndoableProperty<BSA_Collision>(modelProperty, collision, oldValue, newValue, undoName));
            assign(newValue);
            RaisePropertyChanged(viewModelProperty);
        }
    }
}
