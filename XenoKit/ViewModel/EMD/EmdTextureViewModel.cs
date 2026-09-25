using Xv2CoreLib.EMD;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight;
using static Xv2CoreLib.EMD.EMD_TextureSamplerDef;
using Xv2CoreLib.EMB_CLASS;
using System.Windows;
using Xv2CoreLib.EMO;

namespace XenoKit.ViewModel.EMD
{
    public class EmdTextureViewModel : ObservableObject
    {
        private EMB_TextureFile embFile;
        private IModelFile modelFile;
        private EMO_File emoFile;
        private EMD_TextureSamplerDef texture;
        private object submeshContext; //EMD_Submesh or EMG_SubmeshGroup

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
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.EmbIndex), texture, texture.EmbIndex, value),
                    new UndoActionDelegate(modelFile, nameof(modelFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler EmbIndex", UndoGroup.EMD);

                texture.EmbIndex = value;

                RaisePropertyChanged(() => EmbIndex);
                modelFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
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
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.AddressModeU), texture, texture.AddressModeU, value),
                    new UndoActionDelegate(modelFile, nameof(modelFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler AddressModeU", UndoGroup.EMD);
                texture.AddressModeU = value;

                RaisePropertyChanged(() => AddressModeU);
                modelFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
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
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.AddressModeV), texture, texture.AddressModeV, value),
                    new UndoActionDelegate(modelFile, nameof(modelFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler AddressModeV", UndoGroup.EMD);
                texture.AddressModeV = value;

                RaisePropertyChanged(() => AddressModeV);
                modelFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
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
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.FilteringMin), texture, texture.FilteringMin, value),
                    new UndoActionDelegate(modelFile, nameof(modelFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler FilteringMin", UndoGroup.EMD);
                texture.FilteringMin = value;

                RaisePropertyChanged(() => FilteringMin);
                modelFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
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
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.FilteringMag), texture, texture.FilteringMag, value),
                    new UndoActionDelegate(modelFile, nameof(modelFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler FilteringMag", UndoGroup.EMD);
                texture.FilteringMag = value;

                RaisePropertyChanged(() => FilteringMag);
                modelFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
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
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.ScaleU), texture, texture.ScaleU, value),
                    new UndoActionDelegate(modelFile, nameof(modelFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler ScaleU", UndoGroup.EMD);

                texture.ScaleU = value;

                RaisePropertyChanged(() => ScaleU);
                modelFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
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
                UndoManager.Instance.AddCompositeUndo(new System.Collections.Generic.List<IUndoRedo>()
                {
                    new UndoableProperty<EMD_TextureSamplerDef>(nameof(EMD_TextureSamplerDef.ScaleV), texture, texture.ScaleV, value),
                    new UndoActionDelegate(modelFile, nameof(modelFile.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Sampler, submeshContext))
                }, "TextureSampler ScaleV", UndoGroup.EMD);
                texture.ScaleV = value;

                RaisePropertyChanged(() => ScaleV);
                modelFile.TriggerModelModifiedEvent(EditTypeEnum.Sampler, texture, submeshContext);
            }
        }

        public EMB_TextureEntry SelectedEmbEntry
        {
            get => embFile != null ? embFile.GetEntry(texture.EmbIndex) : null;
            set
            {
                if(embFile != null)
                {
                    EmbIndex = (byte)value.ID;
                }
            }
        }
        public Visibility TextureSelectorVisibility => embFile != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TextureIndexVisibility => embFile == null ? Visibility.Visible : Visibility.Collapsed;

        public EmdTextureViewModel(EMD_TextureSamplerDef texture, object submeshContext, IModelFile modelFile, EMB_TextureFile embFile)
        {
            this.texture = texture;
            this.modelFile = modelFile;
            this.submeshContext = submeshContext;
            this.embFile = embFile;
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
