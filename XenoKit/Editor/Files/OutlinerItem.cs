using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using Xv2CoreLib;
using Xv2CoreLib.ACB;
using Xv2CoreLib.EAN;
using xv2 = Xv2CoreLib.Xenoverse2;
using file = Xv2CoreLib.FileManager;
using XenoKit.Engine;
using Xv2CoreLib.BCS;
using Xv2CoreLib.EffectContainer;
using System.IO;
using System.Collections.Generic;
using XenoKit.Engine.Model;
using Xv2CoreLib.NSK;
using XenoKit.Editor.Data;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Xv2CoreLib.CUS;

namespace XenoKit.Editor
{
    public class OutlinerItem : INotifyPropertyChanged
    {
        #region INotifyPropChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public enum OutlinerItemType
        {
            CaC,
            Character,
            Skill,
            Moveset,
            CMN,
            Inspector,

            //Manual Loads Only:
            ACB,
            EEPK,
            EAN,
            CAM,
            STAGE_MANUAL //NSK + EMB + EMM 
        }

        public string ID => GetUniqueID();
        public OutlinerItemType Type { get; set; }
        public bool ReadOnly { get; set; }
        public bool IsManualLoaded { get; private set; } = false;
        public bool OnlyLoadFromCPK { get; private set; } = false;
        public bool CanDelete
        {
            get => Type != OutlinerItemType.CMN && Type != OutlinerItemType.Inspector;
        }

        //Data
        public Move move { get; set; }
        public Actor character { get; set; }
        public ManualFiles ManualFiles { get; set; }
        public CustomAvatar CustomAvatar { get; set; }

        //UI
        public string DisplayName
        {
            get
            {
                if (Type == OutlinerItemType.Inspector) return "";
                if (Type == OutlinerItemType.CMN) return "Common";
                if (IsManualLoaded) return ManualFiles.Name;
                if (Type == OutlinerItemType.CaC) return $"{CustomAvatar.CaC.Name}";
                return (Type == OutlinerItemType.Character) ? character.Name : move.Name;
            }
        }
        public string DisplayType
        {
            get
            {
                switch (Type)
                {
                    case OutlinerItemType.Inspector:
                        return "Viewer Mode";
                    default:
                        return Type.ToString().ToUpper();
                }
            }
        }
        public EditorVisibility Visibilities { get; set; }

        //Editor Helpers
        /// <summary>
        /// Can files (vox, ean, cam) be added in the editor?
        /// </summary>
        public bool CanAddFiles { get { return (IsManualLoaded || GetMove().MoveType == Move.Type.CMN || GetMove().MoveType == Move.Type.Moveset) ? false : true; } }
        public bool CanUseSystemTab { get { return (GetMove().MoveType == Move.Type.Skill && !IsManualLoaded); } }

        #region SelectedItems
        private Xv2File<EAN_File> _selectedEanFile = null;
        private Xv2File<EAN_File> _selectedCamFile = null;
        private Xv2File<ACB_Wrapper> _selectedSeAcbFile = null;
        private Xv2File<ACB_Wrapper> _selectedVoxAcbFile = null;
        private EAN_Animation _selectedAnimation = null;
        private EAN_Animation _selectedCamera = null;

        public Xv2File<EAN_File> SelectedEanFile
        {
            get { return _selectedEanFile; }
            set
            {
                if (_selectedEanFile != value)
                {
                    _selectedEanFile = value;
                    NotifyPropertyChanged(nameof(SelectedEanFile));
                }
            }
        }
        public Xv2File<EAN_File> SelectedCamFile
        {
            get { return _selectedCamFile; }
            set
            {
                if (_selectedCamFile != value)
                {
                    _selectedCamFile = value;
                    NotifyPropertyChanged(nameof(SelectedCamFile));
                }
            }
        }
        public Xv2File<ACB_Wrapper> SelectedSeAcbFile
        {
            get { return _selectedSeAcbFile; }
            set
            {
                if (_selectedSeAcbFile != value)
                {
                    _selectedSeAcbFile = value;
                    NotifyPropertyChanged(nameof(SelectedSeAcbFile));
                }
            }
        }
        public Xv2File<ACB_Wrapper> SelectedVoxAcbFile
        {
            get { return _selectedVoxAcbFile; }
            set
            {
                if (_selectedVoxAcbFile != value)
                {
                    _selectedVoxAcbFile = value;
                    NotifyPropertyChanged(nameof(SelectedVoxAcbFile));
                }
            }
        }
        public EAN_Animation SelectedAnimation
        {
            get { return _selectedAnimation; }
            set
            {
                if (_selectedAnimation != value)
                {
                    _selectedAnimation = value;
                    NotifyPropertyChanged(nameof(SelectedAnimation));
                }
            }
        }
        public EAN_Animation SelectedCamera
        {
            get { return _selectedCamera; }
            set
            {
                if (_selectedCamera != value)
                {
                    _selectedCamera = value;
                    NotifyPropertyChanged(nameof(SelectedCamera));
                }
            }
        }

        #endregion

