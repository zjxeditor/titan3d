#include "GfxFBXImporter.h"
#include "GfxFBXManager.h"
#include "GfxFileImportOption.h"
#include "GfxAssetImportOption.h"
#include "GfxAsset_File.h"
#include <fbxsdk.h>
#include "FbxDataConverter.h"

#define new VNEW
NS_BEGIN
RTTI_IMPL(EngineNS::GfxFBXImporter, EngineNS::VIUnknown);
void GetFbxNodeImportOption(FbxNode* node, GfxFileImportOption* assetImportOption);
GfxFBXImporter::GfxFBXImporter()
{
}


GfxFBXImporter::~GfxFBXImporter()
{
}

void GfxFBXImporter::ConvertScene(FbxScene* scene, GfxFileImportOption* fileOption)
{
	//Merge the anim stack before the conversion since the above 0 layer will not be converted
	//int AnimStackCount = scene->GetSrcObjectCount<FbxAnimStack>();
	////Merge the animation stack layer before converting the scene
	//for (int AnimStackIndex = 0; AnimStackIndex < AnimStackCount; AnimStackIndex++)
	//{
	//	FbxAnimStack* CurAnimStack = scene->GetSrcObject<FbxAnimStack>(AnimStackIndex);
	//	//int ResampleRate = GetGlobalAnimStackSampleRate(CurAnimStack);
	//	//MergeAllLayerAnimation(CurAnimStack, ResampleRate);
	//}

	//Set the original file information

	FbxAMatrix AxisConversionMatrix;
	AxisConversionMatrix.SetIdentity();

	FbxAMatrix JointOrientationMatrix;
	//if (GetFileImportOption()->ConvertScene)
	{
		FbxAxisSystem ImportAxis(FbxAxisSystem::eMax);
		FbxAxisSystem SceneAxisSystem = scene->GetGlobalSettings().GetAxisSystem();

		if (SceneAxisSystem != ImportAxis)
		{
			//FbxRootNodeUtility::RemoveAllFbxRoots(scene);
			ImportAxis.ConvertScene(scene);

			FbxAMatrix SourceMatrix;
			SceneAxisSystem.GetMatrix(SourceMatrix);
			FbxAMatrix matrix;
			ImportAxis.GetMatrix(matrix);
			AxisConversionMatrix = SourceMatrix.Inverse() * matrix;


			/*if (GetImportOptions()->bForceFrontXAxis)
			{
				JointOrientationMatrix.SetR(FbxVector4(-90.0, -90.0, 0.0));
			}*/
		}
	}

	FbxDataConverter::SetJointPostConversionMatrix(JointOrientationMatrix);

	FbxDataConverter::SetAxisConversionMatrix(AxisConversionMatrix);

	// Convert the scene's units to what is used in this program, if needed.
	// The base unit used in both FBX and Unreal is centimeters.  So unless the units 
	// are already in centimeters (ie: scalefactor 1.0) then it needs to be converted
	if (fileOption->mConvertSceneUnit && scene->GetGlobalSettings().GetSystemUnit() != FbxSystemUnit::m)
	{
		//FbxSystemUnit::m.ConvertScene(scene);
	}
	auto factor = FbxSystemUnit::m.GetConversionFactorFrom(scene->GetGlobalSettings().GetSystemUnit());
	//Reset all the transform evaluation cache since we change some node transform
	scene->GetAnimationEvaluator()->Reset();
}

