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
    public enum TimeLineManipulationMode { Linked, Free }
    internal enum TimeLineAction { Move, StretchStart, StretchEnd }

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
        static TimeLineDragAdorner _dragAdorner;
        static TimeLineDragAdorner DragAdorner
        {
            get => _dragAdorner;
            set
            {
                if (_dragAdorner != null)
                    _dragAdorner.Detach();
                _dragAdorner = value;
            }
        }

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
            startBinding.Mode = BindingMode.TwoWay;
            startBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            Binding endBinding = new Binding(nameof(ITimeLineItem.TimeLine_Duration));
            endBinding.Mode = BindingMode.TwoWay;
            endBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            Binding tooltipBinding = new Binding(nameof(ITimeLineItem.DisplayName));
            endBinding.Mode = BindingMode.OneWay;

            TimeLineItemControl adder = new TimeLineItemControl(ParentTimeLine, data.LayerGroup == 0);
            adder.Opacity = ParentTimeLine.GetTimeLineItemOpacity(data);
            adder.DataContext = data;
            adder.Content = data;

            adder.SetBinding(TimeLineItemControl.StartTimeProperty, startBinding);
            adder.SetBinding(TimeLineItemControl.DurationProperty, endBinding);
            adder.SetBinding(ToolTipProperty, tooltipBinding);

            if (_template != null)
            {
                adder.ContentTemplate = _template;
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
            _isRightMouseButtonDown = true;
        }

        private void Item_PreviewDragButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is TimeLineItemControl ctrl)
            {
                if (_isRightMouseButtonDown)
                {
                    ParentTimeLine.SetSelectedItem(ctrl.DataContext as ITimeLineItem, false, true);
                }
            }
        }
        #endregion

        #region Edit Events
        private double _curX = 0;
        private TimeLineAction _action;
        private double _totalDeltaThisAction = 0;
        private DateTime _manipulationEventTime = DateTime.MinValue;
        private bool _isLeftMouseButtonDown = false;
        private bool _isRightMouseButtonDown = false;
        private bool _leftCtrlDown = false;

        private void Item_PreviewEditButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is TimeLineItemControl ctrl)
            {
                ctrl.ReleaseMouseCapture();
                //Keyboard.Focus(this);

                if ((_totalDeltaThisAction < -0.01) || (_totalDeltaThisAction > 0.01))
                {
                    //To avoid accidental clicks when using the manipulation tools, clicks will only be registered a certain period after a significant manipulation event occured.
                    _manipulationEventTime = DateTime.Now;
                }
                else
                {
                    //Detect mouse click events.
                    if (DateTime.Now - new TimeSpan(0, 0, 0, 0, 500) > _manipulationEventTime)
                    {
                        if(_isLeftMouseButtonDown)
                        {
                            ParentTimeLine.SetSelectedItem(ctrl.DataContext as ITimeLineItem, Keyboard.IsKeyDown(Key.LeftCtrl));
                        }
                    }
                }

            }
        }

        private void Item_PreviewEditButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(sender is TimeLineItemControl ctrl)
            {
                _totalDeltaThisAction = 0;
                _action = ctrl.GetClickAction();
                ctrl.CaptureMouse();
                _isLeftMouseButtonDown = true;
                ParentTimeLine.TryGetFocus();
            }
        }

        protected void OnKeyDown(object sender, KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.LeftCtrl)
            {
                _leftCtrlDown = e.Key == Key.LeftCtrl;
                ManipulationMode = TimeLineManipulationMode.Linked;
            }
        }
        
        protected void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl)
                _leftCtrlDown = false;
            if (!_leftCtrlDown)
                ManipulationMode = TimeLineManipulationMode.Linked;
        }

        internal void HandleItemManipulation(TimeLineItemControl ctrl, TimeLineItemChangedEventArgs e)
        {
            double deltaT = e.DeltaTime;
            int direction = deltaT.CompareTo(default);
            if (direction == 0)
                return;

            bool doStretch;
            TimeLineItemControl previous;
            TimeLineItemControl after;
            int afterIndex = -1;
            int previousIndex = -1;
            after = GetTimeLineItemControlStartingAfter(ctrl.StartTime, ref afterIndex);
            previous = GetTimeLineItemControlStartingBefore(ctrl.StartTime, ref previousIndex);
            if (after != null)
                after.ReadyToDraw = false;
            if (ctrl != null)
                ctrl.ReadyToDraw = false;
            double useDeltaX = e.DeltaX;
            double cLeft = 0;
            double cWidth = 0;
            double cEnd = 0;
            ctrl.GetPlacementInfo(ref cLeft, ref cWidth, ref cEnd);

            _totalDeltaThisAction += e.DeltaX;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            switch (e.Action)
            {
                case TimeLineAction.Move:
                    if (direction > 0)
                    {
                        List<ITimeLineItem> items = new List<ITimeLineItem>();

                        List<TimeLineItemControl> afterChain = GetAfterChain(ctrl, out int stopFrame);
                        afterChain.Add(ctrl);

                        float floatingStartTime = 0;
                        float floatingDuration = 0;
                        bool setFloatValues = false;

                        foreach (TimeLineItemControl item in afterChain.OrderByDescending(x => x.StartTime))
                        {
                            if (!setFloatValues)
                            {
                                floatingStartTime = item.StartTimeFloat;
                                floatingDuration = item.DurationFloat;
                                setFloatValues = true;
                            }

                            items.Add(item.DataContext as ITimeLineItem);
                            item.SetFloatingStartAndDuration(floatingStartTime, floatingDuration);
                            item.MoveMe(useDeltaX, undos, -1, stopFrame);

                            stopFrame = item.StartTime;
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

                        List<TimeLineItemControl> beforeChain = GetBeforeChain(ctrl, out int stopFrame);
                        beforeChain.Add(ctrl);

                        float floatingStartTime = 0;
                        float floatingDuration = 0;
                        bool setFloatValues = false;

                        foreach (TimeLineItemControl item in beforeChain.OrderBy(x => x.StartTime))
                        {
                            if (item.StartTime == 0) break;
                            if (!setFloatValues)
                            {
                                floatingStartTime = item.StartTimeFloat;
                                floatingDuration = item.DurationFloat;
                                setFloatValues = true;
                            }

                            items.Add(item.DataContext as ITimeLineItem);
                            item.SetFloatingStartAndDuration(floatingStartTime, floatingDuration);
                            item.MoveMe(useDeltaX, undos, stopFrame);

                            stopFrame = item.StartTime + item.Duration;
                        }

                        if (undos.Count > 0)
                        {
                            UndoManager.Instance.AddCompositeUndo(undos, "Move", ParentTimeLine.UndoGroup, "Move", items);
                            UndoManager.Instance.ForceEventCall(ParentTimeLine.UndoGroup, "MoveReact", items);
                        }
                    }
                    break;
                case TimeLineAction.StretchStart:
                    switch (e.Mode)
                    {
                        case TimeLineManipulationMode.Linked:
                            double gap = double.MaxValue;
                            if (previous != null)
                            {
                                double pLeft = 0;
                                double pWidth = 0;
                                double pEnd = 0;
                                previous.GetPlacementInfo(ref pLeft, ref pWidth, ref pEnd);
                                gap = cLeft - pEnd;
                            }
                            if (direction < 0 && Math.Abs(gap) < Math.Abs(useDeltaX) && Math.Abs(gap) > _bumpThreshold)//if we are negative and not linked, but about to bump
                                useDeltaX = -gap;

                            if (Math.Abs(gap) < _bumpThreshold)
                            {
                                //we are linked
                                if (ctrl.CanDelta(0, useDeltaX) && previous.CanDelta(1, useDeltaX))
                                {
                                    //ctrl.SetFloatingStartAndDuration(previous.StartTimeFloat, previous.DurationFloat);
                                    int _delta = ctrl.MoveStartTime(useDeltaX, undos);
                                    previous.ChangeDuration(previous.Duration - _delta, undos);
                                }
                            }
                            else if (ctrl.CanDelta(0, useDeltaX))
                            {
                                ctrl.MoveStartTime(useDeltaX, undos);
                            }

                            UndoManager.Instance.AddCompositeUndo(undos, "Stretch Start Time", ParentTimeLine.UndoGroup, "Stretch", ctrl.DataContext);
                            UndoManager.Instance.ForceEventCall(ParentTimeLine.UndoGroup, "MoveReact", ctrl.DataContext);

                            break;
                        case TimeLineManipulationMode.Free:
                            gap = double.MaxValue;
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
                        default:
                            break;
                    }
                    break;
                case TimeLineAction.StretchEnd:
                    switch (e.Mode)
                    {
                        case TimeLineManipulationMode.Linked:
                            double gap = double.MaxValue;
                            if (after != null)
                            {
                                double aLeft = 0;
                                double aWidth = 0;
                                double aEnd = 0;
                                after.GetPlacementInfo(ref aLeft, ref aWidth, ref aEnd);
                                gap = aLeft - cEnd;
                            }

                            if (direction > 0 && gap > _bumpThreshold && gap < useDeltaX)//if we are positive, not linked but about to bump
                                useDeltaX = -gap;
                            if (gap < _bumpThreshold)
                            {
                                //we are linked
                                if (ctrl.CanDelta(1, useDeltaX) && after.CanDelta(0, useDeltaX))
                                {
                                    //ctrl.SetFloatingStartAndDuration(after.StartTimeFloat, after.DurationFloat);
                                    int delta = ctrl.MoveEndTime(useDeltaX, undos);
                                    after.ChangeStartTime(after.StartTime - delta, undos);
                                }
                            }
                            else if (ctrl.CanDelta(0, useDeltaX))
                            {
                                ctrl.MoveEndTime(useDeltaX, undos);
                            }

                            UndoManager.Instance.AddCompositeUndo(undos, "Stretch Duration", ParentTimeLine.UndoGroup, "Stretch", ctrl.DataContext);
                            UndoManager.Instance.ForceEventCall(ParentTimeLine.UndoGroup, "MoveReact", ctrl.DataContext);
                            break;
                        case TimeLineManipulationMode.Free:
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
                HandleItemManipulation(ctrl, new TimeLineItemChangedEventArgs()
                {
                    Action = _action,
                    DeltaTime = deltaT,
                    DeltaX = deltaX,
                    Mode = curMode
                });

                _curX = mouseX;

                //When we pressed, this lost focus and we therefore didn't capture any changes to the key status
                //so we check it again after our manipulation finishes.  That way we can be linked and go out of or back into it while dragging
                ManipulationMode = TimeLineManipulationMode.Free;
                _leftCtrlDown = Keyboard.IsKeyDown(Key.LeftCtrl);

                //Linked mode is currently broken. Disabled for now
                //if (_leftCtrlDown)
                //{
                //    ManipulationMode = TimeLineManipulationMode.Linked;
                //}
            }
        }

        #region Get Children Methods

        /// <summary>
        /// Returns a list of all timeline controls starting with the current one and moving forward
        /// so long as they are contiguous.
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private List<TimeLineItemControl> GetTimeLineForwardChain(TimeLineItemControl current, int afterIndex, ref double chainGap)
        {
            List<TimeLineItemControl> returner = new List<TimeLineItemControl>() { current };
            double left = 0, width = 0, end = 0;
            current.GetPlacementInfo(ref left, ref width, ref end);
            if (afterIndex < 0)
            {
                //we are on the end of the list so there is no limit.
                chainGap = double.MaxValue;
                return returner;
            }
            double bumpThreshold = _bumpThreshold;
            double lastAddedEnd = end;
            while (afterIndex < Items.Count)
            {
                left = width = end = 0;
                TimeLineItemControl checker = GetTimeLineItemControlAt(afterIndex++);
                if (checker != null)
                {
                    checker.GetPlacementInfo(ref left, ref width, ref end);
                    double gap = left - lastAddedEnd;
                    if (gap > bumpThreshold)
                    {
                        chainGap = gap;
                        return returner;
                    }
                    returner.Add(checker);
                    lastAddedEnd = end;
                }

            }
            //we have chained off to the end and thus have no need to worry about our gap
            chainGap = double.MaxValue;
            return returner;
        }

        /// <summary>
        /// Returns a list of all timeline controls starting with the current one and moving backwoards
        /// so long as they are contiguous.  If the chain reaches back to the start time of the timeline then the
        /// ChainsBackToStart boolean is modified to reflect that.
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private List<TimeLineItemControl> GetTimeLineBackwardsChain(TimeLineItemControl current, int prevIndex, ref bool ChainsBackToStart, ref double chainGap)
        {

            List<TimeLineItemControl> returner = new List<TimeLineItemControl>() { current };
            double left = 0, width = 0, end = 0;
            current.GetPlacementInfo(ref left, ref width, ref end);
            if (prevIndex < 0)
            {
                chainGap = double.MaxValue;
                ChainsBackToStart = left == 0;
                return returner;
            }

            double lastAddedLeft = left;
            while (prevIndex >= 0)
            {
                left = width = end = 0;

                TimeLineItemControl checker = GetTimeLineItemControlAt(prevIndex--);
                if (checker != null)
                {
                    checker.GetPlacementInfo(ref left, ref width, ref end);
                    if (lastAddedLeft - end > _bumpThreshold)
                    {
                        //our chain just broke;
                        chainGap = lastAddedLeft - end;
                        ChainsBackToStart = lastAddedLeft == 0;
                        return returner;
                    }
                    returner.Add(checker);
                    lastAddedLeft = left;
                }

            }
            ChainsBackToStart = lastAddedLeft == 0;
            chainGap = lastAddedLeft;//gap between us and zero;
            return returner;

        }

        private List<ITimeLineItem> GetTimeLineItemChain(List<TimeLineItemControl> controls)
        {
            List<ITimeLineItem> items = new List<ITimeLineItem>();

            foreach(var ctrl in controls)
            {
                if (ctrl.DataContext is ITimeLineItem item)
                {
                    items.Add(item);
                }
                else
                {
                    throw new InvalidDataException("Invalid TimeLineItem DataContext!");
                }
            }

            return items;
        }

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

    internal class TimeLineItemChangedEventArgs : EventArgs
    {
        public TimeLineManipulationMode Mode { get; set; }
        public TimeLineAction Action { get; set; }
        public double DeltaTime { get; set; }
        public double DeltaX { get; set; }

    }

    internal class TimeLineDragAdorner : Adorner
    {
        private ContentPresenter _adorningContentPresenter;
        internal ITimeLineItem Data { get; set; }
        internal DataTemplate Template { get; set; }
        Point _mousePosition;
        public Point MousePosition
        {
            get => _mousePosition;
            set
            {
                if (_mousePosition != value)
                {
                    _mousePosition = value;
                    _layer.Update(AdornedElement);
                }

            }
        }

        AdornerLayer _layer;
        public TimeLineDragAdorner(TimeLineItemControl uiElement, DataTemplate template)
            : base(uiElement)
        {
            _adorningContentPresenter = new ContentPresenter();
            _adorningContentPresenter.Content = uiElement.DataContext;
            _adorningContentPresenter.ContentTemplate = template;
            _adorningContentPresenter.Opacity = 0.5;
            _layer = AdornerLayer.GetAdornerLayer(uiElement);

            _layer.Add(this);
            IsHitTestVisible = false;

        }
        public void Detach()
        {
            _layer.Remove(this);
        }
        protected override Visual GetVisualChild(int index)
        {
            return _adorningContentPresenter;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            //_adorningContentPresenter.Measure(constraint);
            return new Size((AdornedElement as TimeLineItemControl).Width, (AdornedElement as TimeLineItemControl).DesiredSize.Height);//(_adorningContentPresenter.Width,_adorningContentPresenter.Height);
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _adorningContentPresenter.Arrange(new Rect(finalSize));
            return finalSize;
        }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            GeneralTransformGroup result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(MousePosition.X - 4, MousePosition.Y - 4));
            return result;
        }

    }

    public interface ITimeLineParent
    {
        UndoGroup UndoGroup { get; }
        double UnitSize { get; }
        int CurrentLength { get; }
        double CurrentWidth { get; }

        void UpdateLength();

        void SetSelectedItem(ITimeLineItem item, bool append, bool rightClickSelection = false);

        int GetGhostDuration(ITimeLineItem timelineItem);

        void OnItemManipulation();

        string GetLayerName(int layerGroup);

        void TryGetFocus();

        float GetTimeLineItemOpacity(ITimeLineItem timelineItem); 

    }
}
