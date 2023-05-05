using Xv2CoreLib.EMD;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight;
using static Xv2CoreLib.EMD.EMD_TextureSamplerDef;

namespace XenoKit.ViewModel.EMD
{
    public class EmdTextureViewModel : ObservableObject
    {
        private EMD_File emdFile;
        private EMD_TextureSamplerDef texture;

        public byte I_00
        {
            get
            {
                return texture.I_00;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.I_00), texture, texture.I_00, value, "TextureSampler I_00"), UndoGroup.EMD);
                texture.I_00 = value;
                RaisePropertyChanged(() => I_00);
            }
        }
        public byte EmbIndex
        {
            get
            {
                return texture.EmbIndex;
            }
            set
            {
                texture.EmbIndex = value;

                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.EmbIndex), texture, texture.EmbIndex, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerTexturesChanged), true)
                }, "TextureSampler EmbIndex", UndoGroup.EMD);

                RaisePropertyChanged(() => EmbIndex);
                emdFile.TriggerTexturesChanged();
            }
        }
        public AddressMode AddressModeU
        {
            get
            {
                return texture.AddressModeU;
            }
            set
            {
                texture.AddressModeU = value;

                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.AddressModeU), texture, texture.AddressModeU, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerTexturesChanged), true)
                }, "TextureSampler AddressModeU", UndoGroup.EMD);

                RaisePropertyChanged(() => AddressModeU);
                emdFile.TriggerTexturesChanged();
            }
        }
        public AddressMode AddressModeV
        {
            get
            {
                return texture.AddressModeV;
            }
            set
            {
                texture.AddressModeV = value;

                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.AddressModeV), texture, texture.AddressModeV, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerTexturesChanged), true)
                }, "TextureSampler AddressModeV", UndoGroup.EMD);

                RaisePropertyChanged(() => AddressModeV);
                emdFile.TriggerTexturesChanged();
            }
        }
        public Filtering FilteringMin
        {
            get
            {
                return texture.FilteringMin;
            }
            set
            {
                texture.FilteringMin = value;

                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.FilteringMin), texture, texture.FilteringMin, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerTexturesChanged), true)
                }, "TextureSampler FilteringMin", UndoGroup.EMD);

                RaisePropertyChanged(() => FilteringMin);
                emdFile.TriggerTexturesChanged();
            }
        }
        public Filtering FilteringMag
        {
            get
            {
                return texture.FilteringMag;
            }
            set
            {
                texture.FilteringMag = value;

                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.FilteringMag), texture, texture.FilteringMag, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerTexturesChanged), true)
                }, "TextureSampler FilteringMag", UndoGroup.EMD);

                RaisePropertyChanged(() => FilteringMag);
                emdFile.TriggerTexturesChanged();
            }
        }
        public float ScaleU
        {
            get
            {
                return texture.ScaleU;
            }
            set
            {
                texture.ScaleU = value;

                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.ScaleU), texture, texture.ScaleU, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerTexturesChanged), true)
                }, "TextureSampler ScaleU", UndoGroup.EMD);

                RaisePropertyChanged(() => ScaleU);
                emdFile.TriggerTexturesChanged();
            }
        }
        public float ScaleV
        {
            get
            {
                return texture.ScaleV;
            }
            set
            {
                texture.ScaleV = value;

                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.ScaleV), texture, texture.ScaleV, value),
                    new UndoActionDelegate(emdFile, nameof(emdFile.TriggerTexturesChanged), true)
                }, "TextureSampler ScaleV", UndoGroup.EMD);

                RaisePropertyChanged(() => ScaleV);
                emdFile.TriggerTexturesChanged();
            }
        }

        public EmdTextureViewModel(EMD_TextureSamplerDef texture, EMD_File emdFile)
        {
            this.texture = texture;
            this.emdFile = emdFile;
        }

        public void UpdateProperties()
        {
            RaisePropertyChanged(() => I_00);
            RaisePropertyChanged(() => EmbIndex);
            RaisePropertyChanged(() => AddressModeU);
            RaisePropertyChanged(() => AddressModeV);
            RaisePropertyChanged(() => FilteringMin);
            RaisePropertyChanged(() => FilteringMag);
            RaisePropertyChanged(() => ScaleU);
            RaisePropertyChanged(() => ScaleV);
        }

    }
}
