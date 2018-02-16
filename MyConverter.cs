using System;
using System.Drawing;

namespace MBLL
{
    public static class MyConverter
    {
        public static Point GetPointFromString(string s)
        { // пересчет значений из бд строки в тип Point
            if (s.Contains(" "))
            {
                int i = s.IndexOf(" ");
                string stmp = s.Substring(0, i);
                int dx = Convert.ToInt32(stmp);
                s = s.Remove(0, i + 1);
                int dy = Convert.ToInt32(s);

                return new Point(dx, dy);
            }
            else
            {
                return new Point(-1, -1);
            }
        }

        /// <summary>
        /// пересчет значений из бд строки в тип Size
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Size GetSizeFromString(string s)
        { // 
            if (s.Contains(" "))
            {
                int i = s.IndexOf(" ");
                string stmp = s.Substring(0, i);
                int dx = Convert.ToInt32(stmp);
                s = s.Remove(0, i + 1);
                int dy = Convert.ToInt32(s);

                return new Size(dx, dy);
            }
            else
            {
                return new Size(-1, -1);
            }
        }

        public static string GetStringFromPoint(Point p)
        { // пересчет значения Point в строку моего формата
            string s = p.X + " " + p.Y;

            return s;
        }

        public static string GetStringFromSize(Size sz)
        { // пересчет значения Size в строку моего формата
            string s = sz.Width + " " + sz.Height;

            return s;
        }

        /// <summary>
        /// получения адресов регистров для Modbus протоколов
        /// </summary>
        /// <param name="valueRegister"></param>
        /// <param name="paramRegister"></param>
        /// <param name="sensNumber"></param>
        public static void GetModbusAddresses(out string valueRegister, out string paramRegister, string sensNumber)
        {
            valueRegister = string.Format("10{0}", sensNumber);
            paramRegister = string.Format("20{0}", sensNumber);

            if (sensNumber.Length == 3)
            {
                string num = sensNumber.Substring(0, 2);
                if (sensNumber[2] == 'T')
                {
                    valueRegister = string.Format("11{0}", num);
                    paramRegister = string.Format("21{0}", num);
                }
                else
                {
                    valueRegister = string.Format("12{0}", num);
                    paramRegister = string.Format("22{0}", num);
                }
            }
        }
    }
}
