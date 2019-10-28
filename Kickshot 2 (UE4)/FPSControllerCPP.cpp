#include "FPSControllerCPP.h"

#include "GameFramework/CharacterMovementComponent.h"
#include "Camera/CameraComponent.h"

#include "Kismet/KismetMathLibrary.h"
#include "TimerManager.h"

#include "Engine/World.h"

#include "Engine/Engine.h"
#include "DrawDebugHelpers.h"

#define print(displayText) if(GEngine) GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, TEXT(displayText));

// Sets default values
AFPSControllerCPP::AFPSControllerCPP()
{
	// Set this character to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	DashStrength = 2500.0f;
	DashTime = 0.3f;
	MaxDashesAllowed = 100;

	CanDash = true;
	DashCounter = 0;
	CanDashCounter = 0;
	LastCounterCallTime = 0;
	DodgeStrength = 2000.0f;
	DashResetTimeInAir = 3.0f;
}

// Called when the game starts or when spawned
void AFPSControllerCPP::BeginPlay()
{
	Super::BeginPlay();

	//Set up references
	this->playerCamera = dynamic_cast<UCameraComponent*>(this->GetComponentByClass(UCameraComponent::StaticClass()));
}

// Called every frame
void AFPSControllerCPP::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}

// Called to bind functionality to input
void AFPSControllerCPP::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);

}

//Looks for an object in front of the player
bool AFPSControllerCPP::LookForObjectInFront()
{
	FHitResult outHit;

	FVector sweepStart = GetActorLocation();
	FVector sweepEnd = GetActorLocation();

	// create a collision sphere
	FCollisionShape myColSphere = FCollisionShape::MakeSphere(500.0f);

	// draw collision sphere
	DrawDebugSphere(GetWorld(), GetActorLocation(), myColSphere.GetSphereRadius(), 50, FColor::Purple, true);


	GetWorld()->SweepSingleByChannel(outHit, sweepStart, sweepEnd, FQuat::Identity, ECC_WorldStatic, myColSphere);

	if (outHit.GetActor()->ActorHasTag("Climbable") || outHit.GetActor()->ActorHasTag("WallRunnable")) //TODO change these strings to const variables
	{
		return true;
	}

	return false;
}

//Lets the player dash
void AFPSControllerCPP::Dash()
{
	//Cannot dash once we reach the max dashes allowed
	if (DashCounter >= MaxDashesAllowed)
	{
		CanDash = false;
		return;
	}

	if (!CanDash)
		return;

	if (!playerCamera)
	{
		print("no player camera");
		return;
	}

	if (this->GetCharacterMovement()->IsFalling())
	{
		this->LaunchCharacter(playerCamera->GetForwardVector() * DashStrength, true, false);
		IncreaseDashes();
	}
	else
	{
		this->GetCharacterMovement()->SetPlaneConstraintNormal(FVector(0.0f, 0.0f, 1.0f));
		priorCharacterLocation = GetActorLocation();

		this->LaunchCharacter(playerCamera->GetForwardVector() * DashStrength, true, false);
		GetWorldTimerManager().SetTimer(_dashTimerHandle1, this, &AFPSControllerCPP::onDashTimerEnd1, 0.1f, false);
	}

}

void AFPSControllerCPP::onDashTimerEnd1()
{
	if (!UKismetMathLibrary::EqualEqual_VectorVector(GetActorLocation(), priorCharacterLocation, 0.000001f))
	{
		IncreaseDashes();
	}
}

void AFPSControllerCPP::IncreaseDashes()
{
	DashCounter++;
	CanDash = false;
	GetWorldTimerManager().SetTimer(_dashTimerHandle2, this, &AFPSControllerCPP::onDashTimerEnd2, DashTime, false);

}

void AFPSControllerCPP::onDashTimerEnd2()
{
	if (LookForObjectInFront()) //object is in front
	{
		//continue for now
	}
	else
	{
		this->GetCharacterMovement()->Velocity = FVector(0.0f, 0.0f, 0.0f);
	}

	this->GetCharacterMovement()->SetPlaneConstraintNormal(FVector(0.0f, 0.0f, 0.0f));
	this->GetCharacterMovement()->GravityScale = 1.0f;

	GetWorldTimerManager().SetTimer(_dashTimerHandle3, this, &AFPSControllerCPP::onDashTimerEnd3, DashResetTimeInAir, false);
}

void AFPSControllerCPP::onDashTimerEnd3()
{
	CanDash = true;
}