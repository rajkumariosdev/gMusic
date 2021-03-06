﻿// This file has been autogenerated from a class added in the UI designer.

using System;

using Foundation;
using AppKit;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Managers;
using System.Reactive.Linq;
using MusicPlayer.Data;

namespace MusicPlayer
{
	public partial class AppDelegate : NSApplicationDelegate, INSOutlineViewDelegate, INSMenuDelegate, INSSplitViewDelegate
	{
		VideoPlaybackWindowController fullScreenController;
		public List<Element> Elements = new List<Element> {
			new MenuImageElement{ Text = "Search", Svg = "SVG/search.svg", NavigateView = new NSNavigationController(new SearchView()) },
			new MenuSection ("my music") {
				new MenuImageElement{ Text = "Artists", Svg = "SVG/artist.svg", NavigateView = new ArtistView () },
				new MenuImageElement{ Text = "Albums", Svg = "SVG/album.svg", NavigateView = new AlbumView () },
				new MenuImageElement{ Text = "Genres", Svg = "SVG/genres.svg", NavigateView = new GenreView ()  },
				new MenuImageElement{ Text = "Songs", Svg = "SVG/songs.svg", NavigateView = new SongView () },
				new MenuImageElement{ Text = "Playlists", Svg = "SVG/playlists.svg", NavigateView = new PlaylistView ()  },
			},
			new MenuSection ("online") {
				new MenuImageElement{ Text = "Radio", Svg = "SVG/radio.svg", NavigateView = new RadioStationView () },
			},
			new MenuSection ("settings") {
				new MenuSwitchElement {
					Text = "Offline Only",
					Svg = "SVG/offline.svg",
					ShouldDeselect = true,
					Value = Settings.ShowOfflineOnly,
					ValueChanged = (b) => {
						Settings.ShowOfflineOnly = b;
					}
				},
				new EqualizerMenuElement { Text = "Equalizer", Svg = "SVG/equalizer.svg",
					ValueChanged = (b) => { 
						MusicPlayer.Playback.Equalizer.Shared.Active = b;
					},
					NavigateView = new EqualizerView ()
				},
				new MenuImageElement{ Text = "Settings", Svg = "SVG/settings.svg" },
			}
		};

		public AppDelegate (IntPtr handle) : base (handle)
		{

		}

		NSView currentView;

		public override void DidFinishLaunching (NSNotification notification)
		{
			AudioOutputHelper.Init();
			var version = Device.SystemVersion;

			//window.StyleMask |= NSWindowStyle.FullSizeContentView;
			//window.TitlebarAppearsTransparent = true;
			SetupApp ();
			// Insert code here to initialize your application
			_sidebarOutlineView.SizeLastColumnToFit ();
			_sidebarOutlineView.WeakDataSource = this;
			_sidebarOutlineView.ReloadData ();
			_sidebarOutlineView.FloatsGroupRows = false;

			_sidebarOutlineView.RowSizeStyle = NSTableViewRowSizeStyle.Default;

			// Expand all the root items; disable the expansion animation that normally happens
			NSAnimationContext.BeginGrouping ();
			NSAnimationContext.CurrentContext.Duration = 0;
			_sidebarOutlineView.ExpandItem (null, true);
			NSAnimationContext.EndGrouping ();
			CheckLogin ();

			#pragma warning disable 4014
			App.Start ();
			#pragma warning restore 4014

			ApiManager.Shared.StartSync ();
			fullScreenController = new VideoPlaybackWindowController ();
			NotificationManager.Shared.ToggleFullScreenVideo += NotificationManager_Shared_ToggleFullScreenVideo;
		}

		void NotificationManager_Shared_ToggleFullScreenVideo (object sender, EventArgs e)
		{
			fullScreenController.Toggle ();
		}

		void SetupApp ()
		{
			App.AlertFunction = (title,message) =>{
				//TODO: replace this
				Console.WriteLine($"ALERT {title} - {message}");
			};
			ApiManager.Shared.Load ();
			//App.AlertFunction = (t, m) => { new UIAlertView(t, m, null, "Ok").Show(); };
			App.Invoker = this.BeginInvokeOnMainThread;
			App.OnPlaying = () => {
				
			};
			App.OnStopped = () => {
				
			};

//			App.OnShowSpinner = (title) => { BTProgressHUD.ShowContinuousProgress(title, ProgressHUD.MaskType.Clear); };
//
//			App.OnDismissSpinner = BTProgressHUD.Dismiss;

		}

		public override bool ApplicationShouldHandleReopen (NSApplication sender, bool hasVisibleWindows)
		{
			if (hasVisibleWindows)
				window.OrderFront (this);
			else
				window.MakeKeyAndOrderFront (this);
			return true;
		}

