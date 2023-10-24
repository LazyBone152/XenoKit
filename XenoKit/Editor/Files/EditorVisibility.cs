using System.Windows;
using static XenoKit.Editor.OutlinerItem;

namespace XenoKit.Editor
{
    public class EditorVisibility
    {
        private readonly OutlinerItemType type;

        //Types:
        public Visibility IsSkill { get { return (type == OutlinerItemType.Skill) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsMoveset { get { return (type == OutlinerItemType.Moveset) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsCharacter { get { return (type == OutlinerItemType.Character || type == OutlinerItemType.CaC) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsCommon { get { return (type == OutlinerItemType.CMN) ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsManual { get { return (type == OutlinerItemType.EAN || type == OutlinerItemType.CAM || type == OutlinerItemType.EEPK || type == OutlinerItemType.ACB || type == OutlinerItemType.STAGE_MANUAL) ? Visibility.Visible : Visibility.Collapsed; } }


        //MainTabs:
        public Visibility BcsVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility AnimationVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility StateVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility ActionVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility EffectVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility AudioVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility HitboxVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility ProjectileVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility CameraVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility SystemVisibility { get; private set; } = Visibility.Collapsed;

        //SubTabs:
        public Visibility ShotBdmVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility BdmVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility SeVisibility { get; private set; } = Visibility.Collapsed;
        public Visibility VoxVisibility { get; private set; } = Visibility.Collapsed;

        public EditorVisibility(OutlinerItemType type)
        {
            this.type = type;
            SetVisibilities();
        }

        private void SetVisibilities()
        {
            if (type == OutlinerItemType.CMN)
            {
                AnimationVisibility = Visibility.Visible;
                ActionVisibility = Visibility.Visible;
                EffectVisibility = Visibility.Visible;
                AudioVisibility = Visibility.Visible;
                SeVisibility = Visibility.Visible;
                HitboxVisibility = Visibility.Visible;
                ProjectileVisibility = Visibility.Visible;
                CameraVisibility = Visibility.Visible;
                BdmVisibility = Visibility.Visible;
                ShotBdmVisibility = Visibility.Visible;
            }
            else if (type == OutlinerItemType.Character || type == OutlinerItemType.Moveset)
            {
                AnimationVisibility = Visibility.Visible;
                ActionVisibility = Visibility.Visible;
                EffectVisibility = Visibility.Visible;
                AudioVisibility = Visibility.Visible;
                SeVisibility = Visibility.Visible;
                HitboxVisibility = Visibility.Visible;
                CameraVisibility = Visibility.Visible;

                if(type == OutlinerItemType.Character)
                {
                    BcsVisibility = Visibility.Visible;
                    VoxVisibility = Visibility.Visible;
                    SystemVisibility = Visibility.Visible;
                }
            }
            else if (type == OutlinerItemType.Skill)
            {
                AnimationVisibility = Visibility.Visible;
                ActionVisibility = Visibility.Visible;
                EffectVisibility = Visibility.Visible;
                AudioVisibility = Visibility.Visible;
                SeVisibility = Visibility.Visible;
                VoxVisibility = Visibility.Visible;
                HitboxVisibility = Visibility.Visible;
                ProjectileVisibility = Visibility.Visible;
                CameraVisibility = Visibility.Visible;
                SystemVisibility = Visibility.Visible;
                BdmVisibility = Visibility.Visible;
                ShotBdmVisibility = Visibility.Visible;
            }
            else if(type == OutlinerItemType.ACB)
            {
                AudioVisibility = Visibility.Visible;
                SeVisibility = Visibility.Visible;
            }
            else if (type == OutlinerItemType.EEPK)
            {
                EffectVisibility = Visibility.Visible;
            }
            else if (type == OutlinerItemType.EAN)
            {
                AnimationVisibility = Visibility.Visible;
            }
            else if (type == OutlinerItemType.CAM)
            {
                CameraVisibility = Visibility.Visible;
            }

        }

    }
}
