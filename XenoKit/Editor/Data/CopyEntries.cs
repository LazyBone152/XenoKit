using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Editor;
using XenoKit.Helper;
using Xv2CoreLib;
using Xv2CoreLib.ACB;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BCM;
using Xv2CoreLib.BDM;
using Xv2CoreLib.BSA;
using Xv2CoreLib.EAN;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Editor
{
    public class CopyEntries
    {
        public List<BAC_Entry> BacEntries { get; set; } = new List<BAC_Entry>();
        public List<BDM_Entry> BdmEntries { get; set; } = new List<BDM_Entry>();
        public List<BDM_Entry> ShotBdmEntries { get; set; } = new List<BDM_Entry>();
        public List<BSA_Entry> BsaEntries { get; set; } = new List<BSA_Entry>();
        public List<BCM_Entry> BcmEntries { get; set; } = new List<BCM_Entry>();
        //public List<EAN_Animation> Animations { get; set; } = new List<EAN_Animation>();
        public List<SerializedAnimation> Animations { get; set; } = new List<SerializedAnimation>();

        public List<EAN_Animation> Cameras { get; set; } = new List<EAN_Animation>();
        public List<Effect> Effects { get; set; } = new List<Effect>();
        public ACB_File SeAcbFile { get; set; } = ACB_File.NewXv2Acb();


    }

}
