using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Unity.Mathematics;

using PurrNet;

using Swole;
using Swole.API.Unity;
using Swole.Animation;
using Swole.API.Unity.Animation;
using UnityEngine.Events;

namespace BulkOut
{

    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayer : NetworkBehaviour
    {

        public const string prefix = "SP";

        public const string eventName_parentHeldItemL = "parent_item_L";
        public const string eventName_parentHeldItemR = "parent_item_R";

        public const string eventName_parentHeldItemResetL = "parent_item_reset_L";
        public const string eventName_parentHeldItemResetR = "parent_item_reset_R";

        public const string eventName_unparentHeldItemL = "unparent_item_L";
        public const string eventName_unparentHeldItemR = "unparent_item_R"; 

        public const string animParam_stamina = "stamina";
        public const string animParam_physique = "physique";
        public const string animParam_pump = "pump";

        public const string animParam_moveX = "moveX";
        public const string animParam_moveZ = "moveZ";
        public const string animParam_turn = "turn";
        public const string animParam_jump = "jump";
        public const string animParam_grounded = "grounded";

        public const string animParam_moveSpeed = "moveSpeed";
        public const float moveAnimSpeedWalk = 1f;
        public const float moveAnimSpeedSprint = 2f;

        private const float halfSqrt2 = 0.70710678f;

        [SerializeField]
        protected Transform leftHandItemBone;
        public Transform LeftHandeItemBone => leftHandItemBone;

        [SerializeField]
        protected Transform rightHandItemBone;
        public Transform RightHandeItemBone => rightHandItemBone;

        [Serializable]
        public enum MoveMode
        {
            AlwaysForward, Aimed
        }

        protected MoveMode moveMode;
        public bool pauseMovement;

        public void PauseMovement() { pauseMovement = true; }
        public void UnpauseMovement() { pauseMovement = false; }

        [SerializeField]
        protected IMuscularBasic muscleController;
        public void SetMuscleController(IMuscularBasic muscleController)
        {
            this.muscleController = muscleController;
        }

        [SerializeField]
        new protected BulkOutCamera camera;
        public void SetCamera(BulkOutCamera camera)
        {
            this.camera = camera;
            if (camera != null) camera.SetTarget(transform); 
        }

        [SerializeField]
        protected CustomAnimator animator;
        public CustomAnimator Animator => animator;
        public void SetAnimator(CustomAnimator animator)
        {
            this.animator = animator;
        }

        [NonSerialized]
        protected CharacterController characterController;

        [SerializeField]
        protected AssetIdentifier defaultAnimId;

        [SerializeField]
        protected SimpleCharacterAsset characterDataAsset;
        protected SimpleCharacter characterData;

        protected CustomAnimationController characterAnimationController;

        protected static readonly CustomAnimationParametersBuilder paramBuilder = new CustomAnimationParametersBuilder();
        protected static readonly CustomMotionControllersBuilder motionControllersBuilder = new CustomMotionControllersBuilder();

        public void SetCharacterData(SimpleCharacter character)
        {
            this.characterData = character;

            SetupAnimations();
        }

        public virtual void SetupAnimations()
        {
            if (animator != null)
            {
                if (characterAnimationController != null)
                {
                    animator.RemoveControllerData(characterAnimationController);
                    GameObject.Destroy(characterAnimationController);
                }

                characterAnimationController = null;
                if (characterData != null)
                {
                    paramBuilder.Clear();
                    motionControllersBuilder.Clear();

                    paramBuilder.AddFloatParameter(new CustomAnimationParameter.Float(animParam_stamina, 1f), out int staminaParamIndex, out _);
                    paramBuilder.AddFloatParameter(new CustomAnimationParameter.Float(animParam_physique, 0f), out int physiqueParamIndex, out _);
                    paramBuilder.AddFloatParameter(new CustomAnimationParameter.Float(animParam_pump, 0f), out int pumpParamIndex, out _);

                    paramBuilder.AddFloatParameter(new CustomAnimationParameter.Float(animParam_moveX, 0f), out int moveXParamIndex, out _);
                    paramBuilder.AddFloatParameter(new CustomAnimationParameter.Float(animParam_moveZ, 0f), out int moveZParamIndex, out _);
                    paramBuilder.AddFloatParameter(new CustomAnimationParameter.Float(animParam_turn, 0f), out int turnParamIndex, out _);
                    paramBuilder.AddTriggerParameter(new CustomAnimationParameter.Trigger(animParam_jump), out int jumpParamIndex, out _);
                    paramBuilder.AddBoolParameter(new CustomAnimationParameter.Boolean(animParam_grounded, false), out int groundedParamIndex, out _);

                    paramBuilder.AddFloatParameter(new CustomAnimationParameter.Float(animParam_moveSpeed, 0f), out int moveSpeedParamIndex, out _);

                    if (characterData.assets.TryBuildMotionControllers(defaultAnimId, paramBuilder, motionControllersBuilder, out var finalIndices, animParam_stamina, animParam_physique, animParam_pump))
                    {
                        var layer = new CustomAnimationLayer(characterData.Name, 1f);


                        #region Move Fwd Turn Left

                        var moveFwdTurnLeft = new BlendTree1D("moveFwdTurnL", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkFwdTurnLeft.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintFwdTurnLeft.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveFwdTurnLeft.forceSyncOnProgress = true;
                        moveFwdTurnLeft.useDedicatedChildForTime = true;
                        moveFwdTurnLeft.timeDedicatedChildIndex = 0;
                        moveFwdTurnLeft.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveFwdTurnLeft, out int ind_moveFwdTurnLeft, out _);

                        #endregion

                        #region Move Fwd

                        var moveFwd = new BlendTree1D("moveFwd", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkFwd.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintFwd.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveFwd.forceSyncOnProgress = true;
                        moveFwd.useDedicatedChildForTime = true;
                        moveFwd.timeDedicatedChildIndex = 0;
                        moveFwd.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveFwd, out int ind_moveFwd, out _);

                        #endregion

                        #region Move Fwd Turn Right

                        var moveFwdTurnRight = new BlendTree1D("moveFwdTurnR", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkFwdTurnRight.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintFwdTurnRight.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveFwdTurnRight.forceSyncOnProgress = true;
                        moveFwdTurnRight.useDedicatedChildForTime = true;
                        moveFwdTurnRight.timeDedicatedChildIndex = 0;
                        moveFwdTurnRight.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveFwdTurnRight, out int ind_moveFwdTurnRight, out _);

                        #endregion


                        #region Move Back Turn Left

                        var moveBackTurnLeft = new BlendTree1D("moveBackTurnL", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkBackTurnLeft.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintBackTurnLeft.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveBackTurnLeft.forceSyncOnProgress = true;
                        moveBackTurnLeft.useDedicatedChildForTime = true;
                        moveBackTurnLeft.timeDedicatedChildIndex = 0;
                        moveBackTurnLeft.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveBackTurnLeft, out int ind_moveBackTurnLeft, out _);

                        #endregion

                        #region Move Back

                        var moveBack = new BlendTree1D("moveBack", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkBack.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintBack.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveBack.forceSyncOnProgress = true;
                        moveBack.useDedicatedChildForTime = true;
                        moveBack.timeDedicatedChildIndex = 0;
                        moveBack.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveBack, out int ind_moveBack, out _);

                        #endregion

                        #region Move Back Turn Right

                        var moveBackTurnRight = new BlendTree1D("moveBackTurnR", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkBackTurnRight.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintBackTurnRight.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveBackTurnRight.forceSyncOnProgress = true;
                        moveBackTurnRight.useDedicatedChildForTime = true;
                        moveBackTurnRight.timeDedicatedChildIndex = 0;
                        moveBackTurnRight.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveBackTurnRight, out int ind_moveBackTurnRight, out _);

                        #endregion



                        #region Move Fwd Left

                        var moveFwdLeft = new BlendTree1D("moveFwdLeft", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkFwdLeft.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintFwdLeft.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveFwdLeft.forceSyncOnProgress = true;
                        moveFwdLeft.useDedicatedChildForTime = true;
                        moveFwdLeft.timeDedicatedChildIndex = 0;
                        moveFwdLeft.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveFwdLeft, out int ind_moveFwdLeft, out _);

                        #endregion

                        #region Move Left

                        var moveLeft = new BlendTree1D("moveLeft", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkLeft.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintLeft.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveLeft.forceSyncOnProgress = true;
                        moveLeft.useDedicatedChildForTime = true;
                        moveLeft.timeDedicatedChildIndex = 0;
                        moveLeft.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveLeft, out int ind_moveLeft, out _);

                        #endregion

                        #region Move Back Left

                        var moveBackLeft = new BlendTree1D("moveBackLeft", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkBackLeft.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintBackLeft.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveBackLeft.forceSyncOnProgress = true;
                        moveBackLeft.useDedicatedChildForTime = true;
                        moveBackLeft.timeDedicatedChildIndex = 0;
                        moveBackLeft.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveBackLeft, out int ind_moveBackLeft, out _);

                        #endregion


                        #region Move Fwd Right

                        var moveFwdRight = new BlendTree1D("moveFwdRight", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkFwdRight.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintFwdRight.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveFwdRight.forceSyncOnProgress = true;
                        moveFwdRight.useDedicatedChildForTime = true;
                        moveFwdRight.timeDedicatedChildIndex = 0;
                        moveFwdRight.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveFwdRight, out int ind_moveFwdRight, out _);

                        #endregion

                        #region Move Right

                        var moveRight = new BlendTree1D("moveRight", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkRight.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintRight.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveRight.forceSyncOnProgress = true;
                        moveRight.useDedicatedChildForTime = true;
                        moveRight.timeDedicatedChildIndex = 0;
                        moveRight.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveRight, out int ind_moveRight, out _);

                        #endregion

                        #region Move Back Right

                        var moveBackRight = new BlendTree1D("moveBackRight", moveSpeedParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_walkBackRight.controllerIndex,
                                threshold = moveAnimSpeedWalk
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = finalIndices.animAsset_sprintBackRight.controllerIndex,
                                threshold = moveAnimSpeedSprint
                            }
                        });
                        moveBackRight.forceSyncOnProgress = true;
                        moveBackRight.useDedicatedChildForTime = true;
                        moveBackRight.timeDedicatedChildIndex = 0;
                        moveBackRight.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveBackRight, out int ind_moveBackRight, out _);

                        #endregion



                        #region Move Forward Base

                        var moveFwdTree = new BlendTree1D("moveFwd", turnParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = ind_moveFwdTurnLeft,
                                threshold = -1f
                            },
                            
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = ind_moveFwd,
                                threshold = 0f
                            },
                            
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = ind_moveFwdTurnRight,
                                threshold = 1f
                            }
                        });
                        moveFwdTree.forceSyncOnProgress = true;
                        moveFwdTree.useDedicatedChildForTime = true;
                        moveFwdTree.timeDedicatedChildIndex = 1;
                        moveFwdTree.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveFwdTree, out int ind_moveFwdTree, out _);

                        #endregion

                        #region Move Back Base

                        var moveBackTree = new BlendTree1D("moveBack", turnParamIndex, new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = ind_moveBackLeft,
                                threshold = -1f
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = ind_moveBack,
                                threshold = 0f
                            },

                            new MotionField1D()
                            {
                                speed = 1f,
                                controllerIndex = ind_moveBackRight,
                                threshold = 1f
                            }
                        });
                        moveBackTree.forceSyncOnProgress = true;
                        moveBackTree.useDedicatedChildForTime = true;
                        moveBackTree.timeDedicatedChildIndex = 1;
                        moveBackTree.speedMultiplierParameter = moveSpeedParamIndex;

                        motionControllersBuilder.AddBlendTree1D(moveBackTree, out int ind_moveBackTree, out _);

                        #endregion



                        #region Move

                        var moveTree = new BlendTree2D("move", moveXParamIndex, moveZParamIndex, new MotionField2D[]
                        {
                            new MotionField2D() // center
                            {
                                speed = 1f,
                                controllerIndex = ind_moveFwdTree,
                                x = 0f,
                                y = 0f
                            },

                            new MotionField2D() // forward
                            {
                                speed = 1f,
                                controllerIndex = ind_moveFwdTree,
                                x = 0f,
                                y = 1f
                            },
                            new MotionField2D() // back
                            {
                                speed = 1f,
                                controllerIndex = ind_moveBackTree,
                                x = 0f,
                                y = -1f
                            },
                            
                            new MotionField2D() // fwd right
                            {
                                speed = 1f,
                                controllerIndex = ind_moveFwdRight,
                                x = halfSqrt2,
                                y = halfSqrt2
                            },
                            new MotionField2D() // right
                            {
                                speed = 1f,
                                controllerIndex = ind_moveRight,
                                x = 1f,
                                y = 0f
                            },
                            new MotionField2D() // back right
                            {
                                speed = 1f,
                                controllerIndex = ind_moveBackRight,
                                x = halfSqrt2,
                                y = -halfSqrt2
                            },
                            new MotionField2D() // back left
                            {
                                speed = 1f,
                                controllerIndex = ind_moveBackLeft,
                                x = -halfSqrt2,
                                y = -halfSqrt2
                            },
                            new MotionField2D() // left
                            {
                                speed = 1f,
                                controllerIndex = ind_moveLeft,
                                x = -1f,
                                y = 0f
                            },
                            new MotionField2D() // fwd left
                            {
                                speed = 1f,
                                controllerIndex = ind_moveFwdLeft,
                                x = -halfSqrt2,
                                y = halfSqrt2
                            },
                        });
                        moveTree.forceSyncOnProgress = true;
                        moveTree.useDedicatedChildForTime = true;
                        moveTree.timeDedicatedChildIndex = 0;

                        motionControllersBuilder.AddBlendTree2D(moveTree, out int ind_moveTree, out _);

                        #endregion


                        var layerStates = new CustomAnimationLayerState[2];

                        var idleState = layerStates[0] = new CustomAnimationLayerState();
                        idleState.name = "idle";
                        idleState.index = 0;
                        idleState.motionControllerIndex = finalIndices.animAsset_idle.controllerIndex; 

                        var moveState = layerStates[1] = new CustomAnimationLayerState();
                        moveState.name = "move";
                        moveState.index = 1;
                        moveState.motionControllerIndex = ind_moveTree;

                        idleState.transitions = new Transition[]
                        {
                            new Transition()
                            {
                                targetStateIndex = moveState.index,
                                transitionTime = 0.2f,
                                minNormalizedTime = 0f,
                                maxNormalizedTime = 1f,
                                validRequirementPaths = new TransitionRequirement[]
                                {
                                    new TransitionRequirement()
                                    {
                                        parameters = new TransitionParameter[]
                                        {
                                            new TransitionParameter()
                                            {
                                                comparisonType = TransitionComparisonType.GreaterThan,
                                                parameterName = animParam_moveSpeed,
                                                parameterValue = 0.01f,
                                                absoluteValue = true
                                            }
                                        }
                                    }
                                }
                            }
                        };

                        moveState.transitions = new Transition[]
                        {
                            new Transition()
                            {
                                targetStateIndex = idleState.index,
                                transitionTime = 0.2f,
                                minNormalizedTime = 0f,
                                maxNormalizedTime = 1f,
                                validRequirementPaths = new TransitionRequirement[]
                                {
                                    new TransitionRequirement()
                                    {
                                        parameters = new TransitionParameter[]
                                        {
                                            new TransitionParameter()
                                            {
                                                comparisonType = TransitionComparisonType.LessThanOrEqualTo,
                                                parameterName = animParam_moveSpeed,
                                                parameterValue = 0.01f,
                                                absoluteValue = true
                                            }
                                        }
                                    }
                                }
                            }
                        };

                        layer.entryStateIndex = 0;
                        layer.states = layerStates;
                        layer.IsAdditive = true;

                        motionControllersBuilder.Finalize(layer);

                        characterAnimationController = ScriptableObject.CreateInstance<CustomAnimationController>();
                        paramBuilder.Finalize(characterAnimationController);
                        motionControllersBuilder.Finalize(characterAnimationController);
                        characterAnimationController.layers = new CustomAnimationLayer[] { layer };

                        characterAnimationController.Prefix = prefix; 
                        animator.ApplyController(characterAnimationController); 
                    }
                    else
                    {
                        Debug.LogError($"Failed to build motion controllers for character {characterData.Name}");
                    }
                }

                paramBuilder.Clear();
                motionControllersBuilder.Clear();
            }
        }

        public float walkSpeed = 1f;
        public float sprintMul = 1.5f;
        public float jumpPower = 12f;
        public float gravity = 10f;

        [SerializeField]
        protected Vector3 velocity;

        public float stamina = 1f;
        public bool IsTired => stamina < 0.5f;
        public bool IsExhausted => stamina < 0.1f;

        private static readonly MuscleGroupIdentifier[] reactivePumpGroups = new MuscleGroupIdentifier[]
        {
            new MuscleGroupIdentifier(MuscleGroup.Biceps, Side.Left),
            new MuscleGroupIdentifier(MuscleGroup.Biceps, Side.Right),
            new MuscleGroupIdentifier(MuscleGroup.Triceps, Side.Left),
            new MuscleGroupIdentifier(MuscleGroup.Triceps, Side.Right),
            new MuscleGroupIdentifier(MuscleGroup.InnerForearm, Side.Left),
            new MuscleGroupIdentifier(MuscleGroup.InnerForearm, Side.Right),
            new MuscleGroupIdentifier(MuscleGroup.OuterForearm, Side.Left),
            new MuscleGroupIdentifier(MuscleGroup.OuterForearm, Side.Right),
            new MuscleGroupIdentifier(MuscleGroup.Shoulders, Side.Left),
            new MuscleGroupIdentifier(MuscleGroup.Shoulders, Side.Right),
            new MuscleGroupIdentifier(MuscleGroup.Pecs, Side.Left),
            new MuscleGroupIdentifier(MuscleGroup.Pecs, Side.Right)
        };
        public float ReactivePump
        {
            get
            {
                float pump = 0f;

                if (muscleController != null)
                {
                    foreach (var pumpGroup in reactivePumpGroups) 
                    {
                        pump += muscleController.GetMuscleGroupPumpUnsafe(muscleController.GetMuscleGroupIndex(pumpGroup));
                    }

                    pump = pump / reactivePumpGroups.Length;
                }

                return pump;
            }
        }

        protected virtual void Awake()
        {
            characterController = gameObject.GetComponent<CharacterController>();

            if (muscleController == null) SetMuscleController(gameObject.GetComponent<IMuscularBasic>());
            if (animator == null) SetAnimator(gameObject.GetComponent<CustomAnimator>());

            if (characterData == null && characterDataAsset != null) characterData = characterDataAsset;
            SetCharacterData(characterData);
        }
        protected virtual void Start()
        {
            characterController.enabled = isOwner;

            if (isOwner)
            {
                if (camera == null) camera = Camera.main.GetComponent<BulkOutCamera>();
                SetCamera(camera);
            }
        }

        public UnityEvent OnUpdate = new UnityEvent();
        public UnityEvent OnLateUpdate = new UnityEvent();

        protected virtual void Update()
        {
            if (isOwner)
            {
                switch (moveMode)
                {
                    case MoveMode.AlwaysForward:
                        Move_AlwaysForward();
                        break;

                    case MoveMode.Aimed:
                        Move_Aimed();
                        break;
                }
            } 

            animator.FindParameter(animParam_pump).SetValue(ReactivePump);

            OnUpdate?.Invoke();
        }

        protected virtual void LateUpdate()
        {
            OnLateUpdate?.Invoke();
        }

        protected virtual void Move_AlwaysForward()
        {
            Vector2 moveDir = new Vector2(InputProxy.MoveAxisX, InputProxy.MoveAxisY);
            if (camera != null)
            {
                Vector3 camDir = camera.transform.forward;
                camDir.y = 0f;
                camDir = camDir.normalized;
                moveDir = ((float3)(Quaternion.Euler(0f, Vector3.SignedAngle(Vector3.forward, camDir, Vector3.up), 0f) * new Vector3(moveDir.x, 0f, moveDir.y))).xz;  
            }

            float moveSpeed = moveDir.magnitude;
            if (moveSpeed < 0.05f) moveSpeed = 0f;
            if (moveSpeed > 0f)
            {
                moveDir = moveDir / moveSpeed;
                animator.FindParameter(animParam_moveSpeed).SetValue(Mathf.Max(Mathf.Abs(InputProxy.MoveAxisX), Mathf.Abs(InputProxy.MoveAxisY)));

                float turn = Mathf.Clamp(Vector3.SignedAngle(transform.forward, new Vector3(moveDir.x, 0f, moveDir.y), Vector3.up) / 15f, -1f, 1f);
                var turnParam = animator.FindParameter(animParam_turn);
                turnParam.SetValue(Mathf.MoveTowards(turnParam.Value, turn, 3f * Time.deltaTime)); 
            } 
            else
            {
                animator.FindParameter(animParam_moveSpeed).SetValue(0f);
                animator.FindParameter(animParam_turn).SetValue(0f);
            }
        }
        protected virtual void Move_Aimed()
        {
            Vector2 moveDir = new Vector2(InputProxy.MoveAxisX, InputProxy.MoveAxisY).normalized;
            float moveSpeed = Mathf.Max(Mathf.Abs(InputProxy.MoveAxisX), Mathf.Abs(InputProxy.MoveAxisY));
            if (moveSpeed < 0.05f) moveSpeed = 0f;
            animator.FindParameter(animParam_moveSpeed).SetValue(moveSpeed);
            if (moveSpeed > 0f)
            {
                var pX = animator.FindParameter(animParam_moveX);
                var pZ = animator.FindParameter(animParam_moveZ);

                pX.SetValue(Mathf.MoveTowards(pX.Value, moveDir.x, Time.deltaTime * 5f));
                pZ.SetValue(Mathf.MoveTowards(pZ.Value, moveDir.y, Time.deltaTime * 5f));
            }
            else
            {
                animator.FindParameter(animParam_moveSpeed).SetValue(0f);
                animator.FindParameter(animParam_turn).SetValue(0f);
            }
        }

        public UnityEvent OnFixedUpdate = new UnityEvent();

        protected virtual void FixedUpdate()
        {
            if (isOwner)
            {
                if (!pauseMovement)
                {
                    if (characterController.collisionFlags.HasFlag(CollisionFlags.Below))
                    {
                        velocity.y = Mathf.Max(0f, velocity.y);
                    }

                    velocity = velocity + Vector3.down * gravity * Time.fixedDeltaTime; 

                    characterController.Move(velocity * Time.fixedDeltaTime);

                    switch (moveMode)
                    {
                        case MoveMode.AlwaysForward:
                            MoveFixed_AlwaysForward(); 
                            break;

                        case MoveMode.Aimed:
                            MoveFixed_Aimed();
                            break;
                    }
                }
            }

            OnFixedUpdate?.Invoke(); 
        }

        protected virtual void MoveFixed_AlwaysForward() 
        {
            Vector2 moveDir = new Vector2(InputProxy.MoveAxisX, InputProxy.MoveAxisY);
            if (camera != null) 
            {
                Vector3 camDir = camera.transform.forward;
                camDir.y = 0f;
                camDir = camDir.normalized;
                moveDir = ((float3)(Quaternion.Euler(0f, Vector3.SignedAngle(Vector3.forward, camDir, Vector3.up), 0f) * new Vector3(moveDir.x, 0f, moveDir.y))).xz;
            }
            Debug.DrawRay(transform.position, new Vector3(moveDir.x, 0f, moveDir.y), Color.green, 1f); 
            float moveMag = moveDir.magnitude;
            if (moveMag < 0.05f) moveMag = 0f;
            if (moveMag > 0f)
            {
                moveDir = moveDir / moveMag;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(new Vector3(moveDir.x, 0f, moveDir.y), Vector3.up), 360f * Time.fixedDeltaTime);
                characterController.Move(transform.forward * Mathf.Min(1f, moveMag) * 1f * Time.fixedDeltaTime);  
            }
        }
        protected virtual void MoveFixed_Aimed()
        {
            Vector2 moveDir = new Vector2(InputProxy.MoveAxisX, InputProxy.MoveAxisY);
            float moveMag = moveDir.magnitude;
            if (moveMag < 0.05f) moveMag = 0f;
            if (moveMag > 0f)
            {
                characterController.Move(transform.rotation * new Vector3(moveDir.x / moveMag, 0f, moveDir.y / moveMag) * Mathf.Min(1f, moveMag) * 1f * Time.fixedDeltaTime);
            }
        }

    }

    [Serializable]
    public class SimplePlayerAssets 
    {
        [Serializable]
        public struct AnimMix
        {
            public AssetIdentifier animId;
            public float mix;
            public bool playOnce;
        }
        [Serializable]
        public struct AnimGroup
        {
            public AnimMix[] anims;
            public bool isAdditive;

            [NonSerialized]
            public int controllerIndex;
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class NonLoopingAttribute : Attribute
        {
        }

        public AnimGroup animAsset_idle;

        [NonLooping]
        public AnimGroup animAsset_jump;
        public AnimGroup animAsset_fall;
        [NonLooping]
        public AnimGroup animAsset_land;

        [Header("Walk")]
        public AnimGroup animAsset_walkFwd;
        public AnimGroup animAsset_walkFwdTurnLeft;
        public AnimGroup animAsset_walkFwdTurnRight;

        public AnimGroup animAsset_walkFwdLeft;
        public AnimGroup animAsset_walkFwdRight;
        public AnimGroup animAsset_walkLeft;
        public AnimGroup animAsset_walkRight;
        public AnimGroup animAsset_walkBackLeft;
        public AnimGroup animAsset_walkBackRight;

        public AnimGroup animAsset_walkBack;
        public AnimGroup animAsset_walkBackTurnLeft;
        public AnimGroup animAsset_walkBackTurnRight;

        [Header("Sprint")]
        public AnimGroup animAsset_sprintFwd;
        public AnimGroup animAsset_sprintFwdTurnLeft;
        public AnimGroup animAsset_sprintFwdTurnRight;

        public AnimGroup animAsset_sprintFwdLeft;
        public AnimGroup animAsset_sprintFwdRight;
        public AnimGroup animAsset_sprintLeft;
        public AnimGroup animAsset_sprintRight;
        public AnimGroup animAsset_sprintBackLeft;
        public AnimGroup animAsset_sprintBackRight;

        public AnimGroup animAsset_sprintBack;
        public AnimGroup animAsset_sprintBackTurnLeft;
        public AnimGroup animAsset_sprintBackTurnRight;

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();

            var fields = typeof(SimplePlayerAssets).GetFields(BindingFlags.Instance | BindingFlags.Public);
            if (fields != null)
            {
                foreach(var field in fields)
                {
                    var val = field.GetValue(this);
                    if (val is AnimMix[] array)
                    {
                        foreach(var anim in array)
                        {
                            if (!string.IsNullOrWhiteSpace(anim.animId.package)) dependencies.Add(anim.animId.package);
                        }
                    }
                }
            }

            return dependencies;
        }

    }

    [Serializable]
    public class SimpleMuscularPlayerAssets
    {

        public SimplePlayerAssets baseAssets;
        public SimplePlayerAssets tiredAssets;
        public SimplePlayerAssets exhaustedAssets;

        public SimplePlayerAssets bigPhysiqueAssets;
        public SimplePlayerAssets pumpedAssets;

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {
            if (dependencies == null) dependencies = new List<PackageIdentifier>();

            if (baseAssets != null) baseAssets.ExtractPackageDependencies(dependencies);
            if (tiredAssets != null) tiredAssets.ExtractPackageDependencies(dependencies);
            if (exhaustedAssets != null) exhaustedAssets.ExtractPackageDependencies(dependencies);

            if (bigPhysiqueAssets != null) bigPhysiqueAssets.ExtractPackageDependencies(dependencies);
            if (pumpedAssets != null) pumpedAssets.ExtractPackageDependencies(dependencies);

            return dependencies;
        }

        private static readonly List<MotionPart> temp_motionParts = new List<MotionPart>();
        private static readonly List<MotionField1D> temp_motionField1Ds = new List<MotionField1D>();
        private static readonly List<MotionField2D> temp_motionField2Ds = new List<MotionField2D>();
        public bool TryBuildMotionControllers(AssetIdentifier defaultAnimId, CustomAnimationParametersBuilder parameters, CustomMotionControllersBuilder motionControllers, out SimplePlayerAssets finalIndices, 
            string staminaParam = SimplePlayer.animParam_stamina, 
            string physiqueParam = SimplePlayer.animParam_physique, 
            string pumpParam = SimplePlayer.animParam_pump
            )
        {
            finalIndices = new SimplePlayerAssets();

            if (!ContentManager.TryFindContent<CustomAnimation>(defaultAnimId.name, defaultAnimId.package, out var defaultAnim))
            {
                swole.LogError($"Default animation asset {defaultAnimId} not found!");
                return false;
            }

            CustomAnimation GetAnimationAsset(AssetIdentifier animId)
            {
                if (!ContentManager.TryFindContent<CustomAnimation>(animId.name, animId.package, out var animAsset))
                {
                    swole.LogError($"Animation asset {animId} not found!");
                    return defaultAnim;
                }

                return animAsset;
            }

            void EvaluateAnimMixes(ref SimplePlayerAssets.AnimGroup group, string name, AnimationLoopMode loopMode, bool allowNull, out int topControllerIndex) // Evaluates the initial anim groups into singular animation references or motion composites
            {
                CustomMotionController controller = null;
                topControllerIndex = -1;
                if (group.anims != null && group.anims.Length > 0)
                {
                    if (group.anims.Length > 1)
                    {
                        temp_motionParts.Clear();

                        for (int i = 0; i < group.anims.Length; i++)
                        {
                            var mix = group.anims[i];
                            var animRef = new AnimationReference(name, GetAnimationAsset(mix.animId), loopMode);
                            motionControllers.AddAnimationReference(animRef, out int idIndex, out _);  
                            temp_motionParts.Add(new MotionPart()
                            {
                                speed = 1f,
                                mix = mix.mix,
                                controllerIndex = idIndex
                            });
                        }

                        controller = new MotionComposite(name, temp_motionParts.ToArray());
                        motionControllers.AddMotionComposite((MotionComposite)controller, out topControllerIndex, out _);
                    }
                    else
                    {
                        var mix = group.anims[0];
                        controller = new AnimationReference(name, GetAnimationAsset(mix.animId), loopMode);
                        motionControllers.AddAnimationReference((AnimationReference)controller, out int idIndex, out _);
                        if (mix.mix < 1f)
                        {
                            var parts = new MotionPart[]
                            {
                                new MotionPart()
                                {
                                    speed = 1f,
                                    mix = mix.mix,
                                    controllerIndex = idIndex
                                }
                            };

                            controller = new MotionComposite(name, parts);
                            motionControllers.AddMotionComposite((MotionComposite)controller, out topControllerIndex, out _);
                        } 
                        else
                        {
                            topControllerIndex = idIndex; 
                        }
                    }
                }
                else if (!allowNull)
                {
                    controller = new AnimationReference(name, defaultAnim, AnimationLoopMode.Loop);
                    motionControllers.AddAnimationReference((AnimationReference)controller, out topControllerIndex, out _);
                }

                group.controllerIndex = topControllerIndex;
            }

            int staminaParamIndex = parameters.FindParameterIndex(staminaParam);
            int physiqueParamIndex = parameters.FindParameterIndex(physiqueParam);
            int pumpParamIndex = parameters.FindParameterIndex(pumpParam);

            var fields = typeof(SimplePlayerAssets).GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (typeof(SimplePlayerAssets.AnimGroup).IsAssignableFrom(field.FieldType))
                {

                    AnimationLoopMode loopMode = field.GetCustomAttribute<SimplePlayerAssets.NonLoopingAttribute>() == null ? AnimationLoopMode.Loop : AnimationLoopMode.PlayOnce;

                    SimplePlayerAssets.AnimGroup baseGroup = (SimplePlayerAssets.AnimGroup)field.GetValue(baseAssets);
                    SimplePlayerAssets.AnimGroup tiredGroup = (SimplePlayerAssets.AnimGroup)field.GetValue(tiredAssets);
                    SimplePlayerAssets.AnimGroup exhaustedGroup = (SimplePlayerAssets.AnimGroup)field.GetValue(exhaustedAssets);
                    SimplePlayerAssets.AnimGroup bigPhysiqueGroup = (SimplePlayerAssets.AnimGroup)field.GetValue(bigPhysiqueAssets);
                    SimplePlayerAssets.AnimGroup pumpedGroup = (SimplePlayerAssets.AnimGroup)field.GetValue(pumpedAssets);

                    EvaluateAnimMixes(ref baseGroup, field.Name, loopMode, false, out int baseControllerIndex);
                    EvaluateAnimMixes(ref tiredGroup, field.Name, loopMode, true, out int tiredControllerIndex);
                    EvaluateAnimMixes(ref exhaustedGroup, field.Name, loopMode, true, out int exhaustedControllerIndex);
                    EvaluateAnimMixes(ref bigPhysiqueGroup, field.Name, loopMode, true, out int bigPhysiqueControllerIndex);
                    EvaluateAnimMixes(ref pumpedGroup, field.Name, loopMode, true, out int pumpedControllerIndex);

                    int topControllerIndex = baseControllerIndex;
                    CustomMotionController topController = motionControllers.GetMotionController(topControllerIndex); 

                    #region Big Physique Anims

                    if (bigPhysiqueControllerIndex >= 0)
                    {
                        if (bigPhysiqueGroup.isAdditive)
                        {
                            MotionComposite comp = null;
                            temp_motionParts.Clear();
                            if (topController is MotionComposite)
                            {
                                comp = (MotionComposite)topController;
                                if (comp.motionParts != null) temp_motionParts.AddRange(comp.motionParts);
                            }
                            else
                            {
                                comp = new MotionComposite(topController.name, null);
                                temp_motionParts.Add(new MotionPart()
                                {
                                    speed = 1f,
                                    mix = 1f,
                                    controllerIndex = topControllerIndex
                                });
                            }

                            temp_motionParts.Add(new MotionPart()
                            {
                                speed = 1f,
                                mix = 1f,
                                controllerIndex = bigPhysiqueControllerIndex,
                                mixParameterIndex = physiqueParamIndex
                            });

                            comp.motionParts = temp_motionParts.ToArray();
                            if (!ReferenceEquals(comp, topController))
                            {
                                topController = comp;
                                motionControllers.AddMotionComposite(comp, out topControllerIndex, out _);
                            }
                        }
                        else
                        {
                            var motionFields = new MotionField1D[]
                            {
                                new MotionField1D()
                                {
                                    controllerIndex = topControllerIndex,
                                    speed = 1f,
                                    threshold = 0f
                                },
                                new MotionField1D()
                                {
                                    controllerIndex = bigPhysiqueControllerIndex,
                                    speed = 1f,
                                    threshold = 1f // full physique
                                },
                            };

                            topController = new BlendTree1D(field.Name + "_tree", physiqueParamIndex, motionFields);
                            motionControllers.AddBlendTree1D((BlendTree1D)topController, out topControllerIndex, out _);
                        }
                    }

                    #endregion

                    #region Pumped Anims

                    if (pumpedControllerIndex >= 0)
                    {
                        if (pumpedGroup.isAdditive)
                        {
                            MotionComposite comp = null;
                            temp_motionParts.Clear();
                            if (topController is MotionComposite)
                            {
                                comp = (MotionComposite)topController;
                                if (comp.motionParts != null) temp_motionParts.AddRange(comp.motionParts);
                            }
                            else
                            {
                                comp = new MotionComposite(topController.name, null); 
                                temp_motionParts.Add(new MotionPart()
                                {
                                    speed = 1f,
                                    mix = 1f,
                                    controllerIndex = topControllerIndex
                                });
                            }

                            temp_motionParts.Add(new MotionPart()
                            {
                                speed = 1f,
                                mix = 1f,
                                controllerIndex = pumpedControllerIndex,
                                mixParameterIndex = pumpParamIndex
                            });

                            comp.motionParts = temp_motionParts.ToArray();
                            if (!ReferenceEquals(comp, topController))
                            {
                                topController = comp;
                                motionControllers.AddMotionComposite(comp, out topControllerIndex, out _);
                            }
                        }
                        else
                        {
                            var motionFields = new MotionField1D[]
                            {
                                new MotionField1D()
                                {
                                    controllerIndex = topControllerIndex,
                                    speed = 1f,
                                    threshold = 0f
                                },
                                new MotionField1D()
                                {
                                    controllerIndex = pumpedControllerIndex,
                                    speed = 1f,
                                    threshold = 1f // full pump
                                },
                            };

                            topController = new BlendTree1D(field.Name + "_tree", pumpParamIndex, motionFields);
                            motionControllers.AddBlendTree1D((BlendTree1D)topController, out topControllerIndex, out _);
                        }
                    }

                    #endregion

                    #region Tired & Exhausted Anims

                    if (tiredControllerIndex >= 0 && exhaustedControllerIndex >= 0 && tiredGroup.isAdditive && exhaustedGroup.isAdditive)
                    {
                        var motionFields = new MotionField1D[]
                        {
                            new MotionField1D()
                            {
                                controllerIndex = -1,
                                speed = 0f,
                                threshold = 1f
                            },
                            new MotionField1D()
                            {
                                controllerIndex = tiredControllerIndex,
                                speed = 1f,
                                threshold = 0.5f // half stamina
                            },
                            new MotionField1D()
                            {
                                controllerIndex = exhaustedControllerIndex,
                                speed = 1f,
                                threshold = 0f // zero stamina
                            }
                        };

                        var tree = new BlendTree1D(field.Name + "_tree", staminaParamIndex, motionFields);
                        motionControllers.AddBlendTree1D(tree, out int treeIndex, out _);

                        MotionComposite comp = null;
                        temp_motionParts.Clear();
                        if (topController is MotionComposite)
                        {
                            comp = (MotionComposite)topController;
                            if (comp.motionParts != null) temp_motionParts.AddRange(comp.motionParts);
                        }
                        else
                        {
                            comp = new MotionComposite(topController.name, null);
                            temp_motionParts.Add(new MotionPart()
                            {
                                speed = 1f,
                                mix = 1f,
                                controllerIndex = topControllerIndex
                            });
                        }

                        temp_motionParts.Add(new MotionPart()
                        {
                            speed = 1f,
                            mix = 1f,
                            controllerIndex = treeIndex
                        });

                        comp.motionParts = temp_motionParts.ToArray();
                        if (!ReferenceEquals(comp, topController))
                        {
                            topController = comp;
                            motionControllers.AddMotionComposite(comp, out topControllerIndex, out _);
                        }
                    }
                    else if (tiredControllerIndex >= 0 && exhaustedControllerIndex >= 0 && !tiredGroup.isAdditive && !exhaustedGroup.isAdditive)
                    {
                        var motionFields = new MotionField1D[]
{
                            new MotionField1D()
                            {
                                controllerIndex = topControllerIndex,
                                speed = 1f,
                                threshold = 1f
                            },
                            new MotionField1D()
                            {
                                controllerIndex = tiredControllerIndex,
                                speed = 1f,
                                threshold = 0.5f // half stamina
                            },
                            new MotionField1D()
                            {
                                controllerIndex = exhaustedControllerIndex,
                                speed = 1f,
                                threshold = 0f // zero stamina
                            }
};

                        topController = new BlendTree1D(field.Name + "_tree", staminaParamIndex, motionFields);
                        motionControllers.AddBlendTree1D((BlendTree1D)topController, out topControllerIndex, out _);
                    }
                    else
                    {
                        if (tiredControllerIndex >= 0)
                        {

                            var motionFields = new MotionField1D[]
                            {
                                new MotionField1D()
                                {
                                    controllerIndex = -1,
                                    speed = 0f,
                                    threshold = 1f
                                },
                                new MotionField1D()
                                {
                                    controllerIndex = tiredControllerIndex,
                                    speed = 1f,
                                    threshold = 0.5f // half stamina
                                },
                            };

                            var tree = new BlendTree1D(field.Name + "_tree", staminaParamIndex, motionFields);
                            motionControllers.AddBlendTree1D(tree, out int treeIndex, out _);

                            if (tiredGroup.isAdditive)
                            {
                                MotionComposite comp = null;
                                temp_motionParts.Clear();
                                if (topController is MotionComposite)
                                {
                                    comp = (MotionComposite)topController;
                                    if (comp.motionParts != null) temp_motionParts.AddRange(comp.motionParts);
                                }
                                else
                                {
                                    comp = new MotionComposite(topController.name, null);
                                    temp_motionParts.Add(new MotionPart()
                                    {
                                        speed = 1f,
                                        mix = 1f,
                                        controllerIndex = topControllerIndex
                                    });
                                }

                                temp_motionParts.Add(new MotionPart()
                                {
                                    speed = 1f,
                                    mix = 1f,
                                    controllerIndex = treeIndex
                                });

                                comp.motionParts = temp_motionParts.ToArray();
                                if (!ReferenceEquals(comp, topController))
                                {
                                    topController = comp;
                                    motionControllers.AddMotionComposite(comp, out topControllerIndex, out _);
                                }
                            } 
                            else
                            {
                                motionFields[0].controllerIndex = topControllerIndex;

                                topController = tree;
                                topControllerIndex = treeIndex;
                            }
                        }

                        if (exhaustedControllerIndex >= 0)
                        {

                            var motionFields = new MotionField1D[]
                            {
                                new MotionField1D()
                                {
                                    controllerIndex = -1,
                                    speed = 0f,
                                    threshold = 1f
                                },
                                new MotionField1D()
                                {
                                    controllerIndex = exhaustedControllerIndex,
                                    speed = 1f,
                                    threshold = 0f // zero stamina
                                },
                            };

                            var tree = new BlendTree1D(field.Name + "_tree", staminaParamIndex, motionFields);
                            motionControllers.AddBlendTree1D(tree, out int treeIndex, out _);

                            if (exhaustedGroup.isAdditive)
                            {
                                MotionComposite comp = null;
                                temp_motionParts.Clear();
                                if (topController is MotionComposite)
                                {
                                    comp = (MotionComposite)topController;
                                    if (comp.motionParts != null) temp_motionParts.AddRange(comp.motionParts);
                                }
                                else
                                {
                                    comp = new MotionComposite(topController.name, null);
                                    temp_motionParts.Add(new MotionPart()
                                    {
                                        speed = 1f,
                                        mix = 1f,
                                        controllerIndex = topControllerIndex
                                    });
                                }

                                temp_motionParts.Add(new MotionPart()
                                {
                                    speed = 1f,
                                    mix = 1f,
                                    controllerIndex = treeIndex
                                });

                                comp.motionParts = temp_motionParts.ToArray();
                                if (!ReferenceEquals(comp, topController))
                                {
                                    topController = comp;
                                    motionControllers.AddMotionComposite(comp, out topControllerIndex, out _);
                                }
                            }
                            else
                            {
                                motionFields[0].controllerIndex = topControllerIndex;

                                topController = tree;
                                topControllerIndex = treeIndex;
                            }
                        }
                    }

                    #endregion

                    SimplePlayerAssets.AnimGroup finalGroup = new SimplePlayerAssets.AnimGroup();
                    finalGroup.controllerIndex = topControllerIndex;

                    field.SetValue(finalIndices, finalGroup);

                }
            }

            return true;
        }

    }
}