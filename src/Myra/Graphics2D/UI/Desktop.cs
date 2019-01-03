﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI.Styles;
using Myra.Utility;

namespace Myra.Graphics2D.UI
{
	public class Desktop
	{
		public const int DoubleClickIntervalInMs = 500;

		private RenderContext _renderContext;

		private bool _layoutDirty = true;
		private Rectangle _bounds;
		private bool _widgetsDirty = true;
		private Widget _focusedWidget;
		private readonly List<Widget> _widgetsCopy = new List<Widget>();
		private readonly List<Widget> _reversedWidgetsCopy = new List<Widget>();
		protected readonly ObservableCollection<Widget> _widgets = new ObservableCollection<Widget>();
		private readonly List<Widget> _focusableWidgets = new List<Widget>();
		private DateTime _lastTouch;

		public Point MousePosition { get; private set; }
		public int MouseWheel { get; private set; }
		public MouseState MouseState { get; private set; }
		public KeyboardState KeyboardState { get; private set; }
		public HorizontalMenu MenuBar { get; set; }

		private IEnumerable<Widget> WidgetsCopy
		{
			get
			{
				UpdateWidgetsCopy();
				return _widgetsCopy;
			}
		}

		private IEnumerable<Widget> ReversedWidgetsCopy
		{
			get
			{
				UpdateWidgetsCopy();
				return _reversedWidgetsCopy;
			}
		}

		public ObservableCollection<Widget> Widgets
		{
			get { return _widgets; }
		}

		public Rectangle Bounds
		{
			get { return _bounds; }

			set
			{
				if (value == _bounds)
				{
					return;
				}

				_bounds = value;
				InvalidateLayout();
			}
		}

		public Widget ContextMenu { get; private set; }

		public Widget FocusedWidget
		{
			get { return _focusedWidget; }

			set
			{
				if (value == _focusedWidget)
				{
					return;
				}

				if (_focusedWidget != null)
				{
					_focusedWidget.IterateFocusable(w => w.IsFocused = false);
				}

				_focusedWidget = value;

				if (_focusedWidget != null)
				{
					_focusedWidget.IterateFocusable(w => w.IsFocused = true);
				}
			}
		}

		private RenderContext RenderContext
		{
			get
			{
				EnsureRenderContext();

				return _renderContext;
			}
		}

		/// <summary>
		/// Parameters passed to SpriteBatch.Begin
		/// </summary>
		public SpriteBatchBeginParams SpriteBatchBeginParams
		{
			get
			{
				return RenderContext.SpriteBatchBeginParams;
			}

			set
			{
				RenderContext.SpriteBatchBeginParams = value;
			}
		}

		public float Opacity { get; set; }

		public bool IsMouseOverGUI
		{
			get
			{
				return IsPointOverGUI(MousePosition);
			}
		}

		public bool IsTouchDown
		{
			get; private set;
		}

		public Func<MouseState> MouseStateGetter { get; set; }
		public Func<KeyboardState> KeyboardStateGetter { get; set; }

		public event EventHandler<GenericEventArgs<Point>> MouseMoved;
		public event EventHandler<GenericEventArgs<MouseButtons>> MouseDown;
		public event EventHandler<GenericEventArgs<MouseButtons>> MouseUp;
		public event EventHandler<GenericEventArgs<MouseButtons>> MouseDoubleClick;

		public event EventHandler<GenericEventArgs<float>> MouseWheelChanged;

		public event EventHandler<GenericEventArgs<Keys>> KeyUp;
		public event EventHandler<GenericEventArgs<Keys>> KeyDown;
		public event EventHandler<GenericEventArgs<char>> Char;

		public event EventHandler<ContextMenuClosingEventArgs> ContextMenuClosing;
		public event EventHandler<GenericEventArgs<Widget>> ContextMenuClosed;

		public Desktop()
		{
			Opacity = 1.0f;
			_widgets.CollectionChanged += WidgetsOnCollectionChanged;

			MouseStateGetter = Mouse.GetState;
			KeyboardStateGetter = Keyboard.GetState;

#if MONOGAME
			MyraEnvironment.Game.Window.TextInput += (s, a) =>
			{
				OnChar(a.Character);
			};
#elif FNA
			TextInputEXT.TextInput += c =>
			{
				OnChar(c);
			};
#endif
		}

