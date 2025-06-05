using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Documents;
using Xv2CoreLib;
using Xv2CoreLib.Resource;
using System.IO;
using Xv2CoreLib.Resource.UndoRedo;
using Xv2CoreLib.BAC;
using MahApps.Metro.Controls;

//Originally based on the code from this article: https://www.codeproject.com/articles/240411/wpf-timeline-control-part-i
//Extensively modified to better suit the needs of a frame based timeline, with some new features added and several unnecessary features removed

namespace XenoKit.Views.TimeLines
{
    public enum TimeLineManipulationMode { Free, Shift }
    public enum TimeLineAction { Move, StretchStart, StretchEnd }

    public class TimeLineLayer<T> : Canvas where T : ITimeLineItem
    {
        public static TimeSpan CalculateMinimumAllowedTimeSpan(double unitSize)
        {
            //minute = unitsize*pixels
            //desired minimum widh for these manipulations = 10 pixels
            int minPixels = 10;
            double hours = minPixels / unitSize;
            //convert to milliseconds
            long ticks = (long)(hours * 60 * 60000 * 10000);
            return new TimeSpan(ticks);
        }

        public ITimeLineParent ParentTimeLine { get; private set; }
        public ItemsControl ParentItemsControl { get; set; }

        private double _bumpThreshold = 1.5;
        private Line _seperator;
        
