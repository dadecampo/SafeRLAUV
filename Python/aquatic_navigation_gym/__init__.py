from gym.envs.registration import register

register(
    id="aquatic_navigation_gym/EasyUuvEnv-v0",
    entry_point="aquatic_navigation_gym.uuv_envs.envs:EasyUuvEnv",
)

register(
    id="aquatic_navigation_gym/MediumUuvEnv-v0",
    entry_point="aquatic_navigation_gym.uuv_envs.envs:MediumUuvEnv",
)

register(
    id="aquatic_navigation_gym/HardUuvEnv-v0",
    entry_point="aquatic_navigation_gym.uuv_envs.envs:HardUuvEnv",
)