using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MBLL.DB_classes;
using ZedGraph;
using MBLL;
using Microsoft.Office.Interop.Excel;
using System.IO;
using System.Drawing.Printing;
using MDAL;

namespace WatchOnRawMaterial
{
    public partial class ParameterInfoForm : Form
    {
        private Sensor _sensor;
        private string _supplier;
        private string _material;
        private MBLL.Parameter _parameter;

        private List<Value> _data;

        private List<Value> ListForPrinting;

        private System.Data.DataTable dataTable;

        private ZedGraphControl graph; // контрол графика

        private bool _isFilter; // включен ли фильтр значений

        private double _beforeValue; // предыдущее значение с датчика

        private int CurrentValue; // номер значения из листа для печати

        private Formula _formula;
        private FilterErika _filtrErika;
        public Formula CurFormula
        {
            get { return _formula; }
            set { _formula = value; }
        }

        public FilterErika CurFiltrErika
        {
            get { return _filtrErika; }
            set { _filtrErika = value; }
        }

        private PointPairList _dataImpulses; // инфа в импульсах
        private PointPairList _dataUser; // инфа в виде для пользователя
        private PointPairList _dataSpline; // сплайн
        /// <summary>
        /// массив для второго среднего графика в случае данных заказчика
        /// </summary>
        private PointPairList _dataSpline10;
        /// <summary>
        /// количество точек для сплайна10
        /// </summary>
        private readonly int _splineCount = 5;

        private bool _isReport; // флаг для определения для чего вызывается форма для отчета или отображения графиков

        public void ClearData()
        {
            //_data.Clear();
            //_dataImpulses.Clear();
            //_dataUser.Clear();
            //graph.DataList.Clear();
            //graph.AxisChange();
            //graph.Invalidate();
        }

        /// <summary>
        /// добавляет на график верхнюю и нижнюю границу для импульсов в фильтре
        /// </summary>
        private void AddBoundsToImpulses()
        {
            if (_filtrErika != null)
            {
                double dmax = XDate.XLDayMax - 2;
                double dmin = XDate.XLDayMin + 2;

                graph.UpperImpulsesBoundList.Add(dmax, _filtrErika.ZolaMaxImpulses);
                graph.UpperImpulsesBoundList.Add(dmin, _filtrErika.ZolaMaxImpulses);
                //graph.UpperImpulseBound = graph.GraphPane.AddCurve(string.Format("Макс:{0}",_filtrErika.ZolaMaxImpulses), graph.UpperImpulsesBoundList, Color.DeepPink, SymbolType.None);

                graph.UnderImpulsesBoundList.Add(dmax, _filtrErika.ZolaMinImpulses);
                graph.UnderImpulsesBoundList.Add(dmin, _filtrErika.ZolaMinImpulses);
                //graph.UnderImpulseBound = graph.GraphPane.AddCurve(string.Format("Мин:{0}", _filtrErika.ZolaMinImpulses), graph.UnderImpulsesBoundList, Color.DeepPink, SymbolType.None);

                //graph.Invalidate();
            }
        }



        /// <summary>
        /// считает среднеарифметическое из заданного количества точек для единично добавляемого значения
        /// </summary>
        /// <param name="list">массив исходных точек</param>
        /// <param name="splineNumber">количество точек для усреднения</param>
        /// <param name="splineList">массив сплайновых точек</param>
        /// <returns></returns>
        private PointPairList FillSpline(PointPairList list, int splineNumber, PointPairList splineList)
        { // 
            if (list.Count > splineNumber)
            {
                double spline = 0;
                for (int i = list.Count - 1; i > list.Count - splineNumber - 1; --i)
                {
                    spline += _dataUser[i].Y;
                }
                spline /= splineNumber;

                spline = Math.Round(spline, 1);

                if (splineList == null)
                    splineList = new PointPairList();
                splineList.Add(list[list.Count - 1].X, spline);
            }

            return splineList;
        }

        private List<Value> GetFilteredList(PointPairList ppl, bool isImpulses)
        { // возвращает переделанный поинтПаир лист в валуес лист
            List<Value> rez = new List<Value>();

            for (int i = 0; i < ppl.Count; ++i)
            {
                Value val = new Value();

                val.TimeOfAdd = XDate.XLDateToDateTime(ppl[i].X);
                val.Value1 = ppl[i].Y;
                rez.Add(val);
            }

            return rez;
        }

        private void GetDataTable(PointPairList ppl)
        { // заполняем таблицу значений
            dataTable.Clear();
            foreach (PointPair pp in ppl)
            {
                DataRow row = dataTable.NewRow();
                row[1] = XDate.XLDateToDateTime(pp.X);
                row[0] = Math.Round(pp.Y,1);
                dataTable.Rows.Add(row);
            }
        }

