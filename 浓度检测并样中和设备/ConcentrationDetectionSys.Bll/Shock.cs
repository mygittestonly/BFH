using ConcentrationDetectionSys.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Bll
{
    /// <summary>
    /// 震荡
    /// </summary>
    public class Shock : BaseStep
    {
        public Shock() : base("振荡")
        {

        }
        public override void Execute(CancellationToken token)
        {
            // 振荡执行逻辑
            //if (CommonParam.s_DeviceState != DeviceState.Running)
            //{
            //    Thread.Sleep(200);
            //    continue;
            //}
        }
    }
}
