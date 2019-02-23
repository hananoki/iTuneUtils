using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CsLib;

namespace iTunesUtility {
	
	
	static class Program {
		


		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main() {
			try {
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault( false );

				Helper._init();
				Application.Run( new iTunesUtilityWindow() );
			}
			finally {
				//Debug.Log( "Main()" );
			}
		}
	}
}
