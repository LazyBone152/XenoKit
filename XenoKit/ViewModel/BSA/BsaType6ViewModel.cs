using System;
using System.Collections.Generic;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType6ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type6 effect;
        private static readonly IReadOnlyCollection<string> TypedFields = new[]
        {
            nameof(BSA_Type6.EepkType),
            nameof(BSA_Type6.SkillID),
            nameof(BSA_Type6.EffectID),
            nameof(BSA_Type6.I_08),
            nameof(BSA_Type6.F_12),
            nameof(BSA_Type6.F_16),
            nameof(BSA_Type6.F_20)
        };

        protected override IReadOnlyCollection<string> TypedFieldNames => TypedFields;

        public Array EepkTypes => Enum.GetValues(typeof(EepkType));
        public Array Switches => Enum.GetValues(typeof(Switch));

        public EepkType EepkType
        {
            get => effect.EepkType;
            set => SetEffectValue(nameof(effect.EepkType), effect.EepkType, value, "BSA Effect EEPK Type");
        }

        public ushort SkillID
        {
            get => effect.SkillID;
            set => SetEffectValue(nameof(effect.SkillID), effect.SkillID, value, "BSA Effect Skill ID");
        }

        public ushort EffectID
        {
            get => effect.EffectID;
            set => SetEffectValue(nameof(effect.EffectID), effect.EffectID, value, "BSA Effect ID");
        }

        public Switch Switch
        {
            get => effect.I_08;
            set => SetEffectValue(nameof(effect.I_08), effect.I_08, value, "BSA Effect Switch");
        }

        public float PositionX
        {
            get => effect.F_12;
            set => SetEffectValue(nameof(effect.F_12), effect.F_12, value, "BSA Effect Position X");
        }

        public float PositionY
        {
            get => effect.F_16;
            set => SetEffectValue(nameof(effect.F_16), effect.F_16, value, "BSA Effect Position Y");
        }

        public float PositionZ
        {
            get => effect.F_20;
            set => SetEffectValue(nameof(effect.F_20), effect.F_20, value, "BSA Effect Position Z");
        }

        public BsaType6ViewModel(BSA_Type6 type) : base(type)
        {
            effect = type;
        }

        private void SetEffectValue<T>(string propertyName, T oldValue, T newValue, string undoName)
        {
            if (Equals(oldValue, newValue))
                return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propertyName, effect, oldValue, newValue, undoName));
            effect.GetType().GetProperty(propertyName).SetValue(effect, newValue, null);
            RaisePropertyChanged(string.Empty);
            NotifyTypeChanged();
        }
    }
}
