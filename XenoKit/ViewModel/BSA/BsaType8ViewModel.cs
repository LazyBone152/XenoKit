using System.Collections.Generic;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType8ViewModel : BsaTypeBaseViewModel
    {
        private static readonly IReadOnlyCollection<string> PrimaryFieldNamesList = new[]
        {
            nameof(BSA_Type8.I_00),
            nameof(BSA_Type8.I_02)
        };
        private static readonly IReadOnlyDictionary<string, string> FieldNames = new Dictionary<string, string>
        {
            { nameof(BSA_Type8.I_00), "BPE ID" },
            { nameof(BSA_Type8.I_02), "Screen Effect Flags" }
        };

        protected override IReadOnlyCollection<string> PrimaryFieldNames => PrimaryFieldNamesList;
        protected override IReadOnlyDictionary<string, string> KnownFieldNames => FieldNames;

        public BsaType8ViewModel(BSA_Type8 type) : base(type)
        {
        }
    }
}