        /// <summary>
        /// Load a file manually (directly from disk).
        /// </summary>
        /// <param name="path">The path to the file.</param>
        public OutlinerItem(string path)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".ean":
                    if (path.Contains(".cam"))
                    {
                        Type = OutlinerItemType.CAM;
                        ManualFiles = ManualFiles.LoadCam(path);
                    }
                    else
                    {
                        Type = OutlinerItemType.EAN;
                        ManualFiles = ManualFiles.LoadEan(path);
                    }
                    break;
                case ".acb":
                    Type = OutlinerItemType.ACB;
                    ManualFiles = ManualFiles.LoadAcb(path);
                    break;
                case ".eepk":
                case ".vfxpackage":
                    Type = OutlinerItemType.EEPK;
                    ManualFiles = ManualFiles.LoadEepk(path);
                    break;
                default:
                    throw new InvalidDataException($"OutlinerItem: The filetype of \"{path}\" is unsupported.");
            }

            IsManualLoaded = true;
            SetSelectedItems();
            Visibilities = new EditorVisibility(Type);
        }

        /// <summary>
        /// Load a file manually (directly from disk).
        /// </summary>
        /// <param name="path">The path to the file.</param>
        public OutlinerItem(string[] paths)
        {
            switch (Path.GetExtension(paths[0]).ToLower())
            {
                case ".nsk":
                    Type = OutlinerItemType.STAGE_MANUAL;
                    ManualFiles = ManualFiles.LoadNsk(paths);
                    break;
                default:
                    throw new InvalidDataException($"OutlinerItem: The filetype of \"{paths[0]}\" is unsupported for multi-file drop.");
            }

            IsManualLoaded = true;
            SetSelectedItems();
            Visibilities = new EditorVisibility(Type);
        }

        public OutlinerItem(Move move, bool readOnly, OutlinerItemType type, bool onlyLoadFromCpk) : this(readOnly, type, onlyLoadFromCpk)
        {
            this.move = move;
            SetSelectedItems();
        }

        public OutlinerItem(Actor chara, bool readOnly, OutlinerItemType type) : this(readOnly, type, false)
        {
            character = chara;
            SetSelectedItems();
        }

        public OutlinerItem(bool readOnly, OutlinerItemType type, bool onlyLoadFromCpk)
        {
            Type = type;
            ReadOnly = readOnly;
            OnlyLoadFromCPK = onlyLoadFromCpk;
            Visibilities = new EditorVisibility(type);
        }

        public OutlinerItem(int cacIndex, Xv2CoreLib.SAV.CaC cac)
        {
            CustomAvatar = new CustomAvatar(cacIndex, cac, this);
            Visibilities = new EditorVisibility(OutlinerItemType.CaC);
        }

        /// <summary>
        /// Fix references after pasting from clipboard.
        /// </summary>
        public void FixReferences()
        {
            if (move != null)
            {
                if (move.Files.SeAcbFile != null)
                {
                    foreach (var file in move.Files.SeAcbFile)
                    {
                        file.File.AcbFile.SetCommandTableVersion();
                        file.File = new ACB_Wrapper(file.File.AcbFile);
                    }
                }

                if (move.Files.VoxAcbFile != null)
                {
                    foreach (var file in move.Files.VoxAcbFile)
                    {
                        file.File.AcbFile.SetCommandTableVersion();
                        file.File = new ACB_Wrapper(file.File.AcbFile);
                    }
                }
            }
        }

        public Move GetMove()
        {
            if (IsManualLoaded) return ManualFiles.Move;

            return Type == OutlinerItemType.Character ? character.Moveset : move;
        }

        public bool SaveValidate(MetroWindow window)
        {
            Move move = GetMove();

            if(move != null)
                if (!move.SaveValidate(window)) return false;

            return true;
        }

        /// <summary>
        /// Call after loading - will set default selected items.
        /// </summary>
        private void SetSelectedItems()
        {
            SelectedEanFile = (GetMove().Files.EanFile.Count > 0) ? GetMove().Files.EanFile[0] : null;
            SelectedCamFile = (GetMove().Files.CamEanFile.Count > 0) ? GetMove().Files.CamEanFile[0] : null;
            SelectedSeAcbFile = (GetMove().Files.SeAcbFile.Count > 0) ? GetMove().Files.SeAcbFile[0] : null;
            SelectedVoxAcbFile = (GetMove().Files.VoxAcbFile.Count > 0) ? GetMove().Files.VoxAcbFile[0] : null;
        }

        public xv2.MoveType GetMoveType()
        {
            switch (Type)
            {
                case OutlinerItemType.Character:
                case OutlinerItemType.Moveset:
                    return xv2.MoveType.Character;
                case OutlinerItemType.Skill:
                    return xv2.MoveType.Character;
                case OutlinerItemType.CMN:
                    return xv2.MoveType.Common;
                default:
                    return 0;
            }
        }

        private string GetUniqueID()
        {
            if (IsManualLoaded) return null;

            switch (Type)
            {
                case OutlinerItemType.Moveset:
                    return $"CHARA_{move.CmsEntry.ShortName}";
                case OutlinerItemType.Character:
                    return $"CHARA_{character.ShortName}";
                case OutlinerItemType.Skill:
                    return $"SKILL_{move.CusEntry.ID1}";
                case OutlinerItemType.CaC:
                    return $"CAC_{CustomAvatar.CaC.Name}";
            }

            return null;
        }

        public void Update()
        {
            if(Type == OutlinerItemType.CaC)
            {
                CustomAvatar.Update();
            }
        }

    }

    public class ManualFiles
    {
        public string Name;

        //Just used for manual loaded BCS files right now
        public Xv2Character CharacterFiles { get; set; }

        //All moveset/skill related files:
        public Move Move { get; set; }

        //Stage Files:
        public List<StageModel> StageModels { get; set; }


        private ManualFiles(Xv2Character chara, string name)
        {
            Name = name;
            CharacterFiles = chara;
        }

        private ManualFiles(Xv2MoveFiles move, string name)
        {
            Name = name;
            Move = new Move(move);
        }

        private ManualFiles(List<StageModel> stages, string name)
        {
            StageModels = stages;
            Name = name;
            Move = new Move(new Xv2MoveFiles());
        }

        #region Load
        public static ManualFiles LoadEan(string path)
        {
            Xv2File<EAN_File> file = new Xv2File<EAN_File>(EAN_File.Load(path), path, true);
            Xv2MoveFiles move = new Xv2MoveFiles();
            move.EanFile.Add(file);

            //temp dem hack
            foreach(var anim in file.File.Animations)
            {
                var node = anim.GetNode("b_C_Base");

                if(node != null)
                {
                    node.RemoveComponent(EAN_AnimationComponent.ComponentType.Position);
                }
            }

            return new ManualFiles(move, Path.GetFileName(path));
        }

        public static ManualFiles LoadCam(string path)
        {
            Xv2File<EAN_File> file = new Xv2File<EAN_File>(EAN_File.Load(path), path, true);
            Xv2MoveFiles move = new Xv2MoveFiles();
            move.CamEanFile.Add(file);

            return new ManualFiles(move, Path.GetFileName(path));
        }

        public static ManualFiles LoadEepk(string path)
        {
            EffectContainerFile eepk = Path.GetExtension(path) == EffectContainerFile.ZipExtension ? EffectContainerFile.LoadVfxPackage(path) : EffectContainerFile.Load(path);

            Xv2File <EffectContainerFile> file = new Xv2File<EffectContainerFile>(eepk, path, true);
            Xv2MoveFiles move = new Xv2MoveFiles();
            move.EepkFile = file;

            return new ManualFiles(move, Path.GetFileName(path));
        }

        public static ManualFiles LoadAcb(string path)
        {
            Xv2File<ACB_Wrapper> file = new Xv2File<ACB_Wrapper>(new ACB_Wrapper(ACB_File.Load(path)), path, true);
            Xv2MoveFiles move = new Xv2MoveFiles();
            move.SeAcbFile.Add(file);

            return new ManualFiles(move, Path.GetFileName(path));
        }
        
        public static ManualFiles LoadNsk(string[] paths)
        {
            List<StageModel> models = new List<StageModel>();
            string name = "Unknown Stage";

            foreach(var path in paths)
            {
                name = Path.GetFileNameWithoutExtension(path);

                if(Path.GetExtension(path) == ".nsk")
                {
                    string embPath = string.Format("{0}/{1}.emb", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
                    string emmPath = string.Format("{0}/{1}.emm", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));

                    if (!File.Exists(embPath))
                    {
                        Log.Add($"{embPath} not found. Skipping load of this nsk.", LogType.Warning);
                        continue;
                    }

                    if (!File.Exists(emmPath))
                    {
                        Log.Add($"{emmPath} not found. Skipping load of this nsk.", LogType.Warning);
                        continue;
                    }

                    byte[] nsk = File.ReadAllBytes(path);
                    byte[] emb = File.ReadAllBytes(embPath);
                    byte[] emm = File.ReadAllBytes(emmPath);

                    models.Add(new StageModel(nsk, emm, emb, SceneManager.MainGameBase));
                }
            }

            return new ManualFiles(models, name);
        }
        #endregion

        #region Save
        public void Save()
        {
            if(Move?.Files?.EanFile?.Count > 0)
            {
                foreach (var file in Move.Files.EanFile)
                    file.File.Save(file.Path);
            }

            if (Move?.Files?.CamEanFile?.Count > 0)
            {
                foreach (var file in Move.Files.CamEanFile)
                    file.File.Save(file.Path);
            }

            if (Move?.Files?.SeAcbFile?.Count > 0)
            {
                foreach (var file in Move.Files.SeAcbFile)
                    file.File.AcbFile.Save(file.Path);
            }

            if(Move?.Files?.EepkFile != null)
            {
                Move.Files.EepkFile.File.Save();
            }
        }

        #endregion

        /*
        public static ManualFiles LoadBcs(string path)
        {
            //need to modify partset loading to load from a specific folder before the game
            Xv2File<BCS_File> bcsFile = new Xv2File<BCS_File>(BCS_File.Load(path), path, true);
            Xv2Character chara = new Xv2Character();
            chara.BcsFile = bcsFile;
        }
        */
    }
}
