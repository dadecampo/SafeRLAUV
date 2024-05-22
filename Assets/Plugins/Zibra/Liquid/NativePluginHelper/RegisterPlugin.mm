#import "UnityAppController.h"
#include "Unity/IUnityGraphics.h"

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ZibraLiquid_UnityPluginLoad(IUnityInterfaces* unityInterfaces);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ZibraLiquid_UnityPluginUnload();

@interface ZibraLiquidNativeTrampoline : NSObject
{												
}												
+(void)load;									
@end											
@implementation ZibraLiquidNativeTrampoline
+(void)load										
{											
	extern void (*ZibraEffects_LiquidPluginLoad)(IUnityInterfaces *);
	extern void (*ZibraEffects_LiquidPluginUnload)();
	ZibraEffects_LiquidPluginLoad = &ZibraLiquid_UnityPluginLoad;
	ZibraEffects_LiquidPluginUnload = &ZibraLiquid_UnityPluginUnload;
}												
@end
