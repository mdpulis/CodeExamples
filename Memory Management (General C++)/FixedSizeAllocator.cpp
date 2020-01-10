#include "FixedSizeAllocator.h"

#include <stdio.h>





FixedSizeAllocator::FixedSizeAllocator()
{
	printf("testing");
}

//The constructor for normal use of the fixed size allocator.
FixedSizeAllocator::FixedSizeAllocator(void* i_memoryStart, size_t i_blockSize)
{
	if (i_memoryStart == nullptr)
		return;

	if (i_blockSize < 0)
		return;

	blockSize = i_blockSize;
	numBlocks = GetNumBlocksFromAllocSize(i_blockSize);

	memoryStart = i_memoryStart;
#ifdef USE_MEMORY_MANAGER
	fsaBitArray = reinterpret_cast<BitArray*>(globalMemoryManager->alloc(sizeof(BitArray)));
	fsaBitArray->SetInfo(numBlocks);
#else
	fsaBitArray = new BitArray(numBlocks);
#endif
}


FixedSizeAllocator::~FixedSizeAllocator()
{
	if (!fsaBitArray->AreAllClear())
	{
#if defined(_DEBUG)
		printf("WARNING: There were outstanding allocations for FixedSizeAllocator of block size %d. Deleting.\n", blockSize);
#endif
	}
}


void FixedSizeAllocator::SetInfo(size_t i_blockSize, void* i_memoryStart)
{
	blockSize = i_blockSize;
	numBlocks = GetNumBlocksFromAllocSize(i_blockSize);

	memoryStart = i_memoryStart;

#ifdef USE_MEMORY_MANAGER
	fsaBitArray = reinterpret_cast<BitArray*>(globalMemoryManager->alloc(sizeof(BitArray)));
	fsaBitArray->SetInfo(numBlocks);
#else
	fsaBitArray = new BitArray(numBlocks);
#endif

}

//This function is currently unused
bool FixedSizeAllocator::FindNextAvailableBlock(size_t & o_FirstAvailable)
{
	const size_t count = 0; // AvailableFlags.size();

	return false;
}

//Allocates memory in the fixed size allocator
void* FixedSizeAllocator::Allocate()
{
	size_t i_firstAvailable;

	if (fsaBitArray->GetFirstClearBit(i_firstAvailable))
	{
		// mark it in use because we're going to allocate it to user
		fsaBitArray->SetBit(i_firstAvailable);

		// calculate its address and return it to user
		return static_cast<char*>(memoryStart) + (i_firstAvailable * blockSize);
	}
	else
	{
		return nullptr;
	}
}

void FixedSizeAllocator::Free(void* i_ptr)
{
	if (!IsPointerInRange(i_ptr))
	{
		printf("Pointer is not in range.\n");
		return;
	}

	int pointerDifference = static_cast<char*>(i_ptr) - static_cast<char*>(memoryStart);
	int bitOffset = pointerDifference / blockSize;

	//If our bit is not set, then we don't have anything to free
	if(!fsaBitArray->IsBitSet(bitOffset))
	{
		return;
	}

	fsaBitArray->ClearBit(bitOffset);
}

//Gets the size that we set aside for this FSA
size_t FixedSizeAllocator::GetReservedSize()
{
	return blockSize * numBlocks;
}

//Looks within the Fixed Allocator's address range to see if the pointer is in it
bool FixedSizeAllocator::IsPointerInRange(void* i_ptr)
{
	if (static_cast<char*>(i_ptr) >= memoryStart &&
		static_cast<char*>(i_ptr) < static_cast<char*>(memoryStart) + GetReservedSize())
	{
		return true;
	}
	else
	{
		return false;
	}
}

//This gets the number of blocks based on alloc size
size_t FixedSizeAllocator::GetNumBlocksFromAllocSize(size_t l_blockSize)
{
	if (l_blockSize <= 16)
	{
		return NUM_BLOCKS_0_to_16;
	}
	else if (l_blockSize >= 17 && l_blockSize <= 32)
	{
		return NUM_BLOCKS_17_to_32;
	}
	else if (l_blockSize >= 33 && l_blockSize <= 96)
	{
		return NUM_BLOCKS_33_to_96;
	}
	else
	{
		//UNSUPPORTED
		return 0;
	}
}


void* FixedSizeAllocator::operator new(const size_t i_size)
{
	return _aligned_malloc(i_size, 4);
}