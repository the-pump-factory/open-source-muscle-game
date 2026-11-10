using System.Collections.Generic;
using UnityEngine;


namespace nugen.modding
{
    public class TransformWidget : Widget
    {

        public static bool isGlobal = false;

        public static bool lockX = false;

        public static bool lockY = false;

        public static bool lockZ = false;

        public static float translationSensitivity = 0.01f;

        public static int completeMouseButton = 0;

        public static int cancelMouseButton = 1;

        public static KeyCode translationKey = KeyCode.G;

        public static KeyCode rotationKey = KeyCode.R;

        public static KeyCode scaleKey = KeyCode.S;

        public static KeyCode shiftAxisKey = KeyCode.LeftShift;

        public static bool translating = false;

        public static bool rotating = false;

        public static bool scaling = false;

        public static bool IsActive()
        {

            return translating || rotating || scaling;

        }

        public static bool cancel = false;

        public float radius = 0.05f;

        public bool rescale = false;

        private static int translationFrame = -1;

        public bool selected = false;

        private bool selected_ = true;

        private bool translating_ = false;

        private bool rotating_ = false;

        private bool scaling_ = false;

        new MeshRenderer renderer;

        new public Collider collider;

        Vector3 previousPosition;

        Quaternion previousRotation;

        Vector3 previousScale;

        public int lastUpdateFrame = -1;

        private static bool isSubscribed = false;

        public static void Subscribe(bool subscribe = true)
        {

            try
            {

                if (isSubscribed)
                {

                    ModdingInterfaceBehaviour ui = ModUI.behaviour;

                    if (ui == null || subscribe) return;

                    ui.OnRaycastClick -= OnClick;

                    isSubscribed = false;

                }
                else
                {

                    ModdingInterfaceBehaviour ui = ModUI.GetBehaviour();

                    if (ui == null || !subscribe) return;

                    ui.OnRaycastClick += OnClick;

                    isSubscribed = true;

                }

            }
            catch { }

        }

        public static void OnClick(int button, ModUI.ClickType clickType, bool clickedObject, RaycastHit hit)
        {

            if (clickType != ModUI.ClickType.Down || Cursor.lockState != CursorLockMode.None) return;

            if (button > 1) return;

            if (hit.collider != null)
            {

                for (int a = 0; a < widgets.Count; a++)
                {

                    TransformWidget widget = widgets[a];

                    if (widget.collider != hit.collider) continue;

                    widget.selected = button == 0;

                    return;

                }

            }

            if (button == 1) for (int a = 0; a < widgets.Count; a++) widgets[a].selected = false;

        }

        public static List<TransformWidget> widgets = new List<TransformWidget>();

        private void Awake()
        {

            widgets.Add(this);

        }

        private void OnDestroy()
        {

            widgets.Remove(this);

        }

        public GameObject primitive;

        public void Initialize()
        {

            if (primitive == null) primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            primitive.transform.SetParent(transform, false);

            primitive.transform.localPosition = Vector3.zero;

            primitive.transform.localScale = Vector3.one * 0.01f;

            renderer = primitive.AddOrGetComponent<MeshRenderer>();

            renderer.sharedMaterial = ModUI.GetDefaultMaterial();

            collider = primitive.GetComponent<Collider>();

        }

        private void Start()
        {

            Initialize();

        }

