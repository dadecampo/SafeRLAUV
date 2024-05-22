using UnityEngine;

namespace com.zibra.common.Samples
{
    internal class ZibraFreeCamera : MonoBehaviour
    {
        public float camSens = 0.1f;
        public float camSpeed = 1.0f;
        public float fov = 60.0f;

        private Vector3 camPos;
        private Vector3 camVel;
        private Vector3 lastMouse;
        private Vector2 angVel;
        private float camPhi;
        private float camTheta;

        private void Start()
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                return;
            }

            camPos = mainCamera.transform.position;
            camVel = new Vector3(0f, 0f, 0f);
            lastMouse = Input.mousePosition;
            camPhi = mainCamera.transform.rotation.eulerAngles.x;
            camTheta = mainCamera.transform.rotation.eulerAngles.y;
            angVel = new Vector2(0, 0);
        }

        protected void Update()
        {
            UpdateCamera();
        }

        private static Vector3 GetBaseInput()
        {
            Vector3 pVelocity = new Vector3();

            if (Input.GetKey(KeyCode.W))
            {
                pVelocity += new Vector3(0, 0, 1);
            }

            if (Input.GetKey(KeyCode.S))
            {
                pVelocity += new Vector3(0, 0, -1);
            }

            if (Input.GetKey(KeyCode.A))
            {
                pVelocity += new Vector3(-1, 0, 0);
            }

            if (Input.GetKey(KeyCode.D))
            {
                pVelocity += new Vector3(1, 0, 0);
            }

            if (Input.GetKey(KeyCode.E))
            {
                pVelocity += new Vector3(0, 1, 0);
            }

            if (Input.GetKey(KeyCode.Q))
            {
                pVelocity += new Vector3(0, -1, 0);
            }

            return pVelocity;
        }

        private void UpdateCamera()
        {
            lastMouse = Input.mousePosition - lastMouse;
            lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0.0f);
            if (Input.GetMouseButtonDown(0))
            {
                lastMouse = new Vector3(0.0f, 0.0f, 0.0f);
            }
            if (Input.GetMouseButton(0))
            {
                angVel += new Vector2(lastMouse.x, lastMouse.y);
            }

            angVel *= 0.94f;
            camPhi += angVel.x;
            camTheta += angVel.y;
            lastMouse = Input.mousePosition;

            camVel += Quaternion.Euler(camPhi, camTheta, 0) * GetBaseInput() * camSpeed;

            camPos += camVel * Time.smoothDeltaTime;
            camVel *= 0.94f;

            Camera mainCamera = Camera.main;

            // update projection matrix
            if (mainCamera != null)
            {
                mainCamera.transform.position = camPos;
                mainCamera.transform.rotation = Quaternion.Euler(camPhi, camTheta, 0);
                if (!mainCamera.stereoEnabled)
                    mainCamera.fieldOfView = fov;
            }
        }
    }
}
