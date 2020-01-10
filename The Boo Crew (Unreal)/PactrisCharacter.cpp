// Copyright 1998-2019 Epic Games, Inc. All Rights Reserved.

#include "PactrisCharacter.h"
#include "UObject/ConstructorHelpers.h"
#include "Camera/CameraComponent.h"
#include "Components/DecalComponent.h"
#include "Components/CapsuleComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/PlayerController.h"
#include "GameFramework/SpringArmComponent.h"
#include "HeadMountedDisplayFunctionLibrary.h"
#include "Materials/Material.h"

#include "PickupKey.h"

#include "Engine.h"
#include "Engine/World.h"

#define print(text) if(GEngine) GEngine->AddOnScreenDebugMessage(-1, 5.f, FColor::Red, TEXT(text));


APactrisCharacter::APactrisCharacter()
{
	// Set size for player capsule
	GetCapsuleComponent()->InitCapsuleSize(42.f, 96.0f);

	// Don't rotate character to camera direction
	bUseControllerRotationPitch = false;
	bUseControllerRotationYaw = false;
	bUseControllerRotationRoll = false;

	// Configure character movement
	GetCharacterMovement()->bOrientRotationToMovement = true; // Rotate character to moving direction
	GetCharacterMovement()->RotationRate = FRotator(0.f, 640.f, 0.f);
	GetCharacterMovement()->bConstrainToPlane = true;
	GetCharacterMovement()->bSnapToPlaneAtStart = true;

	PlayerLight = CreateDefaultSubobject<UPointLightComponent>("PlayerLight");
	PlayerLight->SetupAttachment(RootComponent);
	PlayerLight->SetRelativeLocation(FVector(0.0f, 0.0f, 200.0f));
	PlayerLight->Intensity = 50.0f;
	PlayerLight->LightColor = FColor(255, 200, 64, 255);
	PlayerLight->AttenuationRadius = 800.0f;
	PlayerLight->CastShadows = false;


	// Activate ticking in order to update the cursor every frame.
	PrimaryActorTick.bCanEverTick = true;
	PrimaryActorTick.bStartWithTickEnabled = true;

	//Our custom variables
	ItemSpawnDistanceFromPlayer = 200.0f;

	playerColorType = EColorType::Red;
	playerType = EPlayerType::P1;
	keysInserted = 0;

	playerStartTransform = FTransform();

	DefaultWalkSpeed = 600.0f;
	BoostedWalkSpeed = 900.0f;
	BoostTime = 5.0f;
	StunTime = 3.0f;
	InvincibilityTime = 3.0f;

	playing = false;
	escaped = false;
	stunned = false;
	currentStunTime = 0.0f;
	boosted = false;
	currentBoostTime = 0.0f;
	invincible = false;
	currentInvincibilityTime = 0.0f;
}

void APactrisCharacter::Tick(float DeltaSeconds)
{
	if (!playing)
		return;

	Super::Tick(DeltaSeconds);

	if (boosted)
	{
		currentBoostTime += DeltaSeconds;
		if (currentBoostTime >= BoostTime)
		{
			boosted = false;
			currentBoostTime = 0.0f;
			UpdateWalkSpeed();
		}
	}

	if (stunned)
	{
		currentStunTime += DeltaSeconds;

		this->AddActorLocalRotation(this->StunnedRotationRatePerSecond * DeltaSeconds);

		if (currentStunTime >= StunTime)
		{
			stunned = false;
			currentStunTime = 0.0f;
		}
	}

	if (invincible)
	{
		currentInvincibilityTime += DeltaSeconds;

		if (currentInvincibilityTime >= InvincibilityTime)
		{
			invincible = false;
			currentInvincibilityTime = 0.0f;
		}
	}
}

void APactrisCharacter::StartPlaying()
{
	playing = true;
}

void APactrisCharacter::SetupReferences(int requiredKeysToOpenDoor)
{
	requiredKeys = requiredKeysToOpenDoor;
}

void APactrisCharacter::MovePlayer(float xValue, float yValue)
{
	FVector direction = FVector::ZeroVector;

	if (xValue > 0.0f)
	{
		direction += FVector::RightVector;
	}
	else if (xValue < 0.0f)
	{
		direction += FVector::LeftVector;
	}
	else
	{
		direction += FVector::ZeroVector;
	}

	if (yValue > 0.0f)
	{
		direction += FVector::ForwardVector;
	}
	else if (yValue < 0.0f)
	{
		direction += FVector::BackwardVector;
	}
	else
	{
		direction += FVector::ZeroVector;
	}

	AddMovementInput(direction);
}

void APactrisCharacter::PickupKey(class APickupKey* pickedUpKey)
{
	if (!this->HasKey())
	{
		currentKey = pickedUpKey;
	}
	else
	{
		//print("Cannot add key to inventory when already has key.");
	}
}

bool APactrisCharacter::HasKey()
{
	if (currentKey != nullptr)
	{
		return true;
	}
	else
	{
		return false;
	}
}

void APactrisCharacter::InsertKey()
{
	if (!currentKey)
		return;

	keysInserted++;

	OnInsertKey.Broadcast(playerType, playerColorType, keysInserted);

	UpdateWalkSpeed();

	currentKey->Destroy();
	currentKey = nullptr;

	if (keysInserted >= requiredKeys)
	{
		this->Escape();
	}
}


void APactrisCharacter::PickupItem(class APickupItem* pickedUpItem)
{
	if (!this->HasItem())
	{
		currentItem = pickedUpItem;
		OnPickUpItem.Broadcast(playerType, pickedUpItem->GetPickupItemType(), true);
	}
	else
	{
		//print("Cannot add item to inventory when already has item.");
	}
}

