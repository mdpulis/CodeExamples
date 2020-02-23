// Copyright 1998-2019 Epic Games, Inc. All Rights Reserved.

#include "ZhengCharacter.h"
#include "ZhengCharacter-inl.h"
#include "HeadMountedDisplayFunctionLibrary.h"
#include "PlayerAttack-inl.h"
#include "PlayerAttackFactory-inl.h"
#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/InputComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/Controller.h"
#include "GameFramework/SpringArmComponent.h"

//////////////////////////////////////////////////////////////////////////
// AZhengCharacter

const float DOUBLE_PRESS_TIMING_WINDOW = 0.2f;
const float STRUM_TO_COMMAND_TIME = 0.1f;

AZhengCharacter::AZhengCharacter()
{
	// Set size for collision capsule
	GetCapsuleComponent()->InitCapsuleSize(42.f, 96.0f);

	// Don't rotate when the controller rotates. Let that just affect the camera.
	bUseControllerRotationPitch = false;
	bUseControllerRotationYaw = false;
	bUseControllerRotationRoll = false;

	// Configure character movement
	GetCharacterMovement()->bOrientRotationToMovement = true; // Character moves in the direction of input...	
	GetCharacterMovement()->RotationRate = FRotator(0.0f, 540.0f, 0.0f); // ...at this rotation rate
	GetCharacterMovement()->JumpZVelocity = 600.f;
	GetCharacterMovement()->AirControl = 0.2f;

	// Create a camera boom (pulls in towards the player if there is a collision)
	CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
	CameraBoom->SetupAttachment(RootComponent);
	CameraBoom->TargetArmLength = 300.0f; // The camera follows at this distance behind the character	
	CameraBoom->bUsePawnControlRotation = true; // Rotate the arm based on the controller

	// Create a follow camera
	FollowCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FollowCamera"));
	FollowCamera->SetupAttachment(CameraBoom, USpringArmComponent::SocketName); // Attach the camera to the end of the boom and let the boom adjust to match the controller orientation
	FollowCamera->bUsePawnControlRotation = false; // Camera does not rotate relative to arm

	// Note: The skeletal mesh and anim blueprint references on the Mesh component (inherited from Character) 
	// are set in the derived blueprint asset named MyCharacter (to avoid direct content references in C++)

	playerAttackFactory = new PlayerAttackFactory();

	CharacterName = "Zheng";
	MaxHealth = 100;
	DefaultWalkSpeed = 400;
	GetCharacterMovement()->MaxWalkSpeed = DefaultWalkSpeed;
	DashWalkSpeed = 800;
	DashMaxTime = 0.5f;
	DashEndingTime = 0.2f;
	DashCooldownTime = 0.5f;
	AttackEndingTime = 0.3f;
	ForwardWalkSpeedMod = 0.5f;
	SideWalkSpeedMod = 1.0f;

	playerNumber = EPlayerNumber::P1;
	currentTarget = nullptr;

	health = MaxHealth;
	alive = true;

	roundsWon = 0;

	dashing = false;
	dashCurrentTime = 0.0f;
	dashEnding = false;
	dashEndingCurrentTime = 0.0f;
	dashDirection = FVector(0, 0, 0);
	dashOnCooldown = false;
	dashOnCooldownCurrentTime = 0.0f;

	attackEnding = false;
	attackEndingCurrentTime = 0.0f;

	strumming = false;
	strumToCommandCurrentTime = 0.0f;
	strumToCommandTime = STRUM_TO_COMMAND_TIME;

	beatConsumed = false;
	beatPassed = false;

	rightPressed = false;
	rightPressedLastTime = 0.0f;
	leftPressed = false;
	leftPressedLastTime = 0.0f;
	upPressed = false;
	upPressedLastTime = 0.0f;
	downPressed = false;
	downPressedLastTime = 0.0f;

	stumblePressed = false;
	damagePressed = false;
	stumbleBlockPressed = false;
	damageBlockPressed = false;
}

void AZhengCharacter::BeginPlay()
{
	Super::BeginPlay();

	playerAttackFactory->SetPlayerRef(this);
}

