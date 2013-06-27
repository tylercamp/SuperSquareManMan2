using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SSMM2.Core;
using Microsoft.Xna.Framework.Graphics;

namespace SSMM2.Effects
{
	public class ParticleSystemDescriptor
	{
		/// <summary>
		/// Initial direction of particles created in degrees. Default is 0.
		/// </summary>
		public float	EjectaDirection = 0.0F;

		/// <summary>
		/// Variation of ejecta direction upon creation in degrees; a value of 360 effects a particle system that
		/// will initially span in all directions, while 0 will effect a particle system that will only fire in
		/// a single direction. Default is 0.
		/// </summary>
		public float	MaxEjectaDirectionVariance = 0.0F;

		/// <summary>
		/// The initial speed of the particle upon creation, in pixels per second. Default of 50.
		/// </summary>
		public Range	StartSpeed = new Range(50.0F);

		/// <summary>
		/// The range of initial scale values for each particle. This single scale applies to both the X and Y
		/// scale factors. Default is 1.
		/// </summary>
		public Range	StartParticleScale = new Range(1.0F);

		/// <summary>
		/// Particle scales LERP as they age between StartParticleScale.Any and StartParticleScale.Any + ParticleScaleVariance.Any. Default
		/// is Value = 0.
		/// </summary>
		public Range	ParticleScaleVariance = new Range(0.0F);

		/// <summary>
		/// The minimum and maximum amount of time that a particle may exist. Time in seconds. Default
		/// is Value = 5.
		/// </summary>
		public Range	ParticleLife = new Range(5.0F);

		/// <summary>
		/// Acceleration of the particle towards the particle system's position. The first value selected
		/// from the range is used for the entirety of the particle's lifetime. Default is 0.
		/// </summary>
		public Range	RadialAcceleration = new Range(0.0F);

		/// <summary>
		/// Acceleration of the particle tangent to the radial acceleration. A single value is chosen from
		/// the range when the particle is created and is used for the particle's lifetime. Default is 0.
		/// </summary>
		public Range	TangentialAcceleration = new Range(0.0F);

		/// <summary>
		/// The factor by which Settings.Gravity is applied. A single value is selected from the range
		/// and is used for the entirety of the particle's lifetime. Default is 0 (no gravity).
		/// </summary>
		public Range	GravityStrengthFactor = new Range(0.0F);

		/// <summary>
		/// The system will automatically LERP the blend color of each particle between the colors defined
		/// in ColorRanges based on its current age and the maximum lifetime of the particle. Default is a
		/// single white color (white for its entire lifetime).
		/// </summary>
		public Color[]	ColorRanges = { new Color(255, 255, 255, 255) };

		/// <summary>
		/// When true, the particle can react to its collision environment. Default is false. When true,
		/// all particles are assumed to have a spherical collision mask with a diameter of (width + height)/2.
		/// </summary>
		public bool		Interactive = false;

		/// <summary>
		/// Whether or not the particles generated should be influenced by the Settings.MaxVelocity variable.
		/// </summary>
		public bool		IgnoreVelocityLimits = false;



		public enum PhysicsMode
		{
			Bounce,
			Destroy
		}

		/// <summary>
		/// How a particle should respond upon colliding with a solid object
		/// </summary>
		public PhysicsMode InteractionMode	= PhysicsMode.Bounce;

		public enum CollisionDetectionMode
		{
			BoundingBox,
			CenterOfParticle
		}

		/// <summary>
		/// What metric is used to determine if there is a collision
		/// </summary>
		public CollisionDetectionMode CollisionMode = CollisionDetectionMode.CenterOfParticle;

		// TODO: Implement
		/// <summary>
		/// The range of factors that will be used to calculate a particle's bounce velocity based on its current velocity.
		/// Valid only if Interactive is true. Default is Value = 0.5.
		/// </summary>
		public Range	BounceFactor = new Range(0.5F);

		/// <summary>
		/// The type of blending to be used for the particles within this system.
		/// </summary>
		public BlendState Blend = BlendState.Additive;

		/// <summary>
		/// The amount of particles that will be created when the system is invoked. Default
		/// of Value = 15.
		/// </summary>
		public Range	BurstSize = new Range(15.0f);

		/// <summary>
		/// The sprite to be used for every particle within the system. Default value of null.
		/// </summary>
		public Texture2D ParticleSprite = null;

		/// <summary>
		/// Number of particles emitted per second by the system
		/// </summary>
		public float EmitCount = 20.0F;

		public bool AlphaFromAge = true;
	}

	/// <summary>
	/// Particle objects are not to be added to a global scene. They are instead to be maintained within scenes
	/// owned by ParticleSystem objects.
	/// </summary>
	public class Particle : Core.KinematicEntity
	{
		ParticleSystem m_Parent;
		public ParticleSystem Parent
		{
			get
			{
				return m_Parent;
			}
		}

		public float Age = 0.0F;
		public float MaxLife
		{
			get;
			private set;
		}