        private void Update()
        {

            if (primitive == null) Initialize();

            primitive.transform.localScale = Vector3.one * (rescale ? Mathf.Min(radius, (Camera.main.transform.position - transform.position).magnitude * radius) : radius);

            if ((!translating && CameraControl.instance != null) ? CameraControl.instance.toggle : false) return;

            if (translationFrame != Time.frameCount)
            {

                translationFrame = Time.frameCount;

                Subscribe(true);

                bool cmb = Input.GetMouseButtonDown(completeMouseButton);

                bool rmb = Input.GetMouseButtonDown(cancelMouseButton);

                if (Input.GetKeyDown(translationKey) || (translating ? (cmb || rmb) : false)) {

                    if (translating) cancel = rmb; else cancel = false;

                    translating = !translating;

                    if (rotating) rotating = false;

                    if (scaling) scaling = false;

                    Cursor.lockState = translating ? CursorLockMode.Locked : CursorLockMode.None;
                
                }

                if (Input.GetKeyDown(rotationKey) || (rotating ? (cmb || rmb) : false))
                {

                    if (rotating) cancel = rmb; else cancel = false;

                    rotating = !rotating;

                    if (translating) translating = false;

                    if (scaling) scaling = false;

                    Cursor.lockState = rotating ? CursorLockMode.Locked : CursorLockMode.None;

                }

                if (Input.GetKeyDown(scaleKey) || (scaling ? (cmb || rmb) : false))
                {

                    if (scaling) cancel = rmb; else cancel = false;

                    scaling = !scaling;

                    if (translating) translating = false;

                    if (rotating) rotating = false;

                    Cursor.lockState = scaling ? CursorLockMode.Locked : CursorLockMode.None;

                }

                if (translating || rotating || scaling)
                {

                    translationSensitivity = Mathf.Max(0.00001f, translationSensitivity + Input.mouseScrollDelta.y * 0.0001f);

                }
                else
                {

                    lockX = lockY = lockZ = false;

                }

                if (Input.GetKeyDown(KeyCode.X))
                {

                    if (!lockX && !lockY && !lockZ)
                    {

                        lockX = lockY = lockZ = true;

                    }

                    lockX = !lockX;

                }

                if (Input.GetKeyDown(KeyCode.Y))
                {

                    if (!lockX && !lockY && !lockZ)
                    {

                        lockX = lockY = lockZ = true;

                    }

                    lockY = !lockY;

                }

                if (Input.GetKeyDown(KeyCode.Z))
                {

                    if (!lockX && !lockY && !lockZ)
                    {

                        lockX = lockY = lockZ = true;

                    }

                    lockZ = !lockZ;

                }

            }

            if (selected != selected_)
            {

                selected_ = selected;

                if (renderer == null) primitive.AddOrGetComponent<MeshRenderer>();

                renderer.sharedMaterial = selected ? ModUI.GetSelectionMaterial() : ModUI.GetDefaultMaterial();

                if (selected && translating)
                {

                    previousPosition = transform.position;

                    previousRotation = transform.rotation;

                }

            }

            if (translating_ != translating)
            {

                translating_ = translating;

                if (selected)
                {

                    if (!translating)
                    {

                        if (cancel) transform.position = previousPosition;

                    }
                    else previousPosition = transform.position;

                }

            }

            if (rotating_ != rotating)
            {

                rotating_ = rotating;

                if (selected)
                {

                    if (!rotating)
                    {

                        if (cancel) transform.rotation = previousRotation;

                    }
                    else previousRotation = transform.rotation;

                }

            }

            if (scaling_ != scaling)
            {

                scaling_ = scaling;

                if (selected)
                {

                    if (!rotating)
                    {

                        if (cancel) transform.localScale = previousScale;

                    }
                    else previousScale = transform.localScale;

                }

            }

            if (selected)
            {

                float mx = Input.GetAxis("Mouse X");
                float my = Input.GetAxis("Mouse Y");

                Vector3 up = Vector3.up;

                Vector3 right = Vector3.right;

                Vector3 forward = Vector3.forward;

                if (translating)
                {

                    Vector3 translation = (((Input.GetKey(shiftAxisKey) ? forward : up) * my) + (right * mx)) * translationSensitivity;

                    translation = new Vector3(lockX ? 0 : translation.x, lockY ? 0 : translation.y, lockZ ? 0 : translation.z);

                    transform.position = transform.position + (isGlobal ? Quaternion.identity : Camera.main.transform.rotation) * translation;

                    if (translation.sqrMagnitude > 0) lastUpdateFrame = Time.frameCount;

                } 
                else if (rotating)
                {

                    Vector3 rotation = (((Input.GetKey(shiftAxisKey) ? forward : up) * my) + (right * mx)) * translationSensitivity * 100;

                    rotation = new Vector3(lockX ? 0 : rotation.x, lockY ? 0 : rotation.y, lockZ ? 0 : rotation.z);

                    transform.rotation = Quaternion.Euler((isGlobal ? Quaternion.identity : Camera.main.transform.rotation) * rotation) * transform.rotation;

                    if (rotation.sqrMagnitude > 0) lastUpdateFrame = Time.frameCount;

                } 
                else if (scaling)
                {

                    Vector3 scale = (((Input.GetKey(shiftAxisKey) ? forward : up) * my) + (right * mx)) * translationSensitivity;

                    scale = new Vector3(lockX ? 0 : scale.x, lockY ? 0 : scale.y, lockZ ? 0 : scale.z);

                    transform.localScale = transform.localScale + Quaternion.Inverse(transform.rotation) * (isGlobal ? Quaternion.identity : Camera.main.transform.rotation) * scale;

                    if (scale.sqrMagnitude > 0) lastUpdateFrame = Time.frameCount;

                }

            }

        }

    }

}
