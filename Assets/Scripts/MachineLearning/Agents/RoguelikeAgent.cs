﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoguelikeAgent : Agent
{
	[Header("Roguelike specific")]
	public int speed = 100;
	public int startingHealth = 100;
	public int startingMana = 50;
	public float attackCooldown = 1f;
	public int attackDamage = 5;

	protected Rigidbody2D rb;
	protected Animator animator;
	protected Vector2 movementInput; //cached input coming from the Brain
	protected SpriteRenderer graphicsSpriteRenderer;
	protected int health;
	protected int mana;

	private float damageCooldown = 1f; //invincibility cooldown after a hit
	private float lastHitTime, lastAttackTime; //used to verify cooldowns
	private int doAttackHash;
	private Collider2D damageCollider;
	private bool canAttack = true; //put to false when attacking, restored to true after the attackCooldown
    private bool hasBeenHit = false;
	private Vector2 startPosition;
	private float movementDelta;
	private Vector3 prevPosition;

	public RoguelikeAgent enemyAgent;

    protected virtual void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		graphicsSpriteRenderer = transform.Find("Graphics").GetComponent<SpriteRenderer>();
		doAttackHash = Animator.StringToHash("DoAttack");
		startPosition = transform.position;
		AgentReset(); //will reset some key variables
	}

	public override List<float> CollectState()
	{
		List<float> state = new List<float>();
		state.Add(transform.position.x);
		state.Add(transform.position.y);
		state.Add(health);
		state.Add((canAttack) ? 1f : 0f);
		state.Add((enemyAgent != null) ? 1f : 0f);
		state.Add((enemyAgent != null) ? enemyAgent.transform.position.x : 0f);
		state.Add((enemyAgent != null) ? enemyAgent.transform.position.y : 0f);
		state.Add(movementDelta);
		return state;
	}

	public override void AgentStep(float[] act)
	{
		//movement vector is valorized
		movementInput.x = Mathf.Clamp(act[0], -1f, 1f);
		movementInput.y = Mathf.Clamp(act[1], -1f, 1f);
		rb.AddForce(movementInput * speed, ForceMode2D.Force);

		movementDelta = (prevPosition - transform.position).sqrMagnitude;
		if (movementDelta > 0f)
		{
			reward += .001f;
		}
		prevPosition = transform.position;

		float attack = Mathf.Clamp(act[2], -1f, 1f);
		if(attack > 0f)
		{
			if(canAttack)
			{
				Attack();
			}
			else
			{
				reward -= .001f; //penalty for trying to attack when it can't
			}
		}

		rb.velocity *= .05f;
	}

	protected virtual void Attack()
	{
		if(canAttack)
		{
			StartCoroutine(DoAttack());
		}
	}

    private IEnumerator DoAttack()
    {
		canAttack = false;
		lastAttackTime = Time.time;
        animator.SetTrigger(doAttackHash);

		yield return new WaitForSeconds(attackCooldown);

		canAttack = true;
    }

    public void DealDamage(RoguelikeAgent target)
	{
		bool isTargetDead = false;

		if(!target.hasBeenHit)
		{
			isTargetDead = target.ReceiveDamage(attackDamage);
			if(isTargetDead)
			{
				reward += 2f;
			}
			else
			{
				reward += .8f;
			}
		}

	}

	//Returns if the Agent is dead or not, to reward the attacker
    public bool ReceiveDamage(int attackDamage)
    {
        health -= attackDamage;
		if(health <= 0)
		{
			Die();
			reward -= 2f;
			return true;
		}
		else
		{
			StartCoroutine(HitFlicker());
			reward -= .5f;
			return false;
		}
    }

	private void Die()
	{
		if(brain.brainType == BrainType.External)
		{
			//During training
			done = true;
		}
		else
		{
			//During gameplay
			Destroy(gameObject);
		}

	}

    private IEnumerator HitFlicker()
    {
		lastHitTime = Time.time;
		hasBeenHit = true;

        while(Time.time < lastHitTime + damageCooldown)
		{
			yield return new WaitForSeconds(.1f);
			graphicsSpriteRenderer.color = Color.red;

			yield return new WaitForSeconds(.1f);
			graphicsSpriteRenderer.color = Color.white;
		}

		hasBeenHit = false;
    }

    protected virtual void FixedUpdate()
	{
		
	}

	public override void AgentReset()
	{
		rb.velocity = Vector3.zero;
		health = startingHealth;
		mana = startingMana;
		transform.position = startPosition;
		prevPosition = transform.position;
	}

	public override void AgentOnDone()
	{
		
	}
}
