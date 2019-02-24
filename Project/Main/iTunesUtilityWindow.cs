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
	public partial class iTunesUtilityWindow : Form {

		TrackInfo[] m_TrackInfo;
		ListViewItem[] _item;
		ListView.ListViewItemCollection listViewItemCollection;
		ListView.SelectedIndexCollection listViewItemSelection;

		Config m_config;
		Color m_CtrlColor;

		public static string m_iTunesLibraryPath {
			get {
				return $"{Helper.m_appPath}\\iTunes Library.json";
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public iTunesUtilityWindow() {
			InitializeComponent();
			iTunesHelper.ActiveEvent += iTunesActiveEvent;
			m_config = new Config();
		}


		void WriteMusicLibraryJson() {
			Helper.WriteJson( m_TrackInfo, m_iTunesLibraryPath );
		}


		/// <summary>
		/// iTuneから情報を取得します
		/// </summary>
		void ImportMusicLibrary() {
			
			ShowStatus( true );

			TraverseLibrary();

			ShowStatus( false );

			WriteMusicLibraryJson();

			ApplyTrackInfoToListView();
		}


		/// <summary>
		/// ライブラリからトラック情報を取得する
		/// </summary>
		void TraverseLibrary() {
			var mainLibrary = iTunesHelper.GetApp().LibraryPlaylist;
			var tracks = mainLibrary.Tracks;
			//var numTracks = tracks.Count;
			int i = 0;
			int m = tracks.Count;

			m_TrackInfo = new TrackInfo[ tracks.Count ];

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			float f = 100.0f / (float) (tracks.Count);
			float ff = 0.0f;
			//*
			Parallel.For( 0, tracks.Count, Helper.m_parallelOptions, cnt => {
				try {
					if( cnt == 0 ) return;
						int cc = cnt;
					m_TrackInfo[ cc ] = new TrackInfo( cnt, tracks[ cc ] );
					this.Invoke( new Action( () => {
						//i++;
						ff += f;
						if( 100.0f < ff ) {
							ff = 100.0f;
						}
						toolStripProgressBar1.Value = (int) ff;
					} ) );
				}
				catch( Exception ) {
				}
			} );
			/**/

			sw.Stop();
			TimeSpan ts = sw.Elapsed;
			Debug.Log( ts.ToString() );

			
		}


		/// <summary>
		/// m_TrackInfo配列をリストビューに反映させる
		/// </summary>
		void ApplyTrackInfoToListView() {
			Invoke( new Action( () => {
				Log.Info( $"トラック数: {m_TrackInfo.Length}" );

				m_listView1.VirtualListSize = m_TrackInfo.Length;
				_item = m_TrackInfo.Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();

				// ModifyFlagが設定された要素のみのインデックス配列を作成
				var idd = m_TrackInfo.Select( ( x, i ) => {
					if( x.ModifyFlag == 0 ) return -1;
					return i;
				} ).ToList();
				idd.RemoveAll( x => x == -1 );

				// ModifyFlagなトラックの背景色を変更
				foreach( var i in idd ) {
					_item[ i ].BackColor = Color.LightPink; ;
				}

				// 各列の幅を広げる
				for( int i = 1; i < m_listView1.Columns.Count; i++ ) {
					var c = m_listView1.Columns[ i ];
					( (ColumnHeader) c ).Width = -1;
				}
			} ) );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="b"></param>
		void ShowStatus( bool b ) {
			Invoke( new Action( () => {
				toolStripStatusLabel1.Visible = b;
				toolStripProgressBar1.Visible = b;
			} ) );
		}


		/// <summary>
		/// 引数が示すindexのアイテムを返すと描画される
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void listView1_RetrieveVirtualItem( object sender, RetrieveVirtualItemEventArgs e ) {
			if( _item == null ) return;
			e.Item = _item[ e.ItemIndex ];
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Form1_Load( object sender, EventArgs e ) {
			Font = SystemFonts.IconTitleFont;

			m_listView1.SetDoubleBuffered( true );
			listViewItemCollection = new ListView.ListViewItemCollection( m_listView1 );
			listViewItemSelection = new ListView.SelectedIndexCollection( m_listView1 );
			m_listView1.RetrieveVirtualItem += new RetrieveVirtualItemEventHandler( listView1_RetrieveVirtualItem );
			ShowStatus( false );
			m_CtrlColor = ToolStripMenuItem1.BackColor;

			var b = Helper.ReadJson<TrackInfo[]>( ref m_TrackInfo, m_iTunesLibraryPath );

			if( b ) {
				ApplyTrackInfoToListView();
			}
			else {
				iTunesHelper.Attach();
				Task.Run( () => ImportMusicLibrary() );
			}

			Helper.ReadJson( ref m_config, Helper.m_configPath );
			m_config.RollbackWindow( this );

			iTunesActiveEvent();
			
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Form1_FormClosing( object sender, FormClosingEventArgs e ) {
			iTunesHelper.Dettach();

			m_config.BackupWindow(this );
			Helper.WriteJson( m_config, Helper.m_configPath );
		}


		private async void ToolStripMenuItem_Click( object sender, EventArgs e ) {
			ToolStripMenuItem.Enabled = false;

			await Task.Run( () => ImportMusicLibrary() );

			// ボタン有効化
			ToolStripMenuItem.Enabled = true;
		}

		private void splitContainer1_SizeChanged( object sender, EventArgs e ) {
			splitContainer1.SplitterDistance = 0;
		}

		int selectIndex {
			get {
				if( listViewItemSelection.Count > 0 ) {
					foreach( int index in listViewItemSelection ) {
						return index;
					}
				}
				return 0;
			}
		}

		void SelectProcess( Action<int,int> action ) {
			if( listViewItemSelection.Count > 0 ) {
				foreach( int index in listViewItemSelection ) {
					action( index, _item[ index ].SubItems[ 0 ].Text.toInt32() );
				}
			}
		}

		//private void 表示ToolStripMenuItem_Click( object sender, EventArgs e ) {
		//	if( listViewItemSelection.Count > 0 ) {
		//		var mainLibrary = iTunesHelper.GetApp().LibraryPlaylist;
		//		var tracks = mainLibrary.Tracks;

		//		Debug.Log( "---- ListViewItem Selection ----" );
		//		foreach( int index in listViewItemSelection ) {
		//			//Debug.Log( $"index = {index.ToString()}" );

		//			Debug.Log( tracks[ index ].Name );
		//		}
		//	}
		//}

		//private void m_listView1_SelectedIndexChanged( object sender, EventArgs e ) {
		//	if( listViewItemSelection.Count > 0 ) {
		//		Debug.Log( "---- ListViewItem Selection ----" );
		//		foreach( Int32 index in listViewItemSelection ) {
		//			Debug.Log( $"index = {index.ToString()}" );
		//		}
		//	}
		//}


		void iTunesActiveEvent() {
			Invoke( new Action(()=> {
				if( iTunesHelper.IsAlive() ) {
					ToolStripMenuItem1.Text = "iTunes 接続中";
					ToolStripMenuItem.Enabled = true;
					ToolStripMenuItem1.BackColor = Color.LightGreen;
				}
				else {
					ToolStripMenuItem1.Text = "iTunes 接続する";
					ToolStripMenuItem.Enabled = false;
					ToolStripMenuItem1.BackColor = m_CtrlColor;
				}
			} ) );
		}

		private void ToolStripMenuItem1_Click( object sender, EventArgs e ) {
			if( iTunesHelper.IsAlive() ) {
				iTunesHelper.Dettach();
			}
			else {
				iTunesHelper.Attach();
			}
		}

		private void 色替えToolStripMenuItem_Click( object sender, EventArgs e ) {
			_item[ selectIndex ].SubItems[1].BackColor = Color.Pink;
		}

		private void m_listView1_ColumnClick( object sender, ColumnClickEventArgs e ) {
			int clm = (int) e.Column;
			Array.Sort( _item , (x,y)=> {
				if( 13 == clm ) {
					if( x.SubItems[ clm ].Text.toInt32() == y.SubItems[ clm ].Text.toInt32() ) return 0;
					if( x.SubItems[ clm ].Text.toInt32() < y.SubItems[ clm ].Text.toInt32() ) return 1;
					return -1;
				}
				return string.Compare( x.SubItems[ clm ].Text, y.SubItems[ clm ].Text );
			} );
			m_listView1.Refresh();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_listView1_DoubleClick( object sender, EventArgs e ) {
			iTunesHelper.Attach();
			//var t = iTunesHelper.Track( _item[ selectIndex ].ID() );
			//var tracks = iTunesHelper.mainLibrary.Tracks;
			var t = iTunesHelper.tracks[ _item[ selectIndex ].ID() ];
			t.Play();
			Marshal.ReleaseComObject( t );
			t = null;
			//Marshal.ReleaseComObject( tracks );
			//tracks = null;
		}


		private void デッドリンクを検出する( object sender = null, EventArgs e = null ) {
			ListViewItem[] _item2 = m_TrackInfo.Where( x => !File.Exists( x.Location ) ).Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
			if( _item2.Length == 0 ) {
				Debug.Log( "デッドリンクはありません" );
			}
			_item = _item2;
			m_listView1.VirtualListSize = _item.Length;
		}

		private void 未設定のアートワークを検出するToolStripMenuItem_Click( object sender, EventArgs e ) {
			_item = m_TrackInfo.Where( x => x.ArtworkNum == 0 ).Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
			m_listView1.VirtualListSize = _item.Length;
		}

		private void 未評価を検出するToolStripMenuItem_Click( object sender = null, EventArgs e = null ) {
			_item = m_TrackInfo.Where( x => 5 < x.AlbumRating || x.AlbumRating == 0 ).Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
			m_listView1.VirtualListSize = _item.Length;
		}

		private void 全て表示するToolStripMenuItem_Click( object sender, EventArgs e ) {
			_item = m_TrackInfo.Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
			m_listView1.VirtualListSize = _item.Length;
		}

		private void 表示ToolStripMenuItem_Click( object sender, EventArgs e ) {
			SelectProcess( ( i, j ) => {
				m_TrackInfo[ j ] = new TrackInfo( j, iTunesHelper.Track( j ) );
				_item[ i ] = new ListViewItem( m_TrackInfo[ j ].GetItemString() );
			} );
			m_listView1.Refresh();
			WriteMusicLibraryJson();
		}

		private void アルバムレーティングを1に設定するToolStripMenuItem_Click( object sender, EventArgs e ) {
			SelectProcess( ( i, j ) => {
				m_TrackInfo[ j ].AlbumRating = 1;
				m_TrackInfo[ j ].ModifyFlag |= TrackInfo.Modify.AlbumRating;
				_item[ i ] = new ListViewItem( m_TrackInfo[ j ].GetItemString() );
				_item[ i ].BackColor = Color.LightPink;
			} );
			m_listView1.Refresh();
			WriteMusicLibraryJson();
		}

		private void iTunesに反映するToolStripMenuItem_Click_1( object sender, EventArgs e ) {
			SelectProcess( ( i, j ) => {
				var t = m_TrackInfo[ j ];
				if( 0 != ( t.ModifyFlag & TrackInfo.Modify.AlbumRating ) ) {
					iTunesHelper.Track( j ).AlbumRating = t.AlbumRating;
				}
				m_TrackInfo[ j ].ModifyFlag = 0;
				_item[ i ] = new ListViewItem( m_TrackInfo[ j ].GetItemString() );
				_item[ i ].BackColor = Color.White;
			} );
			未評価を検出するToolStripMenuItem_Click();
		}

		private void ログToolStripMenuItem_Click( object sender, EventArgs e ) {
			LogWindow.Visible = true;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="ev"></param>
		private void iTunesからこの曲を削除する( object sender = null, EventArgs ev = null ) {
			List<int> lst = new List<int>();
			SelectProcess( ( i, j ) => {
				lst.Add( j );
			} );
			string aa = "";
			if( 30 < lst.Count ) {
				aa = $"{lst.Count}曲をiTunesから削除します\n\n{aa}\nよろしいですか？";
			}
			else {
				aa = string.Join( "\n", lst.Select( x => $"> {m_TrackInfo[ x ].Artist} - {m_TrackInfo[ x ].Name}" ).ToArray() );
				aa = $"以下をiTunesから削除します\n\n{aa}\nよろしいですか？";
			}
			var result = MessageBox.Show( aa, "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Question );
			if( result == DialogResult.OK ) {
				try {
					var dels = lst.Select( x => iTunesHelper.Track( x ) ).ToArray();
					//foreach( var d in dels ) {
					for( int i = dels.Length - 1; 0 <= i; i-- ) {
						var d = dels[ i ];
						Log.Info( $"{d.Name}: Delete" );
						d.Delete();
						
					}

					for( int i = lst.Count - 1; 0 <= i; i-- ) {
						ArrayUtility.RemoveAt( ref m_TrackInfo, lst[ i ] );
					}
					// 削除要素に合わせてインデックスを再設定
					for( int i = 0; i < m_TrackInfo.Length; i++ ) {
						m_TrackInfo[ i ].Index = i;
					}
					ApplyTrackInfoToListView();

					//デッドリンクを検出する();
				}
				catch( Exception e ) {
					Log.Exception( e );
				}
			}
			//Debug.Log( aa );
		}


		//private void EmbedArtworkButton_Click_1( object sender, EventArgs e ) {
		//	if( _tokenSource != null ) {
		//		_tokenSource.Cancel( true );
		//	}
		//}
	}
}
