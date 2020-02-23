// Copyright 1998-2019 Epic Games, Inc. All Rights Reserved.

#include "ZhengGameMode.h"
#include "ZhengPlayerController.h"
#include "ZhengCharacter.h"
#include "UObject/ConstructorHelpers.h"

AZhengGameMode::AZhengGameMode()
{
	// use our custom PlayerController class
	PlayerControllerClass = AZhengPlayerController::StaticClass();

	NumPlayers = 2;
	NumRoundsToWin = 3;

	TimeSinceBattleStarted = 0.0f;
	TimeSinceRoundStarted = 0.0f;
	RoundTime = 99.0f;

	roundCurrentTime = RoundTime;

	midBattle = false;
	midRound = false;
}

void AZhengGameMode::BeginPlay()
{
	Super::BeginPlay();

	AssignPlayerStarts();

	BeginBattle();
	BeginRound();
}

void AZhengGameMode::Tick(float deltaTime)
{
	Super::Tick(deltaTime);

	if (midBattle)
	{
		if (midRound)
		{
			roundCurrentTime -= deltaTime;
			if (roundCurrentTime <= 0)
			{
				EndRound();
			}
		}
	}

}

void AZhengGameMode::AssignPlayerStarts()
{
	TArray<AActor*> foundClasses;

	UGameplayStatics::GetAllActorsOfClass(GetWorld(), APlayerStart::StaticClass(), foundClasses);

	if (foundClasses.Num() <= 0)
	{
		print("didn't find any classes");
	}

	for (int i = 0; i < foundClasses.Num(); i++)
	{
		playerStarts.Add(Cast<APlayerStart>(foundClasses[i]));
	}
}

bool AZhengGameMode::IsMidBattle()
{
	return midBattle;
}

bool AZhengGameMode::IsMidRound()
{
	return midRound;
}

void AZhengGameMode::BeginBattle()
{
	roundCurrentTime = RoundTime;
	midBattle = true;
}

void AZhengGameMode::EndBattle()
{
	midBattle = false;
}

bool AZhengGameMode::CheckForEndOfBattle()
{
	for (int i = 0; i < ZhengPlayers.Num(); i++)
	{
		if (ZhengPlayers[i]->GetRoundsWon() >= NumRoundsToWin)
		{
			return true;
		}
	}

	return false;
}

void AZhengGameMode::BeginRound()
{
	roundCurrentTime = RoundTime;
	midRound = true;
}

void AZhengGameMode::EndRound()
{
	midRound = false;
	GetRoundWinner()->IncrementRoundsWon();

	if (CheckForEndOfBattle())
	{
		GetRoundWinner()->WinBattle();
	}
	else
	{
		ResetPlayers();
		BeginRound();
	}

}

bool AZhengGameMode::CheckForEndOfRound()
{
	if (GetNumberOfRemainingPlayers() <= 1)
	{
		return true;
	}

	return false;
}

void AZhengGameMode::ResetPlayers()
{
	for (int i = 0; i < ZhengPlayers.Num(); i++)
	{
		if (playerStarts.Num() >= i)
		{
			ZhengPlayers[i]->SetActorLocation(playerStarts[i]->GetActorLocation());
		}
		else
		{
			ZhengPlayers[i]->SetActorLocation(FVector(0, 0, 0));
		}
		ZhengPlayers[i]->ResetForRound();
	}
}

void AZhengGameMode::ResetPlayersForBattle()
{
	midBattle = true;
	for (int i = 0; i < ZhengPlayers.Num(); i++)
	{
		if (playerStarts.Num() >= i)
		{
			ZhengPlayers[i]->SetActorLocation(playerStarts[i]->GetActorLocation());
		}
		else
		{
			ZhengPlayers[i]->SetActorLocation(FVector(0, 0, 0));
		}
		ZhengPlayers[i]->ResetForBattle();
	}
}

int AZhengGameMode::GetNumberOfRemainingPlayers()
{
	int remainingPlayers = 0;

	for (int i = 0; i < ZhengPlayers.Num(); i++)
	{
		if (ZhengPlayers[i]->IsAlive())
			remainingPlayers++;
	}

	return remainingPlayers;
}

AZhengCharacter* AZhengGameMode::GetRoundWinner()
{
	AZhengCharacter* highestHealthCharacter = nullptr;

	for (int i = 0; i < ZhengPlayers.Num(); i++)
	{
		if (ZhengPlayers[i]->IsAlive())
		{
			if (highestHealthCharacter == nullptr)
			{
				highestHealthCharacter = ZhengPlayers[i];
			}
			else
			{
				if (ZhengPlayers[i]->GetHealth() > highestHealthCharacter->GetHealth())
				{
					highestHealthCharacter = ZhengPlayers[i];
				}
			}
		}
	}

	if (highestHealthCharacter != nullptr)
	{
		return highestHealthCharacter;
	}

	print("Did not find any alive characters. Returning the 1st player.");
	return ZhengPlayers[0];
}