void APactrisCharacter::UseItem()
{
	if (!this->HasItem())
	{
		return;
	}


	switch (currentItem->pickupItemType)
	{
	case(EPickupItemType::EnergyDrink):
		UseEnergyDrink();
		break;
	case(EPickupItemType::Barricade):
		UseBarricade();
		break;
	case(EPickupItemType::RomanCandle):
		UseRomanCandle();
		break;
	}

	OnPickUpItem.Broadcast(playerType, currentItem->GetPickupItemType(), false);

	currentItem = nullptr;

	if (itemUseVoicesSFX.Num() > 0)
	{
		int randSFX = FMath::RandRange(0, itemUseVoicesSFX.Num() - 1);
		UGameplayStatics::PlaySoundAtLocation(this, itemUseVoicesSFX[randSFX], GetActorLocation());
	}
}

bool APactrisCharacter::HasItem()
{
	if (currentItem != nullptr)
	{
		return true;
	}
	else
	{
		return false;
	}
}

void APactrisCharacter::Escape()
{
	playing = false;
	escaped = true;
	OnEscape.Broadcast(playerType, playerColorType);

	if (escapeVoicesSFX.Num() > 0)
	{
		int randSFX = FMath::RandRange(0, escapeVoicesSFX.Num() - 1);
		UGameplayStatics::PlaySoundAtLocation(this, escapeVoicesSFX[randSFX], GetActorLocation());
	}
}

bool APactrisCharacter::CanMove()
{
	if (playing && !stunned)
	{
		return true;
	}
	else
	{
		return false;
	}
}

bool APactrisCharacter::CanUseItem()
{
	if (playing && !stunned)
	{
		return true;
	}
	else
	{
		return false;
	}
}

void APactrisCharacter::UpdateWalkSpeed()
{
	if (boosted)
	{
		GetCharacterMovement()->MaxWalkSpeed = BoostedWalkSpeed * (1.0f - (0.1f * keysInserted));
	}
	else
	{
		GetCharacterMovement()->MaxWalkSpeed = DefaultWalkSpeed * (1.0f - (0.1f * keysInserted));
	}
}

void APactrisCharacter::TakeDamage()
{
	PlayerLight->SetVisibility(false);

	stunned = true;

	if (currentKey != nullptr)
	{
		currentKey->DetachFromActor(FDetachmentTransformRules::KeepRelativeTransform);
		currentKey->SetActorLocation(this->GetActorLocation());
		currentKey->pickedUp = false;
		currentKey = nullptr;
	}

	this->SetActorLocation(FVector(0.0f, 0.0f, -2000.0f)); //put under the map

	if (hurtVoicesSFX.Num() > 0)
	{
		int randSFX = FMath::RandRange(0, hurtVoicesSFX.Num() - 1);
		UGameplayStatics::PlaySoundAtLocation(this, hurtVoicesSFX[randSFX], GetActorLocation());
	}
}

void APactrisCharacter::TakeStun()
{
	if (stunned)
	{
		return;
	}

	stunned = true;

	if (hurtVoicesSFX.Num() > 0)
	{
		int randSFX = FMath::RandRange(0, hurtVoicesSFX.Num() - 1);
		UGameplayStatics::PlaySoundAtLocation(this, hurtVoicesSFX[randSFX], GetActorLocation());
	}
}

void APactrisCharacter::Respawn()
{
	PlayerLight->SetVisibility(true);

	stunned = false;
	currentStunTime = 0.0f;

	invincible = true;
	currentInvincibilityTime = 0.0f;

	SetActorTransform(playerStartTransform);

	if (respawnVoicesSFX.Num() > 0)
	{
		int randSFX = FMath::RandRange(0, respawnVoicesSFX.Num() - 1);
		UGameplayStatics::PlaySoundAtLocation(this, respawnVoicesSFX[randSFX], GetActorLocation());
	}
}

void APactrisCharacter::SetPlayerType(EPlayerType newPlayerType)
{
	playerType = newPlayerType;
}

EPlayerType APactrisCharacter::GetPlayerType()
{
	return playerType;
}

void APactrisCharacter::SetPlayerColorType(EColorType newColorType)
{
	playerColorType = newColorType;
}

EColorType APactrisCharacter::GetPlayerColorType()
{
	return playerColorType;
}

void APactrisCharacter::SetPlayerStartTransform(FTransform newStartTransform)
{
	playerStartTransform = newStartTransform;
}

FTransform APactrisCharacter::GetPlayerStartTransform()
{
	return playerStartTransform;
}

void APactrisCharacter::UseEnergyDrink()
{
	boosted = true;
	currentBoostTime = 0.0f;
	UpdateWalkSpeed();

	if (boostVoicesSFX.Num() > 0)
	{
		int randSFX = FMath::RandRange(0, boostVoicesSFX.Num() - 1);
		UGameplayStatics::PlaySoundAtLocation(this, boostVoicesSFX[randSFX], GetActorLocation());
	}
}

void APactrisCharacter::UseRomanCandle()
{
	FActorSpawnParameters spawnParams;
	spawnParams.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;

	ARomanCandle* rc = GetWorld()->SpawnActor<ARomanCandle>(RomanCandleClass, GetActorLocation() + (this->GetActorForwardVector() * ItemSpawnDistanceFromPlayer), GetActorRotation(), spawnParams);
	if (rc != nullptr)
	{
		rc->SetMovingForwardVector(this->GetActorForwardVector());
	}
	else
	{
		//print("already nullptr");
	}
}

void APactrisCharacter::UseBarricade()
{
	GetWorld()->SpawnActor<ABarricade>(BarricadeClass, GetActorLocation() + (this->GetActorForwardVector() * ItemSpawnDistanceFromPlayer), GetActorRotation());
}

