using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Freiyer.Sandbox;
using CNTK;

namespace Reversi_Web
{
    class ReversiAI
    {
        private Reversi _Reversi;
        private ReversiPlayer _Player;
        private string _ModelUri;

        private DeviceDescriptor _Device;
        private Function _Eval;
        private Function _Train;

        public ReversiAI(Reversi reversi, ReversiPlayer player, string modelUri)
        {
            _Reversi = reversi;
            _Player = player;
            _ModelUri = modelUri;

            _Device = DeviceDescriptor.GPUDevice(0);
        }

        public void Eval()
        {

        }

        public void Learn()
        {

        }
    }
}