void AZhengCharacter::Tick(float deltaTime)
{
	Super::Tick(deltaTime);

	//When we die, stop processing tick
	if (!alive)
		return;

	//Deal with any inputs
	HandleInputs();


	//Rotation
	
	if (currentTarget !=  nullptr && !(this->GetCharacterMovement()->IsFalling()))
	{
		FRotator newRotation = UKismetMathLibrary::FindLookAtRotation(GetActorLocation(), currentTarget->GetActorLocation());
		newRotation = FRotator(0.0f, newRotation.Yaw, 0.0f);

		SetActorRotation(newRotation);
	}

	
	//Movement
	float x = 0.0f;
	float y = 0.0f;

	if (rightPressed)
		x += 1.0f;
	if (leftPressed)
		x -= 1.0f;
	if (upPressed)
		y += 1.0f;
	if (downPressed)
		y -= 1.0f;


	//Movement
	if (dashing)
	{
		if (!dashEnding)
		{
			DashPlayer();
		}
		else
		{
			//end of dash, can't move
		}
	}
	else
	{
		if (CanMove())
		{
			MovePlayer(x, y);
		}
	}

	//Timers
	UpdateTimers(deltaTime);
}

void AZhengCharacter::HandleInputs()
{
	if (inputQueue.empty())
	{
		return;
	}

	for (std::list<ControllerInput*>::iterator it = inputQueue.begin(); it != inputQueue.end(); ++it)
	{
		bool inputProcessed = false;

		if ((*it)->InputType == ControllerInputTypes::Press)
		{
			switch ((*it)->Input)
			{
			case ControllerInputs::Right:
				rightPressed = true;
				if (CanDash())
				{
					if (GetWorld()->GetTimeSeconds() - rightPressedLastTime < DOUBLE_PRESS_TIMING_WINDOW)
					{
						StartDash(this->GetActorRightVector());
					}
				}
				rightPressedLastTime = GetWorld()->GetTimeSeconds();
				break;
			case ControllerInputs::Left:
				leftPressed = true;
				if (CanDash())
				{
					if (GetWorld()->GetTimeSeconds() - leftPressedLastTime < DOUBLE_PRESS_TIMING_WINDOW)
					{
						StartDash(this->GetActorRightVector() * -1);
					}
				}
				leftPressedLastTime = GetWorld()->GetTimeSeconds();
				break;
			case ControllerInputs::Up:
				upPressed = true;
				if (CanDash())
				{
					if (GetWorld()->GetTimeSeconds() - upPressedLastTime < DOUBLE_PRESS_TIMING_WINDOW)
					{
						StartDash(this->GetActorForwardVector());
					}
				}
				upPressedLastTime = GetWorld()->GetTimeSeconds();
				break;
			case ControllerInputs::Down:
				downPressed = true;
				if (CanDash())
				{
					if (GetWorld()->GetTimeSeconds() - downPressedLastTime < DOUBLE_PRESS_TIMING_WINDOW)
					{
						StartDash(this->GetActorForwardVector() * -1);
					}
				}
				downPressedLastTime = GetWorld()->GetTimeSeconds();
				break;
			case ControllerInputs::Physical:
				stumblePressed = true;
				if (CanStrum())
				{
					if (!IsStrumming())
					{
						StartStrum();
					}
					
					strummedCommandsList.Add(EPlayerAttackComponent::Physical);
					strumToCommandCurrentTime = 0.0f;
				}
				break;
			case ControllerInputs::Magical:
				damagePressed = true;
				if (CanStrum())
				{
					if (!IsStrumming())
					{
						StartStrum();
					}

					strummedCommandsList.Add(EPlayerAttackComponent::Magical);
					strumToCommandCurrentTime = 0.0f;
				}
				break;
			case ControllerInputs::Pushback:
				stumbleBlockPressed = true;
				if (CanStrum())
				{
					if (!IsStrumming())
					{
						StartStrum();
					}

					strummedCommandsList.Add(EPlayerAttackComponent::Pushback);
					strumToCommandCurrentTime = 0.0f;
				}
				break;
			case ControllerInputs::Special:
				damageBlockPressed = true;
				if (CanStrum())
				{
					if (!IsStrumming())
					{
						StartStrum();
					}

					strummedCommandsList.Add(EPlayerAttackComponent::Special);
					strumToCommandCurrentTime = 0.0f;
				}
				break;
			}
		}
		else if ((*it)->InputType == ControllerInputTypes::Release)
		{
			switch ((*it)->Input)
			{
			case ControllerInputs::Right:
				rightPressed = false;
				break;
			case ControllerInputs::Left:
				leftPressed = false;
				break;
			case ControllerInputs::Up:
				upPressed = false;
				break;
			case ControllerInputs::Down:
				downPressed = false;
				break;
			case ControllerInputs::Physical:
				stumblePressed = false;
				break;
			case ControllerInputs::Magical:
				damagePressed = false;
				break;
			case ControllerInputs::Pushback:
				stumbleBlockPressed = false;
				break;
			case ControllerInputs::Special:
				damageBlockPressed = false;
				break;
			}
		}

		inputQueue.remove(*it);
		it--;

	}

}

