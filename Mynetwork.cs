﻿using System.Threading;
using System.Net;
using System.Net.Sockets;
using System;

namespace signaling
{
    public class Mynetwork
    {
        int MCASTPORT;                // Порт для UDP сокета
        string MYIPADDR;              // Наш адрес

        Socket _socket;
        bool IsThreadRunning = false; // Переменная управляющая потоком слушателя
        Thread _listener; // Переменная класса для создания отдельного потока для слушателя сети

        public delegate void ReceivedMessage(byte[] message); // Делегат события получения сообщения
        public event ReceivedMessage myReceivedMessage; // Нашt событие для делегата

        
        public Mynetwork() 
        {
            // Узнаем свой адрес в сети
            // Сетевой адаптер должен быть корректно настроен
            // dnslookup должен выдавать на имя компьютера его
            // ip адрес
            IPAddress[] _ipaddrs = Dns.GetHostEntry(_getName()).AddressList;
            foreach (IPAddress _oneaddr in _ipaddrs)
            {
                if (_oneaddr.AddressFamily == AddressFamily.InterNetwork)
                {
                    MYIPADDR = _oneaddr.ToString();
                }
            }
        }

        public void Listen()
        {
            // Создаем и запускам отдельный поток-слушатель сети
            _listener = new Thread(new ThreadStart(_receiveWorker));
            _listener.Start();
        }

        public void StopListen()
        {
            IsThreadRunning = false;
            if (_socket != null)_socket.Close();
        }

        public bool IsListen
        {
            get {
                return IsThreadRunning;
            }
        }

        public void SendMessage(byte[] message)
        {
            _multicastSend(MYIPADDR, MCASTPORT, message);
        }

        public string port
        {
            set {
                int localvlu = Convert.ToInt32(value);
                if (localvlu > 1024 && localvlu < 32768)
                {
                    MCASTPORT = localvlu;
                }
            }
            get { 
                return MCASTPORT.ToString (); 
            }
        }

        public string ipaddr
        {
            get
            {
                return MYIPADDR;
            }
        }

        /// <summary>
        /// Поток-слушатель сети
        /// Функция передается в ThreadStart
        /// </summary>
        private void _receiveWorker()
        {
             _receive(MYIPADDR, MCASTPORT);
        }
        
        /// <summary>
        /// Функция отправки сообщения
        /// </summary>
        /// <param name="mAddress"></param>
        /// <param name="port"></param>
        /// <param name="message"></param>
        private void _multicastSend(string address, int port, byte[] message)
        {
            if (message.Length > 255) throw new Exception ("Message is large to 255 bytes");
           
            try
            {
                IPAddress _groupAddress = IPAddress.Parse(address);
                int _groupPort = port;
                UdpClient sender = new UdpClient();
                IPEndPoint groupEP = new IPEndPoint(_groupAddress, _groupPort);
                sender.Send(message, message.Length, groupEP);
                sender.Close();
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString ());   
            }
        }

        /// <summary>
        /// Слушаем сеть и в случае приема сообщения возвращаем его
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        private void _receive(string address, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint _ipendp = new IPEndPoint(IPAddress.Any, port);
            _socket.Bind(_ipendp);
            IsThreadRunning = true;
            while (IsThreadRunning)
            {
                byte[] _receivebuf = new byte[255];
                try
                {
                    _socket.Receive(_receivebuf);
                    myReceivedMessage(_receivebuf);
                }
                catch (Exception) {
                    IsThreadRunning = false;
                }
            }
        }

        /// <summary>
        /// Получаем название машины
        /// </summary>
        /// <returns></returns>
        private string _getName()
        {
            return Environment.MachineName;
        }


    }
}