		private void OnChar(char c)
		{
			var ev = Char;
			if (ev != null)
			{
				ev(this, new GenericEventArgs<char>(c));
			}

			if (_focusedWidget != null)
			{
				_focusedWidget.IterateFocusable(w => w.OnChar(c));
			}
		}

		private void InputOnMouseDown()
		{
			if (ContextMenu != null && !ContextMenu.Bounds.Contains(MousePosition))
			{
				var ev = ContextMenuClosing;
				if (ev != null)
				{
					var args = new ContextMenuClosingEventArgs(ContextMenu);
					ev(this, args);

					if (args.Cancel)
					{
						return;
					}
				}

				HideContextMenu();
			}
		}

		public void ShowContextMenu(Widget menu, Point position)
		{
			if (menu == null)
			{
				throw new ArgumentNullException("menu");
			}

			HideContextMenu();

			ContextMenu = menu;

			if (ContextMenu != null)
			{
				ContextMenu.HorizontalAlignment = HorizontalAlignment.Left;
				ContextMenu.VerticalAlignment = VerticalAlignment.Top;

				ContextMenu.Left = position.X;
				ContextMenu.Top = position.Y;

				ContextMenu.Visible = true;

				_widgets.Add(ContextMenu);
			}
		}

		public void HideContextMenu()
		{
			if (ContextMenu == null)
			{
				return;
			}

			_widgets.Remove(ContextMenu);
			ContextMenu.Visible = false;

			var ev = ContextMenuClosed;
			if (ev != null)
			{
				ev(this, new GenericEventArgs<Widget>(ContextMenu));
			}

			ContextMenu = null;
		}

		private void WidgetsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			if (args.Action == NotifyCollectionChangedAction.Add)
			{
				foreach (Widget w in args.NewItems)
				{
					w.Desktop = this;
					w.MeasureChanged += WOnMeasureChanged;
				}
			}
			else if (args.Action == NotifyCollectionChangedAction.Remove)
			{
				foreach (Widget w in args.OldItems)
				{
					w.MeasureChanged -= WOnMeasureChanged;
					w.Desktop = null;
				}
			}

			InvalidateLayout();
			_widgetsDirty = true;
		}

		private void WOnMeasureChanged(object sender, EventArgs eventArgs)
		{
			InvalidateLayout();
		}

		private void EnsureRenderContext()
		{
			if (_renderContext == null)
			{
				var spriteBatch = new SpriteBatch(MyraEnvironment.GraphicsDevice);
				_renderContext = new RenderContext
				{
					Batch = spriteBatch
				};
			}
		}

		public void Render()
		{
			if (Bounds.IsEmpty)
			{
				return;
			}

			UpdateInput();
			UpdateLayout();

			EnsureRenderContext();

			var oldScissorRectangle = _renderContext.Batch.GraphicsDevice.ScissorRectangle;

			_renderContext.Begin();

			_renderContext.Batch.GraphicsDevice.ScissorRectangle = Bounds;
			_renderContext.View = Bounds;
			_renderContext.Opacity = Opacity;

			if (Stylesheet.Current.DesktopStyle != null && 
				Stylesheet.Current.DesktopStyle.Background != null)
			{
				_renderContext.Draw(Stylesheet.Current.DesktopStyle.Background, Bounds);
			}

			foreach (var widget in WidgetsCopy)
			{
				if (widget.Visible)
				{
					widget.Render(_renderContext);
				}
			}

			_renderContext.End();
			_renderContext.Batch.GraphicsDevice.ScissorRectangle = oldScissorRectangle;
		}

		public void InvalidateLayout()
		{
			_layoutDirty = true;
		}

		public void UpdateLayout()
		{
			if (!_layoutDirty)
			{
				return;
			}

			ProcessWidgets();

			foreach (var widget in WidgetsCopy)
			{
				if (widget.Visible)
				{
					widget.Layout(_bounds);
				}
			}

			_layoutDirty = false;
		}

