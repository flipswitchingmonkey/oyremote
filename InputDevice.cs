using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OyRemote
{
    class InputDevice
    {
        public RadioButton radio;
        public string deviceID;
        public InputDevice(RadioButton radio, string deviceID)
        {
            this.radio = radio;
            this.deviceID = deviceID;
        }
    }
}
