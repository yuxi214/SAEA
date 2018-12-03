﻿/****************************************************************************
*Copyright (c) 2018 Microsoft All Rights Reserved.
*CLR版本： 4.0.30319.42000
*机器名称：WENLI-PC
*公司名称：Microsoft
*命名空间：SAEA.TcpP2P
*文件名： PeerSocket
*版本号： V3.3.3.5
*唯一标识：1f88c4b4-a22d-4e98-a403-16c5e911ef86
*当前的用户域：WENLI-PC
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2018/3/1 22:44:06
*描述：
*
*=====================================================================
*修改标记
*修改时间：2018/3/1 22:44:06
*修改人： yswenli
*版本号： V3.3.3.5
*描述：
*
*****************************************************************************/
using SAEA.Common;
using SAEA.Sockets.Handler;
using SAEA.Sockets.Interface;
using SAEA.TcpP2P.Net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAEA.TcpP2P
{
    /// <summary>
    /// 
    /// </summary>
    public class Peer
    {
        Sender _psSender;

        Receiver _p2pConnect;

        NatInfo _me;

        NatInfo _remote;

        public List<NatInfo> PeerList
        {
            get; private set;
        }

        public event Action<List<NatInfo>> OnPeerListResponse;

        public event Action<NatInfo> OnP2pSucess;

        public event Action<string> OnP2pFailed;

        public event Action<byte[]> OnMessage;

        public event OnDisconnectedHandler OnServerDisconnected;

        public event OnDisconnectedHandler OnP2pDisconnected;

        public bool IsConnected
        {
            get; set;
        }

        public void ConnectPeerServer(string ipPort)
        {
            var tuple = ipPort.GetIPPort();
            _psSender = new Sender(tuple.Item1, tuple.Item2);
            _psSender.OnPeerListResponse += _psSender_OnPeerListResponse;
            _psSender.OnReceiveP2PTask += _psSender_OnReceiveP2PTask;
            _psSender.OnDisconnected += _psSender_OnDisconnected;
            _psSender.ConnectServer();
        }

        private void _psSender_OnDisconnected(string ID, Exception ex)
        {
            OnServerDisconnected?.Invoke(ID, ex);
        }

        public void RequestP2p(string remoteIpPort)
        {
            _psSender.SendP2PRequest(remoteIpPort);
        }

        private void _psSender_OnPeerListResponse(List<NatInfo> obj)
        {
            PeerList = obj;
            _me = PeerList.Where(b => b.IsMe).First();
            OnPeerListResponse?.Invoke(PeerList);
        }

        private void _psSender_OnReceiveP2PTask(NatInfo obj)
        {
            _p2pConnect = new Receiver(false);
            _p2pConnect.OnP2PSucess += _p2pSender_OnP2PSucess;
            _p2pConnect.OnMessage += _p2pConnect_OnMessage;
            _p2pConnect.OnDisconnected += _p2pConnect_OnDisconnected;
            _p2pConnect.Start(_me.Port);

            TaskHelper.Start(() =>
            {
                int tryCount = 0;

                while (!IsConnected)
                {
                    tryCount++;
                    _p2pConnect.HolePunching(obj.ToString());
                    ThreadHelper.Sleep(300);
                    if (tryCount >= 30)
                    {
                        OnP2pFailed?.Invoke("重试超过30次");
                        break;
                    }
                }
            });

        }

        private void _p2pConnect_OnDisconnected(string ID, Exception ex)
        {
            OnP2pDisconnected?.Invoke(ID, ex);
        }

        private void _p2pConnect_OnMessage(IUserToken arg1, ISocketProtocal arg2)
        {
            if (arg2.Content != null) OnMessage?.Invoke(arg2.Content);
        }

        private void _p2pSender_OnMessage(Sockets.Interface.ISocketProtocal obj)
        {
            if (obj.Content != null) OnMessage?.Invoke(obj.Content);
        }

        private void _p2pSender_OnP2PSucess(NatInfo obj)
        {
            IsConnected = true;
            _remote = obj;
            OnP2pSucess?.Invoke(_remote);
        }

        public void SendMessage(byte[] bytes)
        {
            if (IsConnected)
                _p2pConnect.SendMessage(_remote.ToString(), bytes);
        }

        public void SendMessage(string msg)
        {
            if (IsConnected)
                _p2pConnect.SendMessage(_remote.ToString(), msg);
        }
    }
}
