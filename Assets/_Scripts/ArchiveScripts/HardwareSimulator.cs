using UnityEngine;

public class HardwareSimulator : MonoBehaviour
{
    public static HardwareSimulator Instance { get; private set; }

    public int Level = 0;
    public int isTouching = 0;
    public int isGrounding = 0;
    public int isBlowing = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        // Simulate water level with number keys 0-3.
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Level = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Level = 1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Level = 2;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Level = 3;
        }

        // Hold Space to simulate fingertip touching water.
        isTouching = Input.GetKey(KeyCode.Space) ? 1 : 0;

        // Hold Enter to simulate both hands grounding.
        isGrounding = Input.GetKey(KeyCode.Return) ? 1 : 0;

        // Trigger blow as a one-frame pulse when pressing B.
        isBlowing = Input.GetKeyDown(KeyCode.B) ? 1 : 0;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10f, 10f, 280f, 120f), "Hardware Simulator");
        GUI.Label(new Rect(10f, 35f, 280f, 25f), "Level: " + Level);
        GUI.Label(new Rect(10f, 55f, 280f, 25f), "isTouching: " + isTouching);
        GUI.Label(new Rect(10f, 75f, 280f, 25f), "isGrounding: " + isGrounding);
        GUI.Label(new Rect(10f, 95f, 280f, 25f), "isBlowing: " + isBlowing);
    }
}