		async void CheckLogin ()
		{
			if (ApiManager.Shared.Count > 0)
				return;
			try {
				var api = ApiManager.Shared.CreateApi (MusicPlayer.Api.ServiceType.Google);
				//api.ResetData ();
				var account = await api.Authenticate ();
				if (account == null)
					return;
				ApiManager.Shared.AddApi (api);
				var manager = ApiManager.Shared.GetMusicProvider (api.Identifier);
				using (new Spinner ("Syncing Database")) {
					await manager.SyncDatabase ();
				}
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		public override void WillTerminate (NSNotification notification)
		{
			// Insert code here to tear down your application
		}

		public void MenuWillHighlightItem (NSMenu menu, NSMenuItem item)
		{
			
		}

		void SetCurrentView (Element element)
		{
			var menuElement = element as MenuElement;
			if (menuElement == null || menuElement.NavigateView == null)
				return;

			var life = currentView as ILifeCycleView;
			life?.ViewWillDissapear ();
			currentView?.RemoveFromSuperview ();

			var view = menuElement.NavigateView;
			view.Frame = _mainContentView.Bounds;
			life = view as ILifeCycleView;
			life?.ViewWillAppear ();
			currentView = view;
			view.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
			_mainContentView.AddSubview (view);
		}


		partial void sidebarMenuDidChange (NSObject notification)
		{
			if (_sidebarOutlineView.SelectedRow < 0)
				return;
			var item = _sidebarOutlineView.ItemAtRow (_sidebarOutlineView.SelectedRow);
			var parent = _sidebarOutlineView.GetParent (item);
			if (parent == null)
				return;
			//SetCurrentViewToName (item.ToString ());
		}


		[Export ("outlineViewSelectionDidChange:")]
		public void SelectionDidChange (Foundation.NSNotification notification)
		{
			if (_sidebarOutlineView.SelectedRow < 0)
				return;
			var item = _sidebarOutlineView.ItemAtRow (_sidebarOutlineView.SelectedRow) as Element;
			item?.Tapped?.Invoke ();
			SetCurrentView (item);
			if (item?.ShouldDeselect == true)
				_sidebarOutlineView.DeselectAll (this);
		}


		List<Element> childrenForItem (Element item)
		{
			if (item == null)
				return Elements;
			var section = item as MenuSection;
			return section.Children;
		}

		[Export ("outlineView:child:ofItem:")]
		public Foundation.NSObject GetChild (AppKit.NSOutlineView outlineView, System.nint childIndex, Foundation.NSObject item)
		{
			var child = childrenForItem (item as Element) [(int)childIndex];
			return child;
		}

		[Export ("outlineView:isItemExpandable:")]
		public bool ItemExpandable (AppKit.NSOutlineView outlineView, Foundation.NSObject item)
		{
			var section = item as MenuSection;
			return section != null && section.IsExpandable;

		}


		[Export ("outlineView:numberOfChildrenOfItem:")]
		public System.nint GetChildrenCount (AppKit.NSOutlineView outlineView, Foundation.NSObject item)
		{
			var children = childrenForItem (item as Element);
			Console.WriteLine (children.Count);
			return children.Count;
		}

		[Export ("outlineView:isGroupItem:")]
		public bool IsGroupItem (AppKit.NSOutlineView outlineView, Foundation.NSObject item)
		{
			return item is MenuSection;
		}

		[Export ("outlineView:shouldShowOutlineCellForItem:")]
		public bool ShouldShowOutlineCell (AppKit.NSOutlineView outlineView, Foundation.NSObject item)
		{
			var element = item as Element;
			return element != null && element.ShouldOutline;
		}

		[Export ("outlineView:objectValueForTableColumn:byItem:")]
		public Foundation.NSObject GetObjectValue (AppKit.NSOutlineView outlineView, AppKit.NSTableColumn tableColumn, Foundation.NSObject item)
		{
			return item;
		}

		[Export ("splitView:canCollapseSubview:")]
		public bool CanCollapse (AppKit.NSSplitView splitView, AppKit.NSView subview)
		{
			return false;
		}

		[Export ("outlineView:heightOfRowByItem:")]
		public System.nfloat GetRowHeight (AppKit.NSOutlineView outlineView, Foundation.NSObject item)
		{
			return item is EqualizerMenuElement ? 44 : 26;
		}

		[Export ("outlineView:viewForTableColumn:item:")]
		public AppKit.NSView GetView (AppKit.NSOutlineView outlineView, AppKit.NSTableColumn tableColumn, Foundation.NSObject item)
		{
			var element = item as Element;
			return element.GetView (outlineView, this);
		}

		[Export ("splitView:constrainSplitPosition:ofSubviewAt:")]
		public System.nfloat ConstrainSplitPosition (AppKit.NSSplitView splitView, System.nfloat proposedPosition, System.nint subviewDividerIndex)
		{
			return 250;
			//return NMath.Max (proposedPosition, 150);
		}
		[Export ("splitView:resizeSubviewsWithOldSize:")]
		public void Resize (AppKit.NSSplitView splitView, CoreGraphics.CGSize oldSize)
		{
			var dividerThickness = splitView.DividerThickness;
			var leftRect = splitView.Subviews [0].Frame;
			var rightRect = splitView.Subviews [1].Frame;
			var newFrame = splitView.Frame;

			leftRect.Height = newFrame.Height;
			leftRect.X = leftRect.Y = 0;
			rightRect.Width = newFrame.Width - leftRect.Width - dividerThickness;
			rightRect.Height = newFrame.Height;
			rightRect.X = leftRect.Width + dividerThickness;

			splitView.Subviews [0].Frame = leftRect;
			splitView.Subviews [1].Frame = rightRect;
		}
	}

}
