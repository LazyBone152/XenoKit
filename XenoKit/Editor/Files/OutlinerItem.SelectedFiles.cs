using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using Xv2CoreLib;
using Xv2CoreLib.ACB;
using Xv2CoreLib.EAN;
using xv2 = Xv2CoreLib.Xenoverse2;
using file = Xv2CoreLib.FileManager;
using XenoKit.Engine;
using Xv2CoreLib.EffectContainer;
using System.IO;
using System.Collections.Generic;
using XenoKit.Engine.Model;
using XenoKit.Editor.Data;
using Xv2CoreLib.BAC;
using Xv2CoreLib.BCM;
using Xv2CoreLib.BSA;
using XenoKit.Engine.Stage;

namespace XenoKit.Editor
{
    public partial class OutlinerItem : INotifyPropertyChanged
    {
        private Xv2File<BAC_File> _selectedBac = null;

        private Xv2File<BSA_File> _selectedBsa = null;

        private Xv2File<BCM_File> _selectedBcm = null;

        private Xv2File<EffectContainerFile> _selectedEepk = null;

        private Xv2File<EAN_File> _selectedEanFile = null;

        private Xv2File<EAN_File> _selectedCamFile = null;

        private Xv2File<ACB_Wrapper> _selectedSeAcbFile = null;

        private Xv2File<ACB_Wrapper> _selectedVoxAcbFile = null;

        private EAN_Animation _selectedAnimation = null;

        private EAN_Animation _selectedCamera = null;

        public Xv2File<BAC_File> SelectedBacFile
        {
            get { return _selectedBac; }
            set
            {
                if (_selectedBac != value)
                {
                    _selectedBac = value;
                    NotifyPropertyChanged(nameof(SelectedBacFile));
                }
            }
        }

        public Xv2File<BSA_File> SelectedBsaFile
        {
            get { return _selectedBsa; }
            set
            {
                if (_selectedBsa != value)
                {
                    _selectedBsa = value;
                    NotifyPropertyChanged(nameof(SelectedBsaFile));
                }
            }
        }

        public Xv2File<BCM_File> SelectedBcmFile
        {
            get { return _selectedBcm; }
            set
            {
                if (_selectedBcm != value)
                {
                    _selectedBcm = value;
                    NotifyPropertyChanged(nameof(SelectedBcmFile));
                }
            }
        }

        public Xv2File<EffectContainerFile> SelectedEepk
        {
            get
            {
                switch (Type)
                {
                    case OutlinerItemType.CMN:
                        return _selectedEepk;
                    case OutlinerItemType.Character:
                        return character.Moveset?.Files?.EepkFile;
                    default:
                        return ManualFiles != null ? ManualFiles.Move.Files?.EepkFile : move?.Files?.EepkFile;

                }
            }
            set
            {
                if (Type == OutlinerItemType.CMN && value != _selectedEepk)
                {
                    _selectedEepk = value;
                    NotifyPropertyChanged(nameof(SelectedEepk));
                }
            }
        }

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

    }
}
