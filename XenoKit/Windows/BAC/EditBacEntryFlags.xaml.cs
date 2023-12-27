using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Windows;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Windows.BAC
{
    /// <summary>
    /// Interaction logic for EditBacEntryFlags.xaml
    /// </summary>
    public partial class EditBacEntryFlags : MetroWindow, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        BAC_Entry bacEntry;

        public bool Flag_Unk1
        {
            get
            {
                return bacEntry.Flag.HasFlag(BAC_Entry.Flags.unk1);
            }
            set
            {
                SetBacFlags(BAC_Entry.Flags.unk1, value);
                NotifyPropertyChanged(nameof(Flag_Unk1));
            }
        }
        public bool Flag_Unk2
        {
            get
            {
                return bacEntry.Flag.HasFlag(BAC_Entry.Flags.unk2);
            }
            set
            {
                SetBacFlags(BAC_Entry.Flags.unk2, value);
                NotifyPropertyChanged(nameof(Flag_Unk2));
            }
        }
        public bool Flag_Unk3
        {
            get
            {
                return bacEntry.Flag.HasFlag(BAC_Entry.Flags.unk3);
            }
            set
            {
                SetBacFlags(BAC_Entry.Flags.unk3, value);
                NotifyPropertyChanged(nameof(Flag_Unk3));
            }
        }
        public bool Flag_Unk7
        {
            get
            {
                return bacEntry.Flag.HasFlag(BAC_Entry.Flags.unk7);
            }
            set
            {
                SetBacFlags(BAC_Entry.Flags.unk7, value);
                NotifyPropertyChanged(nameof(Flag_Unk7));
            }
        }



        public EditBacEntryFlags(BAC_Entry bac, Window parent)
        {
            Owner = parent;
            bacEntry = bac;
            DataContext = this;
            InitializeComponent();
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            Closing += EditBacEntryFlags_Closing;
        }

        private void EditBacEntryFlags_Closing(object sender, CancelEventArgs e)
        {
            UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;
        }

        private void Instance_UndoOrRedoCalled(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(Flag_Unk1));
            NotifyPropertyChanged(nameof(Flag_Unk2));
            NotifyPropertyChanged(nameof(Flag_Unk3));
            NotifyPropertyChanged(nameof(Flag_Unk7));
        }

        private void SetBacFlags(BAC_Entry.Flags flag, bool state)
        {
            var newFlag = bacEntry.Flag.SetFlag(flag, state);

            if (bacEntry.Flag != newFlag)
            {
                UndoManager.Instance.AddUndo(new UndoableProperty<BAC_Entry>(nameof(BAC_Entry.Flag), bacEntry, bacEntry.Flag, newFlag, "BAC Entry Flags"));
                bacEntry.Flag = newFlag;
            }
        }

    }
}