        public ParameterInfoForm(Sensor Sensor, string Supplier, string Material, List<Value> Data, Formula formula, FilterErika filterErika, bool isReport)
        {
            InitializeComponent();

            if (Data != null && Data.Count > 0)
                Data.Sort(new ValuesCompareByDate());
            if (Protocol.Protocol1 == WhatProtocol.Suwilka)
            {
                SupplierLabel.Text = "Поставщик :";
                MaterialLabel.Text = "Потребитель :";
            }
            if (Protocol.Protocol1 == WhatProtocol.Zola || Protocol.Protocol1 == WhatProtocol.ZolaVlaga)
            {
                SupplierLabel.Text = "Потребитель :";
                MaterialLabel.Text = "№ удостоверения :";
            }

            _isReport = isReport;
            _formula = formula;
            Culture tmpCult =
                CultureGateway.Instance.GetCultureBySensorIdAndCultureNameId(Sensor.Id, Sensor.CultureNameId);
            if (_formula == null)
                _formula = FormulsGateway.Instance.GiveMeNewFormula(tmpCult.Id);
            _sensor = Sensor;
            if(_supplier != null)
                _supplier = Supplier;
            else
                _supplier = "-";
            if (_material != null)
                _material = Material;
            else
                _material = "-";

            _parameter = ParametersGateway.Instance.GetParameterByID(_sensor.ParameterId);

            _data = new List<Value>();
            graph = new ZedGraphControl();
            // цвет графика
            graph.GraphColor = Color.Blue;
            // метод построения графика
            graph.IsLineGraph = true;
            graph.IsBigPoints = false;
            //graph.GraphCurve.Symbol.Size = 2;
            ListForPrinting = new List<Value>();
            // высчитаем оптимальное количество точек на графике
            graph.ListCapacity = 60 * _sensor.GraphHour;  
            _filtrErika = filterErika;
            if (_filtrErika == null)
                _filtrErika = FilterErikaGateway.Instance.GiveMeNewFilter(tmpCult.Id);

            GraphPane pane = graph.GraphPane;

            // Установим размеры шрифтов для меток вдоль осей
            pane.XAxis.Scale.FontSpec.Size = 16;
            pane.YAxis.Scale.FontSpec.Size = 16;

            // Установим размеры шрифтов для подписей по осям
            //pane.XAxis.Title.FontSpec.Size = 35;
            //pane.YAxis.Title.FontSpec.Size = 35;

            // Установим размеры шрифта для легенды
            //pane.Legend.FontSpec.Size = 35;

            // Установим размеры шрифта для общего заголовка
            pane.Title.FontSpec.Size = 16;
            //pane.Title.FontSpec.IsUnderline = true;

            if(_formula != null)
            {
                if (_formula.IsFilterUsing == 1)
                {
                    unitsOfUserToolStripMenuItem.Checked = true;
                    impulsesToolStripMenuItem.Checked = false;
                    splineToolStripMenuItem.Checked = false;
                    graphBoundsToolStripMenuItem.Enabled = true;
                    SetYBounds(true);
                }
                else if (_formula.IsFilterUsing == 0)
                {
                    unitsOfUserToolStripMenuItem.Checked = false;
                    impulsesToolStripMenuItem.Checked = true;
                    splineToolStripMenuItem.Checked = false;
                    graphBoundsToolStripMenuItem.Enabled = false;
                    SetYBounds(false);
                }
                else if (_formula.IsFilterUsing == 2)
                {
                    unitsOfUserToolStripMenuItem.Checked = false;
                    impulsesToolStripMenuItem.Checked = false;
                    splineToolStripMenuItem.Checked = true;
                    graphBoundsToolStripMenuItem.Enabled = true;
                    SetYBounds(true);
                }
            }

            _dataImpulses = new PointPairList();
            _dataUser = new PointPairList();
            _dataSpline = new PointPairList();
            _dataSpline10 = new PointPairList();

            dataTable = new System.Data.DataTable();
            
            dataTable.Columns.Add("Значение ");
            dataTable.Columns.Add("Время добавления ");
            //dataTable.Columns[1]. = 150;

            if (Data != null && Data.Count > 0)
            {
                _data = Data;
                // запихать данные на датаГрид и график
                for (int i = 0; i < _data.Count; ++i)
                {
                    double d = XDate.CalendarDateToXLDate(_data[i].TimeOfAdd.Year, _data[i].TimeOfAdd.Month,
                        _data[i].TimeOfAdd.Day, _data[i].TimeOfAdd.Hour, _data[i].TimeOfAdd.Minute,
                        _data[i].TimeOfAdd.Second);

                    _dataImpulses.Add(d, _data[i].Value1);
                    double tmpValue = _data[i].Value1;
                    if (_formula != null && _filtrErika != null)
                        tmpValue = _filtrErika.GetFilteredValue(_data[i].Value1, _formula);

                    tmpValue = Math.Round(tmpValue, 1);

                    if (_sensor.SensorNum[1] == '3' && (Protocol.Protocol1 == WhatProtocol.Zola || Protocol.Protocol1 == WhatProtocol.ZolaVlaga)) // если нагрузка
                    {
                        if (_filtrErika.WeightKonveyrState == KonveyrStates.EmptyKonveyr)
                        {
                            tmpValue = 0;
                        }
                        if (_filtrErika.WeightKonveyrState != KonveyrStates.NoData && _filtrErika.WeightKonveyrState != KonveyrStates.MinMin
                            && _filtrErika.WeightKonveyrState != KonveyrStates.KonveyrMaxError)
                        {
                            _dataUser.Add(d, tmpValue);
                            _dataSpline = FillSpline(_dataUser, _sensor.SplineNumber, _dataSpline);
                            _dataSpline10 = FillSpline(_dataUser, _splineCount, _dataSpline10);
                        }
                    }
                    else
                    {
                        if (tmpValue > UniConstants.ErrorLevel/*tmpValue != -777 && tmpValue != -111 && tmpValue != -333 && tmpValue != -77 && tmpValue != 0 && tmpValue != -55*/)
                        {
                            _dataUser.Add(d, tmpValue);
                            _dataSpline = FillSpline(_dataUser, _sensor.SplineNumber, _dataSpline);
                            _dataSpline10 = FillSpline(_dataUser, _splineCount, _dataSpline10);
                        }
                    }
                }
                
                if(_formula != null)
                {
                    if (_formula.IsFilterUsing == 1)
                    {
                        foreach (PointPair pp in _dataUser)
                            graph.DataList.Add(pp);
                        foreach (PointPair pair in _dataSpline10)
                            graph.DataSplineList.Add(pair);
                        if (_dataUser.Count > 0)
                            LastValueButton.Text = Math.Round(_dataUser[_dataUser.Count - 1].Y, 1).ToString();
                        SetNamesOfBoundsImpulsesToEmptyStr();

                        GetDataTable(_dataUser);
                    }
                    else if (_formula.IsFilterUsing == 0)
                    {
                        foreach (PointPair pp in _dataImpulses)
                            graph.DataList.Add(pp);
                        if (_dataImpulses.Count > 0)
                            LastValueButton.Text = Math.Round(_dataUser[_dataUser.Count - 1].Y, 1).ToString();
                        SetNamesOfBoundsImpulsesToValue();

                        GetDataTable(_dataImpulses);
                    }
                    else if (_formula.IsFilterUsing == 2)
                    {
                        foreach (PointPair pp in _dataSpline)
                            graph.DataList.Add(pp);
                        if (_dataSpline.Count > 0)
                            LastValueButton.Text = Math.Round(_dataUser[_dataUser.Count - 1].Y, 1).ToString();
                        SetNamesOfBoundsImpulsesToEmptyStr();

                        GetDataTable(_dataSpline);
                    }
                }
                else
                {
                    foreach (PointPair pp in _dataImpulses)
                        graph.DataList.Add(pp);
                    if (_dataImpulses.Count > 0)
                        LastValueButton.Text = Math.Round(_dataUser[_dataUser.Count - 1].Y, 1).ToString();
                    SetNamesOfBoundsImpulsesToValue();

                    GetDataTable(_dataImpulses);
                }
                ApplyGraphChanges(graph.DataList);
                _beforeValue = _data[_data.Count - 1].Value1;

                dataGridView1.DataSource = dataTable;
                dataGridView1.Columns[1].Width = 175;
            }
            progressBar1.Visible = false;
        }

        public void SetGraphOptions(int gColor, int PointSize, int IsLine)
        {
            // цвет графика
            graph.GraphColor = Color.FromArgb(gColor);
            // метод построения графика
            if(IsLine == 0)
                graph.IsLineGraph = false;
            else
                graph.IsLineGraph = true;
            //if (PointSize == 2)
            //{
            //    graph.IsBigPoints = false;
            //    graph.GraphCurve.Symbol.Size = 2;
            //}
            //else
            //{
            //    graph.IsBigPoints = true;
            //    graph.GraphCurve.Symbol.Size = 5;
            //}
        }

