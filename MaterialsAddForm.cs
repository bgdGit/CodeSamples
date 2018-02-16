using System;
using System.Collections.Generic;
using MDAL;
using MBLL;

namespace WatchOnRawMaterial
{
    public partial class MaterialsAddForm : BaseForm
    {
        private Material _m;
        private bool _isAdd; // флаг для того чтобы узнать, добавляем мы или апдейтим

        private List<Material> _matList;

        public MaterialsAddForm()
        {
            InitializeComponent();
            _isAdd = true;
            _m = new Material();
        }

        public MaterialsAddForm(Material m)
        { // конструктор для случая добавления
            InitializeComponent();
            _isAdd = false;
            _m = m;
            this.Text = "Редактируем существующего потребителя";
        }        

        private void MaterialsAddForm_Load(object sender, EventArgs e)
        {
            comboBox1.DataSource = SupplierGateway.Instance.GetSuppliers();
            comboBox1.DisplayMember = "SupplierName";
            comboBox1.ValueMember = "Id";

            if (_isAdd)
            {
                textBox1.Enabled = false;
                numericUpDown2.Enabled = false;

                textBox1.Text = "";
            }
            else
            {
                comboBox1.SelectedValue = _m.SupplierId;

                textBox1.Text = _m.MaterialName;
                numericUpDown2.Value = _m.MiddleTime;
            }

            button1.Enabled = false;

            comboBox1.SelectedIndexChanged += new EventHandler(comboBox1_SelectedIndexChanged);
            textBox1.TextChanged += new EventHandler(Durak);
            numericUpDown2.ValueChanged += new EventHandler(Durak);

            _matList = new List<Material>();
            _matList = MaterialGateway.Instance.GetMaterial();
        }

        void Durak(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && numericUpDown2.Value > 0)
            {
                bool flag = false;
                for (int i = 0; i < _matList.Count; ++i)
                {
                    if (textBox1.Text == _matList[i].MaterialName)
                    {
                        flag = true;
                    }
                }
                if (!flag)
                    button1.Enabled = true;
            }
            else
                button1.Enabled = false;
        }

        void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = true;
            numericUpDown2.Enabled = true;
            button1.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _m.MaterialName = textBox1.Text;
            _m.MiddleTime = (int)numericUpDown2.Value;
            _m.SupplierId = (int)comboBox1.SelectedValue;

            if (_isAdd)
            {
                MaterialGateway.Instance.AddMaterial(_m);
            }
            else
            {
                MaterialGateway.Instance.UpdateMaterial(_m);
            }
        }
    }
}