        #region DPs
        private DataTemplate _template;
        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(TimeLineLayer<T>), new UIPropertyMetadata(null, new PropertyChangedCallback(OnItemTemplateChanged)));
        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineLayer<T> tc = d as TimeLineLayer<T>;
            if (tc != null)
            {
                tc.SetTemplate(e.NewValue as DataTemplate);
            }
        }

        public TimeLineManipulationMode ManipulationMode
        {
            get => (TimeLineManipulationMode)GetValue(ManipulationModeProperty);
            set => SetValue(ManipulationModeProperty, value);
        }

        public static readonly DependencyProperty ManipulationModeProperty = DependencyProperty.Register(nameof(ManipulationMode), typeof(TimeLineManipulationMode), typeof(TimeLineLayer<T>), new UIPropertyMetadata(TimeLineManipulationMode.Free));
        #endregion

        public TimeLineLayer(ITimeLineParent parentTimeLine)
        {
            ParentTimeLine = parentTimeLine;
            _seperator = new Line();
            Children.Add(_seperator);
            Focusable = true;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
        }

        #region Items
        public int Layer { get; private set; }
        public int LayerGroup { get; private set; }
        public AsyncObservableCollection<T> Items { get; private set; }

        public void SetLayerContext(int layer, int layerGroup, AsyncObservableCollection<T> items)
        {
            if(items == null)
            {
                Items = null;
                return;
            }

            Layer = layer;
            LayerGroup = layerGroup;
            Items = items;
            InitializeItems(Items);
        }

        public void RefreshLayer()
        {
            InitializeItems(Items);
        }

        /// <summary>
        /// Check if an <see cref="ITimeLineItem"/> is valid for this layer, and either add or remove it depending on the result.
        /// </summary>
        public void CheckItemIsValid(ITimeLineItem item)
        {
            if (ItemIsValidForLayer(item))
            {
                AddItem(item);
            }
            else
            {
                RemoveItem(item);
            }
        }

        public void AddItemToLayer(ITimeLineItem item)
        {
            //Calling method is responsible for changing layer variables
            if(item.Layer == Layer && item.LayerGroup == LayerGroup)
            {
                AddItem(item);
            }
        }

        private bool ItemIsValidForLayer(ITimeLineItem item)
        {
            return item?.Layer == Layer && item?.LayerGroup == LayerGroup;
        }

        public void UpdateItemsTiming(ITimeLineItem item)
        {
            var ctrl = GetTimeLineItemControl(item);

            if(ctrl != null)
            {
                ctrl.ResetStartAndDurationFromSource();
                ctrl.PlaceOnCanvas();
            }
        }

        private static bool IsTimingCollision(ITimeLineItem item, IList<ITimeLineItem> items)
        {
            foreach (ITimeLineItem current in items.Where(x => x.Layer == item.Layer && x.LayerGroup == item.LayerGroup && x != item))
            {
                int currentEndTime = current.TimeLine_StartTime + current.TimeLine_Duration;
                int endTime = item.TimeLine_StartTime + item.TimeLine_Duration;

                //StartTime is within another entry
                if (item.TimeLine_StartTime >= current.TimeLine_StartTime && item.TimeLine_StartTime < currentEndTime)
                    return true;

                //StartTime is before another entry, but it ends within another
                if (item.TimeLine_StartTime < current.TimeLine_StartTime && endTime > current.TimeLine_StartTime)
                    return true;
            }

            return false;
        }
        #endregion

        #region control life cycle events
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
        }

        #endregion

        #region miscellaneous helpers
        private void SetTemplate(DataTemplate dataTemplate)
        {
            _template = dataTemplate;
            for (int i = 0; i < Children.Count; i++)
            {
                TimeLineItemControl titem = Children[i] as TimeLineItemControl;
                if (titem != null)
                    titem.ContentTemplate = dataTemplate;
            }
        }

        private void InitializeItems(AsyncObservableCollection<T> observableCollection)
        {
            if (observableCollection == null)
                return;
            Children.Clear();
            Children.Add(_seperator);

            foreach (ITimeLineItem data in observableCollection.Where(x => x.LayerGroup == LayerGroup && x.Layer == Layer))
            {
                TimeLineItemControl adder = CreateTimeLineItemControl(data);

                Children.Add(adder);
            }
        }

        public void AddItem(ITimeLineItem itm)
        {
            if (itm.Layer != Layer || itm.LayerGroup != LayerGroup) return;

            if(GetTimeLineItemControl(itm) == null)
            {
                Children.Add(CreateTimeLineItemControl(itm)); 
            }
        }

        public void RemoveItem(object removeItem)
        {
            TimeLineItemControl checker = GetTimeLineItemControl(removeItem);

            if(checker != null)
            {
                Children.Remove(checker);
            }
        }

        public bool ContainsItem(object item)
        {
            return GetTimeLineItemControl(item) != null;
        }

        private TimeLineItemControl GetTimeLineItemControl(object item)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                TimeLineItemControl checker = Children[i] as TimeLineItemControl;
                if (checker != null && checker.DataContext == item)
                {
                    return checker;
                }
            }

            return null;
        }

        private TimeLineItemControl CreateTimeLineItemControl(ITimeLineItem data)
        {
            Binding startBinding = new Binding(nameof(ITimeLineItem.TimeLine_StartTime));
            startBinding.Mode = BindingMode.OneWay;
            startBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            Binding endBinding = new Binding(nameof(ITimeLineItem.TimeLine_Duration));
            endBinding.Mode = BindingMode.OneWay;
            endBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            Binding tooltipBinding = new Binding(nameof(ITimeLineItem.DisplayName));
            tooltipBinding.Mode = BindingMode.OneWay;

            TimeLineItemControl adder = new TimeLineItemControl(ParentTimeLine, data.LayerGroup == 0);
            adder.Opacity = ParentTimeLine.GetTimeLineItemOpacity(data);
            adder.DataContext = data;
            adder.Content = data;

            adder.SetBinding(TimeLineItemControl.StartTimeProperty, startBinding);
            adder.SetBinding(TimeLineItemControl.DurationProperty, endBinding);
            adder.SetBinding(ToolTipProperty, tooltipBinding);

            if (_template != null)
            {
                adder.ApplyContentTemplate(_template);
                //adder.ContentTemplate = _template;
            }

            /*adder.PreviewMouseLeftButtonDown += item_PreviewEditButtonDown;
            adder.MouseMove += item_MouseMove;
            adder.PreviewMouseLeftButtonUp += item_PreviewEditButtonUp;*/
            adder.PreviewMouseLeftButtonDown += Item_PreviewEditButtonDown;
            adder.MouseMove += Item_MouseMove;
            adder.PreviewMouseLeftButtonUp += Item_PreviewEditButtonUp;

            adder.PreviewMouseRightButtonUp += Item_PreviewDragButtonUp;
            adder.PreviewMouseRightButtonDown += Item_PreviewDragButtonDown;
            return adder;
        }
        #endregion

        public void UpdateUnitSize()
        {
            if (Items == null)
                return;

            for (int i = 0; i < Items.Count; i++)
            {
                TimeLineItemControl ctrl = GetTimeLineItemControlAt(i);
                if (ctrl != null)
                {
                    ctrl.PlaceOnCanvas();
                }

            }

            //ReDrawChildren();
            DrawSeperatorLine();
        }

        private void DrawSeperatorLine()
        {
            if (!Children.Contains(_seperator))
                Children.Add(_seperator);

            _seperator.Stroke = Brushes.Gray;
            _seperator.StrokeThickness = 1;
            _seperator.Y1 = Height - 1;
            _seperator.Y2 = Height - 1;
            _seperator.X1 = 0;
            _seperator.X2 = ParentTimeLine.CurrentWidth;

            Canvas.SetLeft(_seperator, 0);
        }

        #region Drag Fields
        private Point _dragStartPosition = new Point(double.MinValue, double.MinValue);
        /// <summary>
        /// When we drag something from an external control over this I need a temp control
        /// that lets me adorn those accordingly as well
        /// </summary>
        private TimeLineItemControl _tmpDraggAdornerControl;
        
        private void Item_PreviewDragButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TimeLineItemControl ctrl)
            {
                ParentTimeLine.SetSelectedItem(ctrl.DataContext as ITimeLineItem, false, true);
            }
        }

        private void Item_PreviewDragButtonUp(object sender, MouseButtonEventArgs e)
        {
        }
        #endregion

        #region Edit Events
        private double _curX = 0;
        private TimeLineAction _action;
        private bool _selectionSet = false; 
        private static float floatingStartTime = 0;
        private static float floatingDuration = 0;

        private void Item_PreviewEditButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is TimeLineItemControl ctrl)
            {
                if (!_selectionSet)
                {
                    ParentTimeLine.SetSelectedItem(ctrl.DataContext as ITimeLineItem, Keyboard.IsKeyDown(Key.LeftCtrl));
                }

                ctrl.ReleaseMouseCapture();
            }
        }

        private void Item_PreviewEditButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(sender is TimeLineItemControl ctrl)
            {
                _selectionSet = false;
                _action = ctrl.GetClickAction(true);
                ctrl.CaptureMouse();
                ParentTimeLine.TryGetFocus();
            }
        }

        /// <summary>
        /// Mouse move is important for both edit and drag behaviors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            TimeLineItemControl ctrl = sender as TimeLineItemControl;
            if (ctrl == null) return;

            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                Point position = Mouse.GetPosition(null);
                if (Math.Abs(position.X - _dragStartPosition.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPosition.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DragDrop.DoDragDrop(this, ctrl, DragDropEffects.Move | DragDropEffects.Scroll);
                }
            }
            else
            {
                if (Mouse.Captured != ctrl)
                {
                    _curX = Mouse.GetPosition(null).X;
                    return;
                }

                double mouseX = Mouse.GetPosition(null).X;
                double deltaX = mouseX - _curX;
                double deltaT = ctrl.GetDeltaTime(deltaX);

                TimeLineManipulationMode curMode = (TimeLineManipulationMode)GetValue(ManipulationModeProperty);
                var args = new TimeLineItemChangedEventArgs()
                {
                    Action = _action,
                    DeltaTime = deltaT,
                    DeltaX = deltaX,
                    Mode = curMode,
                    Layer = Layer,
                    LayerGroup = LayerGroup
                };

                HandleItemManipulation(ctrl, args);
                ParentTimeLine.ItemManipulationStarted(args);

                _curX = mouseX;

                ManipulationMode = Keyboard.IsKeyDown(Key.LeftShift) ? TimeLineManipulationMode.Shift : TimeLineManipulationMode.Free;
            }
        }

        protected void OnKeyDown(object sender, KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }
        
        protected void OnKeyUp(object sender, KeyEventArgs e)
        {
        }

        internal void HandleItemManipulation(TimeLineItemControl ctrl, TimeLineItemChangedEventArgs e)
        {
            if (ctrl == null & e.Action != TimeLineAction.Move) return;

            double deltaT = e.DeltaTime;
            int direction = deltaT.CompareTo(default);
            if (direction == 0)
                return;

            bool doStretch;
            int afterIndex = -1;
            int previousIndex = -1;
            TimeLineItemControl after = null;
            TimeLineItemControl previous = null;
            double useDeltaX = e.DeltaX;
            double cLeft = 0;
            double cWidth = 0;
            double cEnd = 0;

            if(ctrl != null)
            {
                after = GetTimeLineItemControlStartingAfter(ctrl.StartTime, ref afterIndex);
                previous = GetTimeLineItemControlStartingBefore(ctrl.StartTime, ref previousIndex);

                if (after != null)
                    after.ReadyToDraw = false;
                if (ctrl != null)
                    ctrl.ReadyToDraw = false;

                ctrl.GetPlacementInfo(ref cLeft, ref cWidth, ref cEnd);
            }

            if(!MathHelpers.FloatEquals(useDeltaX, 0.0) && !_selectionSet && ctrl != null)
            {
                ParentTimeLine.SetSelectedItem(ctrl.DataContext as ITimeLineItem, Keyboard.IsKeyDown(Key.LeftCtrl), false, true);
                _selectionSet = true;
            }

            List<IUndoRedo> undos = new List<IUndoRedo>();

            switch (e.Action)
            {
                case TimeLineAction.Move:
                    bool setFloatValues = false;

                    if (direction > 0)
                    {
                        List<ITimeLineItem> items = new List<ITimeLineItem>();
                        int stopFrame;

                        foreach (ITimeLineItem selectedItem in ParentTimeLine.SelectedItems.Where(x => x.Layer == Layer && x.LayerGroup == LayerGroup).OrderByDescending(x => x.TimeLine_StartTime))
                        {
                            TimeLineItemControl _ctrl = GetTimeLineItemControl(selectedItem);

                            List<TimeLineItemControl> afterChain;

                            if(e.Mode == TimeLineManipulationMode.Shift)
                            {
                                afterChain = GetAllControlsAfter(_ctrl);
                                stopFrame = int.MaxValue;
                            }
                            else
                            {
                                afterChain = GetAfterChain(_ctrl, out stopFrame);
                            }

                            afterChain.Add(_ctrl);

                            foreach (TimeLineItemControl item in afterChain.OrderByDescending(x => x.StartTime))
                            {
                                if (!setFloatValues && ctrl != null)
                                {
                                    floatingStartTime = item.StartTimeFloat;
                                    floatingDuration = item.DurationFloat;
                                    setFloatValues = true;
                                }

                                if (!items.Contains(item.DataContext))
                                {
                                    items.Add(item.DataContext as ITimeLineItem);
                                    item.SetFloatingStartAndDuration(floatingStartTime, floatingDuration);
                                    item.MoveMe(useDeltaX, undos, -1, stopFrame);
                                }

                                stopFrame = item.StartTime;
                            }
                        }

                        if (undos.Count > 0)
                        {
                            UndoManager.Instance.AddCompositeUndo(undos, "Move", ParentTimeLine.UndoGroup, "Move", items);
                            UndoManager.Instance.ForceEventCall(ParentTimeLine.UndoGroup, "MoveReact", items);
                        }
                    }
                    else if (direction < 0)
                    {
                        List<ITimeLineItem> items = new List<ITimeLineItem>();
                        bool reachedStart = false;
                        int stopFrame;

                        foreach (ITimeLineItem selectedItem in ParentTimeLine.SelectedItems.Where(x => x.Layer == Layer && x.LayerGroup == LayerGroup).OrderBy(x => x.TimeLine_StartTime))
                        {
                            if (reachedStart) break;
                            TimeLineItemControl _ctrl = GetTimeLineItemControl(selectedItem);

                            List<TimeLineItemControl> beforeChain = GetBeforeChain(_ctrl, out stopFrame);
                            beforeChain.Add(_ctrl);

                            if(e.Mode == TimeLineManipulationMode.Shift)
                            {
                                beforeChain.AddRange(GetAllControlsAfter(_ctrl));
                            }

                            foreach (TimeLineItemControl item in beforeChain.OrderBy(x => x.StartTime))
                            {
                                if (item.StartTime == 0)
                                {
                                    reachedStart = true;
                                    break;
                                }
                                if (!setFloatValues && ctrl != null)
                                {
                                    floatingStartTime = item.StartTimeFloat;
                                    floatingDuration = item.DurationFloat;
                                    setFloatValues = true;
                                }

                                if (!items.Contains(item.DataContext))
                                {
                                    items.Add(item.DataContext as ITimeLineItem);
                                    item.SetFloatingStartAndDuration(floatingStartTime, floatingDuration);
                                    item.MoveMe(useDeltaX, undos, stopFrame);
                                }

                                stopFrame = item.StartTime + item.Duration;
                            }
                        }

                        if (undos.Count > 0)
                        {
                            UndoManager.Instance.AddCompositeUndo(undos, "Move", ParentTimeLine.UndoGroup, "Move", items);
                            UndoManager.Instance.ForceEventCall(ParentTimeLine.UndoGroup, "MoveReact", items);
                        }
                    }
                    break;
                case TimeLineAction.StretchStart:
                    double gap = double.MaxValue;
                    doStretch = direction > 0;
                    if (direction < 0)
                    {
                        //disallow us from free stretching into another item

                        if (previous != null)
                        {
                            double pLeft = 0;
                            double pWidth = 0;
                            double pEnd = 0;
                            previous.GetPlacementInfo(ref pLeft, ref pWidth, ref pEnd);
                            gap = cLeft - pEnd;
                        }
                        else
                        {
                            //don't allow us to stretch further than the gap between current and start time
                            gap = cLeft;
                        }

                        doStretch = gap > _bumpThreshold;
                        if (gap < useDeltaX)
                        {
                            useDeltaX = gap;
                        }
                    }

                    doStretch &= ctrl.CanDelta(0, useDeltaX);

                    if (doStretch)
                    {
                        ctrl.MoveStartTime(useDeltaX, undos);

                        UndoManager.Instance.AddCompositeUndo(undos, "Stretch Start Time", ParentTimeLine.UndoGroup, "Stretch", ctrl.DataContext);
                        UndoManager.Instance.ForceEventCall(ParentTimeLine.UndoGroup, "MoveReact", ctrl.DataContext);
                    }
                    break;
                case TimeLineAction.StretchEnd:
                    double nextGap = double.MaxValue;
                    doStretch = true;
                    if (direction > 0 && after != null)
                    {
                        //disallow us from free stretching into another item
                        double nLeft = 0;
                        double nWidth = 0;
                        double nEnd = 0;
                        after.GetPlacementInfo(ref nLeft, ref nWidth, ref nEnd);
                        nextGap = nLeft - cEnd;
                        doStretch = nextGap > _bumpThreshold;
                        if (nextGap < useDeltaX)
                            useDeltaX = nextGap;
                    }


                    doStretch &= ctrl.CanDelta(1, useDeltaX);
                    if (doStretch)
                    {
                        ctrl.MoveEndTime(useDeltaX, undos);

                        UndoManager.Instance.AddCompositeUndo(undos, "Stretch Duration", ParentTimeLine.UndoGroup, "Stretch", ctrl.DataContext);
                        UndoManager.Instance.ForceEventCall(ParentTimeLine.UndoGroup, "MoveReact", ctrl.DataContext);
                    }

                    break;
                default:
                    break;
            }
        
            if(!MathHelpers.FloatEquals(e.DeltaX, 0))
            {
                ParentTimeLine.OnItemManipulation();
            }
        }

        #endregion

        #region Get Children Methods
        private TimeLineItemControl GetTimeLineItemControlStartingBefore(float dateTime, ref int index)
        {
            index = -1;
            for (int i = 0; i < Items.Count; i++)
            {
                TimeLineItemControl checker = GetTimeLineItemControlAt(i);
                if (checker != null && checker.StartTime == dateTime && i != 0)
                {
                    index = i - 1;
                    return GetTimeLineItemControlAt(i - 1);
                }
            }
            index = -1;
            return null;
        }

        private TimeLineItemControl GetTimeLineItemControlStartingAfter(float dateTime, ref int index)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                TimeLineItemControl checker = GetTimeLineItemControlAt(i);
                if (checker != null && checker.StartTime > dateTime)
                {
                    index = i;
                    return checker;
                }
            }
            index = -1;
            return null;
        }

        private TimeLineItemControl GetTimeLineItemControlAt(int i)
        {
            if(i >= Children.Count) return null;
            return Children[i] as TimeLineItemControl;
        }

        private List<TimeLineItemControl> GetAllControlsBefore(TimeLineItemControl mainCtrl)
        {
            List<TimeLineItemControl> ctrls = new List<TimeLineItemControl>();
            int beforeFrame = mainCtrl.StartTime;

            foreach (var ctrl in Children)
            {
                if (ctrl is TimeLineItemControl timelineItem)
                {
                    if (timelineItem.StartTime < beforeFrame)
                    {
                        ctrls.Add(timelineItem);
                    }
                }
            }

            return ctrls;
        }

        private List<TimeLineItemControl> GetAllControlsAfter(TimeLineItemControl mainCtrl)
        {
            List<TimeLineItemControl> ctrls = new List<TimeLineItemControl>();
            int afterFrame = mainCtrl.StartTime;

            foreach (var ctrl in Children)
            {
                if (ctrl is TimeLineItemControl timelineItem)
                {
                    if (timelineItem.StartTime > afterFrame)
                    {
                        ctrls.Add(timelineItem);
                    }
                }
            }

            return ctrls;
        }

        private List<TimeLineItemControl> GetBeforeChain(TimeLineItemControl mainCtrl, out int prevEntryFrame)
        {
            List<TimeLineItemControl> ctrls = new List<TimeLineItemControl>();

            if (mainCtrl.StartTime != 0)
            {
                int frameToCheck = mainCtrl.StartTime;

                foreach (TimeLineItemControl ctrl in GetAllControlsBefore(mainCtrl).OrderByDescending(x => x.StartTime))
                {
                    if (ctrl.StartTime + ctrl.Duration >= frameToCheck)
                    {
                        ctrls.Add(ctrl);
                        frameToCheck = ctrl.StartTime;
                    }
                    else
                    {
                        prevEntryFrame = ctrl.StartTime + ctrl.Duration;
                        return ctrls;
                    }
                }
            }

            prevEntryFrame = -1;
            return ctrls;
        }

        private List<TimeLineItemControl> GetAfterChain(TimeLineItemControl mainCtrl, out int nextEntryFrame)
        {
            List<TimeLineItemControl> ctrls = new List<TimeLineItemControl>();
            int frameToCheck = mainCtrl.StartTime + mainCtrl.Duration;

            foreach (TimeLineItemControl ctrl in GetAllControlsAfter(mainCtrl).OrderBy(x => x.StartTime))
            {
                if (ctrl.StartTime <= frameToCheck)
                {
                    ctrls.Add(ctrl);
                    frameToCheck = ctrl.StartTime + ctrl.Duration;
                }
                else
                {
                    nextEntryFrame = ctrl.StartTime;
                    return ctrls;
                }
            }

            nextEntryFrame = int.MaxValue;
            return ctrls;
        }
        #endregion
    }

    public class TimeLineItemChangedEventArgs : EventArgs
    {
        public TimeLineManipulationMode Mode { get; set; }
        public TimeLineAction Action { get; set; }
        public double DeltaTime { get; set; }
        public double DeltaX { get; set; }

        public int Layer { get; set; }
        public int LayerGroup { get; set; }
    }

    public interface ITimeLineParent
    {
        List<ITimeLineItem> SelectedItems { get; }

        UndoGroup UndoGroup { get; }
        double UnitSize { get; }
        int CurrentLength { get; }
        double CurrentWidth { get; }

        void UpdateLength();

        void SetSelectedItem(ITimeLineItem item, bool append, bool rightClickSelection = false, bool dontUnselect = false);

        int GetGhostDuration(ITimeLineItem timelineItem);

        void OnItemManipulation();

        string GetLayerName(int layerGroup);

        void TryGetFocus();

        float GetTimeLineItemOpacity(ITimeLineItem timelineItem);

        void ItemManipulationStarted(TimeLineItemChangedEventArgs args);
    }
}
