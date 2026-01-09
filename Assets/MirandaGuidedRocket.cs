using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Simple homing rocket for Miranda's guided skill.
public class MirandaGuidedRocket : MonoBehaviour
{
    public Transform target;
    public float speed = 20f;
    public float rotateSpeed = 360f; // degrees per second
    public float lifeTime = 8f;
    public float damage = 65f;

    private float lifeTimer = 0f;

    void Start()
    {
        lifeTimer = 0f;
        // optional: destroy after lifeTime regardless
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (target == null)
        {
            // fly straight
            transform.position += transform.forward * speed * Time.deltaTime;
            return;
        }

        // rotate towards target
        Vector3 toTarget = (target.position - transform.position).normalized;
        if (toTarget.sqrMagnitude > 0.001f)
        {
            Quaternion desired = Quaternion.LookRotation(toTarget);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, rotateSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void OnTriggerEnter(Collider other)
    {
        // avoid hitting owner or friendly checks - simple implementation: deal damage to anything with TakeDamage
        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        collision.collider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
    }
}
