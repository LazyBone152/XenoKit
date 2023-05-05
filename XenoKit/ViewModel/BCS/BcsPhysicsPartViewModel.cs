using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight;
using Xv2CoreLib;
using XenoKit.Engine;

namespace XenoKit.ViewModel.BCS
{
    public class BcsPhysicsPartViewModel : ObservableObject
    {
        private PartSet partSet;
        private PhysicsPart part;

        public short Model
        {
            get
            {
                return part.Model1;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Model1), part, part.Model1, value, "PhysicsPart Model"), UndoGroup.BCS, undoContext: part);
                part.Model1 = value;
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Model2), part, part.Model2, value, "PhysicsPart Model2"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Texture), part, part.Texture, value, "PhysicsPart Texture"), UndoGroup.BCS, undoContext: part);
                part.Texture = value;
                RaisePropertyChanged(() => Texture);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Hide_FaceBase), part, part.Hide_FaceBase, value, "PhysicsPart Hide_FaceBase"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Hide_Forehead), part, part.Hide_Forehead, value, "PhysicsPart Hide_Forehead"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Hide_Eye), part, part.Hide_Eye, value, "PhysicsPart Hide_Eye"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Hide_Nose), part, part.Hide_Nose, value, "PhysicsPart Hide_Nose"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Hide_Ear), part, part.Hide_Ear, value, "PhysicsPart Hide_Ear"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Hide_Hair), part, part.Hide_Hair, value, "PhysicsPart Hide_Hair"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Hide_Bust), part, part.Hide_Bust, value, "PhysicsPart Hide_Bust"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Hide_Pants), part, part.Hide_Pants, value, "PhysicsPart Hide_Pants"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Hide_Rist), part, part.Hide_Rist, value, "PhysicsPart Hide_Rist"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Hide_Boots), part, part.Hide_Boots, value, "PhysicsPart Hide_Boots"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.HideMat_FaceBase), part, part.HideMat_FaceBase, value, "PhysicsPart HideMat_FaceBase"), UndoGroup.BCS, undoContext: part);
                part.HideMat_FaceBase = value;
                RaisePropertyChanged(() => HideMat_FaceBase);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.HideMat_Forehead), part, part.HideMat_Forehead, value, "PhysicsPart HideMat_Forehead"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Forehead = value;
                RaisePropertyChanged(() => HideMat_Forehead);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.HideMat_Eye), part, part.HideMat_Eye, value, "PhysicsPart HideMat_Eye"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Eye = value;
                RaisePropertyChanged(() => HideMat_Eye);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.HideMat_Nose), part, part.HideMat_Nose, value, "PhysicsPart HideMat_Nose"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Nose = value;
                RaisePropertyChanged(() => HideMat_Nose);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.HideMat_Ear), part, part.HideMat_Ear, value, "PhysicsPart HideMat_Ear"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Ear = value;
                RaisePropertyChanged(() => HideMat_Ear);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.HideMat_Hair), part, part.HideMat_Hair, value, "PhysicsPart HideMat_Hair"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Hair = value;
                RaisePropertyChanged(() => HideMat_Hair);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.HideMat_Bust), part, part.HideMat_Bust, value, "PhysicsPart HideMat_Bust"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Bust = value;
                RaisePropertyChanged(() => HideMat_Bust);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.HideMat_Pants), part, part.HideMat_Pants, value, "PhysicsPart HideMat_Pants"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Pants = value;
                RaisePropertyChanged(() => HideMat_Pants);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.HideMat_Rist), part, part.HideMat_Rist, value, "PhysicsPart HideMat_Rist"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Rist = value;
                RaisePropertyChanged(() => HideMat_Rist);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.HideMat_Boots), part, part.HideMat_Boots, value, "PhysicsPart HideMat_Boots"), UndoGroup.BCS, undoContext: part);
                part.HideMat_Boots = value;
                RaisePropertyChanged(() => HideMat_Boots);
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
                if (value?.Length < 4)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.CharaCode), part, part.CharaCode, value, "PhysicsPart  CharaCode"), UndoGroup.BCS, undoContext: part);
                    part.CharaCode = value;
                }
                RaisePropertyChanged(() => CharaCode);
                ReloadPartSet();
            }
        }
        public string Bone
        {
            get
            {
                return part.BoneToAttach;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.BoneToAttach), part, part.BoneToAttach, value, "PhysicsPart Bone"), UndoGroup.BCS, undoContext: part);
                part.BoneToAttach = value;
                RaisePropertyChanged(() => Bone);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.EmdPath), part, part.EmdPath, value, "PhysicsPart EmdPath"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.EmmPath), part, part.EmmPath, value, "PhysicsPart EmmPath"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.EmbPath), part, part.EmbPath, value, "PhysicsPart EmbPath"), UndoGroup.BCS, undoContext: part);
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.EanPath), part, part.EanPath, value, "PhysicsPart EanPath"), UndoGroup.BCS, undoContext: part);
                part.EanPath = value;
                RaisePropertyChanged(() => EanPath);
                ReloadPartSet();
            }
        }
        public string ScdPath
        {
            get
            {
                return part.ScdPath;
            }
            set
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.ScdPath), part, part.ScdPath, value, "PhysicsPart ScdPath"));
                part.ScdPath = value;
                RaisePropertyChanged(() => ScdPath);
            }
        }

        public BcsPhysicsPartViewModel(PhysicsPart part, PartSet partSet)
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
                UndoManager.Instance.AddUndo(new UndoableProperty<PhysicsPart>(nameof(part.Flags), part, part.Flags, newFlag, "Part Flags"), UndoGroup.BCS, undoContext: part);
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

            RaisePropertyChanged(() => CharaCode);
            RaisePropertyChanged(() => Bone);
            RaisePropertyChanged(() => EmdPath);
            RaisePropertyChanged(() => EmmPath);
            RaisePropertyChanged(() => EmbPath);
            RaisePropertyChanged(() => EanPath);
            RaisePropertyChanged(() => ScdPath);
        }


        private void ReloadPartSet()
        {
            if (SceneManager.Actors[0]?.PartSet != null)
            {
                Part parentPart = partSet.GetParentPart(part);

                if (SceneManager.Actors[0].PartSet.IsPartSet(parentPart))
                {
                    SceneManager.Actors[0].PartSet.LoadPart((int)parentPart.PartType);
                }
            }
        }
    }
}
