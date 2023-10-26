using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XenoKit.Editor;
using XenoKit.Editor.Data;
using XenoKit.Engine;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.BCS;
using Xv2CoreLib.Resource;

namespace XenoKit.Views
{
    /// <summary>
    /// Interaction logic for CacView.xaml
    /// </summary>
    public partial class CacView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int CurrentRace = -1;
        public CustomAvatar Avatar { get; set; }
        public Xv2CoreLib.SAV.CaC CAC { get; set; }
        public BCS_File BcsFile { get; set; }
        public Xv2CoreLib.SAV.CacPreset Preset { get; set; }

        public int SelectedPreset
        {
            get => Avatar != null ? Avatar.Preset : 0;
            set
            {
                if(Avatar?.Preset != value)
                {
                    Avatar.Preset = value;
                    Preset = CAC?.Presets[value];
                    NotifyPropertyChanged(nameof(SelectedPreset));
                    NotifyPropertyChanged(nameof(Preset));
                    UpdateColors(true);
                    Avatar.IsAppearenceDirty = true;
                }
            }
        }

        public int HairID
        {
            get => CAC != null ? CAC.Appearence.I_152 : -1;
            set
            {
                if(CAC != null)
                {
                    CAC.Appearence.I_152 = value;
                    NotifyPropertyChanged(nameof(HairID));
                }
            }
        }

        public List<Xv2Item> Presets { get; set; }

        //Body Parts
        public AsyncObservableCollection<Xv2Item> Hair { get; set; } //Head/Hair
        public AsyncObservableCollection<Xv2Item> Eyes { get; set; }
        public AsyncObservableCollection<Xv2Item> FaceForehead { get; set; } //Pupils
        public AsyncObservableCollection<Xv2Item> Nose { get; set; }
        public AsyncObservableCollection<Xv2Item> FaceBase { get; set; } //Mouth/Jaw
        public AsyncObservableCollection<Xv2Item> Ears { get; set; }

        //Equipment
        public AsyncObservableCollection<Xv2Item> Top { get; set; }
        public AsyncObservableCollection<Xv2Item> Bottom { get; set; }
        public AsyncObservableCollection<Xv2Item> Gloves { get; set; }
        public AsyncObservableCollection<Xv2Item> Shoes { get; set; }
        public AsyncObservableCollection<Xv2Item> Accessory { get; set; }

        //Colors
        public Brush Skin1 => CAC != null ? GetColorBrush(0, CAC.Appearence.I_36) : null;
        public Brush Skin2 => CAC != null ? GetColorBrush(1, CAC.Appearence.I_38) : null;
        public Brush Skin3 => CAC != null ? GetColorBrush(2, CAC.Appearence.I_40) : null;
        public Brush Skin4 => CAC != null ? GetColorBrush(3, CAC.Appearence.I_42) : null;
        public Brush HairColor => CAC != null ? GetColorBrush(4, CAC.Appearence.I_44) : null;
        public Brush EyeColor => CAC != null ? GetColorBrush(5, CAC.Appearence.I_46) : null;
        public Brush MakeupColor1 => CAC != null ? GetColorBrush(22, CAC.Appearence.I_48) : null;
        public Brush MakeupColor2 => CAC != null ? GetColorBrush(23, CAC.Appearence.I_50) : null;
        public Brush MakeupColor3 => CAC != null ? GetColorBrush(24, CAC.Appearence.I_52) : null;

