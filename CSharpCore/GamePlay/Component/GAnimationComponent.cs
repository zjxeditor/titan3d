﻿using EngineNS.Bricks.Animation.AnimNode;
using EngineNS.Bricks.Animation.Pose;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace EngineNS.GamePlay.Component
{
    [GamePlay.Component.CustomConstructionParamsAttribute(typeof(GAnimationComponentInitializer), "动画组件", "Animation Component")]
    public class GAnimationComponent : GComponent
    {
        [Rtti.MetaClass]
        public class GAnimationComponentInitializer : GComponentInitializer
        {
            [Rtti.MetaData]
            public RName SkeletonAssetName
            {
                get; set;
            }
            [Rtti.MetaData]
            public RName AnimationName
            {
                get; set;
            }
        }
        protected RName mSkeletonAssetName = RName.EmptyName;
        [Browsable(false)]
        public RName SkeletonAssetName
        {
            get => mSkeletonAssetName;
            set
            {
                if (value == null || value == RName.EmptyName)
                    return;
                mSkeletonAssetName = value;
                var initializer = this.Initializer as GAnimationComponentInitializer;
                if (initializer != null)
                    initializer.SkeletonAssetName = value;
                //InitializeAnimationPose();
            }
        }
        protected CGfxAnimationPoseProxy mAnimationPoseProxy = new CGfxAnimationPoseProxy();
        [Browsable(false)]
        public CGfxAnimationPoseProxy AnimationPoseProxy { get => mAnimationPoseProxy; set => mAnimationPoseProxy = value; }
        CGfxAnimationNode mAnimation = null;
        [EngineNS.Editor.MacrossMember(Editor.MacrossMemberAttribute.enMacrossType.Callable | Editor.MacrossMemberAttribute.enMacrossType.ReadOnly)]
        [Browsable(false)]
        public CGfxAnimationNode Animation
        {
            set
            {
                mAnimation = value;
                if (mAnimationPoseProxy.IsNullPose)
                    return;
                mAnimation.AnimationPoseProxy = mAnimationPoseProxy;
            }
            get { return mAnimation; }
        }
        AnimationClip mClip = null;
        [Browsable(false)]
        public AnimationClip Clip { get => mClip; set => mClip = value; }
        [Browsable(false)]
        public Bricks.Animation.Binding.AnimationBindingPose BindingPose { get; set; }
        public float PlayRate
        {
            get
            {
                if (mClip != null)
                    return mClip.PlayRate;
                return 0;
            }
            set
            {
                if (mClip != null)
                    mClip.PlayRate = value;
            }
        }
        public bool Pause
        {
            get
            {
                if (mClip != null)
                    return mClip.Pause;
                return false;
            }
            set
            {
                if (mClip != null)
                    mClip.Pause = value;
            }
        }
        RName mAnimationName = RName.EmptyName;
        [DisplayName("Animation")]
        [EngineNS.Editor.Editor_RNameTypeAttribute(EngineNS.Editor.Editor_RNameTypeAttribute.AnimationClip)]
        public RName AnimationName
        {
            get => mAnimationName;
            set
            {
                if (value == RName.EmptyName || value == null)
                    return;
                mAnimationName = value;
                var initializer = this.Initializer as GAnimationComponentInitializer;
                initializer.AnimationName = value;
                var anim = new CGfxAnimationSequence();
                bool Sequence = false;
                if (Sequence)
                {
                    anim.Init(value);
                }
                else
                {
                    Clip = new AnimationClip();
                    Clip.SyncLoad(CEngine.Instance.RenderContext, value);
                    BindingPose = Clip.Bind(mHost);

                }
            }
        }
        public GAnimationComponent()
        {
            OnlyForGame = false;
            this.Initializer = new GAnimationComponentInitializer();
        }
        public GAnimationComponent(RName skeletonAsset)
        {
            var initializer = new GAnimationComponentInitializer();
            SkeletonAssetName = skeletonAsset;
            initializer.SkeletonAssetName = skeletonAsset;
            this.Initializer = initializer;
        }
        public override async System.Threading.Tasks.Task<bool> SetInitializer(CRenderContext rc, Actor.GActor host, IComponentContainer hostContainer, GComponentInitializer v)
        {
            if (rc == null)
                rc = CEngine.Instance.RenderContext;
            await base.SetInitializer(rc, host, hostContainer, v);
            var init = v as GAnimationComponentInitializer;
            if (init == null)
                return false;
            SkeletonAssetName = init.SkeletonAssetName;
            LinkSkinModifier();
            AnimationName = init.AnimationName;
            return true;
        }
        void InitializeAnimationPose()
        {
            if (mAnimationPoseProxy.IsNullPose)
                mAnimationPoseProxy.Pose = EngineNS.CEngine.Instance.SkeletonAssetManager.GetSkeleton(EngineNS.CEngine.Instance.RenderContext, mSkeletonAssetName).BoneTab.Clone();
        }
        ~GAnimationComponent()
        {

        }
        public override void Tick(GPlacementComponent placement)
        {
            Animation?.TickLogic();

            Clip?.Tick();
            //    BindingPose.ExceuteoBind();
            base.Tick(placement);
        }
        public override void OnAdded()
        {
            LinkSkinModifier();
            base.OnAdded();
        }
        bool IsSkinModifierLinked = false;
        void LinkSkinModifier()
        {
            if (IsSkinModifierLinked)
                return;
            if (mHostContainer != mHost)
            {
                LinkSkinModifier(mHostContainer);
            }
            else
            {
                LinkSkinModifier(mHost);
            }
            IsSkinModifierLinked = true;
        }
        void LinkSkinModifier(Actor.GActor actor)
        {
            var meshComp = actor.GetComponent<GMeshComponent>();
            if (meshComp != null && meshComp.SceneMesh != null)
            {
                var skinModifier = meshComp.SceneMesh.MdfQueue.FindModifier<EngineNS.Graphics.Mesh.CGfxSkinModifier>();
                if (skinModifier != null)
                {
                    SkeletonAssetName = RName.GetRName(skinModifier.SkeletonAsset);
                    AnimationPoseProxy = skinModifier.AnimationPoseProxy;
                }
            }
            var mutiMeshComp = actor.GetComponent<GMutiMeshComponent>();
            if (mutiMeshComp != null)
            {
                foreach (var subMesh in mutiMeshComp.Meshes)
                {
                    var skinModifier = subMesh.Value.MdfQueue.FindModifier<EngineNS.Graphics.Mesh.CGfxSkinModifier>();
                    if (skinModifier != null)
                    {
                        AnimationPoseProxy = skinModifier.AnimationPoseProxy;
                    }
                }
            }

        }
        void LinkSkinModifier(IComponentContainer compContainer)
        {
            var meshComp = compContainer as GMeshComponent;
            if (meshComp != null && meshComp.SceneMesh != null)
            {
                var skinModifier = meshComp.SceneMesh.MdfQueue.FindModifier<EngineNS.Graphics.Mesh.CGfxSkinModifier>();
                if (skinModifier != null)
                {
                    SkeletonAssetName = RName.GetRName(skinModifier.SkeletonAsset);
                    AnimationPoseProxy = skinModifier.AnimationPoseProxy;
                }
            }
            var mutiMeshComp = compContainer as GMutiMeshComponent;
            if (mutiMeshComp != null)
            {
                foreach (var subMesh in mutiMeshComp.Meshes)
                {
                    var skinModifier = subMesh.Value.MdfQueue.FindModifier<EngineNS.Graphics.Mesh.CGfxSkinModifier>();
                    if (skinModifier != null)
                    {
                        AnimationPoseProxy = skinModifier.AnimationPoseProxy;
                    }
                }
            }
        }
        public override void OnAddedScene()
        {

        }
        public override void OnRemoveScene()
        {

        }
    }
}