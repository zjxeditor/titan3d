﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CodeDomNode.Time
{
    [EngineNS.Rtti.MetaClass]
    public class CosTimeConstructParam : CodeGenerateSystem.Base.ConstructionParams
    {

    }
    [CodeGenerateSystem.CustomConstructionParams(typeof(CosTimeConstructParam))]
    public partial class CosTime : CodeGenerateSystem.Base.BaseNodeControl
    {
        CodeGenerateSystem.Base.LinkPinControl mCtrlValueLinkHandle = new CodeGenerateSystem.Base.LinkPinControl();

        partial void InitConstruction();
        public CosTime(CodeGenerateSystem.Base.ConstructionParams csParam)
            : base(csParam)
        {
            InitConstruction();

            IsOnlyReturnValue = true;
            AddLinkPinInfo("CosTime_CtrlValueLinkHandle", mCtrlValueLinkHandle, null);
        }

        public static void InitNodePinTypes(CodeGenerateSystem.Base.ConstructionParams smParam)
        {
            CollectLinkPinInfo(smParam, "CosTime_CtrlValueLinkHandle", CodeGenerateSystem.Base.enLinkType.Single, CodeGenerateSystem.Base.enBezierType.Right, CodeGenerateSystem.Base.enLinkOpType.Start, true);
        }

        public override string GCode_GetTypeString(CodeGenerateSystem.Base.LinkPinControl element, CodeGenerateSystem.Base.GenerateCodeContext_Method context)
        {
            return "System.Single";
        }

        public override Type GCode_GetType(CodeGenerateSystem.Base.LinkPinControl element, CodeGenerateSystem.Base.GenerateCodeContext_Method context)
        {
            return typeof(System.Single);
        }

        public override System.CodeDom.CodeExpression GCode_CodeDom_GetValue(CodeGenerateSystem.Base.LinkPinControl element, CodeGenerateSystem.Base.GenerateCodeContext_Method context, CodeGenerateSystem.Base.GenerateCodeContext_PreNode preNodeContext = null)
        {
            return new System.CodeDom.CodeSnippetExpression("((float)System.Math.Cos((EngineNS.CEngine.Instance.EngineTimeSecond)*2*System.Math.PI))");
        }
    }
}
