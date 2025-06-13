using Godot;
using Godot.Collections;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

static class States
{
	public const int PATROL = 0;
	public const int SUSPICIOUS = 1;
	public const int CHASE = 2;
	public const int GLOBAL_CHASE = 3;
}

public partial class Guard : CharacterBody3D
{
	[Signal] public delegate void DamagePlayerEventHandler();
	// General variables
	NavigationAgent3D navigationAgent;
	Vision vision;
	Area3D neighborDetection;
	Node3D player;
	AnimationTree animationTree;

	float time;
	const float delayTime = 0.5f;
	float delay;
	// State variables
	int state;
	// Animation variables
	const float MOVING_THRESHOLD = 0.1f;
	// Movement variables
	const float DELTA_ACCEL = 3.5f;
	const float DELTA_ROT_SPEED = 5f;
	const float OPPOSITE_THRESHOLD = Mathf.Pi / 6f;
	const float SMALL_MOV_TOL = 1f;
	[Export] public float chaseSpeed = 7f;
	[Export] public float patrolSpeed = 5f;
	float speed;
	// Pathfinding variables
	bool targetReached;
	Vector3 playerPosition;
	const float PLAYER_DIST_DELTA = 0.5f;
	const float NEIGHBOR_DET_RADIUS = 2f;
	const float NORMAL_RADIUS = 0.5f;
	// Chase variables
	public float persistenceTime = 2f; //TODO: obsolete
	[Export] public float attackRange = 4f;
	const float tooClose = 3f;
	float timer;
	bool chasing;
	// Patrol variables
	[Export] public Array<Node3D> patrolPoints;
	[Export] public Dictionary<int, float> overrideWaitTimes;
	[Export] public float patrolPointWaitTime = 0f;
	bool neighborsDetected;
	const float ARRIVAL_RANGE = 2f + 0.03f;
	float waitTime;
	int currentPatrolPoint;
	// Suspicious variables
	Array<Vector3> suspicionPoints;
	const float SUS_ARRIVAL_RANGE = 2f + 0.03f;
	const float MIN_IGNORE_DIST = 15f;
	[Export] public float suspiciousSpeed = 7f;
	const float MAX_SIMILAR_DIST = 3f;
	// Global Chase
	bool globalChase;
	
	
	public override void _Ready()
	{
		// Add to group
		AddToGroup("Guards");
		// Connect signals
		DamagePlayer += GetNode<PlayerHealthManager>("/root/" + GetTree().Root.GetChild(0).Name + "/Game Manager/Player Health").AddDamage;
		// Set up navigation agent
		navigationAgent = GetChild<NavigationAgent3D>(0);
		navigationAgent.MaxSpeed = Mathf.Inf;
		navigationAgent.VelocityComputed += SafeVelocityCalculated;
		// Set up vision
		vision = GetChild<Vision>(1);
		// Set up neighbor detection
		neighborDetection = GetChild<Area3D>(2);
		// Set up player reference
		player = GetNode<Node3D>("/root/" + GetTree().Root.GetChild(0).Name + "/Player/Player");
		// Set up animation tree
		animationTree = GetChild(6).GetChild<AnimationTree>(2);

		// State
		SwitchToPatrol();

		// Pathfinding
		targetReached = false;
		// Movement
		speed = 0f;
		// Chase
		chasing = false;
		// Patrol
		currentPatrolPoint = 0;
		waitTime = 0f;
		navigationAgent.TargetPosition = patrolPoints[0].GlobalPosition; 
		// Suspicious
		suspicionPoints = new();
		// Global Chase
		globalChase = false;
		
		delay = 0f;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (delay > delayTime)
		{
			time = (float) delta;
			// Mangage guard chase state
			// ManagePersistence((float) delta);
			// // Manage states
			// SetVariables();
			// if (!chasing)
			// 	Patrol();
			// // Manage movement and rotation in both chase and patrol state
			// SetSpeed((float) delta);
			// SetRotation((float) delta);

			// IsTargetReached();
			NeighborAwareness();
			GuardStateMachine();
			ManageAnimation();
		}
		else
		{
			delay += (float) delta;
		}
	}
	
	public void SafeVelocityCalculated(Vector3 safeVelocity) // Signal calls this after each physics frame
	{
		// Velocity = navigationAgent.Velocity;
		
		// if (neighborsDetected)
		// 	Velocity = safeVelocity;
		
		// if (Velocity.Length() < SMALL_MOV_TOL || navigationAgent.IsTargetReached())
		// 	Velocity = Vector3.Zero;
		
		Velocity = safeVelocity;

		MoveAndSlide();
	}

