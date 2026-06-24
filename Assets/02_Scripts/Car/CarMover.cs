using UnityEngine;

public class CarMover : MonoBehaviour
{
    public float speed = 10f;

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        
        if (transform.position.x > 80f)
        {
            
            Destroy(gameObject);
        }
    }
}
