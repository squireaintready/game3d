using UnityEngine;

namespace TowerDefense
{
    public class Projectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private bool destroyOnHit = true;
        [SerializeField] private float hitRadius = 0.5f; // Increased for more reliable hits

        [Header("Visual")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem hitEffect;

        private Enemy target;
        private float damage;
        private float speed;
        private Vector3 lastTargetPosition;
        private float lifetime;
        private bool hasHit;

        public void Initialize(Enemy target, float damage, float speed)
        {
            this.target = target;
            this.damage = damage;
            this.speed = speed;

            if (target != null)
            {
                lastTargetPosition = target.transform.position;
            }

            // Orient toward target
            if (target != null)
            {
                Vector3 direction = (target.transform.position - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        private void Update()
        {
            if (hasHit) return;

            lifetime += Time.deltaTime;
            if (lifetime >= maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            MoveTowardTarget();
            CheckHit();
        }

        private void MoveTowardTarget()
        {
            Vector3 targetPos;

            if (target != null)
            {
                targetPos = target.transform.position + Vector3.up * 0.5f;
                lastTargetPosition = targetPos;
            }
            else
            {
                // Continue toward last known position
                targetPos = lastTargetPosition;
            }

            Vector3 direction = (targetPos - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            // Rotate to face movement direction
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void CheckHit()
        {
            if (target != null)
            {
                // Check against the actual target position we're aiming at (with Y offset)
                Vector3 targetPos = target.transform.position + Vector3.up * 0.5f;
                float distance = Vector3.Distance(transform.position, targetPos);
                if (distance <= hitRadius)
                {
                    Hit();
                }
            }
            else
            {
                // Check if we've reached the last known position
                float distance = Vector3.Distance(transform.position, lastTargetPosition);
                if (distance <= hitRadius)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void Hit()
        {
            if (hasHit) return;
            hasHit = true;

            if (target != null)
            {
                Debug.Log($"[Projectile] Hit {target.name} for {damage} damage");
                target.TakeDamage(damage);
            }

            if (hitEffect != null)
            {
                var effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration);
            }

            if (destroyOnHit)
            {
                // Detach trail if present
                if (trailRenderer != null)
                {
                    trailRenderer.transform.SetParent(null);
                    Destroy(trailRenderer.gameObject, trailRenderer.time);
                }

                Destroy(gameObject);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
    }
}
