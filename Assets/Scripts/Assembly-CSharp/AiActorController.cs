using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class AiActorController : ActorController
{
	public struct AiParameters
	{
		public float LEAD_SWAY_MAGNITUDE;

		public float LEAD_NOISE_MAGNITUDE;

		public float SWAY_MAGNITUDE;

		public float AIM_BASE_SWAY;

		public float AIM_MAX_SWAY;

		public float AI_FIRE_RECTANGLE_BOUND;
	}

	private const float AI_TICK_PERIOD = 0.2f;

	private const float AI_KEEP_TARGET_TIME = 0.5f;

	private const float AI_ORDER_PERIOD = 0.5f;

	private const float AI_VEHICLE_PERIOD = 0.5f;

	private const float VEHICLE_STUCK_DISTANCE = 0.4f;

	private const float VEHICLE_STUCK_TIME = 1.5f;

	private const float VEHICLE_STUCK_RECOVER_TIME = 1f;

	private const float MAX_RECENT_ANTI_STUCK_EVENTS = 2f;

	private const float ANTI_STUCK_EVENT_LIFETIME = 30f;

	private const int CAR_UNEVEN_SURFACE_PENALTY = 100000;

	private const int GRAPH_MASK_ON_FOOT = 1;

	private const int GRAPH_MASK_BOAT = 2;

	private const int GRAPH_MASK_CAR = 4;

	private const float HELICOPTER_TARGET_FLIGHT_HEIGHT = 40f;

	private const float HELICOPTER_HEIGHT_EXTRAPOLATION_TIME = 3f;

	private const float HELICOPTER_MAX_PITCH = 25f;

	private const float HELICOPTER_MAX_ROLL = 25f;

	private const float HELICOPTER_ATTACK_RANGE = 200f;

	private const float AI_WEAPON_FAST_TICK_PERIOD = 0.05f;

	private const float AI_WEAPON_SLOW_TICK_PERIOD = 0.5f;

	private const float AI_WEAPON_SLOW_TICK_DISTANCE = 40f;

	public const float TAKING_FIRE_MAX_DISTANCE = 2.5f;

	private const float FOOT_BLOCK_SPHERECAST_RADIUS = 0.5f;

	private const float FOOT_CHECK_BLOCKER_AHEAD_RANGE = 2f;

	private const int FOOT_BLOCK_MASK = 4096;

	private const float VEHICLE_BLOCK_AHEAD_TIME = 1f;

	private const float VEHICLE_BLOCK_AVOID_MULTIPLIER = 0.3f;

	private const int VEHICLE_BLOCK_MASK = 256;

	private const float AI_MIN_SCAN_TIME = 0.8f;

	private const float AI_MAX_SCAN_TIME = 3f;

	private const float AI_FACE_HIGHLIGHTED_DISTANCE = 30f;

	private const float AI_FACE_HIGHLIGHTED_CHANCE = 0.2f;

	private const float AI_CHASE_EXTRAPOLATION_TIME = 2f;

	private const float AI_INVESTIGATE_MIN_TIME = 3f;

	private const float AI_UPDATE_CLOSE_ACTORS_TIME = 1f;

	private const float CLOSE_ACTORS_RANGE = 10f;

	private const float LOCAL_AVOIDANCE_MIN_DISTANCE = 1.5f;

	private const float LOCAL_AVOIDANCE_SPEED = 2f;

	private const int FRIENDLY_LAYER_MASK = 5376;

	private const int GROUND_LAYER_MASK = 1;

	private const float FATIGUE_GAIN = 0.1f;

	private const float FATIGUE_DRAIN = 0.3f;

	private const float AIM_SLERP_SPEED = 6f;

	private const float AIM_CONSTANT_SPEED = 5f;

	private const float MIN_GOTO_DELTA = 2f;

	private const int CAN_SEE_RAYCAST_SAMPLES = 3;

	private const float FOV_MIN_DOT = 0.1f;

	private const float WAYPOINT_COMPLETE_DISTANCE = 0.2f;

	private const float WAYPOINT_COMPLETE_DISTANCE_LQ = 1f;

	private const float WAYPOINT_COMPLETE_DISTANCE_VEHICLE = 2.5f;

	private const float WAYPOINT_COMPLETE_DISTANCE_VEHICLE_AQUATIC = 4f;

	private const float LEAN_SPEED = 2f;

	private const float EYE_HEIGHT = 0.2f;

	private const float MAX_ENTER_SEAT_DISTANCE = 5f;

	private const float MAX_VEHICLE_DISTANCE = 30f;

	private static string[] primaryWeaponNames = new string[4] { "AK-47", "AK-47", "76 EAGLE", "SL-DEFENDER" };

	private static string[] secondaryWeaponNames = new string[1] { "S-IND7" };

	private static string[] gearNames = new string[12]
	{
		"BEU AW1", "BEU AW1", "FRAG", "FRAG", "FRAG", "FRAG", "FRAG", "SPEARHEAD", "AMMO BAG", "AMMO BAG",
		"MEDIPACK", "MEDIPACK"
	};

	private static AiParameters PARAMETERS_EASY;

	private static AiParameters PARAMETERS_NORMAL;

	public Transform eyeTransform;

	public Transform weaponParent;

	private Quaternion facingDirection = Quaternion.identity;

	private Quaternion targetFacingDirection = Quaternion.identity;

	[NonSerialized]
	public Actor target;

	private bool hasPath;

	private bool calculatingPath;

	private Seeker seeker;

	private Path path;

	private int waypoint;

	private Vector3 lastSeenTargetPosition = Vector3.zero;

	private Vector3 lastSeenTargetVelocity = Vector3.zero;

	private bool skipNextScan;

	private RadiusModifier radiusModifier;

	private bool fire;

	private float randomTimeOffset;

	private float fatigue;

	private List<Actor> closeActors;

	private CoverPoint cover;

	private bool inCover;

	private Action stayInCoverAction = new Action(3f);

	private float lean;

	private Action takingFireAction = new Action(3f);

	private Vector3 takingFireDirection;

	private Seat targetSeat;

	private bool forceAntiStuckReverse;

	private int recentAntiStuckEvents;

	private bool canTurnCarTowardsWaypoint = true;

	private bool aquatic;

	private bool flying;

	private bool hasFlightTarget;

	private Vector3 flightTargetPosition;

	private Action helicopterAttackAction = new Action(4f);

	private Action helicopterAttackCooldownAction = new Action(8f);

	private Action helicopterTakeoffAction = new Action(2f);

	private float smoothNoisePhase;

	[NonSerialized]
	public Squad squad;

	[NonSerialized]
	public bool squadLeader;

	private Vector3 lastGotoPoint;

	private bool blockerAhead;

	private Vector3 blockerPosition;

	public static AiParameters PARAMETERS
	{
		get
		{
			if (OptionsUi.GetOptions().difficulty == 0)
			{
				return PARAMETERS_EASY;
			}
			return PARAMETERS_NORMAL;
		}
	}

	public static void SetupParameters()
	{
		PARAMETERS_EASY = default(AiParameters);
		PARAMETERS_EASY.LEAD_SWAY_MAGNITUDE = 0.3f;
		PARAMETERS_EASY.LEAD_NOISE_MAGNITUDE = 0.1f;
		PARAMETERS_EASY.SWAY_MAGNITUDE = 1.5f;
		PARAMETERS_EASY.AIM_BASE_SWAY = 0.01f;
		PARAMETERS_EASY.AIM_MAX_SWAY = 0.1f;
		PARAMETERS_EASY.AI_FIRE_RECTANGLE_BOUND = 2.5f;
		PARAMETERS_NORMAL = default(AiParameters);
		PARAMETERS_NORMAL.LEAD_SWAY_MAGNITUDE = 0.1f;
		PARAMETERS_NORMAL.LEAD_NOISE_MAGNITUDE = 0.05f;
		PARAMETERS_NORMAL.SWAY_MAGNITUDE = 0.5f;
		PARAMETERS_NORMAL.AIM_BASE_SWAY = 0.002f;
		PARAMETERS_NORMAL.AIM_MAX_SWAY = 0.05f;
		PARAMETERS_NORMAL.AI_FIRE_RECTANGLE_BOUND = 1f;
	}

	private void Awake()
	{
		seeker = GetComponent<Seeker>();
		Seeker obj = seeker;
		obj.pathCallback = (OnPathDelegate)Delegate.Combine(obj.pathCallback, new OnPathDelegate(OnPathComplete));
		randomTimeOffset = UnityEngine.Random.Range(0f, 10f);
		radiusModifier = GetComponent<RadiusModifier>();
		smoothNoisePhase = UnityEngine.Random.Range(0f, (float)Math.PI * 2f);
	}

	private List<Actor> FindPotentialTargets()
	{
		List<Actor> list = new List<Actor>(ActorManager.instance.actors);
		list.RemoveAll((Actor actor) => actor.dead || actor.team == base.actor.team || !HasEffectiveWeaponAgainst(actor.GetTargetType()));
		Dictionary<Actor, float> distanceTo = new Dictionary<Actor, float>(list.Count);
		foreach (Actor item in list)
		{
			float num = ((!item.fallenOver) ? 0f : 30f);
			distanceTo.Add(item, Vector3.Distance(item.Position(), actor.Position()) + num);
		}
		list.Sort((Actor x, Actor y) => distanceTo[x].CompareTo(distanceTo[y]));
		return list;
	}

	private void StartAiCoroutines()
	{
		StartCoroutine(AiBlocked());
		StartCoroutine(AiVehicle());
		StartCoroutine(AiOrders());
		StartCoroutine(AiTarget());
		StartCoroutine(AiWeapon());
		StartCoroutine(AiTrack());
		StartCoroutine(AiScan());
		StartCoroutine(AiTrackClosestActors());
	}

	private IEnumerator AiBlocked()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 0.4f));
		Collider[] colliders = new Collider[128];
		while (true)
		{
			if (hasPath)
			{
				Ray ray = new Ray(base.actor.CenterPosition(), GetWaypointDelta());
				blockerAhead = false;
				RaycastHit hitInfo;
				if (base.actor.IsSeated())
				{
					Vehicle vehicle2 = base.actor.seat.vehicle;
					if (vehicle2.HasBlockSensor())
					{
						int nHits = vehicle2.BlockTest(colliders, 1f, 256);
						for (int i = 0; i < nHits; i++)
						{
							Collider collider = colliders[i];
							Hurtable hurtable = collider.GetComponent<Hitbox>().parent;
							blockerAhead = hurtable.team == base.actor.team;
							if (blockerAhead)
							{
								Actor actor = hurtable as Actor;
								if (actor != null)
								{
									blockerPosition = actor.Position();
								}
								break;
							}
						}
					}
				}
				else if (Physics.SphereCast(ray, 0.5f, out hitInfo, 2f, 4096))
				{
					Vehicle vehicle = hitInfo.collider.GetComponentInParent<Vehicle>();
					bool isTargetVehicle = targetSeat != null && targetSeat.vehicle == vehicle;
					blockerAhead = !isTargetVehicle && vehicle.claimedBySquad && !vehicle.stuck;
					blockerPosition = vehicle.transform.position;
				}
			}
			yield return new WaitForSeconds(0.2f);
		}
	}

	private IEnumerator AiVehicle()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1f));
		Vector3 lastSampledVehiclePosition = Vector3.zero;
		float lastSampleTime = 0f;
		while (true)
		{
			if (actor.IsSeated() && actor.seat.vehicle != null && actor.IsDriver())
			{
				Type vehicleType = actor.seat.vehicle.GetType();
				if (hasPath)
				{
					forceAntiStuckReverse = false;
					if (vehicleType == typeof(Car))
					{
						Vector3 waypointDelta = GetWaypointDelta();
						Car car = (Car)actor.seat.vehicle;
						canTurnCarTowardsWaypoint = car.CanTurnTowards(waypointDelta);
					}
					if (!LastWaypoint() && (vehicleType == typeof(Car) || vehicleType == typeof(Boat)))
					{
						Vector3 betweenWaypointDelta = GetUpcomingBetweenWaypointsDelta();
						if (GetNextWaypointDelta().magnitude < betweenWaypointDelta.magnitude)
						{
							RecalculatePath();
						}
					}
					Vector3 newPosition = actor.seat.vehicle.transform.position;
					if (Vector3.Distance(newPosition, lastSampledVehiclePosition) > 0.4f)
					{
						lastSampledVehiclePosition = newPosition;
						lastSampleTime = Time.time;
					}
					else if (Time.time > lastSampleTime + 1.5f)
					{
						if (vehicleType == typeof(Boat))
						{
							actor.seat.vehicle.stuck = true;
							squad.ExitVehicle();
							squad.MoveTo(lastGotoPoint);
						}
						else if (vehicleType == typeof(Car))
						{
							PushAntiStuckEvent();
							forceAntiStuckReverse = true;
							yield return new WaitForSeconds(1f);
							forceAntiStuckReverse = false;
							yield return new WaitForSeconds(1f);
							if (!actor.IsSeated())
							{
								continue;
							}
							RecalculatePath();
							lastSampleTime = Time.time;
						}
					}
				}
				if (vehicleType == typeof(Helicopter))
				{
					if (!squad.AllSeated())
					{
						helicopterTakeoffAction.Start();
					}
					if (HasTarget() && Vector3.Dot(actor.seat.vehicle.transform.forward, target.CenterPosition() - actor.seat.vehicle.transform.position) > 0f && helicopterAttackAction.TrueDone() && helicopterAttackCooldownAction.TrueDone() && Vector3.Distance(base.transform.position, target.transform.position) < 200f)
					{
						helicopterAttackAction.Start();
						helicopterAttackCooldownAction.Start();
					}
				}
			}
			if (actor.CanEnterSeat() && (!actor.fallenOver || actor.inWater) && HasTargetSeat() && Vector3.Distance(actor.CenterPosition(), targetSeat.vehicle.transform.position) < 5f)
			{
				CancelPath();
				actor.EnterSeat(targetSeat);
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private void PushAntiStuckEvent()
	{
		if ((float)recentAntiStuckEvents > 2f)
		{
			squad.squadVehicle.stuck = true;
			squad.ExitVehicle();
			squad.MoveTo(lastGotoPoint);
			recentAntiStuckEvents = 0;
			CancelInvoke("PopAntiStuckEvent");
		}
		recentAntiStuckEvents++;
		Invoke("PopAntiStuckEvent", 30f);
	}

	private void PopAntiStuckEvent()
	{
		recentAntiStuckEvents--;
	}

	private IEnumerator AiOrders()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1f));
		while (true)
		{
			if (IsSquadLeader() && squad.Ready())
			{
				if (!actor.fallenOver && squad.HasVehicle() && squad.AllSeated() && !squad.squadVehicle.HasDriver())
				{
					actor.LeaveSeat();
					actor.EnterSeat(squad.squadVehicle.seats[0]);
				}
				if (squad.state == Squad.State.EnterVehicle)
				{
					if (squad.squadVehicle.dead || squad.AllSeated())
					{
						squad.state = Squad.State.Stationary;
					}
				}
				else if (squad.state != Squad.State.DigIn && squad.IsTakingFire())
				{
					squad.DigInTowards(takingFireDirection);
				}
				else if (!squad.IsTakingFire() && !hasPath && !hasFlightTarget)
				{
					if (!actor.IsSeated() && !HasTargetSeat())
					{
						List<Vehicle> nearbyVehicles = NearbyUnclaimedVehicles();
						if (nearbyVehicles.Count > 0)
						{
							squad.EnterVehicle(nearbyVehicles[0]);
						}
					}
					if (squad.state != Squad.State.EnterVehicle && stayInCoverAction.TrueDone())
					{
						SpawnPoint target = ActorManager.RandomEnemySpawnPoint(actor.team);
						if (target != null)
						{
							squad.MoveTo(target.transform.position);
						}
					}
				}
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private IEnumerator AiTarget()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.2f));
		Action investigateAction = new Action(3f);
		while (true)
		{
			List<Actor> potentialTargets = FindPotentialTargets();
			Actor closestHighlighted = null;
			foreach (Actor a in potentialTargets)
			{
				if (!a.dead && CanSeeActor(a, true))
				{
					SetTarget(a);
					break;
				}
				if (!a.dead && a.IsHighlighted() && Vector3.Distance(a.Position(), actor.Position()) < 30f && UnityEngine.Random.Range(0f, 1f) < 0.2f)
				{
					LookAt(a.Position());
					skipNextScan = true;
					if (closestHighlighted == null)
					{
						closestHighlighted = a;
					}
				}
				yield return new WaitForSeconds(0.2f);
			}
			if (!HasTarget() && !actor.fallenOver)
			{
				Actor squadTarget = squad.GetTarget();
				if (squadTarget != null && HasEffectiveWeaponAgainst(squadTarget.GetTargetType()))
				{
					SetTarget(squadTarget);
				}
				else if (closestHighlighted != null && investigateAction.TrueDone() && !HasCover() && stayInCoverAction.TrueDone())
				{
					squad.MoveTo(closestHighlighted.Position());
					investigateAction.Start();
				}
			}
			else if (inCover)
			{
				stayInCoverAction.Start();
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private void SetTarget(Actor target)
	{
		this.target = target;
		SwitchToEffectiveWeapon(target);
	}

	private void DropTarget()
	{
		target = null;
		if (!actor.fallenOver)
		{
			SwitchToPrimaryWeapon();
		}
	}

	private IEnumerator AiWeapon()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.5f));
		while (true)
		{
			fire = false;
			if (actor.HasUnholsteredWeapon() && !actor.activeWeapon.HasAnyAmmo())
			{
				SwitchToPrimaryWeapon();
			}
			if (actor.IsSeated() && actor.seat.vehicle.GetType() == typeof(Car) && actor.IsDriver())
			{
				fire = blockerAhead;
				yield return new WaitForSeconds(0.2f);
				continue;
			}
			if (!HasTarget() && actor.hasAmmoBox && actor.weapons[actor.ammoBoxSlot].AmmoFull() && (squad.MemberNeedsResupply() || (!ActorManager.instance.player.dead && actor.team == ActorManager.instance.player.team && ActorManager.instance.player.needsResupply && Vector3.Distance(ActorManager.instance.player.transform.position, base.transform.position) < 10f)))
			{
				if (actor.activeWeapon == actor.weapons[actor.ammoBoxSlot])
				{
					if (!ActorManager.instance.player.dead && actor.team == ActorManager.instance.player.team && ActorManager.instance.player.needsResupply && Vector3.Distance(ActorManager.instance.player.transform.position, base.transform.position) < 10f)
					{
						LookAt(ActorManager.instance.player.transform.position);
					}
					fire = UnityEngine.Random.Range(0, 2) == 0;
				}
				else
				{
					actor.SwitchWeapon(actor.ammoBoxSlot);
				}
				yield return new WaitForSeconds(0.2f);
				continue;
			}
			if (!HasTarget() && actor.hasMedipack && actor.weapons[actor.medipackSlot].AmmoFull() && (squad.MemberNeedsHealth() || (!ActorManager.instance.player.dead && actor.team == ActorManager.instance.player.team && ActorManager.instance.player.health < 80f && Vector3.Distance(ActorManager.instance.player.transform.position, base.transform.position) < 10f)))
			{
				if (actor.activeWeapon == actor.weapons[actor.medipackSlot])
				{
					if (!ActorManager.instance.player.dead && actor.team == ActorManager.instance.player.team && ActorManager.instance.player.health < 80f && Vector3.Distance(ActorManager.instance.player.transform.position, base.transform.position) < 10f)
					{
						LookAt(ActorManager.instance.player.transform.position);
					}
					fire = UnityEngine.Random.Range(0, 2) == 0;
				}
				else
				{
					actor.SwitchWeapon(actor.medipackSlot);
				}
				yield return new WaitForSeconds(0.2f);
				continue;
			}
			if (!HasTarget() || !actor.HasUnholsteredWeapon())
			{
				fire = false;
				yield return new WaitForSeconds(0.2f);
				continue;
			}
			if (HasTarget() && actor.activeWeapon.IsEmpty())
			{
				SwitchToEffectiveWeapon(target);
				yield return new WaitForSeconds(0.2f);
				continue;
			}
			if (!actor.activeWeapon.configuration.auto && fire)
			{
				fire = false;
				yield return new WaitForSeconds(0.05f);
				continue;
			}
			Vector3 muzzlePosition = actor.WeaponMuzzlePosition();
			Vector3 deltaTarget = target.CenterPosition() - muzzlePosition + WeaponLead();
			float distance = deltaTarget.magnitude;
			Vector3 forward = ((!actor.IsSeated() || !actor.seat.HasMountedWeapon()) ? FacingDirection() : actor.activeWeapon.configuration.muzzle.forward);
			Vector3 orth1 = Vector3.Cross(forward, Vector3.up).normalized;
			Vector3 orth2 = Vector3.Cross(forward, orth1);
			float a = Vector3.Dot(deltaTarget, orth1);
			float b = Vector3.Dot(deltaTarget, orth2);
			float allowedAimSpread = actor.activeWeapon.configuration.aiAllowedAimSpread;
			bool insideAimCube = Vector3.Dot(deltaTarget, forward) > 0f && Mathf.Abs(a) < PARAMETERS.AI_FIRE_RECTANGLE_BOUND * allowedAimSpread && Mathf.Abs(b) < PARAMETERS.AI_FIRE_RECTANGLE_BOUND * allowedAimSpread;
			if (actor.activeWeapon.CanFire() && insideAimCube && CanSeeActor(target))
			{
				Ray friendlyRay = new Ray(muzzlePosition + 0.3f * forward, forward);
				RaycastHit hitInfo;
				if (distance > 5f && Physics.Raycast(friendlyRay, out hitInfo, distance - 5f, 5376))
				{
					if (hitInfo.collider.gameObject.layer == 8)
					{
						fire = hitInfo.collider.GetComponent<Hitbox>().parent.team != actor.team;
					}
					else
					{
						fire = true;
					}
				}
				else
				{
					fire = true;
				}
			}
			yield return new WaitForSeconds(Mathf.Lerp(0.05f, 0.5f, distance / 40f));
		}
	}

	private IEnumerator AiTrack()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.2f));
		while (true)
		{
			if (HasTarget())
			{
				if (target.dead)
				{
					DropTarget();
				}
				else if (CanSeeActor(target))
				{
					lastSeenTargetPosition = target.Position();
					lastSeenTargetVelocity = target.Velocity();
				}
				else if (!HasCover())
				{
					Goto(lastSeenTargetPosition + lastSeenTargetVelocity * 2f);
					DropTarget();
				}
			}
			yield return new WaitForSeconds(0.2f);
		}
	}

	private IEnumerator AiScan()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.2f));
		while (true)
		{
			yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 3f));
			if (!skipNextScan && !HasTarget())
			{
				Vector3 facingDirection;
				if (IsTakingFire())
				{
					facingDirection = takingFireDirection;
				}
				if (InCover())
				{
					facingDirection = cover.transform.forward + UnityEngine.Random.insideUnitSphere * 0.1f;
				}
				else
				{
					facingDirection = FacingDirection() * 0.5f + UnityEngine.Random.insideUnitSphere;
					if (hasPath)
					{
						facingDirection += Velocity().normalized * 1f;
					}
					facingDirection += 0.2f * SquadFacingBias();
					facingDirection.Normalize();
					facingDirection.y *= UnityEngine.Random.Range(0.1f, 1f);
					if (facingDirection.y < 0f)
					{
						facingDirection.y *= 0.2f;
					}
				}
				targetFacingDirection = Quaternion.LookRotation(facingDirection, Vector3.up);
			}
			skipNextScan = false;
		}
	}

	private IEnumerator AiTrackClosestActors()
	{
		closeActors = new List<Actor>();
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 1f));
		while (true)
		{
			closeActors = ActorManager.AliveActorsInRange(base.transform.position, 10f);
			yield return new WaitForSeconds(1f);
		}
	}

	private Vector3 SquadFacingBias()
	{
		Vector3 zero = Vector3.zero;
		int num = 0;
		foreach (AiActorController member in squad.members)
		{
			if (member != this)
			{
				zero -= member.transform.position - base.transform.position;
				num++;
			}
		}
		if (num == 0)
		{
			return Vector3.zero;
		}
		return zero / num;
	}

	private bool HasEffectiveWeaponAgainst(Actor.TargetType targetType)
	{
		Weapon[] weapons = actor.weapons;
		foreach (Weapon weapon in weapons)
		{
			if (weapon != null && weapon.EffectivenessAgainst(targetType) != 0)
			{
				return true;
			}
		}
		return false;
	}

	private void LookAt(Vector3 position)
	{
		LookDirection(position - actor.Position());
	}

	private void LookDirection(Vector3 direction)
	{
		targetFacingDirection = Quaternion.LookRotation(direction);
	}

	private void OnPathComplete(Path p)
	{
		if (!p.error)
		{
			calculatingPath = false;
			hasPath = true;
			path = p;
			waypoint = 0;
			if (!inCover && HasCover())
			{
				path.vectorPath.Add(cover.transform.position);
			}
		}
		else
		{
			Debug.LogError(p.errorLog);
		}
	}

	private void RecalculatePath()
	{
		if (hasPath)
		{
			Vector3 targetPoint = lastGotoPoint;
			CancelPath();
			Goto(targetPoint);
		}
	}

	public void Goto(Vector3 targetPoint)
	{
		if (flying && actor.IsDriver())
		{
			flightTargetPosition = targetPoint;
			hasFlightTarget = true;
		}
		else if (!calculatingPath && (!actor.IsSeated() || actor.IsDriver()) && (!hasPath || Vector3.Distance(path.vectorPath[path.vectorPath.Count - 1], targetPoint) > 2f))
		{
			calculatingPath = true;
			int graphMask = 1;
			if (actor.IsDriver())
			{
				graphMask = ((!aquatic) ? 4 : 2);
			}
			lastGotoPoint = targetPoint;
			seeker.StartPath(actor.Position(), targetPoint, null, graphMask);
		}
	}

	public void CancelPath()
	{
		calculatingPath = false;
		path = null;
		hasPath = false;
	}

	private void Update()
	{
		if (actor.dead)
		{
			return;
		}
		if (!InCover() || IsReloading() || CoolingDown())
		{
			lean = Mathf.MoveTowards(lean, 0f, 2f * Time.deltaTime);
		}
		else if (InCover() && cover.type == CoverPoint.Type.LeanLeft)
		{
			lean = Mathf.MoveTowards(lean, -1f, 2f * Time.deltaTime);
		}
		else if (InCover() && cover.type == CoverPoint.Type.LeanRight)
		{
			lean = Mathf.MoveTowards(lean, 1f, 2f * Time.deltaTime);
		}
		else
		{
			lean = Mathf.MoveTowards(lean, 0f, 2f * Time.deltaTime);
		}
		if (HasTarget())
		{
			if (actor.HasUnholsteredWeapon())
			{
				targetFacingDirection = Quaternion.LookRotation(target.CenterPosition() - actor.WeaponMuzzlePosition() + WeaponLead(), Vector3.up);
			}
			else
			{
				targetFacingDirection = Quaternion.LookRotation(target.CenterPosition() - actor.CenterPosition(), Vector3.up);
			}
		}
		facingDirection = Quaternion.Slerp(facingDirection, targetFacingDirection, 6f * Time.deltaTime);
		facingDirection = Quaternion.RotateTowards(facingDirection, targetFacingDirection, 5f * Time.deltaTime);
		fatigue = Mathf.Clamp01(fatigue - 0.3f * Time.deltaTime);
	}

	private bool IsReloading()
	{
		return actor.HasUnholsteredWeapon() && actor.activeWeapon.reloading;
	}

	private bool CoolingDown()
	{
		return actor.HasUnholsteredWeapon() && actor.activeWeapon.configuration.cooldown > 0.3f && actor.activeWeapon.CoolingDown();
	}

	private Vector3 WeaponLead()
	{
		Vector3 vector = target.Position() - actor.Position();
		float num = vector.magnitude / actor.activeWeapon.projectileSpeed;
		Vector3 normalized = vector.normalized;
		Vector3 vector2 = vector;
		float num2 = num;
		for (int i = 0; i < 1; i++)
		{
			Vector3 vector3 = Physics.gravity * Mathf.Pow(num2, 2f) / 2f;
			num2 = num / Vector3.Dot((vector - vector3).normalized, normalized);
		}
		Vector3 vector4 = SmoothNoise(0.2f);
		Vector3 vector5 = SmoothNoise(0.2333f);
		Vector3 vector6 = target.Velocity();
		Vector3 vector7 = FacingDirection();
		Vector3 vector8 = vector6 - Vector3.Dot(vector6, vector7) * vector7;
		Vector3 vector9 = target.Velocity() + vector4 * vector8.magnitude * 0.3f;
		float num3 = num2 * (1f + PARAMETERS.LEAD_SWAY_MAGNITUDE * (vector4.x + vector4.z) + UnityEngine.Random.Range(0f - PARAMETERS.LEAD_NOISE_MAGNITUDE, PARAMETERS.LEAD_NOISE_MAGNITUDE));
		Vector3 vector10 = vector9 * num3 - Physics.gravity * Mathf.Pow(num3, 2f) / 2f;
		return vector10 + vector5 * PARAMETERS.SWAY_MAGNITUDE;
	}

	private Vector3 SmoothNoise(float frequency)
	{
		float num = frequency * Time.time;
		return new Vector3(Mathf.Sin(num * 7.9f + smoothNoisePhase), Mathf.Sin(num * 8.3f + smoothNoisePhase), Mathf.Sin(num * 8.9f + smoothNoisePhase));
	}

	public override float Lean()
	{
		return lean;
	}

	public override bool Fire()
	{
		return fire;
	}

	public override bool Aiming()
	{
		return false;
	}

	public override bool Reload()
	{
		return actor.HasUnholsteredWeapon() && actor.activeWeapon.IsEmpty();
	}

	public override bool OnGround()
	{
		return true;
	}

	public override bool ProjectToGround()
	{
		return true;
	}

	private Vector3 GetWaypointDeltaBlockable()
	{
		if (blockerAhead)
		{
			return Vector3.zero;
		}
		return GetWaypointDelta();
	}

	private Vector3 GetWaypointDelta()
	{
		Vector3 vector = ((!actor.IsDriver()) ? actor.Position() : actor.seat.vehicle.transform.position);
		Vector3 vector2 = path.vectorPath[waypoint] - vector;
		Vector3 vector3 = vector2;
		vector3.y = 0f;
		float num = (actor.IsSeated() ? ((!aquatic) ? 2.5f : 4f) : ((!actor.IsLowQuality()) ? 0.2f : 1f));
		if (vector3.magnitude < num)
		{
			if (!LastWaypoint())
			{
				waypoint++;
			}
			else
			{
				PathDone();
				hasPath = false;
			}
		}
		return vector2;
	}

	private Vector3 GetUpcomingBetweenWaypointsDelta()
	{
		return path.vectorPath[waypoint + 1] - path.vectorPath[waypoint];
	}

	private Vector3 GetNextWaypointDelta()
	{
		Vector3 vector = ((!actor.IsDriver()) ? actor.Position() : actor.seat.vehicle.transform.position);
		return path.vectorPath[waypoint + 1] - vector;
	}

	private bool LastWaypoint()
	{
		return path.vectorPath.Count <= waypoint + 1;
	}

	private void PathDone()
	{
		if (HasCover())
		{
			LookDirection(cover.transform.forward);
			inCover = true;
			stayInCoverAction.Start();
		}
	}

	public override Vector3 Velocity()
	{
		if (hasPath)
		{
			float num = 3.5f;
			if (HasTarget())
			{
				num = 2f;
			}
			fatigue = Mathf.Clamp01(fatigue + num * 0.1f * Time.deltaTime);
			return GetWaypointDeltaBlockable().ToGround().normalized * num;
		}
		return Vector3.zero;
	}

	public override Vector3 SwimInput()
	{
		if (hasPath)
		{
			return GetWaypointDeltaBlockable().ToGround().normalized;
		}
		return Vector3.zero;
	}

	private Vector3 LocalAvoidanceVelocity()
	{
		Vector3 zero = Vector3.zero;
		int num = 0;
		foreach (Actor closeActor in closeActors)
		{
			Vector3 vector = base.transform.position - closeActor.transform.position;
			float magnitude = vector.magnitude;
			if (magnitude < 1.5f)
			{
				float num2 = Mathf.Lerp(2f, 0f, magnitude / 1.5f);
				zero += vector.normalized * num2;
				num++;
			}
		}
		if (num > 0)
		{
			return zero / num;
		}
		return Vector3.zero;
	}

	public override Vector2 BoatInput()
	{
		if (!hasPath)
		{
			return Vector2.zero;
		}
		Vehicle vehicle = actor.seat.vehicle;
		float z = vehicle.LocalVelocity().z;
		Vector3 waypointDeltaBlockable = GetWaypointDeltaBlockable();
		waypointDeltaBlockable.y = 0f;
		float magnitude = waypointDeltaBlockable.magnitude;
		Vector3 normalized = waypointDeltaBlockable.normalized;
		Debug.DrawRay(vehicle.transform.position, waypointDeltaBlockable, Color.red);
		Vector2 vector = new Vector2(Vector3.Dot(normalized, actor.transform.right), Vector3.Dot(normalized, actor.transform.forward));
		return Vector2.ClampMagnitude(vector, 1f);
	}

	public override Vector2 CarInput()
	{
		if (!hasPath)
		{
			return Vector2.zero;
		}
		Vehicle vehicle = actor.seat.vehicle;
		float z = vehicle.LocalVelocity().z;
		if (blockerAhead)
		{
			float num = Mathf.Sign(vehicle.transform.worldToLocalMatrix.MultiplyPoint(blockerPosition).x) * 0.3f;
			if (z > 0.1f)
			{
				return new Vector2(0f - num, -1f);
			}
			return new Vector2(num, 1f);
		}
		Vector3 waypointDeltaBlockable = GetWaypointDeltaBlockable();
		waypointDeltaBlockable.y = 0f;
		float magnitude = waypointDeltaBlockable.magnitude;
		float magnitude2 = (vehicle.Velocity() * 3f).magnitude;
		float num2 = 0.1f;
		if (!LastWaypoint())
		{
			Vector3 upcomingBetweenWaypointsDelta = GetUpcomingBetweenWaypointsDelta();
			upcomingBetweenWaypointsDelta.y = 0f;
			num2 = upcomingBetweenWaypointsDelta.magnitude;
		}
		float num3 = Mathf.Clamp01((magnitude - magnitude2) / magnitude2);
		float b = Mathf.Clamp01(0.3f + num2 / magnitude2);
		float num4 = Mathf.Lerp(1f, b, 1f - num3);
		if (num4 < 0.4f)
		{
			num4 *= -1f;
		}
		Debug.DrawRay(vehicle.transform.position + Vector3.up, Vector3.up, Color.black);
		Debug.DrawRay(vehicle.transform.position + Vector3.up, Vector3.up * num4, Color.red);
		Vector2 result = new Vector2(Vector3.Dot(waypointDeltaBlockable, actor.transform.right), Vector3.Dot(waypointDeltaBlockable, actor.transform.forward));
		bool flag = !canTurnCarTowardsWaypoint ^ forceAntiStuckReverse;
		Color color = Color.blue;
		result.y = Mathf.Clamp(Mathf.Sign(result.y) * num4, -1f, 0.8f);
		result.x = Mathf.Clamp(result.x / (1f + Mathf.Abs(z)), -1f, 1f);
		if (flag)
		{
			result.y = -0.5f * Mathf.Abs(result.y);
			color = Color.red;
		}
		if (z < 0f)
		{
			result.x *= -1f;
		}
		Debug.DrawRay(actor.seat.vehicle.transform.position, GetWaypointDelta(), color);
		return result;
	}

	public override Vector4 HelicopterInput()
	{
		if (!squad.AllSeated() || !helicopterTakeoffAction.TrueDone())
		{
			return new Vector4(0f, -1f + helicopterTakeoffAction.Ratio() * 1.5f, 0f, 0f);
		}
		Rigidbody rigidbody = actor.seat.vehicle.rigidbody;
		Transform transform = actor.seat.vehicle.transform;
		Vector3 position = transform.position;
		Vector3 localEulerAngles = transform.localEulerAngles;
		float y = transform.eulerAngles.y;
		float num = position.y;
		Vector3 vector = position + rigidbody.velocity * 3f;
		RaycastHit hitInfo;
		if (Physics.SphereCast(new Ray(vector + Vector3.up * 10f, Vector3.down), 1f, out hitInfo, 999f, 1))
		{
			num = hitInfo.distance;
		}
		float num2 = 0f;
		Vector3 zero = Vector3.zero;
		Vector3 forward = transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 rhs = new Vector3(forward.z, 0f, 0f - forward.x);
		bool flag = HasTarget() && !helicopterAttackAction.TrueDone();
		Vector3 vector2 = position + forward;
		if (flag)
		{
			vector2 = target.CenterPosition() + WeaponLead();
			Debug.DrawLine(base.transform.position, vector2, Color.red);
		}
		else if (hasFlightTarget)
		{
			vector2 = flightTargetPosition;
		}
		num2 = Heading(position, vector2);
		zero = vector2 - position;
		float y2 = zero.y;
		zero.y = 0f;
		float magnitude = zero.magnitude;
		float num3 = Mathf.DeltaAngle(y, num2);
		float num4 = 40f;
		float num5 = num4 - num;
		float num6 = 25f * Mathf.Clamp(Vector3.Dot(zero * 0.02f, forward), -1f, 1f);
		float current = -25f * Mathf.Clamp(Vector3.Dot(zero * 0.02f, rhs), -1f, 1f);
		if (num5 > 5f)
		{
			num6 = 0f;
			current = 0f;
		}
		float num7 = 1f;
		if (flag)
		{
			num6 = (0f - Mathf.Atan2(y2, magnitude)) * 57.29578f;
			num7 = 2.5f;
		}
		Vector3 vector3 = transform.InverseTransformDirection(rigidbody.angularVelocity);
		float x = 0.01f * num7 * num3 - vector3.y;
		float w = 0.1f * num7 * Mathf.DeltaAngle(localEulerAngles.x, num6) - 2f * vector3.x;
		float z = 0.1f * num7 * Mathf.DeltaAngle(current, localEulerAngles.z) + 2f * vector3.z;
		float y3 = ((!(num5 > 0f)) ? (0.01f * num5) : (1f * num5));
		return new Vector4(x, y3, z, w);
	}

	private float Heading(Vector3 root, Vector3 target)
	{
		Vector3 vector = target - root;
		return (0f - Mathf.Atan2(vector.z, vector.x)) * 57.29578f + 90f;
	}

	public override Vector3 FacingDirection()
	{
		float num = Time.time + randomTimeOffset;
		Vector3 vector = new Vector3(Mathf.Sin(num * 3.1f), Mathf.Cos(num * 5.3f), Mathf.Cos(num * 3.7f));
		return facingDirection * Vector3.forward + vector * (PARAMETERS.AIM_BASE_SWAY + PARAMETERS.AIM_MAX_SWAY * fatigue);
	}

	public override bool UseMuzzleDirection()
	{
		return false;
	}

	public override void ReceivedDamage(float damage, float balanceDamage, Vector3 point, Vector3 direction, Vector3 force)
	{
		if (!HasTarget())
		{
			LookAt(point - direction * 10f);
		}
	}

	public override void DisableInput()
	{
	}

	public override void EnableInput()
	{
	}

	public override void StartSeated(Seat seat)
	{
		flying = seat.vehicle.GetType() == typeof(Helicopter);
		if (seat.vehicle.GetType() == typeof(Car))
		{
			seeker.tagPenalties[0] = 100000;
		}
		if (seat.vehicle.GetType() == typeof(Boat))
		{
			aquatic = true;
			seeker.startEndModifier.exactEndPoint = StartEndModifier.Exactness.Original;
		}
	}

	public override void EndSeated(Vector3 exitPosition, Quaternion flatFacing)
	{
		flying = false;
		aquatic = false;
		radiusModifier.enabled = false;
		seeker.tagPenalties[0] = 0;
		seeker.startEndModifier.exactEndPoint = StartEndModifier.Exactness.ClosestOnNode;
	}

	public override void StartRagdoll()
	{
	}

	public override void GettingUp()
	{
	}

	public override void EndRagdoll()
	{
		if (inCover)
		{
			Goto(cover.transform.position);
		}
	}

	public override void Die()
	{
		LeaveCover();
		CancelPath();
		squad.DropMember(this);
		squad = null;
		StopAllCoroutines();
		CancelInvoke();
	}

	public bool HasTarget()
	{
		return target != null;
	}

	private bool CanSeeActor(Actor target, bool considerFov = false)
	{
		Vector3 vector = target.Position() - actor.Position();
		Vector3 normalized = vector.normalized;
		if (!considerFov || target.IsHighlighted() || Vector3.Dot(normalized, FacingDirection()) > 0.1f)
		{
			for (int i = 0; i < 3; i++)
			{
				Vector3 vector2 = vector + Vector3.down * 0.5f + Vector3.Scale(UnityEngine.Random.insideUnitSphere, new Vector3(0.7f, 0.8f, 0.7f));
				Ray ray = new Ray(eyeTransform.position - eyeTransform.right * 0.2f, vector2.normalized);
				if (!Physics.Raycast(ray, vector.magnitude, 1))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void SpawnAt(Vector3 position)
	{
		target = null;
		targetSeat = null;
		hasFlightTarget = false;
		takingFireAction.Stop();
		radiusModifier.enabled = false;
		recentAntiStuckEvents = 0;
		StartAiCoroutines();
	}

	public override void ApplyRecoil(Vector3 impulse)
	{
		facingDirection = Quaternion.LookRotation(FacingDirection() * 20f + impulse.z * Vector3.down + Vector3.right * impulse.x, Vector3.up);
	}

	public bool FindCover()
	{
		return FindCoverAtPoint(actor.Position());
	}

	public bool FindCoverAtPoint(Vector3 point)
	{
		if (HasCover())
		{
			LeaveCover();
		}
		inCover = false;
		cover = CoverManager.instance.ClosestVacant(point);
		if (HasCover())
		{
			CancelPath();
			cover.taken = true;
			Goto(cover.transform.position);
			return true;
		}
		CancelPath();
		Goto(point);
		return false;
	}

	public bool FindCoverTowards(Vector3 direction)
	{
		if (HasCover())
		{
			LeaveCover();
		}
		inCover = false;
		cover = CoverManager.instance.ClosestVacantCoveringDirection(base.transform.position, direction);
		if (HasCover())
		{
			cover.taken = true;
			Goto(cover.transform.position);
			return true;
		}
		return false;
	}

	public void LeaveCover()
	{
		inCover = false;
		if (HasCover())
		{
			cover.taken = false;
			cover = null;
		}
	}

	public bool HasCover()
	{
		return cover != null;
	}

	public bool InSquad()
	{
		return squad != null;
	}

	public void AssignedToSquad(Squad squad)
	{
		this.squad = squad;
		if (IsSquadLeader())
		{
			EmoteRegroup();
		}
		else
		{
			EmoteHailLeaderSlow();
		}
	}

	public bool IsSquadLeader()
	{
		return squad.Leader() == this;
	}

	public bool InCover()
	{
		return inCover;
	}

	public void EmoteRegroup()
	{
		actor.EmoteRegroup();
	}

	public void EmoteMoveOrder(Vector3 target)
	{
		LookAt(target);
		actor.EmoteMove();
	}

	public void EmoteHailLeaderSlow()
	{
		Invoke("EmoteHailLeader", UnityEngine.Random.Range(0.6f, 1.5f));
	}

	public void EmoteHailPlayer()
	{
		if (!HasTarget())
		{
			LookAt(FpsActorController.instance.actor.CenterPosition());
			actor.EmoteHail();
		}
	}

	public void EmoteHailLeader()
	{
		if (!HasTarget())
		{
			LookAt(squad.Leader().transform.position);
			actor.EmoteHail();
		}
	}

	public void EmoteHalt()
	{
		actor.EmoteHalt();
	}

	public void MarkTakingFireFrom(Vector3 direction)
	{
		takingFireDirection = direction;
		takingFireAction.Start();
	}

	public bool IsTakingFire()
	{
		return !takingFireAction.TrueDone();
	}

	public override SpawnPoint SelectedSpawnPoint()
	{
		return ActorManager.RandomSpawnPointForTeam(actor.team);
	}

	public override Transform WeaponParent()
	{
		return weaponParent;
	}

	public bool HasTargetSeat()
	{
		return targetSeat != null;
	}

	public void GotoAndEnterSeat(Seat seat)
	{
		targetSeat = seat;
		Goto(seat.transform.position);
	}

	public void LeaveSeat()
	{
		targetSeat = null;
		if (actor.IsSeated())
		{
			actor.LeaveSeat();
		}
	}

	private List<Vehicle> NearbyUnclaimedVehicles()
	{
		List<Vehicle> list = new List<Vehicle>(ActorManager.instance.vehicles);
		Vector3 squadPosition = actor.CenterPosition();
		list.RemoveAll((Vehicle vehicle) => vehicle.stuck || vehicle.claimedBySquad || vehicle.EmptySeats() < squad.members.Count || Vector3.Distance(vehicle.transform.position, squadPosition) > 30f);
		list.Sort((Vehicle x, Vehicle y) => Vector3.Distance(x.transform.position, squadPosition).CompareTo(Vector3.Distance(y.transform.position, squadPosition)));
		return list;
	}

	public override void SwitchedToWeapon(Weapon weapon)
	{
	}

	public override bool Crouch()
	{
		return InCover() && cover.type == CoverPoint.Type.Crouch && (IsReloading() || CoolingDown());
	}

	public override void StartCrouch()
	{
	}

	public override void EndCrouch()
	{
	}

	public override WeaponManager.LoadoutSet GetLoadout()
	{
		WeaponManager.LoadoutSet loadoutSet = new WeaponManager.LoadoutSet();
		loadoutSet.primary = WeaponManager.EntryNamed(primaryWeaponNames[UnityEngine.Random.Range(0, primaryWeaponNames.Length)]);
		loadoutSet.secondary = WeaponManager.EntryNamed(secondaryWeaponNames[UnityEngine.Random.Range(0, secondaryWeaponNames.Length)]);
		loadoutSet.gear1 = WeaponManager.EntryNamed(gearNames[UnityEngine.Random.Range(0, gearNames.Length)]);
		return loadoutSet;
	}

	private void SwitchToPrimaryWeapon()
	{
		for (int i = 0; i < actor.weapons.Length; i++)
		{
			if (actor.weapons[i] != null && actor.weapons[i].HasAnyAmmo())
			{
				actor.SwitchWeapon(i);
				break;
			}
		}
	}

	private void SwitchToEffectiveWeapon(Actor target)
	{
		Actor.TargetType targetType = target.GetTargetType();
		float range = Vector3.Distance(base.transform.position, target.transform.position);
		int num = -1;
		for (int i = 0; i < actor.weapons.Length; i++)
		{
			Weapon weapon = actor.weapons[i];
			if (!(weapon != null) || !weapon.HasSpareAmmo() || !weapon.EffectiveAtRange(range))
			{
				continue;
			}
			switch (weapon.EffectivenessAgainst(targetType))
			{
			case Weapon.Effectiveness.Preferred:
				if (!weapon.IsEmpty())
				{
					actor.SwitchWeapon(i);
					return;
				}
				num = i;
				break;
			case Weapon.Effectiveness.Yes:
				num = i;
				break;
			}
		}
		if (num != -1)
		{
			actor.SwitchWeapon(num);
		}
	}

	private void OnGUI()
	{
		if (!ActorManager.instance.debug || actor.dead || !(Camera.main != null))
		{
			return;
		}
		float num = Vector3.Dot(actor.CenterPosition() - Camera.main.transform.position, Camera.main.transform.forward);
		if (num > 1f && num < 100f)
		{
			Vector3 vector = Camera.main.WorldToScreenPoint(actor.CenterPosition() + Vector3.up);
			GUI.skin.label.alignment = TextAnchor.UpperCenter;
			GUI.Label(new Rect(vector.x - 100f, (float)Screen.height - vector.y, 200f, 50f), string.Concat("Squad #", squad.number, ": ", squad.state, (!squad.IsGroupedUp()) ? string.Empty : " grouped"));
			if (!stayInCoverAction.TrueDone())
			{
				GUI.Label(new Rect(vector.x - 100f, (float)Screen.height - vector.y + 20f, 200f, 50f), "Staying in cover");
			}
			if (blockerAhead)
			{
				GUI.Label(new Rect(vector.x - 100f, (float)Screen.height - vector.y + 40f, 200f, 50f), "Blocker ahead!");
			}
		}
	}

	public override bool IsGroupedUp()
	{
		return squad != null && squad.IsGroupedUp();
	}

	public override bool IsSprinting()
	{
		return false;
	}
}
