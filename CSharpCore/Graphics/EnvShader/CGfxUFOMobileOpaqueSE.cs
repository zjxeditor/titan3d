using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Graphics.EnvShader
{
    public class CGfxUFOMobileOpaqueSE : CGfxShadingEnv
    {
        //public string mFilePath = "Shaders/MMDMobileOpaque.shadingenv";
        
        public CShaderResourceView mEnvMap;
        public CShaderResourceView mEyeEnvMap;
        CSamplerState SamplerStat;

        public CShaderResourceView mTex_ShadowMap;
        CSamplerState mSampler_ShadowMap;

        public override void GetMacroDefines(List<CShaderDefinitions.MacroDefine> defs)
        {
        }
        public override RName ShadingEnvName
        {
            get
            {
                return RName.GetRName("Shaders/UFOMobileOpaqueSE.shadingenv");
            }
        }
        protected override void OnCreated()
        {
            AddSRV("gEnvMap");
            AddSRV("gEyeEnvMap");
            AddSRV("gShadowMap");
            
            var SamplerStatDesc = new CSamplerStateDesc();
            SamplerStatDesc.SetDefault();
            SamplerStatDesc.Filter = ESamplerFilter.SPF_MIN_MAG_MIP_LINEAR;
            SamplerStat = CEngine.Instance.SamplerStateManager.GetSamplerState(CEngine.Instance.RenderContext, SamplerStatDesc);

            var SamplerDesc_ShadowMap = new CSamplerStateDesc();
            SamplerDesc_ShadowMap.SetDefault();
            SamplerDesc_ShadowMap.AddressU = EAddressMode.ADM_BORDER;
            SamplerDesc_ShadowMap.AddressV = EAddressMode.ADM_BORDER;
            SamplerDesc_ShadowMap.BorderColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            mSampler_ShadowMap = CEngine.Instance.SamplerStateManager.GetSamplerState(CEngine.Instance.RenderContext, SamplerDesc_ShadowMap);
        }

        public override void BindResources(Graphics.Mesh.CGfxMesh mesh, CPass pass)
        {
            var TexBinder = pass.ShaderResources;
            var SamplerBinder = pass.ShaderSamplerBinder;
            var Shader = pass.Effect;

            //
            if (Shader.CacheData.EnvMapBindPoint != UInt32.MaxValue)
                TexBinder.PSBindTexture(Shader.CacheData.EnvMapBindPoint, mEnvMap);

            if (Shader.CacheData.SampEnvMapBindPoint != UInt32.MaxValue - 1)
                SamplerBinder.PSBindSampler(Shader.CacheData.SampEnvMapBindPoint, SamplerStat);

            if (Shader.CacheData.EyeEnvMapBindPoint != UInt32.MaxValue - 1)
                TexBinder.PSBindTexture(Shader.CacheData.EyeEnvMapBindPoint, mEyeEnvMap);

            if (Shader.CacheData.SampEyeEnvMapBindPoint != UInt32.MaxValue - 1)
                SamplerBinder.PSBindSampler(Shader.CacheData.SampEyeEnvMapBindPoint, SamplerStat);

            //shadow map binding;
            if (Shader.CacheData.BindingSlot_ShadowMap != UInt32.MaxValue - 1)
            {
                TexBinder.PSBindTexture(Shader.CacheData.BindingSlot_ShadowMap, mTex_ShadowMap);
            }
            
            if (Shader.CacheData.BindingSlot_ShadowMapSampler != UInt32.MaxValue - 1)
            {
                SamplerBinder.PSBindSampler(Shader.CacheData.BindingSlot_ShadowMapSampler, mSampler_ShadowMap);
            }
            
            //
        }
    }
}