        //Preset Colors
        public Brush TopColor1 => CAC != null ? GetColorBrush(6, Preset.I_28) : null;
        public Brush TopColor2 => CAC != null ? GetColorBrush(7, Preset.I_30) : null;
        public Brush TopColor3 => CAC != null ? GetColorBrush(8, Preset.I_32) : null;
        public Brush TopColor4 => CAC != null ? GetColorBrush(9, Preset.I_34) : null;
        public Brush BottomColor1 => CAC != null ? GetColorBrush(10, Preset.I_36) : null;
        public Brush BottomColor2 => CAC != null ? GetColorBrush(11, Preset.I_38) : null;
        public Brush BottomColor3 => CAC != null ? GetColorBrush(12, Preset.I_40) : null;
        public Brush BottomColor4 => CAC != null ? GetColorBrush(13, Preset.I_42) : null;
        public Brush GlovesColor1 => CAC != null ? GetColorBrush(14, Preset.I_44) : null;
        public Brush GlovesColor2 => CAC != null ? GetColorBrush(15, Preset.I_46) : null;
        public Brush GlovesColor3 => CAC != null ? GetColorBrush(16, Preset.I_48) : null;
        public Brush GlovesColor4 => CAC != null ? GetColorBrush(17, Preset.I_50) : null;
        public Brush ShoesColor1 => CAC != null ? GetColorBrush(18, Preset.I_52) : null;
        public Brush ShoesColor2 => CAC != null ? GetColorBrush(19, Preset.I_54) : null;
        public Brush ShoesColor3 => CAC != null ? GetColorBrush(20, Preset.I_56) : null;
        public Brush ShoesColor4 => CAC != null ? GetColorBrush(21, Preset.I_58) : null;

        //Size
        public int AvatarHeight
        {
            get => CAC != null ? CAC.Appearence.Height : 1;
            set
            {
                if (CAC == null) return;
                if(CAC.Appearence.Height != value)
                {
                    CAC.Appearence.Height = value;
                    Avatar.IsSizeDirty = true;
                    NotifyPropertyChanged(nameof(AvatarHeight));
                }
            }
        }
        public int AvatarBodyType
        {
            get => CAC != null ? CAC.Appearence.Width : 1;
            set
            {
                if (CAC == null) return;
                if (CAC.Appearence.Width != value)
                {
                    CAC.Appearence.Width = value;
                    Avatar.IsSizeDirty = true;
                    NotifyPropertyChanged(nameof(AvatarBodyType));
                }
            }
        }

        public CacView()
        {
            Presets = new List<Xv2Item>();
            Hair = new AsyncObservableCollection<Xv2Item>();
            Eyes = new AsyncObservableCollection<Xv2Item>();
            FaceForehead = new AsyncObservableCollection<Xv2Item>();
            Nose = new AsyncObservableCollection<Xv2Item>();
            FaceBase = new AsyncObservableCollection<Xv2Item>();
            Ears = new AsyncObservableCollection<Xv2Item>();
            Top = new AsyncObservableCollection<Xv2Item>();
            Bottom = new AsyncObservableCollection<Xv2Item>();
            Gloves = new AsyncObservableCollection<Xv2Item>();
            Shoes = new AsyncObservableCollection<Xv2Item>();
            Accessory = new AsyncObservableCollection<Xv2Item>();

            for(int i = 0; i < 8; i++)
            {
                Presets.Add(new Xv2Item(i, i > 0 ? $"Preset {i}" : "Main"));
            }

            InitializeComponent();
            Files.SelectedItemChanged += Files_SelectedItemChanged;
            DataContext = this;
        }

        private void Files_SelectedItemChanged(object sender, EventArgs e)
        {
            if (Files.Instance.SelectedItem?.Type != OutlinerItem.OutlinerItemType.CaC) return;

            if (Files.Instance.SelectedItem.CustomAvatar.Race != CurrentRace)
            {
                InitTab();
            }
            else
            {
                UpdateProperties();
            }

        }

        public void InitTab()
        {
            if (Files.Instance.SelectedItem.Type != OutlinerItem.OutlinerItemType.CaC) return;

            CurrentRace = Files.Instance.SelectedItem.CustomAvatar.Race;
            Avatar = Files.Instance.SelectedItem.CustomAvatar;
            CAC = Files.Instance.SelectedItem.CustomAvatar.CaC;
            Preset = CAC.Presets[SelectedPreset];
            BcsFile = Files.Instance.SelectedItem.character.CharacterData.BcsFile.File;

            CreateLists();
            UpdateColors();
            UpdateProperties();
        }