		public int CalculateTotalWidgets(bool visibleOnly)
		{
			var result = 0;
			foreach (var w in _widgets)
			{
				if (visibleOnly && !w.Visible)
				{
					continue;
				}

				++result;

				var asContainer = w as Container;
				if (asContainer != null)
				{
					result += asContainer.CalculateTotalChildCount(visibleOnly);
				}
			}

			return result;
		}

		public void HandleButton(ButtonState buttonState, ButtonState lastState, MouseButtons buttons)
		{
			if (buttonState == ButtonState.Pressed && lastState == ButtonState.Released)
			{
				IsTouchDown = true;

				var ev = MouseDown;
				if (ev != null)
				{
					ev(this, new GenericEventArgs<MouseButtons>(buttons));
				}

				InputOnMouseDown();
				ReversedWidgetsCopy.HandleMouseDown(buttons);

				if ((DateTime.Now - _lastTouch).TotalMilliseconds < DoubleClickIntervalInMs)
				{
					// Double click
					var ev2 = MouseDoubleClick;
					if (ev2 != null)
					{
						ev2(this, new GenericEventArgs<MouseButtons>(buttons));
					}

					ReversedWidgetsCopy.HandleMouseDoubleClick(buttons);

					_lastTouch = DateTime.MinValue;
				}
				else
				{
					_lastTouch = DateTime.Now;
				}
			}
			else if (buttonState == ButtonState.Released && lastState == ButtonState.Pressed)
			{
				IsTouchDown = false;

				var ev = MouseUp;
				if (ev != null)
				{
					ev(this, new GenericEventArgs<MouseButtons>(buttons));
				}

				ReversedWidgetsCopy.HandleMouseUp(buttons);
			}
		}

		public void UpdateInput()
		{
			if (MouseStateGetter != null)
			{
				var lastState = MouseState;

				MouseState = MouseStateGetter();
				MousePosition = new Point(MouseState.X, MouseState.Y);

				if (SpriteBatchBeginParams.TransformMatrix != null)
				{
					// Apply transform
					var t = Vector2.Transform(
						new Vector2(MousePosition.X, MousePosition.Y),
						SpriteBatchBeginParams.InverseTransform);

					MousePosition = new Point((int)t.X, (int)t.Y);
				}

				MouseWheel = MouseState.ScrollWheelValue;

				if (MouseState.X != lastState.X || MouseState.Y != lastState.Y)
				{
					var ev = MouseMoved;
					if (ev != null)
					{
						ev(this, new GenericEventArgs<Point>(MousePosition));
					}

					ReversedWidgetsCopy.HandleMouseMovement(MousePosition);
				}

				HandleButton(MouseState.LeftButton, lastState.LeftButton, MouseButtons.Left);
				HandleButton(MouseState.MiddleButton, lastState.MiddleButton, MouseButtons.Middle);
				HandleButton(MouseState.RightButton, lastState.RightButton, MouseButtons.Right);

				if (MouseWheel != lastState.ScrollWheelValue)
				{
					var delta = MouseWheel - lastState.ScrollWheelValue;
					var ev = MouseWheelChanged;
					if (ev != null)
					{
						ev(null, new GenericEventArgs<float>(delta));
					}

					if (_focusedWidget != null)
					{
						_focusedWidget.IterateFocusable(w => w.OnMouseWheel(delta));
					}
				}
			}

			if (KeyboardStateGetter != null)
			{
				var lastKeyboardState = KeyboardState;
				KeyboardState = Keyboard.GetState();

				var pressedKeys = KeyboardState.GetPressedKeys();

				MyraEnvironment.ShowUnderscores = (MenuBar != null && MenuBar.OpenMenuItem != null) ||
				                                  KeyboardState.IsKeyDown(Keys.LeftAlt) ||
				                                  KeyboardState.IsKeyDown(Keys.RightAlt);

				for (var i = 0; i < pressedKeys.Length; ++i)
				{
					var key = pressedKeys[i];
					if (!lastKeyboardState.IsKeyDown(key))
					{
						var ev = KeyDown;
						if (ev != null)
						{
							ev(this, new GenericEventArgs<Keys>(key));
						}

						if (MenuBar != null && MyraEnvironment.ShowUnderscores)
						{
							MenuBar.OnKeyDown(key);
						}
						else
						{
							if (_focusedWidget != null)
							{
								_focusedWidget.IterateFocusable(w => w.OnKeyDown(key));
							}
						}

						if (key == Keys.Escape && ContextMenu != null)
						{
							HideContextMenu();
						}
					}
				}

				var lastPressedKeys = lastKeyboardState.GetPressedKeys();
				for (var i = 0; i < lastPressedKeys.Length; ++i)
				{
					var key = lastPressedKeys[i];
					if (!KeyboardState.IsKeyDown(key))
					{
						// Key had been released
						var ev = KeyUp;
						if (ev != null)
						{
							ev(this, new GenericEventArgs<Keys>(key));
						}

						if (_focusedWidget != null)
						{
							_focusedWidget.IterateFocusable(w => w.OnKeyUp(key));
						}
					}
				}
			}
		}

