import numpy as np
import torch

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel

from stable_baselines3 import PPO
from stable_baselines3.ppo import MlpPolicy
from stable_baselines3.common.vec_env import DummyVecEnv
from stable_baselines3.common.env_util import make_vec_env

from environment_factory import get_env
from environment_factory import AuvEnvEnumeration

env = get_env(auvEnum = AuvEnvEnumeration.Gym_Env)
policy_kwargs = dict(net_arch=[128, 128, 128])
model = PPO(MlpPolicy, env, verbose=1, gamma=0.99, n_steps=98304, batch_size=1024, policy_kwargs=policy_kwargs)
model.learn(total_timesteps=983040, reset_num_timesteps=False, progress_bar=True)
torch.save(model.policy.state_dict(), "prova.onnx")
#model = torch.load("prova.onnx")
obs = env.reset()
for i in range(245760):
    action, _states = model.predict(obs, deterministic=True)
    obs, reward, done, info = env.step(action.reshape(-1)[0])
    # VecEnv resets automatically
    if done:
        obs = env.reset()

env.close()