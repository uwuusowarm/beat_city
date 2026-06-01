using UnityEngine;

public enum PickupType
{
    HealPotion,
    Pistol
}

public class Pickup : MonoBehaviour
{
    [SerializeField] private PickupType type;
    [SerializeField] private int healAmount = 25;
    [SerializeField] Health health;

    private void OnTriggerEnter(Collider other)
    {
        switch (type)
        {
            case PickupType.HealPotion:
                if (health == null)
                {
                    health = other.GetComponent<Health>();
                }
                if (health != null)
                {
                    health.Heal(healAmount);
                    Destroy(gameObject);
                }
                break;
            case PickupType.Pistol:
                Debug.Log("Pistole aufgehoben");
                Destroy(gameObject);
                break;


        }
    }
}
