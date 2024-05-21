from gym.envs.registration import register

register(
    id="aquatic_navigation_gym/FirstUuvEnv-v0",
    entry_point="aquatic_navigation_gym.uuv_envs.envs:FirstUuvEnv",
)

register(
    id="aquatic_navigation_gym/SecondUuvEnv-v0",
    entry_point="aquatic_navigation_gym.uuv_envs.envs:SecondUuvEnv",
)

register(
    id="aquatic_navigation_gym/ThirdUuvEnv-v0",
    entry_point="aquatic_navigation_gym.uuv_envs.envs:ThirdUuvEnv",
)