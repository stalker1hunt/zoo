using System;
using UnityEngine;

namespace ZooWorld
{
    public class AnimalActor : MonoBehaviour
    {
        private AnimalDefinition definition;
        [SerializeField] private Rigidbody body;
        [SerializeField] private SphereCollider bodyCollider;
        [SerializeField] private AnimalMovement movement;
        private IWorldBounds bounds;
        private IAnimalCollisions collisions;
        private IAnimalLifecycle lifecycle;
        private Action<AnimalActor> returnToPool;
        private const float RecoveryAcceleration = 9f;
        private Vector3 locomotionVelocity;
        private float collisionRadius;

        public AnimalDefinition Definition => definition;
        public Rigidbody Body => body;
        public AnimalMovement Movement => movement;
        public Vector3 Direction { get; private set; }
        public bool IsAlive { get; private set; }
        public uint Generation { get; private set; }

        public void ValidateConfiguration()
        {
            if (body == null || bodyCollider == null || movement == null)
                throw new InvalidOperationException($"Animal prefab '{name}' needs body, collider and movement references.");
            if (body.gameObject != gameObject || bodyCollider.gameObject != gameObject || movement.gameObject != gameObject)
                throw new InvalidOperationException($"Animal prefab '{name}' must reference its own root components.");
            if (!enabled || !movement.enabled || !bodyCollider.enabled || !gameObject.activeSelf)
                throw new InvalidOperationException($"Animal prefab '{name}' and its gameplay components must be enabled.");
            if (bodyCollider.isTrigger || bodyCollider.center != Vector3.zero || !(bodyCollider.radius > 0f))
                throw new InvalidOperationException($"Animal prefab '{name}' needs a centered, solid sphere collider with positive radius.");
            RigidbodyConstraints required = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            if (body.isKinematic || body.useGravity || (body.constraints & required) != required)
                throw new InvalidOperationException($"Animal prefab '{name}' needs a dynamic, gravity-free body with frozen Y and rotation.");
            movement.ValidateConfiguration();
        }

        internal void Activate(AnimalDefinition species, Vector3 position, Vector3 direction, IWorldBounds worldBounds,
            IAnimalCollisions collisionService, IAnimalLifecycle population, Action<AnimalActor> release)
        {
            definition = species;
            bounds = worldBounds;
            collisions = collisionService;
            lifecycle = population;
            returnToPool = release;
            Generation++;
            IsAlive = true;
            locomotionVelocity = Vector3.zero;
            collisionRadius = bodyCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
            Direction = new Vector3(direction.x, 0f, direction.z).normalized;
            Quaternion rotation = Direction.sqrMagnitude > 0f ? Quaternion.LookRotation(Direction) : Quaternion.identity;
            transform.SetPositionAndRotation(position, rotation);
            movement.Initialize(species.MovementSettings);
            movement.ResetState();
            body.position = position;
            body.rotation = rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.detectCollisions = true;
            gameObject.SetActive(true);
            body.WakeUp();
        }

        internal void ReturnToPool()
        {
            if (!IsAlive) return;
            IsAlive = false; // Claim release before callbacks can attempt a second death.
            body.detectCollisions = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            locomotionVelocity = Vector3.zero;
            // A custom movement can fail during activation. Cleanup must still return the instance.
            try { movement.ResetState(); }
            catch (Exception error) { Debug.LogException(error, this); }
            bounds = null;
            collisions = null;
            lifecycle = null;
            Action<AnimalActor> release = returnToPool;
            returnToPool = null;
            gameObject.SetActive(false);
            release(this);
        }

        internal void ApplyKnockback(Vector3 velocity)
        {
            Direction = velocity.normalized;
            body.AddForce(velocity, ForceMode.VelocityChange);
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;
            Vector3 direction = Direction;
            float speed = movement.Tick(Time.fixedDeltaTime, ref direction);
            Vector3 position = body.position;
            bounds.Constrain(ref position, ref direction, collisionRadius);
            if (position != body.position) body.position = position;
            Direction = direction.normalized;
            // Retain velocity imparted by PhysX, then gradually recover ordinary locomotion.
            Vector3 physicalVelocity = body.linearVelocity - locomotionVelocity;
            physicalVelocity = Vector3.MoveTowards(physicalVelocity, Vector3.zero, RecoveryAcceleration * Time.fixedDeltaTime);
            locomotionVelocity = Direction * speed;
            body.linearVelocity = locomotionVelocity + physicalVelocity;
            if (Direction.sqrMagnitude > 0.001f) body.MoveRotation(Quaternion.LookRotation(Direction));
        }

        private void OnCollisionEnter(Collision collision)
        {
            // The other Rigidbody is supplied by PhysX, so it cannot be a serialized prefab reference.
            if (IsAlive && collision.rigidbody != null && collision.rigidbody.TryGetComponent(out AnimalActor other))
                collisions.Resolve(this, other);
        }

        private void OnDestroy()
        {
            if (IsAlive) lifecycle?.Forget(this);
            IsAlive = false;
        }
    }
}