		private Vector2 m_TargetScale;
		private Vector2 m_StartScale;
		private float m_TangentialFactor;
		private float m_RadialFactor;
        private float m_GravityStrength;

		public ParticleSystemDescriptor Descriptor
		{
			private set;
			get;
		}

		
		public Particle(Core.Scene ownerScene, ParticleSystem parent, Vector2 position)
			: base (ownerScene)
		{
			m_Parent = parent;

			Position = position;

			ParticleSystemDescriptor desc = parent.Descriptor;
			MaxLife = desc.ParticleLife.Any;
			m_StartScale = new Vector2(desc.StartParticleScale.Any);
			m_TargetScale = m_StartScale + new Vector2 (desc.ParticleScaleVariance.Any);

			Descriptor = parent.Descriptor;

			//	We override with a custom implementation
			HasGravity = false;

			float startDirection = desc.EjectaDirection + (float)Utility.Random.NextDouble() * desc.MaxEjectaDirectionVariance - desc.MaxEjectaDirectionVariance / 2.0F;
			if (desc.StartSpeed.Any == 0.0F)
				HasGravity = HasGravity;
			Velocity = new Vector2(
				(float)Math.Cos(MathHelper.ToRadians(startDirection)),
				(float)-Math.Sin(MathHelper.ToRadians(startDirection))
				) * desc.StartSpeed.Any;

			m_TangentialFactor = desc.TangentialAcceleration.Any;
			m_RadialFactor = desc.RadialAcceleration.Any;
            m_GravityStrength = desc.GravityStrengthFactor.Any;

			IgnoreVelocityLimits = desc.IgnoreVelocityLimits;

			BlendColor = desc.ColorRanges[0];

			Solid = parent.Descriptor.Interactive;
		}

		public override void Update(float timeDelta)
		{
			Vector2 previousPosition = Position;

			base.Update(timeDelta);

			if (Descriptor.Interactive)
			{
				Core.Entity collidedEntity;
				if (Descriptor.CollisionMode == ParticleSystemDescriptor.CollisionDetectionMode.BoundingBox)
					collidedEntity = OwnerScene.CollisionWorld.EntityPlaceFree<CollisionType.Solid>(this, Position + m_Parent.Position);
				else
					collidedEntity = OwnerScene.CollisionWorld.PointFree<CollisionType.Solid>(this, Position + m_Parent.Position);

				if (collidedEntity != null)
				{
					switch (Descriptor.InteractionMode)
					{
						case ParticleSystemDescriptor.PhysicsMode.Bounce:
							if (Descriptor.CollisionMode == ParticleSystemDescriptor.CollisionDetectionMode.BoundingBox)
								collidedEntity = OwnerScene.CollisionWorld.EntityPlaceFree<CollisionType.Solid>(this, new Vector2(previousPosition.X, Position.Y) + m_Parent.Position);
							else
								collidedEntity = OwnerScene.CollisionWorld.PointFree<CollisionType.Solid>(this, new Vector2(previousPosition.X, Position.Y) + m_Parent.Position);

							if (collidedEntity != null)
								Velocity.Y *= -1;



							if (Descriptor.CollisionMode == ParticleSystemDescriptor.CollisionDetectionMode.BoundingBox)
								collidedEntity = OwnerScene.CollisionWorld.EntityPlaceFree<CollisionType.Solid>(this, new Vector2(Position.X, previousPosition.Y) + m_Parent.Position);
							else
								collidedEntity = OwnerScene.CollisionWorld.PointFree<CollisionType.Solid>(this, new Vector2(Position.X, previousPosition.Y) + m_Parent.Position);
							if (collidedEntity != null)
								Velocity.X *= -1;
							break;

						case ParticleSystemDescriptor.PhysicsMode.Destroy:
							this.Destroy();
							break;
					}
				}
			}

			Age += timeDelta;

			if (Age > MaxLife)
				Age = MaxLife;

			ApplyLerpParticleScale(Age / MaxLife, m_StartScale, m_TargetScale);
			ApplyRadialAcceleration(m_Parent.ClosestPointInBoundsToPoint(Position), m_RadialFactor);
			ApplyGravity(Settings.Gravity, m_GravityStrength);
			ApplyTangentialAcceleration(Vector2.Zero, m_TangentialFactor);
			ApplyColorBlending(Age / MaxLife, Descriptor.ColorRanges);

			if (Age >= MaxLife)
				this.Destroy();
		}

		public override void LoadContent()
		{
			SetSpriteFromResource(m_Parent.Descriptor.ParticleSprite);
		}

		public override void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch)
		{
			Position += m_Parent.Position;
			Color oldColor = BlendColor;
			BlendColor *= (m_Parent.BlendColor.A / 255.0F);

			DrawSelf(spriteBatch);

			BlendColor = oldColor;
			Position -= m_Parent.Position;
		}

		#region Particle Behaviors
		void ApplyLerpParticleScale(float ageNormal, Vector2 startScale, Vector2 endScale)
		{
			Scale = Vector2.Lerp(startScale, endScale, ageNormal);
		}

