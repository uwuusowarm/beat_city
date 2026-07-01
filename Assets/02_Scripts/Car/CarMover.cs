using UnityEngine;

public class CarMover : MonoBehaviour
{
    [Header("Fahrt")]
    public float speed = 10f;

    [Header("Lifetime")]
    public float lifeTime = 5f;
  

    [Header("Räder")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    public float wheelRotationSpeed = 500f;

    [Header("Federung")]
    public float suspensionHeight = 0.05f;
    public float smoothTime = 0.1f;

    private Vector3 velocity;
    private Vector3 targetPos;
    private float timer = 0f;

    void Start()
    {
        targetPos = transform.localPosition;
        
    }


    void Update()
    {
        if(timer < lifeTime)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);

            ApplySuspension();
            RotateWheels();
            timer += Time.deltaTime;

        }
        else
        {
            Destroy(gameObject);

        }
     
    }

    void ApplySuspension()
    {
        float bob = Mathf.Sin(Time.time * 3f) * suspensionHeight;

        Vector3 desired = new Vector3(
            transform.localPosition.x,
            targetPos.y + bob,
            transform.localPosition.z
        );

        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            desired,
            ref velocity,
            smoothTime
        );
    }

    void RotateWheels()
    {
        float rotation = wheelRotationSpeed * Time.deltaTime;

        frontLeftWheel.Rotate(0f, 0f, rotation);
        frontRightWheel.Rotate(0f, 0f, rotation);
        rearLeftWheel.Rotate(0f, 0f, rotation);
        rearRightWheel.Rotate(0f, 0f, rotation);
    }
}
