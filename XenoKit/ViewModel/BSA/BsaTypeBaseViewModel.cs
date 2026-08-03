using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BSA
{
    /// <summary>
    /// Shared plumbing for every BSA subtype viewmodel, and the viewmodel for the Activation section.
    /// Mirrors BACTypeBaseViewModel, except that the typed viewmodels inherit this rather than duplicating
    /// the model/undo subscription 12 times.
    /// </summary>
    public class BsaTypeBaseViewModel : ObservableObject, IDisposable
    {
        protected readonly BSA_TypeBase bsaType;

        public IBsaType SourceType => bsaType;

        public ushort StartTime
        {
            get => bsaType.StartTime;
            set => SetValue(nameof(bsaType.StartTime), bsaType.StartTime, value, v => bsaType.StartTime = v, "BSA Start Time");
        }

        public ushort Duration
        {
            get => bsaType.Duration;
            set => SetValue(nameof(bsaType.Duration), bsaType.Duration, value, v => bsaType.Duration = v, "BSA Duration");
        }

        public BsaTypeBaseViewModel(BSA_TypeBase bsaType)
        {
            this.bsaType = bsaType;
            this.bsaType.PropertyChanged += BsaType_PropertyChanged;

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
        }

        public static BsaTypeBaseViewModel Create(IBsaType type)
        {
            switch (type)
            {
                case BSA_Type0 type0: return new BsaType0ViewModel(type0);
                case BSA_Type1 type1: return new BsaType1ViewModel(type1);
                case BSA_Type2 type2: return new BsaType2ViewModel(type2);
                case BSA_Type3 type3: return new BsaType3ViewModel(type3);
                case BSA_Type4 type4: return new BsaType4ViewModel(type4);
                case BSA_Type6 type6: return new BsaType6ViewModel(type6);
                case BSA_Type7 type7: return new BsaType7ViewModel(type7);
                case BSA_Type8 type8: return new BsaType8ViewModel(type8);
                case BSA_Type10 type10: return new BsaType10ViewModel(type10);
                case BSA_Type12 type12: return new BsaType12ViewModel(type12);
                case BSA_Type13 type13: return new BsaType13ViewModel(type13);
                case BSA_Type14 type14: return new BsaType14ViewModel(type14);
                default: return null;
            }
        }

        public virtual void Dispose()
        {
            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled -= Instance_UndoOrRedoCalled;

            bsaType.PropertyChanged -= BsaType_PropertyChanged;
        }

        private void Instance_UndoOrRedoCalled(object sender, EventArgs e)
        {
            UpdateProperties();
        }

        private void BsaType_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);
        }

        /// <summary>
        /// Re-raises every exposed property. Only needed after undo/redo, which writes to the model directly.
        /// </summary>
        protected virtual void UpdateProperties()
        {
            RaisePropertyChanged(() => StartTime);
            RaisePropertyChanged(() => Duration);
        }

        /// <summary>
        /// Records the undo, assigns through a typed delegate (no reflection on the forward path), then raises
        /// the property. No UndoGroup is passed: a group would wrap this in UndoGroupContainer, which is not
        /// IMergableUndo, and that disables UndoManager's 1 second same-field merge.
        /// </summary>
        /// <param name="modelProperty">Name of the property on the model, used by UndoableProperty to restore it.</param>
        /// <param name="viewModelProperty">Filled in by the compiler. Many viewmodel properties are renamed from
        /// the raw model field (PositionX for F_12), so the binding must be told the viewmodel name, not the model one.</param>
        protected void SetValue<TValue>(string modelProperty, TValue oldValue, TValue newValue, Action<TValue> assign, string undoName, [CallerMemberName] string viewModelProperty = null)
        {
            if (EqualityComparer<TValue>.Default.Equals(oldValue, newValue)) return;

            UndoManager.Instance.AddUndo(new UndoableProperty<BSA_TypeBase>(modelProperty, bsaType, oldValue, newValue, undoName));
            assign(newValue);
            RaisePropertyChanged(viewModelProperty);
            bsaType.RefreshType();
        }
    }
}
