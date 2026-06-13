using System.Collections.Generic;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType0ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type0 passEntry;
        private static readonly IReadOnlyCollection<string> TypedFields = new[]
        {
            nameof(BSA_Type0.I_00),
            nameof(BSA_Type0.I_02),
            nameof(BSA_Type0.BSA_EntryID),
            nameof(BSA_Type0.I_06),
            nameof(BSA_Type0.F_08),
            nameof(BSA_Type0.F_12)
        };

        protected override IReadOnlyCollection<string> TypedFieldNames => TypedFields;

        public short I_00
        {
            get => passEntry.I_00;
            set => SetPassValue(nameof(passEntry.I_00), passEntry.I_00, value, "BSA Pass I_00");
        }

        public ushort MainCondition
        {
            get => passEntry.I_02;
            set => SetPassValue(nameof(passEntry.I_02), passEntry.I_02, value, "BSA Pass Main Condition");
        }

        public ushort BsaEntryId
        {
            get => passEntry.BSA_EntryID;
            set => SetPassValue(nameof(passEntry.BSA_EntryID), passEntry.BSA_EntryID, value, "BSA Pass Entry");
        }

        public short I_06
        {
            get => passEntry.I_06;
            set => SetPassValue(nameof(passEntry.I_06), passEntry.I_06, value, "BSA Pass I_06");
        }

        public float BacCondition
        {
            get => passEntry.F_08;
            set => SetPassValue(nameof(passEntry.F_08), passEntry.F_08, value, "BSA Pass BAC Condition");
        }

        public float F_12
        {
            get => passEntry.F_12;
            set => SetPassValue(nameof(passEntry.F_12), passEntry.F_12, value, "BSA Pass F_12");
        }

        public BsaType0ViewModel(BSA_Type0 type) : base(type)
        {
            passEntry = type;
        }

        private void SetPassValue<T>(string propertyName, T oldValue, T newValue, string undoName)
        {
            if (Equals(oldValue, newValue))
                return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propertyName, passEntry, oldValue, newValue, undoName));
            passEntry.GetType().GetProperty(propertyName).SetValue(passEntry, newValue, null);
            RaisePropertyChanged(string.Empty);
        }
    }
}