SystemUnit GetSystemUnitType(FbxSystemUnit& unit)
{
	if (unit == FbxSystemUnit::mm)
		return SU_mm;
	if (unit == FbxSystemUnit::dm)
		return SU_dm;
	if (unit == FbxSystemUnit::cm)
		return SU_cm;
	if (unit == FbxSystemUnit::m)
		return SU_m;
	if (unit == FbxSystemUnit::km)
		return SU_km;
	if (unit == FbxSystemUnit::Inch)
		return SU_Inch;
	if (unit == FbxSystemUnit::Foot)
		return SU_Foot;
	if (unit == FbxSystemUnit::Mile)
		return SU_Mile;
	if (unit == FbxSystemUnit::Yard)
		return SU_Yard;
	return SU_Custom;
}
vBOOL GfxFBXImporter::PreImport(const char* fileName, GfxFileImportOption* option, GfxFBXManager* manager)
{
	int lFileMajor, lFileMinor, lFileRevision;
	int lSDKMajor, lSDKMinor, lSDKRevision;
	//int i, lAnimStackCount;
	bool lStatus;
	//char lPassword[1024];

	// Get the version number of the FBX files generated by the
	// version of FBX SDK that you are using.
	FbxManager::GetFileFormatVersion(lSDKMajor, lSDKMinor, lSDKRevision);

	auto fbxFileName = FbxDataConverter::ConvertToFbxString(fileName);
	// Create an importer.
	FbxImporter* lImporter = FbxImporter::Create(manager->GetSDKManager(), fbxFileName);

	// Initialize the importer by providing a filename.
	const bool lImportStatus = lImporter->Initialize(fbxFileName, -1, manager->GetSDKManager()->GetIOSettings());

	// Get the version number of the FBX file format.
	lImporter->GetFileVersion(lFileMajor, lFileMinor, lFileRevision);

	if (!lImportStatus)  // Problem with the file to be imported
	{
		FbxString error = lImporter->GetStatus().GetErrorString();
		VFX_LTRACE(ELTT_Error, "Call to FbxImporter::Initialize() failed.");
		VFX_LTRACE(ELTT_Error, "Error returned: %s", error.Buffer());

		if (lImporter->GetStatus().GetCode() == FbxStatus::eInvalidFileVersion)
		{
			VFX_LTRACE(ELTT_Error, "FBX version number for this FBX SDK is %d.%d.%d",
				lSDKMajor, lSDKMinor, lSDKRevision);
			VFX_LTRACE(ELTT_Error, "FBX version number for file %s is %d.%d.%d",
				fileName, lFileMajor, lFileMinor, lFileRevision);
		}
		return FALSE;
	}

	//VFX_LTRACE(ELTT_Error, "FBX version number for this FBX SDK is %d.%d.%d",
	//	lSDKMajor, lSDKMinor, lSDKRevision);
	//VFX_LTRACE(ELTT_Error, "FBX version number for file %s is %d.%d.%d",
	//	fileName, lFileMajor, lFileMinor, lFileRevision);

	if (lImporter->IsFBX())
	{

		// Import options determine what kind of data is to be imported.
		// The default is true, but here we set the options explictly.

		manager->IOSettings()->SetBoolProp(IMP_FBX_MATERIAL, true);
		manager->IOSettings()->SetBoolProp(IMP_FBX_TEXTURE, true);
		manager->IOSettings()->SetBoolProp(IMP_FBX_LINK, true);
		manager->IOSettings()->SetBoolProp(IMP_FBX_SHAPE, true);
		manager->IOSettings()->SetBoolProp(IMP_FBX_GOBO, true);
		manager->IOSettings()->SetBoolProp(IMP_FBX_ANIMATION, true);
		manager->IOSettings()->SetBoolProp(IMP_FBX_GLOBAL_SETTINGS, true);
	}

	auto scene = FbxScene::Create(manager->GetSDKManager(), fbxFileName);
	// Import the scene.
	lStatus = lImporter->Import(scene);

	// The import file may have a password
	if (lStatus == false &&
		lImporter->GetStatus().GetCode() == FbxStatus::ePasswordError)
	{
		VFX_LTRACE(ELTT_info, "Please enter password: ");
		return FALSE;
	}

	// Convert mesh, NURBS and patch into triangle mesh
	FbxGeometryConverter lGeomConverter(manager->GetSDKManager());
	lGeomConverter.Triangulate(scene, /*replace*/true);

	auto info = lImporter->GetFileHeaderInfo();
	auto time = info->mCreationTimeStamp;
	std::string createrName(info->mCreator.Buffer());
	option->mCreater = createrName;
	FbxSystemUnit SceneSystemUnit = scene->GetGlobalSettings().GetSystemUnit();
	option->mFileSystemUnit = GetSystemUnitType(SceneSystemUnit);
	option->mScaleFactor = (float)FbxSystemUnit::m.GetConversionFactorFrom(SceneSystemUnit);

	mSceneMap.insert(std::make_pair(HashHelper::APHash(fileName), scene));
	auto stringFileName = std::string(fileName);
	auto indexStart = stringFileName.find_last_of('\\');
	auto indexEnd = stringFileName.find_last_of('.');
	auto pureFileName = stringFileName.substr(indexStart + 1, indexEnd - indexStart - 1);
	option->Name = pureFileName;
	GetAssetImportOption(scene, option);
	mImportOptionsMap.insert(std::make_pair(HashHelper::APHash(fileName), option));
	// Destroy the importer
	lImporter->Destroy();
	return TRUE;
}

