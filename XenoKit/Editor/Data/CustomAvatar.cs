using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Engine;
using Xv2CoreLib;
using Xv2CoreLib.SAV;
using static Xv2CoreLib.Xenoverse2;

namespace XenoKit.Editor.Data
{
    public class CustomAvatar
    {
        private OutlinerItem OutlinerItem;

        public int CaCIndex { get; set; }
        public CaC CaC { get; set; }
        public Actor[] Actor { get; set; }
        public int Preset { get; set; }

        public int Race => (int)CaC.I_20;
        public int CmsID => (int)GetAvatarID(CaC.I_20);

        public bool IsAppearenceDirty { get; set; } = false;
        public bool IsColorsDirty { get; set; } = false;
        public bool IsSizeDirty { get; set; } = false;

        public CustomAvatar(int cacIndex, CaC cac, OutlinerItem parentItem)
        {
            OutlinerItem = parentItem;
            CaC = cac;
            Actor = new Actor[8];
            CaCIndex = cacIndex;
        }


        public void InitActor()
        {
            //Create actor
            if (Actor[Race] == null)
            {
                Xv2Character xv2Character = Xenoverse2.Instance.GetCharacter(CmsID);
                Actor[Race] = new Actor(SceneManager.MainGameBase, xv2Character, 0);
                Actor[Race].AnimationPlayer.PlayPrimaryAnimation(Actor[Race].CharacterData.MovesetFiles.EanFile[0].File, 0, autoTerminate: true);
            }

            OutlinerItem.character = Actor[Race];
            Actor[Race].Voice = CaC.I_24;

            switch (CaC.I_20)
            {
                case Xv2CoreLib.SAV.Race.HUF:
                case Xv2CoreLib.SAV.Race.SYF:
                case Xv2CoreLib.SAV.Race.MAF:
                    Actor[Race].SkillVoiceAlias = $"F{CaC.I_24.ToString("D2")}";
                    break;
                case Xv2CoreLib.SAV.Race.HUM:
                case Xv2CoreLib.SAV.Race.SYM:
                case Xv2CoreLib.SAV.Race.MAM:
                case Xv2CoreLib.SAV.Race.FRI:
                case Xv2CoreLib.SAV.Race.NMC:
                    Actor[Race].SkillVoiceAlias = $"M{CaC.I_24.ToString("D2")}";
                    break;
            }
        }

        public void SetActorAppearence()
        {
            Actor[Race].PartSet.ApplyTransformation(CaC.Appearence.I_132, Xv2CoreLib.BCS.PartTypeFlags.FaceBase, true);
            Actor[Race].PartSet.ApplyTransformation(CaC.Appearence.I_136, Xv2CoreLib.BCS.PartTypeFlags.FaceForehead, true);
            Actor[Race].PartSet.ApplyTransformation(CaC.Appearence.I_140, Xv2CoreLib.BCS.PartTypeFlags.FaceEye, true);
            Actor[Race].PartSet.ApplyTransformation(CaC.Appearence.I_144, Xv2CoreLib.BCS.PartTypeFlags.FaceNose, true);
            Actor[Race].PartSet.ApplyTransformation(CaC.Appearence.I_148, Xv2CoreLib.BCS.PartTypeFlags.FaceEar, true);
            Actor[Race].PartSet.ApplyTransformation(CaC.Appearence.I_152, Xv2CoreLib.BCS.PartTypeFlags.Hair, true);

            //ID refers to an IDB entry, not part set
            Actor[Race].PartSet.ApplyTransformation(Xenoverse2.Instance.GetTopPartSetID(CaC.Presets[Preset].I_00), Xv2CoreLib.BCS.PartTypeFlags.Bust, true);
            Actor[Race].PartSet.ApplyTransformation(Xenoverse2.Instance.GetTopPartSetID(CaC.Presets[Preset].I_04), Xv2CoreLib.BCS.PartTypeFlags.Pants, true);
            Actor[Race].PartSet.ApplyTransformation(Xenoverse2.Instance.GetTopPartSetID(CaC.Presets[Preset].I_12), Xv2CoreLib.BCS.PartTypeFlags.Boots, true);
            Actor[Race].PartSet.ApplyTransformation(Xenoverse2.Instance.GetTopPartSetID(CaC.Presets[Preset].I_08), Xv2CoreLib.BCS.PartTypeFlags.Rist, true);

        }

        public void SetCustomColors()
        {
            Actor[Race].PartSet.SetCacCustomColors(CaC, Preset);
            Actor[Race].PartSet.ApplyCustomColors();
        }

        public void SetActorSize()
        {
            int bodyId = CaC.Appearence.GetBcsBodyFromHeightWidth();
            var bcsBody = Actor[Race].CharacterData.BcsFile.File.Bodies.FirstOrDefault(x => x.ID == bodyId);

            if(bcsBody != null)
            {
                Actor[Race].Skeleton.SetBoneScale(bcsBody);
            }
            else
            {
                Log.Add($"BCS Body {bodyId} not found in character BCS.", LogType.Warning);
            }
        }

        private CustomCharacter GetAvatarID(Race race)
        {
            return (CustomCharacter)Enum.Parse(typeof(CustomCharacter), race.ToString());
        }

        public void Update()
        {
            if (IsSizeDirty)
            {
                SetActorSize();
                IsSizeDirty = false;
            }

            if (IsColorsDirty)
            {
                SetCustomColors();
                IsColorsDirty = false;
            }

            if (IsAppearenceDirty)
            {
                SetActorAppearence();
                SetCustomColors();
                IsAppearenceDirty = false;
            }
        }
    }
}
