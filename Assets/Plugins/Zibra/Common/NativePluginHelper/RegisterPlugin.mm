#import "UnityAppController.h"
#include "Unity/IUnityGraphics.h"

void (*ZibraEffects_LiquidPluginLoad)(IUnityInterfaces *) = nullptr;
void (*ZibraEffects_LiquidPluginUnload)() = nullptr;
void (*ZibraEffects_SmokeAndFirePluginLoad)(IUnityInterfaces *) = nullptr;
void (*ZibraEffects_SmokeAndFirePluginUnload)() = nullptr;

@interface ZibraEffectsAppController : UnityAppController
{
}
- (void)shouldAttachRenderDelegate;
@end
@implementation ZibraEffectsAppController
- (void)shouldAttachRenderDelegate
{
	if (ZibraEffects_LiquidPluginLoad && ZibraEffects_LiquidPluginUnload)
	{
		UnityRegisterRenderingPluginV5(ZibraEffects_LiquidPluginLoad, ZibraEffects_LiquidPluginUnload);
	}
	if (ZibraEffects_SmokeAndFirePluginLoad && ZibraEffects_SmokeAndFirePluginUnload)
	{
		UnityRegisterRenderingPluginV5(ZibraEffects_SmokeAndFirePluginLoad, ZibraEffects_SmokeAndFirePluginUnload);
	}
}

@end
IMPL_APP_CONTROLLER_SUBCLASS(ZibraEffectsAppController);
