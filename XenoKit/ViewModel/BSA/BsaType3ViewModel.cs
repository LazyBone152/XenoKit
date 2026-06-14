using System;
using System.Collections.Generic;
using System.Globalization;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType3ViewModel : BsaTypeBaseViewModel
    {
        private const ushort BoundsTypeMask = 0x000F;
        private readonly BSA_Type3 hitbox;
        private static readonly IReadOnlyCollection<string> TypedFields = new[]
        {
            nameof(BSA_Type3.I_00),
            nameof(BSA_Type3.I_04),
            nameof(BSA_Type3.F_08),
            nameof(BSA_Type3.F_12),
            nameof(BSA_Type3.F_16),
            nameof(BSA_Type3.F_20),
            nameof(BSA_Type3.F_24),
            nameof(BSA_Type3.F_28),
            nameof(BSA_Type3.F_32),
            nameof(BSA_Type3.F_36),
            nameof(BSA_Type3.F_40),
            nameof(BSA_Type3.F_44)
        };
        private static readonly IReadOnlyCollection<string> PrimaryFieldNamesList = new[]
        {
            nameof(BSA_Type3.I_48),
            nameof(BSA_Type3.I_50),
            nameof(BSA_Type3.I_52),
            nameof(BSA_Type3.FirstHit),
            nameof(BSA_Type3.MultipleHits),
            nameof(BSA_Type3.LastHit)
        };
        private static readonly IReadOnlyDictionary<string, string> FieldNames = new Dictionary<string, string>
        {
            { nameof(BSA_Type3.I_48), "Amount" },
            { nameof(BSA_Type3.I_50), "Power" },
            { nameof(BSA_Type3.I_52), "I_52" },
            { nameof(BSA_Type3.FirstHit), "BDM First Hit ID" },
            { nameof(BSA_Type3.MultipleHits), "BDM Multiple Hits ID" },
            { nameof(BSA_Type3.LastHit), "BDM Last Hit ID" }
        };

        protected override IReadOnlyCollection<string> TypedFieldNames => TypedFields;
        protected override IReadOnlyCollection<string> PrimaryFieldNames => PrimaryFieldNamesList;
        protected override IReadOnlyDictionary<string, string> KnownFieldNames => FieldNames;

        public Array BoundingBoxTypes => Enum.GetValues(typeof(BAC_Type1.BoundingBoxTypeEnum));
        public Array Switches => Enum.GetValues(typeof(Switch));

        public BAC_Type1.BoundingBoxTypeEnum BoundingBoxType
        {
            get => (BAC_Type1.BoundingBoxTypeEnum)(hitbox.I_00 & BoundsTypeMask);
            set
            {
                ushort newFlags = (ushort)((hitbox.I_00 & ~BoundsTypeMask) | ((ushort)value & BoundsTypeMask));
                SetMatrixFlags(newFlags, "BSA Hitbox Bounds Type");
                RaisePropertyChanged(nameof(BoundingBoxType));
                RaisePropertyChanged(nameof(BoundsEnabled));
                RaisePropertyChanged(nameof(MatrixFlagsText));
            }
        }

        public bool BoundsEnabled => BoundingBoxType != BAC_Type1.BoundingBoxTypeEnum.Uniform;

        public Switch GrowMaxBounds
        {
            get => hitbox.I_04 == 0 ? Switch.Off : Switch.On;
            set
            {
                ushort newValue = value == Switch.On ? (ushort)1 : (ushort)0;
                SetHitboxValue(nameof(hitbox.I_04), hitbox.I_04, newValue, "BSA Hitbox Grow Max Bounds");
                RaisePropertyChanged(nameof(GrowMaxBounds));
            }
        }

        public string MatrixFlagsText
        {
            get => $"0x{hitbox.I_00:X}";
            set
            {
                if (!TryParseUshort(value, out ushort newFlags))
                {
                    RaisePropertyChanged(nameof(MatrixFlagsText));
                    return;
                }

                SetMatrixFlags(newFlags, "BSA Hitbox Matrix Flags");
                RaisePropertyChanged(nameof(BoundingBoxType));
                RaisePropertyChanged(nameof(BoundsEnabled));
                RaisePropertyChanged(nameof(MatrixFlagsText));
            }
        }

        public float PositionX
        {
            get => hitbox.F_08;
            set => SetHitboxValue(nameof(hitbox.F_08), hitbox.F_08, value, "BSA Hitbox Position X");
        }

        public float PositionY
        {
            get => hitbox.F_12;
            set => SetHitboxValue(nameof(hitbox.F_12), hitbox.F_12, value, "BSA Hitbox Position Y");
        }

        public float PositionZ
        {
            get => hitbox.F_16;
            set => SetHitboxValue(nameof(hitbox.F_16), hitbox.F_16, value, "BSA Hitbox Position Z");
        }

        public float Size
        {
            get => hitbox.F_20;
            set => SetHitboxValue(nameof(hitbox.F_20), hitbox.F_20, value, "BSA Hitbox Scale");
        }

        public float MaxX
        {
            get => hitbox.F_24;
            set => SetHitboxValue(nameof(hitbox.F_24), hitbox.F_24, value, "BSA Hitbox Max X");
        }

        public float MaxY
        {
            get => hitbox.F_28;
            set => SetHitboxValue(nameof(hitbox.F_28), hitbox.F_28, value, "BSA Hitbox Max Y");
        }

        public float MaxZ
        {
            get => hitbox.F_32;
            set => SetHitboxValue(nameof(hitbox.F_32), hitbox.F_32, value, "BSA Hitbox Max Z");
        }

        public float MinX
        {
            get => hitbox.F_36;
            set => SetHitboxValue(nameof(hitbox.F_36), hitbox.F_36, value, "BSA Hitbox Min X");
        }

        public float MinY
        {
            get => hitbox.F_40;
            set => SetHitboxValue(nameof(hitbox.F_40), hitbox.F_40, value, "BSA Hitbox Min Y");
        }

        public float MinZ
        {
            get => hitbox.F_44;
            set => SetHitboxValue(nameof(hitbox.F_44), hitbox.F_44, value, "BSA Hitbox Min Z");
        }

        public BsaType3ViewModel(BSA_Type3 type) : base(type)
        {
            hitbox = type;
        }

        private void SetMatrixFlags(ushort newFlags, string undoName)
        {
            if (hitbox.I_00 == newFlags)
                return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(nameof(hitbox.I_00), hitbox, hitbox.I_00, newFlags, undoName));
            hitbox.I_00 = newFlags;
            NotifyTypeChanged();
        }

        private void SetHitboxValue<T>(string propertyName, T oldValue, T newValue, string undoName)
        {
            if (Equals(oldValue, newValue))
                return;

            UndoManager.Instance.AddUndo(new UndoablePropertyGeneric(propertyName, hitbox, oldValue, newValue, undoName));
            hitbox.GetType().GetProperty(propertyName).SetValue(hitbox, newValue, null);
            RaisePropertyChanged(string.Empty);
            NotifyTypeChanged();
        }

        private static bool TryParseUshort(string value, out ushort result)
        {
            string text = value?.Trim() ?? string.Empty;

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return ushort.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);

            return ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }
    }
}
