# Safe Reinforcement Learning for Autonomous Underwater Vehicles

This project aims to create a series of underwater environments in Unity, in order to test different Deep Reinforcement Learning approaches for solving the problem of an AUV's underwater cave traversal. We simulated the water using ZibraAI Liquids Pro and used ML-agents for the agent's training phase.

# Repository organization
Inside the 'Trainers' folder, you will find XAML files that define hyperparameters and characteristics of the various training phases, so you will find:

 - **trainer_lesson_1.xaml**: the trainer of the first curriculum learning lesson.
 - **trainer_lesson_2.xaml**: the trainer of the second curriculum learning lesson.
 - **trainer_lesson_3.xaml**: the trainer of the first curriculum learning lesson.
 - **trainer_e2e.xaml**: the trainer of the end-to-end training.

# Training

This section has been created to allow you to reproduce the trainings we have conducted. We will provide you with the commands to execute the different phases of our training.
First of all, let's enter the folder where the training XAML files are located.

	cd Trainers

## Curriculum Learning


### Lesson 1

    mlagents-learn .\trainer_config_final_medium_lastpoint_e2e.yaml --run-id=EXPERIMENTS_MEDIUM_E2E_NN128_NOSAFE --env=../Builds/Build_Medium_FIXED/SafeRLAUV.app --time-scale=1 --quality-level=0 --width=512 --height=512 --force --base-port=2

### Lesson 2
	mlagents-learn .\trainer_config_final_medium_lastpoint_e2e.yaml --run-id=EXPERIMENTS_MEDIUM_E2E_NN128_NOSAFE --env=../Builds/Build_Medium_FIXED/SafeRLAUV.app --time-scale=1 --quality-level=0 --width=512 --height=512 --force --base-port=2

### Lesson 3
	mlagents-learn .\trainer_config_final_medium_lastpoint_e2e.yaml --run-id=EXPERIMENTS_MEDIUM_E2E_NN128_NOSAFE --env=../Builds/Build_Medium_FIXED/SafeRLAUV.app --time-scale=1 --quality-level=0 --width=512 --height=512 --force --base-port=2

## End-to-End

	mlagents-learn .\trainer_config_final_medium_lastpoint_e2e.yaml --run-id=EXPERIMENTS_MEDIUM_E2E_NN128_NOSAFE --env=../Builds/Build_Medium_FIXED/SafeRLAUV.app --time-scale=1 --quality-level=0 --width=512 --height=512 --force --base-port=2
