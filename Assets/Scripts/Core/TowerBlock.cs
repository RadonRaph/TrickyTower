using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [CORE] Un bloc de la tour (sur chaque prefab de bloc, avec un Rigidbody).
///  - Dans la main : il suit la manette (kinématique).
///  - Lâché : il tombe. Dès qu'il est immobile sur la tour, il se lie aux blocs
///    qu'il touche (FixedJoint) et rapporte ses points.
///  - S'il tombe sous le socle, il est perdu.
/// Pas besoin de le modifier.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TowerBlock : MonoBehaviour
{
    public enum State { InHand, Falling, Placed }

    [Tooltip("Points gagnés quand le bloc est posé (rempli par BlockSpawner)")]
    public int score;

    [HideInInspector] public State state = State.Falling;

    // Immobile pendant ce temps (s) = posé
    const float StableDelay = 0.4f;
    // Au-delà de ce temps (s) après le lâcher, un bloc qui touche la tour est posé même s'il bouge encore
    const float MaxFallTime = 6f;
    // Vitesse maximale du lâcher (m/s) : on pose, on ne lance pas
    const float MaxReleaseSpeed = 1.5f;

    Rigidbody body;
    TowerGame game;
    BlockJuice juice;
    readonly HashSet<Collider> touching = new HashSet<Collider>();
    float stableTime;
    float releaseTime;
    Vector3 lastPosition;
    Vector3 handVelocity;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        juice = GetComponent<BlockJuice>();
    }

    public void Hold(Transform hand, TowerGame owner)
    {
        game = owner;
        state = State.InHand;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.None;
        transform.SetParent(hand, true);
        lastPosition = transform.position;
        handVelocity = Vector3.zero;
    }

    public void Release()
    {
        transform.SetParent(null, true);
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.linearVelocity = Vector3.ClampMagnitude(handVelocity, MaxReleaseSpeed);
        body.angularVelocity = Vector3.zero;
        state = State.Falling;
        releaseTime = Time.time;
        stableTime = 0f;
    }

    void Update()
    {
        if (state == State.InHand)
        {
            // Vitesse de la main (lissée), donnée au bloc quand on le lâche
            Vector3 velocity = (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            handVelocity = Vector3.Lerp(handVelocity, velocity, 0.5f);
            lastPosition = transform.position;
            return;
        }

        if (state != State.Falling || game == null)
        {
            return;
        }

        if (game.IsLost(this))
        {
            game.OnBlockLost(this);
            return;
        }

        bool onTower = HasSupport();
        bool still = body.linearVelocity.magnitude < 0.05f && body.angularVelocity.magnitude < 0.3f;
        if (onTower && still)
        {
            stableTime += Time.deltaTime;
        }
        else
        {
            stableTime = 0f;
        }

        if (stableTime > StableDelay || (onTower && Time.time - releaseTime > MaxFallTime))
        {
            Place();
        }
    }

    // Le bloc touche-t-il le socle ou un bloc déjà posé ?
    bool HasSupport()
    {
        foreach (Collider other in touching)
        {
            if (IsSupport(other))
            {
                return true;
            }
        }
        return false;
    }

    bool IsSupport(Collider other)
    {
        if (other == null || other.attachedRigidbody == null)
        {
            return false;
        }
        if (other.attachedRigidbody == game.socleBody)
        {
            return true;
        }
        TowerBlock block = other.attachedRigidbody.GetComponent<TowerBlock>();
        return block != null && block.state == State.Placed;
    }

    // Lie le bloc à tout ce qu'il touche : la tour devient un seul objet (qui peut encore casser)
    void Place()
    {
        state = State.Placed;
        List<Rigidbody> linked = new List<Rigidbody>();
        List<Vector3> points = new List<Vector3>();
        foreach (Collider other in touching)
        {
            if (!IsSupport(other) || linked.Contains(other.attachedRigidbody))
            {
                continue;
            }
            FixedJoint joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = other.attachedRigidbody;
            joint.breakForce = game.linkBreakForce;
            joint.breakTorque = game.linkBreakForce;
            linked.Add(other.attachedRigidbody);
            points.Add(other.ClosestPoint(body.worldCenterOfMass));
        }

        if (juice != null)
        {
            juice.OnPlaced(score, points);
        }
        game.OnBlockPlaced(this);
    }

    void OnCollisionEnter(Collision collision)
    {
        touching.Add(collision.collider);
    }

    void OnCollisionStay(Collision collision)
    {
        touching.Add(collision.collider);
    }

    void OnCollisionExit(Collision collision)
    {
        touching.Remove(collision.collider);
    }
}
