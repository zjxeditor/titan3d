#pragma once
#include "../IRenderContext.h"
#include "VKPreHead.h"

NS_BEGIN

class IVKRenderSystem;

class IVKRenderContext : public IRenderContext
{
public:
	IVKRenderContext();
	~IVKRenderContext();

	virtual ERHIType GetRHIType() override
	{
		return RHT_VULKAN;
	}

	virtual ISwapChain* CreateSwapChain(const ISwapChainDesc* desc) override;
	virtual ICommandList* CreateCommandList(const ICommandListDesc* desc) override;
	
	virtual IPass* CreatePass() override;

	virtual IRenderPipeline* CreateRenderPipeline(const IRenderPipelineDesc* desc) override;

	//������Ϣ
	virtual IVertexBuffer* CreateVertexBuffer(const IVertexBufferDesc* desc) override;
	virtual IIndexBuffer* CreateIndexBuffer(const IIndexBufferDesc* desc) override;
	virtual IInputLayout* CreateInputLayout(const IInputLayoutDesc* desc) override;
	virtual IGeometryMesh* CreateGeometryMesh() override;

	virtual IIndexBuffer* CreateIndexBufferFromBuffer(const IIndexBufferDesc* desc, const IGpuBuffer* pBuffer) override;
	virtual IVertexBuffer* CreateVertexBufferFromBuffer(const IVertexBufferDesc* desc, const IGpuBuffer* pBuffer) override;
	//��Դ��Ϣ
	virtual IFrameBuffers* CreateFrameBuffers(const IFrameBuffersDesc* desc) override;
	virtual IRenderTargetView* CreateRenderTargetView(const IRenderTargetViewDesc* desc) override;
	virtual IDepthStencilView* CreateDepthRenderTargetView(const IDepthStencilViewDesc* desc) override;
	virtual ITexture2D* CreateTexture2D(const ITexture2DDesc* desc) override;
	virtual IShaderResourceView* CreateShaderResourceView(const IShaderResourceViewDesc* desc) override;
	virtual IShaderResourceView* CreateShaderResourceViewFromBuffer(IGpuBuffer* pBuffer, const ISRVDesc* desc) override;
	virtual IGpuBuffer* CreateGpuBuffer(const IGpuBufferDesc* desc, void* pInitData) override;
	virtual IUnorderedAccessView* CreateUnorderedAccessView(IGpuBuffer* pBuffer, const IUnorderedAccessViewDesc* desc) override;
	virtual IShaderResourceView* LoadShaderResourceView(const char* file) override;
	virtual ISamplerState* CreateSamplerState(const ISamplerStateDesc* desc) override;
	virtual IRasterizerState* CreateRasterizerState(const IRasterizerStateDesc* desc) override;
	virtual IDepthStencilState* CreateDepthStencilState(const IDepthStencilStateDesc* desc) override;
	virtual IBlendState* CreateBlendState(const IBlendStateDesc* desc) override;
	//shader
	virtual IShaderProgram* CreateShaderProgram(const IShaderProgramDesc* desc) override;
	virtual IVertexShader* CreateVertexShader(const IShaderDesc* desc) override;
	virtual IPixelShader* CreatePixelShader(const IShaderDesc* desc) override;
	virtual IComputeShader* CreateComputeShader(const IShaderDesc* desc) override;

	virtual IConstantBuffer* CreateConstantBuffer(const IConstantBufferDesc* desc) override;

public:
	TObjectHandle<IVKRenderSystem>	mRenderSystem;
	VkPhysicalDevice				mPhysicalDevice;
	VkDevice						mLogicalDevice;

	VkPhysicalDeviceFeatures		mDeviceFeatures;

	VkQueue							mGraphicsQueue;
	VkQueue							mPresentQueue;

	VkCommandPool					mCommandPool;

	bool Init(const IRenderContextDesc* desc, IVKRenderSystem* pSys, VkPhysicalDevice device, VkSurfaceKHR surface);

	VkDescriptorSetLayout			mCBDescSetLayout;
	void InitDescLayout(VkDevice device);
};

NS_END