	void NeighborAwareness()
	{
		if (neighborDetection.GetOverlappingBodies().Count > 1)
		{
			navigationAgent.Radius = NEIGHBOR_DET_RADIUS;
			neighborsDetected = true;
		}
		else
		{
			navigationAgent.Radius = NORMAL_RADIUS;
			neighborsDetected = false;
		}
	}

	void GuardStateMachine()
	{
		switch (state)
		{
			case States.PATROL:
				PatrolState();
				break;
			case States.SUSPICIOUS:
				SuspiciousState();
				break;
			case States.CHASE:
				ChaseState();
				break;
			case States.GLOBAL_CHASE:
				GlobalChaseState();
				break;
			default:
				GD.PushError(Name + ": STATE ERROR!!!");
				break;
		}
	}

	void ManageAnimation()
	{
		bool idle = true;
		bool running;

		// Check if guard's moving
		if (Velocity.Length() >= MOVING_THRESHOLD)
			idle = false;
		
		// Check if guard's running
		running = (state == States.SUSPICIOUS) || (state == States.CHASE) || (state == States.GLOBAL_CHASE);
		
		// Assign values
		animationTree.Set("parameters/conditions/idle", idle);
		animationTree.Set("parameters/conditions/moving", !idle);
		animationTree.Set("parameters/conditions/running", running);
		animationTree.Set("parameters/conditions/not_running", !running);
	}

	//------------------------------------------------------------------------

	void PatrolState()
	{
		//* State change check
		if (vision.playerSeen)
			SwitchToChase();
		else if (suspicionPoints.Count > 0)
			SwitchToSuspicious();
		else if (globalChase)
			SwitchToGlobalChase();
		//* State logic
		else
			Patrol();
	}

	void ChaseState()
	{
		//* State change check
		if (!vision.playerSeen)
		{
			AddSuspicionPoint(navigationAgent.TargetPosition);
			SwitchToSuspicious();
		}
		else if (globalChase)
			SwitchToGlobalChase();
		//* State logic
		else
		{
			playerPosition = (Vector3) vision.playerPosition;
			Chase();
			InflictDamage();
		}
	}

	void SuspiciousState()
	{
		//* State change check
		if (vision.playerSeen)
			SwitchToChase();
		else if (suspicionPoints.Count == 0)
			SwitchToPatrol();
		else if (globalChase)
			SwitchToGlobalChase();
		//* State logic
		else
			Investigate();
	}

	void GlobalChaseState()
	{
		//* State change check
		if (!globalChase)
		{
			AddSuspicionPoint(navigationAgent.TargetPosition);
			SwitchToSuspicious();
		}
		//* State logic
		else
		{
			playerPosition = player.GlobalPosition;
			Chase();
			InflictDamage();
		}
	}

	//------------------------------------------------------------------------

	void SwitchToPatrol()
	{
		// Find closest patrol point and set it as target (also sets currentPatrolPoint)
		Vector3? closest = null;

		foreach (var node in patrolPoints)
		{
			if (closest == null || GlobalPosition.DistanceTo(node.GlobalPosition) < GlobalPosition.DistanceTo((Vector3) closest))
			{
				currentPatrolPoint = patrolPoints.IndexOf(node);
				closest = node.GlobalPosition;
			}
		}

		navigationAgent.TargetPosition = (Vector3) closest;

		// Sets correct targetDistance
		navigationAgent.TargetDesiredDistance = ARRIVAL_RANGE;

		// Switch states
		state = States.PATROL;
	}

	void SwitchToChase()
	{
		// Sets correct targetDistance
		navigationAgent.TargetDesiredDistance = attackRange;

		// Clears all suspicion
		suspicionPoints.Clear();

		// Switch states
		state = States.CHASE;
	}

	void SwitchToSuspicious()
	{
		// Sets correct variables
		navigationAgent.TargetDesiredDistance = SUS_ARRIVAL_RANGE;

		// Switch states
		state = States.SUSPICIOUS;
	}

	void SwitchToGlobalChase()
	{
		// Sets correct targetDistance
		navigationAgent.TargetDesiredDistance = attackRange;

		// Clears all suspicion
		suspicionPoints.Clear();

		// Switch states
		state = States.GLOBAL_CHASE;
	}

