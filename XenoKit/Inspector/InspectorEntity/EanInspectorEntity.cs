using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Editor;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EMA;

namespace XenoKit.Inspector.InspectorEntities
{
    public class EanInspectorEntity : InspectorEntity
    {
        public override string FileType => "Animations";
        public string DisplayName => System.IO.Path.GetFileName(Path);
        public EAN_File EanFile { get; private set; }

        private bool _isSecondaryAnim = false;
        public bool IsSecondaryAnimation
        {
            get => _isSecondaryAnim;
            set
            {
                if(_isSecondaryAnim != value)
                {
                    _isSecondaryAnim = value;
                    NotifyPropertyChanged(nameof(IsSecondaryAnimation));
                }
            }
        }

        public EanInspectorEntity(string path) : base(path)
        {
            Path = path;
            Load();
        }

        public override bool Load()
        {
            if(System.IO.Path.GetExtension(Path) == ".ema")
            {
                EMA_File ema = EMA_File.Load(Path);
                EanFile = ema.ConvertToEan(false);
            }
            else
            {
                EanFile = EAN_File.Load(Path);
            }

            IsSecondaryAnimation = false;

            if (EanFile?.Skeleton != null)
            {
                if(EanFile.Skeleton.NonRecursiveBones.Count > 2)
                {
                    if (EanFile.Skeleton.NonRecursiveBones[0].Name.Contains("FCE_BONE") || EanFile.Skeleton.NonRecursiveBones[1].Name.Contains("FCE_BONE"))
                        IsSecondaryAnimation = true;

                    if (EanFile.Skeleton.NonRecursiveBones[0].Name.Contains("FaceRootDummy") || EanFile.Skeleton.NonRecursiveBones[1].Name.Contains("FaceRootDummy"))
                        IsSecondaryAnimation = true;
                }
            }

            return true;
        }

        public override bool Save()
        {
            if (System.IO.Path.GetExtension(Path) == ".ema")
            {
                Log.Add("Save is not supported for ema files right now.");
                return false;
            }
            EanFile.Save(Path);
            return true;
        }
    }
}
