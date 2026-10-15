using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Common
{
    public class  CapacityClass
    {
        public int DoneNum_CN
        {
            get
            {
                return DoneOKNum_CN + DoneNGNum_CN;
            }
        }

        public int DoneOKNum_CN { get; set; }
        public int DoneNGNum_CN { get; set; }
        public double Yield 
        {
            get
            {
                if (DoneNum_CN == 0)
                    return 0;
                else
                    return DoneOKNum_CN / (double)(DoneNum_CN) * 100;
            }
        }
        public double AbnormalityRate
        {
            get
            {
                if (DoneNum_CN == 0)
                    return 0;
                else
                    return DoneNGNum_CN / (double)(DoneNum_CN) * 100;
            }
        }
        public int DoneNum_BY { get; set; }
        public int DoneNum_ZH { get; set; }
    }
}
