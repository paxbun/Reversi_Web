using System;
using System.Collections.Generic;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;
using Freiyer.Sandbox;

//netsh http add urlacl url=http://*:5566/ user=freiy
//netsh http delete urlacl url=http://*:5566
namespace Reversi_Web
{
    public enum ReversiBehaviorMode
    {
        None = 0,
        PlayerPlayer = 1,
        PlayerAI = 2,
        AIAI = 3
    };
    
    public class ReversiBehavior : WebSocketBehavior
    {
        public static IList<Tuple<ReversiBehavior, ReversiBehavior, Reversi>> PlayingList { get; set; }
        public static IList<Tuple<ReversiBehavior, Reversi>> MatchingList { get; set; }
            = new List<Tuple<ReversiBehavior, Reversi>>();
        public static string Path { get; set; }

        private Reversi _Reversi;
        private ReversiPlayer _Player;
        private ReversiBehavior _Other = null;
        private ReversiBehaviorMode _Mode;
        private IDictionary<string, Action<string>> _ActionDict
            = new Dictionary<string, Action<string>>();

        private ReversiAI _AI0;
        private ReversiAI _AI1;
        private Thread _AI0Thread;
        private Thread _AI1Thread;

        private string _EtcBuffer(string str)
        {
            return "PRNT" + str;
        }

        private string _UpdateBuffer()
        {
            char[] buffer = new char[71];
            buffer[0] = 'U';
            buffer[1] = 'P';
            buffer[2] = 'D';
            buffer[3] = 'T';
            PieceState[,] pieceStates = new PieceState[8, 8];
            _Reversi.CopyState(pieceStates);
            for (var i = 0; i < 8; i++)
                for (var j = 0; j < 8; j++)
                {
                    if (pieceStates[i, j] == PieceState.PieceAvailable)
                    {
                        if (_Reversi.GetCurrentPlayer() == _Player)
                            buffer[4 + i * 8 + j] = (char)((int)pieceStates[i, j]);
                        else
                            buffer[4 + i * 8 + j] = (char)0;
                    }
                    else
                        buffer[4 + i * 8 + j] = (char)((int)pieceStates[i, j]);
                }
            if(_Reversi.GetCurrentPlayer() == _Player)
            {
                if (_Reversi.GetState() == ReversiState.NoAvailableSpaces)
                    buffer[4 + 64] = (char)10;
                else if (((byte)_Reversi.GetState() & (byte)ReversiState.InProgress) != 0)
                    buffer[4 + 64] = (char)_Reversi.GetCurrentPlayer();
                else
                    buffer[4 + 64] = (char)((int)_Reversi.GetState() + 1);
            }
            else
            {
                if (((byte)_Reversi.GetState() & (byte)ReversiState.InProgress) != 0)
                    buffer[4 + 64] = (char)_Reversi.GetCurrentPlayer();
                else
                    buffer[4 + 64] = (char)((int)_Reversi.GetState() + 1);
            }
            buffer[4 + 65] = (char)_Reversi.GetDarkCount();
            buffer[4 + 66] = (char)_Reversi.GetLightCount();
            return new string(buffer);
        }
        
        private void _Init(string argument)
        {
            _Mode = (ReversiBehaviorMode)argument[0];
            _Init();
        }

        private void _Init()
        {
            switch(_Mode)
            {
                case ReversiBehaviorMode.PlayerPlayer:
                    _InitP2P();
                    break;
                case ReversiBehaviorMode.PlayerAI:
                    _InitP2A();
                    break;
                case ReversiBehaviorMode.AIAI:
                    _InitA2A();
                    break;
            }
        }
        
        private void _InitP2P()
        {
            Send("INIT");
            lock (MatchingList)
            {
                if (MatchingList.Count != 0)
                {
                    var tuple = MatchingList[0];
                    MatchingList.RemoveAt(0);
                    _Reversi = tuple.Item2;
                    _Other = tuple.Item1;
                    _Other._Other = this;
                    if (new Random().NextDouble() >= 0.5)
                    {
                        _Player = ReversiPlayer.Dark;
                        _Other._Player = ReversiPlayer.Light;
                    }
                    else
                    {
                        _Player = ReversiPlayer.Light;
                        _Other._Player = ReversiPlayer.Dark;
                    }
                    _Other._Start();
                    _Start();
                }
                else
                {
                    _Reversi = new Reversi();
                    MatchingList.Add(new Tuple<ReversiBehavior, Reversi>(this, _Reversi));
                }
            }
        }

        private void _InitP2A()
        {
            Send("INIT");
            _Reversi = new Reversi();
            if (new Random().NextDouble() >= 0.5)
            {
                _Player = ReversiPlayer.Dark;
                _AI0 = new ReversiAI(_Reversi, ReversiPlayer.Light, Path);
                // TODO
            }
            else
            {
                _Player = ReversiPlayer.Light;
                _AI0 = new ReversiAI(_Reversi, ReversiPlayer.Dark, Path);
            }
            _Start();
        }

