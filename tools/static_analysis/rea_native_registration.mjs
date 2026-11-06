import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {spawn, spawnSync} from 'node:child_process';
import os from 'node:os';
import {createHash, randomUUID} from 'node:crypto';
import {startOwnedMemoryProfile} from './rea_owned_memory_profile.mjs';
import {parserBudget, calibrationBudget, gameMouseCalibrationBudget, clientMouseCalibrationBudget, checkResourceBudget, readHostResourceSnapshot} from './rea_resource_budget.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const verifyR0ToolBaseline = process.argv.length === 3 && process.argv[2] === '--verify-r0-tool-baseline';
const r0ToolBaseline = process.argv.length === 3 && (process.argv[2] === '--r0-tool-baseline' || verifyR0ToolBaseline);
const verifyR0ToolBaselineTerminalRetry = process.argv.length === 3 && process.argv[2] === '--verify-r0-tool-baseline-terminal-retry';
const r0ToolBaselineTerminalRetry = process.argv.length === 3 && (process.argv[2] === '--r0-tool-baseline-terminal-retry' || verifyR0ToolBaselineTerminalRetry);
const verifyR1EngineInput = process.argv.length === 3 && process.argv[2] === '--verify-r1-engine-input';
const r1EngineInput = process.argv.length === 3 && (process.argv[2] === '--r1-engine-input' || verifyR1EngineInput);
const verifyR1EngineMouseCore = process.argv.length === 3 && process.argv[2] === '--verify-r1-engine-mouse-core';
const r1EngineMouseCore = process.argv.length === 3 && (process.argv[2] === '--r1-engine-mouse-core' || verifyR1EngineMouseCore);
const verifyR1GameMouseCalibration = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-mouse-calibration';
const r1GameMouseCalibration = process.argv.length === 3 && (process.argv[2] === '--r1-game-mouse-calibration' || verifyR1GameMouseCalibration);
const verifyR1GameMouseLinks = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-mouse-links';
const r1GameMouseLinks = process.argv.length === 3 && (process.argv[2] === '--r1-game-mouse-links' || verifyR1GameMouseLinks);
const verifyR1GameMouseState = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-mouse-state';
const r1GameMouseState = process.argv.length === 3 && (process.argv[2] === '--r1-game-mouse-state' || verifyR1GameMouseState);
const verifyR1GameStateTransition = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-state-transition';
const r1GameStateTransition = process.argv.length === 3 && (process.argv[2] === '--r1-game-state-transition' || verifyR1GameStateTransition);
const verifyR1LevelObjectRecords = process.argv.length === 3 && process.argv[2] === '--verify-r1-level-object-records';
const r1LevelObjectRecords = process.argv.length === 3 && (process.argv[2] === '--r1-level-object-records' || verifyR1LevelObjectRecords);
const verifyR1EngineMouseGetters = process.argv.length === 3 && process.argv[2] === '--verify-r1-engine-mouse-getters';
const r1EngineMouseGetters = process.argv.length === 3 && (process.argv[2] === '--r1-engine-mouse-getters' || verifyR1EngineMouseGetters);
const verifyR1MoveProductionEngine = process.argv.length === 3 && process.argv[2] === '--verify-r1-move-production-engine';
const r1MoveProductionEngine = process.argv.length === 3 && (process.argv[2] === '--r1-move-production-engine' || verifyR1MoveProductionEngine);
const verifyR1EngineAnimationSpeed = process.argv.length === 3 && process.argv[2] === '--verify-r1-engine-animation-speed';
const r1EngineAnimationSpeed = process.argv.length === 3 && (process.argv[2] === '--r1-engine-animation-speed' || verifyR1EngineAnimationSpeed);
const verifyR1MoveWorldDistance = verifyR1EngineAnimationSpeed || (process.argv.length === 3 && process.argv[2] === '--verify-r1-move-world-distance');
const r1MoveWorldDistance = r1EngineAnimationSpeed || (process.argv.length === 3 && (process.argv[2] === '--r1-move-world-distance' || verifyR1MoveWorldDistance));
const verifyR1MoveActionDispatchLeaves = process.argv.length === 3 && process.argv[2] === '--verify-r1-move-action-dispatch-leaves';
const r1MoveActionDispatchLeaves = process.argv.length === 3 && (process.argv[2] === '--r1-move-action-dispatch-leaves' || verifyR1MoveActionDispatchLeaves);
const verifyR1GameWaypointAnimation = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-waypoint-animation';
const r1GameWaypointAnimation = process.argv.length === 3 && (process.argv[2] === '--r1-game-waypoint-animation' || verifyR1GameWaypointAnimation);
const verifyR1GameAnimationSetNames = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-animation-set-names';
const r1GameAnimationSetNames = process.argv.length === 3 && (process.argv[2] === '--r1-game-animation-set-names' || verifyR1GameAnimationSetNames);
const verifyR1GameAnimationSetProduction = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-animation-set-production';
const r1GameAnimationSetProduction = process.argv.length === 3 && (process.argv[2] === '--r1-game-animation-set-production' || verifyR1GameAnimationSetProduction);
const verifyR1GameAssociatedIdentityRepair = verifyR1GameAnimationSetProduction || verifyR1GameAnimationSetNames || verifyR1GameWaypointAnimation || (process.argv.length === 3 && process.argv[2] === '--verify-r1-game-associated-identity-repair');
const r1GameAssociatedIdentityRepair = r1GameAnimationSetProduction || r1GameAnimationSetNames || r1GameWaypointAnimation || (process.argv.length === 3 && (process.argv[2] === '--r1-game-associated-identity-repair' || verifyR1GameAssociatedIdentityRepair));
const verifyR1GameAssociatedRetry = verifyR1GameAssociatedIdentityRepair || (process.argv.length === 3 && process.argv[2] === '--verify-r1-game-associated-retry');
const r1GameAssociatedRetry = r1GameAssociatedIdentityRepair || (process.argv.length === 3 && (process.argv[2] === '--r1-game-associated-retry' || verifyR1GameAssociatedRetry));
const verifyR1GameAssociated = verifyR1GameAssociatedRetry || (process.argv.length === 3 && process.argv[2] === '--verify-r1-game-associated');
const r1GameAssociated = r1GameAssociatedRetry || (process.argv.length === 3 && (process.argv[2] === '--r1-game-associated' || verifyR1GameAssociated));
const verifyR1DurationManagerDirect = process.argv.length === 3 && process.argv[2] === '--verify-r1-duration-manager-direct';
const r1DurationManagerDirect = process.argv.length === 3 && (process.argv[2] === '--r1-duration-manager-direct' || verifyR1DurationManagerDirect);
const verifyR1DurationManagerConstructor = process.argv.length === 3 && process.argv[2] === '--verify-r1-duration-manager-constructor';
const r1DurationManagerConstructor = process.argv.length === 3 && (process.argv[2] === '--r1-duration-manager-constructor' || verifyR1DurationManagerConstructor);
const verifyR1GameRunSpeedDirect = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-run-speed-direct';
const r1GameRunSpeedDirect = process.argv.length === 3 && (process.argv[2] === '--r1-game-run-speed-direct' || verifyR1GameRunSpeedDirect);
const verifyR1GameRunSpeed = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-run-speed';
const r1GameRunSpeed = process.argv.length === 3 && (process.argv[2] === '--r1-game-run-speed' || verifyR1GameRunSpeed);
const verifyR1GameStateLookupDirect = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-state-lookup-direct';
const r1GameStateLookupDirect = process.argv.length === 3 && (process.argv[2] === '--r1-game-state-lookup-direct' || verifyR1GameStateLookupDirect);
const verifyR1GameMainPlayerConnectionMethods = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-mainplayer-connection-methods';
const r1GameMainPlayerConnectionMethods = process.argv.length === 3 && (process.argv[2] === '--r1-game-mainplayer-connection-methods' || verifyR1GameMainPlayerConnectionMethods);
const verifyR1GameMainPlayerConnection = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-mainplayer-connection';
const r1GameMainPlayerConnection = process.argv.length === 3 && (process.argv[2] === '--r1-game-mainplayer-connection' || verifyR1GameMainPlayerConnection);
const verifyR1GamePlayerVideoDirect = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-player-video-direct';
const r1GamePlayerVideoDirect = process.argv.length === 3 && (process.argv[2] === '--r1-game-player-video-direct' || verifyR1GamePlayerVideoDirect);
const verifyR1GameMouseDirectDependencies = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-mouse-direct-dependencies';
const r1GameMouseDirectDependencies = process.argv.length === 3 && (process.argv[2] === '--r1-game-mouse-direct-dependencies' || verifyR1GameMouseDirectDependencies);
const verifyR1GameSkillReferenceDirect = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-skill-reference-direct';
const r1GameSkillReferenceDirect = process.argv.length === 3 && (process.argv[2] === '--r1-game-skill-reference-direct' || verifyR1GameSkillReferenceDirect);
const verifyR1GameStopStateDirect = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-stop-state-direct';
const r1GameStopStateDirect = process.argv.length === 3 && (process.argv[2] === '--r1-game-stop-state-direct' || verifyR1GameStopStateDirect);
const verifyR1GameReachedGoalDependencies = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-reached-goal-dependencies';
const r1GameReachedGoalDependencies = process.argv.length === 3 && (process.argv[2] === '--r1-game-reached-goal-dependencies' || verifyR1GameReachedGoalDependencies);
const verifyR1EngineReachedGoalDependencies = process.argv.length === 3 && process.argv[2] === '--verify-r1-engine-reached-goal-dependencies';
const r1EngineReachedGoalDependencies = process.argv.length === 3 && (process.argv[2] === '--r1-engine-reached-goal-dependencies' || verifyR1EngineReachedGoalDependencies);
// Shared pinned runtime guards for reached-goal queries and their fixed direct leaves.
const r1ReachedGoalDependencies = r1LevelObjectRecords || r1MoveWorldDistance || r1MoveProductionEngine || r1EngineMouseGetters || r1GameAssociated || r1MoveActionDispatchLeaves || r1DurationManagerDirect || r1DurationManagerConstructor || r1GameRunSpeedDirect || r1GameRunSpeed || r1GameStateLookupDirect || r1GameMainPlayerConnectionMethods || r1GameMainPlayerConnection || r1GamePlayerVideoDirect || r1GameMouseDirectDependencies || r1GameSkillReferenceDirect || r1GameStopStateDirect || r1GameReachedGoalDependencies || r1EngineReachedGoalDependencies;
const verifyR1WorldPositionLevelInitialization = process.argv.length === 3 && process.argv[2] === '--verify-r1-world-position-and-level-initialization';
const r1WorldPositionLevelInitialization = process.argv.length === 3 && (process.argv[2] === '--r1-world-position-and-level-initialization' || verifyR1WorldPositionLevelInitialization);
const verifyR1SkillMovedDirect = process.argv.length === 3 && process.argv[2] === '--verify-r1-skill-moved-direct';
const r1SkillMovedDirect = process.argv.length === 3 && (process.argv[2] === '--r1-skill-moved-direct' || verifyR1SkillMovedDirect);
const verifyR1TerrainTypeMaterial = process.argv.length === 3 && process.argv[2] === '--verify-r1-terrain-type-material';
const r1TerrainTypeMaterial = process.argv.length === 3 && (process.argv[2] === '--r1-terrain-type-material' || verifyR1TerrainTypeMaterial);
const verifyR1WaterInitialization = process.argv.length === 3 && process.argv[2] === '--verify-r1-water-initialization';
const r1WaterInitialization = process.argv.length === 3 && (process.argv[2] === '--r1-water-initialization' || verifyR1WaterInitialization);
const verifyR1GroundResourcesWater = process.argv.length === 3 && process.argv[2] === '--verify-r1-ground-resources-and-water';
const r1GroundResourcesWater = process.argv.length === 3 && (process.argv[2] === '--r1-ground-resources-and-water' || verifyR1GroundResourcesWater);
const verifyR1GroundFaceUnlink = process.argv.length === 3 && process.argv[2] === '--verify-r1-ground-face-and-unlink';
const r1GroundFaceUnlink = process.argv.length === 3 && (process.argv[2] === '--r1-ground-face-and-unlink' || verifyR1GroundFaceUnlink);
const verifyR1GroundIntersectionRemaining = process.argv.length === 3 && process.argv[2] === '--verify-r1-ground-intersection-remaining';
const r1GroundIntersectionRemaining = process.argv.length === 3 && (process.argv[2] === '--r1-ground-intersection-remaining' || verifyR1GroundIntersectionRemaining);
const verifyR1GroundIntersectionDirect = process.argv.length === 3 && process.argv[2] === '--verify-r1-ground-intersection-direct';
const r1GroundIntersectionDirect = process.argv.length === 3 && (process.argv[2] === '--r1-ground-intersection-direct' || verifyR1GroundIntersectionDirect);
const verifyR1GroundRayTree = process.argv.length === 3 && process.argv[2] === '--verify-r1-ground-ray-tree';
const r1GroundRayTree = process.argv.length === 3 && (process.argv[2] === '--r1-ground-ray-tree' || verifyR1GroundRayTree);
const verifyR1GroundFloorGeometry = process.argv.length === 3 && process.argv[2] === '--verify-r1-ground-floor-geometry';
const r1GroundFloorGeometry = process.argv.length === 3 && (process.argv[2] === '--r1-ground-floor-geometry' || verifyR1GroundFloorGeometry);
const verifyR1GroundBlocksNodeDestroy = process.argv.length === 3 && process.argv[2] === '--verify-r1-ground-blocks-and-node-destroy';
const r1GroundBlocksNodeDestroy = process.argv.length === 3 && (process.argv[2] === '--r1-ground-blocks-and-node-destroy' || verifyR1GroundBlocksNodeDestroy);
const verifyR1SpatialGroundDirect = process.argv.length === 3 && process.argv[2] === '--verify-r1-spatial-ground-direct';
const r1SpatialGroundDirect = process.argv.length === 3 && (process.argv[2] === '--r1-spatial-ground-direct' || verifyR1SpatialGroundDirect);
const verifyR1EngineSpatialGround = process.argv.length === 3 && process.argv[2] === '--verify-r1-engine-spatial-ground';
const r1EngineSpatialGround = process.argv.length === 3 && (process.argv[2] === '--r1-engine-spatial-ground' || verifyR1EngineSpatialGround);
const verifyR1EngineEntityPublicationCore = process.argv.length === 3 && process.argv[2] === '--verify-r1-engine-entity-publication-core';
const r1EngineEntityPublicationCore = process.argv.length === 3 && (process.argv[2] === '--r1-engine-entity-publication-core' || verifyR1EngineEntityPublicationCore);
const verifyR1SameRegionMovement = process.argv.length === 3 && process.argv[2] === '--verify-r1-same-region-movement';
const r1SameRegionMovement = process.argv.length === 3 && (process.argv[2] === '--r1-same-region-movement' || verifyR1SameRegionMovement);
const verifyR1PositionPublication = process.argv.length === 3 && process.argv[2] === '--verify-r1-position-publication';
const r1PositionPublication = process.argv.length === 3 && (process.argv[2] === '--r1-position-publication' || verifyR1PositionPublication);
const verifyR1GameLevelPublicationTail = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-level-publication-tail';
const r1GameLevelPublicationTail = process.argv.length === 3 && (process.argv[2] === '--r1-game-level-publication-tail' || verifyR1GameLevelPublicationTail);
const verifyR1GameEntityPublicationGates = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-entity-publication-gates';
const r1GameEntityPublicationGates = process.argv.length === 3 && (process.argv[2] === '--r1-game-entity-publication-gates' || verifyR1GameEntityPublicationGates);
const verifyR1GameMovementMath = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-movement-math';
const r1GameMovementMath = process.argv.length === 3 && (process.argv[2] === '--r1-game-movement-math' || verifyR1GameMovementMath);
const verifyR1GameCallbackRemainingRetry = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-callback-remaining-retry';
const r1GameCallbackRemainingRetry = process.argv.length === 3 && (process.argv[2] === '--r1-game-callback-remaining-retry' || verifyR1GameCallbackRemainingRetry);
const verifyR1ActorRadius = process.argv.length === 3 && process.argv[2] === '--verify-r1-actor-radius';
const r1ActorRadius = process.argv.length === 3 && (process.argv[2] === '--r1-actor-radius' || verifyR1ActorRadius);
const postRestartObservationMode = r1ReachedGoalDependencies || r1WorldPositionLevelInitialization || r1TerrainTypeMaterial || r1WaterInitialization || r1GroundResourcesWater || r1GroundFaceUnlink || r1GroundIntersectionRemaining || r1GroundIntersectionDirect || r1GroundRayTree || r1GroundFloorGeometry || r1GroundBlocksNodeDestroy || r1SpatialGroundDirect || r1EngineSpatialGround || r1EngineEntityPublicationCore || r1SameRegionMovement || r1PositionPublication || r1SkillMovedDirect || r1GameLevelPublicationTail || r1GameEntityPublicationGates || r1GameMovementMath || r1GameCallbackRemainingRetry || r1ActorRadius;
const verifyR1GameCallbackRemaining = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-callback-remaining';
const r1GameCallbackRemaining = process.argv.length === 3 && (process.argv[2] === '--r1-game-callback-remaining' || verifyR1GameCallbackRemaining);
const verifyR1GameCallbackCoreRetry = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-callback-core-retry';
const r1GameCallbackCoreRetry = process.argv.length === 3 && (process.argv[2] === '--r1-game-callback-core-retry' || verifyR1GameCallbackCoreRetry);
const verifyR1GameCallbackCore = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-callback-core';
const r1GameCallbackCore = r1GameCallbackCoreRetry || (process.argv.length === 3 && (process.argv[2] === '--r1-game-callback-core' || verifyR1GameCallbackCore));
const verifyR1GameMovementInterface = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-movement-interface';
const r1GameMovementInterface = process.argv.length === 3 && (process.argv[2] === '--r1-game-movement-interface' || verifyR1GameMovementInterface);
const verifyR1GameEntityConstructors = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-entity-constructors';
const r1GameEntityConstructors = process.argv.length === 3 && (process.argv[2] === '--r1-game-entity-constructors' || verifyR1GameEntityConstructors);
const verifyR1GameEntityConstructorSearch = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-entity-constructor-search';
const r1GameEntityConstructorSearch = process.argv.length === 3 && (process.argv[2] === '--r1-game-entity-constructor-search' || verifyR1GameEntityConstructorSearch);
const gameEntityConstructorPattern = '^(?:(?:GAME::)?(?:Player::Player|Character::Character)(?:\\s*\\([^\\r\\n]*\\))?|\\?\\?0(?:Player|Character)@GAME@@[^\\r\\n]*)$';
const verifyR1GameStateRegistrationSearch = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-state-registration-search';
const r1GameStateRegistrationSearch = process.argv.length === 3 && (process.argv[2] === '--r1-game-state-registration-search' || verifyR1GameStateRegistrationSearch);
const verifyR1GameAddState = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-add-state';
const r1GameAddState = process.argv.length === 3 && (process.argv[2] === '--r1-game-add-state' || verifyR1GameAddState);
const verifyR1PlayerRegisterStates = process.argv.length === 3 && process.argv[2] === '--verify-r1-player-register-states';
const r1PlayerRegisterStates = process.argv.length === 3 && (process.argv[2] === '--r1-player-register-states' || verifyR1PlayerRegisterStates);
const verifyR1PlayerMouseHandlers = process.argv.length === 3 && process.argv[2] === '--verify-r1-player-mouse-handlers';
const verifyR1PlayerMouseHandlersRetry = process.argv.length === 3 && process.argv[2] === '--verify-r1-player-mouse-handlers-retry';
const r1PlayerMouseHandlersRetry = process.argv.length === 3 && (process.argv[2] === '--r1-player-mouse-handlers-retry' || verifyR1PlayerMouseHandlersRetry);
const verifyR1PlayerMoveEntries = process.argv.length === 3 && process.argv[2] === '--verify-r1-player-move-entries';
const r1PlayerMoveEntries = process.argv.length === 3 && (process.argv[2] === '--r1-player-move-entries' || verifyR1PlayerMoveEntries);
const verifyR1PlayerMoveDispatch = process.argv.length === 3 && process.argv[2] === '--verify-r1-player-move-dispatch';
const r1PlayerMoveDispatch = process.argv.length === 3 && (process.argv[2] === '--r1-player-move-dispatch' || verifyR1PlayerMoveDispatch);
const verifyR1MovementActionPath = process.argv.length === 3 && process.argv[2] === '--verify-r1-movement-action-path';
const r1MovementActionPath = process.argv.length === 3 && (process.argv[2] === '--r1-movement-action-path' || verifyR1MovementActionPath);
const verifyR1PathLocalDispatch = process.argv.length === 3 && process.argv[2] === '--verify-r1-path-local-dispatch';
const r1PathLocalDispatch = process.argv.length === 3 && (process.argv[2] === '--r1-path-local-dispatch' || verifyR1PathLocalDispatch);
const verifyR1ActionExecutor = process.argv.length === 3 && process.argv[2] === '--verify-r1-action-executor';
const r1ActionExecutor = process.argv.length === 3 && (process.argv[2] === '--r1-action-executor' || verifyR1ActionExecutor);
const verifyR1ActionSupport = process.argv.length === 3 && process.argv[2] === '--verify-r1-action-support';
const r1ActionSupport = process.argv.length === 3 && (process.argv[2] === '--r1-action-support' || verifyR1ActionSupport);
const verifyR1MoveStart = process.argv.length === 3 && process.argv[2] === '--verify-r1-move-start';
const r1MoveStart = process.argv.length === 3 && (process.argv[2] === '--r1-move-start' || verifyR1MoveStart);
const verifyR1MoveStartDependencies = process.argv.length === 3 && process.argv[2] === '--verify-r1-move-start-dependencies';
const r1MoveStartDependencies = process.argv.length === 3 && (process.argv[2] === '--r1-move-start-dependencies' || verifyR1MoveStartDependencies);
const verifyR1MoveManager = process.argv.length === 3 && process.argv[2] === '--verify-r1-move-manager';
const r1MoveManager = process.argv.length === 3 && (process.argv[2] === '--r1-move-manager' || verifyR1MoveManager);
const verifyR1WaypointExecution = process.argv.length === 3 && process.argv[2] === '--verify-r1-waypoint-execution';
const r1WaypointExecution = process.argv.length === 3 && (process.argv[2] === '--r1-waypoint-execution' || verifyR1WaypointExecution);
const verifyR1WaypointQueueHelpers = process.argv.length === 3 && process.argv[2] === '--verify-r1-waypoint-queue-helpers';
const r1WaypointQueueHelpers = process.argv.length === 3 && (process.argv[2] === '--r1-waypoint-queue-helpers' || verifyR1WaypointQueueHelpers);
const verifyR1PortalDependencies = process.argv.length === 3 && process.argv[2] === '--verify-r1-portal-dependencies';
const r1PortalDependencies = process.argv.length === 3 && (process.argv[2] === '--r1-portal-dependencies' || verifyR1PortalDependencies);
const verifyR1GameCameraConstructor = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-camera-constructor';
const r1GameCameraConstructor = process.argv.length === 3 && (process.argv[2] === '--r1-game-camera-constructor' || verifyR1GameCameraConstructor);
const verifyR1CrowdPointCopy = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-point-copy';
const r1CrowdPointCopy = process.argv.length === 3 && (process.argv[2] === '--r1-crowd-point-copy' || verifyR1CrowdPointCopy);
const verifyR1CrowdPointReallocation = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-point-reallocation';
const r1CrowdPointReallocation = process.argv.length === 3 && (process.argv[2] === '--r1-crowd-point-reallocation' || verifyR1CrowdPointReallocation);
const verifyR1CrowdDirectAlgorithms = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-direct-algorithms';
const r1CrowdDirectAlgorithms = process.argv.length === 3 && (process.argv[2] === '--r1-crowd-direct-algorithms' || verifyR1CrowdDirectAlgorithms);
const verifyR1EngineCallbackGetters = process.argv.length === 3 && process.argv[2] === '--verify-r1-engine-callback-getters';
const r1EngineCallbackGetters = process.argv.length === 3 && (process.argv[2] === '--r1-engine-callback-getters' || verifyR1EngineCallbackGetters);
const verifyR1CrowdMotionBranches = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-motion-branches';
const r1CrowdMotionBranches = process.argv.length === 3 && (process.argv[2] === '--r1-crowd-motion-branches' || verifyR1CrowdMotionBranches);
const verifyR1CrowdVectorGrowthRetry = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-vector-growth-retry';
const r1CrowdVectorGrowthRetry = process.argv.length === 3 && (process.argv[2] === '--r1-crowd-vector-growth-retry' || verifyR1CrowdVectorGrowthRetry);
const verifyR1CrowdVectorGrowth = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-vector-growth';
const r1CrowdVectorGrowth = r1CrowdVectorGrowthRetry || (process.argv.length === 3 && (process.argv[2] === '--r1-crowd-vector-growth' || verifyR1CrowdVectorGrowth));
const verifyR1CrowdMotionHelpers = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-motion-helpers';
const r1CrowdMotionHelpers = process.argv.length === 3 && (process.argv[2] === '--r1-crowd-motion-helpers' || verifyR1CrowdMotionHelpers);
const verifyR1CrowdAgentGroupRegistration = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-agent-group-registration';
const r1CrowdAgentGroupRegistration = process.argv.length === 3 && (process.argv[2] === '--r1-crowd-agent-group-registration' || verifyR1CrowdAgentGroupRegistration);
const verifyR1CrowdVectorRegistration = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-vector-registration';
const r1CrowdVectorRegistration = process.argv.length === 3 && (process.argv[2] === '--r1-crowd-vector-registration' || verifyR1CrowdVectorRegistration);
const verifyR1CrowdAdapterRegistration = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-adapter-registration';
const r1CrowdAdapterRegistration = process.argv.length === 3 && (process.argv[2] === '--r1-crowd-adapter-registration' || verifyR1CrowdAdapterRegistration);
const verifyR1CrowdQueryProduction = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-query-production';
const r1CrowdQueryProduction = process.argv.length === 3 && (process.argv[2] === '--r1-crowd-query-production' || verifyR1CrowdQueryProduction);
const verifyR1CrowdMovementDirect = process.argv.length === 3 && process.argv[2] === '--verify-r1-crowd-movement-direct';
const r1CrowdMovementDirect = process.argv.length === 3 && (process.argv[2] === '--r1-crowd-movement-direct' || verifyR1CrowdMovementDirect);
const verifyR1MovementFloorCore = process.argv.length === 3 && process.argv[2] === '--verify-r1-movement-floor-core';
const r1MovementFloorCore = process.argv.length === 3 && (process.argv[2] === '--r1-movement-floor-core' || verifyR1MovementFloorCore);
const verifyR1NavigationConsumerDirect = process.argv.length === 3 && process.argv[2] === '--verify-r1-navigation-consumer-direct';
const r1NavigationConsumerDirect = process.argv.length === 3 && (process.argv[2] === '--r1-navigation-consumer-direct' || verifyR1NavigationConsumerDirect);
const verifyR1StraightPathPortalWrapper = process.argv.length === 3 && process.argv[2] === '--verify-r1-straight-path-portal-wrapper';
const r1StraightPathPortalWrapper = process.argv.length === 3 && (process.argv[2] === '--r1-straight-path-portal-wrapper' || verifyR1StraightPathPortalWrapper);
const verifyR1CorridorPortalHelper = process.argv.length === 3 && process.argv[2] === '--verify-r1-corridor-portal-helper';
const r1CorridorPortalHelper = process.argv.length === 3 && (process.argv[2] === '--r1-corridor-portal-helper' || verifyR1CorridorPortalHelper);
const verifyR1NavigationCrossPolyCorridor = process.argv.length === 3 && process.argv[2] === '--verify-r1-navigation-cross-poly-corridor';
const r1NavigationCrossPolyCorridor = process.argv.length === 3 && (process.argv[2] === '--r1-navigation-cross-poly-corridor' || verifyR1NavigationCrossPolyCorridor);
const verifyR1TriangulationAndTileConnection = process.argv.length === 3 && process.argv[2] === '--verify-r1-triangulation-and-tile-connection';
const r1TriangulationAndTileConnection = process.argv.length === 3 && (process.argv[2] === '--r1-triangulation-and-tile-connection' || verifyR1TriangulationAndTileConnection);
const verifyR1MeshLinksAndTriangulationLeaf = process.argv.length === 3 && process.argv[2] === '--verify-r1-mesh-links-and-triangulation-leaf';
const r1MeshLinksAndTriangulationLeaf = process.argv.length === 3 && (process.argv[2] === '--r1-mesh-links-and-triangulation-leaf' || verifyR1MeshLinksAndTriangulationLeaf);
const verifyR1MeshTileAndPredicates = process.argv.length === 3 && process.argv[2] === '--verify-r1-mesh-tile-and-predicates';
const r1MeshTileAndPredicates = process.argv.length === 3 && (process.argv[2] === '--r1-mesh-tile-and-predicates' || verifyR1MeshTileAndPredicates);
const verifyR1NavPublishAndPolygonCore = process.argv.length === 3 && process.argv[2] === '--verify-r1-nav-publish-and-polygon-core';
const r1NavPublishAndPolygonCore = process.argv.length === 3 && (process.argv[2] === '--r1-nav-publish-and-polygon-core' || verifyR1NavPublishAndPolygonCore);
const verifyR1NavPublicationAndContourLeaves = process.argv.length === 3 && process.argv[2] === '--verify-r1-nav-publication-and-contour-leaves';
const r1NavPublicationAndContourLeaves = process.argv.length === 3 && (process.argv[2] === '--r1-nav-publication-and-contour-leaves' || verifyR1NavPublicationAndContourLeaves);
const verifyR1NavUpdateAndContours = process.argv.length === 3 && process.argv[2] === '--verify-r1-nav-update-and-contours';
const r1NavUpdateAndContours = process.argv.length === 3 && (process.argv[2] === '--r1-nav-update-and-contours' || verifyR1NavUpdateAndContours);
const verifyR1TileProductionDirectStages = process.argv.length === 3 && process.argv[2] === '--verify-r1-tile-production-direct-stages';
const r1TileProductionDirectStages = process.argv.length === 3 && (process.argv[2] === '--r1-tile-production-direct-stages' || verifyR1TileProductionDirectStages);
const verifyR1TileCacheAndDecompression = process.argv.length === 3 && process.argv[2] === '--verify-r1-tile-cache-and-decompression';
const r1TileCacheAndDecompression = process.argv.length === 3 && (process.argv[2] === '--r1-tile-cache-and-decompression' || verifyR1TileCacheAndDecompression);
const verifyR1MapRegionAndTileProduct = process.argv.length === 3 && process.argv[2] === '--verify-r1-map-region-and-tile-product';
const r1MapRegionAndTileProduct = process.argv.length === 3 && (process.argv[2] === '--r1-map-region-and-tile-product' || verifyR1MapRegionAndTileProduct);
const verifyR1MapDirectoryAndPathTiles = process.argv.length === 3 && process.argv[2] === '--verify-r1-map-directory-and-path-tiles';
const r1MapDirectoryAndPathTiles = process.argv.length === 3 && (process.argv[2] === '--r1-map-directory-and-path-tiles' || verifyR1MapDirectoryAndPathTiles);
const verifyR1WorldLoadAndNavPublication = process.argv.length === 3 && process.argv[2] === '--verify-r1-world-load-and-nav-publication';
const r1WorldLoadAndNavPublication = process.argv.length === 3 && (process.argv[2] === '--r1-world-load-and-nav-publication' || verifyR1WorldLoadAndNavPublication);
const verifyR1LevelLoadAndNavJobCandidates = process.argv.length === 3 && process.argv[2] === '--verify-r1-level-load-and-navjob-candidates';
const r1LevelLoadAndNavJobCandidates = process.argv.length === 3 && (process.argv[2] === '--r1-level-load-and-navjob-candidates' || verifyR1LevelLoadAndNavJobCandidates);
const verifyR1LevelStreamAndNavJob = process.argv.length === 3 && process.argv[2] === '--verify-r1-level-stream-and-navjob';
const r1LevelStreamAndNavJob = process.argv.length === 3 && (process.argv[2] === '--r1-level-stream-and-navjob' || verifyR1LevelStreamAndNavJob);
const verifyR1RegionBlobLoading = process.argv.length === 3 && process.argv[2] === '--verify-r1-region-blob-loading';
const r1RegionBlobLoading = process.argv.length === 3 && (process.argv[2] === '--r1-region-blob-loading' || verifyR1RegionBlobLoading);
const verifyR1RegionLoadPublication = process.argv.length === 3 && process.argv[2] === '--verify-r1-region-load-publication';
const r1RegionLoadPublication = process.argv.length === 3 && (process.argv[2] === '--r1-region-load-publication' || verifyR1RegionLoadPublication);
const verifyR1NavMeshProductionEntry = process.argv.length === 3 && process.argv[2] === '--verify-r1-navmesh-production-entry';
const r1NavMeshProductionEntry = process.argv.length === 3 && (process.argv[2] === '--r1-navmesh-production-entry' || verifyR1NavMeshProductionEntry);
const verifyR1RegionLoadEntry = process.argv.length === 3 && process.argv[2] === '--verify-r1-region-load-entry';
const r1RegionLoadEntry = process.argv.length === 3 && (process.argv[2] === '--r1-region-load-entry' || verifyR1RegionLoadEntry);
const verifyR1NavigationMeshQueryInitialization = process.argv.length === 3 && process.argv[2] === '--verify-r1-navigation-mesh-query-initialization';
const r1NavigationMeshQueryInitialization = process.argv.length === 3 && (process.argv[2] === '--r1-navigation-mesh-query-initialization' || verifyR1NavigationMeshQueryInitialization);
const verifyR1NavigationImplementationConstructor = process.argv.length === 3 && process.argv[2] === '--verify-r1-navigation-implementation-constructor';
const r1NavigationImplementationConstructor = process.argv.length === 3 && (process.argv[2] === '--r1-navigation-implementation-constructor' || verifyR1NavigationImplementationConstructor);
const verifyR1NavManagerConstructor = process.argv.length === 3 && process.argv[2] === '--verify-r1-navmanager-constructor';
const r1NavManagerConstructor = process.argv.length === 3 && (process.argv[2] === '--r1-navmanager-constructor' || verifyR1NavManagerConstructor);
const verifyR1NavigationNearestDependencies = process.argv.length === 3 && process.argv[2] === '--verify-r1-navigation-nearest-dependencies';
const r1NavigationNearestDependencies = process.argv.length === 3 && (process.argv[2] === '--r1-navigation-nearest-dependencies' || verifyR1NavigationNearestDependencies);
const verifyR1NavigationNearestHelpers = process.argv.length === 3 && process.argv[2] === '--verify-r1-navigation-nearest-helpers';
const r1NavigationNearestHelpers = process.argv.length === 3 && (process.argv[2] === '--r1-navigation-nearest-helpers' || verifyR1NavigationNearestHelpers);
const verifyR1NavigationPolygonGeometry = process.argv.length === 3 && process.argv[2] === '--verify-r1-navigation-polygon-geometry';
const r1NavigationPolygonGeometry = process.argv.length === 3 && (process.argv[2] === '--r1-navigation-polygon-geometry' || verifyR1NavigationPolygonGeometry);
const verifyR1NavigationSinglePoly = process.argv.length === 3 && process.argv[2] === '--verify-r1-navigation-single-poly';
const r1NavigationSinglePoly = process.argv.length === 3 && (process.argv[2] === '--r1-navigation-single-poly' || verifyR1NavigationSinglePoly);
const verifyR1NavigationCore = process.argv.length === 3 && process.argv[2] === '--verify-r1-navigation-core';
const r1NavigationCore = process.argv.length === 3 && (process.argv[2] === '--r1-navigation-core' || verifyR1NavigationCore);
const verifyR1EngineNavigationSearch = process.argv.length === 3 && process.argv[2] === '--verify-r1-engine-navigation-search';
const r1EngineNavigationSearch = process.argv.length === 3 && (process.argv[2] === '--r1-engine-navigation-search' || verifyR1EngineNavigationSearch);
const verifyR1EngineCameraTarget = process.argv.length === 3 && process.argv[2] === '--verify-r1-engine-camera-target';
const r1EngineCameraTarget = process.argv.length === 3 && (process.argv[2] === '--r1-engine-camera-target' || verifyR1EngineCameraTarget);
const verifyR1EnginePortalCamera = process.argv.length === 3 && process.argv[2] === '--verify-r1-engine-portal-camera';
const r1EnginePortalCamera = process.argv.length === 3 && (process.argv[2] === '--r1-engine-portal-camera' || verifyR1EnginePortalCamera);
const verifyR1GameCameraAngle = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-camera-angle';
const r1GameCameraAngle = process.argv.length === 3 && (process.argv[2] === '--r1-game-camera-angle' || verifyR1GameCameraAngle);
const verifyR1GameCameraPosition = process.argv.length === 3 && process.argv[2] === '--verify-r1-game-camera-position';
const r1GameCameraPosition = process.argv.length === 3 && (process.argv[2] === '--r1-game-camera-position' || verifyR1GameCameraPosition);
const r1PlayerMouseHandlers = r1GameCameraPosition || r1GameCameraAngle || r1GameCameraConstructor || r1PortalDependencies || r1WaypointQueueHelpers || r1WaypointExecution || r1MoveManager || r1MoveStartDependencies || r1MoveStart || r1ActionSupport || r1ActionExecutor || r1PathLocalDispatch || r1MovementActionPath || r1PlayerMoveDispatch || r1PlayerMoveEntries || r1PlayerMouseHandlersRetry || (process.argv.length === 3 && (process.argv[2] === '--r1-player-mouse-handlers' || verifyR1PlayerMouseHandlers));
const r1GameFixedMode = r1GameAssociated || r1MoveActionDispatchLeaves || r1DurationManagerDirect || r1DurationManagerConstructor || r1GameRunSpeedDirect || r1GameRunSpeed || r1GameStateLookupDirect || r1GameMainPlayerConnectionMethods || r1GameMainPlayerConnection || r1GamePlayerVideoDirect || r1GameMouseDirectDependencies || r1GameSkillReferenceDirect || r1GameStopStateDirect || r1GameReachedGoalDependencies || r1SkillMovedDirect || r1GameLevelPublicationTail || r1GameEntityPublicationGates || r1GameMovementMath || r1GameCallbackRemainingRetry || r1GameCallbackRemaining || r1GameCallbackCore || r1GameMovementInterface || r1GameEntityConstructors || r1GameEntityConstructorSearch || r1GameMouseCalibration || r1GameMouseLinks || r1GameMouseState || r1GameStateTransition || r1GameStateRegistrationSearch || r1GameAddState || r1PlayerRegisterStates || r1PlayerMouseHandlers;
const verifyR1ExeMouseCallers = process.argv.length === 3 && process.argv[2] === '--verify-r1-exe-mouse-callers';
const r1ExeMouseCallers = process.argv.length === 3 && (process.argv[2] === '--r1-exe-mouse-callers' || verifyR1ExeMouseCallers);
const binarySubobjectProfile = process.argv.length === 3 && process.argv[2] === '--binary-subobject-profile';
const verifyParserEntryPlanOnly = process.argv.length === 3 && process.argv[2] === '--verify-binary-parser-plan';
const verifyParserHelpersPlanOnly = process.argv.length === 3 && process.argv[2] === '--verify-binary-parser-helpers-plan';
const verifyParserVectorPlanOnly = process.argv.length === 3 && process.argv[2] === '--verify-binary-parser-vector-growth-plan';
const verifyRecordPublicationPlanOnly = process.argv.length === 3 && process.argv[2] === '--verify-record-cache-publication-plan';
const verifyRecordMaintenancePlanOnly = process.argv.length === 3 && process.argv[2] === '--verify-record-cache-maintenance-plan';
const verifyRecordNodePlanOnly = process.argv.length === 3 && process.argv[2] === '--verify-record-cache-node-plan';
const verifyParserPlanOnly = verifyR1GameAssociated || verifyR1MoveActionDispatchLeaves || verifyR1MoveWorldDistance || verifyR1MoveProductionEngine || verifyR1DurationManagerDirect || verifyR1DurationManagerConstructor || verifyR1GameRunSpeedDirect || verifyR1GameRunSpeed || verifyR1GameStateLookupDirect || verifyR1LevelObjectRecords || verifyR1GameMainPlayerConnectionMethods || verifyR1GameMainPlayerConnection || verifyR1EngineMouseGetters || verifyR1GamePlayerVideoDirect || verifyR1GameMouseDirectDependencies || verifyR1GameSkillReferenceDirect || verifyR1GameStopStateDirect || verifyR1GameReachedGoalDependencies || verifyR1EngineReachedGoalDependencies || verifyR1WorldPositionLevelInitialization || verifyR1TerrainTypeMaterial || verifyR1WaterInitialization || verifyR1GroundResourcesWater || verifyR1GroundFaceUnlink || verifyR1GroundIntersectionRemaining || verifyR1GroundIntersectionDirect || verifyR1GroundRayTree || verifyR1GroundFloorGeometry || verifyR1GroundBlocksNodeDestroy || verifyR1SpatialGroundDirect || verifyR1EngineSpatialGround || verifyR1EngineEntityPublicationCore || verifyR1SameRegionMovement || verifyR1PositionPublication || verifyR1SkillMovedDirect || verifyR1GameLevelPublicationTail || verifyR1GameEntityPublicationGates || verifyR1GameMovementMath || verifyR1GameCallbackRemainingRetry || verifyR1ActorRadius || verifyR1GameCallbackRemaining || verifyR1GameCallbackCoreRetry || verifyR1GameCallbackCore || verifyR1CrowdPointCopy || verifyR1CrowdPointReallocation || verifyR1CrowdDirectAlgorithms || verifyR1EngineCallbackGetters || verifyR1CrowdMotionBranches || verifyR1GameMovementInterface || verifyR1GameEntityConstructors || verifyR1CrowdVectorGrowthRetry || verifyR1CrowdVectorGrowth || verifyR1GameEntityConstructorSearch || verifyR1CrowdMotionHelpers || verifyR1CrowdAgentGroupRegistration || verifyR1CrowdVectorRegistration || verifyR1CrowdAdapterRegistration || verifyR1CrowdQueryProduction || verifyR1CrowdMovementDirect || verifyR1MovementFloorCore || verifyR1NavigationConsumerDirect || verifyR1StraightPathPortalWrapper || verifyR1CorridorPortalHelper || verifyR1NavigationCrossPolyCorridor || verifyR1TriangulationAndTileConnection || verifyR1MeshLinksAndTriangulationLeaf || verifyR1MeshTileAndPredicates || verifyR1NavPublishAndPolygonCore || verifyR1NavPublicationAndContourLeaves || verifyR1NavUpdateAndContours || verifyR1TileProductionDirectStages || verifyR1TileCacheAndDecompression || verifyR1MapRegionAndTileProduct || verifyR1MapDirectoryAndPathTiles || verifyR1WorldLoadAndNavPublication || verifyR1LevelLoadAndNavJobCandidates || verifyR1LevelStreamAndNavJob || verifyR1RegionBlobLoading || verifyR1RegionLoadPublication || verifyR1NavMeshProductionEntry || verifyR1RegionLoadEntry || verifyR1NavigationMeshQueryInitialization || verifyR1NavigationImplementationConstructor || verifyR1NavManagerConstructor || verifyR1NavigationNearestDependencies || verifyR1NavigationNearestHelpers || verifyR1NavigationPolygonGeometry || verifyR1NavigationSinglePoly || verifyR1NavigationCore || verifyR1EngineNavigationSearch || verifyR1EngineCameraTarget || verifyR1GameCameraPosition || verifyR1EnginePortalCamera || verifyR1GameCameraAngle || verifyR1GameCameraConstructor || verifyR1PortalDependencies || verifyR1WaypointQueueHelpers || verifyR1WaypointExecution || verifyR1MoveManager || verifyR1MoveStartDependencies || verifyR1MoveStart || verifyR1ActionSupport || verifyR1ActionExecutor || verifyR1PathLocalDispatch || verifyR1MovementActionPath || verifyR1PlayerMoveDispatch || verifyR1PlayerMoveEntries || verifyR1PlayerMouseHandlersRetry || verifyR1PlayerMouseHandlers || verifyR1PlayerRegisterStates || verifyR1GameAddState || verifyR1GameStateRegistrationSearch || verifyR1GameStateTransition || verifyR1ExeMouseCallers || verifyR1GameMouseState || verifyR1GameMouseLinks || verifyR1GameMouseCalibration || verifyR1EngineMouseCore || verifyR1EngineInput || verifyR0ToolBaselineTerminalRetry || verifyR0ToolBaseline || verifyParserEntryPlanOnly || verifyParserHelpersPlanOnly || verifyParserVectorPlanOnly || verifyRecordPublicationPlanOnly || verifyRecordMaintenancePlanOnly || verifyRecordNodePlanOnly;
const binaryParserHelpers = process.argv.length === 3 && (process.argv[2] === '--binary-parser-helpers' || verifyParserHelpersPlanOnly);
const binaryParserVector = process.argv.length === 3 && (process.argv[2] === '--binary-parser-vector-growth' || verifyParserVectorPlanOnly);
const recordCachePublication = process.argv.length === 3 && (process.argv[2] === '--record-cache-publication' || verifyRecordPublicationPlanOnly);
const recordCacheMaintenance = process.argv.length === 3 && (process.argv[2] === '--record-cache-maintenance' || verifyRecordMaintenancePlanOnly);
const recordCacheNode = process.argv.length === 3 && (process.argv[2] === '--record-cache-node' || verifyRecordNodePlanOnly);
const recordCacheFixedPair = recordCachePublication || recordCacheMaintenance || recordCacheNode;
const parserRetryAfterExitRace = process.argv.length === 3 && process.argv[2] === '--binary-parser-entry-retry-2';
const parserRetry = process.argv.length === 3 && (process.argv[2] === '--binary-parser-entry-retry' || parserRetryAfterExitRace);
const binaryParserEntry = process.argv.length === 3 && (process.argv[2] === '--binary-parser-entry' || parserRetry || verifyParserEntryPlanOnly);
const parserBudgetMode = r1LevelObjectRecords || r1MoveWorldDistance || r1MoveProductionEngine || r1EngineMouseGetters || r1EngineReachedGoalDependencies || r1WorldPositionLevelInitialization || r1TerrainTypeMaterial || r1WaterInitialization || r1GroundResourcesWater || r1GroundFaceUnlink || r1GroundIntersectionRemaining || r1GroundIntersectionDirect || r1GroundRayTree || r1GroundFloorGeometry || r1GroundBlocksNodeDestroy || r1SpatialGroundDirect || r1EngineSpatialGround || r1EngineEntityPublicationCore || r1SameRegionMovement || r1PositionPublication || r1ActorRadius || r1CrowdPointCopy || r1CrowdPointReallocation || r1CrowdDirectAlgorithms || r1EngineCallbackGetters || r1CrowdMotionBranches || r1CrowdVectorGrowth || r1CrowdMotionHelpers || r1CrowdAgentGroupRegistration || r1CrowdVectorRegistration || r1CrowdAdapterRegistration || r1CrowdQueryProduction || r1CrowdMovementDirect || r1MovementFloorCore || r1NavigationConsumerDirect || r1StraightPathPortalWrapper || r1CorridorPortalHelper || r1NavigationCrossPolyCorridor || r1TriangulationAndTileConnection || r1MeshLinksAndTriangulationLeaf || r1MeshTileAndPredicates || r1NavPublishAndPolygonCore || r1NavPublicationAndContourLeaves || r1NavUpdateAndContours || r1TileProductionDirectStages || r1TileCacheAndDecompression || r1MapRegionAndTileProduct || r1MapDirectoryAndPathTiles || r1WorldLoadAndNavPublication || r1LevelLoadAndNavJobCandidates || r1LevelStreamAndNavJob || r1RegionBlobLoading || r1RegionLoadPublication || r1NavMeshProductionEntry || r1RegionLoadEntry || r1NavigationMeshQueryInitialization || r1NavigationImplementationConstructor || r1NavManagerConstructor || r1NavigationNearestDependencies || r1NavigationNearestHelpers || r1NavigationPolygonGeometry || r1NavigationSinglePoly || r1NavigationCore || r1EngineNavigationSearch || r1EngineCameraTarget || r1EnginePortalCamera || r1EngineMouseCore || r1EngineInput || r0ToolBaselineTerminalRetry || r0ToolBaseline || binaryParserEntry || binaryParserHelpers || binaryParserVector || recordCacheFixedPair;
const ownedMemoryMode = r1ExeMouseCallers || r1GameFixedMode || binarySubobjectProfile || parserBudgetMode;
const budget = r1ExeMouseCallers ? clientMouseCalibrationBudget : r1GameFixedMode ? gameMouseCalibrationBudget : parserBudgetMode ? parserBudget : calibrationBudget;
const wrapper = path.join(root, r1GameAssociatedRetry ? 'tools/static_analysis/rea_game_associated_retry_server.mjs' : ownedMemoryMode ? 'tools/static_analysis/rea_profiled_server.mjs' : 'tools/static_analysis/rea_project_server.mjs');
const packageRoot = path.join(root, '.tooling/rea/node_modules/rea-agents');
const launcher = path.join(packageRoot, 'scripts/rea.mjs');
const containers = process.argv.length === 3 && process.argv[2] === '--containers';
const containerSupport = process.argv.length === 3 && process.argv[2] === '--container-support';
const containerFill = process.argv.length === 3 && process.argv[2] === '--container-fill';
const containerReserve = process.argv.length === 3 && process.argv[2] === '--container-reserve';
const objectBase = process.argv.length === 3 && process.argv[2] === '--object-base';
const objectNameAssignment = process.argv.length === 3 && process.argv[2] === '--object-name-assignment';
const objectNameGrowth = process.argv.length === 3 && process.argv[2] === '--object-name-growth';
const recordLoadDependencies = process.argv.length === 3 && process.argv[2] === '--record-load-dependencies';
const recordCacheCompare = process.argv.length === 3 && process.argv[2] === '--record-cache-compare';
const recordClassGetters = process.argv.length === 3 && process.argv[2] === '--record-class-getters';
const recordGetterSupport = process.argv.length === 3 && process.argv[2] === '--record-getter-support';
const binaryColdInsert = process.argv.length === 3 && process.argv[2] === '--binary-cold-insert';
const binaryPointerGrowth = process.argv.length === 3 && process.argv[2] === '--binary-pointer-growth';
const binaryTableConstructor = process.argv.length === 3 && process.argv[2] === '--binary-table-constructor';
const memoryGuardEnabled = binaryColdInsert || binaryPointerGrowth || binaryTableConstructor || ownedMemoryMode;
// Only these reviewed batches are selectable; paths, fingerprints and budgets
// cannot be supplied by arguments or taken from an unverified plan.
const batch = r1GameAnimationSetProduction ? {
  plan: '../../docs/reverse-client/r1-controller-action-slots-plan/plan.json', directory: '../reverse-client/r1-controller-action-slots-run1',
  hash: 'b998aabb1ae88df7dbc5b8a1c70605820add4e8c00e56cff1cd90d23345c2502', maxToolCalls: 9, finalSessionLabel: '09-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameAnimationSetNames ? {
  plan: '../../docs/reverse-client/r1-movement-manager-name-search-plan/plan.json', directory: '../reverse-client/r1-movement-manager-name-search-run1',
  hash: '611713f13ba454ba5ea089605852f944ed27533cec5e6c6f8f283267e8347837', maxToolCalls: 6, finalSessionLabel: '06-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameWaypointAnimation ? {
  plan: '../../docs/reverse-client/r1-game-waypoint-animation-plan/plan.json', directory: '../reverse-client/r1-game-waypoint-animation-run1',
  hash: '3e0cce7ca80468aaf8741b448237586afafa64a7036dd0c5ac1a2575675c07ee', maxToolCalls: 11, finalSessionLabel: '11-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameAssociatedIdentityRepair ? {
  plan: '../../docs/reverse-client/r1-game-associated-identity-repair-plan/plan.json', directory: '../reverse-client/r1-game-associated-run3',
  hash: 'a64e666a6d593b33fe8e65c73b2577a7ff9dff54fcbf8aa527069eebb9825363', maxToolCalls: 8, finalSessionLabel: '08-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameAssociatedRetry ? {
  plan: '../../docs/reverse-client/r1-game-associated-retry-plan/plan.json', directory: '../reverse-client/r1-game-associated-run2',
  hash: '6b76f85471561cc706d4f8275b2733d4362c0827f7c6949822c6a42bdc47582b', maxToolCalls: 8, finalSessionLabel: '08-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameAssociated ? {
  plan: '../../docs/reverse-client/r1-game-associated-plan/plan.json', directory: '../reverse-client/r1-game-associated-run1',
  hash: '747a6ec19bce1beb36c9fbb5e116234d262af62ca3d7cec26de1f58bb7a61e4e', maxToolCalls: 8, finalSessionLabel: '08-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1MoveActionDispatchLeaves ? {
  plan: '../../docs/reverse-client/r1-move-action-dispatch-leaves-plan/plan.json', directory: '../reverse-client/r1-move-action-dispatch-leaves-run1',
  hash: 'ec75cfb42336e2f159a116e8a0431700795d8ed3e91ba82154c9456fc99fa16e', maxToolCalls: 11, finalSessionLabel: '11-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1DurationManagerDirect ? {
  plan: '../../docs/reverse-client/r1-duration-manager-direct-plan/plan.json', directory: '../reverse-client/r1-duration-manager-direct-run1',
  hash: 'e5dcc8b0d974cfd7c2e882b7bffe2e9ca4ca87b9ff0afc55c892d5f55e76eb0d', maxToolCalls: 11, finalSessionLabel: '11-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1DurationManagerConstructor ? {
  plan: '../../docs/reverse-client/r1-duration-manager-constructor-plan/plan.json', directory: '../reverse-client/r1-duration-manager-constructor-run1',
  hash: '6156533f11bf6abf5292e8cf9282c72ac84abcdd41413daf6dded2ea4b77846e', maxToolCalls: 8, finalSessionLabel: '08-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameRunSpeedDirect ? {
  plan: '../../docs/reverse-client/r1-game-run-speed-direct-plan/plan.json', directory: '../reverse-client/r1-game-run-speed-direct-run1',
  hash: 'e3880c8dbe3f15223094d4a13325d48ca83c19334c019679e4b5475a24894f44', maxToolCalls: 14, finalSessionLabel: '14-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameRunSpeed ? {
  plan: '../../docs/reverse-client/r1-game-run-speed-plan/plan.json', directory: '../reverse-client/r1-game-run-speed-run1',
  hash: '4f57d268bd3d6c29dab6691639e9d4f4232b87e0b33e8699b22ca36cbbfcc205', maxToolCalls: 8, finalSessionLabel: '08-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameStateLookupDirect ? {
  plan: '../../docs/reverse-client/r1-game-state-lookup-direct-plan/plan.json', directory: '../reverse-client/r1-game-state-lookup-direct-run1',
  hash: '3d80d6b0e0e6696c1b74fa09e3d1561621e91a912e77f49883a78338eeb8af30', maxToolCalls: 11, finalSessionLabel: '11-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1LevelObjectRecords ? {
  plan: '../../docs/reverse-client/r1-level-object-records-plan/plan.json', directory: '../reverse-client/r1-level-object-records-run1',
  hash: '48437331f2b5eabc7e76dc5ba162905053376fa3258d50721e24e8edf982f60f', maxToolCalls: 8, finalSessionLabel: '08-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-world-position-and-level-initialization-plan.json', sha256:'2e94c3fdf888357a6be8a84bd03b8db4a54b66164e501785ff80352d79b9a2f0'},
} : r1GameMainPlayerConnectionMethods ? {
  plan: '../../docs/reverse-client/r1-game-mainplayer-connection-methods-plan/plan.json', directory: '../reverse-client/r1-game-mainplayer-connection-methods-run1',
  hash: 'c436f9a73988fbef9032d30cb8a9be1b66a5b1d957425f3baa4953b442612cac', maxToolCalls: 11, finalSessionLabel: '11-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameMainPlayerConnection ? {
  plan: '../../docs/reverse-client/r1-game-mainplayer-connection-plan/plan.json', directory: '../reverse-client/r1-game-mainplayer-connection-run1',
  hash: 'de234801052907606224c63aa61311d9dccbdebcadc097febf89e003888cda29', maxToolCalls: 8, finalSessionLabel: '08-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1EngineAnimationSpeed ? {
  plan: '../../.tooling/r1-lua-provider-registration-prepared/plan.json', directory: '../reverse-client/r1-engine-lua-provider-registration-run1',
  hash: '929299a4f84a830fde23796ec97fa3cbad075bc55e9aa2ac5782ccafd6ae6e7a', maxToolCalls: 9, finalSessionLabel: '09-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-world-position-and-level-initialization-plan.json', sha256:'2e94c3fdf888357a6be8a84bd03b8db4a54b66164e501785ff80352d79b9a2f0'},
} : r1MoveWorldDistance ? {
  plan: '../../docs/reverse-client/r1-move-world-distance-plan/plan.json', directory: '../reverse-client/r1-move-world-distance-run1',
  hash: '9823b315c827f24477f0898cd9d77872492007d4396f36ec4d10089d08ae3edb', maxToolCalls: 8, finalSessionLabel: '08-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-world-position-and-level-initialization-plan.json', sha256:'2e94c3fdf888357a6be8a84bd03b8db4a54b66164e501785ff80352d79b9a2f0'},
} : r1MoveProductionEngine ? {
  plan: '../../docs/reverse-client/r1-move-production-engine-plan/plan.json', directory: '../reverse-client/r1-move-production-engine-run1',
  hash: '45889ac23ac8b2c5afec54846c9583edc8b066247cc193d3809d6711d2b877bc', maxToolCalls: 11, finalSessionLabel: '11-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-world-position-and-level-initialization-plan.json', sha256:'2e94c3fdf888357a6be8a84bd03b8db4a54b66164e501785ff80352d79b9a2f0'},
} : r1EngineMouseGetters ? {
  plan: '../../docs/reverse-client/r1-engine-mouse-getters-plan/plan.json', directory: '../reverse-client/r1-engine-mouse-getters-run1',
  hash: 'b85d4633d63c9710c4ca7ac464c7dc83fcd19d22ede123e8dfd7086b919d739c', maxToolCalls: 17, finalSessionLabel: '17-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-world-position-and-level-initialization-plan.json', sha256:'2e94c3fdf888357a6be8a84bd03b8db4a54b66164e501785ff80352d79b9a2f0'},
} : r1GamePlayerVideoDirect ? {
  plan: '../../docs/reverse-client/r1-game-player-video-direct-plan/plan.json', directory: '../reverse-client/r1-game-player-video-direct-run1',
  hash: '81e8b9b24a34915f5b25e323acd5a55aa7bc4155e72774c20df0ef5a92c9bdaf', maxToolCalls: 8, finalSessionLabel: '08-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameMouseDirectDependencies ? {
  plan: '../../docs/reverse-client/r1-game-mouse-direct-dependencies-plan/plan.json', directory: '../reverse-client/r1-game-mouse-direct-dependencies-run1',
  hash: '5beedcf5799dcaf1b9e34e8d29334f56980dd5a7b39da00118433873d1d3927f', maxToolCalls: 14, finalSessionLabel: '14-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameSkillReferenceDirect ? {
  plan: '../../docs/reverse-client/r1-game-skill-reference-direct-plan/plan.json', directory: '../reverse-client/r1-game-skill-reference-direct-run1',
  hash: '882b1caba7e2fec7e634bc68be73ca7d3a2837d4a1533d3f7af0cb8b5bb16eb3', maxToolCalls: 8, finalSessionLabel: '08-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameStopStateDirect ? {
  plan: '../../docs/reverse-client/r1-game-stop-state-direct-plan/plan.json', directory: '../reverse-client/r1-game-stop-state-direct-run1',
  hash: '8a8edc93946fa2245f98b354c7a0a349bc7132a9cdc7d31e3f63e523142ef718', maxToolCalls: 8, finalSessionLabel: '08-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1GameReachedGoalDependencies ? {
  plan: '../../docs/reverse-client/r1-game-reached-goal-dependencies/plan.json', directory: '../reverse-client/r1-game-reached-goal-dependencies-run1',
  hash: '386c14126ae37b07a05380af41c1446f6f37b965828b4322f566cd6b8ab5361b', maxToolCalls: 11, finalSessionLabel: '11-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-skill-moved-direct-plan.json', sha256:'96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677'},
} : r1EngineReachedGoalDependencies ? {
  plan: '../../docs/reverse-client/r1-engine-reached-goal-dependencies/plan.json', directory: '../reverse-client/r1-engine-reached-goal-dependencies-run1',
  hash: '6e511eb1a6444f8d8c41567b5242e46787d5bc1363d81e93af049d3ce578ae2c', maxToolCalls: 13, finalSessionLabel: '13-final-session',
  runtimeProfile: {file:'tools/static_analysis/rea-r1-world-position-and-level-initialization-plan.json', sha256:'2e94c3fdf888357a6be8a84bd03b8db4a54b66164e501785ff80352d79b9a2f0'},
} : r1WorldPositionLevelInitialization ? {
  plan: 'rea-r1-world-position-and-level-initialization-plan.json', directory: '../reverse-client/r1-world-position-and-level-initialization',
  hash: '2e94c3fdf888357a6be8a84bd03b8db4a54b66164e501785ff80352d79b9a2f0', maxToolCalls: 11, finalSessionLabel: '11-final-session',
} : r1SkillMovedDirect ? {
  plan: 'rea-r1-skill-moved-direct-plan.json', directory: '../reverse-client/r1-skill-moved-direct',
  hash: '96e164adb4ff4909806229a8a4a7dce460cb134d76de0cc21768e9ea29313677', maxToolCalls: 8, finalSessionLabel: '08-final-session',
} : r1TerrainTypeMaterial ? {
  plan: 'rea-r1-terrain-type-material-plan.json', directory: '../reverse-client/r1-terrain-type-material',
  hash: '74d16aaa63e3a577f51f559dd4e8295a8378b3039d1dd6c91f61d89592d039cc', maxToolCalls: 9, finalSessionLabel: '09-final-session',
} : r1WaterInitialization ? {
  plan: 'rea-r1-water-initialization-plan.json', directory: '../reverse-client/r1-water-initialization',
  hash: 'd2a2bb641c56833200a3e28bbfad7daf677466b1498b90661d137ae7a483d7cb', maxToolCalls: 11, finalSessionLabel: '11-final-session',
} : r1GroundResourcesWater ? {
  plan: 'rea-r1-ground-resources-and-water-plan.json', directory: '../reverse-client/r1-ground-resources-and-water',
  hash: '332823c058daa61d170d148f2ebf3f94a863663b4491a82aa86e0992200eb517', maxToolCalls: 17, finalSessionLabel: '17-final-session',
} : r1GroundFaceUnlink ? {
  plan: 'rea-r1-ground-face-and-unlink-plan.json', directory: '../reverse-client/r1-ground-face-and-unlink',
  hash: '29ebc7e5306b7e820cef7838211b3b9c80050688aa429a1fc7b3e7e5a123a9af', maxToolCalls: 17, finalSessionLabel: '17-final-session',
} : r1GroundIntersectionRemaining ? {
  plan: 'rea-r1-ground-intersection-remaining-plan.json', directory: '../reverse-client/r1-ground-intersection-remaining',
  hash: '621fbc1ca0459fd739417daef820f7b978b58f2987095b3237f75178a3ced288', maxToolCalls: 26, finalSessionLabel: '26-final-session',
} : r1GroundIntersectionDirect ? {
  plan: 'rea-r1-ground-intersection-direct-plan.json', directory: '../reverse-client/r1-ground-intersection-direct',
  hash: '4bb0c6ed7ac99fa510ee0fcdba5299de60e65f9300967ccc06c050b4dc15fc43', maxToolCalls: 29, finalSessionLabel: '29-final-session',
} : r1GroundRayTree ? {
  plan: 'rea-r1-ground-ray-tree-plan.json', directory: '../reverse-client/r1-ground-ray-tree',
  hash: 'adb7329d6cc878bb96be5d63f0bc744a0491c6fe695a7836067f9d8d944bd745', maxToolCalls: 26, finalSessionLabel: '26-final-session',
} : r1GroundFloorGeometry ? {
  plan: 'rea-r1-ground-floor-geometry-plan.json', directory: '../reverse-client/r1-ground-floor-geometry',
  hash: 'cd2ca7dc0d32de2b0fedc764069db45d1095507e1fd2d3faa53f7edd67d77394', maxToolCalls: 14, finalSessionLabel: '14-final-session',
} : r1GroundBlocksNodeDestroy ? {
  plan: 'rea-r1-ground-blocks-and-node-destroy-plan.json', directory: '../reverse-client/r1-ground-blocks-and-node-destroy',
  hash: '722db669aadec8a0e84cd235ea0db62afde7691da09305956f2eafade3b8d5ad', maxToolCalls: 17, finalSessionLabel: '17-final-session',
} : r1SpatialGroundDirect ? {
  plan: 'rea-r1-spatial-ground-direct-plan.json', directory: '../reverse-client/r1-spatial-ground-direct',
  hash: '93db63ac66314b664b12c8cbbe6a8df42f26cf564d43683aa2c19e031d555be0', maxToolCalls: 14, finalSessionLabel: '14-final-session',
} : r1EngineSpatialGround ? {
  plan: 'rea-r1-engine-spatial-ground-plan.json', directory: '../reverse-client/r1-engine-spatial-ground',
  hash: '9a1e0a0984fb2521aa7f7755b4668d5c0df896a9b9cbb79aa3a064b57f3950dc', maxToolCalls: 20, finalSessionLabel: '20-final-session',
} : r1GameLevelPublicationTail ? {
  plan: 'rea-r1-game-level-publication-tail-plan.json', directory: '../reverse-client/r1-game-level-publication-tail',
  hash: 'b2a692d9454575bc1d5917232de83c8625e19a7ddf7899fd251a7d04e919621f', maxToolCalls: 11, finalSessionLabel: '11-final-session',
} : r1EngineEntityPublicationCore ? {
  plan: 'rea-r1-engine-entity-publication-core-plan.json', directory: '../reverse-client/r1-engine-entity-publication-core',
  hash: '82c1dc1cd6e14906d56dbedf8bdee5ae4cbe43fb12b6dfaf94225ddc3044f902', maxToolCalls: 17, finalSessionLabel: '17-final-session',
} : r1GameEntityPublicationGates ? {
  plan: 'rea-r1-game-entity-publication-gates-plan.json', directory: '../reverse-client/r1-game-entity-publication-gates',
  hash: 'c29082325575eb0e2624691607fd048359c86cd30b101344585c17d16c9f2e60', maxToolCalls: 20, finalSessionLabel: '20-final-session',
} : r1SameRegionMovement ? {
  plan: 'rea-r1-same-region-movement-plan.json', directory: '../reverse-client/r1-same-region-movement',
  hash: '16bb7ac1a357aadf532a47eb36fb43c7b5c6a408d1e437c63a63b0349905a077', maxToolCalls: 11, finalSessionLabel: '11-final-session',
} : r1PositionPublication ? {
  plan: 'rea-r1-position-publication-plan.json', directory: '../reverse-client/r1-position-publication',
  hash: 'b5a146881beed04d39afdc416fd73bb1fa35e02b3a4e27b29816b27f8a1564a8', maxToolCalls: 11, finalSessionLabel: '11-final-session',
} : r1GameMovementMath ? {
  plan: 'rea-r1-game-movement-math-plan.json', directory: '../reverse-client/r1-game-movement-math',
  hash: '2069ac269b0b0149b996c258377423b03dd0d63ad5a87c81e0f9eda4d28797f5', maxToolCalls: 14, finalSessionLabel: '14-final-session',
} : r1GameCallbackRemainingRetry ? {
  plan: 'rea-r1-game-callback-remaining-retry-plan.json',
  directory: '../reverse-client/r1-game-callback-remaining-retry-1',
  hash: '7d00c83d4f52298a5e50f83d3ac2ae8ac536b5b4ae68acb8359168e1d38a15ca', maxToolCalls: 14, finalSessionLabel: '14-final-session',
} : r1ActorRadius ? {
  plan: 'rea-r1-actor-radius-plan.json', directory: '../reverse-client/r1-actor-radius',
  hash: 'c911eaf7381a60e7e115192672a5ef683489137adb9d9e94addc4f2b65dd9c03', maxToolCalls: 8, finalSessionLabel: '08-final-session',
} : r1GameCallbackRemaining ? {
  plan: 'rea-r1-game-callback-remaining-plan.json',
  directory: '../reverse-client/r1-game-callback-remaining',
  hash: 'dab0446e4dad30530c6bdf1166816b4add359e309d1740488dcd6decfe84faa1',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1GameCallbackCoreRetry ? {
  plan: 'rea-r1-game-callback-core-retry-plan.json',
  directory: '../reverse-client/r1-game-callback-core-retry-1',
  hash: 'd80daae4c7148d48ef7eea28bfa19b8f57d7417ef361b460c1e4022d291b70d3',
  maxToolCalls: 23,
  finalSessionLabel: '23-final-session',
} : r1GameCallbackCore ? {
  plan: 'rea-r1-game-callback-core-plan.json',
  directory: '../reverse-client/r1-game-callback-core',
  hash: '8d70ef34323f0802d624d913d36bfa5dd86cff1cd29f1cf956e0c59abd1c271f',
  maxToolCalls: 23,
  finalSessionLabel: '23-final-session',
} : r1CrowdPointCopy ? {
  plan: 'rea-r1-crowd-point-copy-plan.json',
  directory: '../reverse-client/r1-crowd-point-copy',
  hash: '60f646a8fa0befa57decd00d4e0137bf16f0a7f6e1142d07f93016e0260a5d9c',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1CrowdPointReallocation ? {
  plan: 'rea-r1-crowd-point-reallocation-plan.json',
  directory: '../reverse-client/r1-crowd-point-reallocation',
  hash: 'caa2361f0304aa87e1a6c80a5bdaeaab75d82927ea7c7b3877fdde33fec21f95',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1CrowdDirectAlgorithms ? {
  plan: 'rea-r1-crowd-direct-algorithms-plan.json',
  directory: '../reverse-client/r1-crowd-direct-algorithms',
  hash: 'a6c1bd6f3f103cfc0c63920903ded3e967b58bd9d503d161d9e512dd4133b8b5',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1EngineCallbackGetters ? {
  plan: 'rea-r1-engine-callback-getters-plan.json',
  directory: '../reverse-client/r1-engine-callback-getters',
  hash: '17d66d760477b82fec837e7e111f3d1b8a817a05e672f196a649c8e52f4b6b40',
  maxToolCalls: 26,
  finalSessionLabel: '26-final-session',
} : r1CrowdMotionBranches ? {
  plan: 'rea-r1-crowd-motion-branches-plan.json',
  directory: '../reverse-client/r1-crowd-motion-branches',
  hash: '690727de533d0d1b4c98fccdcd7b25c6911cf702ce92360209bcd2e48372adf0',
  maxToolCalls: 29,
  finalSessionLabel: '29-final-session',
} : r1GameMovementInterface ? {
  plan: 'rea-r1-game-movement-interface-plan.json',
  directory: '../reverse-client/r1-game-movement-interface',
  hash: '3a64bc1f9752a0023c1b7bd9cae1aa165857481683a324b25cd2e120c3061f1e',
  maxToolCalls: 20,
  finalSessionLabel: '20-final-session',
} : r1GameEntityConstructors ? {
  plan: 'rea-r1-game-entity-constructors-plan.json',
  directory: '../reverse-client/r1-game-entity-constructors',
  hash: 'cae676d0115d27f684abcfdc58762e96f33bff312d1bced4dc2161e69aa095e1',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r1CrowdVectorGrowthRetry ? {
  plan: 'rea-r1-crowd-vector-growth-retry-plan.json',
  directory: '../reverse-client/r1-crowd-vector-growth-retry-1',
  hash: '0071414657278015c86a09063eaac7dd768e5ee13e93ffe685dbd482fe586533',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r1CrowdVectorGrowth ? {
  plan: 'rea-r1-crowd-vector-growth-plan.json',
  directory: '../reverse-client/r1-crowd-vector-growth',
  hash: '420c9d2904d4670a52552095e4c377394840035df81edd0d3029b0cbf9f1b27d',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r1GameEntityConstructorSearch ? {
  plan: 'rea-r1-game-entity-constructor-search-plan.json',
  directory: '../reverse-client/r1-game-entity-constructor-search',
  hash: '3c093a758642fd2871b405f6951360c79fb4e3955c7c6e0154898a165843fbb3',
  maxToolCalls: 6,
  finalSessionLabel: '06-final-session',
} : r1CrowdMotionHelpers ? {
  plan: 'rea-r1-crowd-motion-helpers-plan.json',
  directory: '../reverse-client/r1-crowd-motion-helpers',
  hash: 'cd81a2327a63e6e1acb2178f945fab7f6a67f39a98c2ea8e821e026a401fd1a6',
  maxToolCalls: 23,
  finalSessionLabel: '23-final-session',
} : r1CrowdAgentGroupRegistration ? {
  plan: 'rea-r1-crowd-agent-group-registration-plan.json',
  directory: '../reverse-client/r1-crowd-agent-group-registration',
  hash: '50ac0f0f341705ea45738545f37fa9f6b260859c72d926293e38068d03cdb63e',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1CrowdVectorRegistration ? {
  plan: 'rea-r1-crowd-vector-registration-plan.json',
  directory: '../reverse-client/r1-crowd-vector-registration',
  hash: 'a106516e99df2ab15fa9a42b9552820514e5db32090493c006effbaab0a64434',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1CrowdAdapterRegistration ? {
  plan: 'rea-r1-crowd-adapter-registration-plan.json',
  directory: '../reverse-client/r1-crowd-adapter-registration',
  hash: 'feef721ae17f48b89120e4eaebbf629887a98056fbb4192fc2696177bc79012e',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : r1CrowdQueryProduction ? {
  plan: 'rea-r1-crowd-query-production-plan.json',
  directory: '../reverse-client/r1-crowd-query-production',
  hash: '64dc2eb2d3aea6b984bab28b30d6c34d3c32adcf28e1a3d4e3605b232d561956',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1CrowdMovementDirect ? {
  plan: 'rea-r1-crowd-movement-direct-plan.json',
  directory: '../reverse-client/r1-crowd-movement-direct',
  hash: 'e20453aa9535d6b926318565405b15487298b95133ff9595e046f20539e57d09',
  maxToolCalls: 20,
  finalSessionLabel: '20-final-session',
} : r1MovementFloorCore ? {
  plan: 'rea-r1-movement-floor-core-plan.json',
  directory: '../reverse-client/r1-movement-floor-core',
  hash: 'c37c0d838d01a370ed909a3c67d2f76bbd1213d0d51bc3d12128d2a81a329bf5',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : r1NavigationConsumerDirect ? {
  plan: 'rea-r1-navigation-consumer-direct-plan.json',
  directory: '../reverse-client/r1-navigation-consumer-direct',
  hash: 'd75470fe0a7a986dffc67a9c947d67b7b5321989f72a09f68ab493540755be33',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1StraightPathPortalWrapper ? {
  plan: 'rea-r1-straight-path-portal-wrapper-plan.json',
  directory: '../reverse-client/r1-straight-path-portal-wrapper',
  hash: '21ca6b67c62db9bf83dadc5fa6220e4560cf8a08acdb42a65b216d14a2c7fba3',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1CorridorPortalHelper ? {
  plan: 'rea-r1-corridor-portal-helper-plan.json',
  directory: '../reverse-client/r1-corridor-portal-helper',
  hash: 'febc7ae185242cf459b229da78be7efe4197dd3e55caa0605e92590f79a23e79',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1NavigationCrossPolyCorridor ? {
  plan: 'rea-r1-navigation-cross-poly-corridor-plan.json',
  directory: '../reverse-client/r1-navigation-cross-poly-corridor',
  hash: '3cc942d62a293310f973973a023f5f8b8da9fd23df4c2216c8584053a06634d1',
  maxToolCalls: 23,
  finalSessionLabel: '23-final-session',
} : r1TriangulationAndTileConnection ? {
  plan: 'rea-r1-triangulation-and-tile-connection-plan.json',
  directory: '../reverse-client/r1-triangulation-and-tile-connection',
  hash: 'cbe70512ebc0a1a5ae3f8be5d156239e13e59f70c83ecd1454a20927c17ae2a9',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : r1MeshLinksAndTriangulationLeaf ? {
  plan: 'rea-r1-mesh-links-and-triangulation-leaf-plan.json',
  directory: '../reverse-client/r1-mesh-links-and-triangulation-leaf',
  hash: '8010a240bf81c3d3752f8fe5510081c583064aba74bb1fc4c5d0e8bc16f0da4e',
  maxToolCalls: 26,
  finalSessionLabel: '26-final-session',
} : r1MeshTileAndPredicates ? {
  plan: 'rea-r1-mesh-tile-and-predicates-plan.json',
  directory: '../reverse-client/r1-mesh-tile-and-predicates',
  hash: '26841d78cbfe3113dad51e3a490aca0fab9e1e50890e72069ef12be368b7d7b2',
  maxToolCalls: 20,
  finalSessionLabel: '20-final-session',
} : r1NavPublishAndPolygonCore ? {
  plan: 'rea-r1-nav-publish-and-polygon-core-plan.json',
  directory: '../reverse-client/r1-nav-publish-and-polygon-core',
  hash: '2c552451b2bbe2af235c20a24bcb80821bf7860259b9caa1b90a48fefe7363b1',
  maxToolCalls: 20,
  finalSessionLabel: '20-final-session',
} : r1NavPublicationAndContourLeaves ? {
  plan: 'rea-r1-nav-publication-and-contour-leaves-plan.json',
  directory: '../reverse-client/r1-nav-publication-and-contour-leaves',
  hash: 'a3229b21704ced1d5a698aa9f611af00952a80780a913991e1b43ee2d5a8667d',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : r1NavUpdateAndContours ? {
  plan: 'rea-r1-nav-update-and-contours-plan.json',
  directory: '../reverse-client/r1-nav-update-and-contours',
  hash: 'a8b93bf56c7ede65d1a74e75b15ed134002fb2b94fa613d2657bcbbf07485958',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : r1TileProductionDirectStages ? {
  plan: 'rea-r1-tile-production-direct-stages-plan.json',
  directory: '../reverse-client/r1-tile-production-direct-stages',
  hash: 'd855afc41987a81426ba0477cc31b0408835ea760e4e3c0c03e2d1a7f94b21df',
  maxToolCalls: 29,
  finalSessionLabel: '29-final-session',
} : r1TileCacheAndDecompression ? {
  plan: 'rea-r1-tile-cache-and-decompression-plan.json',
  directory: '../reverse-client/r1-tile-cache-and-decompression',
  hash: '0bdd2afafecf8671f207fdc471d53ef2c6694fa88aa43156afbd19d0dc858a03',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1MapRegionAndTileProduct ? {
  plan: 'rea-r1-map-region-and-tile-product-plan.json',
  directory: '../reverse-client/r1-map-region-and-tile-product',
  hash: 'daa5373466562a6487f18b691600becdd39d1345164b6427c1c4d1f655e5f14e',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1MapDirectoryAndPathTiles ? {
  plan: 'rea-r1-map-directory-and-path-tiles-plan.json',
  directory: '../reverse-client/r1-map-directory-and-path-tiles',
  hash: 'eba108ea461df6a5217d9c29690aafd9b95b5c1df66c16b03ba2fef21145052b',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1WorldLoadAndNavPublication ? {
  plan: 'rea-r1-world-load-and-nav-publication-plan.json',
  directory: '../reverse-client/r1-world-load-and-nav-publication',
  hash: 'd6a89e9c24e431265c044c5b5e28c22878085b4ca2d09a7e3040b7c5ef23cf84',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r1LevelLoadAndNavJobCandidates ? {
  plan: 'rea-r1-level-load-and-navjob-candidates-plan.json',
  directory: '../reverse-client/r1-level-load-and-navjob-candidates',
  hash: 'd5277c8038dacf7cea05a770cfb95374165f7c54625838360927c2ee2dff6177',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : r1LevelStreamAndNavJob ? {
  plan: 'rea-r1-level-stream-and-navjob-plan.json',
  directory: '../reverse-client/r1-level-stream-and-navjob',
  hash: '4c273fc115c06a692e714a3ebd57e9865e4a79d3b7be9463e51aae70a252a047',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1RegionBlobLoading ? {
  plan: 'rea-r1-region-blob-loading-plan.json',
  directory: '../reverse-client/r1-region-blob-loading',
  hash: 'c01506203adaa0a87733241f7bcdbfada646570d4b7c188df0fea89006440c60',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1RegionLoadPublication ? {
  plan: 'rea-r1-region-load-publication-plan.json',
  directory: '../reverse-client/r1-region-load-publication',
  hash: 'f0fe47ec0a4b59cc2b5010bea627ba03d30f883b3ff7f12b38779f81e7201873',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1NavMeshProductionEntry ? {
  plan: 'rea-r1-navmesh-production-entry-plan.json',
  directory: '../reverse-client/r1-navmesh-production-entry',
  hash: '8e5be16b63798b44ed00da9cd155a893a866aeac2ca522437de640ef95d1183b',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1RegionLoadEntry ? {
  plan: 'rea-r1-region-load-entry-plan.json',
  directory: '../reverse-client/r1-region-load-entry',
  hash: '64c58e7cf8b14d0f28b03369e4d9d5197574b71591d8cce5762c097403bf0f5c',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1NavigationMeshQueryInitialization ? {
  plan: 'rea-r1-navigation-mesh-query-initialization-plan.json',
  directory: '../reverse-client/r1-navigation-mesh-query-initialization',
  hash: 'e0c5f61a4334d06c17612663dd359b786fe5c48d18cd86f644f6b115a6b2aabc',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r1NavigationImplementationConstructor ? {
  plan: 'rea-r1-navigation-implementation-constructor-plan.json',
  directory: '../reverse-client/r1-navigation-implementation-constructor',
  hash: '5ed02c1f7566dfc16e20179fac9e3ea09255fdbd1ca92ff313c1e8f9514049c3',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1NavManagerConstructor ? {
  plan: 'rea-r1-navmanager-constructor-plan.json',
  directory: '../reverse-client/r1-navmanager-constructor',
  hash: '2cf715f5f958c78e52d8933254e1a94aecf776b6ba7d059a18d17ccc3ac17a38',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1NavigationNearestDependencies ? {
  plan: 'rea-r1-navigation-nearest-dependencies-plan.json',
  directory: '../reverse-client/r1-navigation-nearest-dependencies',
  hash: 'e3fadb7795659c0155292631f492478cd5501cadb240cb4465471150e64cc467',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1NavigationNearestHelpers ? {
  plan: 'rea-r1-navigation-nearest-helpers-plan.json',
  directory: '../reverse-client/r1-navigation-nearest-helpers',
  hash: 'e21d33506daa44723f495251a7ec93514874089ed3897596740203fe17f51a84',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r1NavigationPolygonGeometry ? {
  plan: 'rea-r1-navigation-polygon-geometry-plan.json',
  directory: '../reverse-client/r1-navigation-polygon-geometry',
  hash: 'c13fd848eaa2af9fd6d9c945594f420a44190780ed5ae7a719d2cb7e806c8242',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1NavigationSinglePoly ? {
  plan: 'rea-r1-navigation-single-poly-plan.json',
  directory: '../reverse-client/r1-navigation-single-poly',
  hash: 'fb9554a6798c1b77863472d8d46fbc65e8976a1642b2c6972132b84feddf704c',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : r1NavigationCore ? {
  plan: 'rea-r1-navigation-core-plan.json',
  directory: '../reverse-client/r1-navigation-core',
  hash: '01b0fcc060f0393994ff15bb203037218a36165ef67a15ee41f98919f93026c6',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1EngineNavigationSearch ? {
  plan: 'rea-r1-engine-navigation-search-plan.json',
  directory: '../reverse-client/r1-engine-navigation-search',
  hash: '8e8d2f1c6dac84a7d56065b8b5b9e794c847d93083ed52350cfb17b990ffc22d',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1EngineCameraTarget ? {
  plan: 'rea-r1-engine-camera-target-plan.json',
  directory: '../reverse-client/r1-engine-camera-target',
  hash: '2dd9f7ffd03d0eea8b5b12e5ed72538b3847b720d7c39c96011663573ebb68e8',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1GameCameraPosition ? {
  plan: 'rea-r1-game-camera-position-plan.json',
  directory: '../reverse-client/r1-game-camera-position',
  hash: '5c68abc74b3da6181d59a024470f7d9a692e1770fbbb3b7aed2a465c47adb754',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1EnginePortalCamera ? {
  plan: 'rea-r1-engine-portal-camera-plan.json',
  directory: '../reverse-client/r1-engine-portal-camera',
  hash: 'eac21b372c6b745b6f69a31e36c24e562078b48dfb044a4bea954ce8a8d7f165',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r1GameCameraAngle ? {
  plan: 'rea-r1-game-camera-angle-plan.json',
  directory: '../reverse-client/r1-game-camera-angle',
  hash: 'e9f94e39b3ee598117e46ff3321d08abd720f8a2b3432f6a2c20b3ee20dee856',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1GameCameraConstructor ? {
  plan: 'rea-r1-game-camera-constructor-plan.json',
  directory: '../reverse-client/r1-game-camera-constructor',
  hash: '0652deebebb8033bc64fea8f9c6b949bbee1a2919b0370a31745623332d7b408',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1PortalDependencies ? {
  plan: 'rea-r1-portal-dependencies-plan.json',
  directory: '../reverse-client/r1-portal-dependencies',
  hash: '6f158a97dadd77cd40e4186a0ea259f1484c6b0de7b3e7c101a00607967cc775',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r1WaypointQueueHelpers ? {
  plan: 'rea-r1-waypoint-queue-helpers-plan.json',
  directory: '../reverse-client/r1-waypoint-queue-helpers',
  hash: '582f4e4c92e9e93c99a59e659fc12fe8bf8d5788ce428b724d5dd54f9a6a6995',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r1WaypointExecution ? {
  plan: 'rea-r1-waypoint-execution-plan.json',
  directory: '../reverse-client/r1-waypoint-execution',
  hash: '5682d292cec3cb42fd9b66ea0b3f86c600cfb730635eb29a1d7d66581ecf2801',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : r1MoveManager ? {
  plan: 'rea-r1-move-manager-plan.json',
  directory: '../reverse-client/r1-move-manager',
  hash: 'fe9c15db2f44d4fb2489eeb9ffd4c24deeb12fb0f9a0f2dedc92e4703be52c57',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : r1MoveStartDependencies ? {
  plan: 'rea-r1-move-start-dependencies-plan.json',
  directory: '../reverse-client/r1-move-start-dependencies',
  hash: '0cbc2af3dfcb5da2f6196df3d0ffd9912681ae2032847000bf5683ba6c68b324',
  maxToolCalls: 18,
  finalSessionLabel: '18-final-session',
} : r1MoveStart ? {
  plan: 'rea-r1-move-start-plan.json',
  directory: '../reverse-client/r1-move-start',
  hash: 'a19befb1da157fc0c4896f2290df54c2601da4941c9930084301e291f4a673e6',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1ActionSupport ? {
  plan: 'rea-r1-action-support-plan.json',
  directory: '../reverse-client/r1-action-support',
  hash: '1474eba186fbd9f60974ac0b6d82e72f5cb56cf51269504d55413e3526e6ca84',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : r1ActionExecutor ? {
  plan: 'rea-r1-action-executor-plan.json',
  directory: '../reverse-client/r1-action-executor',
  hash: 'd6815cbe3ad945dbc58d8e316bcf1cb0827df0a755e645a695e451bdbf141d8e',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1PathLocalDispatch ? {
  plan: 'rea-r1-path-local-dispatch-plan.json',
  directory: '../reverse-client/r1-path-local-dispatch',
  hash: 'e840bf11d12cc4e6a9b007aa778ec404a8c5ff9d38eeb71d34220a71bcb93d5e',
  maxToolCalls: 12,
  finalSessionLabel: '12-final-session',
} : r1MovementActionPath ? {
  plan: 'rea-r1-movement-action-path-plan.json',
  directory: '../reverse-client/r1-movement-action-path',
  hash: '9dbc1e47d4d82e787c8efa37a057e43b912385cac9b5c121d5c280d72c2d0b4d',
  maxToolCalls: 18,
  finalSessionLabel: '18-final-session',
} : r1PlayerMoveDispatch ? {
  plan: 'rea-r1-player-move-dispatch-plan.json',
  directory: '../reverse-client/r1-player-move-dispatch',
  hash: '06e541e88d8186748bbede79a00a2e52d0f75460dc6a862858173c705900ad66',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r1PlayerMoveEntries ? {
  plan: 'rea-r1-player-move-entries-plan.json',
  directory: '../reverse-client/r1-player-move-entries',
  hash: '6a381e7674c0344065ecfed705a849d4eab1022e95dea6146e3fa5e1383fe1bf',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1PlayerMouseHandlersRetry ? {
  plan: 'rea-r1-player-mouse-handlers-retry-plan.json',
  directory: '../reverse-client/r1-player-mouse-handlers-retry-1',
  hash: '1f3044afb655fbdec0aff0666b00d4300e9f2abef6ca6a0a0a599f93988fd3e2',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1PlayerMouseHandlers ? {
  plan: 'rea-r1-player-mouse-handlers-plan.json',
  directory: '../reverse-client/r1-player-mouse-handlers',
  hash: 'e82a31c3a7f13ecb304dedf83318c60ace5c18efbbefc3f7a24032231cdd7f41',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : r1PlayerRegisterStates ? {
  plan: 'rea-r1-player-register-states-plan.json',
  directory: '../reverse-client/r1-player-register-states',
  hash: 'a79bcab4e9354e9a223a45259626f9fa02eda2a86685b53e92f3d85517dfa01e',
  maxToolCalls: 9,
  finalSessionLabel: '09-final-session',
} : r1GameAddState ? {
  plan: 'rea-r1-game-add-state-plan.json',
  directory: '../reverse-client/r1-game-add-state',
  hash: '09184e1069be04db65bd18ac28a1f66bf7c1d57d2c6ea7d25af21015fa20976b',
  maxToolCalls: 9,
  finalSessionLabel: '09-final-session',
} : r1GameStateRegistrationSearch ? {
  plan: 'rea-r1-game-state-registration-search-plan.json',
  directory: '../reverse-client/r1-game-state-registration-search',
  hash: '97a158ffb0f5bba8833e4f0a51f82c7c4b7f4a584a8a77d3ce27b945a5028d48',
  maxToolCalls: 6,
  finalSessionLabel: '06-final-session',
} : r1GameStateTransition ? {
  plan: 'rea-r1-game-state-transition-plan.json',
  directory: '../reverse-client/r1-game-state-transition',
  hash: '16d1b92eef000e85de4ea7e4aa4c12381e957ea1b295ed349316cee860daa350',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1ExeMouseCallers ? {
  plan: 'rea-r1-exe-mouse-callers-plan.json',
  directory: '../reverse-client/r1-exe-mouse-callers',
  hash: '7c82ed6b2dbb79aac5707346335a555aed9063a19a5bc7f188f6a04ae5e47b81',
  maxToolCalls: 6,
  finalSessionLabel: '06-final-session',
} : r1GameMouseState ? {
  plan: 'rea-r1-game-mouse-state-plan.json',
  directory: '../reverse-client/r1-game-mouse-state',
  hash: 'b30d896a08ea810db23c83863c0935c1bf618dcf87861a8e1f903f9a84142db9',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r1GameMouseLinks ? {
  plan: 'rea-r1-game-mouse-links-plan.json',
  directory: '../reverse-client/r1-game-mouse-links',
  hash: '2f5824eb04f547cfeb39192e857c1515be4c3d88038bd031e3e94878d3ec7f5b',
  maxToolCalls: 9,
  finalSessionLabel: '09-final-session',
} : r1GameMouseCalibration ? {
  plan: 'rea-r1-game-mouse-calibration-plan.json',
  directory: '../reverse-client/r1-game-mouse-calibration',
  hash: '608d4fd05ffacecb95b83aed40ecce39092192282fe0a54f319b2fb06dadc70b',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r1EngineMouseCore ? {
  plan: 'rea-r1-engine-mouse-core-plan.json',
  directory: '../reverse-client/r1-engine-mouse-core',
  hash: '61a87f46105efdc054db9bb2c2f1c7570addf48406002a672c915fa20babc004',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : r1EngineInput ? {
  plan: 'rea-r1-engine-input-plan.json',
  directory: '../reverse-client/r1-engine-input',
  hash: '4dfe23b9a3fc052c29d4fdc22570063bb6dbdc842343587186daad094ee6dfd6',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : r0ToolBaselineTerminalRetry ? {
  plan: 'rea-r0-tool-baseline-terminal-retry-plan.json',
  directory: '../reverse-client/r0-tool-baseline-terminal-retry',
  hash: '5da37b99eaec317f63b9306c3c36a1678c68bf3e2869ea089d0436fba43c7d90',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : r0ToolBaseline ? {
  plan: 'rea-r0-tool-baseline-plan.json',
  directory: '../reverse-client/r0-tool-baseline',
  hash: '93f7aa8b44f0354895307c9bfd9322a49d83d8144dde56f684a57bc88bfd02cc',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : containers ? {
  plan: 'rea-registration-containers-plan.json',
  directory: 'rea-registration-containers',
  hash: 'd43e8709234b703bfa55a338a2b579839a029bfeaf179709831f250439f22668',
  maxToolCalls: 20,
  finalSessionLabel: '20-final-session',
} : containerSupport ? {
  plan: 'rea-container-support-plan.json',
  directory: 'rea-container-support',
  hash: 'f7a2b4899dba830f96c1db438ff6a1ca635ea616f82c7fec30b6d7bf504ecad0',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : containerFill ? {
  plan: 'rea-container-fill-plan.json',
  directory: 'rea-container-fill',
  hash: '34f71ca6bb85ed654e5e76f946d7d78bc3bca36a8b74a6769e251803efbf33fb',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : containerReserve ? {
  plan: 'rea-container-reserve-plan.json',
  directory: 'rea-container-reserve',
  hash: 'f44bd9fccbc053a6bb058f29d34195cc4d35d0c95eff1e0afeb3c16f1016fc8c',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : objectBase ? {
  plan: 'rea-object-base-plan.json',
  directory: 'rea-object-base',
  hash: '52031cb72f37b8722e1782560a483ceadc6411fd08ab42ea710a8cac1d645592',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : objectNameAssignment ? {
  plan: 'rea-object-name-assignment-plan.json',
  directory: 'rea-object-name-assignment',
  hash: '8cfd4de2e4baa7debf90aa5a5bfc9a5c765266ee431af4bd5286f3fc9618012a',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : objectNameGrowth ? {
  plan: 'rea-object-name-growth-plan.json',
  directory: 'rea-object-name-growth',
  hash: 'a8b9f4d66387fb58b4c1ead7037a9e9f6f43a3829b52b1b006bb3b5c36f96294',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : recordLoadDependencies ? {
  plan: 'rea-record-load-dependencies-plan.json',
  directory: 'rea-record-load-dependencies',
  hash: '73337fb202fb932af8312ab799f3aa4403da91f4ab377c5b5cf124bcbe4578f9',
  maxToolCalls: 14,
  finalSessionLabel: '14-final-session',
} : recordCacheCompare ? {
  plan: 'rea-record-cache-compare-plan.json',
  directory: 'rea-record-cache-compare',
  hash: '7c8ab947e489c860d9be56f93f1666409f8cc1f272ae7aae8c2a76e4e2535cd3',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : recordClassGetters ? {
  plan: 'rea-record-class-getters-plan.json',
  directory: 'rea-record-class-getters',
  hash: '344044fcca3774a89eda63be0ab39035e8b5a93e05565abe32d5ac6d3b47aebc',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : recordGetterSupport ? {
  plan: 'rea-record-getter-support-plan.json',
  directory: 'rea-record-getter-support',
  hash: '96cecea8e627427d61d275f3db086078bb076011c1b9af7e1827070ae4159547',
  maxToolCalls: 17,
  finalSessionLabel: '17-final-session',
} : recordCacheMaintenance ? {
  plan: 'rea-record-cache-maintenance-plan.json',
  directory: 'rea-record-cache-maintenance',
  hash: 'd4f2231584dd5ed268cdddb3eb0ef55b3aa3743840fac669bf187b7712a6dac8',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : recordCacheNode ? {
  plan: 'rea-record-cache-node-plan.json',
  directory: 'rea-record-cache-node',
  hash: '48b235048fc20dba98c718d29d6511600833199bc248f9b712b104b9a6e20e44',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : recordCachePublication ? {
  plan: 'rea-record-cache-publication-plan.json',
  directory: 'rea-record-cache-publication',
  hash: 'b8edfd6904516091befee8c69d6111080b5c7f58ad342b6a69d378f5589eb762',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : binaryParserVector ? {
  plan: 'rea-binary-parser-vector-growth-plan.json',
  directory: 'rea-binary-parser-vector-growth',
  hash: 'f4ff0db29c3ef1eae0df15425f11dac673d04635eb317fb7153e6987d53dbf38',
  maxToolCalls: 7,
  finalSessionLabel: '07-final-session',
} : binaryParserHelpers ? {
  plan: 'rea-binary-parser-helpers-plan.json',
  directory: 'rea-binary-parser-helpers',
  hash: '4a1bf562bb0875a898b57c0f09a16de1c028e84dfeb8ae69b8ddbb45b04e67a6',
  maxToolCalls: 11,
  finalSessionLabel: '11-final-session',
} : binaryParserEntry ? {
  plan: 'rea-binary-parser-entry-plan.json',
  directory: parserRetryAfterExitRace ? 'rea-binary-parser-entry-retry-2' : parserRetry ? 'rea-binary-parser-entry-retry-1' : 'rea-binary-parser-entry',
  hash: 'adf41b0f3ec4d59f11352b21eb3173c8cb077a638a452c0f21f488b6b21251ff',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : binarySubobjectProfile ? {
  plan: 'rea-binary-subobject-init-plan.json',
  directory: 'rea-binary-subobject-profile',
  hash: 'b10df059c65aa0dd3d42abf880f8b1147cf3fcf98db856866a987b4fc03f3e1e',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : binaryTableConstructor ? {
  plan: 'rea-binary-table-constructor-plan.json',
  directory: 'rea-binary-table-constructor',
  hash: '9eb574c85e2bd680d20623deb50079df655913b05979aa29f036bbb4e9492d6e',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : binaryPointerGrowth ? {
  plan: 'rea-binary-pointer-growth-plan.json',
  directory: 'rea-binary-pointer-growth',
  hash: '433d3614559a25c5ef2d09e90d24ee6c1ed469e11e15c7fdb4db628b77512a9e',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : binaryColdInsert ? {
  plan: 'rea-binary-cold-insert-plan.json',
  directory: 'rea-binary-cold-insert',
  hash: 'c158a3dc653eb175e248e8162ff874bd65fdf11a65ae96941811b54c2a381c3b',
  maxToolCalls: 8,
  finalSessionLabel: '08-final-session',
} : {
  plan: 'rea-native-registration-plan.json',
  directory: 'rea-native-registration',
  hash: '5d217f9f43baa7311805df34e3841d7b6db36c5c1837afc58a9d650c20472eb5',
  maxToolCalls: 18,
  finalSessionLabel: '18-final-session',
};
const planPath = path.join(root, 'tools/static_analysis', batch.plan);
const output = path.join(root, 'docs/object-lifecycle', batch.directory);
const expectedPlanHash = batch.hash;
const {maxToolCalls, finalSessionLabel} = batch;
const targetPath = r1ExeMouseCallers ? 'D:/Steam/steamapps/common/Grim Dawn/x64/Grim Dawn.exe' : r1GameFixedMode ? 'D:/Steam/steamapps/common/Grim Dawn/x64/Game.dll' : 'D:/Steam/steamapps/common/Grim Dawn/x64/Engine.dll';
const targetHash = r1ExeMouseCallers ? 'd3915e2599317b6670cc088ba958afb2b2f4fa8141cfd1d777ee02f9ea1fc4c4' : r1GameFixedMode ? 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2' : '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153';
const allowed = r1GameAnimationSetProduction ? ['binary_session','open_binary','close_binary','procedure_pseudo_code','procedure_assembly'] : r1EngineAnimationSpeed ? ['binary_session','open_binary','close_binary','procedure_pseudo_code','procedure_assembly','procedure_callees','procedure_callers'] : r1GameAnimationSetNames ? ['binary_session','open_binary','close_binary','search_procedures'] : r1EngineReachedGoalDependencies ? ['binary_session','open_binary','close_binary','procedure_pseudo_code','procedure_assembly','procedure_callees','procedure_callers','xrefs'] : (r1LevelObjectRecords || r1MoveWorldDistance || r1MoveProductionEngine || r1EngineMouseGetters || r1GameAssociated || r1MoveActionDispatchLeaves || r1DurationManagerDirect || r1DurationManagerConstructor || r1GameRunSpeedDirect || r1GameRunSpeed || r1GameStateLookupDirect || r1GameMainPlayerConnectionMethods || r1GameMainPlayerConnection || r1GamePlayerVideoDirect || r1GameMouseDirectDependencies || r1GameSkillReferenceDirect || r1GameStopStateDirect || r1GameReachedGoalDependencies || r1SkillMovedDirect || r1GameLevelPublicationTail || r1GameEntityPublicationGates || r1GameMovementMath || r1GameCallbackRemainingRetry || r1GameCallbackRemaining || r1GameCallbackCore || r1GameMovementInterface || r1GameEntityConstructors || r1WorldPositionLevelInitialization || r1TerrainTypeMaterial || r1WaterInitialization || r1GroundResourcesWater || r1GroundFaceUnlink || r1GroundIntersectionRemaining || r1GroundIntersectionDirect || r1GroundRayTree || r1GroundFloorGeometry || r1GroundBlocksNodeDestroy || r1SpatialGroundDirect || r1EngineSpatialGround || r1EngineEntityPublicationCore || r1SameRegionMovement || r1PositionPublication || r1ActorRadius || r1CrowdPointCopy || r1CrowdPointReallocation || r1CrowdDirectAlgorithms || r1EngineCallbackGetters || r1CrowdMotionBranches || r1CrowdVectorGrowth || r1CrowdMotionHelpers || r1CrowdAgentGroupRegistration || r1CrowdVectorRegistration || r1CrowdAdapterRegistration || r1CrowdQueryProduction || r1CrowdMovementDirect || r1MovementFloorCore || r1NavigationConsumerDirect || r1StraightPathPortalWrapper || r1CorridorPortalHelper || r1NavigationCrossPolyCorridor || r1TriangulationAndTileConnection || r1MeshLinksAndTriangulationLeaf || r1MeshTileAndPredicates || r1NavPublishAndPolygonCore || r1NavPublicationAndContourLeaves || r1NavUpdateAndContours || r1TileProductionDirectStages || r1TileCacheAndDecompression || r1MapRegionAndTileProduct || r1MapDirectoryAndPathTiles || r1WorldLoadAndNavPublication || r1LevelLoadAndNavJobCandidates || r1LevelStreamAndNavJob || r1RegionBlobLoading || r1RegionLoadPublication || r1NavMeshProductionEntry || r1RegionLoadEntry || r1NavigationMeshQueryInitialization || r1NavigationImplementationConstructor || r1NavManagerConstructor || r1NavigationNearestDependencies || r1NavigationNearestHelpers || r1NavigationPolygonGeometry || r1NavigationSinglePoly || r1NavigationCore || r1EngineNavigationSearch || r1EngineCameraTarget || r1EnginePortalCamera) ? ['binary_session','open_binary','close_binary','procedure_pseudo_code','procedure_assembly','procedure_callees'] : (r1GameAddState || r1PlayerRegisterStates) ? ['binary_session', 'open_binary', 'close_binary', 'procedure_pseudo_code', 'procedure_assembly', 'procedure_callees', 'procedure_callers'] : (r1GameEntityConstructorSearch || r1GameStateRegistrationSearch) ? ['binary_session', 'open_binary', 'close_binary', 'search_procedures'] : r1ExeMouseCallers ? ['binary_session', 'open_binary', 'close_binary', 'xrefs'] : recordCacheFixedPair || r1GameStateTransition || r1PlayerMouseHandlers
  ? ['binary_session', 'open_binary', 'close_binary', 'procedure_pseudo_code', 'procedure_assembly', 'procedure_callees']
  : ['binary_session', 'open_binary', 'close_binary', 'procedure_pseudo_code', 'procedure_assembly', 'procedure_info', 'procedure_callees', 'xrefs', 'address_name'];
const requestTimeout = 600000;
const byteLimit = 32 * 1024 * 1024;
const digest = value => createHash('sha256').update(value).digest('hex');
const fileHash = filename => digest(fs.readFileSync(filename));
const samePath = (left, right) => typeof left === 'string' && path.resolve(left).toLowerCase() === path.resolve(right).toLowerCase();
const canonical = value => JSON.stringify(order(value));
function order(value) {
  if (Array.isArray(value)) return value.map(order);
  if (value && typeof value === 'object') return Object.fromEntries(Object.keys(value).sort().map(key => [key, order(value[key])]));
  return value;
}

// Evidence is saved with credential values redacted, including JSON embedded in text blocks.
const secrets = new Set();
const secretKey = /^(?:authorization|cookie|set-cookie|password|secret|api[_-]?key|access[_-]?token|refresh[_-]?token|session[_-]?token|auth[_-]?token|bearer[_-]?token|token)$/i;
function collectSecrets(value) {
  if (!value || typeof value !== 'object') return;
  for (const [key, child] of Object.entries(value)) {
    if (secretKey.test(key) && typeof child === 'string' && child.length > 3) secrets.add(child);
    else if (typeof child === 'object') collectSecrets(child);
    else if (typeof child === 'string' && /^[\s]*[\[{]/.test(child)) {
      try { collectSecrets(JSON.parse(child)); } catch { /* Ordinary analysis text. */ }
    }
  }
}
function redactText(value) {
  let text = value.replace(/\bBearer\s+[^\s"'<>]+/gi, 'Bearer [REDACTED]');
  text = text.replace(/((?:session[_-]?token|auth[_-]?token|access[_-]?token|refresh[_-]?token|api[_-]?key|password|secret|authorization|token)(?:\\?["'])?\s*[:=]\s*(?:\\?["'])?)([^\s,"';}\\]+)/gi, '$1[REDACTED]');
  for (const secret of secrets) text = text.split(secret).join('[REDACTED]');
  return text;
}
function redact(value) {
  if (Array.isArray(value)) return value.map(redact);
  if (value && typeof value === 'object') return Object.fromEntries(Object.entries(value).map(([key, child]) => [key, secretKey.test(key) ? '[REDACTED]' : redact(child)]));
  if (typeof value !== 'string') return value;
  if (/^[\s]*[\[{]/.test(value)) {
    try { return JSON.stringify(redact(JSON.parse(value))); } catch { /* Not embedded JSON. */ }
  }
  return redactText(value);
}
function save(name, value) {
  assert.match(name, /^[A-Za-z0-9_-]+$/);
  collectSecrets(value);
  fs.writeFileSync(path.join(output, `${name}.json`), JSON.stringify(redact(value), null, 2), {flag: 'wx'});
}
function progress(state, detail = {}) {
  const entry = {utc: new Date().toISOString(), state, ...detail};
  const text = JSON.stringify(redact(entry));
  fs.appendFileSync(path.join(output, 'progress.ndjson'), `${text}\n`);
  console.log(text);
}

// Validate the exact pinned plan against the actual catalog; never import vendor code here.
function validateArgs(args, schema) {
  const resolve = node => {
    if (!node.$ref) return node;
    assert.match(node.$ref, /^#\/\$defs\/[A-Za-z0-9_]+$/);
    const target = schema.$defs?.[node.$ref.split('/').at(-1)];
    assert.ok(target, 'Unresolved input-schema reference');
    return {...resolve(target), ...Object.fromEntries(Object.entries(node).filter(([key]) => key !== '$ref'))};
  };
  const valid = (value, input) => {
    const node = resolve(input);
    if (node.anyOf && !node.anyOf.some(choice => valid(value, choice))) return false;
    if ('const' in node && value !== node.const) return false;
    if (node.enum && !node.enum.includes(value)) return false;
    if (node.type === 'string' && (typeof value !== 'string' || value.length < (node.minLength ?? 0) || (node.pattern && !new RegExp(node.pattern, 'u').test(value)))) return false;
    if (node.type === 'boolean' && typeof value !== 'boolean') return false;
    return true;
  };
  assert.equal(schema.type, 'object');
  assert.equal(schema.additionalProperties, false);
  for (const required of schema.required ?? []) assert.ok(Object.hasOwn(args, required), `Missing argument: ${required}`);
  for (const [key, value] of Object.entries(args)) {
    assert.ok(Object.hasOwn(schema.properties, key), `Unrecognized argument: ${key}`);
    assert.ok(valid(value, schema.properties[key]), `Invalid argument: ${key}`);
  }
}
function failureIn(value) {
  if (!value || typeof value !== 'object') return null;
  if (value.error != null && value.error !== false) return value.error;
  if (value.isError === true) return 'MCP isError=true';
  if (typeof value.code === 'string' && typeof value.category === 'string' && typeof value.message === 'string') return value;
  if (['failed', 'error', 'unavailable', 'unsupported', 'cancelled', 'timeout'].includes(value.status)) return value;
  return null;
}
function payload(response) {
  for (const value of [response, response.result, response.result?.structuredContent]) {
    const error = failureIn(value);
    if (error) throw Error(`Tool error: ${JSON.stringify(redact(error))}`);
  }
  const data = response.result?.structuredContent;
  assert.ok(data && typeof data === 'object', 'Missing structuredContent');
  for (const value of [data.result, data.normalized_result]) {
    const error = failureIn(value);
    if (error) throw Error(`Structured tool error: ${JSON.stringify(redact(error))}`);
  }
  return data;
}
function verifyTarget(target) {
  assert.ok(target && samePath(target.path ?? target.local_path, targetPath), 'Unexpected target path');
  assert.equal(target.sha256 ?? target.digest?.sha256, targetHash, 'Unexpected target SHA256');
  assert.equal(target.format, 'pe');
  assert.equal(target.architecture, 'x86_64');
  if (Object.hasOwn(target, 'kind')) assert.equal(target.kind, 'executable');
}
function verifySession(data, expectedOpen) {
  const session = data.result;
  assert.equal(session?.open, expectedOpen, 'Unexpected session open state');
  const identity = session.server_identity;
  assert.equal(identity?.package?.name, 'rea-agents');
  assert.equal(identity?.package?.version, '6.5.0');
  assert.equal(identity?.server?.name, 'rea');
  assert.equal(identity?.server?.version, '6.5.0');
  assert.ok(samePath(identity.package.root_path, packageRoot), 'Unexpected package root');
  assert.ok(samePath(identity.server.command_path, launcher), 'Unexpected server entrypoint');
  assert.equal(identity.alignment?.state, 'aligned', 'Server expectations were not confirmed');
  if (expectedOpen) {
    verifyTarget(session);
    assert.equal(session.analysis_provider_binding?.provider?.id, 'ghidra', 'Ghidra is not bound');
    assert.equal(session.analysis_provider_binding?.selection_source, 'request');
    const selected = session.analysis_provider_candidates?.find(candidate => candidate.selected);
    assert.equal(selected?.provider?.id, 'ghidra');
    assert.equal(selected.availability?.status, 'available');
    assert.equal(selected.target_support?.status, 'supported');
  } else {
    assert.equal(session.analysis_provider_binding, null, 'Provider still bound after close');
    assert.equal(session.analysis_run, null, 'Analysis run still present after close');
    assert.ok(!session.path, 'Closed session retains an active target path');
  }
}
function verifyEvidence(data, call) {
  assert.equal(data.operation, call.tool, 'Evidence operation mismatch');
  if ((r1GameAnimationSetNames || r1GameEntityConstructorSearch || r1GameStateRegistrationSearch) && call.tool === 'search_procedures') {
    assert.equal(data.parameters?.pattern, call.args.pattern, 'Evidence search pattern mismatch');
    assert.equal(data.parameters?.mode, call.args.mode, 'Evidence search mode mismatch');
    assert.equal(data.parameters?.case_sensitive, call.args.case_sensitive, 'Evidence search case mismatch');
  } else if ((r1EngineReachedGoalDependencies || r1GameMouseLinks || r1ExeMouseCallers || (r1EngineAnimationSpeed && call.args.address === '0x1800743c0')) && call.tool === 'xrefs') assert.equal(data.parameters?.address, call.args.address, 'Evidence address mismatch');
  else if(r1EngineAnimationSpeed && ['inspect_native_instruction','resolve_containing_procedure','prev_address'].includes(call.tool)) assert.equal(data.parameters?.address,call.args.address,'Evidence exact requested address mismatch');
  else assert.equal(data.parameters?.procedure, call.args.procedure, 'Evidence procedure mismatch');
  assert.equal(data.provider?.id, 'ghidra', 'Evidence provider mismatch');
  verifyTarget(data.subject);
  assert.equal(typeof data.evidence_id, 'string');
  if ((r1GameAnimationSetNames || r1GameEntityConstructorSearch || r1GameStateRegistrationSearch) && call.tool === 'search_procedures') {
    // Empty matches are valid. No result limit or candidate behavior is inferred.
    assert.ok(Array.isArray(data.normalized_result) && data.normalized_result.every(item =>
      item !== null && typeof item === 'object' && !Array.isArray(item) &&
      typeof item.address === 'string' && typeof item.value === 'string'), 'Procedure search rows unavailable');
    if (r1GameAnimationSetNames) {
      assert.ok(data.normalized_result.every(item =>
        canonical(Object.keys(item).sort()) === canonical(['address','value']) &&
        /^0x[0-9a-f]+$/.test(item.address) && item.value.includes('AnimationSet')),
      'Name search result outside fixed literal address/value contract; no widening permitted');
    } else if (r1GameEntityConstructorSearch) {
      const exactName = new RegExp(gameEntityConstructorPattern, 'u');
      assert.ok(data.normalized_result.every(item =>
        canonical(Object.keys(item).sort()) === canonical(['address','value']) &&
        /^0x[0-9a-f]+$/.test(item.address) && exactName.test(item.value)),
      'Constructor search result outside fixed address/value name contract; no widening permitted');
    }
  } else if ((r1EngineReachedGoalDependencies || r1GameMouseLinks || r1ExeMouseCallers || (r1EngineAnimationSpeed && call.args.address === '0x1800743c0')) && call.tool === 'xrefs') {
    // Bare source addresses are untyped and may be incomplete; an empty set is valid.
    assert.ok(Array.isArray(data.normalized_result) && data.normalized_result.every(value => typeof value === 'string'), 'Xrefs unavailable');
  } else if (call.tool === 'procedure_callees' || ((r1EngineAnimationSpeed || r1EngineReachedGoalDependencies || r1GameAddState || r1PlayerRegisterStates) && call.tool === 'procedure_callers')) assert.ok(Array.isArray(data.normalized_result) && data.normalized_result.every(value => typeof value === 'string'), 'Call relationships unavailable');
  else if(r1EngineAnimationSpeed && call.tool==='resolve_containing_procedure') {
    const observed=data.normalized_result;assert.ok(observed && typeof observed==='object' && !Array.isArray(observed),'Containing-procedure observation unavailable');assert.equal(observed.query_address,call.args.address);assert.equal(typeof observed.found,'boolean');
    if(observed.found){assert.ok(observed.procedure && typeof observed.procedure==='object');assert.equal(typeof observed.procedure.address,'string');assert.equal(typeof observed.procedure.name,'string');assert.ok(observed.procedure.body && typeof observed.procedure.body==='object');}
    else{assert.equal(observed.procedure,null);assert.ok(['outside_segments','not_in_procedure'].includes(observed.reason),'Negative ownership reason unavailable');}
  } else if(r1EngineAnimationSpeed && call.tool==='prev_address') {
    assert.ok(typeof data.normalized_result==='string' && data.normalized_result.trim().length>0,'Previous analyzed-object address unavailable');
    // Navigation only: no control-flow predecessor or function-entry inference.
  }
  else if(r1EngineAnimationSpeed && call.tool==='inspect_native_instruction') {
    const observed=data.normalized_result;
    assert.ok(observed && typeof observed==='object' && !Array.isArray(observed),'Typed instruction/data observation unavailable');
    assert.equal(observed.address,call.args.address,'Returned exact address mismatch');
    assert.ok(['decoded','not-instruction-boundary','data','undecodable','outside-memory'].includes(observed.status),'Unknown address-inspection status');
    assert.ok(observed.procedure===null || typeof observed.procedure==='string','Missing nullable procedure observation');
    assert.ok(Array.isArray(observed.operands) && Array.isArray(observed.references) && Array.isArray(observed.limitations) && observed.flow && typeof observed.flow==='object','Typed inspection facets unavailable');
    assert.ok(observed.incoming_references===undefined || observed.incoming_references===null || Array.isArray(observed.incoming_references),'Incoming-reference availability must remain explicit');
    // Data/non-boundary/unknown outcomes are retained observations, never inferred function bodies or runtime writers.
  }
  else assert.ok(typeof data.normalized_result === 'string' && data.normalized_result.trim().length > 0, 'Analysis body unavailable');
}

async function main() {
  assert.ok(process.argv.length === 2 || containers || containerSupport || containerFill || containerReserve || objectBase || objectNameAssignment || objectNameGrowth || recordLoadDependencies || recordCacheCompare || recordClassGetters || recordGetterSupport || binaryColdInsert || binaryPointerGrowth || binaryTableConstructor || ownedMemoryMode, 'This fixed runner accepts only reviewed batch arguments');
  const gib = 1024 ** 3;
  const launchBytes = (r1LevelObjectRecords || r1MoveWorldDistance || r1MoveProductionEngine || r1EngineMouseGetters || r1EngineReachedGoalDependencies || r1WorldPositionLevelInitialization || r1TerrainTypeMaterial || r1WaterInitialization || r1GroundResourcesWater || r1GroundFaceUnlink || r1GroundIntersectionRemaining || r1GroundIntersectionDirect || r1GroundRayTree || r1GroundFloorGeometry || r1GroundBlocksNodeDestroy || r1SpatialGroundDirect || r1EngineSpatialGround || r1EngineEntityPublicationCore || r1SameRegionMovement || r1PositionPublication || r1ActorRadius || r1CrowdPointCopy || r1CrowdPointReallocation || r1CrowdDirectAlgorithms || r1EngineCallbackGetters || r1CrowdMotionBranches || r1CrowdVectorGrowth || r1CrowdMotionHelpers || r1CrowdAgentGroupRegistration || r1CrowdVectorRegistration || r1CrowdAdapterRegistration || r1CrowdQueryProduction || r1CrowdMovementDirect || r1MovementFloorCore || r1NavigationConsumerDirect || r1StraightPathPortalWrapper || r1CorridorPortalHelper || r1NavigationCrossPolyCorridor || r1TriangulationAndTileConnection || r1MeshLinksAndTriangulationLeaf || r1MeshTileAndPredicates || r1NavPublishAndPolygonCore || r1NavPublicationAndContourLeaves || r1NavUpdateAndContours || r1TileProductionDirectStages || r1TileCacheAndDecompression || r1MapRegionAndTileProduct || r1MapDirectoryAndPathTiles || r1WorldLoadAndNavPublication || r1LevelLoadAndNavJobCandidates || r1LevelStreamAndNavJob || r1RegionBlobLoading || r1RegionLoadPublication || r1NavMeshProductionEntry || r1RegionLoadEntry || r1NavigationMeshQueryInitialization || r1NavigationImplementationConstructor || r1NavManagerConstructor || r1NavigationNearestDependencies || r1NavigationNearestHelpers || r1NavigationPolygonGeometry || r1NavigationSinglePoly || r1NavigationCore || r1EngineNavigationSearch || r1EngineCameraTarget || r1EnginePortalCamera) ? 5 * gib : parserBudgetMode || r1GameFixedMode || r1ExeMouseCallers ? budget.launchBytes : 6 * gib;
  const stopBytes = parserBudgetMode || r1GameFixedMode || r1ExeMouseCallers ? budget.stopBytes : 5 * gib;
  const checkLaunch = snapshot => {
    checkResourceBudget(snapshot, budget, 'launch');
    if (r1LevelObjectRecords || r1MoveWorldDistance || r1MoveProductionEngine || r1EngineMouseGetters || r1EngineReachedGoalDependencies || r1WorldPositionLevelInitialization || r1TerrainTypeMaterial || r1WaterInitialization || r1GroundResourcesWater || r1GroundFaceUnlink || r1GroundIntersectionRemaining || r1GroundIntersectionDirect || r1GroundRayTree || r1GroundFloorGeometry || r1GroundBlocksNodeDestroy || r1SpatialGroundDirect || r1EngineSpatialGround || r1EngineEntityPublicationCore || r1SameRegionMovement || r1PositionPublication || r1ActorRadius || r1CrowdPointCopy || r1CrowdPointReallocation || r1CrowdDirectAlgorithms || r1EngineCallbackGetters || r1CrowdMotionBranches || r1CrowdVectorGrowth || r1CrowdMotionHelpers || r1CrowdAgentGroupRegistration || r1CrowdVectorRegistration || r1CrowdAdapterRegistration || r1CrowdQueryProduction || r1CrowdMovementDirect || r1MovementFloorCore || r1NavigationConsumerDirect || r1StraightPathPortalWrapper || r1CorridorPortalHelper || r1NavigationCrossPolyCorridor || r1TriangulationAndTileConnection || r1MeshLinksAndTriangulationLeaf || r1MeshTileAndPredicates || r1NavPublishAndPolygonCore || r1NavPublicationAndContourLeaves || r1NavUpdateAndContours || r1TileProductionDirectStages || r1TileCacheAndDecompression || r1MapRegionAndTileProduct || r1MapDirectoryAndPathTiles || r1WorldLoadAndNavPublication || r1LevelLoadAndNavJobCandidates || r1LevelStreamAndNavJob || r1RegionBlobLoading || r1RegionLoadPublication || r1NavMeshProductionEntry || r1RegionLoadEntry || r1NavigationMeshQueryInitialization || r1NavigationImplementationConstructor || r1NavManagerConstructor || r1NavigationNearestDependencies || r1NavigationNearestHelpers || r1NavigationPolygonGeometry || r1NavigationSinglePoly || r1NavigationCore || r1EngineNavigationSearch || r1EngineCameraTarget || r1EnginePortalCamera) {
      assert.ok(snapshot.available_physical_bytes >= launchBytes, 'Engine camera physical below5GiB');
      assert.ok(snapshot.system_commit_limit_bytes - snapshot.system_commit_bytes >= launchBytes, 'Engine camera commit headroom below5GiB');
    }
  };
  const memoryAllowed = (bytes, starting) => Number.isFinite(bytes) && bytes >= (starting ? launchBytes : stopBytes);
  if (memoryGuardEnabled && !verifyParserPlanOnly) {
    assert.ok(memoryAllowed(os.freemem(), true), `Less than ${launchBytes / gib} GiB free physical memory; backend not started`);
  }
  assert.equal(fileHash(planPath), expectedPlanHash, 'Plan changed; review before execution');
  const plan = JSON.parse(fs.readFileSync(planPath, 'utf8'));
  if (r1ReachedGoalDependencies) {
    // Frozen query input stays unchanged; reuse only pinned runtime protection metadata.
    assert.equal(plan.status, 'prepared-not-executed'); assert.equal(plan.runner_mode_registered, false);
    assert.ok(!fs.existsSync(output), 'Reached-goal output already exists; review prior outcome');
    assert.equal(fileHash(path.join(root,batch.runtimeProfile.file)),batch.runtimeProfile.sha256);
    const profile = JSON.parse(fs.readFileSync(path.join(root,batch.runtimeProfile.file),'utf8'));
    const keys = ['memory_profile','time_limits','monitor_revision','diagnostic_monitor_review','sampler_observation_deployment','recovery_requirement',
      ...((r1GameAssociated || r1MoveActionDispatchLeaves || r1DurationManagerDirect || r1DurationManagerConstructor || r1GameRunSpeedDirect || r1GameRunSpeed || r1GameStateLookupDirect || r1GameMainPlayerConnectionMethods || r1GameMainPlayerConnection || r1GamePlayerVideoDirect || r1GameMouseDirectDependencies || r1GameSkillReferenceDirect || r1GameStopStateDirect || r1GameReachedGoalDependencies) ? ['prior_engine_mouse_core','prior_game_trial_observation'] : [])];
    for (const key of keys) { assert.equal(Object.hasOwn(plan,key),false); assert.ok(Object.hasOwn(profile,key)); plan[key]=profile[key]; }
    if (r1GameAnimationSetProduction) {
      const sources=[{"file":".tooling/gd-controller-nonzero-association-20261104/inspection.json","sha256":"97bac1fc6e3009330bc84237790172118e739b96206c23a18aca78d605b3bc41","addresses":["0x180127b70","0x180123810"],"source_kind":"Exact original read receipt: Game vtable6E0350 +60/+80 disk pointers; loaded dispatch and runtime object remain unverified."}];assert.equal(canonical(plan.address_sources),canonical(sources));assert.equal(fileHash(path.join(root,sources[0].file)),sources[0].sha256);
      const slots=JSON.parse(fs.readFileSync(path.join(root,sources[0].file),'utf8'));
      assert.equal(slots.status,'passed-exact-three-slots-disk-read');assert.equal(path.resolve(slots.target),path.resolve(targetPath));assert.equal(slots.target_sha256,targetHash);
      for(const key of ['sha256_before','sha256_after'])assert.equal(slots[key],targetHash);
      for(const key of ['source_closed','source_stat_stable'])assert.equal(slots[key],true);
      for(const key of ['original_loaded_or_executed','original_modified','process_memory_read','target_payload_read'])assert.equal(slots[key],false);
      assert.equal(slots.actual_bytes,24);assert.equal(slots.slots.length,3);assert.equal(canonical(slots.failures),'[]');assert.equal(slots.table_rva,'0x6e0350');
      const fixedSlots=[['0x60','0x6e03b0','0x6de9b0','707b128001000000','0x180127b70','0x127b70'],['0x80','0x6e03d0','0x6de9d0','1038128001000000','0x180123810','0x123810']];
      for(let i=0;i<fixedSlots.length;i++){const s=slots.slots[i];assert.equal(canonical([s.slot,s.slot_rva,s.file_offset,s.raw_hex,s.pointer_preferred_va,s.candidate_rva]),canonical(fixedSlots[i]));assert.equal(s.kind,'vtable');assert.equal(s.base_rva,'0x6e0350');assert.equal(s.requested_bytes,8);assert.equal(s.actual_bytes,8);for(const key of ['is_null','payload_read','runtime_binding_observed'])assert.equal(s[key],false);assert.equal(Buffer.from(s.raw_hex,'hex').readBigUInt64LE(),BigInt(s.pointer_preferred_va));}
      const recovery=plan.failed_name_search_retention;
      assert.equal(canonical(recovery),canonical({"summary":{"file":"docs/reverse-client/r1-movement-manager-name-search-run1/summary.json","sha256":"43845feb2f1f539534e601bf515429b05bcd8a76ff455bff12a4ba64767400c0"},"retention":{"file":"docs/reverse-client/r1-movement-manager-name-search-run1/runtime-retention.json","sha256":"707ed38e057fce519dd24aeb93bc10afdaa15dde68d5aa22dd669c1b39adcf5a"},"old_closed_session_verified":false,"old_job_empty_confirmed":false,"retry_name_search":false}));for(const pin of [recovery.summary,recovery.retention])assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);
      const failed=JSON.parse(fs.readFileSync(path.join(root,recovery.summary.file),'utf8'));
      assert.equal(failed.status,'failed');assert.equal(failed.tool_calls,4);assert.equal(failed.completed_plan_calls,3);assert.equal(failed.close_acknowledged,false);assert.equal(failed.closed_session_verified,false);assert.equal(failed.process_exit_confirmed,true);assert.equal(failed.forced_termination,true);
      assert.equal(failed.owned_memory_profile.job_empty_confirmed,false);assert.equal(failed.owned_memory_profile.sampler_exit_confirmed,true);assert.equal(failed.owned_memory_profile.sampler_exit_code,1);
      for(const key of ['sha256_before','sha256_after'])assert.equal(failed.target[key],targetHash);
      const retained=JSON.parse(fs.readFileSync(path.join(root,recovery.retention.file),'utf8'));
      assert.equal(retained.status,'retained-content-unchanged');assert.equal(retained.source_absent,true);assert.equal(retained.file_count,43);assert.equal(retained.manifest.length,43);
      for(const key of ['old_closed_session_verified','old_job_empty_confirmed','originals_opened'])assert.equal(retained[key],false);
      assert.equal(retained.old_terminal_flags_not_promoted,true);assert.equal(retained.job_cleanup_inference_only,true);assert.equal(retained.processes_terminated,0);assert.equal(retained.deleted_files,0);
      assert.equal(path.resolve(retained.source),path.join(root,'analysis-runtime/rea/rea-ghidra-4ebbea6809586aa9a730e2c4600bafc8'));
      assert.equal(path.resolve(retained.destination),path.join(root,'analysis-runtime/retained/r1-movement-manager-name-search-failed-20261102T204106Z'));
      assert.ok(!fs.existsSync(retained.source));const retainedStat=fs.lstatSync(retained.destination);assert.ok(retainedStat.isDirectory()&&!retainedStat.isSymbolicLink());
      assert.equal(retained.checks.length,3);for(const check of retained.checks){assert.equal(check.recorded_pid_count,50);assert.equal(check.matches,0);assert.equal(check.relevant_unreadable_commandlines,0);}
      assert.ok(!fs.existsSync(path.join(root,'docs/reverse-client/r1-movement-manager-name-search-run1/04-CharacterMovementManager-literal-name-search.json')),'Previously unreturned search response appeared; review before launch');
    }
    if (r1EngineAnimationSpeed) {
      assert.equal(plan.planned_output,'docs/reverse-client/r1-engine-lua-provider-registration-run1');
      assert.equal(plan.calls[7].label,'08-Engine-close'); assert.equal(plan.calls[8].label,'09-final-session');
      assert.equal(plan.unchanged_guard_pins.length,8);
      for (const pin of plan.unchanged_guard_pins) assert.equal(fileHash(path.join(root,pin.file)),pin.sha256,'Unchanged Engine guard changed');
      const sources=[{"file":"docs/reverse-client/r1-lua-base-log-slot.json","sha256":"0178883646ad899fa03ff2dcf512fe06c57e05c9b59bded0857c307146fd3bd9","addresses":["0x180077050"],"slot_va":"0x18033e308","slot_bytes":"5070078001000000","source_kind":"Exact8-byte baseconstructor-vtable slot read withoriginal/copy hashes;runtime global dynamic type remainsunknown"}];assert.equal(canonical(plan.address_sources),canonical(sources));for(const source of sources)assert.equal(fileHash(path.join(root,source.file)),source.sha256);
      const slot=JSON.parse(fs.readFileSync(path.join(root,sources[0].file),'utf8'));assert.equal(slot.slot_va,'0x18033e308');assert.equal(slot.bytes,'5070078001000000');assert.equal(slot.target_va,'0x180077050');assert.equal(slot.semantic_bytes,8);assert.equal(slot.runtime_global_dynamic_type_proven,false);assert.equal(slot.original_loaded,false);assert.equal(Buffer.from(slot.bytes,'hex').readBigUInt64LE(),0x180077050n);for(const phase of ['before','after']){assert.equal(slot[phase].length,2);for(const item of slot[phase])assert.equal(item.sha256,targetHash);}
      const reused=[{"file":"docs/reverse-client/r1-engine-lua-manager-core-run1/07-LuaManagerInitialize-0x1800f2e00-procedure_pseudo_code.json","sha256":"f0178bba6c20f67ab1350fac14c3d06cb9d08bb8b18a462b432b9a17ef2e6126","operation":"procedure_pseudo_code","procedure":"0x1800f2e00"},{"file":"docs/reverse-client/r1-engine-lua-manager-core-run1/08-LuaManagerInitialize-0x1800f2e00-procedure_assembly.json","sha256":"189560d9bffcb960574a4477ef472497a78d9263f4f1fd017b70c2bd0980464a","operation":"procedure_assembly","procedure":"0x1800f2e00"},{"file":"docs/reverse-client/r1-engine-lua-manager-core-run1/09-LuaManagerInitialize-0x1800f2e00-procedure_callees.json","sha256":"1689c8a4b76528b3f29ef28bce86de40f2fb114998757723ed7634552b6a1855","operation":"procedure_callees","procedure":"0x1800f2e00"},{"file":"docs/reverse-client/r1-engine-lua-core-open-run1/04-LuaInitializeDirect-0x18025a030-procedure_pseudo_code.json","sha256":"267c66100e95a1df13805254a983fa596b9ea816b390dc79aadb21e93d3aebef","operation":"procedure_pseudo_code","procedure":"0x18025a030"},{"file":"docs/reverse-client/r1-engine-lua-core-open-run1/05-LuaInitializeDirect-0x18025a030-procedure_assembly.json","sha256":"1feda1cb91d2da4b2990e9508ff5b710346c0a9ae46ad7b3fbb6afc2c9dd1040","operation":"procedure_assembly","procedure":"0x18025a030"},{"file":"docs/reverse-client/r1-engine-lua-core-open-run1/06-LuaInitializeDirect-0x18025a030-procedure_callees.json","sha256":"74005956888c03702267d87ce558201963a7ca16695ad61328d351d17216a8f5","operation":"procedure_callees","procedure":"0x18025a030"}];assert.equal(canonical(plan.reused_existing_bodies),canonical(reused));
      for(const pin of reused){assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);const cached=payload(JSON.parse(fs.readFileSync(path.join(root,pin.file),'utf8')));verifyTarget(cached.subject);assert.equal(cached.operation,pin.operation);assert.equal(cached.parameters.procedure,pin.procedure);assert.ok(cached.normalized_result.length>0);}
      const initializeAssembly=payload(JSON.parse(fs.readFileSync(path.join(root,reused[1].file),'utf8')));assert.ok(initializeAssembly.normalized_result.split(/\r?\n/).includes('0x1800f2e28: CALL qword ptr [0x1804218f8]'));
      const support=[{"file":"docs/reverse-client/r1-lua-manager-bindings/inspection.json","sha256":"841c72e555cd23ba71bb3d7497bec39c26c59a25f1b9fcc72083f6692c903dcd","source_kind":"Exact disk bindings for already retrieved GetState and Initialize,not new25A030 body"},{"file":".tooling/lua-registry-writer-boundary-run1/result.json","sha256":"cdeacb4aa0125cf93c21895b5531d097618427aa591040e3bce363b4434248a5","source_kind":"No containing.pdata entry for008004 after168 metadata bytes; not guessedfunction absence"},{"file":".tooling/lua-publication-leaves-run1/result.json","sha256":"7e3392d572c230159ae31a743016358e471c546d49fe0d1ca81002db02540954","source_kind":"Preservedfailedwriter alignment plus two successful boundedleaf ranges;neverrepeatfirsttwo"}];assert.equal(canonical(plan.supporting_cached_evidence),canonical(support));for(const pin of support)assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);
      const pin=plan.prior_engine_query;
      assert.equal(pin.file,'docs/reverse-client/r1-engine-lua-publication-run1/summary.json');assert.equal(pin.sha256,'9733bc9fc23944c0b1282f75f5b275886e84f5361fca82cd1a75022f04cc0fd9');assert.equal(pin.calls,21);
      assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);const previous=JSON.parse(fs.readFileSync(path.join(root,pin.file),'utf8'));
      assert.equal(previous.status,'passed');assert.equal(previous.completed_plan_calls,21);assert.equal(previous.tool_calls,21);
      for(const key of ['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(previous[key],true);
      assert.equal(previous.forced_termination,false);assert.equal(previous.child_exit_code,0);
      for(const key of ['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(previous.owned_memory_profile[key],true);
      assert.equal(previous.owned_memory_profile.sampler_exit_code,0);assert.equal(previous.owned_memory_profile.failures.length,0);
      for(const key of ['sha256_before','sha256_after'])assert.equal(previous.target[key],targetHash);
      assert.equal(previous.memory.launch_threshold_gib,5);assert.equal(previous.memory.stop_threshold_gib,2);assert.equal(previous.memory.heap_limit,'384M');assert.equal(previous.memory.stop_triggered,false);
      const release=plan.prior_engine_release;assert.equal(release.file,'docs/reverse-client/r1-engine-lua-publication-run1/owned-memory-summary.json');assert.equal(release.sha256,'8477ad6e4feddd7c43058f6bcd13bad77a44e53e5b3f8245fa4d627f8ee33a4d');assert.equal(fileHash(path.join(root,release.file)),release.sha256);
      const released=JSON.parse(fs.readFileSync(path.join(root,release.file),'utf8'));for(const key of ['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(released[key],true);assert.equal(released.sampler_exit_code,0);assert.equal(released.failures.length,0);
      const current=plan.prior_current_release;assert.equal(current.file,'docs/reverse-client/r1-lua-manager-core-release.json');assert.equal(current.sha256,'80d97abe575eab33b1e0b9713071a63655865afcca04af54c9552b454b71cdb7');assert.equal(current.fresh_launch_checks_still_required,true);assert.equal(fileHash(path.join(root,current.file)),current.sha256);
      const priorCurrent=JSON.parse(fs.readFileSync(path.join(root,current.file),'utf8'));assert.equal(priorCurrent.current_matching_pids.length,0);assert.equal(priorCurrent.external_heavy_processes.length,0);assert.equal(priorCurrent.pending_operations,0);assert.equal(priorCurrent.job_empty_confirmed,true);assert.equal(priorCurrent.sampler_exit_confirmed,true);assert.equal(priorCurrent.historical_unknown_job_terminal_not_changed,true);
      const post=plan.post_luajit_release;assert.equal(post.file,'docs/reverse-client/r1-luajit204-postrelease.json');assert.equal(post.sha256,'dc1c1923b124c04420224d1876a94f8633b38f4ca798260aed2966d1be2f459b');assert.equal(fileHash(path.join(root,post.file)),post.sha256);const releasedNow=JSON.parse(fs.readFileSync(path.join(root,post.file),'utf8'));assert.equal(releasedNow.job_disposed,true);assert.equal(releasedNow.all_recorded_pids_absent,true);assert.equal(releasedNow.recorded_current_processes.length,0);assert.equal(releasedNow.original_job_empty_observed,false);assert.equal(releasedNow.safe_slot_released,true);
      const interrupted={"summary":"docs/reverse-client/r1-engine-lua-writer-log-run1/summary.json","summary_sha256":"257f2252883da364418211b7ba9f265589a3d996a6538f7e8b4e690f07dba0d9","release":"docs/reverse-client/r1-engine-lua-writer-log-run1/owned-memory-summary.json","release_sha256":"9b88c86e7f7f2acc8f2fb8ba4c0f23719789d5b21933bb9b4a899d03ca8b8e52","failure_response":"docs/reverse-client/r1-engine-lua-writer-log-run1/06-RegistryWriter-0x180008004-prev_address.json","failure_sha256":"6cd933655369b502c6ea7769d44ae8f7318f0bc5a83b0f2643d7f78a1c085502","historical_status":"failed","completed_plan_calls":5,"actual_tool_calls":8,"reason":"capability_unavailable/unsupported_provider;prev_address not declared byghidra,retryablefalse","not_retried":["inspect_native_instruction008004","resolve_containing_procedure008004","prev_address008004"],"pending_target":"0x180077050","clean_release_preserves_failure":true};assert.equal(canonical(plan.interrupted_query),canonical(interrupted));
      for(const [file,digest] of [[interrupted.summary,interrupted.summary_sha256],[interrupted.release,interrupted.release_sha256],[interrupted.failure_response,interrupted.failure_sha256]])assert.equal(fileHash(path.join(root,file)),digest);
      const failed=JSON.parse(fs.readFileSync(path.join(root,interrupted.summary),'utf8'));assert.equal(failed.status,'failed');assert.equal(failed.completed_plan_calls,5);assert.equal(failed.tool_calls,8);for(const key of ['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(failed[key],true);assert.equal(failed.forced_termination,false);assert.equal(failed.child_exit_code,0);for(const key of ['sha256_before','sha256_after'])assert.equal(failed.target[key],targetHash);
      const terminal=JSON.parse(fs.readFileSync(path.join(root,interrupted.release),'utf8'));for(const key of ['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(terminal[key],true);assert.equal(terminal.sampler_exit_code,0);assert.equal(terminal.failures.length,0);
      const rawFailure=JSON.parse(fs.readFileSync(path.join(root,interrupted.failure_response),'utf8'));assert.equal(rawFailure.result.isError,true);const failureText=rawFailure.result.content.filter(item=>item.type==='text');assert.equal(failureText.length,1);const capability=JSON.parse(failureText[0].text).error;assert.equal(capability.code,'capability_unavailable');assert.equal(capability.category,'unsupported_provider');assert.equal(capability.retryable,false);assert.equal(capability.details.provider_id,'ghidra');assert.equal(capability.details.operation,'prev_address');assert.equal(capability.details.reason,'operation is not declared by this provider');
      for(const name of ['07-BaseEngineLog-0x180077050-procedure_pseudo_code.json','08-BaseEngineLog-0x180077050-procedure_assembly.json','09-BaseEngineLog-0x180077050-procedure_callees.json'])assert.ok(!fs.existsSync(path.join(root,'docs/reverse-client/r1-engine-lua-writer-log-run1',name)),'Previously pending body now exists; stop duplicate query');
      const selection=[{"file":"docs/reverse-client/r1-engine-lua-writer-log-run1/04-RegistryWriter-0x180008004-inspect_native_instruction.json","sha256":"3bbbb3d503ce1fda53f4ecf56c22c2d83a328941419996c6676b9c94a6b21e90","kind":"Knownwriter008004 MOV[422888],EAX;sourcevalueandentryunknown"},{"file":".tooling/lua-publication-leaves-run1/registry-index-writer-neighborhood-dumpbin.txt","sha256":"931bf39c22ee156ab20358c8f2a356807fb18588dca7fbc50ac880a4417b8cf4","kind":"Retainedmisalignedrawspan:bytes008001..008003=0F45C1;candidatelocationonly,nosemanticsaccepted"},{"file":"docs/reverse-client/r1-engine-lua-writer-log-run1/tool-catalog-selected.json","sha256":"4440932e214fe9d8c278335ad05308c656d1d54e2fcc1902d845481e0d38bcb2","kind":"inspect_native_instruction contract distinguishes exactboundary/nonboundary/data/unknown"}];assert.equal(canonical(plan.selection_sources),canonical(selection));for(const pin of selection)assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);
      const selectedWriter=payload(JSON.parse(fs.readFileSync(path.join(root,selection[0].file),'utf8')));verifyTarget(selectedWriter.subject);assert.equal(selectedWriter.operation,'inspect_native_instruction');assert.equal(selectedWriter.parameters.address,'0x180008004');assert.equal(selectedWriter.normalized_result.status,'decoded');assert.equal(selectedWriter.normalized_result.bytes,'89057ea84100');assert.equal(selectedWriter.normalized_result.length,6);assert.equal(selectedWriter.normalized_result.procedure,null);
      const selectionCatalog=JSON.parse(fs.readFileSync(path.join(root,selection[2].file),'utf8'));assert.equal(selectionCatalog.tools.filter(tool=>tool.name==='inspect_native_instruction').length,1);
      const latest={"summary":"docs/reverse-client/r1-engine-lua-base-log-run1/summary.json","summary_sha256":"d013eb56bbe2750641cd65c9d0656d340612dbf2042708aef68b98f1cfe52d1c","release":"docs/reverse-client/r1-engine-lua-base-log-run1/owned-memory-summary.json","release_sha256":"ef01e4eca094b8a33e86e5f9f56fa8f507032cf9cad1090dd8ec0b97cc0617b2","completed_calls":8,"fresh_current_checks_required":true};assert.equal(canonical(plan.latest_completed_query),canonical(latest));for(const [file,hash]of[[latest.summary,latest.summary_sha256],[latest.release,latest.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);
      const latestSummary=JSON.parse(fs.readFileSync(path.join(root,latest.summary),'utf8'));assert.equal(latestSummary.status,'passed');assert.equal(latestSummary.completed_plan_calls,8);assert.equal(latestSummary.tool_calls,8);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(latestSummary[key],true);assert.equal(latestSummary.forced_termination,false);assert.equal(latestSummary.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(latestSummary.target[key],targetHash);
      const latestRelease=JSON.parse(fs.readFileSync(path.join(root,latest.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(latestRelease[key],true);assert.equal(latestRelease.sampler_exit_code,0);assert.equal(latestRelease.failures.length,0);
      const dataflowPins=[{"file":".tooling/lua-registry-writer-dataflow-run1/result.json","sha256":"04d27549475666162a4f202af67e8f5067b65383bfa2f5ef9fa54900616ca0e6"},{"file":"docs/reverse-client/r1-lua-registry-selection-run1/04-RegistrySelection-0x180008001-inspect_native_instruction.json","sha256":"398b5de56f2bdde56646d678ac53e29288e49d6c0c50e22c3f159f33ab5f030a"},{"file":"docs/reverse-client/r1-lua-registry-selection-run1/05-RegistrySelection-0x180008000-inspect_native_instruction.json","sha256":"108523ca501281acf0c96764273abf576633a36fe5c650bbe21fbeaf56220f22"},{"file":"docs/reverse-client/r1-engine-lua-initialize-dependency-run1/05-LuaInitializeDependency-0x180258e50-procedure_assembly.json","sha256":"b61d98ddad9968e13c7f6c9be8551a98eb4e03e8faccc7913ee461394896a8bf"}];assert.equal(canonical(plan.dataflow.source_pins),canonical(dataflowPins));for(const pin of dataflowPins)assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);
      const prefix=JSON.parse(fs.readFileSync(path.join(root,dataflowPins[0].file),'utf8'));assert.equal(prefix.length,64);assert.equal(prefix.rva_start,'0x7fc4');assert.equal(prefix.file_offset,0x73c4);const prefixBytes=Buffer.from(prefix.hex,'hex');assert.equal(prefixBytes.length,64);assert.equal(prefixBytes.subarray(44).toString('hex'),'833de597410002b8d8b9f0ffb9f0d8ffff0f45c1');assert.equal(0x7ff7+prefixBytes.readInt32LE(46),0x4217dc);assert.equal(prefixBytes.readInt32LE(52),-1001000);assert.equal(prefixBytes.readInt32LE(57),-10000);for(const phase of ['hashes_before','hashes_after']){assert.equal(prefix[phase].length,2);for(const item of prefix[phase])assert.equal(item.sha256,targetHash);}assert.equal(prefix.original_loaded,false);assert.equal(prefix.backend_started,false);
      const cmov=payload(JSON.parse(fs.readFileSync(path.join(root,dataflowPins[1].file),'utf8')));verifyTarget(cmov.subject);assert.equal(cmov.operation,'inspect_native_instruction');assert.equal(cmov.parameters.address,'0x180008001');assert.equal(cmov.normalized_result.status,'decoded');assert.equal(cmov.normalized_result.bytes,'0f45c1');assert.equal(cmov.normalized_result.procedure,null);
      const interior=payload(JSON.parse(fs.readFileSync(path.join(root,dataflowPins[2].file),'utf8')));verifyTarget(interior.subject);assert.equal(interior.parameters.address,'0x180008000');assert.equal(interior.normalized_result.status,'not-instruction-boundary');
      const binding=payload(JSON.parse(fs.readFileSync(path.join(root,dataflowPins[3].file),'utf8')));verifyTarget(binding.subject);assert.equal(binding.operation,'procedure_assembly');assert.equal(binding.parameters.procedure,'0x180258e50');assert.ok(binding.normalized_result.includes('0x180258eec: MOV dword ptr [0x1804217dc], 0x2'));assert.ok(binding.normalized_result.includes('0x180258f08: MOV dword ptr [0x1804217dc], EAX'));
      const selectionPrior={"summary":"docs/reverse-client/r1-lua-registry-selection-run1/summary.json","summary_sha256":"fae94a6ad11100e1f4d50d7b38bc1667a5edabaec7a1f1fa762a065953e8ec03","release":"docs/reverse-client/r1-lua-registry-selection-run1/owned-memory-summary.json","release_sha256":"3c2f7a787bcb69ac52c2f8bea5a4a6abe342ca74315a815fd3dd4f5bf1b63407","completed_calls":7,"fresh_current_checks_required":true};assert.equal(canonical(plan.latest_current_query),canonical(selectionPrior));for(const [file,hash]of[[selectionPrior.summary,selectionPrior.summary_sha256],[selectionPrior.release,selectionPrior.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);const selectionSummary=JSON.parse(fs.readFileSync(path.join(root,selectionPrior.summary),'utf8'));assert.equal(selectionSummary.status,'passed');assert.equal(selectionSummary.completed_plan_calls,7);assert.equal(selectionSummary.tool_calls,7);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(selectionSummary[key],true);assert.equal(selectionSummary.forced_termination,false);assert.equal(selectionSummary.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(selectionSummary.target[key],targetHash);const selectionRelease=JSON.parse(fs.readFileSync(path.join(root,selectionPrior.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(selectionRelease[key],true);assert.equal(selectionRelease.sampler_exit_code,0);assert.equal(selectionRelease.failures.length,0);
      const glueSources=[{"file":"docs/reverse-client/r1-engine-lua-publication-run1/10-LuaPublicationDirect-0x1802568d0-procedure_pseudo_code.json","sha256":"424cee3ce91716d8381950684e194d576fcee10ea372ad2b9d9421276e319de9","operation":"procedure_pseudo_code","procedure":"0x1802568d0"},{"file":"docs/reverse-client/r1-engine-lua-publication-run1/11-LuaPublicationDirect-0x1802568d0-procedure_assembly.json","sha256":"6d146c88e86f64a2b49424906c8d741d402c029abfd88ee7070fbf6fed79d060","operation":"procedure_assembly","procedure":"0x1802568d0"},{"file":"docs/reverse-client/r1-engine-lua-publication-run1/12-LuaPublicationDirect-0x1802568d0-procedure_callees.json","sha256":"2bd41e5bc54f1d3366c2cccd87fc4ff218252bddff3e253d27afc06317d487de","operation":"procedure_callees","procedure":"0x1802568d0"}];assert.equal(canonical(plan.luaglue_copy_sources),canonical(glueSources));
      for(const pin of glueSources){assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);const body=payload(JSON.parse(fs.readFileSync(path.join(root,pin.file),'utf8')));verifyTarget(body.subject);assert.equal(body.operation,pin.operation);assert.equal(body.parameters.procedure,'0x1802568d0');assert.ok(body.normalized_result.length>0);}
      const glueAssembly=payload(JSON.parse(fs.readFileSync(path.join(root,glueSources[1].file),'utf8')));assert.ok(glueAssembly.normalized_result.split(/\r?\n/).includes('0x180257595: CALL 0x180255f70'));
      const glueCallees=payload(JSON.parse(fs.readFileSync(path.join(root,glueSources[2].file),'utf8')));assert.ok(glueCallees.normalized_result.includes('0x180255f70'));
      const predecessor={"summary":"docs/reverse-client/r1-lua-registry-selection-run2/summary.json","summary_sha256":"a93b11f6d9ec8c6ee95d7afc24fe877918eba04679a362e6c74f72e12283e0ed","release":"docs/reverse-client/r1-lua-registry-selection-run2/owned-memory-summary.json","release_sha256":"781a2626886c89c58bd752de149ead7625bb9031155ca4690ff7b968ae4b2ccc","completed_calls":8,"fresh_current_checks_required":true};assert.equal(canonical(plan.current_predecessor),canonical(predecessor));for(const [file,hash]of[[predecessor.summary,predecessor.summary_sha256],[predecessor.release,predecessor.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);
      const ps=JSON.parse(fs.readFileSync(path.join(root,predecessor.summary),'utf8'));assert.equal(ps.status,'passed');assert.equal(ps.completed_plan_calls,8);assert.equal(ps.tool_calls,8);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(ps[key],true);assert.equal(ps.forced_termination,false);assert.equal(ps.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(ps.target[key],targetHash);
      const pr=JSON.parse(fs.readFileSync(path.join(root,predecessor.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(pr[key],true);assert.equal(pr.sampler_exit_code,0);assert.equal(pr.failures.length,0);
      const appendSources=[{"file":"docs/reverse-client/r1-engine-luaglue-copy-run1/04-LuaGlueRecordCopy-0x180255f70-procedure_pseudo_code.json","sha256":"2b2eab053dacae8462d954cc47f3b186d02dc13b1e29dba095917acfa93af450","operation":"procedure_pseudo_code","procedure":"0x180255f70"},{"file":"docs/reverse-client/r1-engine-luaglue-copy-run1/05-LuaGlueRecordCopy-0x180255f70-procedure_assembly.json","sha256":"1473782cb22c88f8e85bd6a3717355af21c05cf5ccefab2e88955bff8fe7aa17","operation":"procedure_assembly","procedure":"0x180255f70"},{"file":"docs/reverse-client/r1-engine-luaglue-copy-run1/06-LuaGlueRecordCopy-0x180255f70-procedure_callees.json","sha256":"e42b9fbba371fe6d0548d374a0ac0d2e3702b3ac097876f384c0e5d9837513e8","operation":"procedure_callees","procedure":"0x180255f70"}];assert.equal(canonical(plan.luaglue_append_sources),canonical(appendSources));for(const pin of appendSources){assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);const body=payload(JSON.parse(fs.readFileSync(path.join(root,pin.file),'utf8')));verifyTarget(body.subject);assert.equal(body.operation,pin.operation);assert.equal(body.parameters.procedure,'0x180255f70');assert.ok(body.normalized_result.length>0);}
      const appendAssembly=payload(JSON.parse(fs.readFileSync(path.join(root,appendSources[1].file),'utf8')));assert.ok(appendAssembly.normalized_result.split(/\r?\n/).includes('0x180256002: CALL 0x180255ce0'));const appendCallees=payload(JSON.parse(fs.readFileSync(path.join(root,appendSources[2].file),'utf8')));assert.ok(appendCallees.normalized_result.includes('0x180255ce0'));
      const adapterSources={"binder":{"file":"docs/reverse-client/r1-engine-lua-initialize-dependency-run1/05-LuaInitializeDependency-0x180258e50-procedure_assembly.json","sha256":"b61d98ddad9968e13c7f6c9be8551a98eb4e03e8faccc7913ee461394896a8bf"},"inspection":{"file":"docs/reverse-client/r1-luaglue-embedded-data-run1/inspection.json","sha256":"acfd23e2e33c962d629765b30e2345ea92ebda04c446d66f8724c4e2551ca35c"},"bytes":{"file":"docs/reverse-client/r1-luaglue-embedded-data-run1/getglobal-258da0-raw.bin","sha256":"970ddbbc3ac61108cbad509e75e7279f9e73b1d7df282dba2d5e347acabda13a"},"hex":"4c8bc2baeed8ffff48ff25118c1c00cc","bytes_count":16,"candidate_boundaries":["0x180258da0","0x180258da3","0x180258da8"],"candidate_semantics_unverified":true};assert.equal(canonical(plan.getglobal_adapter_sources),canonical(adapterSources));for(const pin of[adapterSources.binder,adapterSources.inspection,adapterSources.bytes])assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);
      const adapterBinder=payload(JSON.parse(fs.readFileSync(path.join(root,adapterSources.binder.file),'utf8')));verifyTarget(adapterBinder.subject);assert.equal(adapterBinder.operation,'procedure_assembly');assert.equal(adapterBinder.parameters.procedure,'0x180258e50');assert.ok(adapterBinder.normalized_result.includes('0x1802599df: LEA RAX, [0x180258da0]'));assert.ok(adapterBinder.normalized_result.includes('0x1802599e6: MOV qword ptr [0x180421880], RAX'));
      const adapterBytes=fs.readFileSync(path.join(root,adapterSources.bytes.file));assert.equal(adapterBytes.length,16);assert.equal(adapterBytes.toString('hex'),adapterSources.hex);const adapterRead=JSON.parse(fs.readFileSync(path.join(root,adapterSources.inspection.file),'utf8'));assert.equal(adapterRead.status,'passed-fixed-raw-data-extraction');for(const key of['sha256_before','sha256_after'])assert.equal(adapterRead[key],targetHash);assert.equal(adapterRead.original_loaded_or_executed,false);assert.equal(adapterRead.source_closed,true);assert.equal(adapterRead.source_stable,true);assert.equal(adapterRead.failures.length,0);
      const appendPrior={"summary":"docs/reverse-client/r1-engine-luaglue-copy-run1/summary.json","summary_sha256":"c3a0de7e6faba61d1ea8db0642dd7da17edea4ddb77ebd313ffba7d9088efea5","release":"docs/reverse-client/r1-engine-luaglue-copy-run1/owned-memory-summary.json","release_sha256":"f692cd845125ba0ead1ff74c291cc7be82446dc39387a3c39c042fc784d648e3","completed_calls":8,"fresh_current_checks_required":true};assert.equal(canonical(plan.append_predecessor),canonical(appendPrior));for(const [file,hash]of[[appendPrior.summary,appendPrior.summary_sha256],[appendPrior.release,appendPrior.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);const aps=JSON.parse(fs.readFileSync(path.join(root,appendPrior.summary),'utf8'));assert.equal(aps.status,'passed');assert.equal(aps.completed_plan_calls,8);assert.equal(aps.tool_calls,8);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(aps[key],true);assert.equal(aps.forced_termination,false);assert.equal(aps.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(aps.target[key],targetHash);const apr=JSON.parse(fs.readFileSync(path.join(root,appendPrior.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(apr[key],true);assert.equal(apr.sampler_exit_code,0);assert.equal(apr.failures.length,0);
      const gcSource={"file":"docs/reverse-client/r1-engine-lua-publication-run1/11-LuaPublicationDirect-0x1802568d0-procedure_assembly.json","sha256":"6d146c88e86f64a2b49424906c8d741d402c029abfd88ee7070fbf6fed79d060","operation":"procedure_assembly","procedure":"0x1802568d0","required_lines":["0x180257682: LEA RDX, [0x180256150]","0x18025768c: CALL qword ptr [0x1804219c8]","0x1802576cd: LEA RDX, [0x1802561a0]","0x1802576d7: CALL qword ptr [0x1804219c8]"],"selection_kind":"Cached registration addresses only; target body semantics not assumed"};assert.equal(canonical(plan.luaglue_gc_source),canonical(gcSource));assert.equal(fileHash(path.join(root,gcSource.file)),gcSource.sha256);const gcAssembly=payload(JSON.parse(fs.readFileSync(path.join(root,gcSource.file),'utf8')));verifyTarget(gcAssembly.subject);assert.equal(gcAssembly.operation,'procedure_assembly');assert.equal(gcAssembly.parameters.procedure,'0x1802568d0');for(const line of gcSource.required_lines)assert.ok(gcAssembly.normalized_result.split(/\r?\n/).includes(line),'Missing cached callback registration instruction');
      const gcPrior={"summary":"docs/reverse-client/r1-engine-luaglue-append-run1/summary.json","summary_sha256":"9e8c0c6d6da63a9ee72341e6f3df85cc7d6ab73c089f29ca47c87f0daea25494","release":"docs/reverse-client/r1-engine-luaglue-append-run1/owned-memory-summary.json","release_sha256":"36e8bad594125f8f9b0de2b313797ea87ea562a429c3e182c05cace04fec27c6","completed_calls":11,"fresh_current_checks_required":true};assert.equal(canonical(plan.gc_predecessor),canonical(gcPrior));for(const [file,hash]of[[gcPrior.summary,gcPrior.summary_sha256],[gcPrior.release,gcPrior.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);const gps=JSON.parse(fs.readFileSync(path.join(root,gcPrior.summary),'utf8'));assert.equal(gps.status,'passed');assert.equal(gps.completed_plan_calls,11);assert.equal(gps.tool_calls,11);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(gps[key],true);assert.equal(gps.forced_termination,false);assert.equal(gps.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(gps.target[key],targetHash);const gpr=JSON.parse(fs.readFileSync(path.join(root,gcPrior.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(gpr[key],true);assert.equal(gpr.sampler_exit_code,0);assert.equal(gpr.failures.length,0);
      const tailSource={"file":"docs/reverse-client/r1-engine-lua-publication-run1/11-LuaPublicationDirect-0x1802568d0-procedure_assembly.json","sha256":"6d146c88e86f64a2b49424906c8d741d402c029abfd88ee7070fbf6fed79d060","operation":"procedure_assembly","procedure":"0x1802568d0","required_lines":["0x180257707: TEST byte ptr [0x1804217d8], 0x1","0x18025770e: JZ 0x180257729","0x180257724: CALL 0x180257cb0","0x180257729: MOV BL, 0x1","0x18025772b: JMP 0x180257780","0x18025777e: XOR BL, BL","0x180257780: LEA R9, [0x1802577c0]","0x180257787: MOV EDX, 0x50","0x18025778c: LEA R8D, [RDX + -0x45]","0x180257790: LEA RCX, [RSP + 0x40]","0x180257795: CALL 0x1802dc3d4","0x18025779a: MOVZX EAX, BL"],"semantics":"Exact observed read/conditional call/cleanup callback address and direct dispatcher call. Writer, log and cleanup contracts remain unknown."};assert.equal(canonical(plan.luaglue_tail_source),canonical(tailSource));assert.equal(fileHash(path.join(root,tailSource.file)),tailSource.sha256);const tailAssembly=payload(JSON.parse(fs.readFileSync(path.join(root,tailSource.file),'utf8')));verifyTarget(tailAssembly.subject);assert.equal(tailAssembly.operation,'procedure_assembly');assert.equal(tailAssembly.parameters.procedure,'0x1802568d0');for(const line of tailSource.required_lines)assert.ok(tailAssembly.normalized_result.split(/\r?\n/).includes(line),'Missing cached LuaGlue tail instruction');
      const flagsPrior={"summary":"docs/reverse-client/r1-engine-luaglue-gc-run1/summary.json","summary_sha256":"c81bfda9fa9da8848fe6daaef3100cfa5165ab033489cbefc70951ad654655f4","release":"docs/reverse-client/r1-engine-luaglue-gc-run1/owned-memory-summary.json","release_sha256":"74c745a46c90bdb805425044e053ad9922fab564fabea64d922f21f71468a079","completed_calls":11,"fresh_current_checks_required":true};assert.equal(canonical(plan.flags_predecessor),canonical(flagsPrior));for(const [file,hash]of[[flagsPrior.summary,flagsPrior.summary_sha256],[flagsPrior.release,flagsPrior.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);const fps=JSON.parse(fs.readFileSync(path.join(root,flagsPrior.summary),'utf8'));assert.equal(fps.status,'passed');assert.equal(fps.completed_plan_calls,11);assert.equal(fps.tool_calls,11);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(fps[key],true);assert.equal(fps.forced_termination,false);assert.equal(fps.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(fps.target[key],targetHash);const fpr=JSON.parse(fs.readFileSync(path.join(root,flagsPrior.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(fpr[key],true);assert.equal(fpr.sampler_exit_code,0);assert.equal(fpr.failures.length,0);
      const registryHistory={"product":".tooling/r1-lua-registry-validation/result.json","product_sha256":"7f7ff93600baca70bb8ea60d4886ba7641092529e4c279273870592b09573d7d","launch":".tooling/r1-lua-registry-build-prepared/launch-result.json","launch_sha256":"15c680cc0adab553d5244bc5801ea31dd0d7386ac8f6f43b2ca728af4a4efca4","release":"docs/reverse-client/r1-lua-registry-closure-release.json","release_sha256":"7329f44e5d8b4a3bfc7284411886f4185d93de4ff45de1aff45f2f1a3989bb20","session":86392,"historical_job_empty_observed":false,"historical_status":"failed","later_current_absence_is_not_historical_empty":true,"fresh_launch_checks_required":true};assert.equal(canonical(plan.registry_build_history),canonical(registryHistory));for(const [file,hash]of[[registryHistory.product,registryHistory.product_sha256],[registryHistory.launch,registryHistory.launch_sha256],[registryHistory.release,registryHistory.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);const rbp=JSON.parse(fs.readFileSync(path.join(root,registryHistory.product),'utf8'));assert.equal(rbp.status,'passed-own-lua-registry-gc-prefix');assert.equal(rbp.completed_compiles,3);assert.equal(rbp.scenarios,8);assert.equal(rbp.failure,null);assert.equal(rbp.postcheck_failure,null);assert.equal(rbp.native_lease_cleanup_failures.length,0);const rbl=JSON.parse(fs.readFileSync(path.join(root,registryHistory.launch),'utf8'));assert.equal(rbl.status,'failed');assert.equal(rbl.worker_exited,true);assert.equal(rbl.worker_exit_code,0);assert.equal(rbl.job_empty_observed,false);assert.equal(rbl.job_disposed,true);assert.ok(rbl.cleanup_failure);assert.equal(rbl.build_result_status,rbp.status);const rbr=JSON.parse(fs.readFileSync(path.join(root,registryHistory.release),'utf8'));assert.equal(rbr.launch_sha256,registryHistory.launch_sha256);assert.equal(rbr.product_sha256,registryHistory.product_sha256);assert.equal(rbr.historical_launch_status,'failed');assert.equal(rbr.historical_job_empty_observed,false);assert.equal(rbr.historical_receipt_unchanged,true);assert.equal(rbr.historical_job_empty_rewritten,false);assert.equal(rbr.current_recorded_pids_absent,true);assert.equal(rbr.current_matches.length,0);assert.equal(rbr.current_conflicts.length,0);assert.equal(rbr.pending_operations,0);assert.equal(rbr.uncertain_current_operations,0);assert.equal(rbr.current_resource_slot_released,true);
      const writerRefs={"file":"docs/reverse-client/r1-engine-luaglue-flags-run1/04-LuaGlueFlag-0x1804217d8-xrefs.json","sha256":"e08a5fa7f9d987059b71951e460984bf9d2eb6c0010432950d3ebbadc5ef8e01","operation":"xrefs","address":"0x1804217d8","observed_addresses":["0x1800743c7","0x1800f0568","0x1800f0af8","0x1800f1088","0x1800f1b7b","0x1800f1c30","0x1800f1cce","0x1802560f9","0x180256203","0x180257707","0x180257b61","0x180257c6f","0x1802581aa"],"requested_addresses":["0x1800743c7","0x1800f0568","0x1800f0af8","0x1800f1088","0x1800f1b7b","0x1800f1c30","0x1800f1cce","0x1802560f9","0x180256203","0x180257b61","0x180257c6f","0x1802581aa"],"excluded_addresses":["0x180257707"],"reference_semantics":"Bare untyped source references only; exact instruction observations may reveal reads,writes,nonboundaries or data. No writer inference beforequery."};assert.equal(canonical(plan.luaglue_writer_reference_source),canonical(writerRefs));assert.equal(fileHash(path.join(root,writerRefs.file)),writerRefs.sha256);const observedRefs=payload(JSON.parse(fs.readFileSync(path.join(root,writerRefs.file),'utf8')));verifyTarget(observedRefs.subject);assert.equal(observedRefs.operation,'xrefs');assert.equal(observedRefs.parameters.address,'0x1804217d8');assert.equal(canonical(observedRefs.normalized_result),canonical(writerRefs.observed_addresses));assert.equal(canonical(writerRefs.observed_addresses.filter(address=>!writerRefs.excluded_addresses.includes(address))),canonical(writerRefs.requested_addresses));assert.equal(canonical(writerRefs.excluded_addresses),canonical(['0x180257707']));
      const writersPrior={"summary":"docs/reverse-client/r1-engine-luaglue-flags-run1/summary.json","summary_sha256":"c4dd4c5a1b7b00a1c7c8190978fab3bd47d24057d22e895b2f90fe8792e462c8","release":"docs/reverse-client/r1-engine-luaglue-flags-run1/owned-memory-summary.json","release_sha256":"d2f55dbd27c472314c1fd92f3b9682b602e822186fd6ced25bc5fd91501f9901","completed_calls":15,"fresh_current_checks_required":true};assert.equal(canonical(plan.writers_predecessor),canonical(writersPrior));for(const [file,hash]of[[writersPrior.summary,writersPrior.summary_sha256],[writersPrior.release,writersPrior.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);const wps=JSON.parse(fs.readFileSync(path.join(root,writersPrior.summary),'utf8'));assert.equal(wps.status,'passed');assert.equal(wps.completed_plan_calls,15);assert.equal(wps.tool_calls,15);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(wps[key],true);assert.equal(wps.forced_termination,false);assert.equal(wps.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(wps.target[key],targetHash);const wpr=JSON.parse(fs.readFileSync(path.join(root,writersPrior.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(wpr[key],true);assert.equal(wpr.sampler_exit_code,0);assert.equal(wpr.failures.length,0);
      const writerEntrySources={"instruction":{"file":"docs/reverse-client/r1-engine-luaglue-writers-run1/04-LuaGlueFlagReference-0x1800743c7-inspect_native_instruction.json","sha256":"4cc99b3b720e4d2b031ec57e27a9a835c1be553d5743efd5945826df64c361c1","address":"0x1800743c7","owner":"0x1800743c0","bytes":"89050bd43a00","length":6,"mnemonic":"MOV","raw_disassembly":"MOV dword ptr [0x1804217d8],EAX","write_target":"0x1804217d8"},"format_caller":{"file":"docs/reverse-client/r1-engine-luaglue-flags-run1/06-LuaGlueTailDirect-0x180257cb0-procedure_assembly.json","sha256":"372b644a3fe24bed56cf8343b02b6cddcb560cf997476a26df56a90479732144","procedure":"0x180257cb0","required_line":"0x180257cf7: CALL 0x180031280"},"unknown":"EAX producer,writer callers andformat behavior; sole directwriter onlyamong13observedrefs,not global completeness."};assert.equal(canonical(plan.flag_writer_entry_sources),canonical(writerEntrySources));for(const pin of[writerEntrySources.instruction,writerEntrySources.format_caller])assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);const wi=payload(JSON.parse(fs.readFileSync(path.join(root,writerEntrySources.instruction.file),'utf8')));verifyTarget(wi.subject);assert.equal(wi.operation,'inspect_native_instruction');assert.equal(wi.parameters.address,'0x1800743c7');const wn=wi.normalized_result;assert.equal(wn.status,'decoded');assert.equal(wn.address,'0x1800743c7');assert.equal(wn.procedure,'0x1800743c0');assert.equal(wn.bytes,'89050bd43a00');assert.equal(wn.length,6);assert.equal(wn.mnemonic,'MOV');assert.equal(wn.raw_disassembly,'MOV dword ptr [0x1804217d8],EAX');assert.ok(wn.references.some(ref=>ref.target_address==='0x1804217d8'&&ref.write===true&&ref.read===false));const fc=payload(JSON.parse(fs.readFileSync(path.join(root,writerEntrySources.format_caller.file),'utf8')));verifyTarget(fc.subject);assert.equal(fc.operation,'procedure_assembly');assert.equal(fc.parameters.procedure,'0x180257cb0');assert.ok(fc.normalized_result.split(/\r?\n/).includes(writerEntrySources.format_caller.required_line));
      const entryPrior={"summary":"docs/reverse-client/r1-engine-luaglue-writers-run1/summary.json","summary_sha256":"ecce498f9e3a30ff5eb1bf305a62a395e9a6a73f4f5f09e66cf87ec0dd9fa988","release":"docs/reverse-client/r1-engine-luaglue-writers-run1/owned-memory-summary.json","release_sha256":"efdaeb933e25b7d153b6bb043f76e2942b78ec8142ce80d245db7ad8751e7993","completed_calls":17,"fresh_current_checks_required":true};assert.equal(canonical(plan.writer_entry_predecessor),canonical(entryPrior));for(const [file,hash]of[[entryPrior.summary,entryPrior.summary_sha256],[entryPrior.release,entryPrior.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);const eps=JSON.parse(fs.readFileSync(path.join(root,entryPrior.summary),'utf8'));assert.equal(eps.status,'passed');assert.equal(eps.completed_plan_calls,17);assert.equal(eps.tool_calls,17);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(eps[key],true);assert.equal(eps.forced_termination,false);assert.equal(eps.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(eps.target[key],targetHash);const epr=JSON.parse(fs.readFileSync(path.join(root,entryPrior.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(epr[key],true);assert.equal(epr.sampler_exit_code,0);assert.equal(epr.failures.length,0);
      const scriptSource={"file":"docs/reverse-client/r1-engine-lua-publication-run1/14-LuaPublicationDirect-0x1800ef8b0-procedure_assembly.json","sha256":"d1d88dc7f7140c4c86d87710cc03dc726bd99f0ecf7ac770f2202118d2face6d","operation":"procedure_assembly","procedure":"0x1800ef8b0","required_lines":["0x1800ef913: CALL 0x1800efad0","0x1800ef924: CALL 0x1800efca0","0x1800ef935: CALL 0x1800efe80"],"targets":["0x1800efad0","0x1800efca0","0x1800efe80"]};assert.equal(canonical(plan.script_registration_source),canonical(scriptSource));assert.equal(fileHash(path.join(root,scriptSource.file)),scriptSource.sha256);const scriptCaller=payload(JSON.parse(fs.readFileSync(path.join(root,scriptSource.file),'utf8')));verifyTarget(scriptCaller.subject);assert.equal(scriptCaller.operation,'procedure_assembly');assert.equal(scriptCaller.parameters.procedure,'0x1800ef8b0');for(const line of scriptSource.required_lines)assert.ok(scriptCaller.normalized_result.split(/\r?\n/).includes(line));
      const scriptPrior={"summary":"docs/reverse-client/r1-engine-luaglue-flag-writer-run1/summary.json","summary_sha256":"08771af821013f9101c196f3cc5cdfef89ee3e3f7d739eff10ca2255e6bfefd5","release":"docs/reverse-client/r1-engine-luaglue-flag-writer-run1/owned-memory-summary.json","release_sha256":"12534b2340e202594d0c10f802e8646533f9a2db046bf28e08ebd28817e586ab","completed_calls":12,"fresh_current_checks_required":true};assert.equal(canonical(plan.script_registration_predecessor),canonical(scriptPrior));for(const[file,hash]of[[scriptPrior.summary,scriptPrior.summary_sha256],[scriptPrior.release,scriptPrior.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);const sps=JSON.parse(fs.readFileSync(path.join(root,scriptPrior.summary),'utf8'));assert.equal(sps.status,'passed');assert.equal(sps.completed_plan_calls,12);assert.equal(sps.tool_calls,12);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(sps[key],true);assert.equal(sps.forced_termination,false);assert.equal(sps.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(sps.target[key],targetHash);const spr=JSON.parse(fs.readFileSync(path.join(root,scriptPrior.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(spr[key],true);assert.equal(spr.sampler_exit_code,0);assert.equal(spr.failures.length,0);
      const bindingSources=[{"file":"docs/reverse-client/r1-engine-script-registration-run1/05-ScriptRegistration-0x1800efad0-procedure_assembly.json","sha256":"4ca89a9ac313878a06b3ec8f0beb8fdeedf9ea1bf4bf808bf30e505288e407e6","procedure":"0x1800efad0","required_line":"0x1800efc66: CALL 0x1800f0060","target":"0x1800f0060","owner_instruction":{"file":"docs/reverse-client/r1-engine-luaglue-writers-run1/05-LuaGlueFlagReference-0x1800f0568-inspect_native_instruction.json","sha256":"6addc03bd1d40a3ee808089ba8260018c793f5630b17f0c12d144b7d086617f2","address":"0x1800f0568","procedure":"0x1800f0060"}},{"file":"docs/reverse-client/r1-engine-script-registration-run1/08-ScriptRegistration-0x1800efca0-procedure_assembly.json","sha256":"b73bc5377f755b67ab186d76e9c50c00b222cc8f317310bc718aa2e4151b9a59","procedure":"0x1800efca0","required_line":"0x1800efe39: CALL 0x1800f05f0","target":"0x1800f05f0","owner_instruction":{"file":"docs/reverse-client/r1-engine-luaglue-writers-run1/06-LuaGlueFlagReference-0x1800f0af8-inspect_native_instruction.json","sha256":"b3f5dcca41c9c777293681f31a4ef2ce1fe1cdc27a07e5587bdc0d20dce8ab0e","address":"0x1800f0af8","procedure":"0x1800f05f0"}},{"file":"docs/reverse-client/r1-engine-script-registration-run1/11-ScriptRegistration-0x1800efe80-procedure_assembly.json","sha256":"a0c85724044895ca8436641b8b624aef981dc922598e306fc70a04571e429dc1","procedure":"0x1800efe80","required_line":"0x1800f0017: CALL 0x1800f0b80","target":"0x1800f0b80","owner_instruction":{"file":"docs/reverse-client/r1-engine-luaglue-writers-run1/07-LuaGlueFlagReference-0x1800f1088-inspect_native_instruction.json","sha256":"689c2e1caf2a3d67e8fd575bc6ea4751616fe7d066f2d30597c7fbf5519335f1","address":"0x1800f1088","procedure":"0x1800f0b80"}}];assert.equal(canonical(plan.script_binding_sources),canonical(bindingSources));for(const pin of bindingSources){assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);const caller=payload(JSON.parse(fs.readFileSync(path.join(root,pin.file),'utf8')));verifyTarget(caller.subject);assert.equal(caller.operation,'procedure_assembly');assert.equal(caller.parameters.procedure,pin.procedure);assert.ok(caller.normalized_result.split(/\r?\n/).includes(pin.required_line));const ip=pin.owner_instruction;assert.equal(fileHash(path.join(root,ip.file)),ip.sha256);const owner=payload(JSON.parse(fs.readFileSync(path.join(root,ip.file),'utf8')));verifyTarget(owner.subject);assert.equal(owner.operation,'inspect_native_instruction');assert.equal(owner.parameters.address,ip.address);assert.equal(owner.normalized_result.status,'decoded');assert.equal(owner.normalized_result.procedure,pin.target);}
      const bindingPrior={"summary":"docs/reverse-client/r1-engine-script-registration-run1/summary.json","summary_sha256":"fb4aab218a9f98b82ed39aa6593af170d3554bb35b8e96e409649285286042f1","release":"docs/reverse-client/r1-engine-script-registration-run1/owned-memory-summary.json","release_sha256":"ee6ba732640b3dc1e71bc33e9d0d4754f46ec1ac8e6e1bc1c54a55feccdf0a75","completed_calls":14,"fresh_current_checks_required":true};assert.equal(canonical(plan.script_binding_predecessor),canonical(bindingPrior));for(const[file,hash]of[[bindingPrior.summary,bindingPrior.summary_sha256],[bindingPrior.release,bindingPrior.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);const bps=JSON.parse(fs.readFileSync(path.join(root,bindingPrior.summary),'utf8'));assert.equal(bps.status,'passed');assert.equal(bps.completed_plan_calls,14);assert.equal(bps.tool_calls,14);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(bps[key],true);assert.equal(bps.forced_termination,false);assert.equal(bps.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(bps.target[key],targetHash);const bpr=JSON.parse(fs.readFileSync(path.join(root,bindingPrior.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(bpr[key],true);assert.equal(bpr.sampler_exit_code,0);assert.equal(bpr.failures.length,0);
      const lifecycleSources=[{"file":"docs/reverse-client/r1-engine-script-bindings-run1/05-ScriptBindingCore-0x1800f0060-procedure_assembly.json","sha256":"cbcb6967173daa1e773e79a71e2c9147c07d0d93022ab354d6a536c24ac22294","procedure":"0x1800f0060","required_lines":["0x1800f052d: MOV R8D, 0x1","0x1800f0533: LEA RDX, [0x1800f1b40]","0x1800f053a: MOV RCX, RBP","0x1800f053d: CALL qword ptr [0x1804219c8]"],"target":"0x1800f1b40","owner_instruction":{"file":"docs/reverse-client/r1-engine-luaglue-writers-run1/08-LuaGlueFlagReference-0x1800f1b7b-inspect_native_instruction.json","sha256":"24814ee346f85a09ef3bb5aed35ce2bf9162ef1eec2e55ca869323b89f598ce0","address":"0x1800f1b7b","procedure":"0x1800f1b40"}},{"file":"docs/reverse-client/r1-engine-script-bindings-run1/08-ScriptBindingCore-0x1800f05f0-procedure_assembly.json","sha256":"ea76659480d1f5e1539cd58f8a4824ad71c83134f2b03191064171ec4a9d0007","procedure":"0x1800f05f0","required_lines":["0x1800f0abd: MOV R8D, 0x1","0x1800f0ac3: LEA RDX, [0x1800f1bd0]","0x1800f0aca: MOV RCX, RBP","0x1800f0acd: CALL qword ptr [0x1804219c8]"],"target":"0x1800f1bd0","owner_instruction":{"file":"docs/reverse-client/r1-engine-luaglue-writers-run1/09-LuaGlueFlagReference-0x1800f1c30-inspect_native_instruction.json","sha256":"3d70d7a92a5c27801d3b6fbec50e32ace0eca168bddf435b2c9a8185dcaa97b1","address":"0x1800f1c30","procedure":"0x1800f1bd0"}},{"file":"docs/reverse-client/r1-engine-script-bindings-run1/11-ScriptBindingCore-0x1800f0b80-procedure_assembly.json","sha256":"98d52ee9984cc9c8234cf5a82810975e58e95dc7b4e875bb3b2a1eeab41cd894","procedure":"0x1800f0b80","required_lines":["0x1800f104d: MOV R8D, 0x1","0x1800f1053: LEA RDX, [0x1800f1c80]","0x1800f105a: MOV RCX, RBP","0x1800f105d: CALL qword ptr [0x1804219c8]"],"target":"0x1800f1c80","owner_instruction":{"file":"docs/reverse-client/r1-engine-luaglue-writers-run1/10-LuaGlueFlagReference-0x1800f1cce-inspect_native_instruction.json","sha256":"3724c50d670cf223ec169bf0c6c695931fcee9c6e0a39b0b632f3226fde77c87","address":"0x1800f1cce","procedure":"0x1800f1c80"}}];assert.equal(canonical(plan.script_lifecycle_sources),canonical(lifecycleSources));for(const pin of lifecycleSources){assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);const registration=payload(JSON.parse(fs.readFileSync(path.join(root,pin.file),'utf8')));verifyTarget(registration.subject);assert.equal(registration.operation,'procedure_assembly');assert.equal(registration.parameters.procedure,pin.procedure);for(const line of pin.required_lines)assert.ok(registration.normalized_result.split(/\r?\n/).includes(line));const ip=pin.owner_instruction;assert.equal(fileHash(path.join(root,ip.file)),ip.sha256);const owner=payload(JSON.parse(fs.readFileSync(path.join(root,ip.file),'utf8')));verifyTarget(owner.subject);assert.equal(owner.operation,'inspect_native_instruction');assert.equal(owner.parameters.address,ip.address);assert.equal(owner.normalized_result.status,'decoded');assert.equal(owner.normalized_result.procedure,pin.target);}
      const lifecycleSlot={"file":"docs/reverse-client/r1-script-node-slots/inspection.json","sha256":"0dc344fac99e5529fb8e6c3e32d06aec1d4813051af631b7d4a7a48f626f4e0d","slot_rvas":["0x340180","0x3401e0","0x3401b0"],"unique_target":"0x1800f13b0","descriptor_rva":"0x307e60","disk_descriptor_byte":0,"loaded_descriptor_observed":false,"function_boundary_verified":false};assert.equal(canonical(plan.script_lifecycle_slot),canonical(lifecycleSlot));assert.equal(fileHash(path.join(root,lifecycleSlot.file)),lifecycleSlot.sha256);const lsi=JSON.parse(fs.readFileSync(path.join(root,lifecycleSlot.file),'utf8'));assert.equal(lsi.status,'passed-disk-descriptor-and-node-slots-only');assert.equal(lsi.semantic_bytes,25);assert.equal(lsi.semantic_operations,4);assert.equal(lsi.bindings.length,3);assert.equal(canonical(lsi.bindings.map(x=>x.pointer_rva)),canonical(lifecycleSlot.slot_rvas));for(const binding of lsi.bindings){assert.equal(binding.target_va,lifecycleSlot.unique_target);assert.equal(binding.slot,'0x0');assert.equal(binding.function_boundary_verified,false);assert.equal(binding.loaded_binding_observed,false);}assert.equal(lsi.descriptor_observations.length,1);assert.equal(lsi.descriptor_observations[0].rva,lifecycleSlot.descriptor_rva);assert.equal(lsi.descriptor_observations[0].value_u8,0);assert.equal(lsi.descriptor_observations[0].loaded_value_observed,false);for(const key of['sha256_before','sha256_after'])assert.equal(lsi[key],targetHash);assert.equal(lsi.source_stat_stable,true);assert.equal(lsi.source_handle_closed,true);assert.equal(lsi.original_loaded_or_executed,false);assert.equal(lsi.failures.length,0);assert.equal(lsi.unsupported.length,0);
      const lifecyclePrior={"summary":"docs/reverse-client/r1-engine-script-bindings-run1/summary.json","summary_sha256":"d41dc8fe2ae86bcb2ff36a1b1d2a6c53be13c07b1b262822760745ab2c60f519","release":"docs/reverse-client/r1-engine-script-bindings-run1/owned-memory-summary.json","release_sha256":"6837ad4447550f74a95dd3946f1a3fb14ff3e649539216c6c1fa86321ca51b80","completed_calls":14,"fresh_current_checks_required":true};assert.equal(canonical(plan.script_lifecycle_predecessor),canonical(lifecyclePrior));for(const[file,hash]of[[lifecyclePrior.summary,lifecyclePrior.summary_sha256],[lifecyclePrior.release,lifecyclePrior.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);const lps=JSON.parse(fs.readFileSync(path.join(root,lifecyclePrior.summary),'utf8'));assert.equal(lps.status,'passed');assert.equal(lps.completed_plan_calls,14);assert.equal(lps.tool_calls,14);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(lps[key],true);assert.equal(lps.forced_termination,false);assert.equal(lps.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(lps.target[key],targetHash);const lpr=JSON.parse(fs.readFileSync(path.join(root,lifecyclePrior.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(lpr[key],true);assert.equal(lpr.sampler_exit_code,0);assert.equal(lpr.failures.length,0);
      const providerSource={"file":"docs/phase-1/boundary-audit/module-boundaries.json","sha256":"d1ec79916baeb45b3257f7c7c2ce356c262f804ae9b2577888a0d14de53e7ba7","relative_path":"x64/Grim Dawn.exe","import_module":"Engine.dll","symbol":"?RegisterProvider@LuaManager@GAME@@QEAAXPEAVLuaProvider@2@@Z","address_known":false};assert.equal(canonical(plan.provider_import_source),canonical(providerSource));assert.equal(fileHash(path.join(root,providerSource.file)),providerSource.sha256);
      const moduleInventory=JSON.parse(fs.readFileSync(path.join(root,providerSource.file),'utf8'));assert.ok(Array.isArray(moduleInventory));const clientEntries=moduleInventory.filter(x=>x.relativePath===providerSource.relative_path);assert.equal(clientEntries.length,1);const engineImports=clientEntries[0].importModules.filter(x=>x.module===providerSource.import_module);assert.equal(engineImports.length,1);assert.equal(engineImports[0].symbols.filter(x=>x===providerSource.symbol).length,1);assert.equal(providerSource.address_known,false);
      const providerPrior={"summary":"docs/reverse-client/r1-engine-script-lifecycle-run1/summary.json","summary_sha256":"9953e1694a4a7aa66edd31f105a4dd11a728b877c9fbeaf4ccdd3a8951101cf4","release":"docs/reverse-client/r1-engine-script-lifecycle-run1/owned-memory-summary.json","release_sha256":"3b31cb2c493217bf09d13561cf717ca8728a3e931b44b706d656a75bf5eaae25","completed_calls":17,"fresh_current_checks_required":true};assert.equal(canonical(plan.provider_predecessor),canonical(providerPrior));for(const[file,hash]of[[providerPrior.summary,providerPrior.summary_sha256],[providerPrior.release,providerPrior.release_sha256]])assert.equal(fileHash(path.join(root,file)),hash);
      const providerPreviousSummary=JSON.parse(fs.readFileSync(path.join(root,providerPrior.summary),'utf8'));assert.equal(providerPreviousSummary.status,'passed');assert.equal(providerPreviousSummary.completed_plan_calls,17);assert.equal(providerPreviousSummary.tool_calls,17);for(const key of['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(providerPreviousSummary[key],true);assert.equal(providerPreviousSummary.forced_termination,false);assert.equal(providerPreviousSummary.child_exit_code,0);for(const key of['sha256_before','sha256_after'])assert.equal(providerPreviousSummary.target[key],targetHash);
      const providerPreviousRelease=JSON.parse(fs.readFileSync(path.join(root,providerPrior.release),'utf8'));for(const key of['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(providerPreviousRelease[key],true);assert.equal(providerPreviousRelease.sampler_exit_code,0);assert.equal(providerPreviousRelease.failures.length,0);assert.equal(plan.provider_followup_limit,32);
      assert.equal(plan.batch_kind,'engine-lua-provider-registration-direct');
    }

    if (r1GameAssociatedRetry) {
      const override = plan.startup_override;
      assert.equal(override.mode, r1GameAnimationSetProduction ? '--r1-game-animation-set-production' : r1GameAnimationSetNames ? '--r1-game-animation-set-names' : r1GameWaypointAnimation ? '--r1-game-waypoint-animation' : r1GameAssociatedIdentityRepair ? '--r1-game-associated-identity-repair' : '--r1-game-associated-retry');
      assert.equal(override.environment_variable, 'REA_GHIDRA_STARTUP_TIMEOUT_MS'); assert.equal(override.value, '480000');
      assert.equal(override.previous_provider_startup_ms, 330000); assert.equal(override.provider_startup_ms, 480000);
      assert.equal(override.outer_request_ms, requestTimeout); assert.equal(override.maximum_new_attempts, 1);
      assert.equal(override.formal_execution_approval_required, true);
      assert.equal(override.wrapper.file, 'tools/static_analysis/rea_game_associated_retry_server.mjs');
      assert.equal(override.wrapper.sha256, 'd905d29ce91cf0934717b59ab2b1a69482c40b2380f01a94377aeb4a0c4501b8');
      assert.equal(fileHash(wrapper), override.wrapper.sha256, 'Dedicated retry wrapper changed');
      assert.equal(plan.planned_output, r1GameAnimationSetProduction ? 'docs/reverse-client/r1-controller-action-slots-run1' : r1GameAnimationSetNames ? 'docs/reverse-client/r1-movement-manager-name-search-run1' : r1GameWaypointAnimation ? 'docs/reverse-client/r1-game-waypoint-animation-run1' : r1GameAssociatedIdentityRepair ? 'docs/reverse-client/r1-game-associated-run3' : 'docs/reverse-client/r1-game-associated-run2');
      for (const pin of plan.unchanged_guard_pins) assert.equal(fileHash(path.join(root,pin.file)),pin.sha256,'Unchanged runtime guard changed');
      if (r1GameAssociatedIdentityRepair) {
        const repair = plan.identity_repair;
        assert.equal(repair.sampler_identity, 'game-associated-identity-repair');
        assert.equal(repair.controller_runner, 'C:/Users/zhuzi/Documents/Grim-Dynasty/tools/static_analysis/rea_native_registration.mjs');
        assert.equal(repair.controller_mode, r1GameAnimationSetProduction ? '--r1-game-animation-set-production' : r1GameAnimationSetNames ? '--r1-game-animation-set-names' : r1GameWaypointAnimation ? '--r1-game-waypoint-animation' : '--r1-game-associated-identity-repair');
        const effectivePins = [
          {file:'tools/static_analysis/rea_owned_memory_profile.mjs',sha256:'c6968067faa5b180556feaaf5e5aa62750e0ae0aa7bf031c5d1ceef990ae1f64'},
          {file:'tools/static_analysis/measure_game_associated_repair_memory.ps1',sha256:r1GameAnimationSetProduction ? 'dcc18bc620104666512b1683bb549b0e22cf7cf36a3f6718429510de6f1b8ab0' : r1GameAnimationSetNames ? 'dcc18bc620104666512b1683bb549b0e22cf7cf36a3f6718429510de6f1b8ab0' : r1GameWaypointAnimation ? '20777a75b720d7b57d23d1cd4e91b7f699e7758034d2b670186836d2f2ca22ff' : '4a886e20b862b70eb66f7ed1dcf17513a34cdd8f57d931791874d93ee00bf502'},
          {file:'.tooling/r1-game-associated-identity-repair-validation/result.json',sha256:'346578005bcf7ebc4a965de7a22104bf4bfd64801b67bfd552396746dd7b66aa'},
        ];
        assert.equal(canonical(repair.effective_guard_pins), canonical(effectivePins));
        for (const pin of [...effectivePins,repair.original_sampler,...repair.prior_failure_pins]) assert.equal(fileHash(path.join(root,pin.file)),pin.sha256,'Identity repair evidence changed');
        assert.equal(repair.original_sampler.file, 'tools/static_analysis/measure_job_process_memory.ps1');
        assert.equal(repair.original_sampler.sha256, '3916ee1f49eddb5f701cce1764dddeaf944badc61fe833ff469600ee58b6d6ca');
        const validated = JSON.parse(fs.readFileSync(path.join(root,effectivePins[2].file),'utf8'));
        assert.equal(validated.status,'passed-isolated-identity-repair-candidates');
        assert.equal(canonical(validated.tests.map(test=>[test.exit_code,test.result.scenarios,test.result.assertions])),canonical([[0,18,36],[0,12,38]]));
        for (const key of ['production_sampler_executed','live_process_queries','job_created','backend_started','original_read']) assert.equal(validated[key],false);
        const priorIdentity = JSON.parse(fs.readFileSync(path.join(root,repair.prior_failure_pins[0].file),'utf8'));
        assert.equal(repair.prior_failure_pins[0].file,'docs/reverse-client/r1-game-associated-run2/summary.json');
        assert.equal(repair.prior_failure_pins[0].sha256,'c2a444e1a66ba0a00b7d2234beb62d29627e20f38dc68f4ffa3e003b0eef2140');
        assert.equal(priorIdentity.status,'failed'); assert.equal(priorIdentity.tool_calls,0); assert.equal(priorIdentity.completed_plan_calls,0);
        assert.equal(priorIdentity.closed_session_verified,false); assert.equal(priorIdentity.owned_memory_profile.job_empty_confirmed,false);
        assert.equal(priorIdentity.process_exit_confirmed,true); assert.equal(priorIdentity.forced_termination,true);
        assert.equal(priorIdentity.owned_memory_profile.sampler_exit_confirmed,true); assert.equal(priorIdentity.owned_memory_profile.sampler_exit_code,1);
        assert.equal(repair.historical_monitor_metadata_is_not_effective_sampler_identity,true);
      }
      const saved = plan.prior_failed_startup;
      for (const pin of [saved.summary,saved.response,saved.cleanup_close,saved.cleanup_session]) assert.equal(fileHash(path.join(root,pin.file)),pin.sha256,'Prior failed startup evidence changed');
      const prior = JSON.parse(fs.readFileSync(path.join(root,saved.summary.file),'utf8'));
      const cleanup = {status:prior.status,completed_plan_calls:prior.completed_plan_calls,tool_calls:prior.tool_calls,
        close_acknowledged:prior.close_acknowledged,closed_session_verified:prior.closed_session_verified,process_exit_confirmed:prior.process_exit_confirmed,
        forced_termination:prior.forced_termination,child_exit_code:prior.child_exit_code,child_signal:prior.child_signal,
        memory_stop_triggered:prior.memory.stop_triggered,expected_heap_observed:prior.owned_memory_profile.expected_heap_observed,
        job_empty_confirmed:prior.owned_memory_profile.job_empty_confirmed,sampler_exit_confirmed:prior.owned_memory_profile.sampler_exit_confirmed,
        sampler_exit_code:prior.owned_memory_profile.sampler_exit_code,sampler_output_drained:prior.owned_memory_profile.sampler_output_drained,
        monitor_failures:prior.owned_memory_profile.failures,target_sha256_before:prior.target.sha256_before,target_sha256_after:prior.target.sha256_after};
      assert.equal(canonical(cleanup),canonical(saved.expected_cleanup),'Prior startup cleanup is incomplete');
      assert.equal(prior.status,'failed'); assert.equal(prior.completed_plan_calls,3); assert.equal(prior.tool_calls,6);
      assert.equal(canonical(prior.records.map(item=>[item.call_number,item.label,item.status])),canonical([
        [1,'01-initial-session','passed'],[2,'02-Game-open','passed'],[3,'03-Game-session','passed'],
        [4,'04-GameAssociated-0x180077d60-procedure_pseudo_code','failed'],[5,'cleanup-close','passed'],[6,'cleanup-session','passed']]));
      assert.equal(saved.successful_target_bodies,0);
      const failed = JSON.parse(fs.readFileSync(path.join(root,saved.response.file),'utf8'));
      assert.equal(failed.result.isError,true); assert.equal(failed.result.structuredContent,undefined);
      const error = JSON.parse(failed.result.content.find(item=>item.type==='text').text).error;
      assert.equal(error.code,'provider_timeout'); assert.equal(error.category,'timeout');
      assert.equal(error.details.operation,'procedure_pseudo_code'); assert.equal(error.details.timeout_ms,330000);
      assert.equal(error.details.diagnostics.failure_message,'Ghidra startup deadline elapsed');
      assert.equal(error.details.diagnostics.target_sha256,targetHash);
      assert.equal(payload(JSON.parse(fs.readFileSync(path.join(root,saved.cleanup_close.file),'utf8'))).result,null);
      verifySession(payload(JSON.parse(fs.readFileSync(path.join(root,saved.cleanup_session.file),'utf8'))),false);
      assert.equal(plan.time_limits.provider_startup_ms,330000); assert.equal(plan.time_limits.changed,false);
      plan.time_limits={...plan.time_limits,provider_startup_ms:480000,changed:true,
        scope:'This fixed retry only:480000ms provider startup, unchanged600000ms per-request outer deadline; not a whole-batch guarantee.'};
    }
    for (const pin of plan.contract_sources) assert.equal(fileHash(path.join(root,pin.file)),pin.sha256,'Saved tool contract changed');
    const prerequisites = [
      {file:'docs/reverse-client/r1-skill-moved-direct/summary.json',sha256:'596d4499deb89419964abfd35c52e9df60ee0f6785c4a4d40b9c930f4c773dab',calls:8,target:'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2'},
      {file:'docs/reverse-client/r1-world-position-and-level-initialization/summary.json',sha256:'dba34660d80fc2cf496d5025f2e160de946f07908ad9373aa49e5dfe5ea350f8',calls:11,target:'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153'},
    ];
    for (const pin of prerequisites) {
      assert.equal(fileHash(path.join(root,pin.file)),pin.sha256); const prior=JSON.parse(fs.readFileSync(path.join(root,pin.file),'utf8'));
      assert.equal(prior.status,'passed'); assert.equal(prior.completed_plan_calls,pin.calls);
      assert.equal(prior.closed_session_verified,true); assert.equal(prior.process_exit_confirmed,true);
      assert.equal(prior.owned_memory_profile.job_empty_confirmed,true); assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
      for (const key of ['sha256_before','sha256_after']) assert.equal(prior.target[key],pin.target);
    }
    plan.runner_mode_registered=true;
    plan.runtime_registration={query_plan_sha256:expectedPlanHash,profile:batch.runtimeProfile,metadata_keys:keys,cleanup_prerequisites:prerequisites,
      frozen_query_input:'plan.json',profile_scope_text_is_historical:true,query_scope_unchanged:true};
  }
  if (r1LevelObjectRecords || r1MoveWorldDistance || r1MoveProductionEngine || r1EngineMouseGetters || r1EngineReachedGoalDependencies || r1WorldPositionLevelInitialization || r1TerrainTypeMaterial || r1WaterInitialization || r1GroundResourcesWater || r1GroundFaceUnlink || r1GroundIntersectionRemaining || r1GroundIntersectionDirect || r1GroundRayTree || r1GroundFloorGeometry || r1GroundBlocksNodeDestroy || r1SpatialGroundDirect || r1EngineSpatialGround || r1EngineEntityPublicationCore || r1SameRegionMovement || r1PositionPublication || r1ActorRadius || r1CrowdPointCopy || r1CrowdPointReallocation || r1CrowdDirectAlgorithms || r1EngineCallbackGetters || r1CrowdMotionBranches || r1CrowdVectorGrowth || r1CrowdMotionHelpers || r1CrowdAgentGroupRegistration || r1CrowdVectorRegistration || r1CrowdAdapterRegistration || r1CrowdQueryProduction || r1CrowdMovementDirect || r1MovementFloorCore || r1NavigationConsumerDirect || r1StraightPathPortalWrapper || r1CorridorPortalHelper || r1NavigationCrossPolyCorridor || r1TriangulationAndTileConnection || r1MeshLinksAndTriangulationLeaf || r1MeshTileAndPredicates || r1NavPublishAndPolygonCore || r1NavPublicationAndContourLeaves || r1NavUpdateAndContours || r1TileProductionDirectStages || r1TileCacheAndDecompression || r1MapRegionAndTileProduct || r1MapDirectoryAndPathTiles || r1WorldLoadAndNavPublication || r1LevelLoadAndNavJobCandidates || r1LevelStreamAndNavJob || r1RegionBlobLoading || r1RegionLoadPublication || r1NavMeshProductionEntry || r1RegionLoadEntry || r1NavigationMeshQueryInitialization || r1NavigationImplementationConstructor || r1NavManagerConstructor || r1NavigationNearestDependencies || r1NavigationNearestHelpers || r1NavigationPolygonGeometry || r1NavigationSinglePoly || r1NavigationCore || r1EngineNavigationSearch || r1EngineCameraTarget || r1EnginePortalCamera || r0ToolBaselineTerminalRetry || r1EngineInput || r1EngineMouseCore || r1GameFixedMode || r1ExeMouseCallers) {
    const monitorFile = 'tools/static_analysis/ReaOwnedJob.cs';
    const monitorHash = (r1ReachedGoalDependencies || r1SkillMovedDirect || r1GameLevelPublicationTail || r1GameEntityPublicationGates || r1GameMovementMath || r1GameCallbackRemainingRetry || r1GameCallbackRemaining || r1GameCallbackCore || r1WorldPositionLevelInitialization || r1TerrainTypeMaterial || r1WaterInitialization || r1GroundResourcesWater || r1GroundFaceUnlink || r1GroundIntersectionRemaining || r1GroundIntersectionDirect || r1GroundRayTree || r1GroundFloorGeometry || r1GroundBlocksNodeDestroy || r1SpatialGroundDirect || r1EngineSpatialGround || r1EngineEntityPublicationCore || r1SameRegionMovement || r1PositionPublication || r1ActorRadius || r1CrowdPointCopy || r1CrowdPointReallocation || r1CrowdDirectAlgorithms || r1EngineCallbackGetters || r1CrowdMotionBranches || r1GameMovementInterface || r1GameEntityConstructors || r1CrowdVectorGrowthRetry) ? 'ad11defc7f3e5436c7f6e1aed19530a3376fe8f5b449101a5d641ea152241118' : '6b507ae86d9a3e05432d3bd13b0053ebe9fe17312dd3edba5939ef4a8c3d0e40';
    assert.equal(plan.monitor_revision?.file, monitorFile);
    assert.equal(plan.monitor_revision?.sha256, monitorHash);
    assert.equal(fileHash(path.join(root, monitorFile)), monitorHash, 'Terminal-state monitor changed; review before retry');
  }
  if (r1CrowdPointCopy || r1CrowdPointReallocation || r1CrowdDirectAlgorithms || r1EngineCallbackGetters || r1CrowdMotionBranches || r1CrowdVectorGrowth || r1CrowdMotionHelpers || r1CrowdAgentGroupRegistration || r1CrowdVectorRegistration || r1CrowdAdapterRegistration || r1CrowdQueryProduction || r1CrowdMovementDirect || r1MovementFloorCore || r1NavigationConsumerDirect || r1StraightPathPortalWrapper || r1CorridorPortalHelper || r1NavigationCrossPolyCorridor || r1TriangulationAndTileConnection || r1MeshLinksAndTriangulationLeaf || r1MeshTileAndPredicates || r1NavPublishAndPolygonCore || r1NavPublicationAndContourLeaves || r1NavUpdateAndContours || r1TileProductionDirectStages || r1TileCacheAndDecompression || r1MapRegionAndTileProduct || r1MapDirectoryAndPathTiles || r1WorldLoadAndNavPublication || r1LevelLoadAndNavJobCandidates || r1LevelStreamAndNavJob || r1RegionBlobLoading || r1RegionLoadPublication || r1NavMeshProductionEntry || r1RegionLoadEntry || r1NavigationMeshQueryInitialization || r1NavigationImplementationConstructor || r1NavManagerConstructor || r1NavigationNearestDependencies || r1NavigationNearestHelpers || r1NavigationPolygonGeometry || r1NavigationSinglePoly || r1NavigationCore || r1EngineNavigationSearch || r1EngineCameraTarget || r1EnginePortalCamera) {
    const priorFile = r1CrowdPointCopy ? 'docs/reverse-client/r1-crowd-point-reallocation/summary.json' : r1CrowdPointReallocation ? 'docs/reverse-client/r1-crowd-direct-algorithms/summary.json' : r1CrowdDirectAlgorithms ? 'docs/reverse-client/r1-engine-callback-getters/summary.json' : r1EngineCallbackGetters ? 'docs/reverse-client/r1-crowd-motion-branches/summary.json' : r1CrowdMotionBranches ? 'docs/reverse-client/r1-game-movement-interface/summary.json' : r1CrowdVectorGrowth ? 'docs/reverse-client/r1-game-entity-constructor-search/summary.json' : r1CrowdMotionHelpers ? 'docs/reverse-client/r1-crowd-agent-group-registration/summary.json' : r1CrowdAgentGroupRegistration ? 'docs/reverse-client/r1-crowd-vector-registration/summary.json' : r1CrowdVectorRegistration ? 'docs/reverse-client/r1-crowd-adapter-registration/summary.json' : r1CrowdAdapterRegistration ? 'docs/reverse-client/r1-crowd-query-production/summary.json' : r1CrowdQueryProduction ? 'docs/reverse-client/r1-crowd-movement-direct/summary.json' : r1CrowdMovementDirect ? 'docs/reverse-client/r1-movement-floor-core/summary.json' : r1MovementFloorCore ? 'docs/reverse-client/r1-navigation-consumer-direct/summary.json' : r1NavigationConsumerDirect ? 'docs/reverse-client/r1-straight-path-portal-wrapper/summary.json' : r1StraightPathPortalWrapper ? 'docs/reverse-client/r1-corridor-portal-helper/summary.json' : r1CorridorPortalHelper ? 'docs/reverse-client/r1-navigation-cross-poly-corridor/summary.json' : r1NavigationCrossPolyCorridor ? 'docs/reverse-client/r1-triangulation-and-tile-connection/summary.json' : r1TriangulationAndTileConnection ? 'docs/reverse-client/r1-mesh-links-and-triangulation-leaf/summary.json' : r1MeshLinksAndTriangulationLeaf ? 'docs/reverse-client/r1-mesh-tile-and-predicates/summary.json' : r1MeshTileAndPredicates ? 'docs/reverse-client/r1-nav-publish-and-polygon-core/summary.json' : r1NavPublishAndPolygonCore ? 'docs/reverse-client/r1-nav-publication-and-contour-leaves/summary.json' : r1NavPublicationAndContourLeaves ? 'docs/reverse-client/r1-nav-update-and-contours/summary.json' : r1NavUpdateAndContours ? 'docs/reverse-client/r1-tile-production-direct-stages/summary.json' : r1TileProductionDirectStages ? 'docs/reverse-client/r1-tile-cache-and-decompression/summary.json' : r1TileCacheAndDecompression ? 'docs/reverse-client/r1-map-region-and-tile-product/summary.json' : r1MapRegionAndTileProduct ? 'docs/reverse-client/r1-map-directory-and-path-tiles/summary.json' : r1MapDirectoryAndPathTiles ? 'docs/reverse-client/r1-world-load-and-nav-publication/summary.json' : r1WorldLoadAndNavPublication ? 'docs/reverse-client/r1-level-load-and-navjob-candidates/summary.json' : r1LevelLoadAndNavJobCandidates ? 'docs/reverse-client/r1-level-stream-and-navjob/summary.json' : r1LevelStreamAndNavJob ? 'docs/reverse-client/r1-region-blob-loading/summary.json' : r1RegionBlobLoading ? 'docs/reverse-client/r1-region-load-publication/summary.json' : r1RegionLoadPublication ? 'docs/reverse-client/r1-navmesh-production-entry/summary.json' : r1NavMeshProductionEntry ? 'docs/reverse-client/r1-region-load-entry/summary.json' : r1RegionLoadEntry ? 'docs/reverse-client/r1-navigation-mesh-query-initialization/summary.json' : r1NavigationMeshQueryInitialization ? 'docs/reverse-client/r1-navigation-implementation-constructor/summary.json' : r1NavigationImplementationConstructor ? 'docs/reverse-client/r1-navmanager-constructor/summary.json' : r1NavManagerConstructor ? 'docs/reverse-client/r1-navigation-nearest-dependencies/summary.json' : r1NavigationNearestDependencies ? 'docs/reverse-client/r1-navigation-nearest-helpers/summary.json' : r1NavigationNearestHelpers ? 'docs/reverse-client/r1-navigation-polygon-geometry/summary.json' : r1NavigationPolygonGeometry ? 'docs/reverse-client/r1-navigation-single-poly/summary.json' : r1NavigationSinglePoly ? 'docs/reverse-client/r1-navigation-core/summary.json' : r1NavigationCore ? 'docs/reverse-client/r1-engine-navigation-search/summary.json' : r1EngineNavigationSearch ? 'docs/reverse-client/r1-engine-camera-target/summary.json' : r1EngineCameraTarget ? 'docs/reverse-client/r1-game-camera-position/summary.json' : 'docs/reverse-client/r1-game-camera-angle/summary.json';
    const priorHash = r1CrowdPointCopy ? '5c393e22a3b32c1f7e8feaeedda9dbdf3020405174f7b94159801a8e640b43b1' : r1CrowdPointReallocation ? '6315714e95700918503909be1b52ffd0347138906672669b72334e87ee89053d' : r1CrowdDirectAlgorithms ? '66ca39735f5e5efb7dee030e27d4b2aff031e6fdaeca2b92711670305a6054d9' : r1EngineCallbackGetters ? 'd443fe60cf6e72100ad30e0d18e5d41ad758f61b47553f808feb3d71b3d5faf0' : r1CrowdMotionBranches ? '185bc461ac2ffef387a1d894582188cc18ff71d6523986b23d5325a0090df553' : r1CrowdVectorGrowth ? 'def1d0911951c67449794738404529609e9e626c23a381af7e8d731824e58a1f' : r1CrowdMotionHelpers ? 'eb627b52177b8e9b2b804f9383a9dfc6ad3e179aa8129be8b125fb319eae6f20' : r1CrowdAgentGroupRegistration ? 'bf5217e21a8a0f2a7a725c29733bac5d96443ad3ffb6a6ceca159a3b8379daa8' : r1CrowdVectorRegistration ? '060b18c52b67cca84ed869e14185c791b779040c4b131cc336929ec4f0d03ac0' : r1CrowdAdapterRegistration ? '540695b2a21d334be54b0fa2bd8c43e3289acea9fade89d945ed407ef0c1ac0c' : r1CrowdQueryProduction ? 'fe035fd2c4f8fff11229a973eb232b7d15215b53137eeffedcfde8436adbbc3c' : r1CrowdMovementDirect ? '6be729476badc1f58041a20adbad7af282274d1de8ae401897c0b6fdab324252' : r1MovementFloorCore ? '71d0e41ba73710e496a1db30f1b8a5b24d2485163a49fdca1cd15862baddccd2' : r1NavigationConsumerDirect ? '0c63144bedb12246ad775609d3777ef468ff787adfdaf7e263f3f83f8c7cf0c8' : r1StraightPathPortalWrapper ? '3f483f64e7c442d3b687d562cab93e66a1cecef3b806fa5fefe51a0145cb39ed' : r1CorridorPortalHelper ? '8b98e428f21029638b4fe5a4299f3c6ea3de8191d663d07817994ea432719e41' : r1NavigationCrossPolyCorridor ? 'fc592626c4706f8e2d3c5bf1ab1c224ea443861cf088820ebaf1087349235e4c' : r1TriangulationAndTileConnection ? '776c6ff8e92a7feca75a58a3f91ec3a338e764a1b59593b214945ef8a4f6c372' : r1MeshLinksAndTriangulationLeaf ? 'dae05f09bcf264a990f8a0e89114aaab92315c82e252cc91f77c164cf6c07450' : r1MeshTileAndPredicates ? '4f9b4952773a3370e2c6c73cd38f9c0d10a17449f617278e37f688dbef05fe94' : r1NavPublishAndPolygonCore ? '2e7dc89e3091ec78988c6c8d390dcc120da32cdfdf9a10ead1c8f7901ea8aecb' : r1NavPublicationAndContourLeaves ? 'afbfe90acd588d8c388e810c006a8233375cb955b6891bcafdbc89d5d6c0d9c5' : r1NavUpdateAndContours ? 'b5f964be6b097290b56582b098eb39e888638a3e1e9e969e55e3fd7322607200' : r1TileProductionDirectStages ? '480fedc413fe013696c49be63371a1097c11d41045adcabc13cce3a68583b83b' : r1TileCacheAndDecompression ? 'd866c7a9d3b58a0d6804dee7e70b1c63760f7516f8ccbad199cd1e2c77abd427' : r1MapRegionAndTileProduct ? '37c3fb40c06109f00c4e150dc9fedd931b65709a4478bd1165d802069e013b90' : r1MapDirectoryAndPathTiles ? '150b7ed628ff8b94dfb884800559b121513a64dc0d5b20e93bbc46b2593dd0e6' : r1WorldLoadAndNavPublication ? '68be5b90722bc635fc0886122a31e085f9ea220e9f4ef4978680d9062efe0cdf' : r1LevelLoadAndNavJobCandidates ? 'bfd9dc66c6f337d10e5f53202a112e5d50e9ce8291e4d455dce217f91a1f3a3a' : r1LevelStreamAndNavJob ? '36bfaaf37379ec842e252a76c3878f450a84a4d6f0c3db19d7f8a0a157e590c2' : r1RegionBlobLoading ? 'd632f4e582082055d689ffcd576a9d5c8d0f425ce03a8b56fe49eeaed640327f' : r1RegionLoadPublication ? '96b9556a792f719a5027109b80e4a0f5caa125b5da81bd38104f61118187a38b' : r1NavMeshProductionEntry ? 'b08080b59924a41167347e581540763c44e230a242dfafbbec1db168fbce40cb' : r1RegionLoadEntry ? '64a5f766300138d7145f3c8401a56fbe8f16946609375a1cc95928700c47b486' : r1NavigationMeshQueryInitialization ? '21bca14c454194ae6cf753cc539ef37771b2b07e5b52d9e06fe8689b74f98e81' : r1NavigationImplementationConstructor ? 'f3699df8eaa097692b4ce3fb872ac4e2bc0b119cc4285f125e2e74ffef78d9dc' : r1NavManagerConstructor ? '6cde3561346003056c716d757cb53259e6e987c3f86402cdfc5eaf5a0942924b' : r1NavigationNearestDependencies ? '981ab6c632e542f6a48449e4279d8781d87d888342214177d9c19108ba87a859' : r1NavigationNearestHelpers ? 'e3e3933bdeb13d693c64c39301f10a2353225122b45c8c49966131460b639b77' : r1NavigationPolygonGeometry ? '4e56505a90a6493e94452070770baeed213ba818b6b81ab5fa49b2213cb5eb0b' : r1NavigationSinglePoly ? '86ce524507630b0e63bef507b76de58d090a1308592c10eec0246705ede59922' : r1NavigationCore ? '1124d1801ed4fc1122baa71d093e1723a7ad252c3e2a276965e0ccdbdc1dbe7c' : r1EngineNavigationSearch ? '2585a18d21187d6ec1d8405bdd8e92c837d484aeba857edc551358a209ff17a6' : r1EngineCameraTarget ? '3f20d55c1a8dffdbee1f614ffa925e4082b87f921102b4dd3f88e087899ac89d' : 'f5b147265b613b1e28bf701f4930998a23fce86a9a69c5cef980304ff7f5790c';
    assert.equal(plan.prior_batch_cleanup?.summary_file, priorFile);
    assert.equal(plan.prior_batch_cleanup?.summary_sha256, priorHash);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash);
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed');
    assert.equal(prior.completed_plan_calls, r1CrowdPointCopy ? 8 : r1CrowdPointReallocation ? 14 : r1CrowdDirectAlgorithms ? 26 : r1EngineCallbackGetters ? 29 : r1CrowdMotionBranches ? 20 : r1CrowdVectorGrowth ? 6 : r1CrowdMotionHelpers ? 14 : r1CrowdAgentGroupRegistration ? 8 : r1CrowdVectorRegistration ? 17 : r1CrowdAdapterRegistration ? 14 : r1CrowdQueryProduction ? 20 : r1CrowdMovementDirect ? 17 : r1MovementFloorCore ? 14 : r1NavigationConsumerDirect ? 8 : r1StraightPathPortalWrapper ? 8 : r1CorridorPortalHelper ? 23 : r1NavigationCrossPolyCorridor ? 17 : r1TriangulationAndTileConnection ? 26 : r1MeshLinksAndTriangulationLeaf ? 20 : r1MeshTileAndPredicates ? 20 : r1NavPublishAndPolygonCore ? 17 : r1NavPublicationAndContourLeaves ? 17 : r1NavUpdateAndContours ? 29 : r1TileProductionDirectStages ? 14 : r1TileCacheAndDecompression ? 14 : r1MapRegionAndTileProduct ? 14 : r1MapDirectoryAndPathTiles ? 11 : r1WorldLoadAndNavPublication ? 17 : r1LevelLoadAndNavJobCandidates ? 14 : r1LevelStreamAndNavJob ? 14 : r1RegionBlobLoading ? 14 : r1RegionLoadPublication ? 14 : r1NavMeshProductionEntry ? 14 : r1RegionLoadEntry ? 11 : r1NavigationMeshQueryInitialization ? 8 : r1NavigationImplementationConstructor ? 8 : r1NavManagerConstructor ? 14 : r1NavigationNearestDependencies ? 11 : r1NavigationNearestHelpers ? 8 : r1NavigationPolygonGeometry ? 17 : (r1NavigationSinglePoly || r1EngineNavigationSearch) ? 14 : 8);
    assert.equal(prior.closed_session_verified, true);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true);
    assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
    for (const key of ['sha256_before','sha256_after']) assert.equal(prior.target[key], (r1CrowdMotionBranches || r1CrowdVectorGrowth) ? 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2' : (r1CrowdPointCopy || r1CrowdPointReallocation || r1CrowdDirectAlgorithms || r1EngineCallbackGetters || r1CrowdMotionBranches || r1CrowdVectorGrowth || r1CrowdMotionHelpers || r1CrowdAgentGroupRegistration || r1CrowdVectorRegistration || r1CrowdAdapterRegistration || r1CrowdQueryProduction || r1CrowdMovementDirect || r1MovementFloorCore || r1NavigationConsumerDirect || r1StraightPathPortalWrapper || r1CorridorPortalHelper || r1NavigationCrossPolyCorridor || r1TriangulationAndTileConnection || r1MeshLinksAndTriangulationLeaf || r1MeshTileAndPredicates || r1NavPublishAndPolygonCore || r1NavPublicationAndContourLeaves || r1NavUpdateAndContours || r1TileProductionDirectStages || r1TileCacheAndDecompression || r1MapRegionAndTileProduct || r1MapDirectoryAndPathTiles || r1WorldLoadAndNavPublication || r1LevelLoadAndNavJobCandidates || r1LevelStreamAndNavJob || r1RegionBlobLoading || r1RegionLoadPublication || r1NavMeshProductionEntry || r1RegionLoadEntry || r1NavigationMeshQueryInitialization || r1NavigationImplementationConstructor || r1NavManagerConstructor || r1NavigationNearestDependencies || r1NavigationNearestHelpers || r1NavigationPolygonGeometry || r1NavigationSinglePoly || r1NavigationCore || r1EngineNavigationSearch) ? '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153' : 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2');
  }
  if (r1EngineInput) {
    const priorFile = 'docs/reverse-client/r0-tool-baseline-terminal-retry/summary.json';
    const priorHash = '73ab36ba20f05200bf51426eae585ce2049058a1fe4e39e7819f2ad0e06dd877';
    assert.equal(plan.prior_r0?.summary_file, priorFile);
    assert.equal(plan.prior_r0?.summary_sha256, priorHash);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'R0 prerequisite evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed');
    assert.equal(prior.closed_session_verified, true);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true);
  }
  if (r1EngineMouseCore) {
    const priorFile = 'docs/reverse-client/r1-engine-input/summary.json';
    const priorHash = '0eaeb55e753fc53b9a1b8a330d1812367440864d7fc9e7e1607137e3bdd160ba';
    assert.equal(plan.prior_r1?.summary_file, priorFile);
    assert.equal(plan.prior_r1?.summary_sha256, priorHash);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'R1 prerequisite evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed');
    assert.equal(prior.closed_session_verified, true);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true);
  }
  if (r1GameFixedMode) {
    const priorFile = 'docs/reverse-client/r1-engine-mouse-core/summary.json';
    const priorHash = '4d26ffeb36f6c1bc0e8b8faf0a77c7f601ef08b5ce31a6a2aa2ca56c2449ba77';
    assert.equal(plan.prior_engine_mouse_core?.summary_file, priorFile);
    assert.equal(plan.prior_engine_mouse_core?.summary_sha256, priorHash);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'Engine mouse-core prerequisite evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed');
    assert.equal(prior.closed_session_verified, true);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true);
    assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
  }
  if (r1GameAssociated || r1MoveActionDispatchLeaves || r1DurationManagerDirect || r1DurationManagerConstructor || r1GameRunSpeedDirect || r1GameRunSpeed || r1GameStateLookupDirect || r1GameMainPlayerConnectionMethods || r1GameMainPlayerConnection || r1GamePlayerVideoDirect || r1GameMouseDirectDependencies || r1GameSkillReferenceDirect || r1GameStopStateDirect || r1GameReachedGoalDependencies || r1SkillMovedDirect || r1GameLevelPublicationTail || r1GameEntityPublicationGates || r1GameMovementMath || r1GameCallbackRemainingRetry || r1GameCallbackRemaining || r1GameCallbackCore || r1GameMovementInterface || r1GameEntityConstructors || r1GameEntityConstructorSearch || r1GameMouseLinks || r1GameMouseState || r1GameStateTransition || r1GameStateRegistrationSearch || r1GameAddState || r1PlayerRegisterStates || r1PlayerMouseHandlers) {
    const priorFile = 'docs/reverse-client/r1-game-mouse-calibration/summary.json';
    const priorHash = '1397b49afa2556c97b2c1ea5ab9807a92c1a1306cd98262d10e697a2ed4d571e';
    assert.equal(plan.prior_game_trial_observation?.summary_file, priorFile);
    assert.equal(plan.prior_game_trial_observation?.summary_sha256, priorHash);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'Prior Game trial observation changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed');
    assert.equal(prior.closed_session_verified, true);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true);
    assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
  }
  if (r1GameMouseState) {
    const priorFile = 'docs/reverse-client/r1-game-mouse-links/summary.json';
    const priorHash = '652f86962bee76353c9f1c2979ba5220fcd9c8d5ddecfe821a87a60680a4b93a';
    assert.equal(plan.prior_game_mouse_links?.summary_file, priorFile);
    assert.equal(plan.prior_game_mouse_links?.summary_sha256, priorHash);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'Game mouse-links prerequisite evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed');
    assert.equal(prior.completed_plan_calls, 9);
    assert.equal(prior.closed_session_verified, true);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true);
    assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
  }
  assert.equal(plan.calls.length, maxToolCalls);
  assert.equal(plan.max_tool_calls, maxToolCalls);
  assert.equal(plan.lifecycle_in_calls, true);
  assert.equal(new Set(plan.calls.map(call => call.label)).size, maxToolCalls);
  assert.equal(plan.calls.at(-1).label, finalSessionLabel);
  assert.equal(plan.calls.at(-1).tool, 'binary_session');
  assert.equal(plan.target.path, targetPath);
  assert.equal(plan.target.sha256, targetHash);
  assert.equal(plan.target.provider_id, 'ghidra');
  if (ownedMemoryMode) for (const source of plan.address_sources ?? []) assert.equal(fileHash(path.join(root, source.file)), source.sha256, 'Address source changed');
  assert.equal(canonical([...plan.allowed_tools].sort()), canonical([...allowed].sort()));
  const metadata = JSON.parse(fs.readFileSync(path.join(packageRoot, 'package.json'), 'utf8'));
  assert.equal(metadata.name, 'rea-agents');
  assert.equal(metadata.version, '6.5.0');
  const baseline = JSON.parse(fs.readFileSync(path.join(root, 'docs/condition-evaluator/rea-leaves/tool-catalog.json'), 'utf8')).result.tools;
  assert.ok(fs.statSync(wrapper).isFile(), 'Project wrapper missing');
  if (r1GameAddState || r1PlayerRegisterStates || r1PlayerMouseHandlers) {
    const priorFile = r1GameCameraPosition ? 'docs/reverse-client/r1-engine-portal-camera/summary.json' : r1GameCameraAngle ? 'docs/reverse-client/r1-game-camera-constructor/summary.json' : r1GameCameraConstructor ? 'docs/reverse-client/r1-portal-dependencies/summary.json' : r1PortalDependencies ? 'docs/reverse-client/r1-waypoint-queue-helpers/summary.json' : r1WaypointQueueHelpers ? 'docs/reverse-client/r1-waypoint-execution/summary.json' : r1WaypointExecution ? 'docs/reverse-client/r1-move-manager/summary.json' : r1MoveManager ? 'docs/reverse-client/r1-move-start-dependencies/summary.json' : r1MoveStartDependencies ? 'docs/reverse-client/r1-move-start/summary.json' : r1MoveStart ? 'docs/reverse-client/r1-action-support/summary.json' : r1ActionSupport ? 'docs/reverse-client/r1-action-executor/summary.json' : r1ActionExecutor ? 'docs/reverse-client/r1-path-local-dispatch/summary.json' : r1PathLocalDispatch ? 'docs/reverse-client/r1-movement-action-path/summary.json' : r1MovementActionPath ? 'docs/reverse-client/r1-player-move-dispatch/summary.json' : r1PlayerMoveDispatch ? 'docs/reverse-client/r1-player-move-entries/summary.json' : r1PlayerMoveEntries ? 'docs/reverse-client/r1-player-mouse-handlers-retry-1/summary.json' : r1PlayerMouseHandlers ? 'docs/reverse-client/r1-player-register-states/summary.json' : r1PlayerRegisterStates ? 'docs/reverse-client/r1-game-add-state/summary.json' : 'docs/reverse-client/r1-game-state-registration-search/summary.json';
    const priorHash = r1GameCameraPosition ? '50a2fb2e7d9f90e74ec6781c165a6bcaedcf15dd398b604ad0d00f9d2a9414a8' : r1GameCameraAngle ? '90d92f7cb93ed95a86c9b9f030f7d87df091a5c46aaa01aef47e40915279eebf' : r1GameCameraConstructor ? '6f53e49cc0ba780e9427c7fd1b70c6ccbdead8810a3ecfd6287779423fa277f5' : r1PortalDependencies ? '4a4d7d99ef010477f5df232da0b92e531de58603a9695c3943313199ea5eabcd' : r1WaypointQueueHelpers ? '73dddf25b2cf4dfc239f1baf3afde509899e80adea4caac0cb4bdfc04b2726db' : r1WaypointExecution ? 'cd779a6cb10077f458332cb2d804d14629b2c61666b2ceca93613bd135ec7b6c' : r1MoveManager ? '123ce9f5ac00996934eaa5316de665da8e735e20df7fecd0479ca5dd1e2389ca' : r1MoveStartDependencies ? '9cd4a5f209e25bae004cf8b455467f5f4d26bc4dc00e75a0f8412d67fe91ea63' : r1MoveStart ? '4bf6b2df45856b142657a0e4d54a7ea6db0dc71923e6c02f651f31a46711cf51' : r1ActionSupport ? '0c01d23dc6df5dab3b88c034ee89a6d07d1615748f8c30db49be3fb60bcd41f1' : r1ActionExecutor ? 'ac6e3862adbf88ca02202c65aa2a9cb25d665be531a2d5abc338ad10c938b541' : r1PathLocalDispatch ? 'd9bbdea2cfaaf78990fef3aa1728562838832703e180a9136b364a4d38458e93' : r1MovementActionPath ? '6ddab1690d46a3e654b644b79b9a70febf5da083739827365da275e4a372b261' : r1PlayerMoveDispatch ? '5a259f1ba07361eeb0e58c9323d9204383f49451209d8eb2203645e0c6404da7' : r1PlayerMoveEntries ? '75ad9502d2443099c8b78496dc5975ae159d3b7b0d32662713fc69d1067cc5b3' : r1PlayerMouseHandlers ? '2f0c963e116d68397822b1acb7e359f4214aec3f3049de4d135a937a6da2764e' : r1PlayerRegisterStates ? 'd10ea3d9dc183deaf812261ea8199564e8171edab5b2da9a6d69e5a7f4e9e03e' : 'c48317ca8695103ec0070807f51faca68afb7741f9534f68c55f2587223f4dbd';
    assert.equal(plan.prior_batch_cleanup?.summary_file, priorFile);
    assert.equal(plan.prior_batch_cleanup?.summary_sha256, priorHash);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash);
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed');
    assert.equal(prior.completed_plan_calls, r1GameCameraPosition ? 11 : r1GameCameraAngle ? 8 : r1GameCameraConstructor ? 11 : r1PortalDependencies ? 11 : r1WaypointQueueHelpers ? 17 : r1WaypointExecution ? 17 : r1MoveManager ? 18 : r1MoveStartDependencies ? 8 : r1MoveStart ? 17 : r1ActionSupport ? 14 : r1ActionExecutor ? 12 : r1PathLocalDispatch ? 18 : r1MovementActionPath ? 11 : (r1PlayerMoveDispatch || r1PlayerMoveEntries) ? 14 : (r1PlayerRegisterStates || r1PlayerMouseHandlers) ? 9 : 6);
    assert.equal(prior.closed_session_verified, true);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true);
    assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
  }
  if (r1GameCallbackRemaining) {
    const priorFile = 'docs/reverse-client/r1-game-callback-core-retry-1/summary.json', priorHash = 'd1eb4e87f69c429fbe14d65bfdb409dd6b3bda1dea1858b0dc8fedf11cdfb1a4';
    assert.equal(plan.prior_batch_cleanup?.summary_file, priorFile); assert.equal(plan.prior_batch_cleanup?.summary_sha256, priorHash);
    assert.equal(plan.prior_batch_cleanup.required_status, 'failed'); assert.equal(plan.prior_batch_cleanup.completed_plan_calls, 9);
    assert.equal(plan.prior_batch_cleanup.tool_calls, 12); assert.equal(plan.prior_batch_cleanup.observed, true);
    for (const key of ['closed_session_verified','process_exit_confirmed','job_empty_confirmed','sampler_exit_confirmed']) assert.equal(plan.prior_batch_cleanup[key], true);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'Prior partial Game batch evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'failed'); assert.equal(prior.completed_plan_calls, 9); assert.equal(prior.tool_calls, 12);
    assert.equal(prior.closed_session_verified, true); assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.child_exit_code, 0); assert.equal(prior.owned_memory_profile.job_empty_confirmed, true);
    assert.equal(prior.owned_memory_profile.sampler_exit_confirmed, true);
    assert.equal(prior.records.length, 12);
    assert.ok(prior.records.slice(0,9).every(item => item.status === 'passed'));
    assert.equal(prior.records[9].status, 'failed'); assert.equal(prior.records[9].call_number, 10);
    assert.equal(prior.records[9].label, '10-GameCallbackCore-0x1805d9f8c-procedure_pseudo_code');
    assert.equal(prior.records[10].label, 'cleanup-close'); assert.equal(prior.records[11].label, 'cleanup-session');
    assert.ok(prior.records.slice(10).every(item => item.status === 'passed'));
    for (const key of ['sha256_before','sha256_after']) assert.equal(prior.target[key], targetHash);
    const errorFile = 'docs/reverse-client/r1-game-callback-core-retry-1/10-GameCallbackCore-0x1805d9f8c-procedure_pseudo_code.json', errorHash = 'd85b13f72ea7df0430bbc1089a8b54ae0b7a84caf3b9a6f87d1622a4588736e1';
    assert.equal(plan.preceding_selector_failure.file, errorFile); assert.equal(plan.preceding_selector_failure.sha256, errorHash);
    assert.equal(plan.preceding_selector_failure.selector, '0x1805d9f8c'); assert.equal(plan.preceding_selector_failure.call_number, 10);
    assert.equal(plan.preceding_selector_failure.mcp_error, 'invalid_request'); assert.equal(plan.preceding_selector_failure.resolution_unknown, true);
    assert.equal(plan.preceding_selector_failure.automatically_retried, false); assert.equal(plan.preceding_selector_failure.normal_cleanup, true);
    assert.equal(fileHash(path.join(root, errorFile)), errorHash, 'Preceding selector error changed');
    const rawError = JSON.parse(fs.readFileSync(path.join(root, errorFile), 'utf8'));
    assert.equal(rawError.result.isError, true);
    const messages = rawError.result.content.filter(item => item.type === 'text'); assert.equal(messages.length, 1);
    const error = JSON.parse(messages[0].text).error;
    assert.equal(error.code, 'invalid_request'); assert.equal(error.category, 'invalid_input');
    assert.equal(error.details.operation, 'procedure_pseudo_code'); assert.equal(error.details.issues.length, 1);
    assert.equal(error.details.issues[0].reason, 'invalid_value');
    assert.equal(error.details.issues[0].message, 'File offset 0x1805d9f8c has no loaded source mapping for the admitted target file; Ghidra image base: 0x180000000. Inspect mappings with rea inspect-native-load-image <path> --provider ghidra --json or MCP inspect_native_load_image.');
    const reused = [
      [
        "docs/reverse-client/r1-game-callback-core-retry-1/04-GameCallbackCore-0x180019d20-procedure_pseudo_code.json",
        "7e6def90d8d958d0f9a443a68f7361ba1ee824f3e694c5f4cf1afe6d7917e165",
        "procedure_pseudo_code"
      ],
      [
        "docs/reverse-client/r1-game-callback-core-retry-1/05-GameCallbackCore-0x180019d20-procedure_assembly.json",
        "436ace29503473d02de31739c5d46488c94008e652c7df6e6eb8850b625e1003",
        "procedure_assembly"
      ],
      [
        "docs/reverse-client/r1-game-callback-core-retry-1/06-GameCallbackCore-0x180019d20-procedure_callees.json",
        "1775799672ddc3ed21e53d0482c784401a97ec49f8e23b9f1863f68d816f2f92",
        "procedure_callees"
      ],
      [
        "docs/reverse-client/r1-game-callback-core-retry-1/07-GameCallbackCore-0x180016d10-procedure_pseudo_code.json",
        "518ecd547b96fb71976a6b4bd88ddb502b9f26bba5138f700a8c28e62d66f185",
        "procedure_pseudo_code"
      ],
      [
        "docs/reverse-client/r1-game-callback-core-retry-1/08-GameCallbackCore-0x180016d10-procedure_assembly.json",
        "64be4eea501132759edeb5eadc47e46902d27f6269031d86a783412841895d09",
        "procedure_assembly"
      ],
      [
        "docs/reverse-client/r1-game-callback-core-retry-1/09-GameCallbackCore-0x180016d10-procedure_callees.json",
        "cf7cf6a8914ba8dd06c2b31bab7cd8e175266b639d8742c9e62026bcf0876f6f",
        "procedure_callees"
      ]
    ];
    assert.equal(plan.completed_work_reused.length, reused.length);
    for (let index = 0; index < reused.length; index++) {
      const [file,fingerprint,tool] = reused[index], item = plan.completed_work_reused[index];
      assert.equal(item.file, file); assert.equal(item.sha256, fingerprint); assert.equal(item.tool, tool); assert.equal(item.requery, false);
      assert.equal(fileHash(path.join(root, file)), fingerprint, 'Completed Game getter evidence changed');
      assert.equal(prior.records[index+3].tool, tool);
      assert.equal(path.basename(file), prior.records[index+3].label + '.json');
    }
    const oldRoot = path.join(root, 'docs/reverse-client/r1-game-callback-core-retry-1');
    for (const item of plan.calls.slice(3,12)) {
      const old = item.args.procedure === '0x18005b730' ? 13 : item.args.procedure === '0x180591480' ? 16 : 19;
      const offset = ['procedure_pseudo_code','procedure_assembly','procedure_callees'].indexOf(item.tool);
      const label = String(old + offset).padStart(2,'0') + '-GameCallbackCore-' + item.args.procedure + '-' + item.tool;
      assert.ok(!fs.existsSync(path.join(oldRoot, label + '.json')), 'Previously unissued result now exists; review reuse');
    }
  }
  if (r1GameCallbackCore) {
    const priorFile = 'docs/reverse-client/r1-crowd-point-copy/summary.json';
    const priorHash = '2157fd2bc737adf72402a85383eb0863ec220743de87c806b3efde47ccd5172f';
    assert.equal(plan.prior_batch_cleanup?.summary_file, priorFile);
    assert.equal(plan.prior_batch_cleanup?.summary_sha256, priorHash);
    assert.equal(plan.prior_batch_cleanup?.observed, true);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'Prior Engine point-copy evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed'); assert.equal(prior.completed_plan_calls, 8);
    assert.equal(prior.closed_session_verified, true); assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true); assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
    for (const key of ['sha256_before','sha256_after']) assert.equal(prior.target[key], '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1GameMovementInterface) {
    const priorFile = 'docs/reverse-client/r1-game-entity-constructors/summary.json';
    const priorHash = '4380a26444ce8cec142b8016326981a4aefcf8df26e0c8a8f833dc03fcd4001e';
    assert.equal(plan.prior_batch_cleanup?.summary_file, priorFile);
    assert.equal(plan.prior_batch_cleanup?.summary_sha256, priorHash);
    assert.equal(plan.prior_batch_cleanup?.observed, true);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'Prior Game constructor evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed'); assert.equal(prior.completed_plan_calls, 11);
    assert.equal(prior.closed_session_verified, true); assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true); assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
    for (const key of ['sha256_before','sha256_after']) assert.equal(prior.target[key], 'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2');
  }
  if (r1GameEntityConstructors) {
    const priorFile = 'docs/reverse-client/r1-crowd-vector-growth-retry-1/summary.json';
    const priorHash = 'b94fed1c486afde89f3c79984b32e072c5a7cd33cd999dcf0c63bfdad527c05a';
    assert.equal(plan.prior_batch_cleanup?.summary_file, priorFile);
    assert.equal(plan.prior_batch_cleanup?.summary_sha256, priorHash);
    assert.equal(plan.prior_batch_cleanup?.observed, true);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'Prior successful growth-retry evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed'); assert.equal(prior.completed_plan_calls, 11);
    assert.equal(prior.closed_session_verified, true); assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true); assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
    for (const key of ['sha256_before','sha256_after']) assert.equal(prior.target[key], '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1GameEntityConstructorSearch) {
    const priorFile = 'docs/reverse-client/r1-crowd-motion-helpers/summary.json';
    const priorHash = 'bc08e52772df1c5ff42063bcade04ee162daa47b544d00194da980e17bd50b48';
    assert.equal(plan.prior_batch_cleanup?.summary_file, priorFile);
    assert.equal(plan.prior_batch_cleanup?.summary_sha256, priorHash);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'Prior crowd-motion cleanup evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed');
    assert.equal(prior.completed_plan_calls, 23);
    assert.equal(prior.closed_session_verified, true);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true);
    assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
    for (const key of ['sha256_before','sha256_after']) assert.equal(prior.target[key], '4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1GameStateRegistrationSearch) {
    const priorFile = 'docs/reverse-client/r1-game-state-transition/summary.json';
    const priorHash = '09766ab179645d987328e020bbf5dd0c8fc63ab042dfb2d2cffc41c3ae65e940';
    assert.equal(plan.prior_batch_cleanup?.summary_file, priorFile);
    assert.equal(plan.prior_batch_cleanup?.summary_sha256, priorHash);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'Prior state-transition cleanup evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed');
    assert.equal(prior.completed_plan_calls, 8);
    assert.equal(prior.closed_session_verified, true);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true);
    assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
  }
  if (r1GameStateTransition) {
    const priorFile = 'docs/reverse-client/r1-exe-mouse-callers/summary.json';
    const priorHash = 'b2a3f318df5cd240dd459ce218cf0b3faeb407ec026128141b210dd50493137e';
    assert.equal(plan.prior_batch_cleanup?.summary_file, priorFile);
    assert.equal(plan.prior_batch_cleanup?.summary_sha256, priorHash);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'Prior EXE cleanup evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed');
    assert.equal(prior.completed_plan_calls, 6);
    assert.equal(prior.closed_session_verified, true);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true);
    assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
  }
  if (r1ExeMouseCallers) {
    const priorFile = 'docs/reverse-client/r1-game-mouse-state/summary.json';
    const priorHash = '86ce71e38477d958290e48a19df5f0b2409a5fc7972349a9ff354d6516095a0b';
    assert.equal(plan.prior_batch_cleanup?.summary_file, priorFile);
    assert.equal(plan.prior_batch_cleanup?.summary_sha256, priorHash);
    assert.equal(fileHash(path.join(root, priorFile)), priorHash, 'Prior batch cleanup evidence changed');
    const prior = JSON.parse(fs.readFileSync(path.join(root, priorFile), 'utf8'));
    assert.equal(prior.status, 'passed');
    assert.equal(prior.completed_plan_calls, 11);
    assert.equal(prior.closed_session_verified, true);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile?.job_empty_confirmed, true);
    assert.equal(prior.owned_memory_profile?.sampler_exit_confirmed, true);
    const namingFile = '.tooling/rea/node_modules/rea-agents/dist/ghidra/GhidraTargetSnapshot.js';
    const namingHash = 'a27e410752f5474b3a97f7388bf8fdace65d21d5dad8858ad3c27e310f03f52b';
    assert.equal(plan.snapshot_naming?.file, namingFile);
    assert.equal(plan.snapshot_naming?.sha256, namingHash);
    assert.equal(fileHash(path.join(root, namingFile)), namingHash, 'Installed snapshot naming changed');
    assert.equal(plan.snapshot_naming?.snapshot_name, 'target-d3915e259931.exe');
    assert.equal(plan.target_profile, 'client');
    assert.equal(plan.memory_profile.budget_id, budget.id);
    assert.equal(plan.memory_profile.heap, budget.heap);
    assert.equal(plan.memory_profile.launch_physical_and_commit_gib * gib, budget.launchBytes);
    assert.equal(plan.memory_profile.stop_physical_or_commit_gib * gib, budget.stopBytes);
    assert.equal(plan.memory_profile.maximum_sample_age_ms, budget.maximumSampleAgeMs);
    assert.equal(plan.memory_profile.stale_monitor_ms, budget.staleAfterMs);
    assert.equal(plan.memory_profile.task_allowance_gib, budget.taskAllowanceGiB);
    assert.equal(plan.memory_profile.system_spare_gib, budget.systemSpareGiB);
    assert.equal(plan.memory_profile.calibration_only, true);
    assert.equal(plan.memory_profile.measured_client_peak_available, false);
    assert.equal(plan.memory_profile.budget_source, 'provisional-first-client-EXE-calibration');
    assert.equal(plan.memory_profile.measurements, undefined);
    assert.equal(plan.memory_profile.measured_game_peak_available, undefined);
    assert.equal(plan.time_limits.provider_startup_ms, 330000);
    assert.equal(plan.time_limits.outer_request_ms, requestTimeout);
    assert.equal(plan.time_limits.changed, false);
    assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','xrefs','close_binary','binary_session']));
    assert.equal(plan.calls[1].args.path, targetPath);
    assert.equal(plan.calls[2].label, '03-Client-session');
    assert.equal(plan.calls[3].args.address, '0x1402d78a0');
    for (const item of plan.calls) {
      const tools = baseline.filter(tool => tool.name === item.tool);
      assert.equal(tools.length, 1);
      validateArgs(item.args, tools[0].inputSchema);
    }
  }
  if (r1GameFixedMode) {
    assert.equal(plan.memory_profile.budget_id, budget.id);
    assert.equal(plan.memory_profile.heap, budget.heap);
    assert.equal(plan.memory_profile.launch_physical_and_commit_gib * gib, budget.launchBytes);
    assert.equal(plan.memory_profile.stop_physical_or_commit_gib * gib, budget.stopBytes);
    assert.equal(plan.memory_profile.maximum_sample_age_ms, budget.maximumSampleAgeMs);
    assert.equal(plan.memory_profile.stale_monitor_ms, budget.staleAfterMs);
    assert.equal(plan.memory_profile.task_allowance_gib, budget.taskAllowanceGiB);
    assert.equal(plan.memory_profile.system_spare_gib, budget.systemSpareGiB);
    assert.equal(plan.memory_profile.calibration_only, r1GameMouseCalibration);
    assert.equal(plan.memory_profile.measured_game_peak_available, !r1GameMouseCalibration);
    assert.equal(plan.memory_profile.budget_source, r1GameMouseCalibration ? 'provisional-first-Game-calibration' : 'unchanged-provisional-Game-budget-with-prior-trial-observation');
    assert.equal(plan.memory_profile.measurements, undefined);
    assert.equal(plan.time_limits.provider_startup_ms, r1GameAssociatedRetry ? 480000 : 330000);
    assert.equal(plan.time_limits.outer_request_ms, requestTimeout);
    assert.equal(plan.time_limits.changed, r1GameAssociatedRetry);
    if (r1GameAnimationSetProduction) {
      const fixed = ['0x180127b70','0x180123810'].flatMap(address=>['procedure_pseudo_code','procedure_assembly'].map(tool=>[tool,address]));
      assert.equal(plan.batch_kind,'controller-action-slots-two-exact-disk-targets');
      assert.equal(plan.unchanged_guard_pins.length,8);
      const enginePin=plan.prior_clean_engine;
      assert.equal(canonical(enginePin),canonical({"file":"docs/reverse-client/r1-engine-stop-final-dependencies-run1/summary.json","sha256":"f682da1dcdb24c4eba966d7675bb24ab83b99c74cbd48b2e15fe3012e3c98de6","calls":11,"target_sha256":"4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153"}),'Prior clean Engine receipt changed');
      assert.equal(fileHash(path.join(root,enginePin.file)),enginePin.sha256);
      const enginePrevious=JSON.parse(fs.readFileSync(path.join(root,enginePin.file),'utf8'));
      assert.equal(enginePrevious.status,'passed');assert.equal(enginePrevious.tool_calls,11);assert.equal(enginePrevious.completed_plan_calls,11);
      for(const key of ['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(enginePrevious[key],true);
      assert.equal(enginePrevious.forced_termination,false);assert.equal(enginePrevious.child_exit_code,0);assert.equal(enginePrevious.memory.stop_triggered,false);
      for(const key of ['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(enginePrevious.owned_memory_profile[key],true);
      assert.equal(enginePrevious.owned_memory_profile.sampler_exit_code,0);assert.equal(canonical(enginePrevious.owned_memory_profile.failures),'[]');
      for(const key of ['sha256_before','sha256_after'])assert.equal(enginePrevious.target[key],enginePin.target_sha256);
      assert.equal(enginePrevious.memory.launch_threshold_gib,5);assert.equal(enginePrevious.memory.stop_threshold_gib,2);assert.equal(enginePrevious.memory.heap_limit,'384M');
      const failedPin=plan.prior_failed_clean_game;
      assert.equal(canonical(failedPin),canonical({"file":"docs/reverse-client/r1-player-animation-tail-run1/summary.json","sha256":"906d518b23a9cd8795bad4547c9d57aae144511fd9ada0fc973f3fc19185eea3","expected_status":"failed","completed_plan_calls":6,"tool_calls":9,"failed_call_number":7,"failed_label":"07-PlayerAnimationTail-0x1805da02e-procedure_pseudo_code","cleanup_labels":["cleanup-close","cleanup-session"],"target_sha256":"cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2"}),'Latest failed-but-clean Game prerequisite changed');
      assert.equal(fileHash(path.join(root,failedPin.file)),failedPin.sha256);
      const interruptedTail=JSON.parse(fs.readFileSync(path.join(root,failedPin.file),'utf8'));
      assert.equal(interruptedTail.status,'failed');assert.equal(interruptedTail.completed_plan_calls,6);assert.equal(interruptedTail.tool_calls,9);
      assert.equal(interruptedTail.records.length,9);assert.ok(interruptedTail.records.slice(0,6).every(item=>item.status==='passed'));
      assert.equal(canonical(interruptedTail.records.slice(6).map(item=>[item.call_number,item.tool,item.label,item.status])),canonical([
        [7,'procedure_pseudo_code','07-PlayerAnimationTail-0x1805da02e-procedure_pseudo_code','failed'],
        [8,'close_binary','cleanup-close','passed'],[9,'binary_session','cleanup-session','passed']]));
      assert.equal(interruptedTail.errors.length,1);
      for(const key of ['close_acknowledged','closed_session_verified','process_exit_confirmed'])assert.equal(interruptedTail[key],true);
      assert.equal(interruptedTail.forced_termination,false);assert.equal(interruptedTail.child_exit_code,0);assert.equal(interruptedTail.memory.stop_triggered,false);
      for(const key of ['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained'])assert.equal(interruptedTail.owned_memory_profile[key],true);
      assert.equal(interruptedTail.owned_memory_profile.sampler_exit_code,0);assert.equal(canonical(interruptedTail.owned_memory_profile.failures),'[]');
      for(const key of ['sha256_before','sha256_after'])assert.equal(interruptedTail.target[key],targetHash);
      // The failed7th query remains failed. Only its cleanup admits this separate two-target plan; no285E50/5DA02E retry or inferred native success.
      // One reviewed retry after the memory stop; never rewrite the run1 terminal result.
      const memoryRecovery = plan.animation_memory_stop_recovery;
      const expectedRecovery = {"schema_version":1,"retry_number":1,"maximum_new_attempts":1,"automatic_retry":false,"stop_on_repeated_failure":true,"original_closed_session_verified":false,"original_body_response_count":0,"original_runtime":"analysis-runtime/rea/rea-ghidra-9d6bb2aa4b39884db9519ae8312925fd","retained_runtime":"analysis-runtime/retained/r1-animation-set-production-memory-stop-20261026T084852Z","pinned_evidence":[{"file":"docs/reverse-client/r1-animation-set-production-run1/summary.json","sha256":"5d3e6d4412b5e910c896534132765a8fbd1ef21bce637e811f7b213607a01ea8"},{"file":"docs/reverse-client/r1-animation-set-production-run1/plan.json","sha256":"8cf52ac2b5ed25d39bbcdccaadaa07171ec50154d9e1bbb4ffd56427a1e0ea7d"},{"file":"docs/reverse-client/r1-animation-set-production-run1/progress.ndjson","sha256":"4da34e4e7a89c8a7c3f14dbfb410e27a0a4c34331b4014fbb52d9550924e341d"},{"file":"docs/reverse-client/r1-animation-set-production-run1/memory.ndjson","sha256":"f6d92c4630092a710ebefc8f1a64541ff9a34112209338cddf59a2680ba8650a"},{"file":"docs/reverse-client/r1-animation-set-production-run1/owned-memory-summary.json","sha256":"811cbefdf6c682c0a1696db6db4b4daaf0a12f29b04bc3c0e8d59d49e1f3f6f1"},{"file":"docs/reverse-client/r1-animation-set-production-run1/pre-retention-process-refresh.json","sha256":"63f1c67c3c2fed67e8846b8e13a915fc2564453d8c8b4622500cb52b26d40134"},{"file":"docs/reverse-client/r1-animation-set-production-run1/runtime-retention-before.json","sha256":"750f59adf3dcc88dbd281460d6551c9cf82321b21cbf2069358446d93fd638fa"},{"file":"docs/reverse-client/r1-animation-set-production-run1/runtime-retention.json","sha256":"7597f89b2eb489a0e1d740b1151cf5f5fdad6e77c685bdcb5cfa08c5effb1bbb"}],"unchanged_guards":true,"fresh_live_resource_and_identity_checks_required":true,"formal_execution_approval_required":true};
      assert.equal(canonical(memoryRecovery),canonical(expectedRecovery),'Animation recovery plan changed');
      const recoveryProofs = {};
      for (const pin of expectedRecovery.pinned_evidence) {
        const file = path.join(root,pin.file);
        assert.equal(fileHash(file),pin.sha256,'Animation memory-stop evidence changed');
        if (pin.file.endsWith('.json')) recoveryProofs[path.basename(file)] = JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));
      }
      const failedAnimation = recoveryProofs['summary.json'];
      assert.equal(failedAnimation.status,'failed');
      assert.equal(failedAnimation.plan_sha256,'8cf52ac2b5ed25d39bbcdccaadaa07171ec50154d9e1bbb4ffd56427a1e0ea7d');
      assert.equal(failedAnimation.tool_calls,4); assert.equal(failedAnimation.completed_plan_calls,3);
      assert.equal(failedAnimation.close_acknowledged,false); assert.equal(failedAnimation.closed_session_verified,false);
      assert.equal(failedAnimation.process_exit_confirmed,true); assert.equal(failedAnimation.forced_termination,true);
      assert.equal(failedAnimation.child_exit_code,92); assert.equal(failedAnimation.child_signal,null);
      assert.equal(failedAnimation.targets_launched,false); assert.equal(failedAnimation.memory.stop_triggered,true);
      assert.equal(failedAnimation.memory.launch_threshold_gib,5); assert.equal(failedAnimation.memory.stop_threshold_gib,2);
      assert.equal(failedAnimation.memory.heap_limit,'384M'); assert.equal(failedAnimation.memory.minimum_free_gib,1.8777275085449219);
      for (const key of ['sha256_before','sha256_after']) assert.equal(failedAnimation.target[key],targetHash);
      const stoppedProfile = failedAnimation.owned_memory_profile;
      assert.equal(canonical(stoppedProfile),canonical(recoveryProofs['owned-memory-summary.json']));
      for (const key of ['job_empty_confirmed','sampler_exit_confirmed','sampler_output_drained']) assert.equal(stoppedProfile[key],true);
      assert.equal(stoppedProfile.sampler_exit_code,0); assert.equal(canonical(stoppedProfile.failures),'[]');
      assert.equal(canonical(failedAnimation.records.map(item=>[item.call_number,item.label,item.status])),canonical([
        [1,'01-initial-session','passed'],[2,'02-Game-open','passed'],[3,'03-Game-session','passed'],
        [4,'04-AnimationSetProduction-0x180024080-procedure_pseudo_code','failed']]));
      assert.equal(failedAnimation.records[3].error,'Free physical memory fell below 2 GiB; stopping the owned Rea process tree');
      const failedDirectory = path.join(root,'docs/reverse-client/r1-animation-set-production-run1');
      assert.ok(!fs.readdirSync(failedDirectory).some(name=>/^\d+-AnimationSetProduction-.*-procedure_(pseudo_code|assembly|callees)\.json$/.test(name)),
        'Prior animation body appeared; inspect and reuse before retry');
      const refresh = recoveryProofs['pre-retention-process-refresh.json'];
      assert.equal(refresh.cim_succeeded,true); assert.equal(refresh.recorded_identity_count,43);
      assert.equal(refresh.remaining_same_identity_count,0); assert.equal(canonical(refresh.reused_pids),'[]');
      assert.equal(refresh.no_process_terminated,true);
      const retainedAnimation = recoveryProofs['runtime-retention.json'];
      assert.equal(retainedAnimation.status,'retained-with-identical-content');
      for (const key of ['all_relative_paths_lengths_sha256_matched','source_absent','failed_summary_unchanged']) assert.equal(retainedAnimation[key],true);
      for (const key of ['closed_session_verified','new_backend_started','retry_authorized_by_this_record']) assert.equal(retainedAnimation[key],false);
      assert.equal(retainedAnimation.deleted_files,0); assert.equal(canonical(retainedAnimation.remaining_active_runtime_directories),'[]');
      assert.equal(retainedAnimation.file_count,120); assert.equal(retainedAnimation.total_bytes,294890040);
      const animationSource = path.join(root,expectedRecovery.original_runtime);
      const animationDestination = path.join(root,expectedRecovery.retained_runtime);
      const beforeAnimation = recoveryProofs['runtime-retention-before.json'];
      for (const record of [beforeAnimation,retainedAnimation]) {
        assert.equal(path.resolve(record.source),animationSource); assert.equal(path.resolve(record.destination),animationDestination);
      }
      assert.ok(!fs.existsSync(animationSource),'Original animation runtime remains active');
      let cursor = animationDestination;
      while (true) {
        const st = fs.lstatSync(cursor); assert.ok(st.isDirectory() && !st.isSymbolicLink(),'Linked retained-runtime ancestor rejected');
        const parent = path.dirname(cursor); if (parent === cursor) break; cursor = parent;
      }
      const owner = beforeAnimation.owner;
      assert.equal(owner.run_id,'926f4eed-09e9-4fa4-976b-802b5dc5d95e');
      assert.equal(owner.ownership_kind,'windows-job-object'); assert.equal(owner.transport,'authenticated-loopback-tcp');
      assert.equal(owner.pid,24400); assert.equal(owner.process_group_id,24400); assert.equal(owner.parent_pid,22260);
      assert.equal(owner.created_at,'2026-10-26T08:44:28.055Z');
      assert.equal(path.resolve(owner.endpoint_path),path.join(animationSource,'bridge-endpoint.json'));
      assert.equal(path.resolve(owner.headless_script),path.join(root,'.tooling/ghidra/ghidra_12.1.4_PUBLIC/support/analyzeHeadless.bat'));
      assert.equal(beforeAnimation.process_check,'pre-retention-process-refresh.json');
      assert.equal(beforeAnimation.files.length,120);
      assert.equal(beforeAnimation.files.reduce((sum,file)=>sum+file.bytes,0),294890040);
      const retainedFilePins = new Map();
      for (const file of beforeAnimation.files) {
        const relative = file.path.replace(/\\/g,'/');
        assert.ok(relative && !path.isAbsolute(file.path) && !relative.split('/').some(part=>part === '' || part === '.' || part === '..') && !relative.includes(':'));
        assert.ok(!retainedFilePins.has(relative) && /^[0-9a-f]{64}$/.test(file.sha256) && Number.isSafeInteger(file.bytes) && file.bytes >= 0);
        retainedFilePins.set(relative,file);
      }
      // Bounded metadata traversal and64KiB hashing avoid loading a Ghidra database into memory.
      const retainedFilesSeen = new Set(); let retainedEntriesVisited = 0;
      const inspectRetainedAnimation = directory => {
        for (const entry of fs.readdirSync(directory,{withFileTypes:true})) {
          assert.ok(++retainedEntriesVisited <= 512,'Retained-runtime traversal exceeds fixed bound');
          const full = path.join(directory,entry.name); const st = fs.lstatSync(full);
          assert.ok(!st.isSymbolicLink(),'Linked retained-runtime entry rejected');
          if (st.isDirectory()) { inspectRetainedAnimation(full); continue; }
          assert.ok(st.isFile(),'Non-file retained-runtime entry rejected');
          const relative = path.relative(animationDestination,full).replace(/\\/g,'/');
          const pin = retainedFilePins.get(relative);
          assert.ok(pin && !retainedFilesSeen.has(relative),'Unexpected retained-runtime file');
          assert.equal(st.size,pin.bytes);
          const hash = createHash('sha256'); const fd = fs.openSync(full,'r'); const buffer = Buffer.alloc(65536);
          try { let count; while ((count=fs.readSync(fd,buffer,0,buffer.length,null)) !== 0) hash.update(buffer.subarray(0,count)); }
          finally { fs.closeSync(fd); }
          assert.equal(hash.digest('hex'),pin.sha256,'Retained animation runtime content changed');
          retainedFilesSeen.add(relative);
        }
      };
      inspectRetainedAnimation(animationDestination);
      assert.equal(retainedFilesSeen.size,retainedFilePins.size);
      assert.equal(canonical(JSON.parse(fs.readFileSync(path.join(animationDestination,'ownership.json'),'utf8').replace(/^\uFEFF/,''))),canonical(owner));
      assert.ok(!fs.existsSync(output),'Animation retry output exists; review its outcome');
      // Unchanged live resource/mutex/Job admission still runs below; historical exit evidence is not a fresh process inventory.
      // A new formally reviewed invocation corrects run2's argv; no prior outcome is upgraded.
      const invocationRecovery = {"schema_version":1,"new_formally_reviewed_attempt":1,"maximum_new_invocations":1,"automatic_retry":false,"stop_on_failure":true,"reason":"Correct the relative controller argument rejected before Job construction and wrapper release; this is a new review, not an automatic continuation or reset of the prior retry.","absolute_runner":"C:\\Users\\zhuzi\\Documents\\Grim-Dynasty\\tools\\static_analysis\\rea_native_registration.mjs","original_job_empty_confirmed":false,"original_closed_session_verified":false,"prior_mcp_calls":0,"prior_samples":0,"pinned_evidence":[{"file":"docs/reverse-client/r1-animation-set-production-run2/summary.json","sha256":"ad9c1dd1a654d631d733ac7df36a9bbe54f70ac7b62151274ec64208192f853b"},{"file":"docs/reverse-client/r1-animation-set-production-run2/owned-memory-sampler-stderr.txt","sha256":"52f1807ff3b1adc9c5f6dc4731449bf53f215dc85f45abbe0dfb02c7956f8413"},{"file":"docs/reverse-client/r1-animation-set-production-run2/post-exit-readonly-check.json","sha256":"17d6e1ec393717042ac9f1204d6f969bd9d825240f99d4f97f9595daab20b7ca"}],"sampler_unchanged":true,"fresh_live_resource_and_identity_checks_required":true,"formal_execution_approval_required":true};
      assert.equal(canonical(plan.invocation_correction),canonical(invocationRecovery),'Invocation correction plan changed');
      const invocationProofs = {};
      for (const pin of invocationRecovery.pinned_evidence) {
        const file=path.join(root,pin.file); assert.equal(fileHash(file),pin.sha256,'Run2 evidence changed');
        invocationProofs[path.basename(file)]=pin.file.endsWith('.json') ? JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,'')) : fs.readFileSync(file,'utf8');
      }
      const rejected=invocationProofs['summary.json'], rejectedProfile=rejected.owned_memory_profile, postExit=invocationProofs['post-exit-readonly-check.json'];
      assert.equal(rejected.status,'failed'); assert.equal(rejected.tool_calls,0); assert.equal(rejected.completed_plan_calls,0); assert.equal(canonical(rejected.records),'[]');
      for (const key of ['close_acknowledged','closed_session_verified']) assert.equal(rejected[key],false);
      assert.equal(rejected.process_exit_confirmed,true); assert.equal(rejected.forced_termination,true);
      assert.equal(rejectedProfile.samples,0); assert.equal(rejectedProfile.job_empty_confirmed,false); assert.equal(rejectedProfile.expected_heap_observed,false);
      assert.equal(rejectedProfile.sampler_exit_confirmed,true); assert.equal(rejectedProfile.sampler_exit_code,1); assert.equal(rejectedProfile.sampler_output_drained,true);
      const rejectedStderr=invocationProofs['owned-memory-sampler-stderr.txt'];
      assert.ok(rejectedStderr.includes('Controller argv does not identify the fixed associated identity-repair task.') && rejectedStderr.includes('measure_game_associated_repair_memory.ps1:52'));
      const failureLine=rejectedStderr.split(/\r?\n/).find(line=>line.startsWith('REA_SAMPLER_FAILURE '));
      assert.ok(failureLine); const gateFailure=JSON.parse(failureLine.slice('REA_SAMPLER_FAILURE '.length));
      assert.equal(gateFailure.sampler_phase,'startup'); assert.equal(gateFailure.root_pid,32812); assert.equal(gateFailure.controller_pid,28848);
      assert.equal(postExit.cim_succeeded,true); assert.equal(canonical(postExit.prior_ids),'[32812,28848,23528]');
      for (const key of ['matching_ids_or_direct_children','heavy','active_runtime_directories']) assert.equal(canonical(postExit[key]),'[]');
      assert.equal(postExit.no_process_terminated,true); assert.equal(postExit.no_security_changed,true);
      assert.equal(postExit.summary_sha256,invocationRecovery.pinned_evidence[0].sha256); assert.equal(postExit.stderr_sha256,invocationRecovery.pinned_evidence[1].sha256);
      assert.ok(!fs.readdirSync(path.join(root,'docs/reverse-client/r1-animation-set-production-run2')).some(name=>/^\d+-.*\.json$/.test(name)),'Run2 MCP response appeared; inspect before invocation');
      assert.ok(!fs.readdirSync(path.join(root,'analysis-runtime/rea')).some(name=>/^rea-ghidra-/.test(name)),'Active runtime exists');
      // Pinned code threw before Job construction and before ready/release. samples0 alone never means Job empty.
      assert.equal(canonical(plan.calls.map(item=>item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item=>item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,7).map(item=>[item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session');assert.equal(plan.calls[7].label,'08-Game-close');assert.equal(plan.calls[8].label,'09-final-session');
      const pin=plan.prior_successful_repaired_monitor;
      assert.equal(pin.file,'docs/reverse-client/r1-game-animation-name-growth-run1/summary.json');
      assert.equal(pin.sha256,'ffbb48f4c41311117e3f4da908200542c72eb86dd4609202ca5915e82a2a8404');
      assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);
      const previous=JSON.parse(fs.readFileSync(path.join(root,pin.file),'utf8'));
      assert.equal(previous.status,'passed');assert.equal(previous.completed_plan_calls,8);
      assert.equal(previous.tool_calls,8);assert.equal(previous.close_acknowledged,true);assert.equal(previous.forced_termination,false);assert.equal(previous.child_exit_code,0);
      assert.equal(previous.owned_memory_profile.sampler_exit_code,0);assert.equal(canonical(previous.owned_memory_profile.failures),'[]');
      for(const key of ['closed_session_verified','process_exit_confirmed'])assert.equal(previous[key],true);
      for(const key of ['job_empty_confirmed','sampler_exit_confirmed'])assert.equal(previous.owned_memory_profile[key],true);
      for(const key of ['sha256_before','sha256_after'])assert.equal(previous.target[key],targetHash);
    } else if (r1GameAnimationSetNames) {
      assert.equal(canonical(plan.calls.map(item=>item.tool)),canonical(['binary_session','open_binary','binary_session','search_procedures','close_binary','binary_session']));
      assert.equal(canonical(plan.calls[3].args),canonical({pattern:'CharacterMovementManager',mode:'literal',case_sensitive:false}));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[3].label,'04-CharacterMovementManager-literal-name-search');
      assert.equal(plan.calls[4].label,'05-Game-close'); assert.equal(plan.calls[5].label,'06-final-session');
      const exactSources=[{"file":"docs/reverse-client/r1-move-action-delete-run1/07-MoveManagerConstructor-0x18008e0b0-procedure_pseudo_code.json","sha256":"b902f7d2219ab316c6c065cdc36812e0aecec621c5f007ef8d087ac0f8faa8da","addresses":["0x18008e0b0"],"source_kind":"Actual saved pseudo-code names GAME::CharacterMovementManager and constructor; literal class token only, no writer or method guessed."}];assert.equal(canonical(plan.address_sources),canonical(exactSources));
      const source=exactSources[0];assert.equal(fileHash(path.join(root,source.file)),source.sha256);
      const body=payload(JSON.parse(fs.readFileSync(path.join(root,source.file),'utf8')));verifyTarget(body.subject);assert.equal(body.operation,'procedure_pseudo_code');assert.equal(body.parameters.procedure,'0x18008e0b0');
      assert.equal(typeof body.normalized_result,'string');assert.ok(body.normalized_result.includes('GAME::CharacterMovementManager::CharacterMovementManager'));
      const pin = plan.prior_successful_repaired_monitor;
      assert.equal(pin.file,'docs/reverse-client/r1-move-action-delete-run1/summary.json');
      assert.equal(pin.sha256,'f28f19f2e132ee126d015a9d818ff3c2f1d6b99fc11c66331cad72cbb0439094');
      assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);
      const previous = JSON.parse(fs.readFileSync(path.join(root,pin.file),'utf8'));
      assert.equal(previous.status,'passed'); assert.equal(previous.completed_plan_calls,11);
      for (const key of ['closed_session_verified','process_exit_confirmed']) assert.equal(previous[key],true);
      for (const key of ['job_empty_confirmed','sampler_exit_confirmed']) assert.equal(previous.owned_memory_profile[key],true);
      for (const key of ['sha256_before','sha256_after']) assert.equal(previous.target[key],targetHash);
    } else if (r1GameWaypointAnimation) {
      const fixed = ['0x18005ae40','0x180024de0'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[9].label,'10-Game-close'); assert.equal(plan.calls[10].label,'11-final-session');
      const pin = plan.prior_successful_repaired_monitor;
      assert.equal(pin.file,'docs/reverse-client/r1-game-associated-run3/summary.json');
      assert.equal(pin.sha256,'141fb8446b8f4d9120462b293aa41ead0e80301ff4414c266815c49f62731796');
      assert.equal(fileHash(path.join(root,pin.file)),pin.sha256);
      const previous = JSON.parse(fs.readFileSync(path.join(root,pin.file),'utf8'));
      assert.equal(previous.status,'passed'); assert.equal(previous.completed_plan_calls,8);
      for (const key of ['closed_session_verified','process_exit_confirmed']) assert.equal(previous[key],true);
      for (const key of ['job_empty_confirmed','sampler_exit_confirmed']) assert.equal(previous.owned_memory_profile[key],true);
      for (const key of ['sha256_before','sha256_after']) assert.equal(previous.target[key],targetHash);
    } else if (r1GameAssociated) {
      const fixed = ['0x180077d60'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,6).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[6].label,'07-Game-close'); assert.equal(plan.calls[7].label,'08-final-session');
    } else if (r1MoveActionDispatchLeaves) {
      const fixed = ['0x180024ff0','0x180081860'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[9].label,'10-Game-close'); assert.equal(plan.calls[10].label,'11-final-session');
    } else if (r1DurationManagerDirect) {
      const fixed = ['0x180279d10','0x180019520'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[9].label,'10-Game-close'); assert.equal(plan.calls[10].label,'11-final-session');
    } else if (r1DurationManagerConstructor) {
      const fixed = ['0x1802790a0'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,6).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[6].label,'07-Game-close'); assert.equal(plan.calls[7].label,'08-final-session');
    } else if (r1GameRunSpeedDirect) {
      const fixed = ['0x18011bac0','0x1800687e0','0x1803e5a10'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,12).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[12].label,'13-Game-close'); assert.equal(plan.calls[13].label,'14-final-session');
    } else if (r1GameRunSpeed) {
      const fixed = ['0x180069040'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,6).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[6].label,'07-Game-close'); assert.equal(plan.calls[7].label,'08-final-session');
    } else if (r1GameStateLookupDirect) {
      const fixed = ['0x180078960','0x180019e40'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[9].label,'10-Game-close'); assert.equal(plan.calls[10].label,'11-final-session');
    } else if (r1GameMainPlayerConnectionMethods) {
      const fixed = ['0x180089cb0','0x1800d04e0'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[9].label,'10-Game-close'); assert.equal(plan.calls[10].label,'11-final-session');
    } else if (r1GameMainPlayerConnection) {
      const fixed = ['0x18030afe0'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,6).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[6].label,'07-Game-close'); assert.equal(plan.calls[7].label,'08-final-session');
    } else if (r1GamePlayerVideoDirect) {
      const fixed = ['0x1802fa290'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,6).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[6].label,'07-Game-close'); assert.equal(plan.calls[7].label,'08-final-session');
    } else if (r1GameMouseDirectDependencies) {
      const fixed = ['0x18005bb80','0x1803ce7b0','0x1803eae40'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,12).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[12].label,'13-Game-close'); assert.equal(plan.calls[13].label,'14-final-session');
    } else if (r1GameSkillReferenceDirect) {
      const fixed = ['0x180531c50'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,6).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[6].label,'07-Game-close');
    } else if (r1GameStopStateDirect) {
      const fixed = ['0x18005b710'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,6).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[6].label,'07-Game-close');
    } else if (r1GameReachedGoalDependencies) {
      const fixed = ['0x18005fc30','0x180019d40'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[9].label,'10-Game-close');
    } else if (r1SkillMovedDirect) {
      const fixed = ['0x180533410'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,6).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session');assert.equal(plan.calls[6].label,'07-Game-close');assert.equal(plan.calls[7].label,'08-final-session');
    } else if (r1GameLevelPublicationTail) {
      const fixed = ['0x180015880','0x18006da30'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session');assert.equal(plan.calls[9].label,'10-Game-close');assert.equal(plan.calls[10].label,'11-final-session');
    } else if (r1GameEntityPublicationGates) {
      const fixed = ['0x180016d20','0x1803d5bc0','0x1805d9ff2','0x1805d9f02','0x180015880'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,18).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session');assert.equal(plan.calls[18].label,'19-Game-close');assert.equal(plan.calls[19].label,'20-final-session');
    } else if (r1GameMovementMath) {
      const fixed = ['0x180591a10','0x180591b80','0x18007e6f0'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,12).map(item => [item.tool,item.args.procedure])),canonical(fixed));
      assert.equal(plan.calls[2].label,'03-Game-session'); assert.equal(plan.calls[12].label,'13-Game-close'); assert.equal(plan.calls[13].label,'14-final-session');
    } else if (r1GameCallbackRemainingRetry || r1GameCallbackRemaining) {
      const fixed = ['0x18005b730','0x180591480','0x18005e160'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,12).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
      assert.equal(plan.calls[12].label, '13-Game-close'); assert.equal(plan.calls[13].label, '14-final-session');
    } else if (r1GameCallbackCore) {
      const fixed = ['0x180019d20','0x180016d10','0x1805d9f8c','0x18005b730','0x180591480','0x18005e160'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,21).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
      assert.equal(plan.calls[21].label, '22-Game-close'); assert.equal(plan.calls[22].label, '23-final-session');
    } else if (r1GameMovementInterface) {
      const fixed = ['0x180066c20','0x1803e8200','0x180066d60','0x180066ee0','0x180067170'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,18).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
      assert.equal(plan.calls[18].label, '19-Game-close'); assert.equal(plan.calls[19].label, '20-final-session');
    } else if (r1GameEntityConstructors) {
      const fixed = ['0x180052030','0x1803cc3f0'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
      assert.equal(plan.calls[9].label, '10-Game-close'); assert.equal(plan.calls[10].label, '11-final-session');
    } else if (r1GameCameraPosition || r1GameCameraAngle || r1GameCameraConstructor) {
      const fixed = [r1GameCameraPosition ? '0x1802be6e0' : r1GameCameraAngle ? '0x1802bfaf0' : '0x1802be3b0']
        .flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,6).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1PortalDependencies) {
      const fixed = ['0x180019230','0x1805dc3f8']
        .flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1WaypointQueueHelpers) {
      const fixed = ['0x18008f340','0x180074110']
        .flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1WaypointExecution) {
      const fixed = ['0x180073f70','0x18008df30','0x18008f4c0','0x18008d9e0']
        .flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,15).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1MoveManager) {
      const fixed = ['0x18008e170','0x18008eaf0','0x18008ed00','0x18005d9a0']
        .flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,15).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1MoveStartDependencies) {
      const fixed = ['0x18005f870','0x18005e670','0x180534540','0x180082ef0','0x1800819e0']
        .flatMap(address => (address === '0x1800819e0' ? ['procedure_assembly'] : ['procedure_pseudo_code','procedure_assembly','procedure_callees']).map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,16).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1MoveStart) {
      const fixed = ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,'0x180082c00']);
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,6).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1ActionSupport) {
      const fixed = ['0x180089bf0','0x18007a6d0','0x180073ba0','0x1805405a0']
        .flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,15).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1ActionExecutor) {
      const fixed = ['0x1801600f0','0x180081870','0x180089ac0']
        .flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,12).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1PathLocalDispatch) {
      const fixed = ['0x18008e200','0x180127e40','0x1802ebb70']
        .flatMap(address => (address === '0x1802ebb70' ? ['procedure_assembly'] : ['procedure_pseudo_code','procedure_assembly','procedure_callees']).map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,10).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1MovementActionPath) {
      const fixed = ['0x1803ce890','0x180082b10','0x180127dd0','0x180078d40','0x180531a30']
        .flatMap(address => (address === '0x180078d40' ? ['procedure_assembly'] : ['procedure_pseudo_code','procedure_assembly','procedure_callees']).map(tool => [tool,address]));
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,16).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1PlayerMoveDispatch) {
      const fixed = [
        ['procedure_assembly','0x180163490'], ['procedure_callees','0x180163490'],
        ['procedure_pseudo_code','0x180124340'], ['procedure_assembly','0x180124340'], ['procedure_callees','0x180124340'],
        ['procedure_assembly','0x180125a00'],
      ];
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])), canonical(fixed));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1PlayerMouseHandlers) {
      const addresses = r1PlayerMoveEntries ? ['0x180165cb0','0x1801662d0','0x180166020'] : ['0x180161d00','0x180162290','0x1801624b0'];
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...addresses.flatMap(() => ['procedure_pseudo_code','procedure_assembly','procedure_callees']),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,12).map(item => item.args.procedure)), canonical(addresses.flatMap(a => [a,a,a])));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1GameAddState || r1PlayerRegisterStates) {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','procedure_pseudo_code','procedure_assembly','procedure_callees','procedure_callers','close_binary','binary_session']));
      assert.ok(plan.calls.slice(3,7).every(item => item.args.procedure === (r1PlayerRegisterStates ? '0x18015e440' : '0x180125730')));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1GameEntityConstructorSearch) {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','search_procedures','close_binary','binary_session']));
      assert.equal(canonical(plan.calls[3].args), canonical({pattern: gameEntityConstructorPattern, mode: 'regex', case_sensitive: true}));
      assert.equal(plan.calls[2].label, '03-Game-session');
      assert.equal(plan.calls[3].label, '04-Player-Character-constructor-name-search');
      assert.equal(plan.calls[4].label, '05-Game-close');
      assert.equal(plan.calls[5].label, '06-final-session');
    } else if (r1GameStateRegistrationSearch) {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','search_procedures','close_binary','binary_session']));
      assert.equal(canonical(plan.calls[3].args), canonical({pattern: 'ControllerAI.*(Register|AddState|Initialize)|(Register|AddState|Initialize).*ControllerAI', mode: 'regex', case_sensitive: false}));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1GameStateTransition) {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','procedure_pseudo_code','procedure_assembly','procedure_callees','close_binary','binary_session']));
      assert.ok(plan.calls.slice(3,6).every(item => item.args.procedure === '0x180123d50'));
      assert.equal(plan.calls[2].label, '03-Game-session');
    } else if (r1GameMouseState) {
      const fixedAddresses = ['0x180122960','0x180128530'];
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixedAddresses.flatMap(() => ['procedure_pseudo_code','procedure_assembly','procedure_callees']),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => item.args.procedure)), canonical(fixedAddresses.flatMap(address => [address,address,address])));
    } else if (r1GameMouseLinks) {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','xrefs','procedure_pseudo_code','procedure_assembly','procedure_callees','close_binary','binary_session']));
      assert.equal(plan.calls[3].args.address, '0x18015e1f0');
      assert.ok(plan.calls.slice(4,7).every(item => item.args.procedure === '0x18015d5a0'));
    } else {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','procedure_pseudo_code','procedure_assembly','procedure_callees','close_binary','binary_session']));
      assert.ok(plan.calls.slice(3,6).every(item => item.args.procedure === '0x18015e1f0'));
    }
    assert.equal(plan.calls[1].args.path, targetPath);
    for (const item of plan.calls) {
      const tools = baseline.filter(tool => tool.name === item.tool);
      assert.equal(tools.length, 1);
      validateArgs(item.args, tools[0].inputSchema);
    }
  }
  if (parserBudgetMode) {
    assert.equal(plan.memory_profile.budget_id, budget.id);
    assert.equal(plan.memory_profile.heap, budget.heap);
    assert.equal(plan.memory_profile.launch_physical_and_commit_gib * gib, budget.launchBytes);
    assert.equal(plan.memory_profile.stop_physical_or_commit_gib * gib, budget.stopBytes);
    assert.equal(plan.memory_profile.maximum_sample_age_ms, budget.maximumSampleAgeMs);
    assert.equal(plan.memory_profile.stale_monitor_ms, budget.staleAfterMs);
    assert.equal(fileHash(path.join(root, plan.memory_profile.measurements)), plan.memory_profile.measurements_sha256);
    if (r1LevelObjectRecords || r1MoveWorldDistance || r1MoveProductionEngine || r1EngineMouseGetters || r1EngineReachedGoalDependencies || r1WorldPositionLevelInitialization || r1TerrainTypeMaterial || r1WaterInitialization || r1GroundResourcesWater || r1GroundFaceUnlink || r1GroundIntersectionRemaining || r1GroundIntersectionDirect || r1GroundRayTree || r1GroundFloorGeometry || r1GroundBlocksNodeDestroy || r1SpatialGroundDirect || r1EngineSpatialGround || r1EngineEntityPublicationCore || r1SameRegionMovement || r1PositionPublication || r1ActorRadius || r1CrowdPointCopy || r1CrowdPointReallocation || r1CrowdDirectAlgorithms || r1EngineCallbackGetters || r1CrowdMotionBranches || r1CrowdVectorGrowth || r1CrowdMotionHelpers || r1CrowdAgentGroupRegistration || r1CrowdVectorRegistration || r1CrowdAdapterRegistration || r1CrowdQueryProduction || r1CrowdMovementDirect || r1MovementFloorCore || r1NavigationConsumerDirect || r1StraightPathPortalWrapper || r1CorridorPortalHelper || r1NavigationCrossPolyCorridor || r1TriangulationAndTileConnection || r1MeshLinksAndTriangulationLeaf || r1MeshTileAndPredicates || r1NavPublishAndPolygonCore || r1NavPublicationAndContourLeaves || r1NavUpdateAndContours || r1TileProductionDirectStages || r1TileCacheAndDecompression || r1MapRegionAndTileProduct || r1MapDirectoryAndPathTiles || r1WorldLoadAndNavPublication || r1LevelLoadAndNavJobCandidates || r1LevelStreamAndNavJob || r1RegionBlobLoading || r1RegionLoadPublication || r1NavMeshProductionEntry || r1RegionLoadEntry || r1NavigationMeshQueryInitialization || r1NavigationImplementationConstructor || r1NavManagerConstructor || r1NavigationNearestDependencies || r1NavigationNearestHelpers || r1NavigationPolygonGeometry || r1NavigationSinglePoly || r1NavigationCore || r1EngineNavigationSearch || r1EngineCameraTarget || r1EnginePortalCamera) {
      assert.equal(plan.memory_profile.additional_launch_floor_gib, 5);
      assert.equal(plan.time_limits.provider_startup_ms, 330000);
      assert.equal(plan.time_limits.outer_request_ms, requestTimeout);
      assert.equal(plan.time_limits.changed, false);
      if (r1EngineReachedGoalDependencies) {
        const fixed = ['0x18001c040','0x1800ec010'].flatMap(address => ['procedure_pseudo_code','procedure_assembly','procedure_callees'].map(tool => [tool,address]));
        assert.equal(canonical(plan.calls.map(item => item.tool)),canonical(['binary_session','open_binary','binary_session',...fixed.map(item => item[0]),'procedure_callers','xrefs','close_binary','binary_session']));
        assert.equal(canonical(plan.calls.slice(3,9).map(item => [item.tool,item.args.procedure])),canonical(fixed));
        assert.equal(canonical(plan.calls[9].args),canonical({procedure:'0x180229700'}));
        assert.equal(canonical(plan.calls[10].args),canonical({address:'0x180229700'}));
        assert.equal(plan.calls[11].label,'12-Engine-close');
      } else if (r1TerrainTypeMaterial) {
        assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','procedure_pseudo_code','procedure_assembly','procedure_callees','procedure_assembly','close_binary','binary_session']));
        assert.equal(canonical(plan.calls.slice(3,7).map(item => item.args.procedure)), canonical(['0x1801e1940','0x1801e1940','0x1801e1940','0x1801e0f10']));
      } else if(r1EngineAnimationSpeed) {
        const providerSymbol='?RegisterProvider@LuaManager@GAME@@QEAAXPEAVLuaProvider@2@@Z';
        assert.equal(canonical(plan.calls.map(item=>item.tool)),canonical(['binary_session','open_binary','binary_session','procedure_pseudo_code','procedure_assembly','procedure_callees','procedure_callers','close_binary','binary_session']));
        assert.equal(canonical(plan.calls.slice(3,7).map(item=>item.args)),canonical([{procedure:providerSymbol},{procedure:providerSymbol},{procedure:providerSymbol},{procedure:'0x1800f2e00'}]));
      } else {
      const fixedAddresses = r1LevelObjectRecords ? ['0x180132580'] : r1EngineAnimationSpeed ? ['0x180257cb0','0x1802577c0','0x1802dc3d4'] : r1MoveWorldDistance ? ['0x180224770'] : r1MoveProductionEngine ? ['0x180008300','0x180078620'] : r1EngineMouseGetters ? ['0x18000bbe0','0x180073660','0x180182d60','0x18022c240'] : r1WorldPositionLevelInitialization ? ['0x18022c690','0x18012b750'] : r1WaterInitialization ? ['0x1801f6a90','0x1801f6e80'] : r1GroundResourcesWater ? ['0x1801ce350','0x18012c590','0x1801f81e0','0x1801f9bd0'] : r1GroundFaceUnlink ? ['0x180051b30','0x1801cf420','0x1801cff90','0x1801ce0e0'] : r1GroundIntersectionRemaining ? ['0x180044660','0x1801cf830','0x1801deb80','0x180113150','0x18003d760','0x1801d0290','0x180130270'] : r1GroundIntersectionDirect ? ['0x1801df220','0x1801de850','0x1801de810','0x1801deb80','0x180113150','0x18003d760','0x1801d0290','0x180130270'] : r1GroundRayTree ? ['0x18003d780','0x18011a600','0x1801cfdf0','0x1801de880','0x1801d1330','0x18000ce80','0x1801d12f0'] : r1GroundFloorGeometry ? ['0x1801d0100','0x1801d0fc0','0x1801d0e20'] : r1GroundBlocksNodeDestroy ? ['0x18011bb30','0x1801dde10','0x1801df4e0','0x1801ce910'] : r1SpatialGroundDirect ? ['0x18011be80','0x18011bbc0','0x1801de090'] : r1EngineSpatialGround ? ['0x180134500','0x180133bc0','0x180133a10','0x1801dda90','0x18007ed70'] : r1EngineEntityPublicationCore ? ['0x18000ce70','0x18002a990','0x180078110','0x1801333e0'] : r1SameRegionMovement ? ['0x1801943e0','0x18012f4f0'] : r1PositionPublication ? ['0x18021bb40','0x180228ab0'] : r1ActorRadius ? ['0x18002b410'] : r1CrowdPointCopy ? ['0x180021300'] : r1CrowdPointReallocation ? ['0x180030ee0'] : r1CrowdDirectAlgorithms ? ['0x180030e10','0x180263290','0x18025ef20'] : r1EngineCallbackGetters ? ['0x18007f070','0x18007f130','0x18007eb20','0x18022c1a0','0x18002b460','0x18007e7c0','0x180077880'] : r1CrowdMotionBranches ? ['0x180030c60','0x180030900','0x18025ba90','0x18025be90','0x1800309f0','0x18025f250','0x18025c080','0x18025bcf0'] : r1CrowdVectorGrowth ? ['0x180034ec0','0x180034fe0'] : r1CrowdMotionHelpers ? ['0x18025e900','0x18025ea00','0x18025eb90','0x18025bf40','0x180034cf0','0x180058190'] : r1CrowdAgentGroupRegistration ? ['0x18025e6c0','0x18025a250','0x18025c340'] : r1CrowdVectorRegistration ? ['0x18025ae20'] : r1CrowdAdapterRegistration ? ['0x180030790','0x180030b30','0x180030bb0','0x180151020'] : r1CrowdQueryProduction ? ['0x18025a880','0x18025a4f0','0x18025e320'] : r1CrowdMovementDirect ? ['0x18002f800','0x18025c440','0x18025a1c0','0x18025c1c0','0x18005daf0'] : r1MovementFloorCore ? ['0x18025b300','0x1800664d0','0x180221da0','0x18025b6d0'] : r1NavigationConsumerDirect ? ['0x180151130','0x18022c350','0x18022c620'] : r1StraightPathPortalWrapper ? ['0x180262d30'] : r1CorridorPortalHelper ? ['0x180262f10'] : r1NavigationCrossPolyCorridor ? ['0x18026dca0','0x18025f680','0x18026ddc0','0x1802631e0','0x18025f6f0','0x18026dc10'] : r1TriangulationAndTileConnection ? ['0x18026a650','0x18026a730','0x180266760','0x1802687a0'] : r1MeshLinksAndTriangulationLeaf ? ['0x18026a7d0','0x180267330','0x180267450','0x180266dd0','0x1802670d0','0x1802686c0','0x180266c60'] : r1MeshTileAndPredicates ? ['0x180268820','0x180268970','0x180268270','0x18026a860','0x18026a9c0'] : r1NavPublishAndPolygonCore ? ['0x180265680','0x18026ab20','0x18026ae40','0x18026b120','0x18026a0c0'] : r1NavPublicationAndContourLeaves ? ['0x18010d990','0x1802694e0','0x1802693f0','0x180269780'] : r1NavUpdateAndContours ? ['0x180151180','0x180269590','0x180269850','0x180269bf0'] : r1TileProductionDirectStages ? ['0x180109820','0x1802660f0','0x180266010','0x1802641c0','0x180268cf0','0x180269d70','0x18026bd40','0x18026e3f0'] : r1TileCacheAndDecompression ? ['0x180263b20','0x180263e40','0x18026cc50'] : r1MapRegionAndTileProduct ? ['0x180191980','0x1802640f0','0x1802658d0'] : r1MapDirectoryAndPathTiles ? ['0x180223140','0x180264200','0x180265790'] : r1WorldLoadAndNavPublication ? ['0x1801099b0','0x18021a4f0'] : r1LevelLoadAndNavJobCandidates ? ['0x18012c770','0x1801095c0','0x180109540','0x180109590'] : r1LevelStreamAndNavJob ? ['0x18012c6a0','0x180195af0','0x180109410'] : r1RegionBlobLoading ? ['0x180194e20','0x1801949f0','0x18010b1e0'] : r1RegionLoadPublication ? ['0x180194550','0x180194440','0x180150440'] : r1NavMeshProductionEntry ? ['0x180196dd0','0x18012dfc0','0x18010d920'] : r1RegionLoadEntry ? ['0x180196380','0x180196790','0x180150d20'] : r1NavigationMeshQueryInitialization ? ['0x1802664d0','0x18025f910'] : r1NavigationImplementationConstructor ? ['0x18010a530'] : r1NavManagerConstructor ? ['0x180150280'] : r1NavigationNearestDependencies ? ['0x180261320','0x18026d580','0x18001c510'] : r1NavigationNearestHelpers ? ['0x180261950','0x180260520'] : r1NavigationPolygonGeometry ? ['0x18026d730'] : r1NavigationSinglePoly ? ['0x1802688c0','0x180260ad0','0x1802623b0','0x18025f770'] : r1NavigationCore ? ['0x1802610f0','0x180261cb0','0x180262480'] : r1EngineNavigationSearch ? ['0x18010c6c0'] : r1EngineCameraTarget ? ['0x18022c0b0','0x18022c5d0','0x1800716e0'] : ['0x180229160','0x1800825c0'];
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixedAddresses.flatMap(() => ['procedure_pseudo_code','procedure_assembly','procedure_callees']),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,3+fixedAddresses.length*3).map(item => item.args.procedure)), canonical(fixedAddresses.flatMap(address => [address,address,address])));
      }
      assert.equal(plan.calls[2].label, '03-Engine-session');
    } else if (r1EngineMouseCore) {
      const fixedAddresses = ['0x180044a10','0x18004e170','0x180150590','0x18000d480'];
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session',...fixedAddresses.flatMap(() => ['procedure_pseudo_code','procedure_assembly','procedure_callees']),'close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,15).map(item => item.args.procedure)), canonical(fixedAddresses.flatMap(address => [address,address,address])));
    } else if (r1EngineInput) {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','procedure_pseudo_code','procedure_assembly','procedure_callees','procedure_pseudo_code','procedure_assembly','procedure_callees','close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item => item.args.procedure)), canonical(['0x180077850','0x180077850','0x180077850','0x180229700','0x180229700','0x180229700']));
    } else if (r0ToolBaseline || r0ToolBaselineTerminalRetry) {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session', 'open_binary', 'binary_session', 'procedure_pseudo_code', 'procedure_assembly', 'procedure_callees', 'close_binary', 'binary_session']));
      assert.ok(plan.calls.slice(3, 6).every(item => item.args.procedure === '0x180274ec0'));
    } else if (binaryParserEntry) {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session', 'open_binary', 'binary_session', 'procedure_pseudo_code', 'procedure_assembly', 'procedure_callees', 'close_binary', 'binary_session']));
      assert.ok(plan.calls.slice(3, 6).every(item => item.args.procedure === '0x18013f510'));
    } else if (binaryParserHelpers) {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','procedure_pseudo_code','procedure_assembly','procedure_pseudo_code','procedure_assembly','procedure_pseudo_code','procedure_assembly','close_binary','binary_session']));
      assert.equal(canonical(plan.calls.slice(3,9).map(item=>item.args.procedure)), canonical(['0x180030d20','0x180030d20','0x1800c69f0','0x1800c69f0','0x180284080','0x180284080']));
    } else if (recordCacheFixedPair) {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','procedure_pseudo_code','procedure_assembly','procedure_callees','procedure_pseudo_code','procedure_assembly','procedure_callees','close_binary','binary_session']));
      const fixedAddresses = recordCacheMaintenance ? ['0x18002ea80','0x18002eb40'] : recordCacheNode ? ['0x18001f170','0x180022860'] : ['0x18005deb0','0x18002e770'];
      assert.equal(canonical(plan.calls.slice(3,9).map(item=>item.args.procedure)), canonical(fixedAddresses.flatMap(address=>[address,address,address])));
    } else {
      assert.equal(canonical(plan.calls.map(item => item.tool)), canonical(['binary_session','open_binary','binary_session','procedure_pseudo_code','procedure_assembly','close_binary','binary_session']));
      assert.ok(plan.calls.slice(3,5).every(item=>item.args.procedure==='0x18002e0f0'));
    }
    assert.equal(plan.calls[1].args.path, targetPath);
    for (const item of plan.calls) {
      const tools = baseline.filter(tool => tool.name === item.tool);
      assert.equal(tools.length, 1);
      validateArgs(item.args, tools[0].inputSchema);
    }
  }
  if (r1PlayerMouseHandlersRetry) {
    const recovery = plan.recovery_from_failed_batch;
    const pinned = [
      ['summary', 'docs/reverse-client/r1-player-mouse-handlers/summary.json', 'bc292b53d91ade457fd392a6c420b15e0633f12d4f38f07523de725a8dfd84c2'],
      ['quarantine', '.tooling/r1-player-mouse-handlers-retry-prepared/runtime-quarantine-result.json', '10a4d7640c2ebd2207ce4ad43ada926315f79b8d7f612120d46ecea4f7024a15'],
      ['before', '.tooling/r1-player-mouse-handlers-retry-prepared/runtime-before.json', '9d0714a98c11911bfc978000da6621f05071f511de106fc9cc5166eef5a1032c'],
      ['after', '.tooling/r1-player-mouse-handlers-retry-prepared/runtime-after.json', '260a4af297de0733eaf6b71a172b1eee9d835f89db1d855d41afa64565dad10e'],
    ];
    const checked = {};
    for (const [key, relative, fingerprint] of pinned) {
      assert.equal(recovery[key + '_file'], relative);
      assert.equal(recovery[key + '_sha256'], fingerprint);
      assert.equal(fileHash(path.join(root, relative)), fingerprint, 'Recovery evidence changed');
      checked[key] = JSON.parse(fs.readFileSync(path.join(root, relative), 'utf8'));
    }
    const failed = checked.summary, retained = checked.quarantine;
    assert.equal(failed.status, 'failed');
    assert.equal(failed.tool_calls, 4); assert.equal(failed.completed_plan_calls, 3);
    assert.equal(failed.closed_session_verified, false); assert.equal(failed.forced_termination, true);
    assert.equal(failed.child_exit_code, 92); assert.equal(failed.process_exit_confirmed, true);
    assert.equal(failed.owned_memory_profile.job_empty_confirmed, true);
    assert.equal(failed.owned_memory_profile.sampler_exit_confirmed, true);
    assert.equal(failed.target.sha256_before, targetHash); assert.equal(failed.target.sha256_after, targetHash);
    assert.equal(retained.status, 'quarantined-with-content-preserved');
    assert.equal(retained.content_manifest_equal, true); assert.equal(retained.deleted_files, 0);
    assert.equal(retained.active_runtime_roots, 0); assert.equal(retained.normal_mcp_close_retroactively_claimed, false);
    assert.equal(retained.runner_guard_changed, false); assert.equal(retained.external_process_gate_waived, false);
    assert.equal(retained.process_check_before_move.recorded_pid_count, 6);
    assert.equal(retained.process_check_before_move.recorded_pids_present, 0);
    assert.equal(retained.process_check_before_move.analysis_process_count, 0);
    const oldRoot = path.join(root, 'analysis-runtime/rea/rea-ghidra-e4dbad03946d109dd66ecb819de833cd');
    const retainedRoot = path.join(root, 'analysis-runtime/retained/failed-r1-player-mouse-handlers-e4dbad03946d109dd66ecb819de833cd');
    assert.equal(path.resolve(retained.source), oldRoot); assert.equal(path.resolve(retained.destination), retainedRoot);
    assert.ok(!fs.existsSync(oldRoot), 'Old active runtime still present');
    assert.ok(fs.statSync(retainedRoot).isDirectory(), 'Preserved runtime missing');
    assert.equal(checked.after.matches_before, true);
    assert.equal(canonical(checked.before.entries), canonical(checked.after.entries));
    for (const item of plan.calls.slice(3, 12)) assert.ok(!fs.existsSync(path.join(root, 'docs/reverse-client/r1-player-mouse-handlers', item.label + '.json')), 'Earlier handler result exists; review reuse');
  }
  if (r1GameCallbackCoreRetry) {
    const recovery = plan.recovery_from_failed_batch;
    assert.equal(recovery.retry_number, 1); assert.equal(recovery.maximum_retries, 1);
    assert.equal(recovery.stop_on_repeated_failure, true); assert.equal(recovery.original_body_response_count, 0);
    for (const key of ['automatic_retry','original_closed_session_verified','original_job_empty_confirmed','monitor_changed','wait_policy_changed','root_cause_proven','runtime_guard_waived','external_process_gate_waived']) assert.equal(recovery[key], false);
    const pinned = [
      [
        "summary",
        "docs/reverse-client/r1-game-callback-core/summary.json",
        "995d942f82bf422e666ae8557f97f34c682e2489be6300464e0aed7ccfef3838"
      ],
      [
        "failure",
        "docs/reverse-client/r1-game-callback-core/owned-memory.ndjson.failure.json",
        "6197326ce20dbbb1ef6b122bd0ec097ad5c6130b84cd2c5c60927a5a89848ad9"
      ],
      [
        "quarantine",
        ".tooling/r1-game-callback-core-retry-prepared/runtime-quarantine-result.json",
        "3aa2a7c97aec916aa9d6799b6d272c9f92ed32dc5545b287cf4a02cac3f4123a"
      ],
      [
        "content_before",
        ".tooling/r1-game-callback-core-retry-prepared/runtime-content-before.json",
        "99e2baf78b942678e15ae0ce3855248ae4efb9ffc62ede3ceffd2f4eca00992d"
      ],
      [
        "content_after",
        ".tooling/r1-game-callback-core-retry-prepared/runtime-content-after.json",
        "99e2baf78b942678e15ae0ce3855248ae4efb9ffc62ede3ceffd2f4eca00992d"
      ],
      [
        "process_recheck",
        ".tooling/r1-game-callback-core-retry-prepared/process-recheck.json",
        "f5e69dc5beaacf10d5eaffbb870d99269b5782cd4ee8a85eb3f6044a55ad2ac1"
      ],
      [
        "original_plan",
        "tools/static_analysis/rea-r1-game-callback-core-plan.json",
        "8d70ef34323f0802d624d913d36bfa5dd86cff1cd29f1cf956e0c59abd1c271f"
      ]
    ];
    const checked = {};
    assert.equal(recovery.pinned_evidence.length, pinned.length);
    for (const [key, relative, fingerprint] of pinned) {
      const evidence = recovery.pinned_evidence.filter(item => item.key === key);
      assert.equal(evidence.length, 1); assert.equal(evidence[0].file, relative); assert.equal(evidence[0].sha256, fingerprint);
      assert.equal(fileHash(path.join(root, relative)), fingerprint, 'Game callback retry recovery evidence changed');
      checked[key] = JSON.parse(fs.readFileSync(path.join(root, relative), 'utf8'));
    }
    const failed = checked.summary, failure = checked.failure, retained = checked.quarantine, observed = checked.process_recheck;
    assert.equal(failed.status, 'failed'); assert.equal(failed.tool_calls, 4); assert.equal(failed.completed_plan_calls, 3);
    assert.equal(failed.closed_session_verified, false); assert.equal(failed.close_acknowledged, false);
    assert.equal(failed.process_exit_confirmed, true); assert.equal(failed.forced_termination, true); assert.equal(failed.child_exit_code, 0);
    assert.equal(failed.owned_memory_profile.job_empty_confirmed, false); assert.equal(failed.owned_memory_profile.sampler_exit_confirmed, true);
    assert.equal(failed.plan_sha256, '8d70ef34323f0802d624d913d36bfa5dd86cff1cd29f1cf956e0c59abd1c271f');
    for (const key of ['sha256_before','sha256_after']) assert.equal(failed.target[key], targetHash);
    assert.equal(failure.probe_step, 'QueryFullProcessImageName'); assert.equal(failure.image_query_error_code, 5);
    assert.equal(failure.process_id, 41796); assert.equal(String(failure.creation_ticks), '639283069960200187');
    assert.equal(failure.in_job, true); assert.equal(failure.exit_confirmed_after_image_failure, false);
    assert.equal(failure.terminal_wait_available_at_entry, false); assert.equal(failure.terminal_wait_attempted, false);
    assert.equal(failure.terminal_wait_consumer_pid, 39924); assert.equal(failure.terminal_wait_exit_result, true);
    assert.equal(retained.status, 'quarantined-with-content-preserved');
    assert.equal(retained.recorded_pids_rechecked, 26); assert.equal(retained.recorded_pids_present, 0); assert.equal(retained.body_responses, 0);
    assert.equal(retained.files, 43); assert.equal(retained.entries, 69); assert.equal(retained.bytes, 226029208);
    assert.equal(retained.content_manifest_equal, true); assert.equal(retained.deleted_files, 0);
    for (const key of ['normal_close_retroactively_claimed','old_job_empty_retroactively_claimed','monitor_changed','backend_started']) assert.equal(retained[key], false);
    assert.equal(observed.status, 'all-recorded-pids-absent-no-named-conflict');
    assert.equal(observed.summary_sha256, '995d942f82bf422e666ae8557f97f34c682e2489be6300464e0aed7ccfef3838');
    assert.equal(observed.query, 'Get-Process snapshot, no process modification or termination');
    assert.equal(observed.known.length, 26); assert.equal(observed.present.length, 0); assert.equal(observed.conflicts.length, 0);
    assert.equal(observed.normal_old_job_empty_claimed, false); assert.equal(observed.backend_started, false);
    assert.equal(new Set(observed.known.map(item => item.pid)).size, 26);
    const normalizeUtc = value => { assert.match(value, /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{1,7}Z$/); return value.replace(/\.(\d{1,7})Z$/, (_,fraction) => '.' + fraction.padEnd(7,'0') + 'Z'); };
    for (const item of observed.known) {
      const source = failed.owned_memory_profile.processes.filter(value => value.pid === item.pid);
      assert.equal(source.length, 1); assert.equal(item.role, source[0].role); assert.equal(item.name, source[0].name);
      assert.equal(normalizeUtc(item.creation_utc), normalizeUtc(source[0].creation_utc));
    }
    const source = path.join(root, 'analysis-runtime/rea/rea-ghidra-cf94a6b7868b84ea869e89d2a19dd997');
    const destination = path.join(root, 'analysis-runtime/retained/failed-r1-game-callback-core-cf94a6b7868b84ea869e89d2a19dd997');
    assert.equal(path.resolve(retained.source), source); assert.equal(path.resolve(retained.destination), destination);
    assert.ok(!fs.existsSync(source), 'Failed Game runtime still active');
    const retainedStat = fs.lstatSync(destination);
    assert.ok(retainedStat.isDirectory() && !retainedStat.isSymbolicLink(), 'Preserved Game runtime missing or linked');
    assert.ok(Array.isArray(checked.content_before) && Array.isArray(checked.content_after));
    assert.equal(checked.content_before.length, 69); assert.equal(canonical(checked.content_before), canonical(checked.content_after));
    const files = checked.content_before.filter(item => item.kind === 'file');
    assert.equal(files.length, 43); assert.equal(files.reduce((sum,item) => sum + item.bytes, 0), 226029208);
    for (const key of ['calls','target','address_sources','memory_profile','time_limits','monitor_revision','diagnostic_monitor_review']) assert.equal(canonical(plan[key]), canonical(checked.original_plan[key]), 'Retry changed original fixed scope or protection');
    for (const item of plan.calls.slice(3,21)) assert.ok(!fs.existsSync(path.join(root, 'docs/reverse-client/r1-game-callback-core', item.label + '.json')), 'Earlier Game callback body exists; review reuse instead of duplicate query');
  }
  if (r1CrowdVectorGrowthRetry) {
    const recovery = plan.recovery_from_failed_batch;
    // This retry alone uses the independently tested diagnostic-only deployment.
    assert.equal(recovery?.monitor_review?.status, 'passed-and-pinned');
    assert.equal(recovery.monitor_review.approved_source_sha256, plan.monitor_revision.sha256);
    assert.equal(recovery.monitor_review.validation_file, plan.monitor_revision.validation_file);
    assert.equal(recovery.monitor_review.validation_sha256, plan.monitor_revision.validation_sha256);
    for (const key of ['root_cause_proven','permissions_changed','wait_limit_changed','admission_checks_changed']) assert.equal(recovery.monitor_review[key], false);
    const monitorPins = [
      ['serializer_file', 'serializer_sha256', 'tools/static_analysis/rea_sampler_failure.ps1', 'dc1da620b889cd727e9eb2fe3eaea733fa565f31cd0007a6270d7543317c89a2'],
      ['sampler_file', 'sampler_sha256', 'tools/static_analysis/measure_job_process_memory.ps1', 'd2f95d1675ae6a79f72aa6f7620c18ef212c9b809f315628ff9abd184edd3e0d'],
      ['candidate_file', 'candidate_sha256', '.tooling/r1-owned-job-diagnostic-candidate/ReaOwnedJobDiagnosticCandidate.cs', 'b52e6ba242277ab864a431433f43efe671bc6c37e73c1fd94d8b0977b7400544'],
      ['validation_file', 'validation_sha256', '.tooling/r1-owned-job-diagnostic-validation/test-result.json', '8d7189e0d2db0a1f740b2894ca1cc364de3d854d3ad428c5bea5d5a53fa0f6c3'],
      ['serializer_validation_file', 'serializer_validation_sha256', '.tooling/r1-owned-job-diagnostic-validation/serializer/result.json', 'eb5dc1cd79621686a5b646ba03897cec638674dd1d6894ec69cee1d5ceb04a60'],
      ['runner_validation_file', 'runner_validation_sha256', '.tooling/r1-owned-job-diagnostic-validation/runner-result.json', '1ff5539479a0ba058bb7e3b4e6692293a07515e48d226c56f5b6667857c4bb29'],
      ['deployment_file', 'deployment_sha256', '.tooling/r1-owned-job-diagnostic-deploy/deployment.json', 'ef9be6d39c736f99fe4dd365b1ed08f6849f4280eacc698fdc9b4b75d4977dbf'],
    ];
    for (const [fileKey, hashKey, relative, fingerprint] of monitorPins) {
      assert.equal(plan.monitor_revision[fileKey], relative); assert.equal(plan.monitor_revision[hashKey], fingerprint);
      assert.equal(fileHash(path.join(root, relative)), fingerprint, 'Retry monitor/serializer validation pin changed');
    }
    const readMonitorProof = key => JSON.parse(fs.readFileSync(path.join(root, plan.monitor_revision[key]), 'utf8'));
    const fixture = readMonitorProof('validation_file'), serializer = readMonitorProof('serializer_validation_file');
    const validation = readMonitorProof('runner_validation_file'), deployed = readMonitorProof('deployment_file');
    assert.equal(fixture.status, 'passed-isolated-contract-tests'); assert.equal(fixture.assertions, 49); assert.equal(fixture.groups, 10);
    assert.equal(fixture.own_processes, 4); assert.equal(fixture.exits_confirmed, 4); assert.equal(fixture.failure_type, null);
    assert.equal(fixture.fault_injection, true); assert.equal(fixture.positive_wait_is_native, true);
    for (const key of ['production_modified','r0_reproduced','root_cause_proven']) assert.equal(fixture[key], false);
    assert.equal(serializer.status, 'passed'); assert.equal(serializer.assertions, 39); assert.equal(serializer.error, null);
    assert.equal(serializer.checks.length, 39); assert.ok(serializer.checks.every(item => item.passed === true));
    assert.equal(serializer.source_sha256.projection, plan.monitor_revision.serializer_sha256);
    assert.equal(validation.status, 'completed-isolated-diagnostic-tests');
    for (const key of ['candidate_compile_exit','tests_compile_exit','fixture_exit']) assert.equal(validation[key], 0);
    for (const key of ['candidate_compile_invocation_succeeded','tests_compile_invocation_succeeded','serializer_invocation_succeeded','serializer_passed','final_inputs_preserved','output_capture_complete']) assert.equal(validation[key], true);
    for (const key of ['original_read','backend_started','production_deployed']) assert.equal(validation[key], false);
    assert.equal(validation.failure_type, null); assert.equal(validation.fixture_secondary_errors.length, 0);
    assert.equal(validation.instrumentation.production_hook_added, false); assert.equal(validation.instrumentation.candidate_modified, false);
    assert.equal(validation.serializer_result_sha256, plan.monitor_revision.serializer_validation_sha256); assert.equal(validation.serializer_assertions, 39);
    const candidateInput = validation.inputs.filter(item => samePath(item.path, path.join(root, plan.monitor_revision.candidate_file)));
    assert.equal(candidateInput.length, 1); assert.equal(candidateInput[0].sha256, plan.monitor_revision.candidate_sha256);
    assert.equal(deployed.status, 'diagnostic-only-source-integrated');
    assert.equal(deployed.native_file, plan.monitor_revision.file); assert.equal(deployed.native_sha256, plan.monitor_revision.sha256);
    assert.equal(deployed.serializer_file, plan.monitor_revision.serializer_file); assert.equal(deployed.serializer_sha256, plan.monitor_revision.serializer_sha256);
    assert.equal(deployed.sampler_sha256, plan.monitor_revision.sampler_sha256);
    assert.equal(deployed.test_result_sha256, plan.monitor_revision.validation_sha256);
    assert.equal(deployed.serializer_test_sha256, plan.monitor_revision.serializer_validation_sha256);
    assert.equal(deployed.runner_result_sha256, plan.monitor_revision.runner_validation_sha256);
    for (const key of ['permissions_changed','wait_limit_changed','admission_checks_changed','backend_launched','root_cause_proven']) assert.equal(deployed[key], false);
    assert.equal(plan.runner_mode_registered, true);
    assert.equal(recovery.original_closed_session_verified, false);
    assert.equal(recovery.original_job_empty_confirmed, false);
    const pinned = [
      ['summary', 'docs/reverse-client/r1-crowd-vector-growth/summary.json', 'e58ed47f4ff779696016b9ea77626d8dad5957467f81363cf988c967c2454631'],
      ['failure', 'docs/reverse-client/r1-crowd-vector-growth/owned-memory.ndjson.failure.json', 'a5eadc1735b5fc5298a362c95fe6cafb8bb47009e174ed4a36d9b299dec7eea8'],
      ['runtime_before', '.tooling/r1-crowd-vector-growth-retry-prepared/runtime-before.json', '5353b05a1a977a289d304ab3d193986d7e2bfe372d12354cb987d3a1701219e6'],
      ['process_names', '.tooling/r1-crowd-vector-growth-retry-prepared/process-name-snapshot.json', '01abfc7124ff8072d8a42e719bd0e99f1e4718d1203ac538b7ba44fff25bcdb3'],
      ['rejected_cim', '.tooling/r1-crowd-vector-growth-retry-prepared/process-inventory.json', 'a2f45b4433d64b79f840962a9bc3e5622762df5293b0860d022ace01ffd9c10d'],
      ['content_before', '.tooling/r1-crowd-vector-growth-retry-prepared/runtime-content-before.json', '01c5457f805e6d73eaebf46c2a2128eeda0461a33861e23688b4ac01484dc9eb'],
      ['content_after', '.tooling/r1-crowd-vector-growth-retry-prepared/runtime-content-after.json', '01c5457f805e6d73eaebf46c2a2128eeda0461a33861e23688b4ac01484dc9eb'],
      ['quarantine', '.tooling/r1-crowd-vector-growth-retry-prepared/runtime-quarantine-result.json', '7f52b6c7351cbe990115254309238a6509543182868632a57c1c0e0eb055d273'],
    ];
    const checked = {};
    assert.equal(recovery.pinned_evidence.length, pinned.length);
    for (const [key, relative, fingerprint] of pinned) {
      const evidence = recovery.pinned_evidence.filter(item => item.key === key);
      assert.equal(evidence.length, 1);
      assert.equal(evidence[0].file, relative); assert.equal(evidence[0].sha256, fingerprint);
      assert.equal(fileHash(path.join(root, relative)), fingerprint, 'Growth retry recovery evidence changed');
      checked[key] = JSON.parse(fs.readFileSync(path.join(root, relative), 'utf8'));
    }
    const failed = checked.summary, failure = checked.failure, observed = checked.runtime_before;
    const retained = checked.quarantine, names = checked.process_names, rejected = checked.rejected_cim;
    assert.equal(failed.status, 'failed');
    assert.equal(failed.tool_calls, 4); assert.equal(failed.completed_plan_calls, 3);
    assert.equal(failed.closed_session_verified, false); assert.equal(failed.forced_termination, true);
    assert.equal(failed.process_exit_confirmed, true); assert.equal(failed.child_exit_code, 0);
    assert.equal(failed.owned_memory_profile.job_empty_confirmed, false);
    assert.equal(failed.owned_memory_profile.sampler_exit_confirmed, true);
    for (const key of ['sha256_before','sha256_after']) assert.equal(failed.target[key], targetHash);
    assert.equal(failure.probe_step, 'QueryFullProcessImageName'); assert.equal(failure.image_query_error_code, 5);
    assert.equal(failure.process_id, 18300); assert.equal(String(failure.creation_ticks), '639283025161805106');
    assert.equal(failure.in_job, true); assert.equal(failure.exit_confirmed_after_image_failure, false);
    assert.equal(observed.known_pids.length, 27);
    assert.ok(observed.known_pids.every(item => item.live === false));
    assert.equal(observed.live_known_count, 0);
    assert.equal(observed.owner_run, '171f8ae7-ddd2-4754-a8d8-ca3b8a64409f');
    assert.equal(observed.session_run, observed.owner_run);
    assert.equal(observed.owner_pid, 37488); assert.equal(observed.owner_parent, 25816);
    assert.equal(observed.ownership_kind, 'windows-job-object');
    assert.equal(observed.target_sha256, targetHash); assert.equal(observed.runtime_copy_sha256, targetHash);
    assert.equal(observed.raw04_file_exists, 0); assert.equal(observed.bridge_endpoint_exists, false);
    assert.equal(observed.normal_close_retroactively_claimed, false); assert.equal(observed.job_empty_retroactively_claimed, false);
    assert.equal(names.mechanism, 'Get-Process exact names; no command line claims');
    assert.equal(canonical(names.names), canonical(['java','javaw','decompile','node','ghidraRun','MSBuild','dotnet','UnrealEditor']));
    assert.equal(names.rows.length, 0); assert.equal(names.no_permission_change, true); assert.equal(names.processes_terminated, false);
    // The failed CIM inventory is preserved only as a failure, never a no-process observation.
    assert.equal(rejected.status, 'failed-cim-query'); assert.equal(rejected.completeness_confirmed, false);
    assert.equal(retained.status, 'quarantined-with-content-preserved');
    assert.equal(retained.recorded_pids_rechecked, 27); assert.equal(retained.recorded_pids_present, 0);
    assert.equal(retained.analysis_or_conflict_names_present, 0);
    assert.equal(retained.content_manifest_equal, true); assert.equal(retained.deleted_files, 0);
    assert.equal(retained.files, 49); assert.equal(retained.entries, 75); assert.equal(retained.bytes, 117828344);
    assert.equal(retained.active_runtime_roots, 0);
    assert.equal(retained.normal_mcp_close_retroactively_claimed, false); assert.equal(retained.old_job_empty_retroactively_claimed, false);
    assert.equal(retained.monitor_changed, false); assert.equal(retained.backend_started, false);
    const source = path.join(root, 'analysis-runtime/rea/rea-ghidra-3f638a53485fa8f0a86b2d3f67878f03');
    const destination = path.join(root, 'analysis-runtime/retained/failed-r1-crowd-vector-growth-3f638a53485fa8f0a86b2d3f67878f03');
    assert.equal(path.resolve(retained.source), source); assert.equal(path.resolve(retained.destination), destination);
    assert.ok(!fs.existsSync(source), 'Failed runtime still active');
    const retainedStat = fs.lstatSync(destination);
    assert.ok(retainedStat.isDirectory() && !retainedStat.isSymbolicLink(), 'Retained runtime missing or linked');
    assert.ok(Array.isArray(checked.content_before) && Array.isArray(checked.content_after));
    assert.equal(checked.content_before.length, 75);
    assert.equal(canonical(checked.content_before), canonical(checked.content_after));
    for (const item of plan.calls.slice(3, 9)) assert.ok(!fs.existsSync(path.join(root, 'docs/reverse-client/r1-crowd-vector-growth', item.label + '.json')), 'Prior growth body response exists; review reuse');
  }
  if (r1ReachedGoalDependencies || r1WorldPositionLevelInitialization || r1TerrainTypeMaterial || r1WaterInitialization || r1GroundResourcesWater || r1GroundFaceUnlink || r1GroundIntersectionRemaining || r1GroundIntersectionDirect || r1GroundRayTree || r1GroundFloorGeometry || r1GroundBlocksNodeDestroy || r1SpatialGroundDirect || r1EngineSpatialGround || r1EngineEntityPublicationCore || r1SameRegionMovement || r1PositionPublication || r1ActorRadius || r1CrowdPointCopy || r1CrowdPointReallocation || r1CrowdDirectAlgorithms || r1EngineCallbackGetters || r1CrowdMotionBranches || r1SkillMovedDirect || r1GameLevelPublicationTail || r1GameEntityPublicationGates || r1GameMovementMath || r1GameCallbackRemainingRetry || r1GameCallbackRemaining || r1GameCallbackCore || r1GameMovementInterface || r1GameEntityConstructors) {
    const recovery = {monitor_review: plan.diagnostic_monitor_review};
    // These explicitly registered modes use the same strictly pinned diagnostic-only deployment.
    assert.equal(recovery?.monitor_review?.status, 'passed-and-pinned');
    assert.equal(recovery.monitor_review.approved_source_sha256, plan.monitor_revision.sha256);
    assert.equal(recovery.monitor_review.validation_file, plan.monitor_revision.validation_file);
    assert.equal(recovery.monitor_review.validation_sha256, plan.monitor_revision.validation_sha256);
    for (const key of ['root_cause_proven','permissions_changed','wait_limit_changed','admission_checks_changed']) assert.equal(recovery.monitor_review[key], false);
    const monitorPins = [
      ['serializer_file', 'serializer_sha256', 'tools/static_analysis/rea_sampler_failure.ps1', 'dc1da620b889cd727e9eb2fe3eaea733fa565f31cd0007a6270d7543317c89a2'],
      ['sampler_file', 'sampler_sha256', 'tools/static_analysis/measure_job_process_memory.ps1', postRestartObservationMode ? '3916ee1f49eddb5f701cce1764dddeaf944badc61fe833ff469600ee58b6d6ca' : 'd2f95d1675ae6a79f72aa6f7620c18ef212c9b809f315628ff9abd184edd3e0d'],
      ['candidate_file', 'candidate_sha256', '.tooling/r1-owned-job-diagnostic-candidate/ReaOwnedJobDiagnosticCandidate.cs', 'b52e6ba242277ab864a431433f43efe671bc6c37e73c1fd94d8b0977b7400544'],
      ['validation_file', 'validation_sha256', '.tooling/r1-owned-job-diagnostic-validation/test-result.json', '8d7189e0d2db0a1f740b2894ca1cc364de3d854d3ad428c5bea5d5a53fa0f6c3'],
      ['serializer_validation_file', 'serializer_validation_sha256', '.tooling/r1-owned-job-diagnostic-validation/serializer/result.json', 'eb5dc1cd79621686a5b646ba03897cec638674dd1d6894ec69cee1d5ceb04a60'],
      ['runner_validation_file', 'runner_validation_sha256', '.tooling/r1-owned-job-diagnostic-validation/runner-result.json', '1ff5539479a0ba058bb7e3b4e6692293a07515e48d226c56f5b6667857c4bb29'],
      ['deployment_file', 'deployment_sha256', '.tooling/r1-owned-job-diagnostic-deploy/deployment.json', 'ef9be6d39c736f99fe4dd365b1ed08f6849f4280eacc698fdc9b4b75d4977dbf'],
    ];
    for (const [fileKey, hashKey, relative, fingerprint] of monitorPins) {
      assert.equal(plan.monitor_revision[fileKey], relative); assert.equal(plan.monitor_revision[hashKey], fingerprint);
      assert.equal(fileHash(path.join(root, relative)), fingerprint, 'Retry monitor/serializer validation pin changed');
    }
    const readMonitorProof = key => JSON.parse(fs.readFileSync(path.join(root, plan.monitor_revision[key]), 'utf8'));
    const fixture = readMonitorProof('validation_file'), serializer = readMonitorProof('serializer_validation_file');
    const validation = readMonitorProof('runner_validation_file'), deployed = readMonitorProof('deployment_file');
    assert.equal(fixture.status, 'passed-isolated-contract-tests'); assert.equal(fixture.assertions, 49); assert.equal(fixture.groups, 10);
    assert.equal(fixture.own_processes, 4); assert.equal(fixture.exits_confirmed, 4); assert.equal(fixture.failure_type, null);
    assert.equal(fixture.fault_injection, true); assert.equal(fixture.positive_wait_is_native, true);
    for (const key of ['production_modified','r0_reproduced','root_cause_proven']) assert.equal(fixture[key], false);
    assert.equal(serializer.status, 'passed'); assert.equal(serializer.assertions, 39); assert.equal(serializer.error, null);
    assert.equal(serializer.checks.length, 39); assert.ok(serializer.checks.every(item => item.passed === true));
    assert.equal(serializer.source_sha256.projection, plan.monitor_revision.serializer_sha256);
    assert.equal(validation.status, 'completed-isolated-diagnostic-tests');
    for (const key of ['candidate_compile_exit','tests_compile_exit','fixture_exit']) assert.equal(validation[key], 0);
    for (const key of ['candidate_compile_invocation_succeeded','tests_compile_invocation_succeeded','serializer_invocation_succeeded','serializer_passed','final_inputs_preserved','output_capture_complete']) assert.equal(validation[key], true);
    for (const key of ['original_read','backend_started','production_deployed']) assert.equal(validation[key], false);
    assert.equal(validation.failure_type, null); assert.equal(validation.fixture_secondary_errors.length, 0);
    assert.equal(validation.instrumentation.production_hook_added, false); assert.equal(validation.instrumentation.candidate_modified, false);
    assert.equal(validation.serializer_result_sha256, plan.monitor_revision.serializer_validation_sha256); assert.equal(validation.serializer_assertions, 39);
    const candidateInput = validation.inputs.filter(item => samePath(item.path, path.join(root, plan.monitor_revision.candidate_file)));
    assert.equal(candidateInput.length, 1); assert.equal(candidateInput[0].sha256, plan.monitor_revision.candidate_sha256);
    assert.equal(deployed.status, 'diagnostic-only-source-integrated');
    assert.equal(deployed.native_file, plan.monitor_revision.file); assert.equal(deployed.native_sha256, plan.monitor_revision.sha256);
    assert.equal(deployed.serializer_file, plan.monitor_revision.serializer_file); assert.equal(deployed.serializer_sha256, plan.monitor_revision.serializer_sha256);
    assert.equal(deployed.sampler_sha256, postRestartObservationMode ? 'd2f95d1675ae6a79f72aa6f7620c18ef212c9b809f315628ff9abd184edd3e0d' : plan.monitor_revision.sampler_sha256);
    assert.equal(deployed.test_result_sha256, plan.monitor_revision.validation_sha256);
    assert.equal(deployed.serializer_test_sha256, plan.monitor_revision.serializer_validation_sha256);
    assert.equal(deployed.runner_result_sha256, plan.monitor_revision.runner_validation_sha256);
    for (const key of ['permissions_changed','wait_limit_changed','admission_checks_changed','backend_launched','root_cause_proven']) assert.equal(deployed[key], false);
    assert.equal(plan.runner_mode_registered, true);
  }
  if (postRestartObservationMode) {
    // Recovery is a separately recorded inference, never a rewritten terminal summary.
    const proofPins = [
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/runtime-quarantine-result.json",
        "sha256": "3719d9efdf8719d5575f870704e6c28edd831d611ab28eed93a84441479fc829"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/process-before-inventory.json",
        "sha256": "e9baee1d751ebe2dbc329ceb068f25fc33ee96d3474e77bfd0a84a6978ca3dcf"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/process-before-move.json",
        "sha256": "34967759d2622ffe3f46942681014a903ebf626f7f9557a0633f0b8c49beb272"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/process-after-move.json",
        "sha256": "b2500ac78ffab31fc94c6e19a6db7c3287401bbf40a6b3e7055b5bf4e1a0c8e8"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/runtime-content-before.json",
        "sha256": "e2f9d06b75bc26f6418b21bc3e68a6983c22b59324d93dc9d2914f18784c3b20"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/runtime-content-before-move.json",
        "sha256": "e2f9d06b75bc26f6418b21bc3e68a6983c22b59324d93dc9d2914f18784c3b20"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/runtime-content-after.json",
        "sha256": "e2f9d06b75bc26f6418b21bc3e68a6983c22b59324d93dc9d2914f18784c3b20"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/runtime-content-final.json",
        "sha256": "e2f9d06b75bc26f6418b21bc3e68a6983c22b59324d93dc9d2914f18784c3b20"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/historical-identities.json",
        "sha256": "98c290f49b3b071a4127529aa21c7bbf816a6b59596f9be210adc0fecd302d45"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/classification-test-result.json",
        "sha256": "0158c21bfa6fa46d87656030888cce9e8853b18086547599a6ce668527d80554"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/retain_interrupted_runtime_post_restart.ps1",
        "sha256": "0769a1fbcf8c44b12a09fe35a31823a6a75eee2d24494814d1ae4334d15b32a9"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/post_restart_classification.ps1",
        "sha256": "c5fb0a7b207f5c6e0fdd4d55b624aef69df4619c758034c59ca1a5645cf534f2"
    },
    {
        "file": ".tooling/r1-game-callback-remaining-post-restart-prepared/test_post_restart_classification.ps1",
        "sha256": "bb88bbf79eb8c61b023d6a7d1468b40af09d4007433a3f111c832235e89fe611"
    }
];
    assert.equal(canonical(plan.recovery_requirement.evidence), canonical(proofPins));
    const proofs = {};
    for (const item of proofPins) {
      assert.equal(fileHash(path.join(root,item.file)),item.sha256,'Recovery evidence changed');
      if (item.file.endsWith('.json')) proofs[path.basename(item.file)] = JSON.parse(fs.readFileSync(path.join(root,item.file),'utf8'));
    }
    const retained = proofs['runtime-quarantine-result.json'];
    assert.equal(retained.status,'interrupted-runtime-retained-with-content-preserved');
    for (const key of ['content_manifest_equal','process_snapshot_complete','single_owner_job_termination_inferred','old_summary_missing']) assert.equal(retained[key],true);
    for (const key of ['deleted_files','active_runtime_roots','unresolved_recovery_blockers','body_responses']) assert.equal(retained[key],0);
    for (const key of ['summary_created','normal_close_retroactively_claimed','old_job_empty_retroactively_claimed','sampler_terminal_exit_retroactively_claimed','monitor_changed','wait_policy_changed','backend_started','wall_clock_ordering_used']) assert.equal(retained[key],false);
    assert.equal(retained.files,120); assert.equal(retained.entries,152); assert.equal(retained.bytes,303049271);
    assert.equal(retained.completed_lifecycle_calls,3); assert.equal(retained.unreturned_call,4);
    const source = path.join(root,'analysis-runtime/rea/rea-ghidra-8038465c1a920f04ed2efb88a70891ee');
    const destination = path.join(root,'analysis-runtime/retained/interrupted-r1-game-callback-remaining-8038465c1a920f04ed2efb88a70891ee');
    assert.equal(path.resolve(retained.source),source); assert.equal(path.resolve(retained.destination),destination);
    assert.ok(!fs.existsSync(source),'Interrupted runtime remains active');
    const dstStat=fs.lstatSync(destination); assert.ok(dstStat.isDirectory() && !dstStat.isSymbolicLink());
    const first=proofs['runtime-content-before.json'];
    for(const key of ['runtime-content-before-move.json','runtime-content-after.json','runtime-content-final.json']) assert.equal(canonical(proofs[key]),canonical(first));
    assert.equal(first.length,152);assert.equal(first.filter(x=>x.kind==='file').length,120);
    assert.equal(first.reduce((sum,item)=>sum+item.bytes,0),303049271);
    for(const name of ['process-before-inventory.json','process-before-move.json','process-after-move.json']){
      const current=proofs[name]; assert.equal(current.snapshot_complete,true); assert.equal(current.snapshot_count,current.rows.length);
      assert.equal(current.classification.classification_allows_retention,true);assert.equal(current.classification.single_owner_job_termination_inferred,true);
      assert.ok(!current.rows.some(row=>[37452,12968].includes(row.pid)));
      assert.equal(current.classification.old_normal_close_claimed,false);assert.equal(current.classification.old_job_empty_claimed,false);
    }
    const interrupted={"observation_file":"docs/reverse-client/r1-game-callback-remaining/interruption-observation.json","observation_sha256":"511bd170444f4cbf1792ae4b1afe1dfce8803f42a6fd60fd2ff33df2cba35821","progress_file":"docs/reverse-client/r1-game-callback-remaining/progress.ndjson","progress_sha256":"e08685c71b2a4e9f1f1f2d91296bd36a18793d715cfbafe3965933648d5b3551","owned_memory_file":"docs/reverse-client/r1-game-callback-remaining/owned-memory.ndjson","owned_memory_sha256":"e7557dd93b4dd09aab2f873148d379b4352db15e3264286f528cb68bd6b81005","memory_file":"docs/reverse-client/r1-game-callback-remaining/memory.ndjson","memory_sha256":"6618227f66e844250b1b105455a00a1ceb4e206f49207bddf70b3e7243f7b6c2","saved_plan_file":"docs/reverse-client/r1-game-callback-remaining/plan.json","saved_plan_sha256":"6e4b004f03636d96fea0561e62382a5b2159dea4c806e9d101e51b1fecd0205c","runtime":"analysis-runtime/rea/rea-ghidra-8038465c1a920f04ed2efb88a70891ee","completed_lifecycle_calls":3,"unreturned_call":4,"unreturned_selector":"0x18005b730","unreturned_response_status":"unknown-no-response-file","body_response_count":0,"summary_file":"docs/reverse-client/r1-game-callback-remaining/summary.json","summary_must_be_absent":true,"normal_close_confirmed":false,"job_empty_terminal_record":false,"sampler_exit_terminal_record":false,"execution_channel_error":"Unknown process id 46200","failure_cause":"unknown","original_observation_process_absence_is_not_authoritative":true,"limitation":"The early observation used an incomplete sandbox Get-Process view. Later formal recovery evidence must independently establish current quiescence; no old terminal outcome is reconstructed."};
    if(r1GameCallbackRemainingRetry) assert.equal(canonical(plan.interrupted_attempt),canonical(interrupted));
    for(const key of ['observation','progress','owned_memory','memory','saved_plan']) assert.equal(fileHash(path.join(root,interrupted[key+'_file'])),interrupted[key+'_sha256']);
    assert.ok(!fs.existsSync(path.join(root,interrupted.summary_file)),'Original interrupted summary now exists; inspect outcome');
    const oldBodyNames=fs.readdirSync(path.join(root,'docs/reverse-client/r1-game-callback-remaining')).filter(name=>/^\d+-GameCallbackRemaining-.*-procedure_(pseudo_code|assembly|callees)\.json$/.test(name));
    assert.equal(oldBodyNames.length,0,'Original call response appeared; inspect reuse');
    const reused=[{"file":"docs/reverse-client/r1-game-callback-core-retry-1/04-GameCallbackCore-0x180019d20-procedure_pseudo_code.json","sha256":"7e6def90d8d958d0f9a443a68f7361ba1ee824f3e694c5f4cf1afe6d7917e165","tool":"procedure_pseudo_code","requery":false},{"file":"docs/reverse-client/r1-game-callback-core-retry-1/05-GameCallbackCore-0x180019d20-procedure_assembly.json","sha256":"436ace29503473d02de31739c5d46488c94008e652c7df6e6eb8850b625e1003","tool":"procedure_assembly","requery":false},{"file":"docs/reverse-client/r1-game-callback-core-retry-1/06-GameCallbackCore-0x180019d20-procedure_callees.json","sha256":"1775799672ddc3ed21e53d0482c784401a97ec49f8e23b9f1863f68d816f2f92","tool":"procedure_callees","requery":false},{"file":"docs/reverse-client/r1-game-callback-core-retry-1/07-GameCallbackCore-0x180016d10-procedure_pseudo_code.json","sha256":"518ecd547b96fb71976a6b4bd88ddb502b9f26bba5138f700a8c28e62d66f185","tool":"procedure_pseudo_code","requery":false},{"file":"docs/reverse-client/r1-game-callback-core-retry-1/08-GameCallbackCore-0x180016d10-procedure_assembly.json","sha256":"64be4eea501132759edeb5eadc47e46902d27f6269031d86a783412841895d09","tool":"procedure_assembly","requery":false},{"file":"docs/reverse-client/r1-game-callback-core-retry-1/09-GameCallbackCore-0x180016d10-procedure_callees.json","sha256":"cf7cf6a8914ba8dd06c2b31bab7cd8e175266b639d8742c9e62026bcf0876f6f","tool":"procedure_callees","requery":false}];
    if(r1GameCallbackRemainingRetry) assert.equal(canonical(plan.completed_work_reused),canonical(reused));
    for(const item of reused) assert.equal(fileHash(path.join(root,item.file)),item.sha256);
    const samplerPins=[
    {
        "file": ".tooling/r1-member-identity-serialization-deploy/deployment.json",
        "sha256": "e978a7fbb0256233a2a5bbdacec662ebcbe808c1c227d20ef216dce8cf3d0db9"
    },
    {
        "file": ".tooling/r1-member-identity-serialization-deploy/measure_job_process_memory.before.ps1",
        "sha256": "d2f95d1675ae6a79f72aa6f7620c18ef212c9b809f315628ff9abd184edd3e0d"
    },
    {
        "file": ".tooling/r1-member-identity-serialization-candidate/measure_job_process_memory.ps1",
        "sha256": "3916ee1f49eddb5f701cce1764dddeaf944badc61fe833ff469600ee58b6d6ca"
    },
    {
        "file": ".tooling/r1-member-identity-serialization-candidate/test_member_identity_serialization.ps1",
        "sha256": "3afa73f7130a87959e38e76ba313f715bd0e4448e146a0417b3163b55ac84ae2"
    },
    {
        "file": ".tooling/r1-member-identity-serialization-candidate/test-result.json",
        "sha256": "812f576f55ce8989638accf2a668a6b1b19e0d23e2869328be63ba7822c3062d"
    }
];
    assert.equal(canonical(plan.sampler_observation_deployment.evidence),canonical(samplerPins));
    for(const item of samplerPins) assert.equal(fileHash(path.join(root,item.file)),item.sha256,'Additive sampler evidence changed');
    const deployment=JSON.parse(fs.readFileSync(path.join(root,samplerPins[0].file),'utf8'));
    assert.equal(deployment.status,'additive-member-identity-serialization-integrated');
    assert.equal(deployment.previous_sha256,'d2f95d1675ae6a79f72aa6f7620c18ef212c9b809f315628ff9abd184edd3e0d');assert.equal(deployment.sha256,'3916ee1f49eddb5f701cce1764dddeaf944badc61fe833ff469600ee58b6d6ca');
    assert.equal(deployment.exact_removal_restores_prior_source,true);assert.equal(deployment.only_added_field,'membership_queries.after_counters_member_identities');
    for(const key of ['tests_rerun','monitor_changed','permissions_changed','wait_limit_changed','admission_checks_changed','resource_limits_changed','backend_launched'])assert.equal(deployment[key],false);
    const projectionTest=JSON.parse(fs.readFileSync(path.join(root,deployment.test_result_file),'utf8'));
    assert.equal(projectionTest.status,'passed');assert.equal(projectionTest.exit_code,0);assert.equal(projectionTest.script_sha256,deployment.test_sha256);
    for(const key of ['live_process_queries','production_sampler_executed','production_changed','candidate_deployed','old_suites_rerun'])assert.equal(projectionTest[key],false);
    assert.equal(fileHash(path.join(root,'tools/static_analysis/measure_job_process_memory.ps1')),'3916ee1f49eddb5f701cce1764dddeaf944badc61fe833ff469600ee58b6d6ca');
    assert.equal(plan.runner_mode_registered,true);
  }
  if (r1WorldPositionLevelInitialization || r1SkillMovedDirect) {
    const priorFile='docs/reverse-client/r1-terrain-type-material/summary.json', priorHash='52c5d21236f0c854324d3bdd0b467ef5c1512294152d6f98ebac5a3a64b9ba02';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.required_status,'passed');assert.equal(plan.prior_batch_cleanup.completed_plan_calls,9);assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,9);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1TerrainTypeMaterial) {
    const priorFile='docs/reverse-client/r1-water-initialization/summary.json', priorHash='d8d743a85a277c989d2977a3c7228e2fa0da096b002f3c7e0d53b3be36e175dd';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.required_status,'passed');assert.equal(plan.prior_batch_cleanup.completed_plan_calls,11);assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,11);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1WaterInitialization) {
    const priorFile='docs/reverse-client/r1-ground-resources-and-water/summary.json', priorHash='4b1442d60a79bc05c1fb29969ae257a2aea574763a8d5716a7a956fee748a72e';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.required_status,'passed');assert.equal(plan.prior_batch_cleanup.completed_plan_calls,17);assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,17);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1GroundResourcesWater) {
    const priorFile='docs/reverse-client/r1-ground-face-and-unlink/summary.json', priorHash='556d843ff2bbf7851f31be0107fbbfa5c37bb8efa46f3100af3de25c560dee19';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.required_status,'passed');assert.equal(plan.prior_batch_cleanup.completed_plan_calls,17);assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,17);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1GroundFaceUnlink) {
    const priorFile='docs/reverse-client/r1-ground-intersection-remaining/summary.json', priorHash='07f285ca456414b7c5c460291edebc4528b94a1e347bfbdbbf4c23b13019e883';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.required_status,'passed');assert.equal(plan.prior_batch_cleanup.completed_plan_calls,26);assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,26);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1GroundIntersectionRemaining) {
    const priorFile='docs/reverse-client/r1-ground-intersection-direct/summary.json', priorHash='f225eb802df82ac96bcaf20b1535bf45e2eecc932e9112876feadfb6e9a92b1b';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.required_status,'failed');assert.equal(plan.prior_batch_cleanup.completed_plan_calls,6);assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'failed');assert.equal(prior.completed_plan_calls,6);assert.equal(prior.tool_calls,9);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
    assert.equal(plan.prior_input_failure.file,'docs/reverse-client/r1-ground-intersection-direct/07-GroundIntersectionDirect-0x1801de850-procedure_pseudo_code.json');assert.equal(plan.prior_input_failure.sha256,'7916cd78076ac681958d68e14d2f87333976e343c5f54fc3ec480d6145c507ae');
    assert.equal(fileHash(path.join(root,plan.prior_input_failure.file)),plan.prior_input_failure.sha256);
    assert.equal(canonical(plan.reused_responses),canonical([{"file":"docs/reverse-client/r1-ground-intersection-direct/04-GroundIntersectionDirect-0x1801df220-procedure_pseudo_code.json","sha256":"ec0f31d6dac294b61b9b5112c8a47eb818ab5c9fabe28aa2c4a178d80eb9b8f2"},{"file":"docs/reverse-client/r1-ground-intersection-direct/05-GroundIntersectionDirect-0x1801df220-procedure_assembly.json","sha256":"4d4ae9e452737e215850afe7dc31a2133ac38b0bb151a307fdb1b21140447e5c"},{"file":"docs/reverse-client/r1-ground-intersection-direct/06-GroundIntersectionDirect-0x1801df220-procedure_callees.json","sha256":"03295ca3b21729fa29efd1b23d1bd90819e09f5a2405308ddf9b5f2874d4bd8c"}]));
    for(const proof of plan.reused_responses)assert.equal(fileHash(path.join(root,proof.file)),proof.sha256);
  }
  if (r1GroundIntersectionDirect) {
    const priorFile='docs/reverse-client/r1-ground-ray-tree/summary.json', priorHash='0500da9491aa4ac0ef5ea16bd2a920aa5d542e41aae27636c3663504c1388ff7';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,26);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1GroundRayTree) {
    const priorFile='docs/reverse-client/r1-ground-floor-geometry/summary.json', priorHash='f0ad572426d34f1f5b21a848dbdee54020a1db3747a78cbdc259e26ec896663c';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,14);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1GroundFloorGeometry) {
    const priorFile='docs/reverse-client/r1-ground-blocks-and-node-destroy/summary.json', priorHash='2e9097e2facbb665430fb8f77beb0ac38e373a12da296f9ac5c6024a639c63ff';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,17);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1GroundBlocksNodeDestroy) {
    const priorFile='docs/reverse-client/r1-spatial-ground-direct/summary.json', priorHash='745705185e62c4969194cb5c6ec78dede1275fadfe06c5e10c7e6b5f4351a90e';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,14);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1SpatialGroundDirect) {
    const priorFile='docs/reverse-client/r1-engine-spatial-ground/summary.json', priorHash='1c7c9a62f71ad344b71d89208b0a1495b8ccf44933629a0de0353adf330c5215';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,20);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1EngineSpatialGround) {
    const priorFile='docs/reverse-client/r1-game-level-publication-tail/summary.json', priorHash='4c269cb791f865f70be615c352dfe6c88eefef519dfa67924f7c3d0ca36a400b';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,11);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2');
  }
  if (r1EngineEntityPublicationCore) {
    const priorFile='docs/reverse-client/r1-game-entity-publication-gates/summary.json', priorHash='3e1d3b25f3031b3fdbcb3ac8419d855502784d3accf68b633fe63fe2ad8132b3';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.required_status,'failed');assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'failed');assert.equal(prior.completed_plan_calls,9);assert.equal(prior.tool_calls,12);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2');
    const failedFile='docs/reverse-client/r1-game-entity-publication-gates/10-EntityPublicationGate-0x1805d9ff2-procedure_pseudo_code.json', failedHash='7e71f844bbbc1aa2ed48945ebfac5d764298946fd2695a58da1530c3f006f6c6';
    assert.equal(plan.prior_batch_cleanup.failed_response_file,failedFile);assert.equal(plan.prior_batch_cleanup.failed_response_sha256,failedHash);assert.equal(fileHash(path.join(root,failedFile)),failedHash);
    const response=JSON.parse(fs.readFileSync(path.join(root,failedFile),'utf8'));assert.equal(response.result.isError,true);
    const error=JSON.parse(response.result.content.find(item=>item.type==='text').text).error;
    assert.equal(error.code,'invalid_request');assert.equal(error.category,'invalid_input');assert.equal(error.details.operation,'procedure_pseudo_code');
    assert.ok(error.details.issues.some(item=>item.message.includes('File offset 0x1805d9ff2 has no loaded source mapping')));
    assert.equal(plan.reused_game_results.length,6);for(const item of plan.reused_game_results){assert.equal(fileHash(path.join(root,item.file)),item.sha256);const body=JSON.parse(fs.readFileSync(path.join(root,item.file),'utf8'));assert.notEqual(body.result.isError,true);}
  }
  if (r1SameRegionMovement) {
    const priorFile='docs/reverse-client/r1-position-publication/summary.json', priorHash='cca1bb3618172f018173050b940f65bbeb56afeab66013eceeacefe5c928d2f1';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);
    assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,11);
    assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1PositionPublication) {
    const priorFile='docs/reverse-client/r1-game-movement-math/summary.json', priorHash='f2824305c00a8807be3273eac11359ba5b789180d4ef3680fe100ee8cc76669d';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);
    assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,14);
    assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2');
  }
  if (r1ActorRadius) {
    const priorFile='docs/reverse-client/r1-game-callback-remaining-retry-1/summary.json';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);
    assert.equal(plan.prior_batch_cleanup.observed,true,'Engine radius remains gated on the actual immediately preceding Game retry');
    assert.match(plan.prior_batch_cleanup.summary_sha256,/^[a-f0-9]{64}$/);
    assert.equal(fileHash(path.join(root,priorFile)),plan.prior_batch_cleanup.summary_sha256);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,14);
    assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'cd7ecd7942d719f96b42aff4896f8c25d52152b4a41191f71a4a66ea48cecad2');
  }
  if (r1GameLevelPublicationTail) {
    const priorFile='docs/reverse-client/r1-engine-entity-publication-core/summary.json', priorHash='762137cc388110d1ca7d9a03c1ea396fb09df460c41b2e711017b2bee93b13e6';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,17);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1GameEntityPublicationGates) {
    const priorFile='docs/reverse-client/r1-same-region-movement/summary.json', priorHash='1266e603157f4ac42b7f8cb164daa057de79e96c40b95f029b94bbd4336ce3b2';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile);assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(plan.prior_batch_cleanup.refresh_required_before_registration,false);assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,11);assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after'])assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (r1GameMovementMath) {
    const priorFile='docs/reverse-client/r1-actor-radius/summary.json', priorHash='799e5728ca5c792bab078bf361cb952fdae1827ba103bfbd15b40be61f78c2de';
    assert.equal(plan.prior_batch_cleanup.summary_file,priorFile); assert.equal(plan.prior_batch_cleanup.summary_sha256,priorHash);assert.equal(plan.prior_batch_cleanup.observed,true);
    assert.equal(fileHash(path.join(root,priorFile)),priorHash);
    const prior=JSON.parse(fs.readFileSync(path.join(root,priorFile),'utf8'));
    assert.equal(prior.status,'passed');assert.equal(prior.completed_plan_calls,8);
    assert.equal(prior.closed_session_verified,true);assert.equal(prior.process_exit_confirmed,true);
    assert.equal(prior.owned_memory_profile.job_empty_confirmed,true);assert.equal(prior.owned_memory_profile.sampler_exit_confirmed,true);
    for(const key of ['sha256_before','sha256_after']) assert.equal(prior.target[key],'4c2ea14ce1c591b315cc29a8a96819a5c3c7e194adbd6f7eea208b9bb8a93153');
  }
  if (verifyParserPlanOnly) {
    console.log(JSON.stringify({status: 'offline-plan-verified', plan_sha256: expectedPlanHash, ...(r1ReachedGoalDependencies ? {runtime_profile:batch.runtimeProfile,output_directory:path.relative(root,output),effective_launch_gib:launchBytes/gib,effective_stop_gib:stopBytes/gib} : {}), budget, tool_calls_planned: maxToolCalls, address_sources_verified: plan.address_sources.length, saved_schema_args_verified: true, original_binary_read: false, backend_launched: false, live_preflight_run: false, current_mcp_schema_check_deferred_until_authorized_execution: true}, null, 2));
    return;
  }
  if (parserRetry) {
    const prior = JSON.parse(fs.readFileSync(path.join(root, 'docs/object-lifecycle/rea-binary-parser-entry/summary.json'), 'utf8'));
    const cleanup = JSON.parse(fs.readFileSync(path.join(root, 'docs/object-lifecycle/rea-binary-parser-entry/runtime-cleanup.json'), 'utf8').replace(/^\uFEFF/, ''));
    assert.equal(prior.status, 'failed');
    assert.equal(prior.tool_calls, 4);
    assert.equal(prior.completed_plan_calls, 3);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(cleanup.removed, true);
    assert.equal(cleanup.remaining_runtime_files, 0);
    assert.ok(!fs.existsSync(path.join(root, 'docs/object-lifecycle/rea-binary-parser-entry/04-Binary-parser-entry-0x18013f510-pseudocode.json')), 'Unexpected prior parser result; review reuse before retry');
  }
  if (parserRetryAfterExitRace) {
    const priorDirectory = path.join(root, 'docs/object-lifecycle/rea-binary-parser-entry-retry-1');
    const prior = JSON.parse(fs.readFileSync(path.join(priorDirectory, 'summary.json'), 'utf8'));
    const release = JSON.parse(fs.readFileSync(path.join(priorDirectory, 'resource-release.json'), 'utf8').replace(/^\uFEFF/, ''));
    assert.equal(prior.status, 'failed');
    assert.equal(prior.tool_calls, 4);
    assert.equal(prior.completed_plan_calls, 3);
    assert.equal(prior.process_exit_confirmed, true);
    assert.equal(prior.owned_memory_profile.sampler_exit_confirmed, true);
    assert.equal(release.remaining_same_identity_count, 0);
    assert.equal(release.active_analysis_process_count, 0);
    assert.equal(release.remaining_runtime_files, 0);
    assert.ok(!fs.existsSync(path.join(priorDirectory, '04-Binary-parser-entry-0x18013f510-pseudocode.json')), 'Unexpected prior parser result; review reuse before retry');
  }
  if (ownedMemoryMode) {
    const runtimeBase = path.join(root, 'analysis-runtime/rea');
    assert.ok(!fs.readdirSync(runtimeBase, {withFileTypes: true}).some(item => /^rea-ghidra-/.test(item.name)), 'Previous runtime still exists; inspect before launch');
  }
  assert.equal(fileHash(targetPath), targetHash, 'Target input differs from the approved fingerprint');
  const launchSnapshot = ownedMemoryMode ? readHostResourceSnapshot(root) : null;
  if (launchSnapshot) checkLaunch(launchSnapshot);
  fs.mkdirSync(output); // Refuse an existing evidence directory, including an empty one.
  if (r1ReachedGoalDependencies) {
    fs.copyFileSync(planPath,path.join(output,'plan.json'),fs.constants.COPYFILE_EXCL);
    save('runtime-plan',plan);
  } else save('plan', plan);
  if (launchSnapshot) save('launch-preflight', launchSnapshot);
  const started = new Date().toISOString();
  const records = [], errors = [];
  let toolCalls = 0, completed = 0, closeNeeded = false, closeAttempted = false, closeAcknowledged = false, closedVerified = false;
  let finalHash = null, stderr = '', stderrTruncated = false, buffer = '', sequence = 0, transportError = null;
  let childExited = false, childExitCode = null, childSignal = null, forcedStop = false, exitConfirmed = false;
  const pending = new Map();
  const env = {};
  for (const key of ['SystemRoot', 'WINDIR', 'ComSpec', 'USERPROFILE', 'APPDATA', 'LOCALAPPDATA', 'PROCESSOR_ARCHITECTURE', 'PATH']) {
    if (process.env[key]) env[key] = process.env[key];
  }
  if (memoryGuardEnabled) assert.ok(memoryAllowed(os.freemem(), true), `Less than ${launchBytes / gib} GiB free physical memory immediately before spawn`);
  if (launchSnapshot) checkLaunch(launchSnapshot); // Recheck freshness immediately before Rea.
  const ownedNonce = ownedMemoryMode ? randomUUID() : null;
  const child = spawn(process.execPath, [wrapper, ...(ownedMemoryMode ? [`--owned-guard=${ownedNonce}`] : [])], {cwd: root, env, windowsHide: true, stdio: ownedMemoryMode ? ['pipe', 'pipe', 'pipe', 'ipc'] : ['pipe', 'pipe', 'pipe']});
  const gatedReady = ownedMemoryMode ? new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(Error('Gated Rea wrapper did not identify itself')), 10000);
    child.once('message', message => {
      clearTimeout(timer);
      if (message?.type !== 'owned-start-waiting' || message?.nonce !== ownedNonce || message?.pid !== child.pid) reject(Error('Gated wrapper identity mismatch'));
      else resolve();
    });
    child.once('exit', () => { clearTimeout(timer); reject(Error('Gated wrapper exited before release')); });
    child.once('error', error => { clearTimeout(timer); reject(error); });
  }) : null;
  gatedReady?.catch(() => {}); // The bounded main path awaits and reports it below.
  const rejectPending = error => { for (const item of pending.values()) { clearTimeout(item.timer); item.reject(error); } pending.clear(); };
  const failTransport = error => { transportError ??= error; rejectPending(error); };
  const memory = memoryGuardEnabled ? {heap_limit: ownedMemoryMode ? budget.heap : '512M', required_reserve_gib: parserBudgetMode || r1GameFixedMode || r1ExeMouseCallers ? budget.systemSpareGiB : binarySubobjectProfile ? null : 4, launch_threshold_gib: launchBytes / gib, stop_threshold_gib: stopBytes / gib, interval_ms: 2000, samples: 0, minimum_free_gib: null, stop_triggered: false, calibration_only: binarySubobjectProfile || r1GameMouseCalibration || r1ExeMouseCallers, ...(r1GameFixedMode ? {calibration_source: plan.memory_profile.budget_source, measured_game_peak_available: plan.memory_profile.measured_game_peak_available} : {}), ...(r1ExeMouseCallers ? {calibration_source: plan.memory_profile.budget_source, measured_client_peak_available: false} : {}), ...((r1GameCallbackCore || r1GameMovementInterface || r1GameEntityConstructors || r1GameEntityConstructorSearch || r1GameMouseLinks || r1GameMouseState || r1GameStateTransition || r1GameStateRegistrationSearch || r1GameAddState || r1PlayerRegisterStates || r1PlayerMouseHandlers) ? {prior_game_trial_observation: plan.prior_game_trial_observation, legacy_budget_fields: plan.memory_profile.legacy_budget_fields} : {}), owned_profile_budget: ownedMemoryMode ? budget : null} : null;
  let ownedProfile = null, ownedProfileSummary = null;
  const stopForMemory = (error, free) => {
    if (memory.stop_triggered || (childExited && !ownedMemoryMode)) return;
    memory.stop_triggered = true;
    forcedStop = true;
    failTransport(error);
    try { progress('memory-stop', {free_gib: free / gib, owned_pid: child.pid, error: error.message}); }
    catch (logError) { errors.push(`Memory-stop log failed: ${logError.message}`); }
    if (ownedMemoryMode) {
      if (ownedProfile) ownedProfile.requestStop();
      else { if (child.connected) child.disconnect(); child.kill(); }
      return; // Never use PID/tree traversal to terminate an unproved descendant.
    }
    try {
      assert.ok(Number.isInteger(child.pid), 'Owned child PID is unavailable');
      const stopped = spawnSync(path.join(process.env.SystemRoot, 'System32/taskkill.exe'), ['/PID', String(child.pid), '/T', '/F'], {windowsHide: true, timeout: 5000, stdio: 'pipe'});
      memory.tree_stop_status = stopped.status;
      if (stopped.error || stopped.status !== 0) throw Error(stopped.error?.message ?? `taskkill status ${stopped.status}`);
    } catch (stopError) {
      errors.push(`Owned process-tree stop unconfirmed: ${stopError.message}`);
      try { child.kill(); } catch (killError) { errors.push(`Owned Rea stop failed: ${killError.message}`); }
    }
  };
  const sampleMemory = () => {
    try {
      const free = os.freemem();
      memory.samples++;
      memory.minimum_free_gib = Math.min(memory.minimum_free_gib ?? Infinity, free / gib);
      if (!memoryAllowed(free, false)) stopForMemory(Error(`Free physical memory fell below ${stopBytes / gib} GiB; stopping the owned Rea process tree`), free);
      fs.appendFileSync(path.join(output, 'memory.ndjson'), `${JSON.stringify({utc: new Date().toISOString(), free_gib: free / gib})}\n`);
    } catch (error) {
      errors.push(`Memory monitoring failed: ${error.message}`);
      stopForMemory(error);
    }
  };
  const memoryTimer = memoryGuardEnabled ? setInterval(sampleMemory, 2000) : null;
  child.on('error', failTransport);
  child.stdin.on('error', failTransport);
  child.on('exit', (code, signal) => { childExited = true; ownedProfile?.markBackendExited(); childExitCode = code; childSignal = signal; rejectPending(Error(`Rea exited (${code ?? signal})`)); });
  child.stderr.setEncoding('utf8');
  child.stderr.on('data', chunk => { if (stderr.length + chunk.length <= 1024 * 1024) stderr += chunk; else stderrTruncated = true; });
  child.stdout.setEncoding('utf8');
  child.stdout.on('data', chunk => {
    buffer += chunk;
    if (Buffer.byteLength(buffer, 'utf8') > byteLimit) { failTransport(Error('MCP response exceeds 32 MiB')); buffer = ''; return; }
    let newline;
    while ((newline = buffer.indexOf('\n')) >= 0) {
      const line = buffer.slice(0, newline); buffer = buffer.slice(newline + 1);
      if (!line.trim()) continue;
      let message;
      try { message = JSON.parse(line); } catch { failTransport(Error('Invalid MCP JSON')); continue; }
      collectSecrets(message);
      if (message.method) {
        if (message.id !== undefined) failTransport(Error('Unexpected server request; no automatic approval'));
        continue;
      }
      const item = pending.get(message.id);
      if (item) { pending.delete(message.id); clearTimeout(item.timer); item.resolve(message); }
    }
  });
  function request(method, params) {
    if (transportError || childExited) return Promise.reject(transportError ?? Error('Rea already exited'));
    const id = ++sequence;
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => { pending.delete(id); reject(Error(`Request timed out after 600s: ${method}`)); }, requestTimeout);
      pending.set(id, {resolve, reject, timer});
      child.stdin.write(`${JSON.stringify({jsonrpc: '2.0', id, method, params})}\n`, error => { if (error) failTransport(error); });
    });
  }
  async function call(item) {
    assert.ok(allowed.includes(item.tool), 'Tool outside static allowlist');
    assert.ok(toolCalls < maxToolCalls, 'Tool call budget exhausted');
    toolCalls++;
    const since = Date.now();
    const record = {label: item.label, tool: item.tool, call_number: toolCalls, status: 'pending'};
    records.push(record);
    if (item.tool === 'open_binary') closeNeeded = true; // Opening can fail after partial allocation.
    if (item.tool === 'close_binary') closeAttempted = true;
    progress('request', {call: toolCalls, label: item.label, tool: item.tool});
    const heartbeat = setInterval(() => progress('waiting', {call: record.call_number, label: item.label, elapsed_seconds: Math.floor((Date.now() - since) / 1000)}), 30000);
    try {
      const response = await request('tools/call', {name: item.tool, arguments: item.args});
      save(item.label, response);
      const data = payload(response);
      if (item.tool === 'open_binary') verifyTarget(data.result);
      else if (item.tool === 'close_binary') {
        assert.ok(Object.hasOwn(data, 'result') && data.result === null, 'Close did not acknowledge the no-snapshot lifecycle result');
        closeAcknowledged = true; closeNeeded = false;
      } else if (item.tool === 'binary_session') {
        const expectedOpen = item.label === (r1ExeMouseCallers ? '03-Client-session' : r1GameFixedMode ? '03-Game-session' : '03-Engine-session');
        verifySession(data, expectedOpen);
        if (item.label === finalSessionLabel || item.label === 'cleanup-session') closedVerified = true;
      } else verifyEvidence(data, item);
      if(r1EngineAnimationSpeed && ['procedure_callees','procedure_callers'].includes(item.tool)){
        record.related_result_count=data.normalized_result.length;record.followup_scope_limit=32;
        record.followup_scope_blocked=data.normalized_result.length>32;record.raw_result_truncated=false;
        // This fixed batch never follows discovered edges. The cap stops later
        // scope expansion only; the response was saved in full before validation.
      }
      record.status = 'passed';
      if (data.evidence_id) { record.evidence_id = data.evidence_id; record.limitations = data.limitations ?? []; }
      progress('completed', {call: record.call_number, label: item.label});
      return data;
    } catch (error) {
      record.status = 'failed'; record.error = error.message;
      progress('failed', {call: record.call_number, label: item.label, error: error.message});
      throw error;
    } finally { clearInterval(heartbeat); record.elapsed_ms = Date.now() - since; }
  }
  const waitExit = ms => new Promise(resolve => {
    if (childExited) { resolve(true); return; }
    const onExit = () => { clearTimeout(timer); resolve(true); };
    const timer = setTimeout(() => { child.off('exit', onExit); resolve(false); }, ms);
    child.once('exit', onExit);
  });
  try {
    if (memoryGuardEnabled) sampleMemory();
    progress('starting', {wrapper, plan_sha256: expectedPlanHash, target_sha256: targetHash, owned_pid: child.pid, memory});
    if (ownedMemoryMode) {
      await gatedReady;
      ownedProfile = startOwnedMemoryProfile({root, output, backendPid: child.pid, controllerPid: process.pid, onFailure: error => stopForMemory(error), budget, targetProfile: r1ExeMouseCallers ? 'client' : r1GameFixedMode ? 'game' : 'engine', samplerIdentity: r1GameAssociatedIdentityRepair ? 'game-associated-identity-repair' : 'default', ownedJob: {nonce: ownedNonce, nodeExecutable: process.execPath}});
      await ownedProfile.ready;
      assert.ok(!transportError && !childExited && child.connected, 'Gated root no longer available');
      await new Promise((resolve, reject) => child.send({type: 'release-owned-start', nonce: ownedNonce}, error => error ? reject(error) : resolve()));
      progress('owned-memory-monitor-ready', {owned_pid: child.pid, controller_pid: process.pid, ...(r1GameAssociatedIdentityRepair ? {effective_sampler_identity:plan.identity_repair.sampler_identity,effective_guard_pins:plan.identity_repair.effective_guard_pins} : {})});
    }
    const init = await request('initialize', {protocolVersion: '2024-11-05', capabilities: {}, clientInfo: {name: 'Grim-Dynasty-native-registration-static', version: '1'}});
    save('initialize', init);
    assert.ok(!failureIn(init));
    assert.equal(init.result?.serverInfo?.name, 'rea');
    assert.equal(init.result?.serverInfo?.version, '6.5.0');
    child.stdin.write(`${JSON.stringify({jsonrpc: '2.0', method: 'notifications/initialized'})}\n`);
    const catalog = await request('tools/list', {});
    assert.ok(!failureIn(catalog) && Array.isArray(catalog.result?.tools), 'Tool catalog failed');
    assert.ok(!catalog.result.nextCursor, 'Incomplete tool catalog');
    const selected = catalog.result.tools.filter(tool => allowed.includes(tool.name));
    save('tool-catalog-selected', {total: catalog.result.tools.length, tools: selected});
    for (const name of allowed) {
      const actual = selected.filter(tool => tool.name === name);
      const expected = baseline.filter(tool => tool.name === name);
      assert.equal(actual.length, 1, `Missing or duplicate actual tool: ${name}`);
      assert.equal(expected.length, 1, `Missing baseline tool: ${name}`);
      assert.equal(canonical(actual[0].inputSchema), canonical(expected[0].inputSchema), `Input schema changed: ${name}`);
      assert.ok(actual[0].outputSchema && expected[0].outputSchema, `Output schema missing: ${name}`);
      assert.equal(canonical(actual[0].outputSchema), canonical(expected[0].outputSchema), `Output schema changed: ${name}`);
      for (const item of plan.calls.filter(call => call.tool === name)) validateArgs(item.args, actual[0].inputSchema);
    }
    for (const item of plan.calls) { await call(item); completed++; }
    ownedProfile?.verifyExpectedHeap();
  } catch (error) {
    errors.push(error.message);
  } finally {
    if (closeNeeded && !closeAttempted && !childExited && !transportError && toolCalls < maxToolCalls) {
      try { await call({tool: 'close_binary', args: {}, label: 'cleanup-close'}); }
      catch (error) { errors.push(`Cleanup close: ${error.message}`); }
    }
    if (closeAcknowledged && !closedVerified && !childExited && !transportError && toolCalls < maxToolCalls) {
      try { await call({tool: 'binary_session', args: {expected_package_version: '6.5.0'}, label: 'cleanup-session'}); }
      catch (error) { errors.push(`Cleanup session: ${error.message}`); }
    }
    ownedProfile?.beginFinalization();
    child.stdin.end();
    exitConfirmed = await waitExit(5000);
    if (!exitConfirmed) {
      forcedStop = true;
      if (ownedMemoryMode && ownedProfile) ownedProfile.requestStop();
      else child.kill();
      exitConfirmed = await waitExit(3000);
      if (!exitConfirmed) { child.kill('SIGKILL'); exitConfirmed = await waitExit(2000); }
    }
    rejectPending(Error('Runner finalizing'));
    child.stdin.destroy(); child.stdout.destroy(); child.stderr.destroy(); child.unref();
    if (child.connected) child.disconnect();
    if (!exitConfirmed) errors.push('Rea exit not confirmed after bounded termination; backend cleanup is uncertain');
    if (forcedStop) errors.push('Rea required forced termination; normal process cleanup was not confirmed');
    if (childExitCode !== 0 || childSignal) errors.push(`Abnormal Rea exit: code=${childExitCode}, signal=${childSignal}`);
    if (!closedVerified) errors.push('Final closed-session state not confirmed');
    if (memoryTimer) { clearInterval(memoryTimer); sampleMemory(); }
    if (ownedProfile) {
      try {
        ownedProfileSummary = await ownedProfile.finish();
        if (ownedProfileSummary.failures.length) errors.push(...ownedProfileSummary.failures);
      } catch (error) { errors.push(`Owned memory profile finalization: ${error.message}`); }
    }
    try { finalHash = fileHash(targetPath); if (finalHash !== targetHash) errors.push('Target fingerprint changed'); }
    catch (error) { errors.push(`Final fingerprint: ${error.message}`); }
    if (stderrTruncated) stderr = stderr.slice(0, Math.max(0, stderr.lastIndexOf('\n')));
    fs.writeFileSync(path.join(output, 'stderr.txt'), redactText(stderr), {flag: 'wx'});
    const passed = errors.length === 0 && completed === maxToolCalls && closedVerified && exitConfirmed;
    save('summary', {status: passed ? 'passed' : 'failed', started, finished: new Date().toISOString(), plan_sha256: expectedPlanHash, request_timeout_ms: requestTimeout, tool_calls: toolCalls, completed_plan_calls: completed, targets_launched: false, target: {path: targetPath, sha256_before: targetHash, sha256_after: finalHash}, close_acknowledged: closeAcknowledged, closed_session_verified: closedVerified, process_exit_confirmed: exitConfirmed, forced_termination: forcedStop, child_exit_code: childExitCode, child_signal: childSignal, stderr_truncated: stderrTruncated, memory, owned_memory_profile: ownedProfileSummary, evidence_redaction: 'Credential values are redacted; other parsed tool-response fields are retained.', records, errors});
    progress(passed ? 'passed' : 'failed', {tool_calls: toolCalls, completed_plan_calls: completed, closed_session_verified: closedVerified});
    process.exitCode = passed ? 0 : 1;
  }
}

try { await main(); }
catch (error) { console.error(redactText(error.message)); process.exitCode = 1; }