void AZhengCharacter::MoveForward(float Value)
{
	if ((Controller != NULL) && (Value != 0.0f))
	{
		// find out which way is forward
		const FRotator Rotation = Controller->GetControlRotation();
		const FRotator YawRotation(0, Rotation.Yaw, 0);

		// get forward vector
		const FVector Direction = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::X);
		AddMovementInput(Direction, Value);
	}
}

void AZhengCharacter::MoveRight(float Value)
{
	if ( (Controller != NULL) && (Value != 0.0f) )
	{
		// find out which way is right
		const FRotator Rotation = Controller->GetControlRotation();
		const FRotator YawRotation(0, Rotation.Yaw, 0);
	
		// get right vector 
		const FVector Direction = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::Y);
		// add movement in that direction
		AddMovementInput(Direction, Value);
	}
}

void AZhengCharacter::UpdateTimers(float deltaTime)
{
	if (dashing)
	{
		if (!dashEnding)
		{
			dashCurrentTime += deltaTime;
			if (dashCurrentTime >= DashMaxTime)
			{
				dashEndingCurrentTime = (dashCurrentTime - DashMaxTime);
				dashEnding = true;
			}
		}
		else //if in the end of the dash
		{
			dashEndingCurrentTime += deltaTime;
			if (dashEndingCurrentTime >= DashEndingTime)
			{
				EndDash();
			}
		}

	}

	if (dashOnCooldown)
	{
		dashOnCooldownCurrentTime += deltaTime;
		if (dashOnCooldownCurrentTime >= DashCooldownTime)
		{
			dashOnCooldown = false;
			dashOnCooldownCurrentTime = 0.0f;
		}
	}

	if (attackEnding)
	{
		attackEndingCurrentTime += deltaTime;
		if (attackEndingCurrentTime >= AttackEndingTime)
		{
			attackEnding = false;
			attackEndingCurrentTime = 0.0f;
		}
	}

	if (strumming)
	{
		strumToCommandCurrentTime += deltaTime;
		if (strumToCommandCurrentTime >= strumToCommandTime)
		{
			EndStrum();
		}
	}

	//This two-part lock prevents the player from playing more than one note, and refreshes on the off-beat.
	//The first part of the lock checks to make sure that a beat has passed by checking to see if the elapsed time is less than the
	//remaining time -- in other words, checks to see if we're just after a beat.
	//The second part checks to make sure that a beat is incoming, so after the off-beat. If it is, then we can release the lock fully.
	
	//Bypass for this lock below
	//beatConsumed = false;
	//beatPassed = false;

	if (beatConsumed)
	{
		if (!beatPassed)
		{
			if (PlayerAttackFactory::elapsed < PlayerAttackFactory::remaining)
			{
				beatPassed = true;
			}
		}
		else
		{
			if (PlayerAttackFactory::elapsed > PlayerAttackFactory::remaining)
			{
				beatConsumed = false;
				beatPassed = false;
				print("Reset!");
			}
		}
	}
}



void AZhengCharacter::SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent)
{
	//// Set up gameplay key bindings
	check(PlayerInputComponent);
}


