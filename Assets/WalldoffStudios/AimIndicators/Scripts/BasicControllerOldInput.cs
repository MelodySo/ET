using UnityEngine;
using WalldoffStudios.Indicators;
using WalldoffStudios.Joysticks;

namespace WalldoffStudios
{
    public class BasicControllerOldInput : MonoBehaviour
    {
        [SerializeField] private AimJoystick aimJoystick = null;
        [SerializeField] private JoystickBase moveJoystick = null;
        
        [SerializeField] private IndicatorController indicatorController = null;
        [SerializeField] private Transform avatarTransform = null;
        [SerializeField] private float speed = 5.0f;
        [SerializeField] private float rotationSmoothing = 1.0f;
        [SerializeField] private bool is2D = false;
        
        private Transform rotationTransform;
        private Transform[] childTransforms;

        private Vector2 moveDir;
        private Vector2 aimDir;
        
        private CharacterController characterController;
        private Rigidbody2D rigidBody2D;
        
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Physics.autoSimulation = true;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            
            rotationTransform = indicatorController.transform;
            
            if (is2D == true)
            {
                rigidBody2D = GetComponent<Rigidbody2D>();

                int children = rotationTransform.childCount;
                childTransforms = new Transform[children];
                
                for (int i = 0; i < children; i++)
                {
                    Transform child = rotationTransform.GetChild(i);
                    childTransforms[i] = child;
                    Vector3 scale = child.localScale;
                    scale.x = -1;
                    child.localScale = scale;
                }
            }
            else
            {
                characterController = GetComponent<CharacterController>();    
            }
            
            indicatorController.SetWorldSpace(is2D);
            
            aimJoystick.Initialize();
            moveJoystick.Initialize();
        }

        private void OnEnable()
        {
            moveJoystick.JoystickUpdateDirection += UpdateMoveDir;
            aimJoystick.JoystickToggleAim += ToggleAim;
            aimJoystick.JoystickUpdateDirection += UpdateAimDir;
            aimJoystick.JoystickAttack += Attack;
        }

        private void OnDisable()
        {
            moveJoystick.JoystickUpdateDirection -= UpdateMoveDir;
            aimJoystick.JoystickToggleAim -= ToggleAim;
            aimJoystick.JoystickUpdateDirection -= UpdateAimDir;
            aimJoystick.JoystickAttack -= Attack;
        }
        
        private void Update()
        {
            SetMoveDir();
            
            if(Input.GetKeyDown(KeyCode.E)) ShotIndicator();
            if(Input.GetKeyDown(KeyCode.R)) IndicatorResetFillAmount();
            if(Input.GetKeyDown(KeyCode.G)) IndicatorToggleTexture(true);
            if(Input.GetKeyDown(KeyCode.F)) IndicatorToggleTexture(false);
            
            RotateAim();
        }

        private void SetMoveDir()
        {
            moveDir = new Vector2();
            if(Input.GetKey(KeyCode.A))
            {
                moveDir.x = -1.0f;
            }
            if(Input.GetKey(KeyCode.D))
            {
                moveDir.x = 1.0f;
            }
            if(Input.GetKey(KeyCode.W))
            {
                moveDir.y = 1.0f;
            }
            if(Input.GetKey(KeyCode.S))
            {
                moveDir.y = -1.0f;
            }
            moveDir.Normalize();
        }

        private void ShotIndicator()
        {
            indicatorController.ShotIndicator();
        }

        private void UpdateMoveDir(Vector2 dir)
        {
            moveDir = dir;
        }

        private void UpdateAimDir(Vector2 dir)
        {
            aimDir = dir;
            indicatorController.UpdateIndicatorAim(dir);
        }

        private void Attack(Vector2 aimDir)
        {
        }

        private void ToggleAim(bool toggle)
        {
            indicatorController.ToggleAim(toggle);
        }

        private void FixedUpdate()
        {
            MovePlayer();
        }

        private void MovePlayer()
        {
            if (is2D == true)
            {
                if(rigidBody2D != null) rigidBody2D.velocity = moveDir * (speed * Time.fixedDeltaTime);
            }
            else
            {
                characterController.Move(new Vector3(moveDir.x, 0, moveDir.y) * (speed * Time.fixedDeltaTime));   
            }
        }

        private void RotateAim()
        {
            float rotAngle = Mathf.Atan2(aimDir.x, aimDir.y) * Mathf.Rad2Deg;
            Quaternion currentRot = rotationTransform.localRotation;
            Quaternion targetRot = Quaternion.Euler(0.0f, rotAngle, 0.0f); 
            
            if (is2D == true)
            {
                targetRot = Quaternion.Euler(0.0f,0.0f,0.0f - rotAngle);
                
                //rotates indicators (2D)
                foreach (Transform child in childTransforms)
                {
                    Vector3 euler = child.localEulerAngles;
                    currentRot = Quaternion.Euler(euler);
                    child.localRotation = Quaternion.Slerp(currentRot, targetRot, rotationSmoothing * Time.deltaTime);
                }
                
                //rotates avatar (2D)
                currentRot = avatarTransform.localRotation;
                targetRot = Quaternion.Euler(0.0f, 0.0f, -180.0f - rotAngle);
                avatarTransform.localRotation = Quaternion.Slerp(currentRot, targetRot, rotationSmoothing * Time.deltaTime);
            }
            else
            {
                //rotates indicator parent (3D)
               rotationTransform.localRotation = Quaternion.Slerp(currentRot, targetRot, rotationSmoothing * Time.deltaTime);
               
               //rotates avatar (3D)
               avatarTransform.localRotation = Quaternion.Slerp(avatarTransform.localRotation, targetRot, rotationSmoothing * Time.deltaTime);
            }
        }

        private void IndicatorResetFillAmount()
        {
            indicatorController.IndicatorResetFillAmount();
        }

        private void IndicatorToggleTexture(bool next)
        {
            indicatorController.IndicatorToggleTexture(next);
        }
    }
}