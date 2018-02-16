
using System.Collections.Generic;
using MBLL;
using System.Data.SqlClient;
using System.Data;

namespace MDAL
{
    public class MaterialGateway : DataBaseGateway
    { // взаимодействие с таблицей материалов
        private static MaterialGateway materialInstance = null;
        public new static MaterialGateway Instance
        {
            get {
                if (materialInstance == null)
                    materialInstance = new MaterialGateway();
                return materialInstance; }
        }
        protected MaterialGateway()
        {
            ConnectionString = DataBaseGateway.Instance.ConnectionString;
            Connection = new SqlConnection(ConnectionString);
        }
        //___________________________________________________________________//

        public void AddMaterial(Material m)
        {
            cmd = new SqlCommand("AddMaterial", Connection);
            cmd.Parameters.AddWithValue("@MaterialName",         m.MaterialName);
            cmd.Parameters.AddWithValue("@MiddleTime",           m.MiddleTime);
            cmd.Parameters.AddWithValue("@SupplierId",           m.SupplierId);
            ExecuteSQLCommand("Невозможно добавить потребителя");
        }

        public List<Material> GetMaterialBySupplierId(int id)
        {
            List<Material> list = new List<Material>();
            // комманда под эскюэль
            cmd = new SqlCommand("GetMatBySupp", Connection);
            cmd.Parameters.AddWithValue("@Supplier", id);
            cmd.CommandType = CommandType.StoredProcedure;
            if (Connection.State == ConnectionState.Closed)
                Connection.Open();
            try
            {
                rdr = cmd.ExecuteReader();
            }
            catch
            {
                Connection.Close();
                rdr.Close();
                return null;
            }
            while (rdr.Read())
            {
                Material m = new Material();
                // перепишем значения в экземпляр нашего списка
                m.Id                = rdr.GetInt32(0);
                m.MaterialName      = rdr.GetString(1);
                m.MiddleTime        = rdr.GetInt32(2);
                m.SupplierId        = rdr.GetInt32(3);

                list.Add(m);
            }
            Connection.Close();

            rdr.Close();

            // список 
            return list;
        }

        public List<Material> GetMaterial()
        {
            List<Material> list = new List<Material>();
            // комманда под эскюэль
            cmd = new SqlCommand("GetMaterial", Connection);
            cmd.CommandType = CommandType.StoredProcedure;
            if (Connection.State == ConnectionState.Closed)
                Connection.Open();
            rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                Material m = new Material();
                // перепишем значения в экземпляр нашего списка
                m.Id                = rdr.GetInt32(0);
                m.MaterialName      = rdr.GetString(1);
                m.MiddleTime        = rdr.GetInt32(2);
                m.SupplierId        = rdr.GetInt32(3);

                list.Add(m);
            }
            Connection.Close();

            rdr.Close();

            // список 
            return list;
        }

        public SqlCommand GetMaterialForUser()
        { // команда для презентабельного отображения информации о материалах
            cmd = new SqlCommand("GetMaterialForUser", Connection);
            cmd.CommandType = CommandType.StoredProcedure;

            return cmd;
        }

        public Material GetMaterialById(int id)
        {
            cmd = new SqlCommand("GetMaterialById", Connection);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.CommandType = CommandType.StoredProcedure;
            if (Connection.State == ConnectionState.Closed)
                Connection.Open();
            rdr = cmd.ExecuteReader();
            try
            {
                rdr.Read();
            } // если ничего нет
            catch
            {
                Connection.Close();
                rdr.Close(); 
                return null;
            }
            Material m = new Material();

            try
            {
                m.Id = rdr.GetInt32(0);
                m.MaterialName = rdr.GetString(1);
                m.MiddleTime = rdr.GetInt32(2);
                m.SupplierId = rdr.GetInt32(3);
            }
            catch
            {
                Connection.Close();
                rdr.Close();
                return null;
            }

            Connection.Close();

            rdr.Close();

            return m;
        }

        public Material GetMaterialByName(string name)
        {
            cmd = new SqlCommand("GetMaterialByName", Connection);
            cmd.Parameters.AddWithValue("@MaterialName", name);
            cmd.CommandType = CommandType.StoredProcedure;
            if (Connection.State == ConnectionState.Closed)
                Connection.Open();
            rdr = cmd.ExecuteReader();
            try
            {
                rdr.Read();
            } // если ничего нет
            catch
            {
                Connection.Close();
                rdr.Close(); 
                return null;
            }
            Material m = new Material();

            try
            {
                m.Id = rdr.GetInt32(0);
                m.MaterialName = rdr.GetString(1);
                m.MiddleTime = rdr.GetInt32(2);
                m.SupplierId = rdr.GetInt32(3);
            }
            catch
            {
                Connection.Close();
                rdr.Close();

                return null;
            }

            Connection.Close();

            rdr.Close();

            return m;
        }

        public void UpdateMaterial(Material m)
        {
            int Id = m.Id;
            Material mTmp = GetMaterialById(Id);
            if (mTmp != null)
            {
                // апдейт записи
                cmd = new SqlCommand("UpdateMaterial", Connection);
                cmd.Parameters.AddWithValue("@Id",                   m.Id);
                cmd.Parameters.AddWithValue("@MaterialName",         m.MaterialName);
                cmd.Parameters.AddWithValue("@MiddleTime",           m.MiddleTime);
                cmd.Parameters.AddWithValue("@SupplierId",           m.SupplierId);
                ExecuteSQLCommand("Невозможно редактировать информацию данного потребителя");
            }
        }
        public void DelMaterial(int id)
        {
            Material mTmp = GetMaterialById(id);
            if (mTmp != null)
            {
                ValuesGateway.Instance.DelValueByMaterialId(id);

                cmd = new SqlCommand("DelMaterial", Connection);
                cmd.Parameters.AddWithValue("@Id", mTmp.Id);
                ExecuteSQLCommand("Невозможно удалить информацию данного потребителя");
            }
         
        }
        public void DelMaterialBySupplierId(int id)
        {
            // сначала почистим хвосты у материалов
            List<Material> mList = GetMaterialBySupplierId(id);
            if (mList != null && mList.Count > 0)
            {
                for (int i = 0; i < mList.Count; ++i)
                {
                    ValuesGateway.Instance.DelValueByMaterialId(mList[i].Id);
                }
            }

            // а теперь удалим и сами материалы
            cmd = new SqlCommand("DelMatBySyppId", Connection);
            cmd.Parameters.AddWithValue("@Id", id);
            ExecuteSQLCommand("Невозможно удалить информацию данного потребителя");
        }
    }
}