        private void CreateLists()
        {
            //Create body part lists
            Hair.Clear();
            Eyes.Clear();
            FaceForehead.Clear();
            Nose.Clear();
            FaceBase.Clear();
            Ears.Clear();

            for (int i = 0; i < 250; i++)
            {
                PartSet partSet = BcsFile.PartSets.FirstOrDefault(x => x.ID == i);
                if (partSet == null) continue;

                if(partSet.Hair != null)
                    Hair.Add(new Xv2Item(partSet.ID, $"Type {Hair.Count + 1}"));

                if (partSet.FaceEye != null)
                    Eyes.Add(new Xv2Item(partSet.ID, $"Type {Eyes.Count + 1}"));

                if (partSet.FaceForehead != null)
                    FaceForehead.Add(new Xv2Item(partSet.ID, $"Type {FaceForehead.Count + 1}"));

                if (partSet.FaceNose != null)
                    Nose.Add(new Xv2Item(partSet.ID, $"Type {Nose.Count + 1}"));

                if (partSet.FaceBase != null)
                    FaceBase.Add(new Xv2Item(partSet.ID, $"Type {FaceBase.Count + 1}"));

                if (partSet.FaceEar != null)
                    Ears.Add(new Xv2Item(partSet.ID, $"Type {Ears.Count + 1}"));
            }

            //Create equipment lists
            Top.Clear();
            Bottom.Clear();
            Gloves.Clear();
            Shoes.Clear();
            Accessory.Clear();

            Top.AddRange(Xenoverse2.Instance.GetTopCostumeNames((Xv2CoreLib.SAV.Race)CurrentRace));
            Bottom.AddRange(Xenoverse2.Instance.GetBottomCostumeNames((Xv2CoreLib.SAV.Race)CurrentRace));
            Gloves.AddRange(Xenoverse2.Instance.GetGlovesCostumeNames((Xv2CoreLib.SAV.Race)CurrentRace));
            Shoes.AddRange(Xenoverse2.Instance.GetShoesCostumeNames((Xv2CoreLib.SAV.Race)CurrentRace));
            Accessory.AddRange(Xenoverse2.Instance.GetAccessoryCostumeNames((Xv2CoreLib.SAV.Race)CurrentRace));
        }

        public Brush GetColorBrush(int colorGroup, int colorId)
        {
            if(BcsFile != null)
            {
                PartColor skin = BcsFile.PartColors.FirstOrDefault(x => x.ID == colorGroup);

                if(skin != null)
                {
                    return skin.GetPreview(colorId);
                }
            }

            return Brushes.White;
        }

        public void UpdateColors(bool onlyUpdatePresets = false)
        {
            if (!onlyUpdatePresets)
            {
                NotifyPropertyChanged(nameof(Skin1));
                NotifyPropertyChanged(nameof(Skin2));
                NotifyPropertyChanged(nameof(Skin3));
                NotifyPropertyChanged(nameof(Skin4));
                NotifyPropertyChanged(nameof(HairColor));
                NotifyPropertyChanged(nameof(EyeColor));
                NotifyPropertyChanged(nameof(MakeupColor1));
                NotifyPropertyChanged(nameof(MakeupColor2));
                NotifyPropertyChanged(nameof(MakeupColor3));
            }

            NotifyPropertyChanged(nameof(TopColor1));
            NotifyPropertyChanged(nameof(TopColor2));
            NotifyPropertyChanged(nameof(TopColor3));
            NotifyPropertyChanged(nameof(TopColor4));
            NotifyPropertyChanged(nameof(BottomColor1));
            NotifyPropertyChanged(nameof(BottomColor2));
            NotifyPropertyChanged(nameof(BottomColor3));
            NotifyPropertyChanged(nameof(BottomColor4));
            NotifyPropertyChanged(nameof(GlovesColor1));
            NotifyPropertyChanged(nameof(GlovesColor2));
            NotifyPropertyChanged(nameof(GlovesColor3));
            NotifyPropertyChanged(nameof(GlovesColor4));
            NotifyPropertyChanged(nameof(ShoesColor1));
            NotifyPropertyChanged(nameof(ShoesColor2));
            NotifyPropertyChanged(nameof(ShoesColor3));
            NotifyPropertyChanged(nameof(ShoesColor4));
        }

