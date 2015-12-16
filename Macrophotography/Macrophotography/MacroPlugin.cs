using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using CameraControl.Core.Interfaces;

namespace Macrophotography
{
    class MacroPlugin : IPanelPlugin
    {
        private UserControl _control;

        public UserControl Control
        {
            get { return _control ?? (_control = new MacroPluginControl()); }
            private set { _control = value; }
        }

        public bool Visible { get; set; }
        
        public string Id
        {
            get { return "{D000C3D7-595C-4632-BE60-2D39C8B9F99E}"; }
        }

        public MacroPlugin()
        {
            Visible = true;
        }
    }
}
