using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight;
using Xv2CoreLib;
using XenoKit.Engine;

namespace XenoKit.ViewModel.BCS
{
    public class BcsPartViewModel : ObservableObject
    {
        private PartSet partSet;
        private Part part;

        public short Model
        {
            get
            {
                return part.Model;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Model), part, part.Model, value, "BCS Model"), UndoGroup.BCS, undoContext: part);
                part.Model = value;
                RaisePropertyChanged(() => Model);
                ReloadPartSet();
            }
        }
        public short Model2
        {
            get
            {
                return part.Model2;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Model2), part, part.Model2, value, "BCS Model2"), UndoGroup.BCS, undoContext: part);
                part.Model2 = value;
                RaisePropertyChanged(() => Model2);
                ReloadPartSet();
            }
        }
        public short Texture
        {
            get
            {
                return part.Texture;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Texture), part, part.Texture, value, "BCS Texture"), UndoGroup.BCS, undoContext: part);
                part.Texture = value;
                RaisePropertyChanged(() => Texture);
            }
        }
        public short Shader
        {
            get
            {
                return part.Shader;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Shader), part, part.Shader, value, "BCS Shader"));
                part.Shader = value;
                RaisePropertyChanged(() => Shader);
            }
        }

        //Part Flags
        public bool Flag_Unk1
        {
            get
            {
                return part.Flags.HasFlag(Part.PartFlags.Unk1);
            }
            set
            {
                SetPartFlags(Part.PartFlags.Unk1, value);
                RaisePropertyChanged(() => Flag_Unk1);
            }
        }
        public bool Flag_DytFromTextureEmb
        {
            get
            {
                return part.Flags.HasFlag(Part.PartFlags.DytFromTextureEmb);
            }
            set
            {
                SetPartFlags(Part.PartFlags.DytFromTextureEmb, value);
                RaisePropertyChanged(() => Flag_DytFromTextureEmb);
                ReloadPartSet();
            }
        }
        public bool Flag_DytRampsFromTextureEmb
        {
            get
            {
                return part.Flags.HasFlag(Part.PartFlags.DytRampsFromTextureEmb);
            }
            set
            {
                SetPartFlags(Part.PartFlags.DytRampsFromTextureEmb, value);
                RaisePropertyChanged(() => Flag_DytRampsFromTextureEmb);
            }
        }
        public bool Flag_GreenScouterOverlay
        {
            get
            {
                return part.Flags.HasFlag(Part.PartFlags.GreenScouterOverlay);
            }
            set
            {
                SetPartFlags(Part.PartFlags.GreenScouterOverlay, value);
                RaisePropertyChanged(() => Flag_GreenScouterOverlay);
            }
        }
        public bool Flag_RedScouterOverlay
        {
            get
            {
                return part.Flags.HasFlag(Part.PartFlags.RedScouterOverlay);
            }
            set
            {
                SetPartFlags(Part.PartFlags.RedScouterOverlay, value);
                RaisePropertyChanged(() => Flag_RedScouterOverlay);
            }
        }
        public bool Flag_BlueScouterOverlay
        {
            get
            {
                return part.Flags.HasFlag(Part.PartFlags.BlueScouterOverlay);
            }
            set
            {
                SetPartFlags(Part.PartFlags.BlueScouterOverlay, value);
                RaisePropertyChanged(() => Flag_BlueScouterOverlay);
            }
        }
        public bool Flag_PurpleScouterOverlay
        {
            get
            {
                return part.Flags.HasFlag(Part.PartFlags.PurpleScouterOverlay);
            }
            set
            {
                SetPartFlags(Part.PartFlags.PurpleScouterOverlay, value);
                RaisePropertyChanged(() => Flag_PurpleScouterOverlay);
            }
        }
        public bool Flag_Unk8
        {
            get
            {
                return part.Flags.HasFlag(Part.PartFlags.Unk8);
            }
            set
            {
                SetPartFlags(Part.PartFlags.Unk8, value);
                RaisePropertyChanged(() => Flag_Unk8);
            }
        }
        public bool Flag_Unk9
        {
            get
            {
                return part.Flags.HasFlag(Part.PartFlags.Unk9);
            }
            set
            {
                SetPartFlags(Part.PartFlags.Unk9, value);
                RaisePropertyChanged(() => Flag_Unk9);
            }
        }
        public bool Flag_OrangeScouterOverlay
        {
            get
            {
                return part.Flags.HasFlag(Part.PartFlags.OrangeScouterOverlay);
            }
            set
            {
                SetPartFlags(Part.PartFlags.OrangeScouterOverlay, value);
                RaisePropertyChanged(() => Flag_OrangeScouterOverlay);
            }
        }

        //HideFlags
        public bool Hide_FaceBase
        {
            get
            {
                return part.Hide_FaceBase;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Hide_FaceBase), part, part.Hide_FaceBase, value, "BCS Hide_FaceBase"), UndoGroup.BCS, undoContext: part);
                part.Hide_FaceBase = value;
                RaisePropertyChanged(() => Hide_FaceBase);
                ReloadPartSet();
            }
        }
        public bool Hide_Forehead
        {
            get
            {
                return part.Hide_Forehead;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Hide_Forehead), part, part.Hide_Forehead, value, "BCS Hide_Forehead"), UndoGroup.BCS, undoContext: part);
                part.Hide_Forehead = value;
                RaisePropertyChanged(() => Hide_Forehead);
                ReloadPartSet();
            }
        }
        public bool Hide_Eye
        {
            get
            {
                return part.Hide_Eye;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Hide_Eye), part, part.Hide_Eye, value, "BCS Hide_Eye"), UndoGroup.BCS, undoContext: part);
                part.Hide_Eye = value;
                RaisePropertyChanged(() => Hide_Eye);
                ReloadPartSet();
            }
        }
        public bool Hide_Nose
        {
            get
            {
                return part.Hide_Nose;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Hide_Nose), part, part.Hide_Nose, value, "BCS Hide_Nose"), UndoGroup.BCS, undoContext: part);
                part.Hide_Nose = value;
                RaisePropertyChanged(() => Hide_Nose);
                ReloadPartSet();
            }
        }
        public bool Hide_Ear
        {
            get
            {
                return part.Hide_Ear;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Hide_Ear), part, part.Hide_Ear, value, "BCS Hide_Ear"), UndoGroup.BCS, undoContext: part);
                part.Hide_Ear = value;
                RaisePropertyChanged(() => Hide_Ear);
                ReloadPartSet();
            }
        }
        public bool Hide_Hair
        {
            get
            {
                return part.Hide_Hair;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Hide_Hair), part, part.Hide_Hair, value, "BCS Hide_Hair"), UndoGroup.BCS, undoContext: part);
                part.Hide_Hair = value;
                RaisePropertyChanged(() => Hide_Hair);
                ReloadPartSet();
            }
        }
        public bool Hide_Bust
        {
            get
            {
                return part.Hide_Bust;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Hide_Bust), part, part.Hide_Bust, value, "BCS Hide_Bust"), UndoGroup.BCS, undoContext: part);
                part.Hide_Bust = value;
                RaisePropertyChanged(() => Hide_Bust);
                ReloadPartSet();
            }
        }
        public bool Hide_Pants
        {
            get
            {
                return part.Hide_Pants;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Hide_Pants), part, part.Hide_Pants, value, "BCS Hide_Pants"), UndoGroup.BCS, undoContext: part);
                part.Hide_Pants = value;
                RaisePropertyChanged(() => Hide_Pants);
                ReloadPartSet();
            }
        }
        public bool Hide_Rist
        {
            get
            {
                return part.Hide_Rist;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Hide_Rist), part, part.Hide_Rist, value, "BCS Hide_Rist"), UndoGroup.BCS, undoContext: part);
                part.Hide_Rist = value;
                RaisePropertyChanged(() => Hide_Rist);
                ReloadPartSet();
            }
        }
        public bool Hide_Boots
        {
            get
            {
                return part.Hide_Boots;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Hide_Boots), part, part.Hide_Boots, value, "BCS Hide_Boots"), UndoGroup.BCS, undoContext: part);
                part.Hide_Boots = value;
                RaisePropertyChanged(() => Hide_Boots);
                ReloadPartSet();
            }
        }

        //HideMatFlags
        public bool HideMat_FaceBase
        {
            get
            {
                return part.HideMat_FaceBase;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.HideMat_FaceBase), part, part.HideMat_FaceBase, value, "BCS HideMat_FaceBase"), UndoGroup.BCS, undoContext: part);
                part.HideMat_FaceBase = value;
                RaisePropertyChanged(() => HideMat_FaceBase);
                ReloadPartSet();
            }
        }
        public bool HideMat_Forehead
        {
            get
            {
                return part.HideMat_Forehead;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.HideMat_Forehead), part, part.HideMat_Forehead, value, "BCS HideMat_Forehead"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Forehead = value;
                RaisePropertyChanged(() => HideMat_Forehead);
                ReloadPartSet();
            }
        }
        public bool HideMat_Eye
        {
            get
            {
                return part.HideMat_Eye;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.HideMat_Eye), part, part.HideMat_Eye, value, "BCS HideMat_Eye"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Eye = value;
                RaisePropertyChanged(() => HideMat_Eye);
                ReloadPartSet();
            }
        }
        public bool HideMat_Nose
        {
            get
            {
                return part.HideMat_Nose;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.HideMat_Nose), part, part.HideMat_Nose, value, "BCS HideMat_Nose"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Nose = value;
                RaisePropertyChanged(() => HideMat_Nose);
                ReloadPartSet();
            }
        }
        public bool HideMat_Ear
        {
            get
            {
                return part.HideMat_Ear;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.HideMat_Ear), part, part.HideMat_Ear, value, "BCS HideMat_Ear"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Ear = value;
                RaisePropertyChanged(() => HideMat_Ear);
                ReloadPartSet();
            }
        }
        public bool HideMat_Hair
        {
            get
            {
                return part.HideMat_Hair;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.HideMat_Hair), part, part.HideMat_Hair, value, "BCS HideMat_Hair"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Hair = value;
                RaisePropertyChanged(() => HideMat_Hair);
                ReloadPartSet();
            }
        }
        public bool HideMat_Bust
        {
            get
            {
                return part.HideMat_Bust;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.HideMat_Bust), part, part.HideMat_Bust, value, "BCS HideMat_Bust"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Bust = value;
                RaisePropertyChanged(() => HideMat_Bust);
                ReloadPartSet();
            }
        }
        public bool HideMat_Pants
        {
            get
            {
                return part.HideMat_Pants;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.HideMat_Pants), part, part.HideMat_Pants, value, "BCS HideMat_Pants"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Pants = value;
                RaisePropertyChanged(() => HideMat_Pants);
                ReloadPartSet();
            }
        }
        public bool HideMat_Rist
        {
            get
            {
                return part.HideMat_Rist;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.HideMat_Rist), part, part.HideMat_Rist, value, "BCS HideMat_Rist"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Rist = value;
                RaisePropertyChanged(() => HideMat_Rist);
                ReloadPartSet();
            }
        }
        public bool HideMat_Boots
        {
            get
            {
                return part.HideMat_Boots;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.HideMat_Boots), part, part.HideMat_Boots, value, "BCS HideMat_Boots"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Boots = value;
                RaisePropertyChanged(() => HideMat_Boots);
                ReloadPartSet();
            }
        }

        public float F_36
        {
            get
            {
                return part.F_36;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.F_36), part, part.F_36, value, "BCS Part F_36"));
                part.F_36 = value;
                RaisePropertyChanged(() => F_36);
            }
        }
        public float F_40
        {
            get
            {
                return part.F_40;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.F_40), part, part.F_40, value, "BCS Part F_40"));
                part.F_40 = value;
                RaisePropertyChanged(() => F_40);
            }
        }
        public int I_44
        {
            get
            {
                return part.I_44;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.I_44), part, part.I_44, value, "BCS Part I_44"));
                part.I_44 = value;
                RaisePropertyChanged(() => I_44);
            }
        }
        public int I_48
        {
            get
            {
                return part.I_48;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.I_48), part, part.I_48, value, "BCS Part I_48"));
                part.I_48 = value;
                RaisePropertyChanged(() => I_48);
            }
        }
        public string CharaCode
        {
            get
            {
                return part.CharaCode;
            }
            set
            {
                if(value?.Length < 4)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.CharaCode), part, part.CharaCode, value, "BCS Part CharaCode"), UndoGroup.BCS, undoContext: part);
                    part.CharaCode = value;
                }
                RaisePropertyChanged(() => CharaCode);
                ReloadPartSet();
            }
        }
        public string EmdPath
        {
            get
            {
                return part.EmdPath;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.EmdPath), part, part.EmdPath, value, "BCS Part EmdPath"), UndoGroup.BCS, undoContext: part);
                part.EmdPath = value;
                RaisePropertyChanged(() => EmdPath);
                ReloadPartSet();
            }
        }
        public string EmmPath
        {
            get
            {
                return part.EmmPath;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.EmmPath), part, part.EmmPath, value, "BCS Part EmmPath"), UndoGroup.BCS, undoContext: part);
                part.EmmPath = value;
                RaisePropertyChanged(() => EmmPath);
                ReloadPartSet();
            }
        }
        public string EmbPath
        {
            get
            {
                return part.EmbPath;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.EmbPath), part, part.EmbPath, value, "BCS Part EmbPath"), UndoGroup.BCS, undoContext: part);
                part.EmbPath = value;
                RaisePropertyChanged(() => EmbPath);
                ReloadPartSet();
            }
        }
        public string EanPath
        {
            get
            {
                return part.EanPath;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.EanPath), part, part.EanPath, value, "BCS Part EanPath"), UndoGroup.BCS, undoContext: part);
                part.EanPath = value;
                RaisePropertyChanged(() => EanPath);
                ReloadPartSet();
            }
        }

        public BcsPartViewModel(Part part, PartSet partSet)
        {
            this.part = part;
            this.partSet = partSet;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        private void SetPartFlags(Part.PartFlags flag, bool state)
        {
            var newFlag = part.Flags.SetFlag(flag, state);

            if (part.Flags != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<Part>(nameof(part.Flags), part, part.Flags, newFlag, "Part Flags"), UndoGroup.BCS, undoContext: part);
                part.Flags = newFlag;
                ReloadPartSet();
            }
        }

        public void ReleaseEvents()
        {
            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object sender, System.EventArgs e)
        {
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            //Needed for updating properties when undo/redo is called
            RaisePropertyChanged(() => Model);
            RaisePropertyChanged(() => Model2);
            RaisePropertyChanged(() => Texture);
            RaisePropertyChanged(() => Shader);
            RaisePropertyChanged(() => Flag_Unk1);
            RaisePropertyChanged(() => Flag_DytFromTextureEmb);
            RaisePropertyChanged(() => Flag_DytRampsFromTextureEmb);
            RaisePropertyChanged(() => Flag_GreenScouterOverlay);
            RaisePropertyChanged(() => Flag_RedScouterOverlay);
            RaisePropertyChanged(() => Flag_BlueScouterOverlay);
            RaisePropertyChanged(() => Flag_PurpleScouterOverlay);
            RaisePropertyChanged(() => Flag_Unk8);
            RaisePropertyChanged(() => Flag_Unk9);
            RaisePropertyChanged(() => Flag_OrangeScouterOverlay);

            RaisePropertyChanged(() => Hide_FaceBase);
            RaisePropertyChanged(() => Hide_Forehead);
            RaisePropertyChanged(() => Hide_Eye);
            RaisePropertyChanged(() => Hide_Nose);
            RaisePropertyChanged(() => Hide_Ear);
            RaisePropertyChanged(() => Hide_Hair);
            RaisePropertyChanged(() => Hide_Bust);
            RaisePropertyChanged(() => Hide_Pants);
            RaisePropertyChanged(() => Hide_Rist);
            RaisePropertyChanged(() => Hide_Boots);

            RaisePropertyChanged(() => HideMat_FaceBase);
            RaisePropertyChanged(() => HideMat_Forehead);
            RaisePropertyChanged(() => HideMat_Eye);
            RaisePropertyChanged(() => HideMat_Nose);
            RaisePropertyChanged(() => HideMat_Ear);
            RaisePropertyChanged(() => HideMat_Hair);
            RaisePropertyChanged(() => HideMat_Bust);
            RaisePropertyChanged(() => HideMat_Pants);
            RaisePropertyChanged(() => HideMat_Rist);
            RaisePropertyChanged(() => HideMat_Boots);

            RaisePropertyChanged(() => F_36);
            RaisePropertyChanged(() => F_40);
            RaisePropertyChanged(() => I_44);
            RaisePropertyChanged(() => I_48);
            RaisePropertyChanged(() => CharaCode);
            RaisePropertyChanged(() => EmdPath);
            RaisePropertyChanged(() => EmmPath);
            RaisePropertyChanged(() => EmbPath);
            RaisePropertyChanged(() => EanPath);
        }
        
        private void ReloadPartSet()
        {
            if(SceneManager.Actors[0]?.PartSet != null)
            {
                if (SceneManager.Actors[0].PartSet.IsPartSet(part))
                {
                    SceneManager.Actors[0].PartSet.LoadPart((int)part.PartType);
                }
            }
        }
    }


}