void AZhengCharacter::MovePlayer(float i_x, float i_y)
{
	//Need to make sure we can actually move
	if (!CanMove())
	{
		return;
	}

	FVector direction = FVector::ZeroVector;
	float scaleValue = 1.0f;

	if (i_x > 0.0f)
	{
		scaleValue *= SideWalkSpeedMod;
		direction += GetActorRightVector();
	}
	else if (i_x < 0.0f)
	{
		scaleValue *= SideWalkSpeedMod;
		direction += GetActorRightVector() * -1;
	}
	else
	{
		direction += FVector::ZeroVector;
	}

	//cut movement speed in half for forward and backward movement
	if (i_y > 0.0f)
	{
		scaleValue *= ForwardWalkSpeedMod;
		direction += GetActorForwardVector();
	}
	else if (i_y < 0.0f)
	{
		scaleValue *= ForwardWalkSpeedMod;
		direction += GetActorForwardVector() * -1;
	}
	else
	{
		direction += FVector::ZeroVector;
	}

	direction = FVector(direction.X, direction.Y, 0.0f);
	AddMovementInput(direction, scaleValue);
}

void AZhengCharacter::DashPlayer()
{
	FVector direction = FVector::ZeroVector;
	float scaleValue = 2.0f;

	direction += dashDirection;
	direction = FVector(direction.X, direction.Y, 0.0f);

	AddMovementInput(direction, scaleValue);
}

void AZhengCharacter::SetPlayerNumber(EPlayerNumber i_playerNumber)
{
	playerNumber = i_playerNumber;
}

EPlayerNumber AZhengCharacter::GetPlayerNumber() const
{
	return playerNumber;
}

void AZhengCharacter::IncrementRoundsWon()
{
	roundsWon++;
	printf("rounds won: %d", roundsWon);
	OnWonRound.Broadcast(playerNumber, roundsWon);
}

int AZhengCharacter::GetRoundsWon() const
{
	return roundsWon;
}

void AZhengCharacter::WinBattle()
{
	OnWonBattle.Broadcast(this);
}

bool AZhengCharacter::CanMove() const
{
	if (alive && !dashing && !attackEnding && !GetCharacterMovement()->IsFalling())
	{
		return true;
	}
	else
	{
		return false;
	}
}

bool AZhengCharacter::IsAlive() const
{
	return alive;
}

bool AZhengCharacter::CanAttack() const
{
	if (alive && !dashing && !attackEnding && !GetCharacterMovement()->IsFalling())
	{
		return true;
	}
	else
	{
		return false;
	}
}

bool AZhengCharacter::CanStrum() const
{
	if (alive && !dashing && !attackEnding && !GetCharacterMovement()->IsFalling() && !blocking)
	{
		return true;
	}
	else
	{
		return false;
	}
}

bool AZhengCharacter::CanDash() const
{
	if (alive && !dashing && !attackEnding && !strumming && !GetCharacterMovement()->IsFalling())
	{
		return true;
	}
	else
	{
		return false;
	}
}

int AZhengCharacter::GetHealth() const
{
	return health;
}

void AZhengCharacter::SetTarget(AActor * i_target)
{
	currentTarget = i_target;
}

void AZhengCharacter::StartStrum()
{
	strumming = true;
	strumToCommandCurrentTime = 0.0f;
	strummedCommandsList.Empty(); //empty out our strum list just in case
}

//Ends and finalizes the strum
void AZhengCharacter::EndStrum()
{
	if (beatConsumed)
	{
		strummedCommandsList.Empty(); //empty out our strum list
		return;
	}
	if (strummedCommandsList.Num() == 0)
	{
		print("Empty strummed commands list.");
		return;
	}

	if (strummedCommandsList.Num() >= 4)
	{
		if (strummedCommandsList.Contains(EPlayerAttackComponent::Physical) && strummedCommandsList.Contains(EPlayerAttackComponent::Magical) &&
			strummedCommandsList.Contains(EPlayerAttackComponent::Pushback) && strummedCommandsList.Contains(EPlayerAttackComponent::Special))
		{
			print("all strum commands in queue! Launch attack!");
			SendAttack();
		}
	}
	else
	{
		//we strum the first one we strummed
		float timing = playerAttackFactory->AddComponentToAttack(strummedCommandsList[0], strumToCommandTime, GetWorld());
		OnInputCommand.Broadcast(playerNumber, strummedCommandsList[0], timing);
	}

	strumming = false;
	beatConsumed = true;
	strumToCommandCurrentTime = 0.0f;
	strummedCommandsList.Empty(); //empty out our strum list
}

