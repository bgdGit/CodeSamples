using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace MBLL
{
    /// <summary>
    /// Класс, который занимается расшифровкой сигналов с СОМ и USB портов
    /// </summary>
    public static class EncoderClass
    {

        /// <summary>
        ///  раскодировка байт из протокола
        /// </summary>
        /// <param name="FByte">первый байт</param>
        /// <param name="SByte">второй байт</param>
        /// <returns></returns>
        private static int GetEncodedBytes(byte FByte, byte SByte)
        { //
            byte[] b = new byte[2];
            // проверка на порядок байтов в системе
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
        /// получение температуры в формате особенного датчика DS18B20
        /// </summary>
        /// <param name="mladwij">младший байт</param>
        /// <param name="starwij">старший байт</param>
        /// <returns>температура</returns>
        private static double GetTemperDS18B20(byte mladwij, byte starwij)
        {
            ushort w = mladwij;            // Читаем сначала младший байт
            w |= (ushort)(starwij << 8);   // Читаем и добавим к слову старший байт
            bool negFlag = ((w & 0x1000) != 0);     // Признак отрицательной температуры
            if (negFlag)                            // Вычислим модуль
                w = (ushort)(65536 - w);
            // Из 4 младших бит формируем дробную часть
            double dt = (((w & 0x0008) != 0) ? 0.5 : 0) +
                        (((w & 0x0004) != 0) ? 0.25 : 0) +
                        (((w & 0x0002) != 0) ? 0.125 : 0) +
                        (((w & 0x0001) != 0) ? 0.0625 : 0);

            double temper = ((w >> 4)) + dt;
            if (negFlag)                            // Теперь учтем знак
                temper = -temper;

            return temper;
        }

        /// <summary>
        /// расшифровка информации по одному из протоколов
        /// </summary>
        /// <param name="byte1">нерасшифрованная,исходная информация</param>
        public static string EncodeInfo(Byte[] byte1)
        {
            string fckInfo = "";

            #region Знач
            if (Protocol.Protocol1 == WhatProtocol.Znach)
            { // алгоритм в случае протокола с сушилкой
                int byteLength = byte1.Length;
                for (int u = 0; u < byte1.Length; ++u)
                {
                    //try
                    //{
                    // расшифровка ключевого слова KANAL
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
                                                    // достаем номер канала
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

                    // расшифровка ключевого слова ZNACH
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
                                                    // достаем номер датчика
                                                    int sensorNumber = GetEncodedBytes(byte1[u + 5], 0);

                                                    // достаем значение температуры
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
            
            #region Влага
            /////////////////////////////////////////////////////////
            if (Protocol.Protocol1 == WhatProtocol.Vlaga)
            { // алгоритм в случае протокола с сушилкой
                int byteLength = byte1.Length;
                for (int u = 0; u < byte1.Length; ++u)
                {
                    //try
                    //{
                        // расшифровка ключевого слова KANAL
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
                                                        // достаем номер канала
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

                    // расшифровка ключевого слова VLAGA
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
                                                        // достаем номер датчика
                                                        int sensorNumber = GetEncodedBytes(byte1[u + 5], 0);

                                                        // достаем значение температуры
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


            #region Влагатемпер - Комбинированный датчик (влага + температура)
            /////////////////////////////////////////////////////////
            if (Protocol.Protocol1 == WhatProtocol.VlagaTemper)
            { // алгоритм в случае протокола с сушилкой
                int byteLength = byte1.Length;
                for (int u = 0; u < byte1.Length; ++u)
                {
                    //try
                    //{
                    // расшифровка ключевого слова KANAL
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
                                                    // достаем номер канала
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

                    // расшифровка ключевого слова VLAGA
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
                                                    // достаем номер датчика
                                                    int sensorNumber = GetEncodedBytes(byte1[u + 5], 0);

                                                    // достаем значение температуры
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

                    // расшифровка ключевого слова TEMPER
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
                                                        // достаем номер датчика
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

                    // расшифровка ключевого слова ZNACH
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
                                                    // достаем номер датчика
                                                    int sensorNumber = GetEncodedBytes(byte1[u + 5], 0);

                                                    // достаем значение температуры
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


            #region Сушилка
            /////////////////////////////////////////////////////////
            if (Protocol.Protocol1 == WhatProtocol.Suwilka)
            { // алгоритм в случае протокола с сушилкой
                for (int u = 0; u < byte1.Length; ++u)
                {
                    try
                    {
                        if (byte1[u] == Convert.ToByte('A') && byte1[u + 1] == Convert.ToByte('L'))
                        { // расшифровка ключевого слова KANAL
                            // достаем номер канала
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
                    { // расшифровка ключевого слова TEMPER
                        try
                        {
                            // достаем номер датчика
                            int sensorNumber = GetEncodedBytes(byte1[u + 1], 0);

                            // достаем значение температуры
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
            #region Золавлага
            /////////////////////////////
            if (Protocol.Protocol1 == WhatProtocol.ZolaVlaga)
            { // алгоритм в случае протокола с датчиком зольности
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
                        { // расшифровка ключевого слова KON
                            // достаем номер конвейера
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
                            // расшифровка числовых байт

                            // зола
                            _zola = GetEncodedBytes(byte1[u + 5], byte1[u + 4]);
                            fckInfo += "Z " + _zola.ToString() + " ";
                            // нагрузка
                            _nagruzka = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                            fckInfo += "N " + _nagruzka.ToString() + " ";
                            // влажность
                            _vlaga = GetEncodedBytes(byte1[u + 9], byte1[u + 8]);
                            fckInfo += "V " + _vlaga.ToString() + " ";
                            // весы 
                            //_vesi = GetEncodedBytes(byte1[i + 11], byte1[i + 10]);
                            //PortData += "V" + _vesi.ToString();
                            // шибер
                            _shiber = GetEncodedBytes(byte1[u + 12], 0);
                            fckInfo += "W " + _shiber.ToString() + " ";
                            // конвейер
                            _kon = GetEncodedBytes(byte1[u + 13], 0);
                            fckInfo += "R " + _kon.ToString() + " ";
                            // маркер
                            //_marker = GetEncodedBytes(byte1[i + 14], 0);
                            //PortData += "M" + _marker.ToString();
                            // конец вагона
                            _endVagon = GetEncodedBytes(byte1[u + 15], 0);
                            fckInfo += "E " + _endVagon.ToString() + " ";
                        }
                    }
                    catch (IndexOutOfRangeException ex) { }
                }
            }
            #endregion
            ////////////////////////////
            #region Зола
            if (Protocol.Protocol1 == WhatProtocol.Zola)
            { // алгоритм в случае протокола с датчиком зольности
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
                    { // расшифровка ключевого слова KON
                        konEnd = u + 1;
                        // достаем номер конвейера
                        int konveyrNumber = GetEncodedBytes(byte1[u + 3], 0);
                        // нужно найти ключевое слово ОК, которое будет означать целосность и валидность данных
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

                                //если данные целостны, расшифруем цифры
                                int okStart = h;
                                int rezPr = okStart - konEnd;
                                if (rezPr == 7)
                                { // если нет управляющего сигнала

                                    // расшифровка числовых байт
                                    // зола
                                    _zola = GetEncodedBytes(byte1[u + 5], byte1[u + 4]);
                                    fckInfo += "Z " + _zola.ToString() + " ";
                                    // нагрузка
                                    _nagruzka = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                                    fckInfo += "N " + _nagruzka.ToString() + " ";
                                    // весы 
                                    //_vesi = GetEncodedBytes(byte1[i + 9], byte1[i + 8]);
                                    //PortData += "V" + _vesi.ToString();
                                    h = byte1.Length;
                                    u += 6;
                                }
                                if (rezPr == 13)
                                { // если есть управляющий сигнал
                                    // расшифровка числовых байт
                                    // зола
                                    _zola = GetEncodedBytes(byte1[u + 5], byte1[u + 4]);
                                    fckInfo += "Z " + _zola.ToString() + " ";
                                    // нагрузка
                                    _nagruzka = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                                    fckInfo += "N " + _nagruzka.ToString() + " ";
                                    // весы 
                                    //_vesi = GetEncodedBytes(byte1[i + 9], byte1[i + 8]);
                                    //PortData += "V" + _vesi.ToString();
                                    // шибер
                                    _shiber = GetEncodedBytes(byte1[u + 10], 0);
                                    fckInfo += "W " + _shiber.ToString() + " ";
                                    // конвейер
                                    _kon = GetEncodedBytes(byte1[u + 11], 0);
                                    fckInfo += "R " + _kon.ToString() + " ";
                                    // маркер
                                    //_marker = GetEncodedBytes(byte1[i + 12], 0);
                                    //PortData += "M" + _marker.ToString();
                                    // конец вагона
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
                                // расшифровка числовых байт
                                // зола
                                _zola = GetEncodedBytes(byte1[u + 5], byte1[u + 4]);
                                fckInfo += "Z " + _zola.ToString() + " ";
                                // нагрузка
                                _nagruzka = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                                fckInfo += "N " + _nagruzka.ToString() + " ";
                                // весы 
                                //_vesi = GetEncodedBytes(byte1[i + 9], byte1[i + 8]);
                                //PortData += "V" + _vesi.ToString();
                                //h = byte1.Length;
                                //u += 6;
                            }
                        }

                        #region OldCode
                        //    // расшифровка числовых байт

                        //    // зола
                        //    _zola = GetEncodedBytes(byte1[u + 5], byte1[u + 4]);
                        //_EncodeData += "Z " + _zola.ToString() + " ";
                        //// нагрузка
                        //_nagruzka = GetEncodedBytes(byte1[u + 7], byte1[u + 6]);
                        //_EncodeData += "N " + _nagruzka.ToString() + " ";
                        //// весы 
                        ////_vesi = GetEncodedBytes(byte1[i + 9], byte1[i + 8]);
                        ////PortData += "V" + _vesi.ToString();
                        //// шибер
                        //_shiber = GetEncodedBytes(byte1[u + 10], 0);
                        //_EncodeData += "W " + _shiber.ToString() + " ";
                        //// конвейер
                        //_kon = GetEncodedBytes(byte1[u + 11], 0);
                        //_EncodeData += "R " + _kon.ToString() + " ";
                        //// маркер
                        ////_marker = GetEncodedBytes(byte1[i + 12], 0);
                        ////PortData += "M" + _marker.ToString();
                        //// конец вагона
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
        /// Преобразование числа с плавающей точкой в 3 байта float формата
        /// </summary>
        /// <param name="d">число, которое нужно преобразовать</param>
        /// <returns>массив байт</returns>
        public static byte[] Get3ByteFloatFromDouble(double d)
        {
            int[] a = new int[18];

            // знак
            int sign1;
            if (d >= 0)
                sign1 = 0;
            else
                sign1 = 1;
            string ssign = sign1.ToString();

            // поиск степени
            double z = Math.Log(d) / Math.Log(2);

            // поиск экспоненты
            int e = (int)z;
            if (e > z)
                e = e - 1;

            // поиск дробной части - число разделить на 2 в степени экспоненты
            double x = d / Math.Pow(2, e);

            string mantiss = "";
            for (int k = 0; k < 17; k++) // преобразование в бинарный формат
            {
                if (x >= Math.Pow(2, -k))
                    a[k] = 1;
                else
                    a[k] = 0;

                if (k > 0 && k < 16)
                    mantiss += a[k]; // получение мантиссы, которая будет зашита в байты

                x = x - a[k] * Math.Pow(2, -k);
            }

            // экспонента, которая будет зашита в байты
            int eb = e + 127;
            if (d == 0)
                eb = 0;

            // если в экспоненте символов меньше, чем для байта, забьем старшие биты нулями
            string sexp1 = Convert.ToString(eb, 2);
            while (sexp1.Length < 8)
                sexp1 = "0" + sexp1;

            // формирование байтов
            string fb1 = ssign + sexp1.Substring(0, 7);
            string sb1 = sexp1[7] + mantiss.Substring(0, 7);
            string tb1 = mantiss.Substring(7, 8);


            // вывоб байтов в HEX коде
            byte b11 = Convert.ToByte(fb1, 2);
            string h11 = b11.ToString("X");

            byte b22 = Convert.ToByte(sb1, 2);
            string h22 = b22.ToString("X");

            byte b33 = Convert.ToByte(tb1, 2);
            string h33 = b33.ToString("X");

            return new byte[] { b33, b22, b11 };
        }

        ///// <summary>
        ///// очистка расшифрованной информации
        ///// </summary>
        //public void ClearData()
        //{
            
        //}
    }
}
