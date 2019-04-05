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

		int m_filterMode;


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
			iTunesHelper.Attach();

			ShowStatusbarControl( true, "インポート中" );

			TraverseLibrary();

			ShowStatusbarControl( false );

			WriteMusicLibraryJson();

			ApplyTrackInfoToListView();
		}


		/// <summary>
		/// ライブラリからトラック情報を取得する
		/// </summary>
		void TraverseLibrary() {
			//var mainLibrary = iTunesHelper.GetApp().LibraryPlaylist;
			//var tracks = mainLibrary.Tracks;
			var numTracks = iTunesHelper.Tracks.Count;
			//int i = 0;
			//int m = tracks.Count;

			m_TrackInfo = new TrackInfo[ numTracks ];

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			float f = 100.0f / (float) ( numTracks );
			float ff = 0.0f;
			//*
			Debug.Log( numTracks );
			int i = 0;
			foreach( IITTrack t in iTunesHelper.Tracks ) {

				Debug.Log( t.Name );
				m_TrackInfo[ i ] = new TrackInfo( i, t );
				this.Invoke( new Action( () => {
					//i++;
					ff += f;
					if( 100.0f < ff ) {
						ff = 100.0f;
					}
					toolStripProgressBar1.Value = (int) ff;
				} ) );

				Marshal.ReleaseComObject( t );
				i++;
			}
			//Parallel.For( 0, numTracks, Helper.m_parallelOptions, cnt => {
			//try {
			//		//if( cnt == 0 ) return;
			//		int cc = cnt;
			//		var t = iTunesHelper.Tracks[ cc ];
			//		m_TrackInfo[ cc ] = new TrackInfo( cnt, t );
			//		Debug.Log( cnt );
			//		this.Invoke( new Action( () => {
			//			//i++;
			//			ff += f;
			//			if( 100.0f < ff ) {
			//				ff = 100.0f;
			//			}
			//			toolStripProgressBar1.Value = (int) ff;
			//		} ) );
			//		Marshal.ReleaseComObject( t );
			//	}
			//	catch( Exception ) {
			//	}
			//} );
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

				if( m_filterMode == 1 ) {
					// デッドリンクを検出する
					デッドリンクを検出する();
				}
				else if( m_filterMode == 2 ) {
					未設定のアートワークを検出する();
				}
				else if( m_filterMode == 3 ) {
					不要なアルバムレーティングを検出する();
				}
				else {
					_item = m_TrackInfo.Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
					//ArrayUtility.RemoveAt(ref _item , 0);
					m_listView1.VirtualListSize = _item.Length;
				}

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
		void ShowStatusbarControl( bool b, string text = null ) {
			Invoke( new Action( () => {
				toolStripStatusLabel1.Visible = b;
				toolStripProgressBar1.Visible = b;
				if( !string.IsNullOrEmpty( text ) ) toolStripStatusLabel1.Text = text;
			} ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Form1_Load( object a1, EventArgs a2 ) {
			Font = SystemFonts.IconTitleFont;

			m_listView1.SetDoubleBuffered( true );
			listViewItemCollection = new ListView.ListViewItemCollection( m_listView1 );
			listViewItemSelection = new ListView.SelectedIndexCollection( m_listView1 );
			m_listView1.RetrieveVirtualItem += new RetrieveVirtualItemEventHandler( ( s, e ) => {
				if( _item == null ) return;
				e.Item = _item[ e.ItemIndex ];
			} );

			ShowStatusbarControl( false );
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

			SetCheckedFillter( 0 );

			Debug.Log( "Starts" );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Form1_FormClosing( object sender, FormClosingEventArgs e ) {
			iTunesHelper.Dettach();

			m_config.BackupWindow( this );
			Helper.WriteJson( m_config, Helper.m_configPath );
		}


		async void ToolStripMenuItem_Click( object sender, EventArgs e ) {
			ToolStripMenuItem.Enabled = false;

			await Task.Run( () => ImportMusicLibrary() );

			// ボタン有効化
			ToolStripMenuItem.Enabled = true;
		}

		private void splitContainer1_SizeChanged( object sender, EventArgs e ) {
			splitContainer1.SplitterDistance = 0;
		}

		//int selectIndex {
		//	get {
		//		if( listViewItemSelection.Count > 0 ) {
		//			foreach( int index in listViewItemSelection ) {
		//				return index;
		//			}
		//		}
		//		return 0;
		//	}
		//}


		/// <summary>
		/// 現在選択されている項目をm_TrackInfoでのインデックス番号として取得する
		/// </summary>
		/// <returns></returns>
		int[] MakeSelectIndexArray() {
			var lst = new List<int>();
			Invoke( new Action( () => {
				if( listViewItemSelection.Count > 0 ) {
					foreach( int index in listViewItemSelection ) {
						lst.Add( _item[ index ].SubItems[ 0 ].Text.toInt32() );
					}
				}
			} ) );
			return lst.ToArray();
		}


		void iTunesActiveEvent() {
			Invoke( new Action( () => {
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


		/// <summary>
		/// カラムクリック
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_listView1_ColumnClick( object sender, ColumnClickEventArgs e ) {
			int clm = (int) e.Column;

			Array.Sort( _item, ( x, y ) => {
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
			var idxs = MakeSelectIndexArray();
			if( idxs.Length == 0 ) return;
			//var t = iTunesHelper.Track( _item[ selectIndex ].ID() );
			//var tracks = iTunesHelper.mainLibrary.Tracks;
			var t = iTunesHelper.tracks[ idxs[ 0 ] + 1 ];
			t.Play();
			Marshal.ReleaseComObject( t );
			t = null;
			//Marshal.ReleaseComObject( tracks );
			//tracks = null;
		}


		private void ログToolStripMenuItem_Click( object sender, EventArgs e ) {
			LogWindow.Visible = true;
		}

		#region フィルター

		void SetCheckedFillter( int n ) {
			ToolStripMenuItem[] tbl = {
				全て表示するToolStripMenuItem,
				デッドリンクを検出するToolStripMenuItem,
				未設定のアートワークを検出するToolStripMenuItem,
				不要なアルバムレーティングを検出するToolStripMenuItem
			};
			for( int i = 0; i < tbl.Length; i++ ) {
				var t = tbl[ i ];
				if( i == n ) {
					t.Checked = true;
				}
				else {
					t.Checked = false;
				}
			}
			m_filterMode = n;
		}

		private void 全て表示する( object sender, EventArgs e ) {
			_item = m_TrackInfo.Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
			ArrayUtility.RemoveAt( ref _item, 0 );
			m_listView1.VirtualListSize = _item.Length;
			SetCheckedFillter( 0 );
		}

		private void デッドリンクを検出する( object sender = null, EventArgs e = null ) {
			ListViewItem[] _item2 = m_TrackInfo.Where( x => !File.Exists( x.Location ) ).Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
			if( _item2.Length == 0 ) {
				Debug.Log( "デッドリンクはありません" );
			}
			_item = _item2;
			ArrayUtility.RemoveAt( ref _item, 0 );
			m_listView1.VirtualListSize = _item.Length;
			SetCheckedFillter( 1 );
		}

		private void 未設定のアートワークを検出する( object sender = null, EventArgs e = null ) {
			_item = m_TrackInfo.Where( x => x.ArtworkNum == 0 ).Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
			ArrayUtility.RemoveAt( ref _item, 0 );
			m_listView1.VirtualListSize = _item.Length;
			SetCheckedFillter( 2 );
		}

		private void 不要なアルバムレーティングを検出する( object sender = null, EventArgs e = null ) {
			_item = m_TrackInfo.Where( x => 5 < x.AlbumRating || x.AlbumRating == 0 ).Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
			ArrayUtility.RemoveAt( ref _item, 0 );
			m_listView1.VirtualListSize = _item.Length;
			SetCheckedFillter( 3 );
		}

		#endregion



		#region コンテキストメニュー

		/// <summary>
		/// コマンド実行用の共通処理
		/// </summary>
		/// <param name="idxs"></param>
		/// <param name="text"></param>
		/// <param name="command"></param>
		/// <param name="complete"></param>
		/// <param name="parallel"></param>
		/// <param name="force"></param>
		async void Execute( int[] idxs, string text, Action<int> command, Action complete, bool parallel = false, bool force = false ) {
			string aa = "";
			if( force == false ) {
				if( 30 < idxs.Length ) {
					aa = $"{idxs.Length}曲を{text}\n\n{aa}\nよろしいですか？";
				}
				else {
					aa = string.Join( "\n", idxs.Select( x => $"> {m_TrackInfo[ x ].Artist} - {m_TrackInfo[ x ].Name}" ).ToArray() );
					aa = $"以下を{text}\n\n{aa}\nよろしいですか？";
				}
				System.Media.SystemSounds.Asterisk.Play();

				var result = MessageBox.Show( this, aa, "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Question );
				if( result == DialogResult.Cancel ) return;
			}

			await Task.Run( () => {
				var sw = new System.Diagnostics.Stopwatch();
				sw.Start();

				float f = 100.0f / (float) ( idxs.Length );
				float ff = 0.0f;
				Invoke( new Action( () => {
					ShowStatusbarControl( true, text );
					toolStripProgressBar1.Value = (int) 0;
				} ) );

				Action bar = () => {
					ff += f;
					if( 100.0f < ff ) ff = 100.0f;
					toolStripProgressBar1.Value = (int) ff;
				};
				if( parallel ) {
					Parallel.For( 0, idxs.Length, Helper.m_parallelOptions, i => {
						command?.Invoke( idxs[ i ] );
						Invoke( bar );
					} );
				}
				else {
					for( int i = idxs.Length - 1; 0 <= i; i-- ) {
						command?.Invoke( idxs[ i ] );
						Invoke( bar );
					}
				}
				Invoke( new Action( () => {
					ShowStatusbarControl( false );
				} ) );
				sw.Stop();
				Log.Info( $"{text}: {sw.Elapsed.ToString()}" );
				complete?.Invoke();
			} );
		}


		void 曲の情報をiTunesから読み込むToolStripMenuItem_Click( object sender, EventArgs e ) {
			var idxs = MakeSelectIndexArray();
			Execute( idxs, "曲の情報をiTunesから読み込む",
				( i ) => {
					try {
						var t = iTunesHelper.Tracks[ i ] as IITFileOrCDTrack;
						m_TrackInfo[ i ] = new TrackInfo( i, t );
						Marshal.ReleaseComObject( t );
					}
					catch( Exception ce ) {
						Log.Exception( ce );
					}
				},
				() => {
					WriteMusicLibraryJson();
					ApplyTrackInfoToListView();
				}, true, true
			);
		}


		void アルバムレーティングを1に設定するToolStripMenuItem_Click( object sender, EventArgs e ) {

			var idxs = MakeSelectIndexArray();
			Execute( idxs, "アルバムレーティングを1に設定します",
				( i ) => {
					try {
						var t = iTunesHelper.Tracks[ i ] as IITFileOrCDTrack;
						t.AlbumRating = 1;
						m_TrackInfo[ i ].AlbumRating = 1;
						Marshal.ReleaseComObject( t );
					}
					catch( Exception ce ) {
						Log.Exception( ce );
					}
				},
				() => {
					WriteMusicLibraryJson();
					ApplyTrackInfoToListView();
				}, true
			);
		}





		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="ev"></param>
		void iTunesからこの曲を削除する( object sender = null, EventArgs ev = null ) {
			var idxs = MakeSelectIndexArray();
			int[] idxs2 = new int[ 0 ];
			Execute( idxs, "iTunesから曲を削除します",
				( i ) => {
					for(; ; ) {
						try {
							var d = iTunesHelper.Tracks[ i ];
							Log.Info( $"{d.Name}: Delete" );
							d.Delete();
							Marshal.ReleaseComObject( d );

							ArrayUtility.RemoveAt( ref m_TrackInfo, i );
							//ArrayUtility.Add(ref idxs2 , i);
						}
						catch( System.Runtime.InteropServices.COMException ce ) {
							Log.Exception( ce );
							// 削除を繰り返すとiTunes側でコンパクション処理が走るのか
							// Tracksへの参照がダメになる
							// なのでDettach > AttachでTracksを取得し直す
							iTunesHelper.Dettach();
							iTunesHelper.Attach();
							continue;
						}
						catch( Exception e ) {
							Log.Exception( e );
						}
						break;
					}
				},
				() => {
					// 削除要素に合わせてインデックスを再設定
					for( int i = 0; i < m_TrackInfo.Length; i++ ) {
						m_TrackInfo[ i ].Index = i;
					}
					WriteMusicLibraryJson();
					ApplyTrackInfoToListView();
				}
			);
		}

		void ファイルを選択してアートワークを設定するToolStripMenuItem_Click( object sender, EventArgs e ) {
			var idxs = MakeSelectIndexArray();
			if( idxs.Length == 0 ) return;

			var ofd = new OpenFileDialog();
			ofd.InitialDirectory = m_TrackInfo[ idxs[ 0 ] ].Location.GetDirectory();
			ofd.FilterIndex = 1;
			ofd.Title = "アートワークに設定するファイルを選択してください";
			ofd.Filter = "Image Files (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png";
			ofd.RestoreDirectory = false;
			ofd.CheckFileExists = true;
			ofd.CheckPathExists = true;
			if( ofd.ShowDialog() == DialogResult.Cancel ) return;

			Execute( idxs, "曲の情報をiTunesから読み込む",
				( i ) => {
					try {
						var t = iTunesHelper.Tracks[ i ] as IITFileOrCDTrack;
						t.AddArtworkFromFile( ofd.FileName );
						Marshal.ReleaseComObject( t );
						m_TrackInfo[ i ].ArtworkNum++;
					}
					catch( Exception ce ) {
						Log.Exception( ce );
					}
				},
				() => {
					WriteMusicLibraryJson();
					ApplyTrackInfoToListView();
				}, true, true
			);
		}


		void cSVに書き出しToolStripMenuItem_Click( object sender, EventArgs e ) {
			//Debug.Log(123);

			//SaveFileDialogクラスのインスタンスを作成
			SaveFileDialog sfd = new SaveFileDialog();

			//はじめのファイル名を指定する
			//はじめに「ファイル名」で表示される文字列を指定する
			sfd.FileName = "新しいファイル.csv";
			//はじめに表示されるフォルダを指定する
			sfd.InitialDirectory = @"C:\";
			//[ファイルの種類]に表示される選択肢を指定する
			//指定しない（空の文字列）の時は、現在のディレクトリが表示される
			sfd.Filter = "CSVファイル(*.csv)|*.csv|すべてのファイル(*.*)|*.*";
			//[ファイルの種類]ではじめに選択されるものを指定する
			//2番目の「すべてのファイル」が選択されているようにする
			sfd.FilterIndex = 2;
			//タイトルを設定する
			sfd.Title = "保存先のファイルを選択してください";
			//ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
			sfd.RestoreDirectory = true;
			//既に存在するファイル名を指定したとき警告する
			//デフォルトでTrueなので指定する必要はない
			sfd.OverwritePrompt = true;
			//存在しないパスが指定されたとき警告を表示する
			//デフォルトでTrueなので指定する必要はない
			sfd.CheckPathExists = true;

			//ダイアログを表示する
			if( sfd.ShowDialog() == DialogResult.OK ) {
				//OKボタンがクリックされたとき、選択されたファイル名を表示する
				Console.WriteLine( sfd.FileName );

				using( var st = new System.IO.StreamWriter( sfd.FileName ) ) {
					//foreach(  ) {
					st.WriteLine( $"Artist\tAlbum\tName\tDiskCount\tDiskNumber\tTrackCount\tTrackNumber\tYear\tRating\tAlbumRating\tAlbumRatingKind\tratingKind\tSampleRate\tSize\tComment\tDuration\tPlayedCount\tDateAdded\t" );
					for( int i = 1; i < m_TrackInfo.Length; i++ ) {
						var p = m_TrackInfo[ i ];
						st.WriteLine( $"{p.Artist}\t{p.Album}\t{p.Name}\t{p.DiskCount}\t{p.DiskNumber}\t{p.TrackCount}\t{p.TrackNumber}\t{p.Year}\t{p.Rating}\t{p.AlbumRating}\t{p.AlbumRatingKind}\t{p.ratingKind}\t{p.SampleRate}\t{p.Size}\t{p.Comment}\t{p.Duration}\t{p.PlayedCount}\t{p.DateAdded}\t" );
					}
				}
			}
		}


		void AAA() {
			iTunesHelper.Attach();
			ShowStatusbarControl( true, "インポート中" );
			var tnow = DateTime.Now;

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			float f = 100.0f / (float) ( m_TrackInfo.Length );
			float ff = 0.0f;
			for( int i = 0; i < m_TrackInfo.Length; ) {
				var cur = m_TrackInfo[ i ];
				try {

					Win32.SetNowDateTime( cur.DateAdded );
					if( !File.Exists( cur.Location ) ) {
						Log.Error( $"Index: {i}: {cur.Name}: {cur.Album}: {cur.Artist}: {cur.Location}: ファイルが存在しない" );
						i++;
						continue;
					}

					var op = iTunesHelper.mainLibrary.AddFile( cur.Location );
					while( op.InProgress ) {
						Thread.Sleep( 100 );
						//Debug.Log( "op.InProgress" );
					}
					var tracks = op.Tracks;
					//var tt = op.Tracks[0] as IITFileOrCDTrack;
					foreach( IITFileOrCDTrack tt in tracks ) {

						tt.Rating = cur.Rating;
						tt.AlbumRating = cur.AlbumRating;
						//tt.AlbumRatingKind = cur.AlbumRatingKind;
						tt.Rating = cur.Rating;
						tt.PlayedCount = cur.PlayedCount;

						tt.Comment = cur.Comment;
						tt.Grouping = cur.Grouping;

						Marshal.ReleaseComObject( tt );
					}

					Marshal.ReleaseComObject( tracks );
					this.Invoke( new Action( () => {
						//i++;
						ff += f;
						if( 100.0f < ff ) {
							ff = 100.0f;
						}
						toolStripProgressBar1.Value = (int) ff;
					} ) );
				}
				catch( System.NullReferenceException ne ) {
				}
				catch( Exception e ) {
					Log.Error( $"Index: {i}: {cur.Name}: {cur.Album}: {cur.Artist}: {cur.Location}" );
					Log.Exception( e );
					Thread.Sleep( 1000 );
					continue;
				}
				i++;
			}
			Win32.SetNowDateTime( tnow );
			ShowStatusbarControl( false );

			sw.Stop();
			TimeSpan ts = sw.Elapsed;
			Debug.Log( ts.ToString() );
		}


		async void リストの曲をインポートToolStripMenuItem_Click( object sender, EventArgs e ) {
			//

			await Task.Run( () => AAA() );
		}


		void BBB( int[] idxs ) {
			iTunesHelper.Attach();
			ShowStatusbarControl( true, "インポート中" );
			var tnow = DateTime.Now;

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			float f = 100.0f / (float) ( m_TrackInfo.Length );
			float ff = 0.0f;
			for( int i = 0; i < idxs.Length; i++ ) {
				var cur = m_TrackInfo[ idxs[ i ] ];
				try {

					Win32.SetNowDateTime( cur.DateAdded );
					if( !File.Exists( cur.Location ) ) {
						Log.Error( $"Index: {i}: {cur.Name}: {cur.Album}: {cur.Artist}: {cur.Location}: ファイルが存在しない" );

						continue;
					}

					var op = iTunesHelper.mainLibrary.AddFile( cur.Location );
					while( op.InProgress ) {
						Thread.Sleep( 100 );
						//Debug.Log( "op.InProgress" );
					}
					var tracks = op.Tracks;
					//var tt = op.Tracks[0] as IITFileOrCDTrack;
					foreach( IITFileOrCDTrack tt in tracks ) {

						tt.Rating = cur.Rating;
						tt.AlbumRating = cur.AlbumRating;
						//tt.AlbumRatingKind = cur.AlbumRatingKind;
						tt.Rating = cur.Rating;
						tt.PlayedCount = cur.PlayedCount;

						tt.Comment = cur.Comment;
						tt.Grouping = cur.Grouping;

						Marshal.ReleaseComObject( tt );
					}

					Marshal.ReleaseComObject( tracks );
					this.Invoke( new Action( () => {
						//i++;
						ff += f;
						if( 100.0f < ff ) {
							ff = 100.0f;
						}
						toolStripProgressBar1.Value = (int) ff;
					} ) );
				}
				catch( System.NullReferenceException ne ) {
					Log.Error( $"Index: {i}: {cur.Name}: {cur.Album}: {cur.Artist}: {cur.Location}" );
					Log.Exception( ne );
					Thread.Sleep( 1000 );
				}
				catch( Exception e ) {
					Log.Error( $"Index: {i}: {cur.Name}: {cur.Album}: {cur.Artist}: {cur.Location}" );
					Log.Exception( e );
					Thread.Sleep( 1000 );
					continue;
				}
			}
			Win32.SetNowDateTime( tnow );
			ShowStatusbarControl( false );

			sw.Stop();
			TimeSpan ts = sw.Elapsed;
			Debug.Log( ts.ToString() );
		}

		async void 曲を追加するToolStripMenuItem_Click( object sender, EventArgs e ) {
			var idxs = MakeSelectIndexArray();
			if( idxs.Length == 0 ) return;

			await Task.Run( () => BBB( idxs ) );
		}




		async void Menu_ExportPlaylist( object sender, EventArgs e ) {
			await Task.Run( () => WritePlaylistAll() );
		}


		void WritePlaylistAll() {
			var lsp = iTunesHelper.GetApp().LibrarySource.Playlists;
			string outdate = DateTime.Now.ToString( "yyyyMMddHHmmss" );
			string outPath = outdate;
			var dirs = new List<string>();
			Directory.CreateDirectory( outPath );

			foreach( IITPlaylist p in lsp ) {
				try {
					if( p.Kind == ITPlaylistKind.ITPlaylistKindLibrary ) continue;

					IITUserPlaylist upl = (IITUserPlaylist) p;
					var parent = upl.get_Parent();

					try {
						if( upl.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindFolder ) {
							//Directory.Exists
							Debug.Log( "ITUserPlaylistSpecialKindFolder: " );

							if( parent == null ) {
								if( 1 <= dirs.Count ) {
									dirs.RemoveAt( dirs.Count - 1 );
								}
								dirs.Add( upl.Name );
							}
							else {
								if( dirs[ dirs.Count - 1 ] != parent.Name ) {
									dirs.RemoveAt( dirs.Count - 1 );
								}
								dirs.Add( upl.Name );
							}

							outPath = outdate + "/" + string.Join( "/", dirs );
							Directory.CreateDirectory( outPath );
							Debug.Log( outPath );
						}
						else if( upl.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone ) {
							void updatePath( bool force = false ) {
								if( force || dirs[ dirs.Count - 1 ] != parent.Name ) {
									dirs.RemoveAt( dirs.Count - 1 );
									outPath = outdate + "/" + string.Join( "/", dirs );
								}
							}
							if( parent == null ) {
								if( 1 <= dirs.Count ) {
									updatePath( true );
								}
							}
							else {
								updatePath();
							}

							Debug.Log( $"{upl.Name}: {upl.SpecialKind.ToString()}: {upl.Smart}" );
							var aa = upl.Name.Replace( "/", "／" );
							var path = outPath + "/" + aa;
							if( !upl.Smart ) {
								path += ".csv";
							}
							Debug.Log( path );

							var tr = p.Tracks;
							WritePlaylist( path, tr, upl.Smart );
							Marshal.ReleaseComObject( tr );
						}
					}
					finally {
						if( parent != null ) {
							Marshal.ReleaseComObject( parent );
						}
						Marshal.ReleaseComObject( upl );
					}
				}
				finally {

					Marshal.ReleaseComObject( p );
				}
			}
			Marshal.ReleaseComObject( lsp );
		}


		void WritePlaylist( string filepath, IITTrackCollection tracks, bool smartPlaylist ) {
			try {
				using( var st = new StreamWriter( filepath ) ) {
					if( smartPlaylist ) return;
					st.WriteLine( "場所" );

					foreach( IITFileOrCDTrack p in tracks ) {
						st.WriteLine( $"{p.Location}" );
						Marshal.ReleaseComObject( p );
					}
				}
			}
			catch( Exception e ) {
				Debug.Error( e );
			}
			finally {
				Marshal.ReleaseComObject( tracks );
			}
		}

		void プレイリストをインポートToolStripMenuItem_Click( object sender, EventArgs e ) {

		}
	}
	
	#endregion

}