		internal void AddFocusableWidget(Widget w)
		{
			w.MouseDown += FocusableWidgetOnMouseDown;
			_focusableWidgets.Add(w);
		}

		internal void RemoveFocusableWidget(Widget w)
		{
			w.MouseDown -= FocusableWidgetOnMouseDown;
			_focusableWidgets.Remove(w);
		}

		private void ProcessWidgets(IEnumerable<Widget> widgets)
		{
			foreach (var w in widgets)
			{
				if (!w.Visible)
				{
					continue;
				}


				if (MenuBar == null && w is HorizontalMenu)
				{
					MenuBar = (HorizontalMenu)w;
				}

				var asContainer = w as Container;
				if (asContainer != null)
				{
					ProcessWidgets(asContainer.ChildrenCopy);
				}
			}
		}

		private void ProcessWidgets()
		{
			MenuBar = null;

			ProcessWidgets(_widgets);
		}

		private void FocusableWidgetOnMouseDown(object sender, GenericEventArgs<MouseButtons> genericEventArgs)
		{
			var widget = (Widget)sender;

			if (!widget.IsFocused)
			{
				FocusedWidget = widget;
			}
		}

		private void UpdateWidgetsCopy()
		{
			if (!_widgetsDirty)
			{
				return;
			}

			_widgetsCopy.Clear();
			_widgetsCopy.AddRange(_widgets);

			_reversedWidgetsCopy.Clear();
			_reversedWidgetsCopy.AddRange(_widgets.Reverse());

			_widgetsDirty = false;
		}

		private bool InternalIsPointOverGUI(Point p, Widget w)
		{
			if (!w.Visible || !w.ActualBounds.Contains(p))
			{
				return false;
			}

			// Non containers are completely solid
			var asContainer = w as Container;
			if (asContainer == null)
			{
				return true;
			}

			// Not real containers are solid as well
			if (!(w is Grid ||
				w is Panel ||
				w is SplitPane ||
				w is ScrollPane))
			{
				return true;
			}

			// Real containers are solid only if backround is set
			if (w.Background != null)
			{
				return true;
			}

			var asScrollPane = w as ScrollPane;
			if (asScrollPane != null)
			{
				// Special case
				if (asScrollPane._horizontalScrollbarVisible && asScrollPane._horizontalScrollbarFrame.Contains(p) ||
					asScrollPane._verticalScrollbarVisible && asScrollPane._verticalScrollbarFrame.Contains(p))
				{
					return true;
				}
			}

			// Or if any child is solid
			foreach (var ch in asContainer.ChildrenCopy)
			{
				if (InternalIsPointOverGUI(p, ch))
				{
					return true;
				}
			}

			return false;
		}

		public bool IsPointOverGUI(Point p)
		{
			foreach (var widget in WidgetsCopy)
			{
				if (InternalIsPointOverGUI(p, widget))
				{
					return true;
				}
			}

			return false;
		}
	}
}