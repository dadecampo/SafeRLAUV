import numpy as np

import gym
import torch
from stable_baselines3 import PPO
from stable_baselines3.ppo import MlpPolicy
import aquatic_navigation_gym

model_name= "model.onnx"
n_steps_update = 20_480 
total_n_steps = 1_000_000
batch_size = 2048

### ENVIRONMENT INITIALIZATION ###
env = gym.make("aquatic_navigation_gym/EasyUuvEnv-v0")

### MODEL DEFINITION ###
model = PPO(MlpPolicy, env, verbose=1, gamma=0.99, n_steps=n_steps_update, batch_size=batch_size)

### TRAINING ###
model.learn(total_timesteps=total_n_steps, reset_num_timesteps=False, progress_bar=True)
torch.save(model.policy.state_dict(), model_name)

### TESTING ###
model.policy.load_state_dict(torch.load(model_name))
obs = env.reset()
for i in range(1000):
    action, _ = model.predict(obs)
    obs, reward, done, info = env.step(action.reshape(-1)[0])
    if done:
        obs = env.reset()

env.close()