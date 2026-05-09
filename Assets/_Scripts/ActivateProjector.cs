using UnityEngine;

public class ActivateProjector : MonoBehaviour
{
    void Start()
  {
      if (Display.displays.Length > 1)
      {
          Display.displays[1].Activate();
      }
  }
}
