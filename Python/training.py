import numpy as np

import gym
import torch
from stable_baselines3 import PPO
from stable_baselines3.ppo import MlpPolicy
import aquatic_navigation_gym

env = gym.make("aquatic_navigation_gym/FirstUuvEnv-v0")
policy_kwargs = dict(net_arch=[128, 128, 128])
model = PPO(MlpPolicy, env, verbose=1, gamma=0.99, n_steps=102_400, batch_size=1024, policy_kwargs=policy_kwargs)
model.learn(total_timesteps=1_500_000, reset_num_timesteps=False, progress_bar=True)
torch.save(model.policy.state_dict(), "prova.onnx")
#model = torch.load("prova.onnx")
obs = env.reset()
for i in range(245_760):
    action, _states = model.predict(obs, deterministic=True)
    obs, reward, done, info = env.step(action.reshape(-1)[0])
    # VecEnv resets automatically
    if done:
        obs = env.reset()

env.close()