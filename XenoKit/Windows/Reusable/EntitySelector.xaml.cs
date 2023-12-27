using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Xv2CoreLib;

namespace XenoKit.Windows
{
    /// <summary>
    /// Interaction logic for EntitySelector.xaml
    /// </summary>
    public partial class EntitySelector : MetroWindow, INotifyPropertyChanged
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
        
        public ObservableCollection<Xv2Item> Items { get; set; }
        private bool _isMultiSelect = false;

        public Xv2Item SelectedItem { get; set; }
        public List<Xv2Item> SelectedItems { get; set; }

        public string BooleanParameterName { get; set; }
        public string BooleanParameterToolTip { get; set; }
        public bool BooleanParameter { get; set; }

        public EntitySelector(IEnumerable<Xv2Item> items, string itemName, bool multiSelect = false)
        {
            _isMultiSelect = multiSelect;
            Items = new ObservableCollection<Xv2Item>(items);
            InitializeComponent();
            DataContext = this;
            Owner = Application.Current.MainWindow;
            Title = string.Format("Select {0}", itemName);
            listBox.SelectionMode = _isMultiSelect ? System.Windows.Controls.DataGridSelectionMode.Extended : System.Windows.Controls.DataGridSelectionMode.Single;
        }

        public RelayCommand SelectItemCommand => new RelayCommand(SelectItem, () => listBox.SelectedItem != null);
        private void SelectItem()
        {
            if (_isMultiSelect)
            {
                SelectedItems = listBox.SelectedItems.Cast<Xv2Item>().ToList();
                SelectedItem = SelectedItems[0];
            }
            else
            {
                SelectedItem = listBox.SelectedItem as Xv2Item;
            }
            Close();
        }

        #region Search
        private string _searchFilter = null;
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                _searchFilter = value;
                RefreshSearchResults();
                NotifyPropertyChanged(nameof(SearchFilter));
            }
        }

        private ListCollectionView _filterList = null;
        public ListCollectionView FilterList
        {
            get
            {
                if (_filterList == null && Items != null)
                {
                    _filterList = new ListCollectionView(Items);
                    _filterList.Filter = new Predicate<object>(SearchFilterCheck);
                }
                return _filterList;
            }
            set
            {
                if (value != _filterList)
                {
                    _filterList = value;
                    NotifyPropertyChanged(nameof(FilterList));
                }
            }
        }

        public bool SearchFilterCheck(object material)
        {
            if (string.IsNullOrWhiteSpace(SearchFilter)) return true;
            var item = material as Xv2Item;
            string searchParam = SearchFilter.ToLower();

            if (item != null)
            {
                if(item.Name != null)
                {
                    if (item.Name.ToLower().Contains(searchParam)) return true;
                }

                int num;
                if (int.TryParse(searchParam, out num))
                {
                    if (item.ID == num) return true;
                }

            }

            return false;
        }

        private void RefreshSearchResults()
        {
            if (_filterList == null)
                _filterList = new ListCollectionView(Items);

            _filterList.Filter = new Predicate<object>(SearchFilterCheck);
            NotifyPropertyChanged(nameof(FilterList));
        }

        public RelayCommand ClearSearchCommand => new RelayCommand(ClearSearch);
        private void ClearSearch()
        {
            SearchFilter = string.Empty;
        }
        #endregion

        public void SetBooleanParameter(string name, string tooltip)
        {
            BooleanParameterName = name;
            BooleanParameterToolTip = tooltip;
            NotifyPropertyChanged(nameof(BooleanParameterName));
            NotifyPropertyChanged(nameof(BooleanParameterToolTip));
            checkbox.Visibility = Visibility.Visible;
        }
    }
}
