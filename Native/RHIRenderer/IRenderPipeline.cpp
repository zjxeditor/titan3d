#include "IRenderPipeline.h"
#include "IVertexShader.h"
#include "IPixelShader.h"
#include "IInputLayout.h"
#include "IRasterizerState.h"
#include "IDepthStencilState.h"
#include "IBlendState.h"
#include "IShaderProgram.h"

#define new VNEW

NS_BEGIN

IRenderPipeline::IRenderPipeline()
{
	mRasterizerState = nullptr;
	mDepthStencilState = nullptr;
	mBlendState = nullptr;
	mSampleMask = 0xffffffff;
	memset(mBlendFactor, 0, sizeof(mBlendFactor));
}

IRenderPipeline::~IRenderPipeline()
{
	Safe_Release(mRasterizerState);
	Safe_Release(mDepthStencilState);
	Safe_Release(mBlendState);
}


void IRenderPipeline::BindRasterizerState(IRasterizerState* State)
{
	if (State)
		State->AddRef();
	Safe_Release(mRasterizerState);
	mRasterizerState = State;
}

void IRenderPipeline::BindDepthStencilState(IDepthStencilState* State)
{
	if (State)
		State->AddRef();
	Safe_Release(mDepthStencilState);
	mDepthStencilState = State;
}

void IRenderPipeline::BindBlendState(IBlendState* State)
{
	if (State)
		State->AddRef();
	Safe_Release(mBlendState);
	mBlendState = State;
}


NS_END

using namespace EngineNS;

extern "C"
{
	CSharpAPI1(EngineNS, IRenderPipeline, BindRasterizerState, IRasterizerState*);
	CSharpAPI1(EngineNS, IRenderPipeline, BindDepthStencilState, IDepthStencilState*);
	CSharpAPI1(EngineNS, IRenderPipeline, BindBlendState, IBlendState*);

	CSharpReturnAPI0(IRasterizerState*, EngineNS, IRenderPipeline, GetRasterizerState);
	CSharpReturnAPI0(IDepthStencilState*, EngineNS, IRenderPipeline, GetDepthStencilState);
	CSharpReturnAPI0(IBlendState*, EngineNS, IRenderPipeline, GetBindBlendState);
}