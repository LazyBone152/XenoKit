using System.Collections.Generic;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType7ViewModel : BsaTypeBaseViewModel
    {
        private static readonly IReadOnlyCollection<string> PrimaryFieldNamesList = new[]
        {
            nameof(BSA_Type7.AcbType),
            nameof(BSA_Type7.CueId)
        };
        private static readonly IReadOnlyDictionary<string, string> FieldNames = new Dictionary<string, string>
        {
            { nameof(BSA_Type7.AcbType), "ACB Type" },
            { nameof(BSA_Type7.CueId), "Cue ID" }
        };

        protected override IReadOnlyCollection<string> PrimaryFieldNames => PrimaryFieldNamesList;
        protected override IReadOnlyDictionary<string, string> KnownFieldNames => FieldNames;

        public BsaType7ViewModel(BSA_Type7 type) : base(type)
        {
        }
    }
}
