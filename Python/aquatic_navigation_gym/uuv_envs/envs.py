import gym
from aquatic_navigation_gym.uuv_envs.environment_factory import get_env
from aquatic_navigation_gym.uuv_envs.environment_factory import UuvEnvEnumeration

class UuvEnv(gym.Env):
    def __init__(self, auvEnum: UuvEnvEnumeration):
        super(UuvEnv, self).__init__()
        self.env = get_env(auvEnum = auvEnum)
        self.action_space = self.env.action_space
        self.observation_space = self.env.observation_space

    def reset(self):
        return self.env.reset()

    def step(self, action):
        return self.env.step(action)
    
    
class EasyUuvEnv(UuvEnv):
    def __init__(self):
        super(EasyUuvEnv, self).__init__(UuvEnvEnumeration.EasyEnv)
        
class MediumUuvEnv(UuvEnv):
    def __init__(self):
        super(MediumUuvEnv, self).__init__(UuvEnvEnumeration.MediumEnv)
        
class HardUuvEnv(UuvEnv):
    def __init__(self):
        super(HardUuvEnv, self).__init__(UuvEnvEnumeration.HardEnv)
    
