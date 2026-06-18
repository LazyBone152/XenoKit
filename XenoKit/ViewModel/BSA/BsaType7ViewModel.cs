using System;
using System.Collections.Generic;
using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType7ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type7 sound;
        private static readonly IReadOnlyCollection<string> TypedFields = new[]
        {
            nameof(BSA_Type7.AcbType),
            nameof(BSA_Type7.CueId)
        };
        protected override IReadOnlyCollection<string> TypedFieldNames => TypedFields;
        public Array AcbTypes => Enum.GetValues(typeof(AcbType));

        public AcbType AcbType
        {
            get => sound.AcbType;
            set => SetBsaValue(nameof(sound.AcbType), sound.AcbType, value, "BSA Sound ACB Type");
        }

        public ushort CueId
        {
            get => sound.CueId;
            set => SetBsaValue(nameof(sound.CueId), sound.CueId, value, "BSA Sound Cue ID");
        }

        public BsaType7ViewModel(BSA_Type7 type) : base(type)
        {
            sound = type;
        }
    }
}