//Cancels a strum partway through
void AZhengCharacter::CancelStrum()
{
	strumming = true;
	strumToCommandCurrentTime = 0.0f;
	strummedCommandsList.Empty(); //empty out our strum list
}

bool AZhengCharacter::IsStrumming() const
{
	return strumming;
}

void AZhengCharacter::SendAttack()
{
	attackEnding = true;
	//If it's a one strum special
	if (playerAttackFactory->GetCurrentAttack().size() == 1 && playerAttackFactory->GetCurrentAttack().front().component == EPlayerAttackComponent::Special) {
		//block
		StartBlocking();
		FTimerHandle handler;
		GetWorld()->GetTimerManager().SetTimer(handler, this, &AZhengCharacter::StopBlocking, 5, false);
		playerAttackFactory->ClearCurrentAttack();
		OnSendAttack.Broadcast(playerNumber);
		return;
	}
	FVector PlayerLocation;
	FRotator PlayerRotation;
	this->GetActorEyesViewPoint(PlayerLocation, PlayerRotation);
	PlayerRotation = this->GetActorRotation();
	// To world location
	FVector MuzzleLocation = PlayerLocation + FTransform(PlayerRotation).TransformVector(FVector(190, 0, -30));
	FRotator MuzzleRotation = PlayerRotation;
	FActorSpawnParameters spawnParams;
	spawnParams.Owner = this;
	spawnParams.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
	spawnParams.bNoFail = true;

	APlayerAttack* toReturn = GetWorld()->SpawnActor<APlayerAttack>(RegularAttack, MuzzleLocation, PlayerRotation, spawnParams);
	FVector LaunchDirection = MuzzleRotation.Vector();
	LaunchDirection = FVector(LaunchDirection.X, LaunchDirection.Y, 0);
	if (toReturn != nullptr) {
		toReturn->ProjectileMovementComponent->HomingTargetComponent = currentTarget->GetRootComponent();
		std::list<ComponentBundle> components = playerAttackFactory->GetCurrentAttack();
		toReturn->SetComponents(components, Cadenzas, playerNumber);
		toReturn->SetPropertiesBasedOnSecondaryModulator();
		toReturn->SetPropertiesBasedOnTertiaryModulator();
		toReturn->SetPropertiesBasedOnComponents(LaunchDirection);
		toReturn->ProjectileMovementComponent->Velocity = LaunchDirection * toReturn->ProjectileMovementComponent->InitialSpeed;
		toReturn->Initialized = true;
	}
	playerAttackFactory->ClearCurrentAttack();
	OnSendAttack.Broadcast(playerNumber);
}

void AZhengCharacter::StartDash(FVector i_dashDirection)
{
	if (dashOnCooldown)
	{
		return;
	}

	GetCharacterMovement()->MaxWalkSpeed = DashWalkSpeed;

	dashing = true;
	dashCurrentTime = 0.0f;
	dashEnding = false;
	dashEndingCurrentTime = 0.0f;
	dashDirection = i_dashDirection;
}

void AZhengCharacter::EndDash()
{
	GetCharacterMovement()->MaxWalkSpeed = DefaultWalkSpeed;

	dashing = false;
	dashCurrentTime = 0.0f;
	dashEnding = false;
	dashEndingCurrentTime = 0.0f;
	dashDirection = FVector(0, 0, 0);

	dashOnCooldown = true;
	dashOnCooldownCurrentTime = 0.0f;
}

void AZhengCharacter::TakeDamage(int i_damage)
{
	if (!alive)
		return;


	health -= i_damage;
	printf_2("new health: %d, max health: %d", health, MaxHealth);
	if (health <= 0)
		Die();
	OnUpdateHealth.Broadcast(playerNumber, health, MaxHealth);
}

