using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace iTunesUtility {
	public class Progressbar {
		float f;
		float ff;
		iTunesUtilityWindow m_iTunesUtilityWindow;
		ToolStripProgressBar m_toolStripProgressBar1;

		public Progressbar( iTunesUtilityWindow iTunesUtilityWindow, ToolStripProgressBar toolStripProgressBar1 ) {
			m_iTunesUtilityWindow = iTunesUtilityWindow;
			m_toolStripProgressBar1 = toolStripProgressBar1;
		}

		public void Begin( int length ) {
			f = 100.0f / (float) ( length );
			ff = 0.0f;
		}

		public void Next() {
			ff += f;
			if( 100.0f < ff ) {
				ff = 100.0f;
			}
			m_iTunesUtilityWindow.Invoke( new Action( () => {
				m_toolStripProgressBar1.Value = (int) ff;
			} ) );
		}
	}
}