        void printDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {// что будем печатать
            // шрифт
            System.Drawing.Font printFont = new System.Drawing.Font("Arial", 14, FontStyle.Regular);
            Brush brush = Brushes.Black; // кисть
            string text = string.Empty; // строка с текстом
            float tab = 20; // отступ по высоте

            // количество строк вмещаемых на листе
            float linesPerPage = e.MarginBounds.Height / DivisionManagerClass.GetDivision(printFont.GetHeight(e.Graphics));
            int CurPage = 0;
            // печать шапки ---------------------------------------//
            if (Protocol.Protocol1 == WhatProtocol.Suwilka)
            {
                text = "Поставщик : " + _supplier;
                e.Graphics.DrawString(text, printFont, brush, 20, tab);
                tab += printFont.Height + printFont.Height;
                text = "Потребитель : " + _material;
            }
            if (Protocol.Protocol1 == WhatProtocol.Zola || Protocol.Protocol1 == WhatProtocol.ZolaVlaga)
            {
                text = "Потребитель : " + _supplier;
                e.Graphics.DrawString(text, printFont, brush, 20, tab);
                tab += printFont.Height + printFont.Height;
                text = "№ удостоверения : " + _material;
            }
            e.Graphics.DrawString(text, printFont, brush, 20, tab);
            tab += printFont.Height + printFont.Height;
            text = "Параметр : " + _parameter.ParameterName;
            e.Graphics.DrawString(text, printFont, brush, 20, tab);
            tab += printFont.Height + printFont.Height;
            CultureName cn = CultureNameGateway.Instance.GetCultureNameById(_sensor.CultureNameId);
            if(cn != null)
            {
                text = string.Format("Культура: ", cn.NameC);
                e.Graphics.DrawString(text, printFont, brush, 20, tab);
                tab += printFont.Height + printFont.Height;
            }
            text = "Значения :";
            e.Graphics.DrawString(text, printFont, brush, 20, tab);
            tab += printFont.Height + printFont.Height;
            CurPage += 4;
            //-----------------------------------------------------//

            // пока есть рядки и они влазят на одну страницу
            for (int i = CurPage; i < linesPerPage && CurrentValue < ListForPrinting.Count; ++i)
            {
                text = ListForPrinting[CurrentValue].Value1 + "   -------   " + ListForPrinting[CurrentValue].TimeOfAdd.ToLongDateString() + " " + ListForPrinting[CurrentValue].TimeOfAdd.ToLongTimeString();
              
                // собственно вывод на приемник
                e.Graphics.DrawString(text, printFont,brush, 20, i * printFont.Height + printFont.Height * 2 + tab);
                CurrentValue++; // считаем рядки
                CurPage++;
            }
            if (CurrentValue < _data.Count) // если не все рядки напечатались, печатаем далее
            {
                e.HasMorePages = true;
            }
            else
            { // если все напечаталось, остановимся
                e.HasMorePages = false;
                CurrentValue = 0;
            }
        }

        private void ParameterInfoForm_Load(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            SupplierInfoLabel.Text = _supplier;
            MaterialInfoLabel.Text = _material;
            if(_sensor != null)
            {
                CultureName cn = CultureNameGateway.Instance.GetCultureNameById(_sensor.CultureNameId);
                if (cn != null)
                    labelCulture.Text = cn.NameC;
                else
                    labelCulture.Text = "";
            }
            SensorInfoRTB.Text = _sensor.SensorInfo;
            ValueLabel.Text = _parameter.ParameterName + " :";
            Text = string.Format("Датчик № {0} - {1}", _sensor.SensorNum, _parameter.ParameterName);

            dataGridView1.ReadOnly = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            if(dataGridView1.Rows.Count > 0)
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Selected = true;
            
            /*добавить график размером с панель графиков-----------------------------------------------------*/
            graph.Location = new System.Drawing.Point(0, 0);
            graph.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            graph.Size = new Size(GraphPanel.Width - 10, GraphPanel.Height - 10);

            // Сетка -----------------------------------------------------------------------------
            // Включаем отображение сетки напротив крупных рисок по оси X
            graph.GraphPane.XAxis.MajorGrid.IsVisible = true;
            // Задаем вид пунктирной линии для крупных рисок по оси X:
            // Длина штрихов равна 10 пикселям, ... 
            graph.GraphPane.XAxis.MajorGrid.DashOn = 10;
            // затем 5 пикселей - пропуск
            graph.GraphPane.XAxis.MajorGrid.DashOff = 5;
            // Включаем отображение сетки напротив крупных рисок по оси Y
            graph.GraphPane.YAxis.MajorGrid.IsVisible = true;
            // Аналогично задаем вид пунктирной линии для крупных рисок по оси Y
            graph.GraphPane.YAxis.MajorGrid.DashOn = 10;
            graph.GraphPane.YAxis.MajorGrid.DashOff = 5;
            // Включаем отображение сетки напротив мелких рисок по оси X
            graph.GraphPane.YAxis.MinorGrid.IsVisible = true;
            // Задаем вид пунктирной линии для крупных рисок по оси Y: 
            // Длина штрихов равна одному пикселю, ... 
            graph.GraphPane.YAxis.MinorGrid.DashOn = 1;
            // затем 2 пикселя - пропуск
            graph.GraphPane.YAxis.MinorGrid.DashOff = 2;
            // Включаем отображение сетки напротив мелких рисок по оси Y
            graph.GraphPane.XAxis.MinorGrid.IsVisible = true;
            // Аналогично задаем вид пунктирной линии для крупных рисок по оси Y
            graph.GraphPane.XAxis.MinorGrid.DashOn = 1;
            graph.GraphPane.XAxis.MinorGrid.DashOff = 2;

            graph.IsShowPointValues = true;

            if (_filtrErika != null)
            {
                SetNamesOfBoundsImpulsesToValue();
            }
            else
            {
                SetNamesOfBoundsImpulsesToEmptyStr();
            }
            // -----------------------------------------------------------------------------------
            
            // заголовки
            graph.GraphPane.Title.Text = string.Format("Датчик № {0} - {1}", _sensor.SensorNum, _parameter.ParameterName);
            graph.GraphPane.XAxis.Title.Text = "";
            graph.GraphPane.YAxis.Title.Text = "";
            // обозначение графика
            //if (unitsOfUserToolStripMenuItem.Checked)
                graph.AddInfoFor2Graphs(-40, -40, 0);
            //else
            //    graph.GraphCurve = graph.Draw1Curve(graph.DataList, graph.GraphColor, graph.GraphCurve);

            if (!graph.IsLineGraph)
                graph.GraphCurve.Line.IsVisible = false;
            else
                graph.GraphCurve.Line.IsVisible = true;
            //if (!graph.IsBigPoints)
            //    graph.GraphCurve.Symbol.Size = 2;
            //else
            //    graph.GraphCurve.Symbol.Size = 5;
            // заливка символов, которые рисует график
            graph.GraphCurve.Symbol.Fill = new Fill(Color.Black);
            // заливка фонов осей
            graph.GraphPane.Chart.Fill = new Fill(Color.White, Color.LightGoldenrodYellow, 45F);
            // заливка фонов
            graph.GraphPane.Fill = new Fill(Color.White, Color.FromArgb(220, 220, 255), 45F);
            // подсчет шкал
            graph.AxisChange();
            // задание шкалы времени           
            graph.GraphPane.XAxis.Type = AxisType.Date; 
            graph.GraphPane.XAxis.Scale.MinorUnit = DateUnit.Month;
            //graph.GraphPane.YAxis.Scale.Min = 0;

            //if (graph.DataList.Count > 0)
            //{
            //    double max = _data[0].Value1;
            //    for (int i = 0; i < _data.Count; ++i)
            //    {
            //        if (_data[i].Value1 > max)
            //            max = _data[i].Value1;
            //    }
            //    graph.GraphPane.YAxis.Scale.Max = max + 10;
            //}

            graph.ColorChanged += graph_ColorChanged;
            graph.PointChanged += graph_PointChanged;
            graph.LineChanged += graph_LineChanged;

            GraphPanel.Controls.Add(graph);
            graph.AxisChange();
            graph.Invalidate();
            /*-----------------------------------------------------------------------------------------------*/

            _isFilter = false;
            Cursor.Current = Cursors.Default;

            printDocument1 = new PrintDocument();
            printPreviewDialog1.Document = printDocument1;
            printDialog1.Document = printDocument1;
            
            printDocument1.PrintPage += printDocument1_PrintPage;

            CurrentValue = 0;

            if (impulsesToolStripMenuItem.Checked)
                SetYBounds(false);
            else
                SetYBounds(true);

            if (ProgramType.Type == ProgType.Client)
            {
                graphHoursToolStripMenuItem.Visible = false;
                graphBoundsToolStripMenuItem.Visible = false;
            }
        }