vBOOL EngineNS::GfxFBXImporter::Import(IRenderContext* rc, const char* fileName, GfxAsset_File* assetFile, GfxFBXManager* manager)
{
	auto scene = mSceneMap[HashHelper::APHash(fileName)];
	ConvertScene(scene, assetFile->FileOption);
	for (UINT i = 0; i < assetFile->FileOption->ObjectOptions.size(); ++i)
	{
		UINT hash = assetFile->FileOption->GetAssetImportOption(i)->Hash;
		auto creater = assetFile->AssetCreaters[hash];
		if (creater)
			creater->Process(rc, scene, assetFile->FileOption, manager);
	}

	return TRUE;
}

void GfxFBXImporter::GetAssetImportOption(FbxScene* scene, GfxFileImportOption* fileImportOption)
{
	//fileImportOption->Name = scene->GetName();
	fileImportOption->Hash = HashHelper::APHash(fileImportOption->Name.c_str());
	auto root = scene->GetRootNode();
	for (int i = 0; i < root->GetChildCount(); ++i)
	{
		GetFbxNodeAssetImportOption(root->GetChild(i), fileImportOption);
	}
	scene->FillAnimStackNameArray(mAnimStackNameArray);
	for (int i = 0; i < mAnimStackNameArray.GetCount(); ++i)
	{
		// select the base layer from the animation stack
		FbxAnimStack * lAnimationStack = scene->FindMember<FbxAnimStack>(mAnimStackNameArray[i]->Buffer());
		if (lAnimationStack == NULL)
		{
			// this is a problem. The anim stack should be found in the scene!
			return;
		}
		FbxAnimLayer* lanimationLayer = lAnimationStack->GetMember<FbxAnimLayer>();
		GetFbxNodeAnimationImportOption(scene, lAnimationStack, lanimationLayer, scene->GetRootNode(), fileImportOption, true);
	}
}
bool GfxFBXImporter::IsHaveAnimCurve(FbxNodeAttribute* nodeAtt, FbxAnimLayer* animLayer)
{
	FbxProperty lProperty = nodeAtt->GetFirstProperty();
	while (lProperty.IsValid())
	{
		auto curveNode = lProperty.GetCurveNode(animLayer);
		if (curveNode)
			return true;
		lProperty = nodeAtt->GetNextProperty(lProperty);
	}
	return false;
}

bool GfxFBXImporter::IsHaveAnimCurve(FbxNode* node, FbxAnimLayer* animLayer)
{
	FbxProperty lProperty = node->GetFirstProperty();
	while (lProperty.IsValid())
	{
		auto curveNode = lProperty.GetCurveNode(animLayer);
		if (curveNode)
			return true;
		lProperty = node->GetNextProperty(lProperty);
	}
	return false;
}

bool GfxFBXImporter::IsSkeletonHaveAnimCurve(FbxNode* node, FbxAnimLayer* animLayer)
{
	if (IsHaveAnimCurve(node, animLayer))
		return true;
	for (int i = 0; i < node->GetChildCount(); ++i)
	{
		auto result = IsSkeletonHaveAnimCurve(node->GetChild(i), animLayer);
		if (result)
			return true;
	}
	return false;
}

