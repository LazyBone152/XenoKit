using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using XenoKit.Editor;
using XenoKit.Engine;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.Windows;
using Xv2CoreLib;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using GalaSoft.MvvmLight.CommandWpf;
using GongSolutions.Wpf.DragDrop;

namespace XenoKit.Views.TimeLines
{
    /// <summary>
    /// Interaction logic for BacTimeLine.xaml
    /// </summary>
    public partial class BacTimeLine : UserControl, ITimeLineParent, INotifyPropertyChanged, IDropTarget
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region DP
        public BAC_Entry BacEntry
        {
            get => (BAC_Entry)GetValue(BacEntryProperty);
            set => SetValue(BacEntryProperty, value);
        }

        public static readonly DependencyProperty BacEntryProperty = DependencyProperty.Register(nameof(BacEntry), typeof(BAC_Entry), typeof(BacTimeLine), new UIPropertyMetadata(null,  new PropertyChangedCallback(OnBacEntryChanged)));

        private static void OnBacEntryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is BacTimeLine timeLine)
            {
                timeLine.HandleBacEvent(e.OldValue as BAC_Entry, e.NewValue as BAC_Entry);
                timeLine.BacEntryChanged();
            }
        }

        public ITimeLineItem SelectedItem
        {
            get => (ITimeLineItem)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(ITimeLineItem), typeof(BacTimeLine), new UIPropertyMetadata(null, new PropertyChangedCallback(OnSelectedItemChanged)));

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BacTimeLine timeLine)
            {
                //timeLine.SetSelectedItem(e.NewValue as ITimeLineItem, false);
            }
        }
        #endregion

        public event SelectionChangedEventHandler SelectionChanged;

        public UndoGroup UndoGroup => UndoGroup.Action;

        private readonly Line currentFrameLine;
        private double _unitSize = 50;
        private bool isFullyZoomedOut = false;
        private int _seekFrame = -1;

        public int SelectedFrame
        {
            get
            {
                if(SceneManager.Actors[0] != null)
                    return _seekFrame != -1 ? _seekFrame : SceneManager.Actors[0].ActionControl.BacPlayer.CurrentFrame;

                return 0;
            }
            set
            {
                _seekFrame = !SceneManager.IsPlaying && SceneManager.Actors[0] != null ? value : -1;
                UpdateCurrentFrame();
            }
        }
        public double UnitSize
        {
            get => _unitSize;
            set
            {
                UpdateUnitSize(value);
            }
        }
        public int CurrentLength { get; private set; }
        public double CurrentWidth { get; private set; } = 100;
        public double CurrentHeight => Layers.Count * 40;

        public Brush SelectedBrush => Brushes.White;
        public Brush SelectedTextBrush => Brushes.Black;

        public List<ITimeLineItem> SelectedItems { get; } = new List<ITimeLineItem>();

        public ObservableCollection<TimeLineLayer<IBacType>> Layers { get; set; } = new ObservableCollection<TimeLineLayer<IBacType>>();

        public BacTimeLine()
        {
            InitializeComponent();
            DataContext = this;
            mainScroll.SizeChanged += MainScroll_SizeChanged;
            UndoManager.Instance.UndoOrRedoCalled += Instance_UndoOrRedoCalled;
            SceneManager.BacDataChanged += SceneManager_BacDataChanged;
            SceneManager.DelayedUpdate += DelayedUpdate;
            Game.GameUpdate += new EventHandler(GameUpdate);

            currentFrameLine = new Line();
            currentFrameLine.Stroke = Brushes.White;
            currentFrameLine.StrokeThickness = 4;
            currentFrameLine.Y1 = -20;
            currentFrameLine.Y2 = 20;

            DrawFrameMarkings();
        }

        private void HandleBacEvent(BAC_Entry oldEntry, BAC_Entry newEntry)
        {
            if (oldEntry != null)
                oldEntry.IBacTypes.CollectionChanged -= IBacTypes_CollectionChanged;

            if (newEntry != null)
                newEntry.IBacTypes.CollectionChanged += IBacTypes_CollectionChanged;
        }

        public void BacEntryChanged()
        {
            RemoveSelectedItems();

            if (BacEntry != null)
            {
                SetSelectedStateOnItems();
                BacEntry.SortTimeLineLayers();
                InitControl();
            }
        }

        /// <summary>
        /// Attempt to focus the main timeline. This is required for the keyboard shortcuts to work properly.
        /// </summary>
        public void TryGetFocus()
        {
            Focus();
        }

        #region UpdateLoop
        private void DelayedUpdate(object sender, EventArgs e)
        {
            if(_seekFrame != -1)
            {
                switch (SceneManager.CurrentSceneState)
                {
                    case EditorTabs.Action:
                        SceneManager.Actors[0].ActionControl.BacPlayer.Seek(_seekFrame);
                        break;
                }

                _seekFrame = -1;
            }
        }

        public void GameUpdate(object sender, EventArgs arg)
        {
            UpdateCurrentFrame();
        }
        #endregion

        #region Layers
        public TimeLineLayer<IBacType> AddLayer(int layerGroup)
        {
            if (BacEntry == null) return null;

            int layer = 0;

            while (BacEntry.DoesLayerExist(layer, layerGroup))
            {
                layer++;
            }

            return AddLayer(layer, layerGroup);
        }

        private TimeLineLayer<IBacType> AddLayer(int layer, int layerGroup)
        {
            TimeLineLayer<IBacType> timeLineLayer = Layers.FirstOrDefault(x => x.Layer == layer && x.LayerGroup == layerGroup);

            if (timeLineLayer != null)
                return timeLineLayer;

            //If layer doesn't exist, then we need to create one and add it to the correct position in the list.
            timeLineLayer = CreateTimeLineLayer(layer, layerGroup);
            int insertIdx = Layers.Count;

            for (int i = 0; i < Layers.Count; i++)
            {
                if ((Layers[i].Layer > layer && Layers[i].LayerGroup == layerGroup) || (Layers[i].LayerGroup > layerGroup))
                {
                    insertIdx = i;
                    break;
                }
            }

            if (Layers.Count <= insertIdx)
                Layers.Add(timeLineLayer);
            else
                Layers.Insert(insertIdx, timeLineLayer);

            RefreshControl();
            return timeLineLayer;
        }

        private TimeLineLayer<IBacType> CreateTimeLineLayer(int layer, int layerGroup)
        {
            TimeLineLayer<IBacType> timeLinelayer = new TimeLineLayer<IBacType>(this);
            timeLinelayer.ParentItemsControl = itemsControl;
            timeLinelayer.Height = 40;
            timeLinelayer.HorizontalAlignment = HorizontalAlignment.Left;
            timeLinelayer.ItemTemplate = Resources["UsedTemplateProperty"] as DataTemplate;
            timeLinelayer.Background = Brushes.Transparent;
            timeLinelayer.SetLayerContext(layer, layerGroup, BacEntry?.IBacTypes);
            timeLinelayer.ToolTip = $"Layer: {layer}, LayerGroup: {layerGroup}";

            return timeLinelayer;
        }

        /// <summary>
        /// Adds an item to the layer assigned to it.
        /// </summary>
        /// <returns>A bool specifying if there has been a change to the layers (removed/added). If there has been a change, then the control will need to be refreshed. </returns>
        private bool AddToLayer(ITimeLineItem item)
        {
            bool layersChanged = false;

            var layer = Layers.FirstOrDefault(x => x.Layer == item.Layer && x.LayerGroup == item.LayerGroup);

            if (layer == null)
            {
                layer = AddLayer(item.Layer, item.LayerGroup);
                layersChanged = true;
            }

            layer.AddItem(item);

            return layersChanged;
        }

        /// <summary>
        /// Removes an item from any layer that it is currently on.
        /// </summary>
        /// <returns>A bool specifying if there has been a change to the layers (removed/added). If there has been a change, then the control will need to be refreshed. </returns>
        private bool RemoveFromLayer(ITimeLineItem item, bool removeEmptyLayers = true)
        {
            bool layersChanged = false;

            foreach(var layer in Layers)
            {
                if (layer.ContainsItem(item))
                {
                    layer.RemoveItem(item);

                    if (!BacEntry.DoesLayerExist(layer.Layer, layer.LayerGroup) && removeEmptyLayers)
                    {
                        Layers.Remove(layer);
                        layersChanged = true;
                        break;
                    }
                }
            }

            return layersChanged;
        }

        private void ChangeLayer(int oldLayer, int newLayer, int layerGroup)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            bool layerExists = BacEntry.DoesLayerExist(newLayer, layerGroup);
            bool layersChanged = false;

            //If items already exist on the new layer, move them onto a temp layer
            if (layerExists)
            {
                foreach(var type in BacEntry.IBacTypes.Where(x => x.Layer == newLayer && x.LayerGroup == layerGroup))
                {
                    RemoveFromLayer(type);
                    type.Layer = int.MaxValue;
                    undos.Add(new UndoablePropertyGeneric(nameof(type.Layer), type, newLayer, oldLayer));
                }
            }

            //Move items onto new layer
            foreach (var type in BacEntry.IBacTypes.Where(x => x.Layer == oldLayer && x.LayerGroup == layerGroup))
            {
                type.Layer = newLayer;

                if (RemoveFromLayer(type))
                    layersChanged = true;

                AddToLayer(type);
                undos.Add(new UndoablePropertyGeneric(nameof(type.Layer), type, oldLayer, newLayer));
            }

            //Move the items that were previously on the new layer onto the old layer
            if (layerExists)
            {
                foreach (var type in BacEntry.IBacTypes.Where(x => x.Layer == int.MaxValue && x.LayerGroup == layerGroup))
                {
                    type.Layer = oldLayer;
                    AddToLayer(type);
                }
            }

            if (layersChanged)
                RefreshControl();

            UndoManager.Instance.AddCompositeUndo(undos, "Move Layer", UndoGroup, "DataChanged", BacEntry);
        }

        private void ChangeLayer(List<ITimeLineItem> items, int newLayer, bool addUndo)
        {
            bool layerChange = false;
            int oldLayer = items[0].Layer;

            List<IUndoRedo> undos = addUndo ? new List<IUndoRedo>() : null;

            foreach(var item in items)
            {
                if (RemoveFromLayer(item))
                    layerChange = true;

                item.Layer = newLayer;

                if (AddToLayer(item))
                    layerChange = true;

                undos?.Add(new UndoablePropertyGeneric(nameof(ITimeLineItem.Layer), item, oldLayer, newLayer));
            }

            if (layerChange)
                RefreshControl();

            if(addUndo)
                UndoManager.Instance.AddCompositeUndo(undos, "Layer", UndoGroup, "DataChanged", BacEntry);
        }

        private IUndoRedo ChangeLayer(ITimeLineItem item, int newLayer, bool removeEmptyLayers = true)
        {
            bool layerChange = false;
            int oldLayer = item.Layer;

            if (RemoveFromLayer(item, removeEmptyLayers))
                layerChange = true;

            item.Layer = newLayer;

            if (AddToLayer(item))
                layerChange = true;

            if (layerChange)
                RefreshControl();

            return new UndoablePropertyGeneric(nameof(ITimeLineItem.Layer), item, oldLayer, newLayer, "Layer");
        }

        public void DeleteLayer(int layer, int layerGroup)
        {
            if (layer == 0 && layerGroup == 0) return;

            if(BacEntry != null)
            {
                //The last animation layer cannot be deleted as a BAC entry requires an animation track to function
                if (layer == 0 && Layers.Where(x => x.Layer == 0).Count() == 1)
                    return;

                //Remove all BAC types from this layer
                List<IUndoRedo> undos = new List<IUndoRedo>();

                for (int i = BacEntry.IBacTypes.Count - 1; i >= 0; i--)
                {
                    if (BacEntry.IBacTypes[i].Layer == layer && BacEntry.IBacTypes[i].LayerGroup == layerGroup)
                    {
                        undos.Add(new UndoableListRemove<IBacType>(BacEntry.IBacTypes, BacEntry.IBacTypes[i], i));
                        BacEntry.IBacTypes.RemoveAt(i);
                    }
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Delete Action Layer", undoArg: "DataChanged", undoContext: BacEntry);

                //Remove layer
                Layers.Remove(Layers.FirstOrDefault(x => x.Layer == layer && x.LayerGroup == layerGroup));
                RefreshControl();
            }

        }

        private IBacType GetNextItemInLayer(IBacType item)
        {
            foreach (IBacType entry in BacEntry.IBacTypes.Where(x => x.Layer == item.Layer && x.LayerGroup == item.LayerGroup && x.StartTime > item.TimeLine_StartTime).OrderBy(x => x.StartTime))
            {
                return entry;
            }

            return null;
        }

        public void OnItemManipulation()
        {
            UpdateLength();
        }

        private bool IsMaxLayer(int layer, int layerGroup)
        {
            foreach(var _layer in Layers.Where(x => x.LayerGroup == layerGroup))
            {
                if (_layer.Layer > layer) return false;
            }

            return true;
        }
        #endregion

        #region Control Updating / Visuals
        private void InitControl()
        {
            Layers.Clear();
            AddLayer(0, 0); //Mandatory animation layer

            foreach (IBacType bacType in BacEntry.IBacTypes)
            {
                AddLayer(bacType.Layer, bacType.LayerGroup);
            }

            UpdateLength();
            UpdateUnitSize(isFullyZoomedOut ? 1 : UnitSize, true);
            timeScroll.ScrollToHorizontalOffset(mainScroll.HorizontalOffset);
        }

        private void RefreshControl()
        {
            NotifyPropertyChanged(nameof(CurrentHeight));
            UpdateUnitSize(UnitSize, true);
        }

        private void UpdateUnitSize(double value, bool forceUpdate = false)
        {
            double minUnitSize = MathHelpers.Clamp(1, 100, mainScroll.ActualWidth / CurrentLength);
            double newUnitSize = MathHelpers.Clamp(minUnitSize, 100.0, value);

            if (_unitSize != newUnitSize || forceUpdate)
            {
                _unitSize = newUnitSize;
                CurrentWidth = CurrentLength * newUnitSize;
                ResizeAllLayers();
                NotifyPropertyChanged(nameof(UnitSize));
                NotifyPropertyChanged(nameof(CurrentWidth));
            }
            else
            {
                CurrentWidth = CurrentLength * newUnitSize;
                NotifyPropertyChanged(nameof(CurrentWidth));
            }

            isFullyZoomedOut = minUnitSize == _unitSize;
        }

        public void UpdateLength()
        {
            if(BacEntry == null)
            {
                CurrentLength = 0;
                return;
            }

            int length = BacEntry.GetTimeLineLength();

            if (SceneManager.Actors[0] != null)
            {
                int duration = BacEntryInstance.CalculateEntryDuration(BacEntry, Editor.Files.Instance.SelectedMove, SceneManager.Actors[0]);

                if (duration > length)
                    length = duration;
            }

            if(CurrentLength != length)
            {
                CurrentLength = length;
                NotifyPropertyChanged(nameof(CurrentLength));
                RefreshControl();
            }
        }

        private void ResizeAllLayers()
        {
            foreach (TimeLineLayer<IBacType> layer in Layers)
            {
                layer.UpdateUnitSize();
                layer.Width = CurrentWidth;
            }

            DrawFrameMarkings();

            //Lines:
            const double FrameFadeOutStart = 40;
            const double FrameFadeOutEnd = 5;
            const double SecondFadeInStart = 30;
            const double SecondFadeInEnd = 0;

            //Calculate fade out/in oppacities for the frame and second lines
            if (UnitSize > FrameFadeOutStart && UnitSize <= 100)
            {
                canvasForFrameLines.Opacity = 1f;
            }
            else if(UnitSize <= FrameFadeOutStart && UnitSize > FrameFadeOutEnd)
            {
                double fadeLength = FrameFadeOutStart - FrameFadeOutEnd;
                double factor = 1.0 -(Math.Abs(UnitSize - FrameFadeOutStart) / fadeLength);
                canvasForFrameLines.Opacity = MathHelpers.Clamp(0.0001f, 1f, (1f * factor));
            }
            else
            {
                //DO NOT set to 0, or the scroll viewer will break... NO idea why
                canvasForFrameLines.Opacity = 0.0001f;
            }

            if (UnitSize > SecondFadeInStart)
            {
                canvasForSecondLines.Opacity = 0.0000001f;
            }
            else if (UnitSize <= SecondFadeInStart && UnitSize > SecondFadeInEnd)
            {
                double fadeLength = SecondFadeInStart - SecondFadeInEnd;
                double factor = Math.Abs(UnitSize - SecondFadeInStart) / fadeLength;
                canvasForSecondLines.Opacity = (0.7f * factor);
            }
            else
            {
                canvasForSecondLines.Opacity = 0.7f;
            }

            DrawFrameLines(canvasForFrameLines, 1, UnitSize, 1, UnitSize, Brushes.Gray, 1);
            DrawFrameLines(canvasForSecondLines, 60, UnitSize * 60, 60, UnitSize * 60, Brushes.LightGray, 2);  
        }

        private void DrawFrameLines(Canvas canvas, int firstLineTime, double firstLineDistance, int timeStep, double unitSize, Brush brush, int lineThickness)
        {
            if (Layers.Count == 0 || canvas.Opacity == 0)
            {
                canvas.Children.Clear();
                return;
            }

            canvas.Width = CurrentWidth;
            canvas.Children.Clear();

            double curX = firstLineDistance;
            int curDate = firstLineTime;
            int curLine = 0;

            while (curX < canvas.Width)
            {
                Line l = new Line();
                l.StrokeThickness = lineThickness;
                l.Stroke = brush;
                l.X1 = 0;
                l.X2 = 0;
                l.Y1 = 0;
                //l.Y2 = Math.Max(DesiredSize.Height, canvas.Height);
                l.Y2 = canvas.Height;
                canvas.Children.Add(l);
                Canvas.SetLeft(l, curX);
                curX += unitSize;
                curDate += timeStep;
                curLine++;
            }
        }
        
        private void DrawFrameMarkings()
        {
            Canvas canvas = canvasForFrameMarkings;
            canvas.Children.Clear();
            canvas.Children.Add(currentFrameLine);

            if (Layers.Count == 0)
            {
                return;
            }

            double curX = UnitSize;
            int currentFrame = 1;
            int curLine = 0;

            while (curX < canvas.Width)
            {
                //Conditionally write numbers based on the current zoom level
                bool writeNumber = false;
                bool isBold = (currentFrame / 60f == (int)(currentFrame / 60));

                if (UnitSize > 22)
                {
                    //Show frame number each frame
                    writeNumber = true;
                }
                else if(UnitSize >= 5)
                {
                    //Show frame number each 5 frames
                    if(currentFrame / 5f == (int)(currentFrame / 5))
                    {
                        writeNumber = true;
                    }
                }
                else
                {
                    //Show frame number each 60 frames
                    if (currentFrame / 60f == (int)(currentFrame / 60))
                    {
                        writeNumber = true;
                    }
                }

                if (writeNumber)
                {
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = currentFrame.ToString();
                    textBlock.FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;
                    canvas.Children.Add(textBlock);
                    Canvas.SetLeft(textBlock, curX);
                }

                curX += UnitSize;
                currentFrame++;
                curLine++;
            }

            UpdateCurrentFrame();
        }
        
        private void UpdateCurrentFrame()
        {
            Canvas.SetLeft(currentFrameLine, SelectedFrame * UnitSize);
            NotifyPropertyChanged(nameof(SelectedFrame));
        }
        
        public int GetGhostDuration(ITimeLineItem timelineItem)
        {
            if(timelineItem is BAC_Type0 animation)
            {
                if (BAC_Type0.IsFullBodyAnimation(animation.EanType))
                {
                    IBacType nextItem = GetNextItemInLayer(animation);

                    int maxGhostDuration = nextItem != null ? nextItem.StartTime - (animation.StartTime + animation.Duration) : 10000;
                    int realAnimationDuration = BacEntryInstance.CalculateAnimationDuration(Files.Instance.SelectedMove, SceneManager.Actors[0], animation);

                    return Math.Min(maxGhostDuration, realAnimationDuration - animation.Duration);
                }
            }

            return 0;
        }

        private void UpdateItemsPlacement(ITimeLineItem item)
        {
            TimeLineLayer<IBacType> layer = Layers.FirstOrDefault(x => x.Layer == item.Layer && x.LayerGroup == item.LayerGroup);

            if (layer != null)
            {
                layer.UpdateItemsTiming(item);
            }
        }

        #endregion

        #region Scrolling
        private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                int factor = UnitSize <= 10 ? 100 : 50;
                UnitSize += e.Delta / factor;
                timeScroll.ScrollToHorizontalOffset(mainScroll.HorizontalOffset);

                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                mainScroll.ScrollToHorizontalOffset(mainScroll.HorizontalOffset + (-e.Delta * 4f));
                timeScroll.ScrollToHorizontalOffset(mainScroll.HorizontalOffset);
                e.Handled = true;
            }

        }

        private void mainScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            timeScroll.ScrollToHorizontalOffset(mainScroll.HorizontalOffset);
        }

        private void MainScroll_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(e.WidthChanged)
                UpdateUnitSize(UnitSize);
        }
        #endregion

        #region Data Changed Events
        //Update length when an edit is made to start or duration on any BAC types.

        private void SceneManager_BacDataChanged(object sender, EventArgs e)
        {
            if (BacEntry != null)
            {
                UpdateLength();
            }
        }

        private void Instance_UndoOrRedoCalled(object source, UndoEventRaisedEventArgs e)
        {
            if (BacEntry == null) return;

            if(e.UndoGroup == UndoGroup)
            {
                //Update StartTime or Duration edited from the view model or a stretch action in the timeline
                if((e.UndoArg == nameof(BAC_TypeBase.StartTime) || e.UndoArg == nameof(BAC_TypeBase.Duration) || e.UndoArg == "Stretch") && BacEntry.IBacTypes.Contains(e.UndoContext))
                {
                    UpdateLength();

                    //Update placement and sizing in grid
                    if (e.UndoContext is ITimeLineItem item)
                    {
                        UpdateItemsPlacement(item);
                    }

                    //If this is a Animation layer (Ghost Trail enabled), then all preceeding items must be updated as well.
                    if (e.UndoContext is BAC_Type0 type0)
                    {
                        foreach (var itm in BacEntry.IBacTypes.Where(x => x.LayerGroup == type0.LayerGroup && x.Layer == type0.Layer && x.StartTime < type0.StartTime))
                        {
                            UpdateItemsPlacement(itm);
                        }
                    }
                }

                //Update StartTime when an item was moved in the timeline
                if(e.UndoArg == "Move" && e.UndoContext is List<ITimeLineItem> items)
                {
                    UpdateLength();

                    //Update placement and sizing in grid
                    foreach(var item in items)
                    {
                        UpdateItemsPlacement(item);
                    }
                }
            
                //Animation parameters have changed on an animation
                if(e.UndoArg == "Animation" && e.UndoContext is BAC_Type0 anim)
                {
                    UpdateItemsPlacement(anim);
                }

                //Generic data changed undo event. In this case we will just reset most of the controls state, but preserve currently selected items
                if(e.UndoArg == "DataChanged" && e.UndoContext == BacEntry)
                {
                    InitControl();
                }

            }
        }

        private void IBacTypes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            bool layersChanged = false;

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is ITimeLineItem timelineItem)
                    {
                        if (timelineItem.Layer == -1)
                            BacEntry.SortTimeLineLayer(timelineItem as IBacType);

                        if (AddToLayer(timelineItem))
                            layersChanged = true;
                    }
                }

            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    if (RemoveFromLayer(item as ITimeLineItem))
                        layersChanged = true;
                }
            }

            SetSelectedStateOnItems();

            if (layersChanged)
            {
                RefreshControl();
            }
        }
        #endregion

        #region DragSelection
        private bool leftMouseDown = false;
        private Point mouseDownPos;

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                // Capture and track the mouse.
                leftMouseDown = true;
                mouseDownPos = e.GetPosition(mainScollGrid);
                mainScollGrid.CaptureMouse();

                // Initial placement of the drag selection box.         
                Canvas.SetLeft(selectionBox, mouseDownPos.X);
                Canvas.SetTop(selectionBox, mouseDownPos.Y);
                selectionBox.Width = 0;
                selectionBox.Height = 0;

                // Make the drag selection box visible.
                selectionBox.Visibility = Visibility.Visible;
            }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (leftMouseDown)
            {
                // Release the mouse capture and stop tracking it.
                leftMouseDown = false;
                mainScollGrid.ReleaseMouseCapture();

                // Hide the drag selection box.
                selectionBox.Visibility = Visibility.Collapsed;

                Point mouseUpPos = e.GetPosition(mainScollGrid);
                Rect rect = new Rect(mouseDownPos, mouseUpPos);

                //Check if any items in any layers should be selected
                List<ITimeLineItem> newSelectedItems = new List<ITimeLineItem>();

                foreach (TimeLineLayer<IBacType> layer in Layers)
                {
                    foreach (object item in layer.Children)
                    {
                        if (item is TimeLineItemControl timelineItem)
                        {
                            var point = timelineItem.TransformToAncestor(itemsControl).Transform(default);

                            if (rect.Contains(point))
                            {
                                //if(timelineItem.DataContext is ITimeLineItem itm)
                                //{
                                //    Log.Add($"Selected Type {itm.LayerGroup}, with StartTime: {itm.TimeLine_StartTime}");
                                //}
                                if (timelineItem.DataContext is ITimeLineItem itm)
                                {
                                    newSelectedItems.Add(itm);
                                }
                            }
                        }
                    }
                }

                SetSelectedItems(newSelectedItems, Keyboard.IsKeyDown(Key.LeftCtrl));
            }
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (leftMouseDown)
            {
                // When the mouse is held down, reposition the drag selection box.

                Point mousePos = e.GetPosition(mainScollGrid);

                if (mouseDownPos.X < mousePos.X)
                {
                    Canvas.SetLeft(selectionBox, mouseDownPos.X);
                    selectionBox.Width = mousePos.X - mouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(selectionBox, mousePos.X);
                    selectionBox.Width = mouseDownPos.X - mousePos.X;
                }

                if (mouseDownPos.Y < mousePos.Y)
                {
                    Canvas.SetTop(selectionBox, mouseDownPos.Y);
                    selectionBox.Height = mousePos.Y - mouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(selectionBox, mousePos.Y);
                    selectionBox.Height = mouseDownPos.Y - mousePos.Y;
                }
            }
        }
        #endregion

        #region Selection
        public void SetSelectedItem(ITimeLineItem item, bool append, bool rightClickSelection = false)
        {
            if (rightClickSelection)
            {
                if (!SelectedItems.Contains(item))
                {
                    SetSelectedItem(item, false);
                }

                return;
            }
            else if (!append)
            {
                SelectedItems.Clear();
                SelectedItems.Add(item);
                SelectedItem = item;
            }
            else
            {
                if (SelectedItems.Contains(item))
                {
                    SelectedItems.Remove(item);
                    SelectedItem = SelectedItems.Count > 0 ? SelectedItems[0] : null;
                }
                else
                {
                    SelectedItems.Add(item);
                    SelectedItem = SelectedItems.Count > 0 ? SelectedItems[0] : null;
                }
            }

            SelectionChanged?.Invoke(this, null);
            SetSelectedStateOnItems();
        }

        private void SetSelectedItems(IList<ITimeLineItem> items, bool append)
        {
            if (!append)
            {
                SelectedItems.Clear();

                if (items.Count == 0)
                    SelectedItem = null;
            }

            foreach(ITimeLineItem item in items)
            {
                if (!SelectedItems.Contains(item))
                {
                    SelectedItems.Add(item);
                }
            }

            SelectedItem = SelectedItems.Count > 0 ? SelectedItems[0] : null;

            SelectionChanged?.Invoke(this, null);
            SetSelectedStateOnItems();
        }

        private void RemoveSelectedItems()
        {
            SelectedItems.Clear();
            SelectedItem = null;
            SelectionChanged?.Invoke(this, null);
            SetSelectedStateOnItems();
        }

        private void SetSelectedStateOnItems()
        {
            if (BacEntry == null) return;

            foreach(ITimeLineItem type in BacEntry.IBacTypes)
            {
                type.TimeLine_IsSelected = SelectedItems.Contains(type);
            }
        }
        
        public void ScrollIntoView(ITimeLineItem item)
        {
            //TODO: the thing
        }
        #endregion

        public string GetLayerName(int layerGroup)
        {
            string name;
            if (BAC_File.BacTypeNames.TryGetValue(layerGroup, out name))
                return name;
            return "UNKNOWN LAYER TYPE";
        }

        #region LayerContextMenu
        private int _contextMenuFrame;
        private int _contextMenuLayerIdx = -1;
        private TimeLineLayer<IBacType> _selectedLayer = null;

        public string ContextMenuLayerToolTip { get; private set; }
        public string ContextMenuNewLayerName => _selectedLayer != null ? $"New {GetLayerName(_selectedLayer.LayerGroup)} Layer" : "";

        private void MainGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            Point point = Mouse.GetPosition(itemsControl);
            CalculateLayerUnderMouse(point);

            if(_selectedLayer != null)
            {
                ContextMenuLayerToolTip = BacEntry.DoesLayerExist(_selectedLayer.Layer, _selectedLayer.LayerGroup) ? 
                    $"{GetLayerName(_selectedLayer.LayerGroup)}: Layer {_selectedLayer.Layer}, Frame: {_contextMenuFrame}"
                    : $"{GetLayerName(_selectedLayer.LayerGroup)}: Layer {_selectedLayer.Layer}, Frame: {_contextMenuFrame} (LAYER EMPTY)";
            }
            NotifyPropertyChanged(nameof(ContextMenuLayerToolTip));
            NotifyPropertyChanged(nameof(ContextMenuNewLayerName));
        }

        private void CalculateLayerUnderMouse(Point mousePosRelativeToItemsControl)
        {
            _contextMenuFrame = (int)(mousePosRelativeToItemsControl.X / UnitSize);
            _contextMenuLayerIdx = (int)(mousePosRelativeToItemsControl.Y / 40);
            _selectedLayer = _contextMenuLayerIdx < Layers.Count ? Layers[_contextMenuLayerIdx] : null;
        }

        //Commands:
        public RelayCommand<int> AddTimelineLayerCommand => new RelayCommand<int>(AddTimelineLayer);
        private void AddTimelineLayer(int bacType)
        {
            if (BacEntry == null) return;
            AddLayer(bacType);
        }

        public RelayCommand AddItemToLayerCommand => new RelayCommand(AddItemToLayer, CanAddAtThisLayerAndFrame);
        private void AddItemToLayer()
        {
            IBacType entry = BacEntry.UndoableAddIBacType(_selectedLayer.LayerGroup, _selectedLayer.Layer, _contextMenuFrame);
            SetSelectedItem(entry, false);
            SceneManager.InvokeBacDataChangedEvent();
        }

        public RelayCommand PasteHereLayerCommand => new RelayCommand(PasteHere, CanPasteHere);
        private void PasteHere()
        {
            CopyItem copyItem = (CopyItem)Clipboard.GetData(ClipboardConstants.BacType_CopyItem);
            copyItem.ResetTimeLineLayers = false;

            //Set desired layer and start time
            foreach(IBacType type in copyItem.Primary.BacEntries[0].IBacTypes)
            {
                type.StartTime = (ushort)_contextMenuFrame;

                //Put type on the desired layer IF the type matches and there are no timing collisions
                if(!BAC_Entry.IsTimingCollision(type.StartTime, type.Duration, _selectedLayer.Layer, type.LayerGroup, BacEntry.IBacTypes) && _selectedLayer.LayerGroup == type.LayerGroup)
                {
                    type.Layer = _selectedLayer.Layer;
                }
                else
                {
                    type.Layer = -1;
                }
            }

            PasteCopyItem pasteWindow = new PasteCopyItem(copyItem, Files.Instance.SelectedMove, BacEntry, false);
            pasteWindow.ShowDialog();

            RefreshControl();
        }

        public RelayCommand DeleteLayerCommand => new RelayCommand(DeleteLayer, CanDeleteLayer);
        private void DeleteLayer()
        {
            DeleteLayer(_selectedLayer.Layer, _selectedLayer.LayerGroup);
        }

        public RelayCommand MoveLayerUpCommand => new RelayCommand(MoveLayerUp, CanMoveLayerUp);
        private void MoveLayerUp()
        {
            ChangeLayer(_selectedLayer.Layer, _selectedLayer.Layer - 1, _selectedLayer.LayerGroup);
        }

        public RelayCommand MoveLayerDownCommand => new RelayCommand(MoveLayerDown, CanMoveLayerDown);
        private void MoveLayerDown()
        {
            ChangeLayer(_selectedLayer.Layer, _selectedLayer.Layer + 1, _selectedLayer.LayerGroup);
        }
        
        public RelayCommand NewCurrentLayerCommand => new RelayCommand(NewCurrentLayer, () => _selectedLayer != null);
        private void NewCurrentLayer()
        {
            AddLayer(_selectedLayer.LayerGroup);
        }


        private bool CanDeleteLayer()
        {
            if (_selectedLayer == null) return false;
            return _selectedLayer.Layer != 0 && _selectedLayer.LayerGroup != 0;
        }

        private bool CanAddAtThisLayerAndFrame()
        {
            if (BacEntry == null) return false;
            if(_selectedLayer != null)
            {
                return !BAC_Entry.IsTimingCollision(_contextMenuFrame, 1, _selectedLayer.Layer, _selectedLayer.LayerGroup, BacEntry.IBacTypes);
            }

            return false;
        }

        private bool CanPasteHere()
        {
            if (!CanAddAtThisLayerAndFrame()) return false;
            return CanPasteBacTypes();
        }

        private bool CanMoveLayerUp()
        {
            if (_selectedLayer == null) return false;
            return _selectedLayer.Layer != 0;
        }

        private bool CanMoveLayerDown()
        {
            if (_selectedLayer == null) return false;
            return !IsMaxLayer(_selectedLayer.Layer, _selectedLayer.LayerGroup);
        }

        #endregion

        #region BacTypeContextMenu
        public RelayCommand RemoveBacTypeCommand => new RelayCommand(RemoveBacType, IsBacTypeSelected);
        private void RemoveBacType()
        {
            BacEntry.UndoableRemoveIBacType(SelectedItems.Cast<IBacType>().ToList());
            SceneManager.InvokeBacDataChangedEvent();
            RemoveSelectedItems();
            RefreshControl();
        }

        public RelayCommand CopyBacTypeCommand => new RelayCommand(CopyBacType, IsBacTypeSelected);
        private void CopyBacType()
        {
            CopyItem copyItem = new CopyItem(SelectedItems.Cast<IBacType>().ToList(), Files.Instance.SelectedMove);
            Clipboard.SetData(ClipboardConstants.BacType_CopyItem, copyItem);
        }

        public RelayCommand PasteBacTypeCommand => new RelayCommand(PasteBacType, CanPasteBacTypes);
        private void PasteBacType()
        {
            CopyItem copyItem = (CopyItem)Clipboard.GetData(ClipboardConstants.BacType_CopyItem);
            PasteCopyItem pasteWindow = new PasteCopyItem(copyItem, Files.Instance.SelectedMove, BacEntry, false);
            pasteWindow.ShowDialog();

            SetSelectedStateOnItems();
            RefreshControl();
        }

        public RelayCommand DuplicateBacTypeCommand => new RelayCommand(DuplicateBacType, IsBacTypeSelected);
        private void DuplicateBacType()
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();

            foreach (var type in SelectedItems)
            {
                var entry = (type as IBacType).Copy();
                entry.Layer = -1;
                undos.Add(BacEntry.AddEntry(entry));
            }

            SetSelectedStateOnItems();
            RefreshControl();
            UndoManager.Instance.AddCompositeUndo(undos, "Duplicate BacType");
        }

        public RelayCommand MoveToNewLayerCommand => new RelayCommand(MoveToNewLayer, AreAllSelectedSameGroup);
        private void MoveToNewLayer()
        {
            ChangeLayer(SelectedItems, BacEntry.AssignNewLayer(SelectedItem.LayerGroup), true);
            RefreshControl();
        }


        private bool AreAllSelectedSameGroup()
        {
            if (SelectedItems.Count == 0) return false;

            for(int i = 0; i < SelectedItems.Count; i++)
            {
                if (SelectedItems[i].LayerGroup != SelectedItems[0].LayerGroup) return false;
            }

            return true;
        }

        private bool IsBacTypeSelected()
        {
            return SelectedItem != null;
        }

        private bool CanPasteBacTypes()
        {
            return Clipboard.ContainsData(ClipboardConstants.BacType_CopyItem) && BacEntry != null;
        }
        #endregion

        #region DropHandler
        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            dropInfo.Effects = DragDropEffects.None;
            dropInfo.DropTargetAdorner = null;
            ITimeLineItem item = GetDropItem(dropInfo);

            if (item != null)
            {
                CalculateLayerUnderMouse(dropInfo.DropPosition);

                if (_selectedLayer?.LayerGroup == item.LayerGroup)
                {
                    if (!BAC_Entry.IsTimingCollision(_contextMenuFrame, item.TimeLine_Duration, _selectedLayer.Layer, item.LayerGroup, BacEntry.IBacTypes, item as IBacType))
                    {
                        dropInfo.Effects = DragDropEffects.Move;
                        dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                    }
                }
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            ITimeLineItem item = GetDropItem(dropInfo);

            if(item is IBacType type)
            {
                CalculateLayerUnderMouse(dropInfo.DropPosition);

                if (_selectedLayer?.LayerGroup == item.LayerGroup)
                {
                    if (!BAC_Entry.IsTimingCollision(_contextMenuFrame, item.TimeLine_Duration, _selectedLayer.Layer, item.LayerGroup, BacEntry.IBacTypes, item as IBacType))
                    {
                        List<IUndoRedo> undos = new List<IUndoRedo>();

                        undos.Add(new UndoablePropertyGeneric(nameof(type.StartTime), type, type.StartTime, (ushort)_contextMenuFrame));
                        type.StartTime = (ushort)_contextMenuFrame;
                        var originalLayer = Layers.FirstOrDefault(x => x.Layer == type.Layer && x.LayerGroup == type.LayerGroup);

                        if(_selectedLayer.Layer != type.Layer)
                        {
                            undos.Add(ChangeLayer(type, _selectedLayer.Layer, false));
                        }

                        //Refresh layers that the item is now on, and was on previously
                        if (originalLayer != _selectedLayer)
                            originalLayer.RefreshLayer();

                        _selectedLayer.RefreshLayer();


                        UndoManager.Instance.AddCompositeUndo(undos, "Move", UndoGroup, "DataChanged", BacEntry);
                    }
                }
            }
        }

        void IDropTarget.DragEnter(IDropInfo dropInfo)
        {
        }

        void IDropTarget.DragLeave(IDropInfo dropInfo)
        {

        }

        private ITimeLineItem GetDropItem(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject data)
            {
                if (data.GetDataPresent(typeof(TimeLineItemControl)))
                {
                    var ctrl = data.GetData(typeof(TimeLineItemControl)) as TimeLineItemControl;

                    if (ctrl != null)
                        return ctrl.DataContext as ITimeLineItem;
                }
            }

            return null;
        }
        #endregion

    }
}
