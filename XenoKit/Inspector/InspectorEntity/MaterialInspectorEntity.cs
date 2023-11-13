using System;
using Xv2CoreLib.EMM;

namespace XenoKit.Inspector.InspectorEntities
{
    public class MaterialInspectorEntity : InspectorEntity
    {
        public override string FileType => "Materials";
        public EMM_File EmmFile { get; private set; }

        public MaterialInspectorEntity(string path) : base(path)
        {
            Path = path;
            Load();
        }

        private MaterialInspectorEntity(MaterialInspectorEntity entity) : base(entity.Path)
        {
            EmmFile = entity.EmmFile;
        }

        public override bool Load()
        {
            EmmFile = EMM_File.LoadEmm(Path);
            return true;
        }

        public override bool Save()
        {
            EmmFile.SaveBinaryEmmFile(Path);
            return true;
        }

        public override InspectorEntity Clone()
        {
            return new MaterialInspectorEntity(this);
        }
    }
}
