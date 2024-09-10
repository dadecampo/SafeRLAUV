# Aquatic Navigation: A Challenging Benchmark for Deep Reinforcement Learning

This project aims to create a series of underwater and surface environments in Unity in order to test different Deep Reinforcement Learning approaches to solve the problem of autonomous agents, such as AUVs and surface drones, traversing one of these marine environments. We simulated the water using ZibraAI Liquids Pro and used ML-agents for the agent's training phase.

![](https://i.imgur.com/oY8Z1El.jpg)
>( a ) AUV overview. ( b ) In this image, you can see how the proximity sensors of the rover are organized. Thanks to these sensors, it is possible to understand the proximity to obstacles and make decisions accordingly 

# Environment Setup
> We have developed and tested this project on Windows 10/11, so we recommend using the same platform. Through the environment settings, it will be possible to test and replicate the training experiments. By purchasing, or requesting a demo of ZibraAI Liquids Pro, you will also be able to modify the objects affected by the plugin within the Unity project.
> It's necessary the use of Git LFS.

1. Download  [Anaconda](https://www.anaconda.com/)  for your System.

2. Install Anaconda

3. Clone SafeRLAUV repository using Git LFS.

   - `git lfs clone https://github.com/dadecampo/SafeRLAUV`

4. Setup enviroment:
   - `conda create --name aquatic_navigation python=3.10.12`
   - `conda activate aquatic_navigation`
   - `cd SafeRLAUV\Python`
   - `pip3 install -r requirements.txt`
   - `pip3 install -e .`

# Repository organization
Inside the 'TrainerConfig' folder, you will find XAML files that define hyperparameters and characteristics of the various training phases, so you will find:

**AUV's trainers**
 - **trainer_lesson_1.xaml**: the trainer of the first curriculum learning lesson.
 - **trainer_lesson_2.xaml**: the trainer of the second curriculum learning lesson.
 - **trainer_lesson_3.xaml**: the trainer of the first curriculum learning lesson.
 - **trainer_e2e.xaml**: the trainer of the end-to-end training.
   
**Surface drone's trainers**
 - **trainer_lesson_1_safe.yaml**: the trainer of the unique surface drone lesson, distances to obstacles measured by the agent's sensors participate in the reward calculation.
 - **trainer_lesson_2_standard.yaml**: the trainer of the unique surface drone lesson, distances to obstacles measured by the agent's sensors do not participate in the reward calculation.
   
# Training

This section has been created to allow you to reproduce the trainings we have conducted. We will provide you with the commands to execute the different phases of our training.

<img src="https://i.imgur.com/Xq0oO3o.jpg" width="678" height="400" align="center">

>AUV Training: in terms of the reward obtained at the end of the various training sessions, we did not observe any differences; it was possible to achieve convergence with both training methodologies, however, we will see important improvement in testing phase.

First of all, let's enter the folder where the training XAML files are located.

	cd TrainerConfig
## Underwater Environments
[Here](https://mega.nz/folder/tdlgmaja#osEW6iAcow4gaFUkFyLjeA) you can download the Builds!

### Curriculum Learning
Curriculum Learning involves a structured approach to training models, where the complexity of the learning tasks is gradually increased over time. Initially, the model is exposed to simpler, more manageable examples before being challenged with progressively more difficult ones.
In our approach we subdivide training in 3 different lessons.

#### Lesson 1
For the first lesson of curriculum learning, the agent will be placed inside the simplest training cave, where it will have to learn to reach the opposite end of the cave without having to worry about countering the force of the currents, as the water will be disabled.

    mlagents-learn .\AUV\trainer_lesson_1.yaml --run-id=LESSON_1 --env=../Builds/NoWater_Env/SafeRLAUV.app --width=512 --height=512

#### Lesson 2
In the second lesson, our agent will be in a slightly more complex cave and will also need to learn how to move in the presence of water, which will be set at half of the strength of the final currents.

	mlagents-learn .\AUV\trainer_lesson_2.yaml --run-id=LESSON_2 --env=../Builds/HalfWater_Env/SafeRLAUV.app --width=512 --height=512 --initialize-from=LESSON_1

#### Lesson 3
In the third and final lesson, the agent will have to face a training environment that includes all the complexities of a test environment, with the currents set to their maximum strength.

	mlagents-learn .\AUV\trainer_lesson_3.yaml --run-id=LESSON_3 --env=../Builds/FullWater_Env/SafeRLAUV.app --width=512 --height=512 --initialize-from=LESSON_2

### End-to-End
In End-to-End training, the agent will undergo a typical training process, after which it should be capable of successfully tackling the subsequent testing phases. In this case, the agent will face a complex environment (equivalent to that encountered during the third phase of curriculum learning), including currents at their maximum strength.

	mlagents-learn .\AUV\trainer_e2e.yaml --run-id=LESSON_E2E --env=../Builds/FullWater_Env/SafeRLAUV.app --width=512 --height=512

## Surface Environments
### Unique Lesson
In the only lesson designed for this surface drone, the agent must traverse an area with various obstacles, being careful not to collide with them before reaching the target area located at the opposite end of the scenario.
There are two possibilities, safe or standard training.

#### Safe

    mlagents-learn .\OnSurface\trainer_lesson_1_safe.yaml --run-id=LESSON_1 --env=../Builds/OnSurface_Env/SafeRLAUV.app --width=512 --height=512 --time-scale=1

#### Standard
    
    mlagents-learn .\OnSurface\trainer_lesson_1_standard.yaml --run-id=LESSON_1 --env=../Builds/OnSurface_Env/SafeRLAUV.app --width=512 --height=512 --time-scale=1


