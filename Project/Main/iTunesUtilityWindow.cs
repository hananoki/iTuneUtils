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

		Progressbar m_progressbar;

		public static string m_iTunesLibraryPath {
			get {
				return $"{Helper.m_appPath}\\iTunes Library.json";
			}
		}

		public static string s_iTunesLibraryPath2 {
			get {
				return $"{Helper.m_appPath}\\iTunes Library.csv";
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


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Form1_Load( object a1, EventArgs a2 ) {
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

			//var b = Helper.ReadJson<TrackInfo[]>( ref m_TrackInfo, m_iTunesLibraryPath );
			var b = ReadLibrary();

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

			if( string.IsNullOrEmpty( m_config.playlistFolder ) ) {
				m_config.playlistFolder = Directory.GetCurrentDirectory();
			}
			m_progressbar = new Progressbar( this, toolStripProgressBar1 );

			Win32.SHSTOCKICONINFO sii = new Win32.SHSTOCKICONINFO();
			sii.cbSize = Marshal.SizeOf( sii );

			Win32.SHGetStockIconInfo( Win32.SIID_SHIELD, Win32.SHGSI_ICON | Win32.SHGSI_SMALLICON, ref sii );
			if( sii.hIcon != IntPtr.Zero ) {
				Icon shieldIcon = Icon.FromHandle( sii.hIcon );
				MainMenuItem_ImportPlaylistToolStrip.Image = shieldIcon.ToBitmap();
				Context_RegisterLibrary.Image = shieldIcon.ToBitmap();
			}

			////Debug.Log( Helper.IsAdministrator().ToString() );
			if( !Helper.IsAdministrator() ) {
				MainMenuItem_ImportPlaylistToolStrip.Visible = false;
				Context_RegisterLibrary.Visible = false;
			}
			//Win32.TOKEN_ELEVATION_TYPE tet = Win32.GetTokenElevationType();
			//if( tet == Win32.TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault ) {
			//	Debug.Log( "UACが無効になっているか、標準ユーザーです" );
			//}
			//else if( tet == Win32.TOKEN_ELEVATION_TYPE.TokenElevationTypeFull ) {
			//	Debug.Log( "UACが有効になっており、昇格しています" );
			//}
			//else if( tet == Win32.TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited ) {
			//	Debug.Log( "UACが有効になっており、昇格していません" );
			//}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Form1_FormClosing( object sender, FormClosingEventArgs e ) {
			iTunesHelper.Dettach();

			m_config.BackupWindow( this );
			Helper.WriteJson( m_config, Helper.m_configPath );
		}


		void splitContainer1_SizeChanged( object sender, EventArgs e ) {
			try {
				splitContainer1.SplitterDistance = 0;
			}
			catch( Exception ex ) {
				Log.Exception( ex );
			}
		}



		void WriteMusicLibraryJson() {
			//Helper.WriteJson( m_TrackInfo, m_iTunesLibraryPath );
			using( var st = new StreamWriter( s_iTunesLibraryPath2 ) ) {
				st.WriteLine( $"Artist\tAlbum\tName\tTrackNumber\tTrackCount\tDiscNumber\tDiscCount\tYear\tGenre\tTime\tRating\tAlbumRating\tAlbumRatingKind\tratingKind\tGrouping\tComment\tPlayedCount\tDateAdded\tModificationDate\tPlayedDate\tLocation\tEnabled" );
				foreach( var p in m_TrackInfo ) {
					st.WriteLine( $"{p.Artist}\t{p.Album}\t{p.Name}\t{p.TrackNumber}\t{p.TrackCount}\t{p.DiscNumber}\t{p.DiscCount}\t{p.Year}\t{p.Genre}\t{p.Time}\t{p.Rating}\t{p.AlbumRating}\t{p.AlbumRatingKind}\t{p.ratingKind}\t{Base64.Encode( p.Grouping )}\t{Base64.Encode( p.Comment )}\t{p.PlayedCount}\t{p.DateAdded}\t{p.ModificationDate}\t{p.PlayedDate}\t{p.Location}\t{p.Enabled}" );
				}
			}
		}


		/// <summary>
		/// ライブラリ情報を読み込みます
		/// </summary>
		/// <returns>失敗時はfalse</returns>
		public bool ReadLibrary(  ) {
			if( !File.Exists( s_iTunesLibraryPath2 ) ) return false;
			var readtext = File.ReadAllText( s_iTunesLibraryPath2 ).Split( new string[] { "\r\n" }, StringSplitOptions.None ).Where( x => !string.IsNullOrEmpty( x ) ).ToList();
			readtext.RemoveAt( 0 );

			m_TrackInfo = new TrackInfo[ readtext.Count ];

			for( int i = 0; i < readtext.Count; i++ ) {
				var s = readtext[ i ];
				var ss = s.Split( '\t' );

				var t = new TrackInfo();
				t.Index = i;
				t.Artist = ss[ 0 ];
				t.Album = ss[ 1 ];
				t.Name = ss[ 2 ];
				t.TrackNumber = ss[ 3 ].toInt32();
				t.TrackCount = ss[ 4 ].toInt32();
				t.DiscNumber = ss[ 5 ].toInt32();
				t.DiscCount = ss[ 6 ].toInt32();
				t.Year = ss[ 7 ].toInt32();
				t.Genre = ss[ 8 ];
				t.Time = ss[ 9 ];
				t.Rating = ss[ 10 ].toInt32();
				t.AlbumRating = ss[ 11 ].toInt32();
				t.AlbumRatingKind = ss[ 12 ].toInt32();
				t.ratingKind = ss[ 13 ].toInt32();
				t.Grouping = Base64.Decode( ss[ 14 ] );
				t.Comment = Base64.Decode( ss[ 15 ] );
				t.PlayedCount = ss[ 16 ].toInt32();
				t.DateAdded = DateTime.Parse( ss[ 17 ] );
				t.ModificationDate = DateTime.Parse( ss[ 18 ] );
				t.PlayedDate = DateTime.Parse( ss[ 19 ] );
				t.Location =  ss[ 20 ] ;
				t.Enabled = bool.Parse( ss[ 21 ] );
				m_TrackInfo[ i ] = t;
			}

			return true;
		}


		#region iTunesからライブラリを取り込む

		/// <summary>
		/// iTunesからライブラリを取り込む
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		async void MainMenu_ImportLibrary( object sender, EventArgs e ) {
			//ToolStripMenuItem.Enabled = false;

			await Task.Run( () => ImportMusicLibrary() );

			// ボタン有効化
			//ToolStripMenuItem.Enabled = true;
		}

		/// <summary>
		/// iTuneから情報を取得します
		/// </summary>
		void ImportMusicLibrary() {
			iTunesHelper.Attach();

			ShowStatusbarControl( true, "ライブラリを取り込み中" );

			TraverseLibrary();

			ShowStatusbarControl( false );

			WriteMusicLibraryJson();

			ApplyTrackInfoToListView();
		}


		void reg( int index, IITTrack tt ) {
			m_TrackInfo[ index ] = new TrackInfo( index, tt );
			m_progressbar.Next();

			Marshal.ReleaseComObject( tt );
		}

		/// <summary>
		/// ライブラリからトラック情報を取得する
		/// </summary>
		void TraverseLibrary() {
			var numTracks = iTunesHelper.Tracks.Count;

			m_TrackInfo = new TrackInfo[ numTracks ];

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			m_progressbar.Begin( numTracks );

			int i = 0;
			foreach( IITTrack t in iTunesHelper.Tracks ) {
				// タスクに分割すると配列オーバーする
				// iTunes.11 のやつだと 979番目に空トラックが発生する等正確に取得できないことがある
				// コンパイラの不具合ようなきもするが、iTunesに多重アクセスするのがそもそもマズイかもしれない
				//Task.Run( () => reg( i, t ) );

				reg( i, t );
				i++;
				Marshal.ReleaseComObject( t );
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

		#endregion


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
					//MenuItem_Tools.Enabled = true;
					ToolStripMenuItem1.BackColor = Color.LightGreen;
				}
				else {
					ToolStripMenuItem1.Text = "iTunes 接続する";
					//MenuItem_Tools.Enabled = false;
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
		void m_listView1_ColumnClick( object sender, ColumnClickEventArgs e ) {
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
		/// ダブルクリック再生
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_listView1_DoubleClick( object sender, EventArgs e ) {
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


		/// <summary>
		/// ログ表示
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void ログToolStripMenuItem_Click( object sender, EventArgs e ) {
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

		void デッドリンクを検出する( object sender = null, EventArgs e = null ) {
			ListViewItem[] _item2 = m_TrackInfo.Where( x => !File.Exists( x.Location ) ).Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
			if( _item2.Length == 0 ) {
				Debug.Log( "デッドリンクはありません" );
			}
			_item = _item2;
			//ArrayUtility.RemoveAt( ref _item, 0 );
			m_listView1.VirtualListSize = _item.Length;
			SetCheckedFillter( 1 );
			Debug.Log( $"デッドリンク {_item.Length} 検出しました" );
		}

		void 未設定のアートワークを検出する( object sender = null, EventArgs e = null ) {
			_item = m_TrackInfo.Where( x => x.ArtworkNum == 0 ).Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
			//ArrayUtility.RemoveAt( ref _item, 0 );
			m_listView1.VirtualListSize = _item.Length;
			SetCheckedFillter( 2 );
		}

		void 不要なアルバムレーティングを検出する( object sender = null, EventArgs e = null ) {
			_item = m_TrackInfo.Where( x => 5 < x.AlbumRating || x.AlbumRating == 0 ).Select( x => new ListViewItem( x.GetItemString() ) ).ToArray();
			//ArrayUtility.RemoveAt( ref _item, 0 );
			m_listView1.VirtualListSize = _item.Length;
			SetCheckedFillter( 3 );
		}

		#endregion



		#region コンテキストメニュー

		/// <summary>
		/// コマンド実行用の共通処理
		/// </summary>
		/// <param name="idxs">操作を行う要素番号の配列</param>
		/// <param name="text"></param>
		/// <param name="command"></param>
		/// <param name="complete">動作完了時に呼び出すアクション</param>
		/// <param name="parallel">forの繰り返し処理をパラレルにするフラグ</param>
		/// <param name="force">確認不要で実行するの強制フラグ</param>
		async void Execute( int[] idxs, string text, Action<int> command, Action complete, bool parallel = false, bool force = false ) {
			string aa = "";

			// ダイアログ確認
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

		async void この曲をiTunesUtilsの内容で登録し直すToolStripMenuItem_Click( object sender, EventArgs e ) {
			var idxs = MakeSelectIndexArray();
			if( idxs.Length == 0 ) return;

			TrackInfo[] tt = new TrackInfo[ idxs.Length ];
			for( int i = 0; i < idxs.Length; i++ ) {
				tt[ i ] = m_TrackInfo[ idxs[ i ] ];
			}
			
			await Task.Run( () => ImportLabrary( tt ) );
		}

		//async void 曲を追加するToolStripMenuItem_Click( object sender, EventArgs e ) {
		//	
		//	

		//	await Task.Run( () => BBB( idxs ) );
		//}
		//void BBB( int[] idxs ) {
		//	iTunesHelper.Attach();
		//	ShowStatusbarControl( true, "インポート中" );
		//	var tnow = DateTime.Now;

		//	var sw = new System.Diagnostics.Stopwatch();
		//	sw.Start();

		//	float f = 100.0f / (float) ( m_TrackInfo.Length );
		//	float ff = 0.0f;
		//	for( int i = 0; i < idxs.Length; i++ ) {
		//		var cur = m_TrackInfo[ idxs[ i ] ];
		//		try {
		//			Win32.SetNowDateTime( cur.DateAdded );
		//			if( !File.Exists( cur.Location ) ) {
		//				Log.Error( $"Index: {i}: {cur.Name}: {cur.Album}: {cur.Artist}: {cur.Location}: ファイルが存在しない" );

		//				continue;
		//			}

		//			var op = iTunesHelper.mainLibrary.AddFile( cur.Location );
		//			while( op.InProgress ) {
		//				Thread.Sleep( 100 );
		//				//Debug.Log( "op.InProgress" );
		//			}
		//			var tracks = op.Tracks;
		//			//var tt = op.Tracks[0] as IITFileOrCDTrack;
		//			foreach( IITFileOrCDTrack tt in tracks ) {

		//				tt.Rating = cur.Rating;
		//				tt.AlbumRating = cur.AlbumRating;
		//				//tt.AlbumRatingKind = cur.AlbumRatingKind;
		//				tt.Rating = cur.Rating;
		//				tt.PlayedCount = cur.PlayedCount;

		//				tt.Comment = cur.Comment;
		//				tt.Grouping = cur.Grouping;

		//				Marshal.ReleaseComObject( tt );
		//			}

		//			Marshal.ReleaseComObject( tracks );
		//			this.Invoke( new Action( () => {
		//				//i++;
		//				ff += f;
		//				if( 100.0f < ff ) {
		//					ff = 100.0f;
		//				}
		//				toolStripProgressBar1.Value = (int) ff;
		//			} ) );
		//		}
		//		catch( System.NullReferenceException ne ) {
		//			Log.Error( $"Index: {i}: {cur.Name}: {cur.Album}: {cur.Artist}: {cur.Location}" );
		//			Log.Exception( ne );
		//			Thread.Sleep( 1000 );
		//		}
		//		catch( Exception e ) {
		//			Log.Error( $"Index: {i}: {cur.Name}: {cur.Album}: {cur.Artist}: {cur.Location}" );
		//			Log.Exception( e );
		//			Thread.Sleep( 1000 );
		//			continue;
		//		}
		//	}
		//	Win32.SetNowDateTime( tnow );
		//	ShowStatusbarControl( false );

		//	sw.Stop();
		//	TimeSpan ts = sw.Elapsed;
		//	Debug.Log( ts.ToString() );
		//}

		#endregion



		#region メニューコマンド iTunesUtilsの内容をiTunesに登録する

		/// <summary>
		/// iTunesUtilsの内容をiTunesに登録する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		async void MainMenu_RegisteriTunesLibrary( object sender, EventArgs e ) {
			await Task.Run( () => ImportLabrary( m_TrackInfo ) );
		}


		void ImportLabrary( TrackInfo[] tinfo ) {
			iTunesHelper.Attach();
			ShowStatusbarControl( true, "ライブラリ登録中" );
			var tnow = DateTime.Now;

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			m_progressbar.Begin( tinfo.Length );
			for( int i = 0; i < tinfo.Length; ) {
				var cur = tinfo[ i ];
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
					}
					var tracks = op.Tracks;
					//var tt = op.Tracks[0] as IITFileOrCDTrack;
					Debug.Log( $"DateAdded: {cur.Location}" );
					foreach( IITFileOrCDTrack tt in tracks ) {
						tt.Enabled = cur.Enabled;
						tt.Rating = cur.Rating;
						tt.AlbumRating = cur.AlbumRating;
						//tt.AlbumRatingKind = cur.AlbumRatingKind;
						tt.Rating = cur.Rating;
						tt.PlayedCount = cur.PlayedCount;
						tt.PlayedDate = cur.PlayedDate;
						//tt.ModificationDate = cur.ModificationDate;
						//tt.Comment = cur.Comment;
						tt.Grouping = cur.Grouping;

						Marshal.ReleaseComObject( tt );
					}

					Marshal.ReleaseComObject( tracks );
					m_progressbar.Next();
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

		#endregion


		#region メニューコマンド iTunesからプレイリストを書き出す

		/// <summary>
		/// iTunesからプレイリストを書き出す
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		async void MainMenu_ExportPlaylist( object sender, EventArgs e ) {
			await Task.Run( () => ExportPlaylist() );
		}


		/// <summary>
		/// 
		/// </summary>
		void ExportPlaylist() {
			ShowStatusbarControl( true, "プレイリストを書き出し中" );

			var lsp = iTunesHelper.GetApp().LibrarySource.Playlists;
			string outdate = DateTime.Now.ToString( "yyyyMMddHHmmss" );
			string outPath = outdate;
			var dirs = new List<string>();
			Directory.CreateDirectory( outPath );

			m_progressbar.Begin( lsp.Count );

			foreach( IITPlaylist p in lsp ) {
				try {
					// いらなそうなので排除
					if( p.Kind == ITPlaylistKind.ITPlaylistKindLibrary ) continue;

					IITUserPlaylist p2 = (IITUserPlaylist) p;
					var parent = p2.get_Parent();

					try {
						// フォルダの時
						if( p2.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindFolder ) {
							Debug.Log( "ITUserPlaylistSpecialKindFolder: " );

							if( parent == null ) {
								if( 1 <= dirs.Count ) {
									dirs.RemoveAt( dirs.Count - 1 );
								}
								dirs.Add( p2.Name );
							}
							else {
								if( dirs[ dirs.Count - 1 ] != parent.Name ) {
									dirs.RemoveAt( dirs.Count - 1 );
								}
								dirs.Add( p2.Name );
							}

							outPath = outdate + "/" + string.Join( "/", dirs );
							Directory.CreateDirectory( outPath );
							Debug.Log( outPath );
						}
						// 通常のプレイリストと思われる時
						else if( p2.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone ) {
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

							Debug.Log( $"{p2.Name}: {p2.SpecialKind.ToString()}: {p2.Smart}" );
							var aa = p2.Name.Replace( "/", "／" );
							var path = outPath + "/" + aa;
							if( !p2.Smart ) {
								path = path + ".csv";
							}
							Debug.Log( path );

							var tr = p.Tracks;
							WritePlaylistFile( path, tr, p2.Smart );
							Marshal.ReleaseComObject( tr );
						}
					}
					finally {
						if( parent != null ) {
							Marshal.ReleaseComObject( parent );
						}
						Marshal.ReleaseComObject( p2 );
					}
				}
				finally {
					m_progressbar.Next();
					Marshal.ReleaseComObject( p );
				}
			}
			Marshal.ReleaseComObject( lsp );

			ShowStatusbarControl( false );

			MessageBox.Show( $"{Directory.GetCurrentDirectory()}\\{outdate} に書き出しました。", "iTunesからプレイリストを書き出す" );
		}


		void WritePlaylistFile( string filepath, IITTrackCollection tracks, bool smartPlaylist ) {
			try {
				using( var st = new StreamWriter( filepath ) ) {
					//if( smartPlaylist ) return;
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

		#endregion


		#region メニューコマンド iTunesにプレイリストを登録する


		/// <summary>
		/// iTunesにプレイリストを登録する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		async void MainMenu_ImportPlaylist( object sender, EventArgs e ) {
			var fbd = new FolderBrowserDialog();
			fbd.SelectedPath = m_config.playlistFolder;
			fbd.ShowNewFolderButton = false;

			if( fbd.ShowDialog() == DialogResult.OK ) {
				m_config.playlistFolder = fbd.SelectedPath;
				await Task.Run( () => ImportPlaylist() );
			}
		}


		/// <summary>
		/// iTunesにプレイリストを登録する
		/// </summary>
		void ImportPlaylist() {

			ShowStatusbarControl( true, "プレイリストを登録中" );

			Directory.SetCurrentDirectory( m_config.playlistFolder.GetDirectory() );
			
			var files = Directory.EnumerateFiles( m_config.playlistFolder.GetBaseName(), "*", SearchOption.AllDirectories ).ToArray();

			m_progressbar.Begin( files.Length );

			var dics = new Dictionary<string, IITUserPlaylist>();
			foreach( var f in files ) {
				var ss = f.Split( '\\' ).ToList();
				ss.RemoveAt( 0 );

				string key = "";
				string playlistName = "";
				bool cancel = false;

				// ルート直下の場合
				if( ss.Count == 1 ) {
					(playlistName, cancel) = MakePlaylistName( ss[ 0 ] );

					var playlist = iTunesHelper.GetApp().CreatePlaylist( playlistName ) as IITUserPlaylist;
					if( !cancel ) {
						AddPlaylistToiTunes( f, playlist );
					}
					Marshal.ReleaseComObject( playlist );
				}
				// フォルダに格納されている場合
				else {
					(playlistName, cancel) = MakePlaylistName( ss[ ss.Count - 1 ] );
					// 末尾のファイル名要素を削除、残りをJoinしてKeyとする
					ss.RemoveAt( ss.Count - 1 );
					if( 1 <= ss.Count ) {
						key = string.Join( "_", ss.ToList() );
					}
					if( !dics.ContainsKey( key ) ) {
						var lastName = ss[ ss.Count - 1 ];
						ss.RemoveAt( ss.Count - 1 );
						if( ss.Count == 0 ) {
							dics.Add( key, iTunesHelper.GetApp().CreateFolder( lastName ) as IITUserPlaylist );
						}
						else {
							dics.Add( key, dics[ string.Join( "_", ss.ToList() ) ].CreateFolder( lastName ) as IITUserPlaylist );
						}
					}
					var playlist = dics[ key ].CreatePlaylist( playlistName ) as IITUserPlaylist;
					if( !cancel ) {
						AddPlaylistToiTunes( f, playlist );
					}
					Marshal.ReleaseComObject( playlist );
					m_progressbar.Next();
				}
			}

			foreach( var p in dics ) {
				Marshal.ReleaseComObject( p.Value );
			}

			ShowStatusbarControl( false );
		}


		/// <summary>
		/// ファイルパスからプレイリスト用の名前とスマートプレイリストのフラグを返す
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		( string playlistName, bool smart) MakePlaylistName( string path ) {
			bool cancel = false;
			(var bs, var ext) = ParsePath( path );
			if( string.IsNullOrEmpty( ext ) ) {
				bs = "#" + bs;
				cancel = true;
			}
			return (bs, cancel);
		}


		/// <summary>
		/// ファイルパスからベース名と拡張子名を返す
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		(string basename, string ext) ParsePath( string path ) {
			return (path.GetBaseName(), path.getExt() );
		}


		/// <summary>
		/// 書き出したプレイリストを読み込んでiTunes側に追加する
		/// </summary>
		/// <param name="playlistFilepath"></param>
		/// <param name="playlist"></param>
		void AddPlaylistToiTunes( string playlistFilepath, IITUserPlaylist playlist ) {
			var readtext = File.ReadAllText( playlistFilepath ).Split( '\r', '\n' ).Where( x => !string.IsNullOrEmpty( x ) ).ToList();
			readtext.RemoveAt( 0 );
			if( readtext.Count == 0 ) return;
			playlist.AddFiles( readtext.ToArray() );

			Marshal.ReleaseComObject( playlist );
		}


		#endregion

		
	}
}