void AZhengCharacter::RecoverHealth(int i_health)
{
	health += i_health;
	if (health > MaxHealth)
		health = MaxHealth;
	OnUpdateHealth.Broadcast(playerNumber, health, MaxHealth);
}

void AZhengCharacter::GetHitByAttack(AttackInformation * i_AttackInfo)
{
	if (blocking && i_AttackInfo->PrimaryAttackType == EPlayerAttackComponent::Magical) { //Magical attacks can be blocked
		print("Attack Blocked!");
		return;
	}

	if (i_AttackInfo->Cadenza) {
		i_AttackInfo->Cadenza->ImpactPlayer(this);
		return;
	}

	print("Got hit by attack.");
	int mpSum = i_AttackInfo->PhysicalCount + i_AttackInfo->MagicalCount;
	if (mpSum) { TakeDamage((3 + mpSum) * static_cast<int>(i_AttackInfo->GeneralScaler)); } //Temporary

	FVector PlayerLocation;
	FRotator PlayerRotation;
	GetActorEyesViewPoint(PlayerLocation, PlayerRotation);
	FVector LaunchVector = GetActorForwardVector() *= (-i_AttackInfo->PushbackCount * 200);
	LaunchVector.Z += 50 + 150 * i_AttackInfo->PushbackCount;
	LaunchCharacter(LaunchVector, false, false);
	//This is not abstracted per character
	if (i_AttackInfo->PrimaryAttackType == EPlayerAttackComponent::Special) {
		for (int i = 0; i < i_AttackInfo->SpecialCount-1; i++) {
			if (fireOrbs.Num() <= 5) {
				AddFireOrb();
			}
		}
		ConsumeFireOrbs();
	}
	else {
		for (int i = 0; i < i_AttackInfo->SpecialCount; i++) {
			if (fireOrbs.Num() < 5) {
				AddFireOrb();
			}
		}
	}
}

void AZhengCharacter::FallOffMap()
{
	health = 0;
	OnUpdateHealth.Broadcast(playerNumber, health, MaxHealth);

	Die();
}

void AZhengCharacter::Die()
{
	alive = false;
	OnDie.Broadcast(playerNumber);
}

void AZhengCharacter::ResetForBattle()
{
	roundsWon = 0;
	OnWonRound.Broadcast(playerNumber, roundsWon);
	OnRestartBattle.Broadcast();
	ResetForRound();
}

void AZhengCharacter::ResetForRound()
{
	GetCharacterMovement()->MaxWalkSpeed = DefaultWalkSpeed;
	GetCharacterMovement()->Velocity = FVector(0.0f, 0.0f, 0.0f);

	alive = true;
	health = MaxHealth;

	dashing = false;
	dashCurrentTime = 0.0f;
	dashEnding = false;
	dashEndingCurrentTime = 0.0f;
	dashDirection = FVector(0, 0, 0);
	dashOnCooldown = false;
	dashOnCooldownCurrentTime = 0.0f;

	attackEnding = false;
	attackEndingCurrentTime = 0.0f;

	strumming = false;
	strumToCommandCurrentTime = 0.0f;

	beatConsumed = false;
	beatPassed = false;

	rightPressed = false;
	rightPressedLastTime = 0.0f;
	leftPressed = false;
	leftPressedLastTime = 0.0f;
	upPressed = false;
	upPressedLastTime = 0.0f;
	downPressed = false;
	downPressedLastTime = 0.0f;

	stumblePressed = false;
	damagePressed = false;
	stumbleBlockPressed = false;
	damageBlockPressed = false;

	playerAttackFactory->ClearCurrentAttack();
	OnUpdateHealth.Broadcast(playerNumber, health, MaxHealth);
	OnSendAttack.Broadcast(playerNumber); //should have a different thing for clearing the command UI but yeah
}

void AZhengCharacter::ReceiveInput(ControllerInput* i_controllerInput)
{
	inputQueue.push_front(i_controllerInput);
}

//Checks to see if we can process the player's inputs at the moment
bool AZhengCharacter::CanProcessInputs() const
{
	if (alive && !dashing && !GetCharacterMovement()->IsFalling())
	{
		return true;
	}
	else
	{
		return false;
	}
}
