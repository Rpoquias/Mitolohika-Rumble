using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Player : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float gravity = -9.81f;
    
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TextMeshProUGUI speedText;

    CharacterController controller;
    float yVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Set slider default value
        speedSlider.value = speed;

        // Update text at start
        UpdateSpeed(speed);

        // Listen to slider changes
        speedSlider.onValueChanged.AddListener(UpdateSpeed);
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (controller.isGrounded && yVelocity < 0)
        {
            yVelocity = -2f;
        }

        yVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = move * speed;
        finalMove.y = yVelocity;

        controller.Move(finalMove * Time.deltaTime);
    }

    void UpdateSpeed(float value)
    {
        speed = value;

        // Update UI text
        speedText.text = "Speed: " + value.ToString("F1");
    }



    public void Pause()
    {
        Time.timeScale = 0f;
    }

    public void UnPause()
    {
        Time.timeScale = 1f;
    }
}