void GfxFBXImporter::GetFbxNodeAnimationImportOption(FbxScene* scene, FbxAnimStack * animStack, FbxAnimLayer* animLayer, FbxNode* node, GfxFileImportOption* fileImportOption, bool skeletonAnimMode)
{
	auto animName = FbxDataConverter::ConvertToStdString(node->GetName());
	auto att = node->GetNodeAttribute();
	if (att == NULL)
	{

	}
	else
	{
		bool createOption = false;
		auto attType = att->GetAttributeType();
		if (attType == IAT_Skeleton || attType == IAT_Null)//dummy as bone
		{
			if (IsSkeletonHaveAnimCurve(node, animLayer))
			{
				createOption = true;
				if (skeletonAnimMode)
				{
					if (mAnimStackNameArray.GetCount() > 1)
					{

						animName = fileImportOption->Name + std::string("_") + FbxDataConverter::ConvertToStdString(animStack->GetName());
					}
					else
					{
						animName = fileImportOption->Name;
					}
				}
			}
		}
		else if (IsHaveAnimCurve(node, animLayer) || IsHaveAnimCurve(att, animLayer))
		{
			createOption = true;
		}
		if (createOption)
		{
			GfxAnimationImportOption* option = new GfxAnimationImportOption();
			option->Name = animName;
			option->Hash = HashHelper::APHash((animName + std::string("_Anim")).c_str());
			option->Type = (ImportAssetType)IAT_Animation;
			option->AnimationType = (ImportAssetType)attType;
			option->FBXNode = node;
			option->AnimLayer = animLayer;
			option->AnimStack = animStack;
			auto span = animStack->GetLocalTimeSpan();
			auto start = span.GetStart().GetSecondDouble();
			auto duration = span.GetDuration().GetSecondDouble();
			auto end = span.GetStop().GetSecondDouble();
			auto rate = FbxTime::GetFrameRate(node->GetScene()->GetGlobalSettings().GetTimeMode());
			option->Duration = (float)duration;
			option->SampleRate = (float)rate;
			fileImportOption->ObjectOptions.insert(std::make_pair(option->Hash, option));
		}

		//skeleton only root
		if (attType == IAT_Skeleton || attType == IAT_Null)
			return;
	}
	for (int i = 0; i < node->GetChildCount(); ++i)
	{
		GetFbxNodeAnimationImportOption(scene, animStack, animLayer, node->GetChild(i), fileImportOption, skeletonAnimMode);
	}
}


void GfxFBXImporter::GetFbxNodeAssetImportOption(FbxNode* node, GfxFileImportOption* fileImportOption)
{
	auto att = node->GetNodeAttribute();
	if (att == nullptr)
	{

	}
	else
	{
		auto attType = att->GetAttributeType();
		switch (attType)
		{
		case IAT_Mesh:
		{
			auto mesh = (FbxMesh*)att;
			GfxMeshImportOption* option = new GfxMeshImportOption();
			option->Name = FbxDataConverter::ConvertToStdString(node->GetName());
			option->Hash = HashHelper::APHash(option->Name.c_str());
			option->Type = (ImportAssetType)attType;
			option->FBXNode = node;
			if (mesh->GetDeformerCount() > 0)
			{
				option->HaveSkin = TRUE;
			}
			if (node->GetMaterialCount() > 0)
				option->RenderAtom = node->GetMaterialCount();
			else
				option->RenderAtom = 1;
			fileImportOption->ObjectOptions.insert(std::make_pair(option->Hash, option));
		}
		break;
		case  IAT_Light:
		{
			{
				GfxAssetImportOption* option = new GfxAssetImportOption();
				option->Name = FbxDataConverter::ConvertToStdString(node->GetName());
				option->Hash = HashHelper::APHash(option->Name.c_str());
				option->Type = (ImportAssetType)attType;
				option->FBXNode = node;
				fileImportOption->ObjectOptions.insert(std::make_pair(option->Hash, option));
			}
		}
		case IAT_Skeleton:
		default:

			break;
		}

		if (attType == IAT_Skeleton)
			return;
	}
	for (int i = 0; i < node->GetChildCount(); ++i)
	{
		GetFbxNodeAssetImportOption(node->GetChild(i), fileImportOption);
	}
}
NS_END

using namespace EngineNS;



extern "C"
{
	CSharpReturnAPI3(vBOOL, EngineNS, GfxFBXImporter, PreImport, const char*, GfxFileImportOption*, GfxFBXManager*);
	CSharpReturnAPI4(vBOOL, EngineNS, GfxFBXImporter, Import, IRenderContext*, const char*, GfxAsset_File*, GfxFBXManager*);
}