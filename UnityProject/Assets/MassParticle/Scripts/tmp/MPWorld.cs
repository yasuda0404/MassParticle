using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


public unsafe class MPWorld : MonoBehaviour {

	public delegate void ParticleHandler(int numParticles, MPParticle* particles);

	public MPSolverType solverType;
	public float force = 1.0f;
	public float particleLifeTime;
	public float timeStep;
	public float deceleration;
	public float pressureStiffness;
	public float wallStiffness;
	public Vector3 coordScale;
	public bool include3DColliders = true;
	public bool include2DColliders = true;
	public int divX = 256;
	public int divY = 1;
	public int divZ = 256;
	public float particleSize = 0.08f;
	public int maxParticleNum = 200000;

	public ParticleHandler particleHandler;
	public Collider[] colliders3d;
	public Collider2D[] colliders2d;

	MPWorld()
	{
		MPNative.mphInitialize();
		particleHandler = (a, b) => DefaultParticleHandler(a, b);
	}

	void Reset()
	{
		MPKernelParams p = MPNative.mpGetKernelParams();
		transform.position = p.WorldCenter;
		transform.localScale = p.WorldSize;
		solverType 			= (MPSolverType)p.SolverType;
		particleLifeTime 	= p.LifeTime;
		timeStep 			= p.Timestep;
		deceleration 		= p.Decelerate;
		pressureStiffness 	= p.PressureStiffness;
		wallStiffness 		= p.WallStiffness;
		coordScale 			= p.Scaler;
		particleSize		= p.ParticleSize;
		maxParticleNum		= p.MaxParticles;
	}

	void Start () {
		MPNative.mpClearParticles();
	}

	unsafe void Update()
	{
		{
			MPKernelParams p = MPNative.mpGetKernelParams();
			p.WorldCenter = transform.position;
			p.WorldSize = transform.localScale;
			p.WorldDiv_x = divX;
			p.WorldDiv_y = divY;
			p.WorldDiv_z = divZ;
			p.SolverType = (int)solverType;
			p.LifeTime = particleLifeTime;
			p.Timestep = timeStep;
			p.Decelerate = deceleration;
			p.PressureStiffness = pressureStiffness;
			p.WallStiffness = wallStiffness;
			p.Scaler = coordScale;
			p.ParticleSize = particleSize;
			p.MaxParticles = maxParticleNum;
			MPNative.mpSetKernelParams(ref p);
		}

		if (include3DColliders)
		{
			colliders3d = Physics.OverlapSphere(transform.position, transform.localScale.magnitude);
			for (int i = 0; i < colliders3d.Length; ++i)
			{
				Collider col = colliders3d[i];
				if (col.isTrigger) { continue; }

				bool recv = false;
				var attr = col.gameObject.GetComponent<MPColliderAttribute>();
				if (attr)
				{
					if (!attr.sendCollision) { continue; }
					recv = attr.receiveCollision;
				}

				SphereCollider sphere = col as SphereCollider;
				CapsuleCollider capsule = col as CapsuleCollider;
				BoxCollider box = col as BoxCollider;
				int ownerid = recv ? i : -1;
				if (sphere)
				{
					MPNative.mpAddSphereCollider(ownerid, sphere.transform.position, sphere.radius * col.gameObject.transform.localScale.magnitude * 0.5f);
				}
				else if (capsule)
				{
					Vector3 e = Vector3.zero;
					float h = Mathf.Max(0.0f, capsule.height - capsule.radius*2.0f);
					float r = capsule.radius * capsule.transform.localScale.x;
					switch (capsule.direction)
					{
						case 0: e.Set(h * 0.5f, 0.0f, 0.0f); break;
						case 1: e.Set(0.0f, h * 0.5f, 0.0f); break;
						case 2: e.Set(0.0f, 0.0f, h * 0.5f); break;
					}
					Vector4 pos1 = new Vector4(e.x, e.y, e.z, 1.0f);
					Vector4 pos2 = new Vector4(-e.x, -e.y, -e.z, 1.0f);
					pos1 = capsule.transform.localToWorldMatrix * pos1;
					pos2 = capsule.transform.localToWorldMatrix * pos2;
					MPNative.mpAddCapsuleCollider(ownerid, pos1, pos2, r);
				}
				else if (box)
				{
					MPNative.mpAddBoxCollider(ownerid, box.transform.localToWorldMatrix, box.size);
				}
			}
		}
		if (include2DColliders)
		{
			Vector2 xy = new Vector2(transform.position.x, transform.position.y);
			colliders2d = Physics2D.OverlapCircleAll(xy, transform.localScale.magnitude);
			for (int i = 0; i < colliders2d.Length; ++i)
			{
				Collider2D col = colliders2d[i];
				if (col.isTrigger) { continue; }

				bool recv = false;
				var attr = col.gameObject.GetComponent<MPColliderAttribute>();
				if (attr)
				{
					if (!attr.sendCollision) { continue; }
					recv = attr.receiveCollision;
				}

				CircleCollider2D sphere = col as CircleCollider2D;
				BoxCollider2D box = col as BoxCollider2D;
				int ownerid = recv ? i : -1;
				if (sphere)
				{
					MPNative.mpAddSphereCollider(ownerid, sphere.transform.position, sphere.radius * col.gameObject.transform.localScale.x);
				}
				else if (box)
				{
					MPNative.mpAddBoxCollider(ownerid, box.transform.localToWorldMatrix, new Vector3(box.size.x, box.size.y, box.size.x));
				}
			}
		}

		MPNative.mpUpdate (Time.deltaTime);
		MPNative.mpClearCollidersAndForces();
		if (particleHandler!=null)
		{
			particleHandler(MPNative.mpGetNumParticles(), MPNative.mpGetParticles());
		}
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(transform.position, transform.localScale*2.0f);
	}


	unsafe void DefaultParticleHandler(int numParticles, MPParticle* particles)
	{
		if (force == 0.0f) { return; }
		for (int i = 0; i < numParticles; ++i)
		{
			if (particles[i].hit != -1 && particles[i].hit != particles[i].hit_prev)
			{
				Collider col = colliders3d[particles[i].hit];
				Vector3 vel = *(Vector3*)&particles[i].velocity;
				Rigidbody rb = col.GetComponent<Rigidbody>();
				if (rb)
				{
					rb.AddForceAtPosition(vel * force, *(Vector3*)&particles[i].position);
				}
			}
		}
	}
}