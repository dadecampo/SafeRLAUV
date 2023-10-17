# Safe Reinforcement Learning for Autonomous Underwater Vehicles

This project aims to create a series of underwater environments in Unity, in order to test different Deep Reinforcement Learning approaches for solving the problem of an AUV's underwater cave traversal. We simulated the water using ZibraAI Liquids Pro and used ML-agents for the agent's training phase.

![](https://i.imgur.com/oY8Z1El.jpg)
>( a ) Rover overview. ( b ) In this image, you can see how the proximity sensors of the rover are organized. Thanks to these sensors, it is possible to understand the proximity to obstacles and make decisions accordingly 

# Environment Setup
> We have developed and tested this project on Windows 11, so we recommend using the same platform. Through the environment settings, it will be possible to test and replicate the training experiments. By purchasing a ZibraAI Liquids Pro license, you will also be able to modify the objects affected by the plugin within the Unity project.

1.  Download  [Anaconda](https://www.anaconda.com/distribution/#download-section)  for your System.

2.  Install Anaconda

3. Setup enviroment:
	
	- `conda create --name safeRLAUV python=3.8.16`
	- `conda activate safeRLAUV`
	- `conda install pytorch==1.7.1 torchvision==0.8.2 torchaudio==0.7.2 cudatoolkit=11.0 -c pytorch`
	- `pip3 install mlagents==0.30.0`
	- `pip3 install protobuf==3.19.6`
	- `pip3 install six`

# Repository organization
Inside the 'Trainers' folder, you will find XAML files that define hyperparameters and characteristics of the various training phases, so you will find:

 - **trainer_lesson_1.xaml**: the trainer of the first curriculum learning lesson.
 - **trainer_lesson_2.xaml**: the trainer of the second curriculum learning lesson.
 - **trainer_lesson_3.xaml**: the trainer of the first curriculum learning lesson.
 - **trainer_e2e.xaml**: the trainer of the end-to-end training.
# Training

This section has been created to allow you to reproduce the trainings we have conducted. We will provide you with the commands to execute the different phases of our training.

<img src="https://i.imgur.com/Xq0oO3o.jpg" width="678" height="400" align="center">

>In terms of the reward obtained at the end of the various training sessions, we did not observe any differences; it was possible to achieve convergence with both training methodologies, however, we will see important improvement in testing phase.

First of all, let's enter the folder where the training XAML files are located.

	cd Trainers

## Curriculum Learning
Curriculum Learning involves a structured approach to training models, where the complexity of the learning tasks is gradually increased over time. Initially, the model is exposed to simpler, more manageable examples before being challenged with progressively more difficult ones.
In our approach we subdivide training in 3 different lessons.

### Lesson 1
For the first lesson of curriculum learning, the agent will be placed inside the simplest training cave, where it will have to learn to reach the opposite end of the cave without having to worry about countering the force of the currents, as the water will be disabled.

    mlagents-learn .\trainer_lesson_1.yaml --run-id=LESSON_1 --env=../Builds/NoWater_Env/SafeRLAUV.app --width=512 --height=512

### Lesson 2
In the second lesson, our agent will be in a slightly more complex cave and will also need to learn how to move in the presence of water, which will be set at half of the strength of the final currents.

	mlagents-learn .\trainer_lesson_2.yaml --run-id=LESSON_2 --env=../Builds/HalfWater_Env/SafeRLAUV.app --width=512 --height=512 --initialize-from=LESSON_1

### Lesson 3
In the third and final lesson, the agent will have to face a training environment that includes all the complexities of a test environment, with the currents set to their maximum strength.

	mlagents-learn .\trainer_lesson_3.yaml --run-id=LESSON_3 --env=../Builds/FullWater_Env/SafeRLAUV.app --width=512 --height=512 --initialize-from=LESSON_2

## End-to-End
In End-to-End training, the agent will undergo a typical training process, after which it should be capable of successfully tackling the subsequent testing phases. In this case, the agent will face a complex environment (equivalent to that encountered during the third phase of curriculum learning), including currents at their maximum strength.

	mlagents-learn .\trainer_e2e.yaml --run-id=LESSON_E2E --env=../Builds/FullWater_Env/SafeRLAUV.app --width=512 --height=512