        void graph_LineChanged()
        {
            if (ProgramType.Type == ProgType.Server)
            {
                GraphWindow gw = GraphWindowsGateway.Instance.GetGraphWindowBySensorIdAndName(_sensor.Id, "pif");
                if (graph.IsLineGraph)
                    gw.IsLine = 1;
                else
                    gw.IsLine = 0;
                GraphWindowsGateway.Instance.UpdateGraphWindow(gw);
            }
        }

        void graph_PointChanged()
        {
            if (ProgramType.Type == ProgType.Server)
            {
                GraphWindow gw = GraphWindowsGateway.Instance.GetGraphWindowBySensorIdAndName(_sensor.Id, "pif");
                //if (graph.IsBigPoints)
                //    gw.PointSize = 5;
                //else
                //    gw.PointSize = 2;
                GraphWindowsGateway.Instance.UpdateGraphWindow(gw);
            }
        }

        void graph_ColorChanged()
        {
            if (ProgramType.Type == ProgType.Server)
            {
                GraphWindow gw = GraphWindowsGateway.Instance.GetGraphWindowBySensorIdAndName(_sensor.Id, "pif");
                gw.Color = graph.GraphColor.ToArgb();
                GraphWindowsGateway.Instance.UpdateGraphWindow(gw);
            }
        }

        /// <summary>
        /// Обновление значения от виртуального датчика
        /// </summary>
        /// <param name="value">значение</param>
        public void UpdateInfoForVirtual(Value value)
        {
            //DataRow row = dataTable.NewRow();

            //row[1] = value.TimeOfAdd;

            //double d = XDate.CalendarDateToXLDate(value.TimeOfAdd.Year, value.TimeOfAdd.Month, value.TimeOfAdd.Day,
            //    value.TimeOfAdd.Hour, value.TimeOfAdd.Minute, value.TimeOfAdd.Second);

            //value.Value1 = Math.Round(value.Value1,2);
            //if (_sensor.SensorNum[1] == '3' && (Protocol.Protocol1 == WhatProtocol.Zola || Protocol.Protocol1 == WhatProtocol.ZolaVlaga)) // если нагрузка
            //{
            //    if (_filtrErika.WeightKonveyrState != KonveyrStates.NoData && _filtrErika.WeightKonveyrState != KonveyrStates.MinMin && _filtrErika.WeightKonveyrState != KonveyrStates.KonveyrMaxError)
            //    {
            //        _dataUser.Add(d, value.Value1);
            //        FillSpline();
            //    }
            //}
            //else
            //{
            //    if (value.Value1 >= 0 )
            //    {
            //        _dataUser.Add(d, value.Value1);
            //        FillSpline();
            //    }
            //}

            //if (unitsOfUserToolStripMenuItem.Checked)
            //{
            //    if (value.Value1 >= 0 /*tmpValue != -777 && tmpValue != -111 && tmpValue != -333 && tmpValue != -77 && tmpValue != -55*/)
            //    {
            //        graph.Y = value.Value1;
            //        graph.X = d;
            //        row[0] = Math.Round(value.Value1,2);
            //        LastValueButton.Text = value.Value1.ToString();
            //        _beforeValue = value.Value1;
            //        //dataTable.Rows.Add(row);
            //    }
            //}
            //else if (splineToolStripMenuItem.Checked)
            //{
            //    if (value.Value1 >= 0 /*tmpValue != -777 && tmpValue != -111 && tmpValue != -333 && tmpValue != -77 && tmpValue != -55*/)
            //    {
            //        if (_dataSpline.Count > 0)
            //        {
            //            graph.Y = _dataSpline[_dataSpline.Count - 1].Y;
            //            graph.X = d;
            //            row[0] = Math.Round(_dataSpline[_dataSpline.Count - 1].Y,2);
            //            LastValueButton.Text = _dataSpline[_dataSpline.Count - 1].Y.ToString();
            //            _beforeValue = _dataSpline[_dataSpline.Count - 1].Y;
            //            //dataTable.Rows.Add(row);
            //        }
            //    }
            //}

            //dataGridView1.Update();
            UpdateInfo(value);
        }

