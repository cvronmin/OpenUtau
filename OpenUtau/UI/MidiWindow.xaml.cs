﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;

using WinInterop = System.Windows.Interop;
using System.Runtime.InteropServices;

using OpenUtau.UI.Models;
using OpenUtau.UI.Controls;
using OpenUtau.Core;
using OpenUtau.Core.USTx;

namespace OpenUtau.UI
{
    /// <summary>
    /// Interaction logic for BorderlessWindow.xaml
    /// </summary>
    public partial class MidiWindow : BorderlessWindow
    {
        internal MidiViewModel midiVM { get; private set; }
        MidiViewHitTest midiHT;
        ContextMenu pitchCxtMenu;
        
        RoutedEventHandler pitchShapeDelegate;
        class PitchPointHitTestResultContainer { public PitchPointHitTestResult Result;}
        PitchPointHitTestResultContainer pitHitContainer;

        EnumTool ToolUsing {
            get
            {
                if (radioToolCursor.IsChecked.Value)
                {
                    return EnumTool.Cursor;
                }
                if (radioToolPaint.IsChecked.Value)
                {
                    return EnumTool.Brush;
                }
                return EnumTool.Cursor;
            }
        }

        private bool _tiny;
        public bool LyricsPresetDedicate { get { return _tiny; } set {
                _tiny = value;
                midiVM.LyricsPresetDedicate = value;
                if (value)
                {
                    keyboardBackground.Visibility = Visibility.Collapsed;
                    //expCanvas.Visibility = Visibility.Collapsed;
                    midiVM.TrackHeight = 32;
                    showPitchToggle.Visibility = Visibility.Collapsed;
                    //CCGrid.Visibility = Visibility.Collapsed;
                    //expVerticalScroll.Visibility = Visibility.Collapsed;
                    //expTickBackground.Visibility = Visibility.Collapsed;
                    mainButton.Visibility = Visibility.Collapsed;
                    midiVM.ViewHeight = 22;
                }
                else
                {
                    keyboardBackground.Visibility = Visibility.Visible;
                    //expCanvas.Visibility = Visibility.Visible;
                    midiVM.TrackHeight = 32;
                    showPitchToggle.Visibility = Visibility.Visible;
                    //CCGrid.Visibility = Visibility.Visible;
                    //expVerticalScroll.Visibility = Visibility.Visible;
                    //expTickBackground.Visibility = Visibility.Visible;
                    mainButton.Visibility = Visibility.Visible;
                }
            }
        }

        public MidiWindow()
        {
            InitializeComponent();

            this.CloseButtonClicked += (o, e) => { Hide(); };
            CompositionTargetEx.FrameUpdating += RenderLoop;

            viewScaler.Max = UIConstants.NoteMaxHeight;
            viewScaler.Min = UIConstants.NoteMinHeight;
            viewScaler.Value = UIConstants.NoteDefaultHeight;
            viewScaler.ViewScaled += viewScaler_ViewScaled;

            viewScalerX.Max = UIConstants.MidiQuarterMaxWidth;
            viewScalerX.Min = UIConstants.MidiQuarterMinWidth;
            viewScalerX.Value = UIConstants.MidiQuarterDefaultWidth;

            midiVM = (MidiViewModel)this.Resources["midiVM"];
            midiVM.TimelineCanvas = this.timelineCanvas;
            midiVM.MidiCanvas = this.notesCanvas;
            midiVM.PhonemeCanvas = this.phonemeCanvas;
            midiVM.ExpCanvas = this.expCanvas;
            midiVM.Subscribe(DocManager.Inst);

            midiHT = new MidiViewHitTest(midiVM);

            comboVMs = new List<ExpComboBoxViewModel>()
            {
                new ExpComboBoxViewModel() { Index=0 },
                new ExpComboBoxViewModel() { Index=1 },
                new ExpComboBoxViewModel() { Index=2 },
                new ExpComboBoxViewModel() { Index=3 }
            };

            comboVMs[0].CreateBindings(expCombo0);
            comboVMs[1].CreateBindings(expCombo1);
            comboVMs[2].CreateBindings(expCombo2);
            comboVMs[3].CreateBindings(expCombo3);

            InitPitchPointContextMenu();
        }
        List<ExpComboBoxViewModel> comboVMs;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            DocManager.Inst.UnSubscribe(midiVM);
            midiVM = null;
            foreach (var item in comboVMs)
            {
                DocManager.Inst.UnSubscribe(item);
            }
        }

