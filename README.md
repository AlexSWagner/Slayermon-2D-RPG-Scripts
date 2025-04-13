# Slayermon

## Introduction

In Slayermon, a 2D RPG game I developed, you take on the role of a ninja tasked with slaying monsters across a small yet diverse game world. This project is a showcase of my programming skills and my understanding of game development principles. Through creating this game, I implemented core systems like player movement, combat, quests, NPC interactions, and teleportation, all from scratch to highlight my technical abilities. It’s not a finished product, but rather a portfolio piece to show what I've learned.

## Features

- **Player Movement**: Smooth and responsive controls for navigating the game world.
- **Combat System**: A weapon-based system with projectile mechanics, like arrows, to fight enemies.
- **Enemy AI**: Monsters that patrol and engage the player in combat.
- **NPC Interactions**: A dialogue system for interacting with non-player characters.
- **Quest System**: Tools for accepting and tracking quests as you explore.
- **Teleportation**: Seamless transitions between areas like the village, cave, and forest.
- **Camera System**: A dynamic camera that follows the player’s movements.
- **Screen Transitions**: Fading effects for smooth scene changes or game states.

## Technical Details

In developing Slayermon, I used the Unity game engine with C# as my scripting language. This project reflects my proficiency in several technical areas that I’m proud to showcase:

- **Design Patterns**: I implemented the Singleton pattern for manager classes like `QuestManager` and `ArrowManager` to centralize control and ensure global access across the game.
- **AI State Machines**: For the enemies, I designed a finite state machine in `EnemyScript.cs` to manage behaviors like patrolling, chasing, and attacking, giving them a sense of life.
- **Event-Driven Programming**: I leveraged Unity’s event system to handle interactions, such as triggering dialogues or starting quests based on player actions.
- **Performance Optimization**: To keep things efficient, I used object pooling in `ArrowManager.cs` for arrow projectiles, cutting down on instantiation overhead during combat.
- **UI Management**: I built a dynamic quest journal UI in `QuestJournal.cs` that updates in real-time as quests progress.

These elements demonstrate my ability to tackle complex systems while keeping performance and scalability in mind—skills I’ve honed through this project.

## Scripts Overview

Here’s a rundown of the scripts I wrote for Slayermon and what they do:

- **`PlayerScript.cs`**: Manages player-specific logic, like health or inventory tracking.
- **`QuestJournal.cs`**: Powers the quest log UI, showing active and completed quests.
- **`QuestManager.cs`**: The central hub for managing quest states and progression.
- **`ScreenFader.cs`**: Handles screen fading for transitions, like entering a new area.
- **`TeleportTrigger.cs`**: Controls teleportation points that move the player between locations.
- **`WeaponScript.cs`**: Defines how weapons work, including attacks and damage.
- **`ArrowManager.cs`**: Manages a pool of arrow projectiles for efficient reuse.
- **`arrowScript.cs`**: Controls the movement and collision of individual arrows.
- **`CameraFollow.cs`**: Keeps the camera locked on the player with smooth tracking.
- **`DialogueManager.cs`**: Runs the dialogue system for NPC conversations.
- **`EnemyScript.cs`**: Implements enemy AI and combat behavior.
- **`NPCScript.cs`**: Handles NPC logic, like triggering dialogues or quests.
- **`PlayerMovement.cs`**: Drives the player’s movement based on input and physics.

For more details on the NPC and teleportation systems, check out the separate `NPCandTeleportSystem_README.md` in the repository.

## Future Improvements

While Slayermon serves its purpose as a portfolio piece, I’ve got ideas for taking it further:

- Adding more areas to expand the game world.
- Creating a deeper quest system with branching narratives.
- Introducing new enemy types with unique abilities.
- Upgrading combat with combos or special ninja moves.
- Adding sound effects and music for atmosphere.
- Optimizing performance to handle bigger levels and more entities.