        public void UpdateInfo(Value value)
        { // обновим график и дата грид при поступлении нового значения
            DataRow row = dataTable.NewRow();
            
            row[1] = value.TimeOfAdd;

            double d = XDate.CalendarDateToXLDate(value.TimeOfAdd.Year, value.TimeOfAdd.Month, value.TimeOfAdd.Day, 
                value.TimeOfAdd.Hour, value.TimeOfAdd.Minute, value.TimeOfAdd.Second);

            _dataImpulses.Add(d, value.Value1);
            //double tmpValue = _filtrErika.GetFilteredValue(value.Value1, _formula.Formula1);
            double tmpValue = value.Value1;
            if (_formula != null /*&& _formula.IsFilterUsing != 0*/ && _filtrErika != null)
                tmpValue = _filtrErika.GetFilteredValue(value.Value1, _formula);
            //tmpValue = Math.Round(tmpValue, 2);
            if (_sensor.SensorNum[1] == '3') // если нагрузка
            {
                if (_filtrErika.WeightKonveyrState == KonveyrStates.EmptyKonveyr)
                {
                    tmpValue = 0;
                    //_dataUser.Add(d, tmpValue);
                }
                if (_filtrErika.WeightKonveyrState != KonveyrStates.NoData && _filtrErika.WeightKonveyrState != KonveyrStates.MinMin && _filtrErika.WeightKonveyrState != KonveyrStates.KonveyrMaxError)
                {
                    _dataUser.Add(d, tmpValue);
                    _dataSpline = FillSpline(_dataUser, _sensor.SplineNumber, _dataSpline);
                    _dataSpline10 = FillSpline(_dataUser, _splineCount, _dataSpline10);
                }
            }
            else
            {
                if (tmpValue >= UniConstants.ErrorLevel /*tmpValue != -777 && tmpValue != -111 && tmpValue != -333 && tmpValue != -77 && tmpValue != -55*/)
                {
                    _dataUser.Add(d, tmpValue);
                    _dataSpline = FillSpline(_dataUser, _sensor.SplineNumber, _dataSpline);
                    _dataSpline10 = FillSpline(_dataUser, _splineCount, _dataSpline10);
                }
            }

            //TODO:разобраться с этим говном:
            //if (unitsOfUserToolStripMenuItem.Checked)
            //{
            //    if (tmpValue >= 0 /*tmpValue != -777 && tmpValue != -111 && tmpValue != -333 && tmpValue != -77 && tmpValue != -55*/)
            //    {
            //        graph.Y = tmpValue;
            //        graph.X = d;
            //        row[0] = Math.Round(tmpValue,2);
            //        LastValueButton.Text = tmpValue.ToString();
            //        _beforeValue = tmpValue;
            //        //dataTable.Rows.Add(row);
            //    }
            //}
            //else
            //{
            //    graph.Y = value.Value1;
            //    graph.X = d;
            //    row[0] = Math.Round(value.Value1,2);
            //    LastValueButton.Text = value.Value1.ToString();
            //    _beforeValue = value.Value1;
            //    //dataTable.Rows.Add(row);
            //}

            if (unitsOfUserToolStripMenuItem.Checked)
            {
                if (tmpValue >= UniConstants.ErrorLevel /*tmpValue != -777 && tmpValue != -111 && tmpValue != -333 && tmpValue != -77 && tmpValue != -55*/)
                {
                    row[0] = Math.Round(tmpValue,1);
                    LastValueButton.Text = tmpValue.ToString();
                    _beforeValue = tmpValue;
                    dataTable.Rows.Add(row);

                    double uv = -40;
                    if (_dataUser != null && _dataUser.Count > 0)
                        uv = _dataUser[_dataUser.Count - 1].Y;
                    double sv = -40;
                    if (_dataSpline10 != null && _dataSpline10.Count > 0)
                        sv = _dataSpline10[_dataSpline10.Count - 1].Y;

                    graph.AddInfoFor2Graphs(uv, sv, d);
                }
            }
            else if (impulsesToolStripMenuItem.Checked)
            {
                if (value.Value1 > UniConstants.ErrorLevel)
                {
                    //graph.Y = value.Value1;
                    //graph.X = d;
                    row[0] = Math.Round(tmpValue, 1);
                    LastValueButton.Text = tmpValue.ToString();
                    _beforeValue = tmpValue;
                    graph.AddInfoFor2Graphs(value.Value1, -40, d);
                    //dataTable.Rows.Add(row);
                }
            }
            else if (splineToolStripMenuItem.Checked)
            {
                if (tmpValue >= UniConstants.ErrorLevel /*tmpValue != -777 && tmpValue != -111 && tmpValue != -333 && tmpValue != -77 && tmpValue != -55*/)
                {
                    if (_dataSpline.Count > 0)
                    {
                        graph.Y = _dataSpline[_dataSpline.Count - 1].Y;
                        graph.X = d;
                        row[0] = Math.Round(_dataSpline[_dataSpline.Count - 1].Y,1);
                        LastValueButton.Text = _dataSpline[_dataSpline.Count - 1].Y.ToString();
                        _beforeValue = _dataSpline[_dataSpline.Count - 1].Y;
                        //dataTable.Rows.Add(row);
                    }
                }
            }
                    
            //dataGridView1.Rows[dataGridView1.Rows.Count - 1].Selected = true;
            dataGridView1.Update();

            //if (value.Value1 > _beforeValue + _beforeValue * 0.2 || value.Value1 < _beforeValue - 0.2 * _beforeValue)
            //{
            //    LastValueButton.BackColor = Color.Red;
            //}
            //else
            //{
            //    LastValueButton.BackColor = Color.White;
            //}
            
        }

        private void releaseObject(object obj)
        {//Освобождение ресурсов
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
            }
            catch
            {
            }
            finally
            {
                GC.Collect();
            }
        }

        private string GetNameOfMonth(int month)
        {
            if (month == 1)
                return "Январь";
            if (month == 2)
                return "Февраль";
            if (month == 3)
                return "Март";
            if (month == 4)
                return "Апрель";
            if (month == 5)
                return "Май";
            if (month == 6)
                return "Июнь";
            if (month == 7)
                return "Июль";
            if (month == 8)
                return "Август";
            if (month == 9)
                return "Сентябрь";
            if (month == 10)
                return "Октябрь";
            if (month == 11)
                return "Ноябрь";
            if (month == 12)
                return "Декабрь";

            return "";
        }

        private void SetACap(Worksheet ws)
        {
            if (Protocol.Protocol1 == WhatProtocol.Suwilka)
            {
                ws.Cells[1, 1] = "Поставщик :";
                ws.Cells[1, 3] = _supplier;
                ws.Cells[2, 1] = "потребитель :";
                ws.Cells[2, 3] = _material;
            }
            if (Protocol.Protocol1 == WhatProtocol.Zola || Protocol.Protocol1 == WhatProtocol.ZolaVlaga)
            {
                ws.Cells[1, 1] = "Потребитель :";
                ws.Cells[1, 3] = _supplier;
                ws.Cells[2, 1] = "№ удостоверения :";
                ws.Cells[2, 3] = _material;
            }
            ws.Cells[3, 1] = "Параметр :";
            ws.Cells[3, 3] = _parameter.ParameterName;
            ws.Cells[4, 1] = "Описание датчика :";
            ws.Cells[5, 1] = _sensor.SensorInfo;
            
            ws.Cells[6, 1] = "Культура :";
            if(_sensor != null)
            {
                CultureName cn = CultureNameGateway.Instance.GetCultureNameById(_sensor.CultureNameId);
                if(cn != null)
                    ws.Cells[6, 2] = cn.NameC;
            }
            ws.Cells[7, 1] = "Значения :";
        }