        private void _InitA2A()
        {
            Send("INIT");
            Send("OBSV");
            _Reversi = new Reversi();
            _Player = 0;
        }

        private void _Start()
        {
            Send(_UpdateBuffer());
            Send("STRT");
            if (_Player == ReversiPlayer.Dark)
                Send(_EtcBuffer("You are Dark."));
            else if (_Player == ReversiPlayer.Light)
                Send(_EtcBuffer("You are Light."));
        }

        private void _Skip(string argument)
        {
            switch (_Mode)
            {
                case ReversiBehaviorMode.PlayerPlayer:
                    _SkipP2P();
                    break;
                case ReversiBehaviorMode.PlayerAI:
                    _SkipP2A();
                    break;
            }
        }

        private void _SkipP2P()
        {
            if (_Player != 0)
            {
                bool result = _Reversi.SkipTurn();
                Send(_UpdateBuffer());
                _Other.Send(_Other._UpdateBuffer());
                if (result)
                {
                    Send(_EtcBuffer("Skipped the turn."));
                    _Other.Send(_Other._EtcBuffer("Skipped the turn."));
                }
                else
                    Send(_EtcBuffer("Cannot skip."));
            }
        }

        private void _SkipP2A()
        {
            if(_Player != 0)
            {
                bool result = _Reversi.SkipTurn();
                Send(_UpdateBuffer());
                if (result)
                {
                    Send(_EtcBuffer("Skipped the turn."));
                    // TODO
                }
                else
                    Send(_EtcBuffer("Cannot skip."));
            }
        }

        private void _Place(string argument)
        {
            int x = argument[0] - '0';
            int y = argument[1] - '0';
            switch (_Mode)
            {
                case ReversiBehaviorMode.PlayerPlayer:
                    _PlaceP2P(x, y);
                    break;
                case ReversiBehaviorMode.PlayerAI:
                    _PlaceP2A(x, y);
                    break;
            }
        }

        private void _PlaceP2P(int x, int y)
        {
            if (_Player != 0)
            {
                if (_Player == _Reversi.GetCurrentPlayer())
                {
                    bool result = _Reversi.PlacePiece(y, x);
                    Send(_UpdateBuffer());
                    _Other.Send(_Other._UpdateBuffer());
                    if ((_Reversi.GetState() & ReversiState.InProgress) != 0)
                    {
                        if (result)
                        {
                            Send(_EtcBuffer("Placed at (" + (char)(y + '1') + ", " + (char)(x + 'A') + ')'));
                            _Other.Send(_Other._EtcBuffer("Placed at (" + (char)(y + '1') + ", " + (char)(x + 'A') + ')'));
                        }
                        else
                            Send(_EtcBuffer("Cannot place."));
                    }
                    else
                    {
                        Send(_EtcBuffer(""));
                    }
                }
                else
                {
                    Send(_EtcBuffer("Waiting for opponent..."));
                }
            }
        }

        private void _PlaceP2A(int x, int y)
        {
            if(_Player != 0)
            {
                bool result = _Reversi.PlacePiece(y, x);
                Send(_UpdateBuffer());
                if ((_Reversi.GetState() & ReversiState.InProgress) != 0)
                {
                    if (result)
                    {
                        Send(_EtcBuffer(String.Format("Placed at ({0}, {1})", (char)(y + '1'), (char)(x + 'A'))));
                        // TODO
                    }
                    else
                        Send(_EtcBuffer("Cannot place."));
                }
                else
                {
                    Send(_EtcBuffer(""));
                }
            }
        }

        private void _Restart(string argument)
        {
            if(_Player != 0)
                _Other._Close();
        }
        
        private void _OnCloseP2P()
        {
            if (_Player != 0)
                _Other._Close();
            else
                lock (MatchingList)
                    MatchingList.RemoveAt(0);
        }
        
        private void _Close()
        {
            Send("CLSE");
        }

        protected override void OnOpen()
        {
            Console.WriteLine("[WS] OnOpen: " + ID);
            _ActionDict.Add("INIT", _Init);
            _ActionDict.Add("SKIP", _Skip);
            _ActionDict.Add("PLCE", _Place);
            _ActionDict.Add("RSTT", _Restart);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            string command = e.Data.Substring(0, 4);
            Console.WriteLine("[WS] OnMessage: " + ID + '/' + command);
            string argument = e.Data.Substring(4);
            if (!_ActionDict.TryGetValue(command, out Action<String> action))
                Send("ERRAError occured.");
            else
                action(argument);

        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("[WS] OnClose: " + ID);
            switch (_Mode)
            {
                case ReversiBehaviorMode.PlayerPlayer:
                    _OnCloseP2P();
                    break;
            }
        }
    }
}