	//------------------------------------------------------------------------

	void Patrol()
	{
		if (navigationAgent.IsTargetReached() && navigationAgent.TargetPosition == patrolPoints[currentPatrolPoint].GlobalPosition)
		{
			// Wait at patrol point for designated time and look in correct direction
			navigationAgent.Velocity = Vector3.Zero;

			waitTime += time;

			float t = patrolPointWaitTime;

			if (overrideWaitTimes != null && overrideWaitTimes.ContainsKey(currentPatrolPoint))
				t = overrideWaitTimes[currentPatrolPoint];

			if (waitTime > t)
			{
				waitTime = 0f;
				currentPatrolPoint++;

				if (currentPatrolPoint >= patrolPoints.Count)
					currentPatrolPoint = 0;
			}
			else
				SetRotationAngle(patrolPoints[currentPatrolPoint].Rotation.Y, false);
		}
		else
		{
			// Move towards the patrol point
			navigationAgent.TargetPosition = patrolPoints[currentPatrolPoint].GlobalPosition;
			SetRotationVector3(navigationAgent.GetNextPathPosition());

			navigationAgent.Velocity = GetVelocityVector(navigationAgent.GetNextPathPosition(), patrolSpeed);
		}
	}

	void Chase()
	{
		// Move towards and look at next path position
		SetRotationVector3(navigationAgent.GetNextPathPosition());

		if (!navigationAgent.IsTargetReached())
			navigationAgent.Velocity = GetVelocityVector(navigationAgent.GetNextPathPosition(), chaseSpeed);
		else
			navigationAgent.Velocity = Vector3.Zero;

		// Update target position if player has moved a decent distance
		if (navigationAgent.TargetPosition.DistanceTo(playerPosition) > PLAYER_DIST_DELTA)
			navigationAgent.TargetPosition = playerPosition;

		// Move back if too close to player
		// if (GlobalPosition.DistanceTo(navigationAgent.TargetPosition) < tooClose)
		// {
		// 	navigationAgent.Velocity = GetVelocityVector(navigationAgent.TargetPosition, chaseSpeed).Length() * navigationAgent.TargetPosition.DirectionTo(GlobalPosition).Normalized();
		// }
	}

	void InflictDamage()
	{
		if (navigationAgent.IsTargetReached())
			EmitSignal(SignalName.DamagePlayer);
	}

	void Investigate()
	{
		Vector3 currentTarget = suspicionPoints[0];
		float distance = GlobalPosition.DistanceTo(currentTarget);

		// Checks if it should bother
		if (distance > MIN_IGNORE_DIST && !navigationAgent.TargetPosition.Equals(currentTarget))
			suspicionPoints.RemoveAt(0);
		else
		{
			// Sets suspicion point as target position
			if (!navigationAgent.TargetPosition.Equals(currentTarget))
				navigationAgent.TargetPosition = currentTarget;

			// Checks if it should bother
			if (navigationAgent.IsTargetReached() && distance <= SUS_ARRIVAL_RANGE)
			{
				suspicionPoints.RemoveAt(0);
			}
			else
			{
				SetRotationVector3(navigationAgent.GetNextPathPosition());
				navigationAgent.Velocity = GetVelocityVector(navigationAgent.GetNextPathPosition(), suspiciousSpeed);
			}
		}
	}

	//------------------------------------------------------------------------

	public void ChasePlayer(Vector3 position)
	{
		playerPosition = position;
		// Set up persistence timer
		chasing = true;
		timer = 0f;
		// Update target position if player has moved a decent distance
		if (navigationAgent.TargetPosition.DistanceTo(playerPosition) > PLAYER_DIST_DELTA)
			navigationAgent.TargetPosition = playerPosition;
	}

	public void AddSuspicionPoint(Vector3 position)
	{
		// Checks if point should be added
		if (GlobalPosition.DistanceTo(position) > MIN_IGNORE_DIST)
			return;
		// Checks if a similar point is already in the array (if there is one, removes it)
		for (int i = 0; i < suspicionPoints.Count; i++)
		{
			if (position.DistanceTo(suspicionPoints[i]) < MAX_SIMILAR_DIST)
			{
				suspicionPoints.RemoveAt(i);
				break;
			}
		}
		// Adds the point
		if (suspicionPoints.Count == 0)
			suspicionPoints.Add(position);
		else
			suspicionPoints.Insert(0, position);
	}

