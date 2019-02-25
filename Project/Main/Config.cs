using System.Windows.Forms;
using System.Drawing;

namespace iTunesUtility {
	public class Config {
		int width;
		int height;
		public int x;
		public int y;

		public int Width {
			get {
				if( width == 0 ) return 800;
				return width;
			}
			set { width = value; }
		}
		
		public int Height {
			get {
				if( height == 0 ) return 600;
				return height;
			}
			set {
				height = value;
			}
		}

		public void RollbackWindow( Control window ) {
			window.Location= new Point( x, y );
			window.Width = Width;
			window.Height = Height;
		}

		public void BackupWindow( Control window ) {
			x = window.Location.X;
			y = window.Location.Y;
			width = window.Width;
			height = window.Height;
		}
	}
}
