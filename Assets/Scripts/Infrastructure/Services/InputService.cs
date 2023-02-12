
using UnityEngine;

namespace Services
{
    public class InputService
    {
        //private const string Horizontal = "Horizontal";
        //private const string Vertical = "Vertical";

       // public Vector3 Axis => new Vector2(Input.GetAxis(Horizontal), Input.GetAxis(Vertical));
        public bool AttackButtonHeldDown => Input.GetButton("Fire1");
        public bool DashButtonDown => Input.GetKeyDown(KeyCode.LeftShift);
        public bool RespectButtonPressed => Input.GetKeyDown(KeyCode.F);
    }
}