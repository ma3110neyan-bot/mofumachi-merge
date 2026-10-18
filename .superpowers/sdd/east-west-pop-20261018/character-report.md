# Task 5 resident presence

Implemented in the commit adding this report; the worker handoff supplies its exact hash.

- TownActor owns soft/contact shadows at the navigation feet, with lift-dependent spread and opacity. Depth scales artwork and shadows from map Y/UV (.94–1.06), independently of camera pan. Existing world-Y ordering remains in TownLifeView.
- Small seeded breathing, sway, hand movement and looking coexist with gait, smooth facing, blink and approved reaction strips. Reset clears gait/reactions/social state and restores grounded depth scale; paused frames do not advance motion. Bind/Pose signatures remain available.
- CharacterBodyGraphic retains every atlas coordinate and expression strip. A tested finite deformation moves the head as one piece above the neck; it never mirrors UVs or scales X negatively.
- Atlas byte audit matches HEAD: Git blob `24d3fccfcaab3cefd14045e24c598360cfee277e`; SHA-256 `23d14025f146c429da01b6e3a74ef803254755f8d149f9eb53516aacaa30163d`.

Validation:

- RED: the resting-sway test failed on the prior implementation (0 degrees); a face-preservation check also failed before moving the head uniformly. New API tests initially failed compilation before their implementation.
- GREEN: 10/10 focused pure checks (eight new tests plus existing gait/blink). TRX: `/tmp/resident-presence-oct18/results/character-final.trx`. Tests use the actual product sources through an isolated copy of the Presentation test project, avoiding shared obj directories.
- All six asmdefs compiled against the real Unity 6000.6.4f1 references with zero warnings/errors, including four new native tests for shadow grounding/reactions, camera-independent depth, pause/disable reset, and finite nonmirrored geometry for all 18 approved atlas entries. Outputs: `/tmp/resident-presence-oct18/asmdef-compat`.
- Interim full Presentation run: 114 passed / 20 failed while the parent's map rewrite was in progress; results sent to parent in `/tmp/resident-presence-oct18/results/presentation-suite.trx`. Failed cases: `EveryDestinationIsOnTheAuthoredFloorAndReachableAtEveryLevel` (0/6); `BothFountainRoutesAndTheEntireTownAreConnectedWithoutCuttingAcrossWater`; `CafeAndQuestGardenFencesAreObstaclesInsteadOfShortcuts`; `EveryStableDestinationAndBothPhysicalEntrancesHaveAFloorSafeRoute` (0/6); `FirstFlowersAndLevelThreeLampsAreVisibleFromTheRegionalPhoneView` (West/Central/East); `NewMapHasBroadPavingOnBothSidesOfTheFountainAndAtBothCabins`; `AThinGapBetweenPaintedFloorAndConnectorCannotBeSkippedBySampling`; `AvoidanceDoesNotLoseTheRouteAndLoopAroundAnObstacle` (.02,.57,.9,-.33); `EveryUnlockedPlaceHasAnObstacleSafeRouteFromThePark` (0/6); `NeighbouringFloorTargetsHaveDifferentRoutesAndAllThreeAreasConnect`; `OpposingResidentsPassWithoutPushingAStationaryResidentOrGettingStuck`; `ParkHasAWalkableSurfaceOnBothSidesOfTheFountain`; `RoutesGoAroundTheFountainAndStayInsideTheAuthoredFloor`; `FastTravelIsDistinctFromCompletedWalkingAndCameraRemainsFinite`; `PaintedConnectingRoadsContinueToTheNeighbourInsteadOfStoppingAtTheEdge`.

Integration: parent agreed to remove TownLifeView's old single ground shadow and add a portrait-index tie-break to its existing world-Y sort. This worker did not edit map, UI, audio, Core, masters or atlas assets.

Native PlayMode execution/captures and physical animation review remain pending because this cloud Editor has no license. Compilation is not native visual evidence.
