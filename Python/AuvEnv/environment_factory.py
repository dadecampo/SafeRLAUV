import numpy as np
from enum import Enum
from typing import List

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper
from mlagents_envs.environment import SideChannel
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel

class AuvEnvEnumeration(Enum):
    Gym_Env = 1
    FirstEnv = 1
    SecondEnv = 2
    ThirdEnv = 2
    
def _get_side_channels(auvEnum: AuvEnvEnumeration) -> List[SideChannel]:
    if (auvEnum == AuvEnvEnumeration.Gym_Env):
        return _get_first_env_channels()
    elif (auvEnum == AuvEnvEnumeration.FirstEnv):
        return _get_first_env_channels()
    elif (auvEnum == AuvEnvEnumeration.SecondEnv):
        return _get_second_env_channels()
    else:
        return _get_third_env_channels()
        
def _get_first_env_channels() -> List[SideChannel]:
    engineConfigChannel = EngineConfigurationChannel()
    engineConfigChannel.set_configuration_parameters(width=800, height=800, quality_level=1, time_scale=10.,
                                                    target_frame_rate=-1, capture_frame_rate=60)
    
    environmentParametersChannel = EnvironmentParametersChannel()
    environmentParametersChannel.set_float_parameter("target_x", 33.66)
    environmentParametersChannel.set_float_parameter("target_y", 6.29)
    environmentParametersChannel.set_float_parameter("target_z", -76.13)
    environmentParametersChannel.set_float_parameter("waterEnabled", 0)
    environmentParametersChannel.set_float_parameter("fastRestart", 0)
    environmentParametersChannel.set_float_parameter("distancePlanesN", 6)
    environmentParametersChannel.set_float_parameter("safeTraining", 1)

    return [engineConfigChannel, environmentParametersChannel]

def _get_second_env_channels() -> List[SideChannel]:
    
    engineConfigChannel = EngineConfigurationChannel()
    engineConfigChannel.set_configuration_parameters(width=800, height=800, quality_level=1, time_scale=10.,
                                                    target_frame_rate=-1, capture_frame_rate=60)
    
    environmentParametersChannel = EnvironmentParametersChannel()
    environmentParametersChannel.set_float_parameter("target_x", 9.38)
    environmentParametersChannel.set_float_parameter("target_y", 4.33)
    environmentParametersChannel.set_float_parameter("target_z", -76.13)
    environmentParametersChannel.set_float_parameter("waterEnabled", 1)
    environmentParametersChannel.set_float_parameter("fastRestart", 0)
    environmentParametersChannel.set_float_parameter("distancePlanesN", 6)
    environmentParametersChannel.set_float_parameter("safeTraining", 1)

    return [engineConfigChannel, environmentParametersChannel]
def _get_third_env_channels() -> List[SideChannel]:
    engineConfigChannel = EngineConfigurationChannel()
    engineConfigChannel.set_configuration_parameters(width=800, height=800, quality_level=1, time_scale=10.,
                                                    target_frame_rate=-1, capture_frame_rate=60)
    
    environmentParametersChannel = EnvironmentParametersChannel()
    environmentParametersChannel.set_float_parameter("target_x", 9.38)
    environmentParametersChannel.set_float_parameter("target_y", 4.33)
    environmentParametersChannel.set_float_parameter("target_z", -76.13)
    environmentParametersChannel.set_float_parameter("waterEnabled", 1)
    environmentParametersChannel.set_float_parameter("fastRestart", 0)
    environmentParametersChannel.set_float_parameter("distancePlanesN", 6)
    environmentParametersChannel.set_float_parameter("safeTraining", 1)

    return [engineConfigChannel, environmentParametersChannel]

def get_env(auvEnum: AuvEnvEnumeration) -> UnityToGymWrapper:
    unity_env = UnityEnvironment( f"../Builds/{auvEnum.name}/SafeRLAUV", worker_id=np.random.randint(0, 1000), side_channels=_get_side_channels(auvEnum))
    env = UnityToGymWrapper(unity_env, flatten_branched=True)
    return env
    