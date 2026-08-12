using UnityEngine;
public class EnemyDrop : MonoBehaviour
{
    [Header("Drop Items")]
    public GameObject item1;
    public GameObject item2;
    [Header("DropChance in %")]
    [Range(0f, 100f)]
    public float dropChance = 50f;
    [Range(0f, 100f)]
    public float item1Chance = 50f;
    [Header("Drop-Physik")]
    public float upwardForce = 4f;
    public float spawnHeightOffset = 0.5f;
    [Header("Rotation")]
    public float rotationSpeed = 60f; 

    public void DropItem()
    {
        float roll = Random.Range(0f, 100f);
        if (roll > dropChance)
            return;
        float itemRoll = Random.Range(0f, 100f);
        bool isItem1 = itemRoll <= item1Chance;
        GameObject itemToDrop = isItem1 ? item1 : item2;
        if (itemToDrop != null)
        {
            Quaternion spawnRotation = isItem1 ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
            SpawnItemWithPhysics(itemToDrop, spawnRotation);
        }
    }
    private void SpawnItemWithPhysics(GameObject itemPrefab, Quaternion spawnRotation)
    {
        Vector3 spawnPos = transform.position + Vector3.up * spawnHeightOffset;
        GameObject spawnedItem = Instantiate(itemPrefab, spawnPos, spawnRotation);
        Rigidbody rb = spawnedItem.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = spawnedItem.AddComponent<Rigidbody>();
        }
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.AddForce(Vector3.up * upwardForce, ForceMode.Impulse);

        Collider enemyCollider = GetComponent<Collider>();
        Collider itemCollider = spawnedItem.GetComponent<Collider>();
        if (enemyCollider != null && itemCollider != null)
        {
            Physics.IgnoreCollision(itemCollider, enemyCollider);
        }

        ItemRotate rotateEffect = spawnedItem.AddComponent<ItemRotate>();
        rotateEffect.rotationSpeed = rotationSpeed;
    }
}