        private void PrintToExcel(Object StateInfo)
        {
            if (unitsOfUserToolStripMenuItem.Checked)
                ListForPrinting = GetFilteredList(_dataUser, false);
            else if (impulsesToolStripMenuItem.Checked)
                ListForPrinting = GetFilteredList(_dataImpulses, true);
            else if (splineToolStripMenuItem.Checked)
                ListForPrinting = GetFilteredList(_dataSpline, false);

            Cursor.Current = Cursors.WaitCursor;
            _Application xlApp = new ApplicationClass();
            Workbook xlWorkBook;
            Worksheet xlWorkSheet;
            object misValue = System.Reflection.Missing.Value;
            string filePath = @"C:\UniProgReport\ValuesExcel.xls";

            if (!Directory.Exists(@"UniProgReport"))
                Directory.CreateDirectory(@"UniProgReport");

            //Если не существует файла то создать его
            bool isFileExist;
            bool isOk = true;
            FileInfo fInfo = new FileInfo(filePath);

            if (!fInfo.Exists)
            {
                //File.Create(filePath);
                //xlWorkBook = xlApp.Workbooks.Add(misValue);//Добавить новый book в файл
                isFileExist = false;
            }
            else
            {
                //Открыть существующий файл
                //xlWorkBook = xlApp.Workbooks.Open(filePath, 0, false, 5, "", "", true,
                //    XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                //isFileExist = true;
                try
                {
                    fInfo.Delete();
                }
                catch (Exception ex)
                {
                    isOk = false;
                    MessageBox.Show(ex.Message);
                }
            }

            if (isOk)
            {
                isFileExist = false;
                xlWorkBook = xlApp.Workbooks.Add(misValue);//Добавить новый book в файл
                ((Worksheet)xlApp.ActiveWorkbook.Sheets[1]).Delete();
                ((Worksheet)xlApp.ActiveWorkbook.Sheets[1]).Delete();


                //при добавлении листа лист добавляет в начало
                //xlWorkSheet = (Worksheet)xlWorkBook.Worksheets.Add(Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                //xlWorkSheet = (Worksheet)xlWorkBook.Worksheets.Add(Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                //xlWorkSheet = (Worksheet)xlWorkBook.Worksheets.Add(Type.Missing, Type.Missing, Type.Missing, Type.Missing);

                //xlApp.Sheets.Add(misValue);
                TrySetProgrBarVisibility(true);

                //Открытие первой вкладки
                //xlWorkSheet = (Worksheet)xlWorkBook.Worksheets.get_Item(xlWorkBook.Worksheets.Count - 1);
                xlWorkSheet = (Worksheet)xlWorkBook.Sheets[1];
                //xlWorkSheet = (Excel.Worksheet)this.Application.Worksheets.Add();

                //xlApp.DisplayStatusBar = true;
                //xlApp.StatusBar = "Импорт данных";

                SetACap(xlWorkSheet);

                string vl;
                if (ListForPrinting != null && ListForPrinting.Count > 0)
                {
                    TrySetProgrBarMaxVal(ListForPrinting.Count);

                    int curMonth = ListForPrinting[0].TimeOfAdd.Month;
                    ((Worksheet)xlApp.Sheets[1]).Name = string.Format("{0} {1}", GetNameOfMonth(curMonth), ListForPrinting[0].TimeOfAdd.Year);

                    for (int i = 0,j=0; i < ListForPrinting.Count; ++i,++j)
                    {
                        if (curMonth != ListForPrinting[i].TimeOfAdd.Month)
                        {
                            curMonth = ListForPrinting[i].TimeOfAdd.Month;
                            xlWorkSheet = (Worksheet)xlWorkBook.Worksheets.Add(Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                            xlWorkSheet = (Worksheet)xlWorkBook.Sheets[1];
                            ((Worksheet)xlApp.Sheets[1]).Name = string.Format("{0} {1}", GetNameOfMonth(curMonth), ListForPrinting[0].TimeOfAdd.Year);
                            SetACap(xlWorkSheet);
                            j = 0;
                        }

                        xlWorkSheet.Cells[j + 8, 1] = ListForPrinting[i].Value1.ToString();
                        xlWorkSheet.Cells[j + 8, 3] = ListForPrinting[i].TimeOfAdd.ToLongDateString() + " "
                            + ListForPrinting[i].TimeOfAdd.ToLongTimeString();
                        // для того чтобы в Excel корректно сохранялась строка с точкой
                        vl = ListForPrinting[i].Value1.ToString();
                        vl = vl.Replace(',', '.');
                        if (vl.Contains("."))
                        {
                            vl += "00";
                        }
                        // значения с точкой а не с запятой, специально для облегчения выбора данных
                        xlWorkSheet.Cells[j + 8, 8] = vl;
                        ListForPrinting.RemoveAt(i);
                        i--;

                        TryUpdateProgressBarValue();
                    }
                }

                //Если файл существовал, просто сохранить его по умолчанию. Иначе сохранить в указанную директорию
                if (isFileExist)
                {
                    xlWorkBook.Save();
                }
                else
                {
                    xlWorkBook.SaveAs(filePath, XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue,
                        misValue, XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);
                }

                //сохраним рисунок графика
                graph.SaveAsJPEG();
                xlWorkSheet.Shapes.AddPicture(@"C:\UniProgReport\1.jpeg",
                    Microsoft.Office.Core.MsoTriState.msoFalse, Microsoft.Office.Core.MsoTriState.msoCTrue,
                    430, 50, 500, 400);


                //xlApp.DisplayStatusBar = false;
                xlWorkBook.Close(true, misValue, misValue);
                xlApp.Quit();
                //Освобождение ресурсов
                releaseObject(xlWorkSheet);
                releaseObject(xlWorkBook);
                releaseObject(xlApp);

                Cursor.Current = Cursors.Default;
                TrySetProgrBarVisibility(false);
                MessageBox.Show("Запись в файл произведена.");
            }
        }

        /// <summary>
        /// потокобезопасная обертка над добавлением значения в прогрес бар
        /// </summary>
        private delegate void TryUpdateProgressBarValueCallback();
        private void TryUpdateProgressBarValue()
        {
            if (progressBar1.InvokeRequired)
            {
                TryUpdateProgressBarValueCallback d = TryUpdateProgressBarValue;
                Invoke(d);
            }
            else
            {
                progressBar1.Value++;
            }
        }

        /// <summary>
        /// потокобезопасная обертка над установкой максимального значения прогрес бара
        /// </summary>
        private delegate void TrySetProgrBarVisibilityCallback(bool isVisible);
        private void TrySetProgrBarVisibility(bool isVisible)
        {
            if (progressBar1.InvokeRequired)
            {
                TrySetProgrBarVisibilityCallback d = new TrySetProgrBarVisibilityCallback(TrySetProgrBarVisibility);
                this.Invoke(d, new object[] { isVisible });
            }
            else
            {
                progressBar1.Visible = isVisible;
            }
        }

        /// <summary>
        /// потокобезопасная обертка над добавлением текста в ртб
        /// </summary>
        private delegate void TrySetProgrBarMaxValCallback(int setmaxVal);
        private void TrySetProgrBarMaxVal(int setmaxVal)
        {
            if (progressBar1.InvokeRequired)
            {
                TrySetProgrBarMaxValCallback d = new TrySetProgrBarMaxValCallback(TrySetProgrBarMaxVal);
                this.Invoke(d, new object[] { setmaxVal });
            }
            else
            {
                progressBar1.Maximum = setmaxVal;
            }
        }

        private void SaveToExelToolStripMenuItem_Click(object sender, EventArgs e)
        { // сохранение данных в Exel
            progressBar1.Value = 0;
            System.Threading.ThreadPool.QueueUserWorkItem(PrintToExcel);
        }

        private void PrintToolStripMenuItem_Click(object sender, EventArgs e)
        { // печать данных
            if (unitsOfUserToolStripMenuItem.Checked)
                ListForPrinting = GetFilteredList(_dataUser, false);
            if (impulsesToolStripMenuItem.Checked)
                ListForPrinting = GetFilteredList(_dataImpulses,true);
            printPreviewDialog1.ShowDialog();
            
            DialogResult result = printDialog1.ShowDialog();
            if(result == DialogResult.OK)
            {
                printDocument1.Print();
            }
        }

        private void chooseGraphHoursToolStripMenuItem_Click(object sender, EventArgs e)
        { // выбрать количество отображаемых часов на графике
            GraphHoursForm ghf = new GraphHoursForm(_sensor.GraphHour);
            if (ghf.ShowDialog() == DialogResult.OK)
            {
                _sensor.GraphHour = ghf.GraphHours;
                graph.ListCapacity = 60 * _sensor.GraphHour;
                SensorGateway.Instance.UpdateSensor(_sensor);
            }
        }

        private void ParameterInfoForm_LocationChanged(object sender, EventArgs e)
        {
            //this.Text = this.Location.X.ToString() + " " + this.Location.Y.ToString();

            if (WindowState != FormWindowState.Minimized && WindowState != FormWindowState.Maximized && !_isReport)
            {
                if (ProgramType.Type == ProgType.Server)
                {
                    GraphWindow gw = GraphWindowsGateway.Instance.GetGraphWindowBySensorIdAndName(_sensor.Id, "pif");
                    gw.Location = Location.X + " " + Location.Y;
                    GraphWindowsGateway.Instance.UpdateGraphWindow(gw);
                }
            }
        }

        private void ParameterInfoForm_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized && WindowState != FormWindowState.Maximized && !_isReport)
            {
                if (ProgramType.Type == ProgType.Server)
                {
                    GraphWindow gw = GraphWindowsGateway.Instance.GetGraphWindowBySensorIdAndName(_sensor.Id, "pif");
                    gw.Size = Size.Width + " " + Size.Height;
                    GraphWindowsGateway.Instance.UpdateGraphWindow(gw);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (!_isReport)
            {
                if (ProgramType.Type == ProgType.Server)
                {
                    GraphWindow gw = GraphWindowsGateway.Instance.GetGraphWindowBySensorIdAndName(_sensor.Id, "pif");
                    gw.IsWisible = 0;
                    GraphWindowsGateway.Instance.UpdateGraphWindow(gw);
                }
            }

            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (MessageBox.Show("Вы уверены что хотите закрыть это окно ?", "Выход", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                e.Cancel = false;
            else
                e.Cancel = true;
            base.OnClosing(e);
        }

        public void AddGraphTime()
        { // если дата графика вышла за рамки, подкорректируем ее - изменим макс и мин значения шкалы времени
            DateTime graphTime = XDate.XLDateToDateTime(graph.GraphPane.XAxis.Scale.Max);
            TimeSpan res = DateTime.Now - graphTime;
            if (/*res.Hours >= 1*/ res.Minutes >5 && res.Hours>0)
            {
                graph.GraphPane.XAxis.Scale.Max = new XDate(DateTime.Now);
                DateTime grphMin = XDate.XLDateToDateTime(graph.GraphPane.XAxis.Scale.Min);
                graphTime = XDate.XLDateToDateTime(graph.GraphPane.XAxis.Scale.Max);
                res = graphTime - grphMin;
                if (res.Hours > _sensor.GraphHour)
                {
                    graphTime = graphTime.AddHours(_sensor.GraphHour * -1);
                    graph.GraphPane.XAxis.Scale.Min = new XDate(graphTime);
                }
                graph.AxisChange();
                graph.Invalidate();
            }
        }

        #region Использование фильтра

        private void AddTableRow(PointPair pp)
        {
            DataRow row = dataTable.NewRow();

            row[0] = pp.Y;
            DateTime tmpDT = XDate.XLDateToDateTime(pp.X);
            row[1] = tmpDT;

            dataTable.Rows.Add(row);
        }
        private void SetNamesOfBoundsImpulsesToValue()
        {
            if (graph.UpperImpulseBound != null)
                graph.UpperImpulseBound.Label.Text = string.Format("Макс:{0}", _filtrErika.ZolaMaxImpulses);
            if (graph.UnderImpulseBound != null)
                graph.UnderImpulseBound.Label.Text = string.Format("Мин:{0}", _filtrErika.ZolaMinImpulses);
        }
        private void SetNamesOfBoundsImpulsesToEmptyStr()
        {
            if (graph.UpperImpulseBound != null)
                graph.UpperImpulseBound.Label.Text = "";
            if (graph.UnderImpulseBound != null)
                graph.UnderImpulseBound.Label.Text = "";
        }

        /// <summary>
        /// Установка графику автоматических границ(по макс значению фильтра)
        /// </summary>
        private void AutomaticalBounds()
        {
            if (_filtrErika != null)
            {
                double min = _filtrErika.ZolaMinImpulses - _filtrErika.ZolaMinImpulses * 0.2;
                if (min < 0)
                    min = 0;
                graph.GraphPane.YAxis.Scale.Min = min;
                if (_sensor.SensorNum[1] == '3' &&
                    (Protocol.Protocol1 == WhatProtocol.Zola || Protocol.Protocol1 == WhatProtocol.ZolaVlaga))
                    graph.GraphPane.YAxis.Scale.Max = _filtrErika.StandartOfImpulses +
                                                      _filtrErika.StandartOfImpulses * 0.3;
                else
                    graph.GraphPane.YAxis.Scale.Max = _filtrErika.ZolaMaxImpulses + _filtrErika.ZolaMaxImpulses * 0.2;

                if (_dataImpulses != null && _dataImpulses.Count > 2)
                {
                    graph.GraphPane.XAxis.Scale.Min = _dataImpulses[0].X;
                    graph.GraphPane.XAxis.Scale.Max = _dataImpulses[_dataImpulses.Count - 1].X;
                }
            }
        }

        private void SetYBounds(bool IsFilterUsing)
        { // границы график аоп оси У
            if (IsFilterUsing)
            { // выставить границы датчика
                if (_sensor.GraphUnderBound != -1 && _sensor.GraphUpperBound != -1)
                {
                    graph.GraphPane.YAxis.Scale.Min = _sensor.GraphUnderBound;
                    graph.GraphPane.YAxis.Scale.Max = _sensor.GraphUpperBound;
                }
            }
            else
            { // автоматические границы 
                AutomaticalBounds();
            }
            graph.AxisChange();
            graph.Invalidate();
        }

        public void ChangeFilter(int isUsing)
        { // вкл/выкл фильтра
            if (isUsing == 1)
            {
                unitsOfUserToolStripMenuItem.Checked = true;
                impulsesToolStripMenuItem.Checked = false;
                splineToolStripMenuItem.Checked = false;

                graphBoundsToolStripMenuItem.Enabled = true;

                graph.DataList.Clear();
                dataTable.Clear();
                foreach (PointPair pp in _dataUser)
                {
                    graph.DataList.Add(pp);
                    AddTableRow(pp);
                }
                foreach (PointPair pp in _dataSpline10)
                    graph.DataSplineList.Add(pp);

                ApplyGraphChanges(_dataUser);
                if (_dataUser.Count > 0)
                    LastValueButton.Text = _dataUser[_dataUser.Count - 1].Y.ToString();
            }
            else if (isUsing == 0)
            {
                impulsesToolStripMenuItem.Checked = true;
                unitsOfUserToolStripMenuItem.Checked = false;
                splineToolStripMenuItem.Checked = false;

                graphBoundsToolStripMenuItem.Enabled = false;

                graph.DataList.Clear();
                dataTable.Clear();
                foreach (PointPair pp in _dataImpulses)
                {
                    graph.DataList.Add(pp);
                    AddTableRow(pp);
                }

                ApplyGraphChanges(_dataImpulses);
                if (_dataImpulses.Count > 0)
                    LastValueButton.Text = _dataImpulses[_dataImpulses.Count - 1].Y.ToString();
            }
            else if (isUsing == 2)
            {
                impulsesToolStripMenuItem.Checked = false;
                unitsOfUserToolStripMenuItem.Checked = false;
                splineToolStripMenuItem.Checked = true;

                graphBoundsToolStripMenuItem.Enabled = true;

                graph.DataList.Clear();
                dataTable.Clear();
                foreach (PointPair pp in _dataSpline)
                {
                    graph.DataList.Add(pp);
                    AddTableRow(pp);
                }

                ApplyGraphChanges(_dataSpline);
                if (_dataSpline.Count > 0)
                    LastValueButton.Text = _dataSpline[_dataSpline.Count - 1].Y.ToString();
            }
        }

        private void ApplyGraphChanges(PointPairList pList)
        { // установка лимитов на значение параметров +-30% от макс/мин
            List<double> tmpList = new List<double>();
            foreach (PointPair pp in pList)
                tmpList.Add(pp.Y);
            tmpList.Sort();
            if (tmpList.Count > 0)
            {
                graph.GraphPane.YAxis.Scale.Max = tmpList[tmpList.Count - 1] + tmpList[tmpList.Count - 1] * 0.3;
                graph.GraphPane.YAxis.Scale.Min = tmpList[0] - tmpList[0] * 0.3;
            }
            graph.AxisChange();
            graph.Invalidate();
            dataGridView1.DataSource = dataTable;
            dataGridView1.Columns[1].Width = 175;
        }

        private void impulsesToolStripMenuItem_Click(object sender, EventArgs e)
        { // единицы графика - импульсы
            if (!impulsesToolStripMenuItem.Checked)
            {
                impulsesToolStripMenuItem.Checked = true;
                unitsOfUserToolStripMenuItem.Checked = false;
                splineToolStripMenuItem.Checked = false;

                graphBoundsToolStripMenuItem.Enabled = false;

                SetNamesOfBoundsImpulsesToValue();
                AddBoundsToImpulses();

                graph.DataList.Clear();
                dataTable.Clear();
                foreach (PointPair pp in _dataImpulses)
                {
                    graph.DataList.Add(pp);
                    AddTableRow(pp);
                }

                ApplyGraphChanges(_dataImpulses);

                if (_dataImpulses.Count > 0)
                    LastValueButton.Text = _dataImpulses[_dataImpulses.Count - 1].Y.ToString();

                SetYBounds(false);
            }
        }

        private void unitsOfUserToolStripMenuItem_Click(object sender, EventArgs e)
        { // единицы графика - единицы заказчика(проценты, тонны)
            if (!unitsOfUserToolStripMenuItem.Checked)
            {
                unitsOfUserToolStripMenuItem.Checked = true;
                impulsesToolStripMenuItem.Checked = false;
                splineToolStripMenuItem.Checked = false;

                graphBoundsToolStripMenuItem.Enabled = true;
                SetNamesOfBoundsImpulsesToEmptyStr();

                graph.DataList.Clear();
                dataTable.Clear();
                foreach (PointPair pp in _dataUser)
                {
                    graph.DataList.Add(pp);
                    AddTableRow(pp);
                }
                foreach (PointPair pp in _dataSpline10)
                    graph.DataSplineList.Add(pp);

                ApplyGraphChanges(_dataUser);
                
                if (_dataUser.Count > 0)
                    LastValueButton.Text = _dataUser[_dataUser.Count - 1].Y.ToString();

                SetYBounds(true);
            }
        }


        private void splineToolStripMenuItem_Click(object sender, EventArgs e)
        { // сплайн
            if (!splineToolStripMenuItem.Checked)
            {
                unitsOfUserToolStripMenuItem.Checked = false;
                impulsesToolStripMenuItem.Checked = false;
                splineToolStripMenuItem.Checked = true;

                graphBoundsToolStripMenuItem.Enabled = true;
                SetNamesOfBoundsImpulsesToEmptyStr();

                graph.DataList.Clear();
                dataTable.Clear();
                foreach (PointPair pp in _dataSpline)
                {
                    graph.DataList.Add(pp);
                    AddTableRow(pp);
                }

                ApplyGraphChanges(_dataSpline);

                if (_dataSpline.Count > 0)
                    LastValueButton.Text = _dataSpline[_dataSpline.Count - 1].Y.ToString();

                SetYBounds(true);
            }
        }
        #endregion

        private void graphBoundsToolStripMenuItem_Click(object sender, EventArgs e)
        { // выбор границ графика по У
            ChooseGraphBoundsForm cgbf = new ChooseGraphBoundsForm(_sensor);
            if (cgbf.ShowDialog() == DialogResult.OK)
            {
                if (cgbf.UnderBoundY != -1 && cgbf.UpperBoundY != -1)
                {
                    graph.GraphPane.YAxis.Scale.Min = cgbf.UnderBoundY;
                    graph.GraphPane.YAxis.Scale.Max = cgbf.UpperBoundY;
                }
                else
                {
                    AutomaticalBounds();
                }
                graph.AxisChange();
                graph.Invalidate();
            }
        }
    }
}