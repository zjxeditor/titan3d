﻿using CodeGenerateSystem.Base;
using EngineNS.IO;
using Macross;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace CodeDomNode.Particle
{
  
    public class ParticleStateControlConstructionParams : StructNodeControlConstructionParams
    {
        [EngineNS.Rtti.MetaData]
        public override string BaseClassName { get; set; } = "EngineNS.Bricks.Particle.ParticleStateNode";
        [EngineNS.Rtti.MetaData]
        public Type CreateType { get; set; } = typeof(EngineNS.Bricks.Particle.ParticleStateNode.ParticleStateData);//测试Box
        public override EditorCommon.CodeGenerateSystem.INodeConstructionParams Duplicate()
        {
            var retVal = base.Duplicate() as ParticleStateControlConstructionParams;
            return retVal;
        }

    }
    [CodeGenerateSystem.CustomConstructionParamsAttribute(typeof(ParticleStateControlConstructionParams))]
    public partial class ParticleStateControl : CodeGenerateSystem.Base.BaseNodeControl, IParticleNode, IParticleSaveData, IParticleGradient
    {
        StructLinkControl mCtrlValueLinkHandleDown = new StructLinkControl();
        StructLinkControl mCtrlValueLinkHandleUp = new StructLinkControl();
        CreateObject createObject;

        partial void InitConstruction();
        public ParticleStateControl(ParticleStateControlConstructionParams csParam)
            : base(csParam)
        {
            InitConstruction();

               NodeName = csParam.NodeName;

            //csParam.SetHostNodesContainerEvent -= InitGraphEvent;
            //csParam.SetHostNodesContainerEvent += InitGraphEvent;
            //InitGraphEvent();
            if (string.IsNullOrEmpty(NodeName))
            {
                NodeName = "ParticleState";
            }

            IsOnlyReturnValue = true;
            AddLinkPinInfo("ParticleStateControlDown", mCtrlValueLinkHandleDown, null);
            AddLinkPinInfo("ParticleStateControlUp", mCtrlValueLinkHandleUp, null);

            mCtrlValueLinkHandleDown.ResetDefaultFilter();
            mCtrlValueLinkHandleDown.ResetDefaultFilterByShape("ParticleStateControlUp");
            CreateObject.CreateObjectConstructionParams createobjparam = new CreateObject.CreateObjectConstructionParams();
            createobjparam.CreateType = csParam.CreateType;
            createObject = new CreateObject(createobjparam);
            createObject.CreateTemplateClas();

            createObject.SetPropertyChangedEvent(OnPropertyChanged);
        }

        #region IParticleGradient
        public Object GetShowGradient()
        {
            return createObject.GetShowPropertyObject();
        }

        Object ParticleNode;
        public void SyncValues(EngineNS.Bricks.Particle.CGfxParticleSystem sys)
        {
            if (ParticleNode == null)
            {
                return;
            }

            if (sys.SubParticleSystems != null && sys.SubParticleSystems.Count != 0)
            {
                for (int i = 0; i < sys.SubParticleSystems.Count; i++)
                {
                    var subsys = sys.SubParticleSystems[i];
                    if (subsys.SubStates != null)
                    {
                        for (int j = 0; j < subsys.SubStates.Length; j++)
                        {
                            var node = subsys.SubStates[j].MacrossParticleStateNode;
                            if (node != null)
                            {
                                if (node.GetType().Name.Equals(GetClassName()))
                                {
                                    subsys.SubStates[j].MacrossParticleStateNode.Data = ParticleNode as EngineNS.Bricks.Particle.ParticleStateNode.ParticleStateData;
                                    return;
                                }
                            }
                        }
                    }

                }

            }
        }
        public void SetPGradient(EngineNS.Bricks.Particle.CGfxParticleSystem sys)
        {
            ParticleNode = null;

            if (sys.SubParticleSystems != null && sys.SubParticleSystems.Count != 0)
            {
                for (int i = 0; i < sys.SubParticleSystems.Count; i++)
                {
                    var subsys = sys.SubParticleSystems[i];
                    if (subsys.SubStates != null)
                    {
                        for (int j = 0; j < subsys.SubStates.Length; j++)
                        {
                            var node = subsys.SubStates[j].MacrossParticleStateNode;
                            if (node != null)
                            {
                                if (node.GetType().Name.Equals(GetClassName()))
                                {
                                    ParticleNode = node.Data;
                                    break;
                                }
                            }
                        }
                        if (ParticleNode != null)
                            break;
                    }
                   
                }

            }
            
        }

        List<string> PropertyNames = new List<string>();
        public void OnPropertyChanged(string propertyName, object newValue, object oldValue)
        {
            var createobj = GetCreateObject();
            if (ParticleNode == null || createobj == null)
            {
                return;
            }

            var srcProInfo = ParticleNode.GetType().GetProperty(propertyName);
            if (srcProInfo != null)
            {
                srcProInfo.SetValue(ParticleNode, newValue);
            }
        }

        #endregion

        public string GetClassName()
        {
            return "ParticleState_" + Id.ToString().Replace("-", "_");
        }
        public StructLinkControl GetLinkControlUp()
        {
            return mCtrlValueLinkHandleUp;
        }
        public CreateObject GetCreateObject()
        {
            return createObject;
        }
        public override object GetShowPropertyObject()
        {
            return this;
        }



        #region IParticleSaveData
        bool mIsLoadLink = false;
        public bool IsLoadLink()
        {
            return mIsLoadLink;
        }

        public void SetLoadLink(bool value)
        {
            mIsLoadLink = value;
        }

        XndNode XndNode;
        public void SetXndNode(XndNode data)
        {
            XndNode = data;
        }

        public XndNode GetXndNode()
        {
            return XndNode;
        }

        string XndAttribName;
        public void SetXndAttribName(string name)
        {
            XndAttribName = name;
        }

        public string GetXndAttribName()
        {
            return XndAttribName;
        }

        #endregion

        public async System.Threading.Tasks.Task InitGraphEvent()
        {
            await InitGraph();
            await ParticleDataSaveLoad.LoadData(CSParam as StructNodeControlConstructionParams, mLinkedNodesContainer, this);
        }

        bool NeedInitGrapth = true;
        //初始化每个结点类中的元素
        public async System.Threading.Tasks.Task InitGraph()
        {
            if (this.HostNodesContainer == null || mLinkedNodesContainer != null)
                return;

            if (NeedInitGrapth == false)
                return;

            NeedInitGrapth = false;


            var assist = this.HostNodesContainer.HostControl;
            if (string.IsNullOrEmpty(HostNodesContainer.TitleString))
            {
                HostNodesContainer.TitleString = "MainGraph";
            }

            var TitleString = HostNodesContainer.TitleString;

            var title = TitleString + "/" + NodeName + ":" + this.Id.ToString();

            var data = new SubNodesContainerData()
            {
                ID = Id,
                Title = title,
            };
            mLinkedNodesContainer = await assist.GetSubNodesContainer(data);

            //TODO..
            Macross.NodesControlAssist NodesControlAssist = mLinkedNodesContainer.HostControl as Macross.NodesControlAssist;
            var MacrossOpPanel = NodesControlAssist.HostControl.MacrossOpPanel;

            var names = new string[] { MacrossPanelBase.GraphCategoryName, MacrossPanelBase.FunctionCategoryName, MacrossPanelBase.VariableCategoryName, MacrossPanelBase.AttributeCategoryName };

            var csparam = CSParam as StructNodeControlConstructionParams;
            csparam.CategoryDic = new Dictionary<string, Category>();
            foreach (var name in names)
            {
                var category1 = new Category(MacrossOpPanel);
                category1.CategoryName = name;
                csparam.CategoryDic.Add(name, category1);
                //categoryPanel.Children.Add(category);
            }
            foreach (var category1 in csparam.CategoryDic)
            {
                category1.Value.OnSelectedItemChanged = (categoryName) =>
                {
                    foreach (var cName in names)
                    {
                        if (cName == categoryName)
                            continue;

                        Category ctg;
                        if (csparam.CategoryDic.TryGetValue(cName, out ctg))
                        {
                            ctg.UnSelectAllItems();
                        }
                    }
                };
            }

            if (data.IsCreated)
            {
                await InitializeLinkedNodesContainer();
            }
            mLinkedNodesContainer.HostNode = this;

            //ParticleDataSaveLoad.ResetNodeConrol(NeedResetLoadValue, mLinkedNodesContainer, csparam);
            EngineNS.Thread.Async.TaskLoader.Release(ref WaitContext, this);
        }

        public IParticleShape FindParticleShapeNode()
        {
            if (mCtrlValueLinkHandleUp.HasLink == false)
            {
                return null;
            }

            LinkInfo linkinfo = mCtrlValueLinkHandleUp.GetLinkInfo(0);
            if (linkinfo.m_linkFromObjectInfo == null || linkinfo.m_linkFromObjectInfo.HostNodeControl == null)
                return null;

            var basenodecontrol = linkinfo.m_linkFromObjectInfo.HostNodeControl;
            while (basenodecontrol != null)
            {
                if ((basenodecontrol as IParticleShape) != null)
                {
                    return basenodecontrol as IParticleShape;
                }

                IParticleNode particlenode = basenodecontrol as IParticleNode;
                if (particlenode == null)
                {
                    return null;
                }

                var linkcontrol = particlenode.GetLinkControlUp();
                if (linkcontrol == null)
                {
                    return null;
                }

                if (linkcontrol.HasLink == false)
                {
                    return null;
                }

                linkinfo = linkcontrol.GetLinkInfo(0);
                if (linkinfo.m_linkFromObjectInfo == null)
                    return null;


                basenodecontrol = linkinfo.m_linkFromObjectInfo.HostNodeControl;
            }
            return null;
        }

        string[] ParticleSubStateTemplate =
        {
            "Init",
            "Update",
        };
        public override void MouseLeftButtonDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EngineNS.CEngine.Instance.EventPoster.RunOn(async () =>
            {
                await InitGraphEvent();
                await OpenGraph();
                return true;
            }, EngineNS.Thread.Async.EAsyncTarget.Editor);

        }

        protected override void EndLink(LinkPinControl linkObj)
        {
            if (linkObj == null)
                return;

            bool alreadyLink = false;
            var pinInfo = GetLinkPinInfo("ParticleStateControlDown");
            if (HostNodesContainer.StartLinkObj == linkObj)
            {
                base.EndLink(null);
                if (HostNodesContainer.PreviewLinkCurve != null)
                    HostNodesContainer.PreviewLinkCurve.Visibility = System.Windows.Visibility.Hidden;
                return;
            }
            if (pinInfo == null)
                return;

            if (CodeGenerateSystem.Base.LinkInfo.CanLinkWith(HostNodesContainer.StartLinkObj, linkObj) && 
                ModuleLinkInfo.CanLinkWith2(HostNodesContainer.StartLinkObj, linkObj))
            {
                //链接之前删除linkObj其他链接
                if (linkObj.LinkOpType == enLinkOpType.End)
                {
                    if (linkObj.LinkInfos.Count > 0)
                    {
                        //linkObj.LinkInfos[0].m_linkFromObjectInfo.RemoveLink(linkObj.LinkInfos[0]);
                        //linkObj.LinkInfos[0].m_linkToObjectInfo.RemoveLink(linkObj.LinkInfos[0]);
                        linkObj.LinkInfos[0].Clear();
                        linkObj.LinkInfos.Clear();
                    }
                }
                var container = new NodesContainer.LinkInfoContainer();
                //if (mStartLinkObj.LinkOpType == enLinkOpType.Start)
                //{
                    container.Start = HostNodesContainer.StartLinkObj;
                    container.End = linkObj;
                //}
                //else
                //{
                //    container.Start = objInfo;
                //    container.End = mStartLinkObj;
                //}
                HostNodesContainer.IsOpenContextMenu = false;
                var redoAction = new Action<Object>((obj) =>
                {
                    var linkInfo = new ModuleLinkInfo(HostNodesContainer.ContainerDrawCanvas, container.Start, container.End);
                });
                redoAction.Invoke(null);
                //EditorCommon.UndoRedo.UndoRedoManager.Instance.AddCommand(HostControl.UndoRedoKey, null, redoAction, null,
                //                            (obj) =>
                //                            {
                //                                for (int i = 0; i < container.End.GetLinkInfosCount(); i++)
                //                                {
                //                                    var info = container.End.GetLinkInfo(i);
                //                                    if (info.m_linkFromObjectInfo == container.Start)
                //                                    {
                //                                        info.Clear();
                //                                        break;
                //                                    }
                //                                }
                //                            }, "Create Link");
                IsDirty = true;
            }

            var count = pinInfo.GetLinkInfosCount();
            for (int index = 0; index < count; ++index)
            {
                //AnimStateLinkInfoForUndoRedo undoRedoLinkInfo = new AnimStateLinkInfoForUndoRedo();
                //var linkInfo = pinInfo.GetLinkInfo(index);
                //if (linkInfo.m_linkFromObjectInfo == HostNodesContainer.StartLinkObj && linkInfo.m_linkToObjectInfo == linkObj)
                //{
                //    alreadyLink = true;
                //    undoRedoLinkInfo.linkInfo = linkInfo as AnimStateLinkInfo;
                //    NodesContainer.TransitionStaeBaseNodeForUndoRedo transCtrl = new NodesContainer.TransitionStaeBaseNodeForUndoRedo();
                //    var redoAction = new Action<Object>((obj) =>
                //    {
                //        transCtrl.TransitionStateNode = undoRedoLinkInfo.linkInfo.AddTransition();
                //    });
                //    redoAction.Invoke(null);
                //    EditorCommon.UndoRedo.UndoRedoManager.Instance.AddCommand(HostNodesContainer.HostControl.UndoRedoKey, null, redoAction, null,
                //                                (obj) =>
                //                                {
                //                                    undoRedoLinkInfo.linkInfo.RemoveTransition(transCtrl.TransitionStateNode);

                //                                }, "Create StateTransition");
                //}

                //if (HostNodesContainer.PreviewLinkCurve != null)
                //    HostNodesContainer.PreviewLinkCurve.Visibility = System.Windows.Visibility.Hidden;
                //base.EndLink(null,false);

            }
            //if (!alreadyLink)
            //    base.EndLink(linkObj);
        }

       
        async System.Threading.Tasks.Task OpenGraph()
        {
            var param = CSParam as ParticleStateControlConstructionParams;
            var assist = this.HostNodesContainer.HostControl;
            var TitleString = HostNodesContainer.TitleString;
            if (string.IsNullOrEmpty(TitleString))
            {
                TitleString = "MainGraph";
            }
            var title = TitleString + "/" + NodeName + ":" + this.Id.ToString();
            var data = new SubNodesContainerData()
            {
                ID = Id,
                Title = title,
            };
            mLinkedNodesContainer = await assist.ShowSubNodesContainer(data);
            if (data.IsCreated)
            {
                //await InitializeLinkedNodesContainer();
            }
            mLinkedNodesContainer.HostNode = this;
            //mLinkedNodesContainer.OnFilterContextMenu = StateControl_FilterContextMenu;
        }

        private void CreateParticleMethodCategory(string methodname, float x, float y)
        {
            Macross.NodesControlAssist NodesControlAssist = mLinkedNodesContainer.HostControl as Macross.NodesControlAssist;

            //加入列表信息
            Macross.Category category;
            var csparam = CSParam as StructNodeControlConstructionParams;
            if (!csparam.CategoryDic.TryGetValue(Macross.MacrossPanelBase.GraphCategoryName, out category))
                return;

            var HostControl = this.HostNodesContainer.HostControl;

            var item = new Macross.CategoryItem(null, category);
            item.CategoryItemType = Macross.CategoryItem.enCategoryItemType.OverrideFunction;
            var data = new Macross.CategoryItem.InitializeData();
            data.Reset();

            var MacrossOpPanel = NodesControlAssist.HostControl.MacrossOpPanel;
            item.Initialize(MacrossOpPanel.HostControl, data);
            //HostControl.CreateNodesContainer(item);

            //MainGridItem.Children.Add(item);
            item.Name = methodname;
            category.Items.Add(item);

            //加入结点信息 TODO..
            Type type = typeof(EngineNS.Bricks.Particle.ParticleStateNode);

            System.Reflection.MethodInfo methodInfo = type.GetMethod(methodname);
            System.Reflection.ParameterInfo[] paramstype = methodInfo.GetParameters();

            var itemPro = new CodeDomNode.CustomMethodInfo();
            //itemPro.AddOutParamOperation(item);
            itemPro.CSType = EngineNS.ECSType.Client;

            itemPro.MethodName = methodname + "_" + NodeName;//"DoParticleSubStateBorn_" + MainGridItem.Name;
            
            //var CustomFunctionData = new Macross.ResourceInfos.MacrossResourceInfo.CustomFunctionData();
            var methodinfo = CodeDomNode.Program.GetMethodInfoAssistFromMethodInfo(methodInfo, type, CodeDomNode.MethodInfoAssist.enHostType.Base, "");
            var nodeType = typeof(CodeDomNode.MethodOverride);
            var csParam = new CodeDomNode.MethodOverride.MethodOverrideConstructParam()
            {
                CSType = mLinkedNodesContainer.CSType,
                HostNodesContainer = mLinkedNodesContainer,
                ConstructParam = "",
                MethodInfo = methodinfo,
            };

            //var center = nodesContainer.NodesControl.GetViewCenter();
            var node = mLinkedNodesContainer.AddOrigionNode(nodeType, csParam, x, y);

            node.IsDeleteable = true;

            //重写双击事件 不需要进入二级编辑
            //item.OnDoubleClick -= item.OnDoubleClick;
            Type ItemType = item.GetType();
            FieldInfo _Field = item.GetType().GetField("OnDoubleClick", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            if (_Field != null)
            {
                object _FieldValue = _Field.GetValue(item);
                if (_FieldValue != null && _FieldValue is Delegate)
                {
                    Delegate _ObjectDelegate = (Delegate)_FieldValue;
                    Delegate[] invokeList = _ObjectDelegate.GetInvocationList();

                    foreach (Delegate del in invokeList)
                    {
                        ItemType.GetEvent("OnDoubleClick").RemoveEventHandler(item, del);
                    }
                }
            }
            item.OnDoubleClick += (categoryItem) =>
            {
                mLinkedNodesContainer.FocusNode(node);
            };

            //NodesControlAssist.Save();
        }
        private void StateControl_FilterContextMenu(CodeGenerateSystem.Controls.NodeListContextMenu contextMenu, CodeGenerateSystem.Controls.NodesContainerControl.ContextMenuFilterData filterData)
        {
            //var ctrlAssist = this.HostNodesContainer.HostControl as Macross.NodesControlAssist;
            //List<string> cachedPosesName = new List<string>();
            //foreach (var ctrl in ctrlAssist.NodesControl.CtrlNodeList)
            //{
            //    if (ctrl is CachedAnimPoseControl)
            //    {
            //        if (!cachedPosesName.Contains(ctrl.NodeName))
            //            cachedPosesName.Add(ctrl.NodeName);
            //    }
            //}
            //foreach (var sub in ctrlAssist.SubNodesContainers)
            //{
            //    foreach (var ctrl in sub.Value.CtrlNodeList)
            //    {
            //        if (ctrl is CachedAnimPoseControl)
            //        {
            //            if (!cachedPosesName.Contains(ctrl.NodeName))
            //                cachedPosesName.Add(ctrl.NodeName);
            //        }
            //    }
            //}
            //var assist = mLinkedNodesContainer.HostControl;
            //assist.NodesControl_FilterContextMenu(contextMenu, filterData);
            //var nodesList = contextMenu.GetNodesList();
            //nodesList.ClearNodes();
            //var stateCP = new AnimStateControlConstructionParams()
            //{
            //    CSType = HostNodesContainer.CSType,
            //    ConstructParam = "",
            //    NodeName = "State",
            //};
            //nodesList.AddNodesFromType(filterData, typeof(CodeDomNode.Particle.StructNodeControl), "State", stateCP, "");
            //foreach (var cachedPoseName in cachedPosesName)
            //{
            //    var getCachedPose = new GetCachedAnimPoseConstructionParams()
            //    {
            //        CSType = HostNodesContainer.CSType,
            //        ConstructParam = "",
            //        NodeName = "CachedPose_" + cachedPoseName,
            //    };
            //    nodesList.AddNodesFromType(filterData, typeof(GetCachedAnimPoseControl), "CachedAnimPose/" + getCachedPose.NodeName, getCachedPose, "");
            //}
        }
        async System.Threading.Tasks.Task InitializeLinkedNodesContainer()
        {
            var param = CSParam as ParticleStateControlConstructionParams;
            var assist = this.HostNodesContainer.HostControl as Macross.NodesControlAssist;

            if (mLinkedNodesContainer == null)
            {
                var TitleString = HostNodesContainer.TitleString;
                if (string.IsNullOrEmpty(HostNodesContainer.TitleString))
                {
                    TitleString = "MainGraph";
                }
                var title = TitleString + "/" + param.NodeName + ":" + this.Id.ToString();

                var data = new SubNodesContainerData()
                {
                    ID = Id,
                    Title = title,
                };
                mLinkedNodesContainer = await assist.GetSubNodesContainer(data);
                if (!data.IsCreated)
                    return;
            }
            // 读取graph
            var tempFile = assist.HostControl.GetGraphFileName(assist.LinkedCategoryItemName);
            var linkXndHolder = await EngineNS.IO.XndHolder.LoadXND(tempFile, EngineNS.Thread.Async.EAsyncTarget.AsyncEditor);
            bool bLoaded = false;
            if (linkXndHolder != null)
            {
                var linkNode = linkXndHolder.Node.FindNode("SubLinks");
                var idStr = Id.ToString();
                foreach (var node in linkNode.GetNodes())
                {
                    if (node.GetName() == idStr)
                    {
                        await mLinkedNodesContainer.Load(node);
                        bLoaded = true;
                        break;
                    }
                }
            }
            if (bLoaded)
            {

            }
            else
            {

                for (int i = 0; i < ParticleSubStateTemplate.Length; i++)
                {
                    CreateParticleMethodCategory(ParticleSubStateTemplate[i], 50, 100 * i + 50);
                }
            }
            mLinkedNodesContainer.HostNode = this;
        }

        #region SaveLoad

        public override void Save(XndNode xndNode, bool newGuid)
        {
            ParticleDataSaveLoad.SaveData("ParticleStateNode", xndNode, newGuid, CSParam as ParticleStateControlConstructionParams, this, mLinkedNodesContainer);
            base.Save(xndNode, newGuid);
        }

        bool NeedResetLoadValue = false;

        EngineNS.Thread.Async.TaskLoader.WaitContext WaitContext = new EngineNS.Thread.Async.TaskLoader.WaitContext();
        public async System.Threading.Tasks.Task<EngineNS.Thread.Async.TaskLoader.WaitContext> AwaitLoad()
        {
            return await EngineNS.Thread.Async.TaskLoader.Awaitload(WaitContext);
        }
        public override async System.Threading.Tasks.Task Load(XndNode xndNode)
        {
            //if (NeedInitGrapth)
            //{
            //    var test = InitGraph();
            //}

            await base.Load(xndNode);

            //await AwaitLoad();
            NeedResetLoadValue = await ParticleDataSaveLoad.LoadData2("ParticleStateNode", xndNode, CSParam as ParticleStateControlConstructionParams, this, mLinkedNodesContainer);

        }


        #endregion

        public static void InitNodePinTypes(CodeGenerateSystem.Base.ConstructionParams smParam)
        {
            CollectLinkPinInfo(smParam, "ParticleStateControlDown", CodeGenerateSystem.Base.enLinkType.Module, CodeGenerateSystem.Base.enBezierType.Left, CodeGenerateSystem.Base.enLinkOpType.Start, false);
            CollectLinkPinInfo(smParam, "ParticleStateControlUp", CodeGenerateSystem.Base.enLinkType.Module, CodeGenerateSystem.Base.enBezierType.Right, CodeGenerateSystem.Base.enLinkOpType.End, true);
        }

        public override string GCode_GetTypeString(CodeGenerateSystem.Base.LinkPinControl element, CodeGenerateSystem.Base.GenerateCodeContext_Method context)
        {
            return "Module";
        }

        public override Type GCode_GetType(CodeGenerateSystem.Base.LinkPinControl element, CodeGenerateSystem.Base.GenerateCodeContext_Method context)
        {
            return typeof(ParticleStateControl);
        }

        public void ResetRecursionReachedFlag()
        {
            //if (!mCtrlValueLinkHandle.RecursionReached)
            //    return;
            //mCtrlValueLinkHandle.RecursionReached = false;
            ////指向的节点
            //for (int i = 0; i < mCtrlValueLinkHandle.GetLinkInfosCount(); ++i)
            //{
            //    var linkInfo = mCtrlValueLinkHandle.GetLinkInfo(i);
            //    if (linkInfo.m_linkFromObjectInfo == mCtrlValueLinkHandle)
            //    {
            //        var anim = linkInfo.m_linkToObjectInfo.HostNodeControl as AnimStateControl;
            //        if (anim != null)
            //            anim.ResetRecursionReachedFlag();
            //    }
            //}
        }
        public override System.CodeDom.CodeExpression GCode_CodeDom_GetValue(CodeGenerateSystem.Base.LinkPinControl element, CodeGenerateSystem.Base.GenerateCodeContext_Method context, CodeGenerateSystem.Base.GenerateCodeContext_PreNode preNodeContext = null)
        {
            return new System.CodeDom.CodeSnippetExpression("System.Math.Sin((System.DateTime.Now.Millisecond*0.001)*2*System.Math.PI)");
        }
        CodeVariableReferenceExpression stateRef = null;
        public override async System.Threading.Tasks.Task GCode_CodeDom_GenerateCode(CodeTypeDeclaration codeClass, CodeStatementCollection codeStatementCollection, LinkPinControl element, GenerateCodeContext_Method context)
        {
            //if (!mCtrlValueLinkHandle.RecursionReached)
            //{
            //    mCtrlValueLinkHandle.RecursionReached = true;
            //    /*
            //     EngineNS.GamePlay.StateMachine.AnimationState State = new EngineNS.GamePlay.StateMachine.AnimationState("State", StateMachine);
            //     State.AnimationPose = StateMachine.AnimationPose;
            //     */
            //    var stateMachineRef = context.AnimStateMachineReferenceExpression;
            //    var validName = StringRegex.GetValidName(NodeName);
            //    System.CodeDom.CodeVariableDeclarationStatement stateStateMent = new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(EngineNS.Bricks.Animation.AnimStateMachine.LogicAnimationState)),
            //                                                                        validName, new CodeObjectCreateExpression(new CodeTypeReference(typeof(EngineNS.Bricks.Animation.AnimStateMachine.LogicAnimationState)), new CodeExpression[] { new CodePrimitiveExpression(validName), stateMachineRef }));
            //    codeStatementCollection.Add(stateStateMent);

            //    stateRef = new CodeVariableReferenceExpression(validName);
            //    CodeFieldReferenceExpression animPoseField = new CodeFieldReferenceExpression(stateRef, "AnimationPoseProxy");
            //    CodeAssignStatement animPoseAssign = new CodeAssignStatement();
            //    animPoseAssign.Left = animPoseField;
            //    animPoseAssign.Right = context.StateMachineAnimPoseReferenceExpression;
            //    codeStatementCollection.Add(animPoseAssign);

            //    context.StateAnimPoseReferenceExpression = animPoseField;
            //    //子图
            //    context.AminStateReferenceExpression = stateRef;
            //    if (mLinkedNodesContainer != null)
            //    {
            //        foreach (var ctrl in mLinkedNodesContainer.CtrlNodeList)
            //        {
            //            if ((ctrl is CodeDomNode.MethodOverride) ||
            //                            (ctrl is CodeDomNode.MethodCustom) || ctrl is FinalAnimPoseControl || ctrl is CodeDomNode.Animation.StateEntryControl)
            //            {
            //                await ctrl.GCode_CodeDom_GenerateCode(codeClass, codeStatementCollection, element, context);
            //            }
            //        }
            //    }

            //    //指向的节点
            //    for (int i = 0; i < mCtrlValueLinkHandle.GetLinkInfosCount(); ++i)
            //    {
            //        var linkInfo = mCtrlValueLinkHandle.GetLinkInfo(i);
            //        if (linkInfo.m_linkFromObjectInfo == mCtrlValueLinkHandle)
            //        {

            //            //需要返回state，来添加两者之间 的转换关系
            //            await linkInfo.m_linkToObjectInfo.HostNodeControl.GCode_CodeDom_GenerateCode(codeClass, codeStatementCollection, element, context);
            //            if (mCtrlValueLinkHandle.LinkCurveType == enLinkCurveType.Line)
            //            {
            //                var linkCurve = linkInfo.LinkPath as CodeGenerateSystem.Base.AnimStateTransitionCurve;
            //                foreach (var transitionCtrl in linkCurve.TransitionControlList)
            //                {
            //                    //构建状态转换宏图
            //                    await transitionCtrl.GCode_CodeDom_GenerateCode(codeClass, codeStatementCollection, element, context);
            //                }
            //                var desState = context.ReturnedAminStateReferenceExpression;
            //                var stateTransitionMethod = context.StateTransitionMethodReferenceExpression;
            //                if (stateTransitionMethod != null)
            //                {
            //                    //生成转换代码
            //                    CodeMethodReferenceExpression methodRef = new CodeMethodReferenceExpression(stateRef, "AddStateTransition");
            //                    CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression(methodRef, new CodeExpression[] { desState, new CodeMethodReferenceExpression(null, stateTransitionMethod.Name) });
            //                    codeStatementCollection.Add(methodInvoke);
            //                }
            //            }
            //        }
            //    }
            //}
            ////将自己的state返回给上层递归
            //context.ReturnedAminStateReferenceExpression = stateRef;

            ////返回currentState
            //foreach (var node in mCtrlValueLinkHandle.GetLinkedObjects())
            //{
            //    if (node is CodeDomNode.Animation.StateEntryControl)
            //    {
            //        context.FirstStateReferenceExpression = stateRef;
            //    }
            //}
        }
    }
}
