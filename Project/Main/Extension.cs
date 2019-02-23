using System.Windows.Forms;
using CsLib;

namespace iTunesUtility {
	public static class Extension {
		public static int ID( this ListViewItem item ) {
			return item.SubItems[ 0 ].Text.toInt32();
		}
	}
}
