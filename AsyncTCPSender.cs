using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using MBLL;
using System.Net.Sockets;

namespace MDAL
{
    public static class AsyncTCPSender
    {
        private static List<Byte[]> lbm;

        private static void Send(Socket handler, Byte[] data)
        {
            // Convert the string data to byte data using ASCII encoding.
            //byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(data, 0, data.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                //MessageBox.Show("Данные были успешно отправлены");

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
                //Console.WriteLine(e.ToString());

            }
        }

        private static readonly Byte[] Kanal5 = Encoding.ASCII.GetBytes("KANAL");
        private static readonly Byte[] vlaga = Encoding.ASCII.GetBytes("VLAGA");

        /// <summary>
        /// заполнить массив с отправляемой информацией
        /// </summary>
        /// <param name="sendedValue">отправляемое значение с одного датчика</param>
        private static void FillTheByteList(SendedToBSValue sendedValue)
        {
            Byte[] Kanal5num = new byte[1];
            Kanal5num[0] = Convert.ToByte(sendedValue.KanalNumber);

            Byte[] sensnum = new byte[1];
            sensnum[0] = Convert.ToByte(sendedValue.SensorInKanalNumber);

            Byte[] symbol = Encoding.ASCII.GetBytes(sendedValue.Sign.ToString());

            Byte[] sensValue = EncoderClass.Get3ByteFloatFromDouble(sendedValue.Value);

            //lbm = new List<byte[]>();
           

            for (int i = 0; i < 3; ++i )
            {
                lbm.Add(Kanal5);
                lbm.Add(Kanal5num);
                lbm.Add(vlaga);
                lbm.Add(sensnum);
                lbm.Add(symbol);
                lbm.Add(sensValue);
            }
        }

        /// <summary>
        /// отправка объединенной информации с разных датчиков
        /// </summary>
        /// <param name="stbvL">массив отправляемых значений</param>
        /// <param name="ip">ип получателя</param>
        /// <param name="port">порт получателя</param>
        public static void SendUnionData(List<SendedToBSValue> stbvL, string ip, string port)
        {
            //string inf0 = "";
            if(stbvL != null && stbvL.Count >0 )
            {
                lbm = new List<byte[]>();
                //for (int i = 0; i < 5; ++i )
                //{
                lbm.Clear();
                    foreach (SendedToBSValue value in stbvL)
                    {
                        //for (int i = 0; i < 3; ++i )
                        //{
                            FillTheByteList(value);
                            
                        //}
                        //MessageBox.Show(
                        //    string.Format("ip:{0} port:{1} inf0: {2}{3} {4} {5}", ip, port, value.KanalNumber,
                        //                  value.SensorInKanalNumber, value.Sign, value.Value));
                        //inf0 += string.Format("ip:{0} port:{1} inf0: {2}{3} {4} {5}\n", ip, port, value.KanalNumber,
                        //                  value.SensorInKanalNumber, value.Sign, value.Value);
                    }
                    ConnectAndSendInfo(ip, port);


                //}
                
            }
            //MessageBox.Show(inf0);
        }

        /// <summary>
        /// Соединение с получателем и отправка данных
        /// </summary>
        /// <param name="ip">ип получателя</param>
        /// <param name="port">порт получателя</param>
        private static void ConnectAndSendInfo(string ip, string port)
        {
            Socket hdlr = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress localAddress = IPAddress.Parse(ip);
            //IPEndPoint ipEndpoint = new IPEndPoint(localAddress, Convert.ToInt32(textBoxPort.Text));

            try
            {
                hdlr.Connect(localAddress, Convert.ToInt32(port));

                int bytelength = 0;

                foreach (Byte[] by in lbm)
                {
                    bytelength += by.Length;
                }

                Byte[] rez = new byte[bytelength];

                int insertPos = 0;
                //for (int i = 0; i < 3; ++i)
                //{
                    foreach (Byte[] by in lbm)
                    {
                        by.CopyTo(rez, insertPos);
                        insertPos += by.Length;
                    }
                //}

                Send(hdlr, rez);
                //MessageBox.Show("Данные успешно переданы : IP-" + ip + " Port-" + port + " Значение-" +
                //                sendedValue.Value + " KANAL-" + sendedValue.KanalNumber + " Sensor-" +
                //                sendedValue.SensorInKanalNumber + " знак" + "-" + sendedValue.Sign.ToString() + "-");
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// отправка значения с одного датчика
        /// </summary>
        /// <param name="sendedValue">отправляемое значение</param>
        /// <param name="ip">ип получателя</param>
        /// <param name="port">порт получателя</param>
        public static void SendData(SendedToBSValue sendedValue, string ip, string port)
        {
            lbm = new List<byte[]>();
            FillTheByteList(sendedValue);

            ConnectAndSendInfo(ip, port);
        }
    }
}