        void InitPitchPointContextMenu()
        {
            pitchCxtMenu = new ContextMenu();
            pitchCxtMenu.Background = Brushes.White;
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Ease In/Out" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Linear" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Ease In" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Ease Out" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Snap to Previous Note" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Delete Point" });
            pitchCxtMenu.Items.Add(new MenuItem() { Header = "Add Point" });

            pitHitContainer = new PitchPointHitTestResultContainer();
            pitchShapeDelegate = (_o, _e) =>
            {
                var o = _o as MenuItem;
                var pitHit = pitHitContainer.Result;
                if (o == pitchCxtMenu.Items[4])
                {
                    if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new SnapPitchPointCommand(pitHit.Note));
                    if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
                }
                else if (o == pitchCxtMenu.Items[5])
                {
                    if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new DeletePitchPointCommand(midiVM.Part, pitHit.Note, pitHit.Index));
                    if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
                }
                else if (o == pitchCxtMenu.Items[6])
                {
                    if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new AddPitchPointCommand(pitHit.Note, new PitchPoint(pitHit.X, pitHit.Y), pitHit.Index + 1));
                    if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
                }
                else
                {
                    PitchPointShape shape =
                        o == pitchCxtMenu.Items[0] ? PitchPointShape.InOut :
                        o == pitchCxtMenu.Items[2] ? PitchPointShape.In :
                        o == pitchCxtMenu.Items[3] ? PitchPointShape.Out : PitchPointShape.Linear;
                    if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                    DocManager.Inst.ExecuteCmd(new ChangePitchPointShapeCommand(midiVM.Part, pitHit.Note.PitchBend.Points[pitHit.Index], shape));
                    if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
                }
            };

            foreach (var item in pitchCxtMenu.Items)
            {
                var _item = item as MenuItem;
                if (_item != null) _item.Click += pitchShapeDelegate;
            }
        }

        void viewScaler_ViewScaled(object sender, EventArgs e)
        {
            double zoomCenter = (midiVM.OffsetY + midiVM.ViewHeight / 2) / midiVM.TrackHeight;
            midiVM.TrackHeight = ((ViewScaledEventArgs)e).Value;
            midiVM.OffsetY = midiVM.TrackHeight * zoomCenter - midiVM.ViewHeight / 2;
            midiVM.MarkUpdate();
        }

        private TimeSpan lastFrame = TimeSpan.Zero;

        void RenderLoop(object sender, EventArgs e)
        {
            if (midiVM == null || midiVM.Part == null || midiVM.Project == null) return;

            TimeSpan nextFrame = ((RenderingEventArgs)e).RenderingTime;
            double deltaTime = (nextFrame - lastFrame).TotalMilliseconds;
            lastFrame = nextFrame;

            DragScroll(deltaTime);
            keyboardBackground.RenderIfUpdated();
            tickBackground.RenderIfUpdated();
            timelineBackground.RenderIfUpdated();
            keyTrackBackground.RenderIfUpdated();
            expTickBackground.RenderIfUpdated();
            midiVM.RedrawIfUpdated();
        }

        public void DragScroll(double deltaTime)
        {
            if (Mouse.Captured == this.notesCanvas && Mouse.LeftButton == MouseButtonState.Pressed)
            {

                const double scrollSpeed = 0.015;
                Point mousePos = Mouse.GetPosition(notesCanvas);
                bool needUdpate = false;
                double delta = scrollSpeed * deltaTime;
                if (mousePos.X < 0)
                {
                    this.horizontalScroll.Value = this.horizontalScroll.Value - this.horizontalScroll.SmallChange * delta;
                    needUdpate = true;
                }
                else if (mousePos.X > notesCanvas.ActualWidth)
                {
                    this.horizontalScroll.Value = this.horizontalScroll.Value + this.horizontalScroll.SmallChange * delta;
                    needUdpate = true;
                }

                if (mousePos.Y < 0 && Mouse.Captured == this.notesCanvas)
                {
                    this.verticalScroll.Value = this.verticalScroll.Value - this.verticalScroll.SmallChange * delta;
                    needUdpate = true;
                }
                else if (mousePos.Y > notesCanvas.ActualHeight && Mouse.Captured == this.notesCanvas)
                {
                    this.verticalScroll.Value = this.verticalScroll.Value + this.verticalScroll.SmallChange * delta;
                    needUdpate = true;
                }

                if (needUdpate)
                {
                    notesCanvas_MouseMove_Helper(mousePos);
                    if (Mouse.Captured == this.timelineCanvas) timelineCanvas_MouseMove_Helper(mousePos);
                    midiVM.MarkUpdate();
                }
            }
            else if (Mouse.Captured == timelineCanvas && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = Mouse.GetPosition(timelineCanvas);
                timelineCanvas_MouseMove_Helper(mousePos);
                midiVM.MarkUpdate();
            }
        }

        # region Note Canvas

        Rectangle selectionBox;
        Point? selectionStart;
        int _lastNoteLength = 120;

        bool _inMove = false;
        bool _inResize = false;
        bool _vbrInLengthen = false;
        bool _vbrInDeepen = false;
        bool _vbrPeriodLengthen = false;
        bool _vbrPhaseMoving = false;
        bool _vbrInMoving = false;
        bool _vbrOutMoving = false;
        bool _vbrDriftMoving = false;
        UNote _noteHit;
        bool _inPitMove = false;
        PitchPoint _pitHit;
        int _pitHitIndex;
        int _tickMoveRelative;
        int _tickMoveStart;
        UNote _noteMoveNoteLeft;
        UNote _noteMoveNoteRight;
        UNote _noteMoveNoteMin;
        UNote _noteMoveNoteMax;
        UNote _noteResizeShortest;

        private void notesCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (midiVM.Part == null) return;
            Point mousePos = e.GetPosition((Canvas)sender);

            var hit = VisualTreeHelper.HitTest(notesCanvas, mousePos).VisualHit;
            System.Diagnostics.Debug.WriteLine("Mouse hit " + hit.ToString());
            if (midiVM.AnyNotesEditing)
            {
                midiVM.notesElement?.LyricBox?.RaiseEvent(new RoutedEventArgs() { RoutedEvent = LostFocusEvent });
            }
            var pitHitResult = midiHT.HitTestPitchPoint(mousePos);

            if (pitHitResult != null)
            {
                if (pitHitResult.OnPoint)
                {
                    _inPitMove = true;
                    _pitHit = pitHitResult.Note.PitchBend.Points[pitHitResult.Index];
                    _pitHitIndex = pitHitResult.Index;
                    _noteHit = pitHitResult.Note;
                    if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                }
            }
            else
            {
                UNote noteHit = midiHT.HitTestNote(mousePos);
                if (noteHit != null) System.Diagnostics.Debug.WriteLine("Mouse hit" + noteHit.ToString());

                if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    selectionStart = new Point(midiVM.CanvasToQuarter(mousePos.X), midiVM.CanvasToNoteNum(mousePos.Y));

                    if (Keyboard.IsKeyUp(Key.LeftShift) && Keyboard.IsKeyUp(Key.RightShift)) midiVM.DeselectAll();

                    if (selectionBox == null)
                    {
                        selectionBox = new Rectangle()
                        {
                            Stroke = Brushes.Black,
                            StrokeThickness = 2,
                            Fill = ThemeManager.BarNumberBrush,
                            Width = 0,
                            Height = 0,
                            Opacity = 0.5,
                            RadiusX = 8,
                            RadiusY = 8,
                            IsHitTestVisible = false
                        };
                        notesCanvas.Children.Add(selectionBox);
                        Panel.SetZIndex(selectionBox, 1000);
                        selectionBox.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        selectionBox.Width = 0;
                        selectionBox.Height = 0;
                        Panel.SetZIndex(selectionBox, 1000);
                        selectionBox.Visibility = System.Windows.Visibility.Visible;
                    }
                    Mouse.OverrideCursor = Cursors.Cross;
                }
                else
                {
                    if (noteHit != null)
                    {
                        _noteHit = noteHit;
                        if (!midiVM.SelectedNotes.Contains(noteHit)) midiVM.DeselectAll();
                        midiVM.SelectNote(noteHit);
                        if (!noteHit.IsLyricBoxActive) {
                            if (e.ClickCount >= 2)
                            {
                                noteHit.IsLyricBoxActive = true;
                                midiVM.AnyNotesEditing = true;
                                midiVM.UpdateViewRegion(noteHit.EndTick + midiVM.Part.PosTick);
                                midiVM.MarkUpdate();
                                midiVM.notesElement?.MarkUpdate();
                                midiVM.RedrawIfUpdated();
                            }
                            else
                            {
                                if (midiHT.HitNoteResizeArea(noteHit, mousePos))
                                {
                                    if (Keyboard.IsKeyDown(Key.RightAlt))
                                    {
                                        _vbrOutMoving = true;
                                        Mouse.OverrideCursor = Cursors.SizeWE;
                                    }
                                    else
                                    {
                                        // Resize note
                                        _inResize = true;
                                        Mouse.OverrideCursor = Cursors.SizeWE;
                                        if (midiVM.SelectedNotes.Count != 0)
                                        {
                                            _noteResizeShortest = noteHit;
                                            foreach (UNote note in midiVM.SelectedNotes)
                                                if (note.DurTick < _noteResizeShortest.DurTick) _noteResizeShortest = note;
                                        }
                                    }
                                    if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                                }
                                else if (noteHit.Vibrato.IsEnabled)
                                {
                                    if (midiHT.HitTestVibratoLengthenArea(noteHit, mousePos))
                                    {
                                        if (Keyboard.IsKeyDown(Key.LeftAlt) && midiHT.HitTestVibrato(mousePos) == noteHit)
                                        {
                                            _vbrInMoving = true;
                                        }
                                        else
                                        {
                                            _vbrInLengthen = true;
                                        }
                                        Mouse.OverrideCursor = Cursors.SizeWE;
                                        if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                                    }
                                    else if (midiHT.HitTestVibrato(mousePos) == noteHit)
                                    {
                                        if (Keyboard.IsKeyDown(Key.LeftShift))
                                        {
                                            _vbrPeriodLengthen = true;
                                            Mouse.OverrideCursor = Cursors.SizeWE;
                                        }
                                        else if (Keyboard.IsKeyDown(Key.RightShift))
                                        {
                                            _vbrPhaseMoving = true;
                                            Mouse.OverrideCursor = Cursors.SizeWE;
                                        }
                                        else if (Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Control))
                                        {
                                            _vbrDriftMoving = true;
                                            Mouse.OverrideCursor = Cursors.SizeNS;
                                        }
                                        else if (Keyboard.IsKeyDown(Key.LeftAlt))
                                        {
                                            _vbrInMoving = true;
                                            Mouse.OverrideCursor = Cursors.SizeWE;
                                        }
                                        else if (Keyboard.IsKeyDown(Key.RightAlt))
                                        {
                                            _vbrOutMoving = true;
                                            Mouse.OverrideCursor = Cursors.SizeWE;
                                        }
                                        else
                                        {
                                            _vbrInDeepen = true;
                                            Mouse.OverrideCursor = Cursors.SizeNS;
                                        }
                                        if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                                    }
                                }
                                else
                                {
                                    // Move note
                                    _inMove = true;
                                    _tickMoveRelative = midiVM.CanvasToSnappedTick(mousePos.X) - noteHit.PosTick;
                                    _tickMoveStart = noteHit.PosTick;
                                    _lastNoteLength = noteHit.DurTick;
                                    if (midiVM.SelectedNotes.Count > 1)
                                    {
                                        _noteMoveNoteMax = _noteMoveNoteMin = noteHit;
                                        _noteMoveNoteLeft = _noteMoveNoteRight = noteHit;
                                        foreach (UNote note in midiVM.SelectedNotes)
                                        {
                                            if (note.PosTick < _noteMoveNoteLeft.PosTick) _noteMoveNoteLeft = note;
                                            if (note.EndTick > _noteMoveNoteRight.EndTick) _noteMoveNoteRight = note;
                                            if (note.NoteNum < _noteMoveNoteMin.NoteNum) _noteMoveNoteMin = note;
                                            if (note.NoteNum > _noteMoveNoteMax.NoteNum) _noteMoveNoteMax = note;
                                        }
                                    }
                                    if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                                }
                            }
                        }
                    }
                    else if ((_noteHit = midiHT.HitTestVibrato(mousePos)) != null)
                    {
                        if (Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Control))
                        {
                            _vbrDriftMoving = true;
                        }
                        else
                        {
                            _vbrInDeepen = true;
                        }
                        Mouse.OverrideCursor = Cursors.SizeNS;
                        if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                    }
                    else if (!midiVM.SelectedNotes.Any()) // Add note
                    {
                        UNote newNote = DocManager.Inst.Project.CreateNote(
                            midiVM.CanvasToNoteNum(mousePos.Y),
                            midiVM.CanvasToSnappedTick(mousePos.X),
                            _lastNoteLength);
                        newNote.PartNo = midiVM.Part.PartNo;
                        newNote.NoteNo = midiVM.Part.Notes.Count;
                        foreach (var item in newNote.Expressions)
                        {
                            newNote.Expressions[item.Key].Data = midiVM.Part.Expressions[item.Key].Data;
                        }

                        if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                        DocManager.Inst.ExecuteCmd(new AddNoteCommand(midiVM.Part, newNote));
                        if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
                        midiVM.MarkUpdate();
                        // Enable drag
                        midiVM.DeselectAll();
                        midiVM.SelectNote(newNote);
                        _inMove = true;
                        _noteHit = newNote;
                        _tickMoveRelative = 0;
                        _tickMoveStart = newNote.PosTick;
                        if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                    }
                    else
                    {
                        midiVM.DeselectAll();
                    }
                }
            }
            ((UIElement)sender).CaptureMouse();
        }

        private void notesCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (midiVM.Part == null) return;
            if (_inMove || _inResize || _vbrInDeepen || _vbrInLengthen || _vbrPeriodLengthen || _vbrPhaseMoving || _vbrInMoving || _vbrOutMoving || _vbrDriftMoving)
            {
                if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
            }
            _inMove = false;
            _inResize = false;
            _vbrInLengthen = false;
            _vbrInDeepen = false;
            _vbrPeriodLengthen = false;
            _vbrPhaseMoving = false;
            _vbrInMoving = false;
            _vbrOutMoving = false;
            _vbrDriftMoving = false;
            _noteHit = null;
            _inPitMove = false;
            _pitHit = null;
            // End selection
            selectionStart = null;
            if (selectionBox != null)
            {
                Canvas.SetZIndex(selectionBox, -100);
                selectionBox.Visibility = System.Windows.Visibility.Hidden;
            }
            midiVM.DoneTempSelect();
            ((Canvas)sender).ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
        }

        private void notesCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition((Canvas)sender);
            notesCanvas_MouseMove_Helper(mousePos);
        }

        private void notesCanvas_MouseMove_Helper(Point mousePos)
        {
            if (midiVM.Part == null) return;
            if (selectionStart != null) // Selection
            {
                double top = midiVM.NoteNumToCanvas(Math.Max(midiVM.CanvasToNoteNum(mousePos.Y), (int)selectionStart.Value.Y));
                double bottom = midiVM.NoteNumToCanvas(Math.Min(midiVM.CanvasToNoteNum(mousePos.Y), (int)selectionStart.Value.Y) - 1);
                double left = Math.Min(mousePos.X, midiVM.QuarterToCanvas(selectionStart.Value.X));
                selectionBox.Width = Math.Abs(mousePos.X - midiVM.QuarterToCanvas(selectionStart.Value.X));
                selectionBox.Height = bottom - top;
                Canvas.SetLeft(selectionBox, left);
                Canvas.SetTop(selectionBox, top);
                midiVM.TempSelectInBox(selectionStart.Value.X, midiVM.CanvasToQuarter(mousePos.X), (int)selectionStart.Value.Y, midiVM.CanvasToNoteNum(mousePos.Y));
            }
            else if (_inPitMove)
            {
                double tickX = midiVM.CanvasToQuarter(mousePos.X) * DocManager.Inst.Project.Resolution / midiVM.BeatPerBar - _noteHit.PosTick;
                double deltaX = DocManager.Inst.Project.TickToMillisecond(tickX, midiVM.Part.PosTick) - _pitHit.X;
                if (_pitHitIndex != 0) deltaX = Math.Max(deltaX, _noteHit.PitchBend.Points[_pitHitIndex - 1].X - _pitHit.X);
                if (_pitHitIndex != _noteHit.PitchBend.Points.Count - 1) deltaX = Math.Min(deltaX, _noteHit.PitchBend.Points[_pitHitIndex + 1].X - _pitHit.X);
                double deltaY = Keyboard.Modifiers == ModifierKeys.Shift ? Math.Round(midiVM.CanvasToPitch(mousePos.Y) - _noteHit.NoteNum) * 10 - _pitHit.Y :
                    (midiVM.CanvasToPitch(mousePos.Y) - _noteHit.NoteNum) * 10 - _pitHit.Y;
                if (_noteHit.PitchBend.Points.First() == _pitHit && _noteHit.PitchBend.SnapFirst || _noteHit.PitchBend.Points.Last() == _pitHit) deltaY = 0;
                if (deltaX != 0 || deltaY != 0)
                    DocManager.Inst.ExecuteCmd(new MovePitchPointCommand(midiVM.Part, _pitHit, deltaX, deltaY));
            }
            else if (_inMove) // Move Note
            {
                if (midiVM.SelectedNotes.Count <= 1)
                {
                    int newNoteNum = Math.Max(0, Math.Min(UIConstants.MaxNoteNum - 1, midiVM.CanvasToNoteNum(mousePos.Y)));
                    int newPosTick = Math.Max(0, Math.Min((int)(midiVM.QuarterCount * midiVM.Project.Resolution / midiVM.BeatPerBar) - _noteHit.DurTick,
                        (int)(midiVM.Project.Resolution * midiVM.CanvasToSnappedQuarter(mousePos.X) / midiVM.BeatPerBar) - _tickMoveRelative));
                    if (newNoteNum != _noteHit.NoteNum || newPosTick != _noteHit.PosTick)
                        DocManager.Inst.ExecuteCmd(new MoveNoteCommand(midiVM.Part, _noteHit, newPosTick - _noteHit.PosTick, newNoteNum - _noteHit.NoteNum));
                }
                else
                {
                    int deltaNoteNum = midiVM.CanvasToNoteNum(mousePos.Y) - _noteHit.NoteNum;
                    int deltaPosTick = ((int)(midiVM.Project.Resolution * midiVM.CanvasToSnappedQuarter(mousePos.X) / midiVM.BeatPerBar) - _tickMoveRelative) - _noteHit.PosTick;

                    if (deltaNoteNum != 0 || deltaPosTick != 0)
                    {
                        bool changeNoteNum = deltaNoteNum + _noteMoveNoteMin.NoteNum >= 0 && deltaNoteNum + _noteMoveNoteMax.NoteNum < UIConstants.MaxNoteNum;
                        bool changePosTick = deltaPosTick + _noteMoveNoteLeft.PosTick >= 0 && deltaPosTick + _noteMoveNoteRight.EndTick <= midiVM.QuarterCount * midiVM.Project.Resolution / midiVM.BeatPerBar;
                        if (changeNoteNum || changePosTick)

                            DocManager.Inst.ExecuteCmd(new MoveNoteCommand(midiVM.Part, midiVM.SelectedNotes,
                                    changePosTick ? deltaPosTick : 0, changeNoteNum ? deltaNoteNum : 0));
                    }
                }
                Mouse.OverrideCursor = Cursors.SizeAll;
            }
            else if (_inResize) // resize
            {
                if (midiVM.SelectedNotes.Count <= 1)
                {
                    int newDurTick = (int)(midiVM.CanvasRoundToSnappedQuarter(mousePos.X) * midiVM.Project.Resolution / midiVM.BeatPerBar) - _noteHit.PosTick;
                    if (newDurTick != _noteHit.DurTick && newDurTick >= midiVM.GetSnapUnit() * midiVM.Project.Resolution / midiVM.BeatPerBar)
                    {
                        DocManager.Inst.ExecuteCmd(new ResizeNoteCommand(midiVM.Part, _noteHit, newDurTick - _noteHit.DurTick));
                        _lastNoteLength = newDurTick;
                    }
                }
                else
                {
                    int deltaDurTick = (int)(midiVM.CanvasRoundToSnappedQuarter(mousePos.X) * midiVM.Project.Resolution / midiVM.BeatPerBar) - _noteHit.EndTick;
                    if (deltaDurTick != 0 && deltaDurTick + _noteResizeShortest.DurTick > midiVM.GetSnapUnit())
                    {
                        DocManager.Inst.ExecuteCmd(new ResizeNoteCommand(midiVM.Part, midiVM.SelectedNotes, deltaDurTick));
                        _lastNoteLength = _noteHit.DurTick;
                    }
                }
            }
            else if (_vbrInLengthen) {
                int deltaDurTick = _noteHit.EndTick - (int)(midiVM.CanvasToQuarter(mousePos.X) * midiVM.Project.Resolution / midiVM.BeatPerBar);
                if (deltaDurTick > 0)
                {
                    var newlen = (double)deltaDurTick / _noteHit.DurTick * 100;
                    DocManager.Inst.ExecuteCmd(new UpdateNoteVibratoCommand(_noteHit, len: newlen));
                }
            }
            else if (_vbrInDeepen) {
                double pitch = midiVM.CanvasToPitch(mousePos.Y);
                DocManager.Inst.ExecuteCmd(new UpdateNoteVibratoCommand(_noteHit, dep: Math.Abs(_noteHit.NoteNum - pitch) * 100));
            }
            else if (_vbrPeriodLengthen) {
                int deltaDurTick = _noteHit.EndTick - (int)(midiVM.CanvasToQuarter(mousePos.X) * midiVM.Project.Resolution / midiVM.BeatPerBar);
                double lengthX = _noteHit.DurTick * _noteHit.Vibrato.Length / 100;
                DocManager.Inst.ExecuteCmd(new UpdateNoteVibratoCommand(_noteHit, per: (lengthX - deltaDurTick) / lengthX * (512 - 64) + 64));
            }
            else if (_vbrPhaseMoving)
            {
                int deltaDurTick = _noteHit.EndTick - (int)(midiVM.CanvasToQuarter(mousePos.X) * midiVM.Project.Resolution / midiVM.BeatPerBar);
                double lengthX = _noteHit.DurTick * _noteHit.Vibrato.Length / 100;
                DocManager.Inst.ExecuteCmd(new UpdateNoteVibratoCommand(_noteHit, shift: (lengthX - deltaDurTick) / lengthX * 100));
            }
            else if (_vbrInMoving)
            {
                int deltaDurTick = _noteHit.EndTick - (int)(midiVM.CanvasToQuarter(mousePos.X) * midiVM.Project.Resolution / midiVM.BeatPerBar);
                double lengthX = _noteHit.DurTick * _noteHit.Vibrato.Length / 100;
                DocManager.Inst.ExecuteCmd(new UpdateNoteVibratoCommand(_noteHit, din: (lengthX - deltaDurTick) / lengthX * 100));
            }
            else if (_vbrOutMoving)
            {
                int deltaDurTick = _noteHit.EndTick - (int)(midiVM.CanvasToQuarter(mousePos.X) * midiVM.Project.Resolution / midiVM.BeatPerBar);
                double lengthX = _noteHit.DurTick * _noteHit.Vibrato.Length / 100;
                DocManager.Inst.ExecuteCmd(new UpdateNoteVibratoCommand(_noteHit, dout: (1 - (lengthX - deltaDurTick) / lengthX) * 100));
            }
            else if (_vbrDriftMoving)
            {
                double pitch = midiVM.CanvasToPitch(mousePos.Y);
                DocManager.Inst.ExecuteCmd(new UpdateNoteVibratoCommand(_noteHit, drift: (_noteHit.NoteNum - pitch) * 100));
            }
            else if (Mouse.RightButton == MouseButtonState.Pressed && ToolUsing == EnumTool.Brush) // Remove Note
            {
                UNote noteHit = midiHT.HitTestNote(mousePos);
                if (noteHit != null) DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, noteHit));
            }
            else if (Mouse.LeftButton == MouseButtonState.Released && Mouse.RightButton == MouseButtonState.Released)
            {
                var pitHit = midiHT.HitTestPitchPoint(mousePos);
                if (pitHit != null)
                {
                    Mouse.OverrideCursor = Cursors.Hand;
                }
                else
                {
                    UNote noteHit = midiHT.HitTestNote(mousePos);
                    if (noteHit != null && (midiHT.HitNoteResizeArea(noteHit, mousePos) || midiHT.HitTestVibratoLengthenArea(noteHit, mousePos)))
                        Mouse.OverrideCursor = Cursors.SizeWE;
                    else
                    {
                        UNote vibHit = midiHT.HitTestVibrato(mousePos);
                        if (vibHit != null)
                        {
                            Mouse.OverrideCursor = Cursors.SizeNS;
                        }
                        else Mouse.OverrideCursor = null;
                    }
                }
            }
        }

        private void notesCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (midiVM.Part == null) return;
            Point mousePos = e.GetPosition((Canvas)sender);

            var pitHit = midiHT.HitTestPitchPoint(mousePos);
            if (pitHit != null)
            {
                Mouse.OverrideCursor = null;
                pitHitContainer.Result = pitHit;

                if (pitHit.OnPoint)
                {
                    ((MenuItem)pitchCxtMenu.Items[4]).Header = pitHit.Note.PitchBend.SnapFirst ? "Unsnap from previous point" : "Snap to previous point";
                    ((MenuItem)pitchCxtMenu.Items[4]).Visibility = pitHit.Index == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

                    if (pitHit.Index == 0 || pitHit.Index == pitHit.Note.PitchBend.Points.Count - 1) ((MenuItem)pitchCxtMenu.Items[5]).Visibility = System.Windows.Visibility.Collapsed;
                    else ((MenuItem)pitchCxtMenu.Items[5]).Visibility = System.Windows.Visibility.Visible;

                    ((MenuItem)pitchCxtMenu.Items[6]).Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    ((MenuItem)pitchCxtMenu.Items[4]).Visibility = System.Windows.Visibility.Collapsed;
                    ((MenuItem)pitchCxtMenu.Items[5]).Visibility = System.Windows.Visibility.Collapsed;
                    ((MenuItem)pitchCxtMenu.Items[6]).Visibility = System.Windows.Visibility.Visible;
                }

                pitchCxtMenu.IsOpen = true;
                pitchCxtMenu.PlacementTarget = this.notesCanvas;
            }
            else
            {
                UNote noteHit = midiHT.HitTestNote(mousePos);
                if (ToolUsing == EnumTool.Brush)
                {
                    if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                    if (noteHit != null && midiVM.SelectedNotes.Contains(noteHit))
                        DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, noteHit));
                    else midiVM.DeselectAll();
                    ((UIElement)sender).CaptureMouse();
                    Mouse.OverrideCursor = Cursors.No;
                }
                else if(ToolUsing == EnumTool.Cursor)
                {
                    if (noteHit != null) {
                        bool vibratoenabled = noteHit.Vibrato.IsEnabled;
                        var menu = new ContextMenu();
                        var i0 = new MenuItem() { Header = "Delete note" };
                        i0.Click += (_o, _e) => {
                            if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                            DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, noteHit));
                            if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
                        };
                        menu.Items.Add(i0);
                        var i1 = new MenuItem()
                        {
                            Header = vibratoenabled ? "Disable Vibrato" : "Enable Vibrato"
                        };
                        i1.Click += (_o, _e) => {
                            if (vibratoenabled)
                            {
                                noteHit.Vibrato.Disable();
                            }
                            else
                            {
                                noteHit.Vibrato.Enable(true);
                            }
                        };
                        menu.Items.Add(i1);
                        menu.IsOpen = true;
                        menu.PlacementTarget = this.notesCanvas;
                    }
                    else
                    {
                        midiVM.DeselectAll();
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("Total notes: " + midiVM.Part.Notes.Count + " selected: " + midiVM.SelectedNotes.Count);
        }

        private void notesCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (midiVM.Part == null) return;
            Mouse.OverrideCursor = null;
            ((UIElement)sender).ReleaseMouseCapture();
            if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
        }

        private void notesCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                timelineCanvas_MouseWheel(sender, e);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                midiVM.OffsetX -= midiVM.ViewWidth * 0.001 * e.Delta;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
            }
            else
            {
                verticalScroll.Value -= verticalScroll.SmallChange * e.Delta / 100;
                verticalScroll.Value = Math.Max(verticalScroll.Minimum, Math.Min(verticalScroll.Maximum, verticalScroll.Value));
            }
        }

        # endregion

        #region Navigate Drag

        private void navigateDrag_NavDrag(object sender, EventArgs e)
        {
            midiVM.OffsetX += ((NavDragEventArgs)e).X * midiVM.SmallChangeX;
            midiVM.OffsetY += ((NavDragEventArgs)e).Y * midiVM.SmallChangeY * 0.5;
            midiVM.MarkUpdate();
        }

        #endregion

        # region Timeline Canvas
        
        private void timelineCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomSpeed = 0.0012;
            Point mousePos = e.GetPosition((UIElement)sender);
            double zoomCenter;
            if (midiVM.OffsetX == 0 && mousePos.X < 128) zoomCenter = 0;
            else zoomCenter = (midiVM.OffsetX + mousePos.X) / midiVM.QuarterWidth;
            midiVM.QuarterWidth *= 1 + e.Delta * zoomSpeed;
            midiVM.OffsetX = Math.Max(0, Math.Min(midiVM.TotalWidth, zoomCenter * midiVM.QuarterWidth - mousePos.X));
        }

        private void viewScalerX_ViewScaled(object sender, EventArgs e)
        {
            if (e is ViewScaledEventArgs args)
            {
                double zoomCenter = midiVM.OffsetX / midiVM.QuarterWidth;
                midiVM.QuarterWidth = args.Value;
                midiVM.OffsetX = Math.Max(0, Math.Min(midiVM.TotalWidth, zoomCenter * midiVM.QuarterWidth));
            }
        }

        private void timelineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (midiVM.Part == null) return;
            Point mousePos = e.GetPosition((UIElement)sender);
            int tick = (int)(midiVM.CanvasToSnappedQuarter(mousePos.X) * midiVM.Project.Resolution / midiVM.Project.BeatPerBar);
            DocManager.Inst.ExecuteCmd(new SeekPlayPosTickNotification(Math.Max(0, tick) + midiVM.Part.PosTick));
            ((Canvas)sender).CaptureMouse();
        }

        private void timelineCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            timelineCanvas_MouseMove_Helper(e.GetPosition(sender as UIElement));
        }

        private void timelineCanvas_MouseMove_Helper(Point mousePos)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed && Mouse.Captured == timelineCanvas)
            {
                int tick = (int)(midiVM.CanvasToSnappedQuarter(mousePos.X) * midiVM.Project.Resolution / midiVM.Project.BeatPerBar);
                if (midiVM.playPosTick != tick + midiVM.Part.PosTick)
                    DocManager.Inst.ExecuteCmd(new SeekPlayPosTickNotification(Math.Max(0, tick) + midiVM.Part.PosTick));
            }
        }

        private void timelineCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((Canvas)sender).ReleaseMouseCapture();
        }

        # endregion

        #region Keys Action

        // TODO : keys mouse over, click, release, click and move

        private void keysCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void keysCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void keysCanvas_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void keysCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //this.notesVerticalScroll.Value = this.notesVerticalScroll.Value - 0.01 * notesVerticalScroll.SmallChange * e.Delta;
            //ncModel.updateGraphics();
        }

        # endregion

        protected override void OnKeyDown(KeyEventArgs e)
        {
            Window_KeyDown(this, e);
            if(!midiVM.AnyNotesEditing && !LyricsPresetDedicate)
                e.Handled = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.F4)
            {
                this.Close();
            }
            else if (midiVM.Part != null && !midiVM.AnyNotesEditing)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control) // Ctrl
                {
                    if (e.Key == Key.A)
                    {
                        midiVM.SelectAll();
                    }
                    else if (!LyricsPresetDedicate)
                    {
                        if (e.Key == Key.Z)
                        {
                            midiVM.DeselectAll();
                            DocManager.Inst.Undo();
                        }
                        else if (e.Key == Key.Y)
                        {
                            midiVM.DeselectAll();
                            DocManager.Inst.Redo();
                        }
                        else if (e.Key == Key.X)
                        {
                            MenuCut_Click(this, new RoutedEventArgs());
                        }
                        else if (e.Key == Key.C)
                        {
                            MenuCopy_Click(this, new RoutedEventArgs());
                        }
                        else if (e.Key == Key.V)
                        {
                            MenuPaste_Click(this, new RoutedEventArgs());
                        }
                    }
                }
                else if (Keyboard.Modifiers == 0) // No midifiers
                {
                    if (e.Key == Key.Delete)
                    {
                        if (midiVM.SelectedNotes.Count > 0)
                        {
                            if(!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
                            DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, midiVM.SelectedNotes));
                            if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
                        }
                    }
                    else if (e.Key == Key.I)
                    {
                        if (!LyricsPresetDedicate) midiVM.ShowPitch = !midiVM.ShowPitch;
                    }
                    else if (e.Key == Key.O)
                    {
                        if (!LyricsPresetDedicate) midiVM.ShowPhoneme = !midiVM.ShowPhoneme;
                    }
                    else if (e.Key == Key.P)
                    {
                        if (!LyricsPresetDedicate) midiVM.Snap = !midiVM.Snap;
                    }
                    else if (e.Key == Key.Enter) {
                        if (Core.Util.Preferences.Default.EnterToEdit && midiVM.SelectedNotes.Any()) {
                            midiVM.SelectedNotes.First().IsLyricBoxActive = true;
                            midiVM.AnyNotesEditing = true;
                            midiVM.UpdateViewRegion(midiVM.SelectedNotes.First().EndTick + midiVM.Part.PosTick);
                            midiVM.MarkUpdate();
                            midiVM.notesElement?.MarkUpdate();
                            midiVM.RedrawIfUpdated();
                        }
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //e.Cancel = true;
            //this.Hide();
        }
        private void expCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((Canvas)sender).CaptureMouse();
            if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
            Point mousePos = e.GetPosition((UIElement)sender);
            expCanvas_SetExpHelper(mousePos);
        }

        private void expCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
            ((Canvas)sender).ReleaseMouseCapture();
        }

        private void expCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition((UIElement)sender);
                expCanvas_SetExpHelper(mousePos);
            }
        }

        private void expCanvas_SetExpHelper(Point mousePos)
        {
            if (midiVM.Part == null) return;
            int newValue;
            string _key = midiVM.visibleExpElement.Key;
            var _expTemplate = DocManager.Inst.Project.ExpressionTable[_key] as IntExpression;
            if (Keyboard.Modifiers == ModifierKeys.Alt) newValue = (int)_expTemplate.Data;
            else newValue = (int)Math.Max(_expTemplate.Min, Math.Min(_expTemplate.Max, (1 - mousePos.Y / expCanvas.ActualHeight) * (_expTemplate.Max - _expTemplate.Min) + _expTemplate.Min));
            UNote note = midiHT.HitTestNoteX(mousePos.X);
            if (midiVM.SelectedNotes.Count == 0 || midiVM.SelectedNotes.Contains(note))
            {
                if (note != null)
                {
                    if (Keyboard.Modifiers == ModifierKeys.Alt) newValue = (int)(midiVM.Part.Expressions[_key] as IntExpression).Data;
                    DocManager.Inst.ExecuteCmd(new SetIntExpCommand(midiVM.Part, note, midiVM.visibleExpElement.Key, newValue));
                }
                else
                {
                    DocManager.Inst.ExecuteCmd(new GlobelSetIntExpCommand(midiVM.Part, midiVM.visibleExpElement.Key, newValue));
                }
            }
        }

        private void mainButton_Click(object sender, RoutedEventArgs e)
        {
            DocManager.Inst.ExecuteCmd(new ShowPitchExpNotification());
        }

        private void horizontalScroll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            midiVM.HorizontalPropertiesChanged();
            midiVM.shadowExpElement?.MarkUpdate();
            midiVM.visibleExpElement?.MarkUpdate();
            midiVM.MarkUpdate();
            midiVM.RedrawIfUpdated();
        }

        private void MenuUndo_Click(object sender, RoutedEventArgs e) { DocManager.Inst.Undo(); }
        private void MenuRedo_Click(object sender, RoutedEventArgs e) { DocManager.Inst.Redo(); }
        private void MenuCut_Click(object sender, RoutedEventArgs e)
        {
            midiVM.CopyNotes();
            var pre = new List<UNote>(midiVM.SelectedNotes);
            if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
            foreach (var item in midiVM.SelectedNotes)
            {
                DocManager.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, item), true);
            }
            if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
            midiVM.DeselectAll();
        }

        private void MenuCopy_Click(object sender, RoutedEventArgs e)
        {
            midiVM.CopyNotes();
        }

        private void MenuPaste_Click(object sender, RoutedEventArgs e)
        {
            int basedelta = int.MaxValue;
            foreach (var note in midiVM.ClippedNotes)
            {
                basedelta = Math.Min(basedelta, note.PosTick);
            }
            if (!LyricsPresetDedicate) DocManager.Inst.StartUndoGroup();
            foreach (var note in midiVM.ClippedNotes)
            {
                var copied = note.Clone();
                copied.PosTick = DocManager.Inst.playPosTick - midiVM.Part.PosTick + note.PosTick - basedelta;
                DocManager.Inst.ExecuteCmd(new AddNoteCommand(midiVM.Part, copied));
            }
            if (!LyricsPresetDedicate) DocManager.Inst.EndUndoGroup();
        }

    }
}
