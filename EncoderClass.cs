using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace MBLL
{
    /// <summary>
    /// �����, ������� ���������� ������������ �������� � ��� � USB ������
    /// </summary>
    public static class EncoderClass
    {

        /// <summary>
        ///  ������������ ���� �� ���������
        /// </summary>
        /// <param name="FByte">������ ����</param>
        /// <param name="SByte">������ ����</param>
        /// <returns></returns>
        private static int GetEncodedBytes(byte FByte, byte SByte)
        { //
            byte[] b = new byte[2];
            // �������� �� ������� ������ � �������
            if (BitConverter.IsLittleEndian)
            {
                b[0] = FByte;
                b[1] = SByte;
            }
            else
            {
                b[1] = FByte;
                b[0] = SByte;
            }

            int result = -1;

            try
            {
                result = BitConverter.ToInt16(b, 0);
            }
            catch
            { }

            return result;
        }

        static byte ReverseBits(byte b)
        {
            return (byte)((b * 0x0202020202ul & 0x010884422010ul) % 1023);
        }

        /// <summary>
        /// ��������� ����������� � ������� ���������� ������� DS18B20
        /// </summary>
        /// <param name="mladwij">������� ����</param>
        /// <param name="starwij">������� ����</param>
        /// <returns>�����������</returns>
        private static double GetTemperDS18B20(byte mladwij, byte starwij)
        {
            ushort w = mladwij;            // ������ ������� ������� ����
            w |= (ushort)(starwij << 8);   // ������ � ������� � ����� ������� ����
            bool negFlag = ((w & 0x1000) != 0);     // ������� ������������� �����������
            if (negFlag)                            // �������� ������
                w = (ushort)(65536 - w);
            // �� 4 ������� ��� ��������� ������� �����
            double dt = (((w & 0x0008) != 0) ? 0.5 : 0) +
                        (((w & 0x0004) != 0) ? 0.25 : 0) +
                        (((w & 0x0002) != 0) ? 0.125 : 0) +
                        (((w & 0x0001) != 0) ? 0.0625 : 0);

            double temper = ((w >> 4)) + dt;
            if (negFlag)                            // ������ ����� ����
                temper = -temper;

            return temper;
        }

        /// <summary>
        /// ����������� ���������� �� ������ �� ����������
        /// </summary>
        /// <param name="byte1">����������������,�������� ����������</param>
        public static string EncodeInfo(Byte[] byte1)
        {
            string fckInfo = "";

            #region ����
            if (Protocol.Protocol1 == WhatProtocol.Znach)
            { // �������� � ������ ��������� � ��������
                int byteLength = byte1.Length;
                for (int u = 0; u < byte1.Length; ++u)
                {
                    //try
                    //{
                    // ����������� ��������� ����� KANAL
                    if (byte1[u] == Convert.ToByte('K'))
                    {
                        if (u < byteLength - 8)
                        {
                            if (byte1[u + 1] == Convert.ToByte('A'))
                            {
                                if (byte1[u + 2] == Convert.ToByte('N'))
                                {
                                    if (byte1[u + 3] == Convert.ToByte('A'))
                                    {
                                        if (byte1[u + 4] == Convert.ToByte('L'))
                                        {
                                            if (byte1[u + 6] == Convert.ToByte('O'))
                                            {
                                                if (byte1[u + 7] == Convert.ToByte('K'))
                                                {
                                                    // ������� ����� ������
                                                    int kanalNumber = GetEncodedBytes(byte1[u + 5], 0);
                                                    fckInfo += "K" + kanalNumber.ToString();
                                                    u += 7;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // ����������� ��������� ����� ZNACH
                    if (byte1[u] == Convert.ToByte('Z'))
                    {
                        if (u < byteLength - 9)
                        {
                            if (byte1[u + 1] == Convert.ToByte('N'))
                            {
                                if (byte1[u + 2] == Convert.ToByte('A'))
                                {
                                    if (byte1[u + 3] == Convert.ToByte('C'))
                                    {
                                        if (byte1[u + 4] == Convert.ToByte('H'))
                                        {
                                            if (byte1[u + 8] == Convert.ToByte('O'))
                                            {
                                                if (byte1[u + 9] == Convert.ToByte('K'))
                                                {
                                                    // ������� ����� �������
                                                    int sensorNumber = GetEncodedBytes(byte1[u + 5], 0);

                                                    // ������� �������� �����������
                                                    int znach = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                                                    fckInfo += "D" + sensorNumber;
                                                    fckInfo += "C" + znach;
                                                    u += 9;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }
            #endregion
            
            #region �����
            /////////////////////////////////////////////////////////
            if (Protocol.Protocol1 == WhatProtocol.Vlaga)
            { // �������� � ������ ��������� � ��������
                int byteLength = byte1.Length;
                for (int u = 0; u < byte1.Length; ++u)
                {
                    //try
                    //{
                        // ����������� ��������� ����� KANAL
                        if (byte1[u] == Convert.ToByte('K'))
                        {
                            if(u<byteLength-8)
                            {
                                if(byte1[u + 1] == Convert.ToByte('A'))
                                {
                                    if(byte1[u + 2] == Convert.ToByte('N'))
                                    {
                                        if(byte1[u + 3] == Convert.ToByte('A'))
                                        {
                                            if(byte1[u+4] == Convert.ToByte('L'))
                                            {
                                                if(byte1[u+6] == Convert.ToByte('O'))
                                                {
                                                    if(byte1[u+7] == Convert.ToByte('K'))
                                                    {
                                                        // ������� ����� ������
                                                        int kanalNumber = GetEncodedBytes(byte1[u + 5], 0);
                                                        fckInfo += "K" + kanalNumber.ToString();
                                                        u += 7;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    //}
                    //catch (IndexOutOfRangeException ex) { }

                    // ����������� ��������� ����� VLAGA
                        if (byte1[u] == Convert.ToByte('V'))
                        {
                            if (u < byteLength - 9)
                            {
                                if (byte1[u + 1] == Convert.ToByte('L'))
                                {
                                    if (byte1[u + 2] == Convert.ToByte('A'))
                                    {
                                        if (byte1[u + 3] == Convert.ToByte('G'))
                                        {
                                            if (byte1[u + 4] == Convert.ToByte('A'))
                                            {
                                                if (byte1[u + 8] == Convert.ToByte('O'))
                                                {
                                                    if (byte1[u + 9] == Convert.ToByte('K'))
                                                    {
                                                        // ������� ����� �������
                                                        int sensorNumber = GetEncodedBytes(byte1[u + 5], 0);

                                                        // ������� �������� �����������
                                                        int vlaga = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                                                        fckInfo += "D" + sensorNumber;
                                                        fckInfo += "V" + vlaga;
                                                        u += 9;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                }
            }
            #endregion


            #region ����������� - ��������������� ������ (����� + �����������)
            /////////////////////////////////////////////////////////
            if (Protocol.Protocol1 == WhatProtocol.VlagaTemper)
            { // �������� � ������ ��������� � ��������
                int byteLength = byte1.Length;
                for (int u = 0; u < byte1.Length; ++u)
                {
                    //try
                    //{
                    // ����������� ��������� ����� KANAL
                    if (byte1[u] == Convert.ToByte('K'))
                    {
                        if (u < byteLength - 8)
                        {
                            if (byte1[u + 1] == Convert.ToByte('A'))
                            {
                                if (byte1[u + 2] == Convert.ToByte('N'))
                                {
                                    if (byte1[u + 3] == Convert.ToByte('A'))
                                    {
                                        if (byte1[u + 4] == Convert.ToByte('L'))
                                        {
                                            if (byte1[u + 6] == Convert.ToByte('O'))
                                            {
                                                if (byte1[u + 7] == Convert.ToByte('K'))
                                                {
                                                    // ������� ����� ������
                                                    int kanalNumber = GetEncodedBytes(byte1[u + 5], 0);
                                                    fckInfo += "K" + kanalNumber.ToString();
                                                    u += 7;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //}
                    //catch (IndexOutOfRangeException ex) { }

                    // ����������� ��������� ����� VLAGA
                    if (byte1[u] == Convert.ToByte('V'))
                    {
                        if (u < byteLength - 9)
                        {
                            if (byte1[u + 1] == Convert.ToByte('L'))
                            {
                                if (byte1[u + 2] == Convert.ToByte('A'))
                                {
                                    if (byte1[u + 3] == Convert.ToByte('G'))
                                    {
                                        if (byte1[u + 4] == Convert.ToByte('A'))
                                        {
                                            if (byte1[u + 8] == Convert.ToByte('O'))
                                            {
                                                if (byte1[u + 9] == Convert.ToByte('K'))
                                                {
                                                    // ������� ����� �������
                                                    int sensorNumber = GetEncodedBytes(byte1[u + 5], 0);

                                                    // ������� �������� �����������
                                                    int vlaga = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                                                    fckInfo += "D" + sensorNumber;
                                                    fckInfo += "V" + vlaga;
                                                    u += 9;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // ����������� ��������� ����� TEMPER
                    if (byte1[u] == Convert.ToByte('T'))
                    {
                        if (u < byteLength - 10)
                        {
                            if (byte1[u + 1] == Convert.ToByte('E'))
                            {
                                if (byte1[u + 2] == Convert.ToByte('M'))
                                {
                                    if (byte1[u + 3] == Convert.ToByte('P'))
                                    {
                                        if (byte1[u + 4] == Convert.ToByte('E'))
                                        {
                                            if (byte1[u + 5] == Convert.ToByte('R'))
                                            {
                                                if (byte1[u + 9] == Convert.ToByte('O'))
                                                {
                                                    if (byte1[u + 10] == Convert.ToByte('K'))
                                                    {
                                                        // ������� ����� �������
                                                        int sensorNumber = GetEncodedBytes(byte1[u + 6], 0);

                                                        fckInfo += "D" + sensorNumber;
                                                        fckInfo += "T" + GetTemperDS18B20(byte1[u + 8], byte1[u + 7]);
                                                        u += 9;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // ����������� ��������� ����� ZNACH
                    if (byte1[u] == Convert.ToByte('Z'))
                    {
                        if (u < byteLength - 9)
                        {
                            if (byte1[u + 1] == Convert.ToByte('N'))
                            {
                                if (byte1[u + 2] == Convert.ToByte('A'))
                                {
                                    if (byte1[u + 3] == Convert.ToByte('C'))
                                    {
                                        if (byte1[u + 4] == Convert.ToByte('H'))
                                        {
                                            if (byte1[u + 8] == Convert.ToByte('O'))
                                            {
                                                if (byte1[u + 9] == Convert.ToByte('K'))
                                                {
                                                    // ������� ����� �������
                                                    int sensorNumber = GetEncodedBytes(byte1[u + 5], 0);

                                                    // ������� �������� �����������
                                                    int znach = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                                                    fckInfo += "D" + sensorNumber;
                                                    fckInfo += "C" + znach;
                                                    u += 9;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }
            #endregion


            #region �������
            /////////////////////////////////////////////////////////
            if (Protocol.Protocol1 == WhatProtocol.Suwilka)
            { // �������� � ������ ��������� � ��������
                for (int u = 0; u < byte1.Length; ++u)
                {
                    try
                    {
                        if (byte1[u] == Convert.ToByte('A') && byte1[u + 1] == Convert.ToByte('L'))
                        { // ����������� ��������� ����� KANAL
                            // ������� ����� ������
                            int kanalNumber = GetEncodedBytes(byte1[u + 2], 0);
                            if (kanalNumber != -1)
                            {
                                if (fckInfo.Length > 2)
                                {
                                    if (fckInfo[fckInfo.Length - 2] != 'K')
                                        fckInfo += "K" + kanalNumber.ToString();
                                }
                                else
                                {
                                    fckInfo += "K" + kanalNumber.ToString();
                                }
                            }
                        }
                    }
                    catch (IndexOutOfRangeException ex) { }

                    if (byte1[u-1] == Convert.ToByte('E') && byte1[u] == Convert.ToByte('R'))
                    { // ����������� ��������� ����� TEMPER
                        try
                        {
                            // ������� ����� �������
                            int sensorNumber = GetEncodedBytes(byte1[u + 1], 0);

                            // ������� �������� �����������
                            int temper = GetEncodedBytes(byte1[u + 3], byte1[u + 2]);
                            double dtemper = temper / 16.0;
                            if (dtemper > 0)
                            {
                                string dblTemper = dtemper.ToString();
                                if (!dblTemper.Contains(","))
                                {
                                    dblTemper += ",0";
                                }
                                if (sensorNumber != -1)
                                    fckInfo += "D" + sensorNumber.ToString();
                                fckInfo += "T" + dblTemper;
                            }
                        }
                        catch (IndexOutOfRangeException ex) { }
                    }

                }
            }
            #endregion
            #region ���������
            /////////////////////////////
            if (Protocol.Protocol1 == WhatProtocol.ZolaVlaga)
            { // �������� � ������ ��������� � �������� ���������
                int _zola;
                int _nagruzka;
                int _vlaga;
                int _vesi;
                int _shiber;
                int _kon;
                int _marker;
                int _endVagon;

                for (int u = 0; u < byte1.Length; ++u)
                {
                    try
                    {
                        if (byte1[u] == Convert.ToByte('K') && byte1[u + 1] == Convert.ToByte('O'))
                        { // ����������� ��������� ����� KON
                            // ������� ����� ���������
                            int konveyrNumber = GetEncodedBytes(byte1[u + 3], 0);
                            if (konveyrNumber != -1)
                            {
                                if (fckInfo.Length > 2)
                                {
                                    if (fckInfo[fckInfo.Length - 2] != 'K')
                                        fckInfo += "K" + konveyrNumber.ToString() + " ";
                                }
                                else
                                {
                                    fckInfo += "K" + konveyrNumber.ToString() + " ";
                                }
                            }
                            // ����������� �������� ����

                            // ����
                            _zola = GetEncodedBytes(byte1[u + 5], byte1[u + 4]);
                            fckInfo += "Z " + _zola.ToString() + " ";
                            // ��������
                            _nagruzka = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                            fckInfo += "N " + _nagruzka.ToString() + " ";
                            // ���������
                            _vlaga = GetEncodedBytes(byte1[u + 9], byte1[u + 8]);
                            fckInfo += "V " + _vlaga.ToString() + " ";
                            // ���� 
                            //_vesi = GetEncodedBytes(byte1[i + 11], byte1[i + 10]);
                            //PortData += "V" + _vesi.ToString();
                            // �����
                            _shiber = GetEncodedBytes(byte1[u + 12], 0);
                            fckInfo += "W " + _shiber.ToString() + " ";
                            // ��������
                            _kon = GetEncodedBytes(byte1[u + 13], 0);
                            fckInfo += "R " + _kon.ToString() + " ";
                            // ������
                            //_marker = GetEncodedBytes(byte1[i + 14], 0);
                            //PortData += "M" + _marker.ToString();
                            // ����� ������
                            _endVagon = GetEncodedBytes(byte1[u + 15], 0);
                            fckInfo += "E " + _endVagon.ToString() + " ";
                        }
                    }
                    catch (IndexOutOfRangeException ex) { }
                }
            }
            #endregion
            ////////////////////////////
            #region ����
            if (Protocol.Protocol1 == WhatProtocol.Zola)
            { // �������� � ������ ��������� � �������� ���������
                int _zola;
                int _nagruzka;
                int _vesi;
                int _shiber;
                int _kon;
                int _marker;
                int _endVagon;

                bool ifOk = false;

                for (int u = 0; u < byte1.Length; ++u)
                {
                    //try
                    //{
                    int konEnd = -1;
                    if (byte1[u] == Convert.ToByte('K') && u < byte1.Length - 1 && byte1[u + 1] == Convert.ToByte('O'))
                    { // ����������� ��������� ����� KON
                        konEnd = u + 1;
                        // ������� ����� ���������
                        int konveyrNumber = GetEncodedBytes(byte1[u + 3], 0);
                        // ����� ����� �������� ����� ��, ������� ����� �������� ���������� � ���������� ������
                        for (int h = u; h < byte1.Length; ++h)
                        {
                            if (byte1[h] == Convert.ToByte('O') && byte1[h + 1] == Convert.ToByte('K'))
                            {
                                ifOk = true;
                                if (konveyrNumber != -1)
                                {
                                    if (fckInfo.Length > 2)
                                    {
                                        if (fckInfo[fckInfo.Length - 2] != 'K')
                                            fckInfo += "K" + konveyrNumber.ToString() + " ";
                                    }
                                    else
                                    {
                                        fckInfo += "K" + konveyrNumber.ToString() + " ";
                                    }
                                }

                                //���� ������ ��������, ���������� �����
                                int okStart = h;
                                int rezPr = okStart - konEnd;
                                if (rezPr == 7)
                                { // ���� ��� ������������ �������

                                    // ����������� �������� ����
                                    // ����
                                    _zola = GetEncodedBytes(byte1[u + 5], byte1[u + 4]);
                                    fckInfo += "Z " + _zola.ToString() + " ";
                                    // ��������
                                    _nagruzka = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                                    fckInfo += "N " + _nagruzka.ToString() + " ";
                                    // ���� 
                                    //_vesi = GetEncodedBytes(byte1[i + 9], byte1[i + 8]);
                                    //PortData += "V" + _vesi.ToString();
                                    h = byte1.Length;
                                    u += 6;
                                }
                                if (rezPr == 13)
                                { // ���� ���� ����������� ������
                                    // ����������� �������� ����
                                    // ����
                                    _zola = GetEncodedBytes(byte1[u + 5], byte1[u + 4]);
                                    fckInfo += "Z " + _zola.ToString() + " ";
                                    // ��������
                                    _nagruzka = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                                    fckInfo += "N " + _nagruzka.ToString() + " ";
                                    // ���� 
                                    //_vesi = GetEncodedBytes(byte1[i + 9], byte1[i + 8]);
                                    //PortData += "V" + _vesi.ToString();
                                    // �����
                                    _shiber = GetEncodedBytes(byte1[u + 10], 0);
                                    fckInfo += "W " + _shiber.ToString() + " ";
                                    // ��������
                                    _kon = GetEncodedBytes(byte1[u + 11], 0);
                                    fckInfo += "R " + _kon.ToString() + " ";
                                    // ������
                                    //_marker = GetEncodedBytes(byte1[i + 12], 0);
                                    //PortData += "M" + _marker.ToString();
                                    // ����� ������
                                    _endVagon = GetEncodedBytes(byte1[u + 13], 0);
                                    fckInfo += "E " + _endVagon.ToString() + " ";

                                    h = byte1.Length;
                                    u += 12;
                                }
                            }
                        }
                        if (!ifOk)
                        {
                            //MessageBox.Show(Convert.ToString(byte1.Length - u));
                            if ((byte1.Length - u) == 8)
                            {
                                if (konveyrNumber != -1)
                                {
                                    if (fckInfo.Length > 2)
                                    {
                                        if (fckInfo[fckInfo.Length - 2] != 'K')
                                            fckInfo += "K" + konveyrNumber.ToString() + " ";
                                    }
                                    else
                                    {
                                        fckInfo += "K" + konveyrNumber.ToString() + " ";
                                    }
                                }
                                // ����������� �������� ����
                                // ����
                                _zola = GetEncodedBytes(byte1[u + 5], byte1[u + 4]);
                                fckInfo += "Z " + _zola.ToString() + " ";
                                // ��������
                                _nagruzka = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                                fckInfo += "N " + _nagruzka.ToString() + " ";
                                // ���� 
                                //_vesi = GetEncodedBytes(byte1[i + 9], byte1[i + 8]);
                                //PortData += "V" + _vesi.ToString();
                                //h = byte1.Length;
                                //u += 6;
                            }
                        }

                        #region OldCode
                        //    // ����������� �������� ����

                        //    // ����
                        //    _zola = GetEncodedBytes(byte1[u + 5], byte1[u + 4]);
                        //_EncodeData += "Z " + _zola.ToString() + " ";
                        //// ��������
                        //_nagruzka = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                        //_EncodeData += "N " + _nagruzka.ToString() + " ";
                        //// ���� 
                        ////_vesi = GetEncodedBytes(byte1[i + 9], byte1[i + 8]);
                        ////PortData += "V" + _vesi.ToString();
                        //// �����
                        //_shiber = GetEncodedBytes(byte1[u + 10], 0);
                        //_EncodeData += "W " + _shiber.ToString() + " ";
                        //// ��������
                        //_kon = GetEncodedBytes(byte1[u + 11], 0);
                        //_EncodeData += "R " + _kon.ToString() + " ";
                        //// ������
                        ////_marker = GetEncodedBytes(byte1[i + 12], 0);
                        ////PortData += "M" + _marker.ToString();
                        //// ����� ������
                        //_endVagon = GetEncodedBytes(byte1[u + 13], 0);
                        //_EncodeData += "E " + _endVagon.ToString() + " 
                        #endregion
                    }
                    //}
                    //catch (IndexOutOfRangeException ex) { }
                }

            }
            #endregion
            ////////////////////////////
            //EndEncodingHandler.Invoke();
            //MessageBox.Show(byte1.Length.ToString()+"  "+ fckInfo);
            return fckInfo;
        }

        /// <summary>
        /// �������������� ����� � ��������� ������ � 3 ����� float �������
        /// </summary>
        /// <param name="d">�����, ������� ����� �������������</param>
        /// <returns>������ ����</returns>
        public static byte[] Get3ByteFloatFromDouble(double d)
        {
            int[] a = new int[18];

            // ����
            int sign1;
            if (d >= 0)
                sign1 = 0;
            else
                sign1 = 1;
            string ssign = sign1.ToString();

            // ����� �������
            double z = Math.Log(d) / Math.Log(2);

            // ����� ����������
            int e = (int)z;
            if (e > z)
                e = e - 1;

            // ����� ������� ����� - ����� ��������� �� 2 � ������� ����������
            double x = d / Math.Pow(2, e);

            string mantiss = "";
            for (int k = 0; k < 17; k++) // �������������� � �������� ������
            {
                if (x >= Math.Pow(2, -k))
                    a[k] = 1;
                else
                    a[k] = 0;

                if (k > 0 && k < 16)
                    mantiss += a[k]; // ��������� ��������, ������� ����� ������ � �����

                x = x - a[k] * Math.Pow(2, -k);
            }

            // ����������, ������� ����� ������ � �����
            int eb = e + 127;
            if (d == 0)
                eb = 0;

            // ���� � ���������� �������� ������, ��� ��� �����, ������ ������� ���� ������
            string sexp1 = Convert.ToString(eb, 2);
            while (sexp1.Length < 8)
                sexp1 = "0" + sexp1;

            // ������������ ������
            string fb1 = ssign + sexp1.Substring(0, 7);
            string sb1 = sexp1[7] + mantiss.Substring(0, 7);
            string tb1 = mantiss.Substring(7, 8);


            // ����� ������ � HEX ����
            byte b11 = Convert.ToByte(fb1, 2);
            string h11 = b11.ToString("X");

            byte b22 = Convert.ToByte(sb1, 2);
            string h22 = b22.ToString("X");

            byte b33 = Convert.ToByte(tb1, 2);
            string h33 = b33.ToString("X");

            return new byte[] { b33, b22, b11 };
        }

        ///// <summary>
        ///// ������� �������������� ����������
        ///// </summary>
        //public void ClearData()
        //{
            
        //}
    }
}