	public void GlobalChase(bool value)
	{
		globalChase = value;
	}

	//------------------------------------------------------------------------

	Vector3 GetVelocityVector(Vector3 target, float speed)
	{
		float length = speed * time;
		Vector3 vector = target - GlobalPosition;

		if (vector.Length() > length)
			vector = vector.Normalized() * speed;

		return vector;
	}

	void ManagePersistence(float deltaTime)
	{
		if (timer < persistenceTime)
			timer += deltaTime;
		else
			chasing = false;
	}

	void SetVariables()
	{
		if (chasing)
		{
			navigationAgent.TargetDesiredDistance = attackRange;
		}
		else
		{
			navigationAgent.TargetDesiredDistance = ARRIVAL_RANGE;
		}
	}

	void SetSpeed(float deltaTime)
	{
		Vector3 forward = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y);
		Vector3 targetPosition = navigationAgent.TargetPosition;

		float targetSpeed;
		float deltaSpeed = DELTA_ACCEL * deltaTime;
		
		// Set target speed
		if (targetReached || IsOppositeDirection())
		{
			targetSpeed = 0f;
		}
		else if (chasing)
		{
			targetSpeed = chaseSpeed;
		}
		else
		{
			targetSpeed = patrolSpeed;
		}
		// Set speed
		//speed += Mathf.Clamp(targetSpeed - speed, -RampUpSpeed(deltaSpeed, GlobalPosition.DistanceTo(targetPosition)), deltaSpeed);
		if (targetReached)
		{
			speed += Mathf.Clamp(targetSpeed - speed, -deltaSpeed * (3f + (navigationAgent.TargetDesiredDistance + GlobalPosition.DistanceTo(targetPosition))), deltaSpeed);
		}
		else
			speed += Mathf.Clamp(targetSpeed - speed, -deltaSpeed, deltaSpeed);
		navigationAgent.Velocity = forward * speed;
	}

	bool IsOppositeDirection()
	{
		Vector3 forward = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y);
		float angle = forward.AngleTo(GlobalPosition.DirectionTo(navigationAgent.GetNextPathPosition()));
		return angle > OPPOSITE_THRESHOLD;
	}

	void IsTargetReached()
	{
		if (navigationAgent.DistanceToTarget() <= navigationAgent.TargetDesiredDistance)
		{
			targetReached = true;
			TargetReached();
		}
		else
			targetReached = false;
	}

	void TargetReached()
	{
		if (chasing)
		{
			EmitSignal(SignalName.DamagePlayer);
		}
	}

	float RampUpSpeed(float deltaSpeed, float distance)
	{
		float x = Mathf.Clamp(distance - 1f, 0f, distance) / navigationAgent.TargetDesiredDistance * 0.25f;

		return Mathf.Clamp(deltaSpeed / x, deltaSpeed, Mathf.Inf);
	}

	void SetRotation(float deltaTime)
	{
		if (chasing)
			SetRotationVector3(navigationAgent.GetNextPathPosition());
	}

	void SetRotationVector3(Vector3 target)
	{
		Vector3 forward = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y);
		Vector3 direction = GlobalPosition.DirectionTo(target);
		direction = new Vector3(direction.X, 0f, direction.Z).Normalized();

		float angleDifference = Mathf.Sign(Mathf.Sign(forward.Cross(direction).Y) * 2f + 1f) * forward.AngleTo(direction);
		float deltaAngle = DELTA_ROT_SPEED * time;
		
		RotateY(Mathf.Clamp(angleDifference, -deltaAngle, deltaAngle));
	}

	void SetRotationAngle(float target, bool deg)
	{
		float targetAngle = target;

		// If angle deg, turn into radian
		if (deg)
			targetAngle = Mathf.DegToRad(targetAngle);

		float angleDifference = Mathf.AngleDifference(Rotation.Y, targetAngle);
		float deltaAngle = DELTA_ROT_SPEED * time;

		RotateY(Mathf.Clamp(angleDifference, -deltaAngle, deltaAngle));
	}

	float RampUpRotSpeed(float deltaAngle, float absDifference)
	{
		GD.Print(Mathf.Pi / 6f + " " + Mathf.Clamp(absDifference % (Mathf.Pi * 2f), 1f, Mathf.Inf) + " " + absDifference % 2);
		return deltaAngle * Mathf.Clamp(absDifference % (Mathf.Pi * 2f), 1f, Mathf.Inf);
	}
}
