// Fill out your copyright notice in the Description page of Project Settings.

#include "PactrisCPPBlueprintLibrary.h"

#define print(text) if(GEngine) GEngine->AddOnScreenDebugMessage(-1, 5.f, FColor::Red, TEXT(text));


FColor UPactrisCPPBlueprintLibrary::ConvertColorTypeToColor(EColorType colorType)
{
	switch (colorType)
	{
	case(EColorType::Red):
		return FColor(255, 0, 0, 255); //red
		break;
	case(EColorType::Blue):
		return FColor(0, 0, 255, 255); //blue
		break;
	case(EColorType::Yellow):
		return FColor(255, 255, 0, 255); //yellow
		break;
	case(EColorType::Green):
		return FColor(0, 255, 0, 255); //green
		break;
	default:
		return FColor(0, 0, 255, 255); //blue
		break;
	}
}

APickupSpawnPoint* UPactrisCPPBlueprintLibrary::GetRandomPickupSpawnPoint(UObject* worldContextObject, EPlayerType playerType)
{
	UWorld* World = GEngine->GetWorldFromContextObject(worldContextObject);
	TArray<AActor*> foundClasses;

	UGameplayStatics::GetAllActorsOfClass(World, APickupSpawnPoint::StaticClass(), foundClasses);

	if (foundClasses.Num() <= 0)
		return nullptr;

	for (int i = 0; i < foundClasses.Num(); i++)
	{
		if (Cast<APickupSpawnPoint>(foundClasses[i])->GetPlayerTypeZone() == playerType || Cast<APickupSpawnPoint>(foundClasses[i])->IsFilled())
		{
			foundClasses.RemoveAt(i);
			i--;

			if (foundClasses.Num() <= 0)
				return nullptr;
		}
	}

	int32 spawnPointIndex = FMath::RandRange(0, foundClasses.Num() - 1);

	Cast<APickupSpawnPoint>(foundClasses[spawnPointIndex])->SetFilled(true);
	return Cast<APickupSpawnPoint>(foundClasses[spawnPointIndex]);
}

APickupSpawnPoint* UPactrisCPPBlueprintLibrary::GetRandomPickupSpawnPointNoPlayerType(UObject* worldContextObject, bool isKey, int numPlayers)
{
	UWorld* World = GEngine->GetWorldFromContextObject(worldContextObject);
	TArray<AActor*> foundClasses;

	UGameplayStatics::GetAllActorsOfClass(World, APickupSpawnPoint::StaticClass(), foundClasses);

	if (foundClasses.Num() <= 0)
		return nullptr;

	for (int i = 0; i < foundClasses.Num(); i++)
	{
		if (Cast<APickupSpawnPoint>(foundClasses[i])->IsFilled())
		{
			foundClasses.RemoveAt(i);
			i--;

			if (foundClasses.Num() <= 0)
				return nullptr;
		}
	}

	//if not a key, then make sure we don't spawn something to reduce the number of available key spots left
	if (!isKey)
	{
		if (foundClasses.Num() <= numPlayers)
		{
			//Can't spawn something
			return nullptr;
		}
	}

	int32 spawnPointIndex = FMath::RandRange(0, foundClasses.Num() - 1);

	Cast<APickupSpawnPoint>(foundClasses[spawnPointIndex])->SetFilled(true);
	return Cast<APickupSpawnPoint>(foundClasses[spawnPointIndex]);
}

int UPactrisCPPBlueprintLibrary::GetZero()
{
	return 0;
}
