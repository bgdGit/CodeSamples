using System;
using System.Collections.Generic;
using System.Text;

namespace MBLL
{
    public class Material : Entity
    { // описание материала
        private string _materialName;
        public string MaterialName
        {
            get { return _materialName; }
            set { _materialName = value; }
        }

        private int _middleTime;
        public int MiddleTime
        {
            get { return _middleTime; }
            set { _middleTime = value; }
        }

        private int _supplierId;
        public int SupplierId
        {
            get { return _supplierId; }
            set { _supplierId = value; }
        }
        public Material()
        {}

        public Material (string materialName, int middleTime, int supplierId)
        {
            _materialName = materialName;
            _middleTime = middleTime;
            _supplierId = supplierId;
        }
    }
}
