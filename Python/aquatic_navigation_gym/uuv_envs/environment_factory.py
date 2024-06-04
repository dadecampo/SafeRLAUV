import numpy as np
from enum import Enum
from typing import List

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper
from mlagents_envs.environment import SideChannel
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel

class UuvEnvEnumeration(Enum):
    EasyEnv = 1
    MediumEnv = 2
    HardEnv = 3
    
def _get_side_channels(auvEnum: UuvEnvEnumeration) -> List[SideChannel]:
    if (auvEnum == UuvEnvEnumeration.EasyEnv):
        return _get_easy_env_channels()
    elif (auvEnum == UuvEnvEnumeration.MediumEnv):
        return _get_medium_env_channels()
    else:
        return _get_hard_env_channels()
        
def _get_easy_env_channels() -> List[SideChannel]:
    engineConfigChannel = EngineConfigurationChannel()
    engineConfigChannel.set_configuration_parameters(width=800, height=800, quality_level=1, time_scale=1, capture_frame_rate=60)
    
    environmentParametersChannel = EnvironmentParametersChannel()
    environmentParametersChannel.set_float_parameter("target_x", 33.66)
    environmentParametersChannel.set_float_parameter("target_y", 6.29)
    environmentParametersChannel.set_float_parameter("target_z", -76.13)
    environmentParametersChannel.set_float_parameter("waterEnabled", 0)
    environmentParametersChannel.set_float_parameter("fastRestart", 0)
    environmentParametersChannel.set_float_parameter("distancePlanesN", 6)
    environmentParametersChannel.set_float_parameter("safeTraining", 1)

    return [engineConfigChannel, environmentParametersChannel]

def _get_medium_env_channels() -> List[SideChannel]:
    
    engineConfigChannel = EngineConfigurationChannel()
    engineConfigChannel.set_configuration_parameters(width=800, height=800, quality_level=1, time_scale=1, capture_frame_rate=60)
    
    environmentParametersChannel = EnvironmentParametersChannel()
    environmentParametersChannel.set_float_parameter("target_x", 9.38)
    environmentParametersChannel.set_float_parameter("target_y", 4.33)
    environmentParametersChannel.set_float_parameter("target_z", -76.13)
    environmentParametersChannel.set_float_parameter("waterEnabled", 1)
    environmentParametersChannel.set_float_parameter("fastRestart", 0)
    environmentParametersChannel.set_float_parameter("distancePlanesN", 6)
    environmentParametersChannel.set_float_parameter("safeTraining", 1)

    return [engineConfigChannel, environmentParametersChannel]
def _get_hard_env_channels() -> List[SideChannel]:
    engineConfigChannel = EngineConfigurationChannel()
    engineConfigChannel.set_configuration_parameters(width=800, height=800, quality_level=1, time_scale=1, capture_frame_rate=60)
    
    environmentParametersChannel = EnvironmentParametersChannel()
    environmentParametersChannel.set_float_parameter("target_x", 9.38)
    environmentParametersChannel.set_float_parameter("target_y", 4.33)
    environmentParametersChannel.set_float_parameter("target_z", -76.13)
    environmentParametersChannel.set_float_parameter("waterEnabled", 1)
    environmentParametersChannel.set_float_parameter("fastRestart", 0)
    environmentParametersChannel.set_float_parameter("distancePlanesN", 6)
    environmentParametersChannel.set_float_parameter("safeTraining", 1)

    return [engineConfigChannel, environmentParametersChannel]

def get_env(auvEnum: UuvEnvEnumeration) -> UnityToGymWrapper:
    unity_env = UnityEnvironment( f"aquatic_navigation_gym/uuv_envs/envs/{auvEnum.name}/SafeRLAUV", worker_id=np.random.randint(0, 1000), side_channels=_get_side_channels(auvEnum))
    env = UnityToGymWrapper(unity_env, flatten_branched=True)
    return env
    