using System.Collections.Generic;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType0ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type0 passEntry;
        private static readonly IReadOnlyCollection<string> TypedFields = new[]
        {
            nameof(BSA_Type0.I_02),
            nameof(BSA_Type0.BSA_EntryID),
            nameof(BSA_Type0.F_08)
        };

        protected override IReadOnlyCollection<string> TypedFieldNames => TypedFields;

        public short I_00
        {
            get => passEntry.I_00;
            set => SetBsaValue(nameof(passEntry.I_00), passEntry.I_00, value, "BSA Pass I_00");
        }

        public ushort MainCondition
        {
            get => passEntry.I_02;
            set => SetBsaValue(nameof(passEntry.I_02), passEntry.I_02, value, "BSA Pass Main Condition");
        }

        public ushort BsaEntryId
        {
            get => passEntry.BSA_EntryID;
            set => SetBsaValue(nameof(passEntry.BSA_EntryID), passEntry.BSA_EntryID, value, "BSA Pass Entry");
        }

        public short I_06
        {
            get => passEntry.I_06;
            set => SetBsaValue(nameof(passEntry.I_06), passEntry.I_06, value, "BSA Pass I_06");
        }

        public float BacCondition
        {
            get => passEntry.F_08;
            set => SetBsaValue(nameof(passEntry.F_08), passEntry.F_08, value, "BSA Pass BAC Condition");
        }

        public float F_12
        {
            get => passEntry.F_12;
            set => SetBsaValue(nameof(passEntry.F_12), passEntry.F_12, value, "BSA Pass F_12");
        }

        public BsaType0ViewModel(BSA_Type0 type) : base(type)
        {
            passEntry = type;
        }
    }
}
