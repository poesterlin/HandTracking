# HandTracking

This repo is part of the code for my masters project. The task is to build a Unity environment to compare teleportation methods using the hand-tracking technology of the Oculus Quest.

## Classes

Descriptions and the location of all custom classes:

| **File**                                                                         | **Class**                            | **Description**                                                           |
| -------------------------------------------------------------------------------- | ------------------------------------ | ------------------------------------------------------------------------- |
| [Flask.cs ](./Assets/Scripts/Flask.cs)                                           | Flask                                | Class managing the behaviour of magic flasks in the forest scene.         |
| [ForrestStudyObserver.cs ](./Assets/Scripts/ForrestStudyObserver.)               | TeleportRecord                       | Record of a teleport that is send to the server                           |
| [ForrestStudyObserver.cs ](./Assets/Scripts/ForrestStudyObserver.)               | ForrestStudyObserver                 | Collects statistics and sets parameters during the forest scene.          |
| [GestureRecognizer.cs ](./Assets/Scripts/GestureRecognizer.cs)                   | GestureList                          | Array of gestures for serialization                                       |
| [GestureRecognizer.cs ](./Assets/Scripts/GestureRecognizer.cs)                   | Gesture                              | Information about one stored gesture. Manly based on bones.               |
| [GestureRecognizer.cs ](./Assets/Scripts/GestureRecognizer.cs)                   | Bone                                 | Information about one stored joint                                        |
| [GestureRecognizer.cs ](./Assets/Scripts/GestureRecognizer.cs)                   | GestureRecognizer                    | Runs recognition algorithm to identify the current gesture.               |
| [HandTrackingGrabbable.cs ](./Assets/Scripts/HandTrackingGrabbable.cs)           | HandTrackingGrabbable : OVRGrabbable | Script that can be placed on an object to be able to grab it.             |
| [HandTrackingGrabber.cs ](./Assets/Scripts/HandTrackingGrabber.cs)               | HandTrackingGrabber : OVRGrabber     | Script that can be placed on a hand to be able to grab things.            |
| [Mortar.cs ](./Assets/Scripts/Mortar.cs)                                         | Mortar                               | Starts animation and sound effects if a Flask is put in.                  |
| [NetworkAdapter.cs ](./Assets/Scripts/NetworkAdapter.cs)                         | NetworkAdapter                       | Provides network helpers.                                                 |
| [PathIntegrationStudyObserver.cs](./Assets/Scripts/PathIntegrationStudyObserver) | PathIntegrationStudyObserver         | Collects statistics and sets parameters during the path integration study |
| [PotionManager.cs ](./Assets/Scripts/PotionManager.cs)                           | PotionManager                        | Manages flask positions                                                   |
| [QuestDebug.cs ](./Assets/Scripts/QuestDebug.cs)                                 | QuestDebug                           | Debug helper for in-scene or network logs.                                |
| [Reticle.cs ](./Assets/Scripts/Reticle.cs)                                       | Reticle                              | Manager for the teleportation reticle.                                    |
| [Setup.cs ](./Assets/Scripts/Setup.cs)                                           | Setup                                | Settings script.                                                          |
| [Teleporters.cs ](./Assets/Scripts/Teleporters.cs)                               | TrackingInfo                         | Information about the current position of hands, the headset and fingers  |
| [Teleporters.cs ](./Assets/Scripts/Teleporters.cs)                               | FingerTeleport : Teleporter          | Teleporter for the index gesture.                                         |
| [Teleporters.cs ](./Assets/Scripts/Teleporters.cs)                               | PalmTeleport : Teleporter            | Teleporter for the palm gesture.                                          |
| [Teleporters.cs ](./Assets/Scripts/Teleporters.cs)                               | TriangleTeleport : Teleporter        | Teleporter for the triangle gesture.                                      |
| [TeleportProvider.cs ](./Assets/Scripts/TeleportProvider.cs)                     | abstract Teleporter                  | Base class for the teleporters.                                           |
| [TeleportProvider.cs ](./Assets/Scripts/TeleportProvider.cs)                     | TeleportProvider                     | Manages the current teleporter.                                           |
| [Tutorial.cs ](./Assets/Scripts/Tutorial.cs)                                     | Tutorial                             | Displays the current gesture tutorial                                     |
