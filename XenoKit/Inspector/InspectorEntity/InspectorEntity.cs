using System;
using System.ComponentModel;
using XenoKit.Engine;
using Xv2CoreLib.Resource;

namespace XenoKit.Inspector.InspectorEntities
{
    [Serializable]
    public class InspectorEntity : Entity, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public virtual string FileType { get; }
        public string FileName => System.IO.Path.GetFileName(Path);
        public string Path { get; set; }

        public bool HasSkinnedChildren
        {
            get
            {
                foreach(InspectorEntity entity in ChildEntities)
                {
                    if (entity is SkinnedInspectorEntity)
                        return true;
                }
                return false;
            }
        }

        private bool _visible = true;
        public bool Visible
        {
            get => _visible;
            set
            {
                if(_visible != value)
                {
                    _visible = value;
                    NotifyPropertyChanged(nameof(Visible));

                    foreach(InspectorEntity file in ChildEntities)
                    {
                        file.Visible = value;
                    }
                }
            }
        }

        public AsyncObservableCollection<InspectorEntity> ChildEntities { get; set; } = new AsyncObservableCollection<InspectorEntity>();

        public InspectorEntity(string path) : base(SceneManager.MainGameBase)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
            Path = path;
        }

        public virtual bool Load()
        {
            return false;
        }

        public void ReloadChildren()
        {
            foreach(InspectorEntity child in ChildEntities)
            {
                child.Load();
                child.ReloadChildren();
            }
        }

        public virtual bool Save()
        {
            return false;
        }

        public override void Update()
        {
            foreach(InspectorEntity child in ChildEntities)
            {
                child.Update();
            }
        }

        public virtual void Draw(DrawStage currentStage)
        {

        }

        public virtual InspectorEntity Clone()
        {
            throw new NotImplementedException("Only implemented on Material and Texture entities!");
        }
    }
}
