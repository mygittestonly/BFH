using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConcentrationDetectionSys.Bll
{
    public abstract class BaseStep
    {
        /// <summary>模块类型名称：称重 / 中和 / 振荡...</summary>
        [Category("参数")]  
        [DisplayName("模块类型名称")]
        public string StepTypeName { get; }
        //构造函数赋值
        public BaseStep(string stepTypeName)
        {
            StepTypeName = stepTypeName;
        }

        // ========= 核心：每个模块自己实现执行逻辑 =========
        public abstract void Execute(CancellationToken token);
    }

   
}
