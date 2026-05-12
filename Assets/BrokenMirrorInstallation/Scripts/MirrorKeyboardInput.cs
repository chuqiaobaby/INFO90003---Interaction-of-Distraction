using UnityEngine;

namespace BrokenMirrorInstallation
{
    [DisallowMultipleComponent]
    public sealed class MirrorKeyboardInput : MonoBehaviour
    {
        [SerializeField] private MirrorFractureController controller;

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<MirrorFractureController>();
            }
        }

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                controller.SetFractureState(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                controller.SetFractureState(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                controller.SetFractureState(3);
            }

            if (Input.GetKeyDown(KeyCode.R) || Input.GetKey(KeyCode.Space))
            {
                controller.ResetMirror();
            }
        }
    }
}
