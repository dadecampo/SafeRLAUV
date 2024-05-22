import numpy as np

import gym
import torch
from stable_baselines3 import PPO
from stable_baselines3.ppo import MlpPolicy
import aquatic_navigation_gym

env = gym.make("aquatic_navigation_gym/EasyUuvEnv-v0")
policy_kwargs = dict(activation_fn=torch.nn.ReLU, net_arch=[128, 128, 128])
model = PPO(MlpPolicy, env, verbose=1, gamma=0.99, n_steps=20_480, batch_size=1024, policy_kwargs=policy_kwargs)
#torch.save(model.policy.state_dict(), "prova.onnx")
#model.policy.load_state_dict(torch.load("prova.onnx"))
model.learn(total_timesteps=1_000_000, reset_num_timesteps=False, progress_bar=True)
torch.save(model.policy.state_dict(), "easy.onnx")
obs = env.reset()
for i in range(245_760):
    action, _states = model.predict(obs)
    obs, reward, done, info = env.step(action.reshape(-1)[0])
    # VecEnv resets automatically
    if done:
        obs = env.reset()

env.close()