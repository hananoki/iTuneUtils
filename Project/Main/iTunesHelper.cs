using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using iTunesLib;
using System.Runtime.InteropServices;
using System.Threading;
using CsLib;

namespace iTunesUtility {
	public static class iTunesHelper {
		static iTunesApp iTunes;

		public static Action ActiveEvent;

		public static bool IsAlive() {
			return iTunes != null ? true : false;
		}

		public static iTunesApp GetApp() {
			Attach();
			return iTunes;
		}

		public static IITFileOrCDTrack Track( int i ) {
			var mainLibrary = iTunesHelper.GetApp().LibraryPlaylist;
			var tracks = mainLibrary.Tracks;
			return (IITFileOrCDTrack) tracks[ i ];
		}

		public static void Attach() {
			if( iTunes != null ) return;
			try {
				iTunes = new iTunesApp();
				iTunes.OnAboutToPromptUserToQuitEvent += new _IiTunesEvents_OnAboutToPromptUserToQuitEventEventHandler( OnAboutToPromptUserToQuitEvent );
				iTunes.OnPlayerPlayEvent += new _IiTunesEvents_OnPlayerPlayEventEventHandler( OnPlayerPlayEvent );
				Debug.Log( "iTunes 接続しました" );
			}
			catch( Exception e ) {
				Debug.Error( e );
			}
			ActiveEvent?.Invoke();
		}

		public static void Dettach() {
			if( iTunes == null ) return;

			iTunes.OnPlayerPlayEvent -= OnPlayerPlayEvent;
			iTunes.OnAboutToPromptUserToQuitEvent -= OnAboutToPromptUserToQuitEvent;

			Marshal.ReleaseComObject( iTunes );
			iTunes = null;
			ActiveEvent?.Invoke();
		}

		/// <summary>
		/// iTunes終了時に呼ばれる？メソッド
		/// </summary>
		public static void OnAboutToPromptUserToQuitEvent() {
			Dettach();
		}

		/// <summary>
		/// 再生イベント時に呼び出されるメソッド
		/// </summary>
		/// <param name="iTrack"></param>
		public static void OnPlayerPlayEvent( object iTrack ) {
			Debug.Log( "iTunes_OnPlayerPlayEvent" );

			//再生中のトラック情報を取得
			IITTrack track = (IITTrack) iTrack;

			//アートワークコレクションを取得
			IITArtworkCollection artwork = track.Artwork;
		}
	}
}