		void ApplyRadialAcceleration(Vector2 centerPosition, float accelerationFactor)
		{
			if (accelerationFactor == 0.0F)
				return;

			Vector2 vectorToCenter = centerPosition - Position;
			if (vectorToCenter != Vector2.Zero)
			{
				vectorToCenter.Normalize();
				ApplyForce(vectorToCenter * accelerationFactor, new Vector2(float.MaxValue, float.MaxValue));
			}
		}

		void ApplyGravity(Vector2 gravity, float strengthFactor)
		{
			ApplyForce(gravity * strengthFactor);
		}

		void ApplyTangentialAcceleration(Vector2 centerPosition, float accelerationFactor)
		{
			Vector2 vectorToCenter = centerPosition - Position;

			if (vectorToCenter == Vector2.Zero)
				return;

			Vector2 perpVector = new Vector2(vectorToCenter.Y, -vectorToCenter.X);
			perpVector.Normalize();
			ApplyForce(perpVector * accelerationFactor);
		}

		void ApplyColorBlending(float ageNormal, Color[] colorRange)
		{
			int firstIndex = (int)Math.Floor(ageNormal * (colorRange.Length - 1));
			int lastIndex = (int)Math.Ceiling(ageNormal * (colorRange.Length - 1));
			
			BlendColor = Color.Lerp(colorRange[firstIndex], colorRange[lastIndex], ((ageNormal * colorRange.Length) - firstIndex) / lastIndex);

			float newAlpha = BlendColor.A / 255.0F;
			if (Descriptor.AlphaFromAge)
				newAlpha *= (1.0F - ageNormal);
			BlendColor.A = 255;
			BlendColor *= newAlpha;
		}
		#endregion
	}

	public class ParticleSystem : Core.Entity
	{
		public ParticleSystemDescriptor Descriptor;

		Core.Scene m_ParticleScene;

		public bool IsStreaming
		{
			get;
			private set;
		}
		float m_TotalAge = 0.0F;
		uint m_TotalStreamedParticles = 0;

		internal Vector2 ClosestPointInBoundsToPoint(Vector2 point)
		{
			Vector2 result = point;
			if (result.X < -Size.X / 2.0F)
				result.X = -Size.X / 2.0F;
			if (result.X > Size.X / 2.0F)
				result.X = Size.X / 2.0F;
			if (result.Y < -Size.Y / 2.0F)
				result.Y = Size.Y / 2.0F;
			if (result.Y > Size.Y / 2.0F)
				result.Y = Size.Y / 2.0F;

			return result;
		}

		public CollisionWorld CollisionSource
		{
			get
			{
				return m_ParticleScene.CollisionWorld;
			}

			set
			{
				m_ParticleScene.CollisionWorld = value;
			}
		}

		/// <summary>
		/// Size is centered around the particle system's position. A size of zero means a point particle system. Default
		/// is zero.
		/// </summary>
		public Vector2 Size = Vector2.Zero;

		public ParticleSystem(Core.Scene ownerScene)
			: base (ownerScene)
		{
			m_ParticleScene = new Core.Scene();
			m_ParticleScene.Camera = ownerScene.Camera;
		}

		public uint ParticleCount
		{
			get
			{
				return m_ParticleScene.EntityCount;
			}
		}

		public void Clear()
		{
			m_ParticleScene = new Core.Scene();
		}

		public override void Destroy()
		{
			base.Destroy();
			Clear();
		}

		public void Stream()
		{
			IsStreaming = true;
			m_TotalAge = 0.0F;
		}

		public void StopStreaming()
		{
			IsStreaming = false;
		}

		public void Burst()
		{
			int particleCount = (int)Descriptor.BurstSize.Any;
			for (int i = 0; i < particleCount; i++)
			{
				Vector2 particlePosition = Vector2.Zero;
				particlePosition.X += Utility.Random.Next(-(int)Size.X / 2, (int)Size.X / 2);
				particlePosition.Y += Utility.Random.Next(-(int)Size.Y / 2, (int)Size.Y / 2);
				(new Particle(m_ParticleScene, this, particlePosition)).Depth = Depth;
			}
		}

		public override void LoadContent()
		{
			m_ParticleScene.LoadContent();
		}

		public override void Update(float timeDelta)
		{
			base.Update(timeDelta);
			if (IsStreaming)
			{
				m_TotalAge += timeDelta;

				uint expectedTotalParticles = (uint)(m_TotalAge * Descriptor.EmitCount);
				while (m_TotalStreamedParticles < expectedTotalParticles)
				{
					Burst();
					++m_TotalStreamedParticles;
				}
			}
			m_ParticleScene.Update(timeDelta);
			m_ParticleScene.HandleDeferredOperations();
		}

		public override void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch)
		{
			GraphicsDevice graphics = SquareManMan.Instance.GraphicsDevice;

			BlendState prevBlend = graphics.BlendState;
			graphics.BlendState = Descriptor.Blend;
			m_ParticleScene.Draw(spriteBatch, primitiveBatch, false);
			graphics.BlendState = prevBlend;
		}
	}
}
