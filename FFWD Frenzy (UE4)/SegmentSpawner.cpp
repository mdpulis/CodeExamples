// Fill out your copyright notice in the Description page of Project Settings.

#define print(text) if(GEngine) GEngine->AddOnScreenDebugMessage(-1, 1.5, FColor::Green, text)
#define printBlue(text) if(GEngine) GEngine->AddOnScreenDebugMessage(-1, 2.5, FColor::Blue, text)
#define printYellow(text) if(GEngine) GEngine->AddOnScreenDebugMessage(-1, 2.5, FColor::Yellow, text)
#define printRed(text) if(GEngine) GEngine->AddOnScreenDebugMessage(-1, 2.5, FColor::Red, text)
#define printf(text, fstring) if(GEngine) GEngine->AddOnScreenDebugMessage(-1, 1.5, FColor::Green, FString::Printf(TEXT(text), fstring))

#include "SegmentSpawner.h"

#include "SegmentBase.h"
#include "SpawnSegmentsTriggerBox.h"

#include "RobotRunnerGameMode.h"
#include "RobotRunnerCharacter.h"
#include "BoostTriggerBox.h"
#include "UObject/ConstructorHelpers.h"
#include "Runtime/Engine/Classes/Engine/World.h"
#include "EngineUtils.h"




// Sets default values
ASegmentSpawner::ASegmentSpawner()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	passedTime = 0.0f;
	segmentSpawnNumber = 0;
	areasDeleted = 0;
	currentLocation = -1000.0f;

	segmentDistance = -1000.0f;
	segmentsPerSpawn = 10;

}


//Protected

// Called when the game starts or when spawned
void ASegmentSpawner::BeginPlay()
{
	Super::BeginPlay();
	

	float newValue = -1.0f;

	PrimaryActorTick.bCanEverTick = true;
	PrimaryActorTick.bStartWithTickEnabled = true;

	if (this->IsActorTickEnabled())
	{
		newValue = 1.0f;
	}
	else
	{
		newValue = 0.0f;
	}

	GEngine->AddOnScreenDebugMessage(-1, 1.5, FColor::Green, FString::Printf(TEXT("Tick Enabled? %F"), newValue));



	FloorSpawnArray.Init(true, 48);
	FloorSpawnCounter = 0;
	this->SpawnArea();
}


// Called every frame
void ASegmentSpawner::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	passedTime += DeltaTime;
}

//Creates and spawns an area (a block of many segments)
void ASegmentSpawner::SpawnArea()
{
	//Start by removing previous areas if they exist
	RemovePreviousAreas();

	int totalSegmentWeight = 0;

	for (int i = 0; i < SegmentInformationList.Num(); i++)
	{
		int segmentWeight = SegmentInformationList[i].segmentWeight;
		totalSegmentWeight += segmentWeight;

		for (int j = 0; j < segmentWeight; j++)
		{
			weightedSegmentBaseList2.Add(Cast<ASegmentBase>(SegmentInformationList[i].segmentBaseType));
			weightedSegmentBaseList.Add(SegmentInformationList[i].segmentBaseType);
		}

	}

	for (int i = 0; i < segmentsPerSpawn; i++)
	{
	
		int segmentIndex = 0;
		bool segmentOkay = false;
		
		segmentIndex = (rand() % weightedSegmentBaseList.Num()); //generates a number between 0 and the number of items -1 in the pullable segment base list
		SpawnSegmentPiece(weightedSegmentBaseList[segmentIndex]);
	}
	
	SpawnSpawnSegmentsTriggerBox();

	printYellow("Size of spawned segments list after addition: " + FString::FromInt(spawnedSegmentsList.Num()));

}


void ASegmentSpawner::SpawnSegmentPiece(TSubclassOf<ASegmentBase> spawnPiece)
{
	UWorld* world = GetWorld();
	if (world)
	{
		FActorSpawnParameters spawnParams;
		spawnParams.Owner = this;

		FRotator rotator(0.0f, 0.0f, 0.0f);

		FVector spawnLocation(0.0f, currentLocation, 0.0f);
		AActor* newSegmentPiece = GetWorld()->SpawnActor<AActor>(spawnPiece, spawnLocation, rotator, spawnParams);

		if (newSegmentPiece)
		{
			ASegmentBase* spawnedSegmentBase = Cast<ASegmentBase>(newSegmentPiece);
			spawnedSegmentBase->SegmentNumber = segmentSpawnNumber;
			spawnedSegmentsList.Add(spawnedSegmentBase);

			segmentSpawnNumber++;
			currentLocation -= 1000.0f;
		}
	}
}

ASegmentBase* ASegmentSpawner::GetPreviousRespawnableSegment(int currentSegmentNumber)
{
	currentSegmentNumber--;

	currentSegmentNumber -= (areasDeleted * segmentsPerSpawn);

	while (currentSegmentNumber > 0)
	{
		if (spawnedSegmentsList[currentSegmentNumber]->GetCharacterCanRespawn())
		{
			return spawnedSegmentsList[currentSegmentNumber];
		}

		currentSegmentNumber--;
	}

	return nullptr;
}



//Private
void ASegmentSpawner::RemovePreviousAreas()
{
	//If we have spawned more than 2 sets, delete the 1st one.
  	if (spawnedSegmentsList.Num() >= segmentsPerSpawn * 2)
	{
		GEngine->AddOnScreenDebugMessage(-1, 1.5, FColor::Blue, "Remove Previous Areas attempt.");

		int deleteCount = 0;
		while (deleteCount < segmentsPerSpawn)
		{
			spawnedSegmentsList[0]->Destroy();
			spawnedSegmentsList.RemoveAt(0);

			deleteCount++;
		}

		printYellow("Size of spawned segments list after removal: " + FString::FromInt(spawnedSegmentsList.Num()));
		areasDeleted++;

		GEngine->ForceGarbageCollection(true);
	}

}

//Removes all currently active areas and deletes them
void ASegmentSpawner::RemoveAllAreas()
{
	GEngine->AddOnScreenDebugMessage(-1, 1.5, FColor::Yellow, "Remove All Areas attempt.");

	while (spawnedSegmentsList.Num() != 0)
	{
		spawnedSegmentsList[0]->Destroy();
		spawnedSegmentsList.RemoveAt(0);
	}

	GEngine->ForceGarbageCollection(true);
}

//Spawns the according trigger box to spawn more areas
void ASegmentSpawner::SpawnSpawnSegmentsTriggerBox()
{
	GEngine->AddOnScreenDebugMessage(-1, 1.5, FColor::Green, "Spawn SPAWN SEGMENTS TRIGGER BOX attempt.");

	UWorld* world = GetWorld();
	if (world)
	{

		FActorSpawnParameters spawnParams;
		spawnParams.Owner = this;

		FRotator rotator(0.0f, 0.0f, 0.0f);

		FVector spawnLocation(0.0f, currentLocation + ((segmentDistance * -1) * (segmentsPerSpawn / 2)), 0.0f);
		AActor* newProjectile = GetWorld()->SpawnActor<AActor>(SpawnSegmentsTriggerBox, spawnLocation, rotator, spawnParams);
	}
}

//Reset the segment spawner parameters
void ASegmentSpawner::ResetSegmentSpawner()
{
	RemoveAllAreas();

	passedTime = 0.0f;
	segmentSpawnNumber = 0;
	areasDeleted = 0;
	currentLocation = segmentDistance;

	SpawnArea(); //start by respawning a new area
}