        public void UpdateProperties()
        {
            NotifyPropertyChanged(nameof(CAC));
            NotifyPropertyChanged(nameof(Preset));
            NotifyPropertyChanged(nameof(SelectedPreset));
            NotifyPropertyChanged(nameof(AvatarHeight));
            NotifyPropertyChanged(nameof(AvatarBodyType));
        }


        public RelayCommand<int> SetColorCommand => new RelayCommand<int>(SelectColor);
        private void SelectColor(int group)
        {
            if (CAC == null) return;
            if (BcsFile.PartColors.FirstOrDefault(x => x.ID == group) == null) return;

            BcsColorSelector selector = new BcsColorSelector(BcsFile, group);
            selector.ShowDialog();

            if(selector.SelectedValue != -1)
            {
                switch ((CustomColorGroup.PartColorMaterials)group)
                {
                    case CustomColorGroup.PartColorMaterials.SKIN_:
                        CAC.Appearence.I_36 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(Skin1));
                        break;
                    case CustomColorGroup.PartColorMaterials.SKIN2:
                        CAC.Appearence.I_38 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(Skin2));
                        break;
                    case CustomColorGroup.PartColorMaterials.SKIN3:
                        CAC.Appearence.I_40 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(Skin3));
                        break;
                    case CustomColorGroup.PartColorMaterials.SKIN4:
                        CAC.Appearence.I_42 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(Skin4));
                        break;
                    case CustomColorGroup.PartColorMaterials.HAIR_:
                        CAC.Appearence.I_44 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(HairColor));
                        break;
                    case CustomColorGroup.PartColorMaterials.EYE_:
                        CAC.Appearence.I_46 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(EyeColor));
                        break;
                    case CustomColorGroup.PartColorMaterials.PAINT_A_:
                        CAC.Appearence.I_48 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(MakeupColor1));
                        break;
                    case CustomColorGroup.PartColorMaterials.PAINT_B_:
                        CAC.Appearence.I_50 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(MakeupColor2));
                        break;
                    case CustomColorGroup.PartColorMaterials.PAINT_C_:
                        CAC.Appearence.I_52 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(MakeupColor3));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC00_BUST_:
                        Preset.I_28 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(TopColor1));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC01_BUST_:
                        Preset.I_30 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(TopColor2));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC02_BUST_:
                        Preset.I_32 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(TopColor3));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC03_BUST_:
                        Preset.I_34 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(TopColor4));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC00_PANTS_:
                        Preset.I_36 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(BottomColor1));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC01_PANTS_:
                        Preset.I_38 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(BottomColor2));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC02_PANTS_:
                        Preset.I_40 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(BottomColor3));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC03_PANTS_:
                        Preset.I_42 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(BottomColor4));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC00_RIST_:
                        Preset.I_44 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(GlovesColor1));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC01_RIST_:
                        Preset.I_46 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(GlovesColor2));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC02_RIST_:
                        Preset.I_48 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(GlovesColor3));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC03_RIST_:
                        Preset.I_50 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(GlovesColor4));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC00_BOOTS_:
                        Preset.I_52 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(ShoesColor1));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC01_BOOTS_:
                        Preset.I_54 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(ShoesColor2));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC02_BOOTS_:
                        Preset.I_56 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(ShoesColor3));
                        break;
                    case CustomColorGroup.PartColorMaterials.CC03_BOOTS_:
                        Preset.I_58 = (ushort)selector.SelectedValue;
                        NotifyPropertyChanged(nameof(ShoesColor4));
                        break;
                }

                Avatar.IsColorsDirty = true;
            }
        }

        private void BodyParts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(Avatar != null)
            {
                Avatar.IsAppearenceDirty = true;
            }
        }
    }
}
