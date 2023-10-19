#import "UnityAppController.h"
#include "Unity/IUnityGraphics.h"

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ZibraLiquid_UnityPluginLoad(IUnityInterfaces* unityInterfaces);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ZibraLiquid_UnityPluginUnload();
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ZibraSmokeAndFire_UnityPluginLoad(IUnityInterfaces* unityInterfaces);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ZibraSmokeAndFire_UnityPluginUnload();

@interface EffectsAppController : UnityAppController
{
}
- (void)shouldAttachRenderDelegate;
@end
@implementation EffectsAppController
- (void)shouldAttachRenderDelegate
{
	// unlike desktops where plugin dynamic library is automatically loaded and registered
	// we need to do that manually on iOS
	UnityRegisterRenderingPluginV5(&ZibraLiquid_UnityPluginLoad, &ZibraLiquid_UnityPluginUnload);
	UnityRegisterRenderingPluginV5(&ZibraSmokeAndFire_UnityPluginLoad, &ZibraSmokeAndFire_UnityPluginUnload);
}

@end
IMPL_APP_CONTROLLER_SUBCLASS(EffectsAppController);
