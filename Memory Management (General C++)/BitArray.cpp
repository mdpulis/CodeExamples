#include "BitArray.h"

#pragma warning( disable : 4319) //~ zero extending unsigned long to size_t of greater size
#pragma warning( disable : 4334) //<< result of 32 bit shift implicity converted to 64 bits
#pragma warning( disable : 4267) //conversion from size_t to unsigned long

BitArray::BitArray()
{
}

BitArray::BitArray(size_t i_numBits)
{
	numBytes = i_numBits / BITS_PER_BYTE; //total number of bytes we have (equiv. to array size)
#ifdef USE_MEMORY_MANAGER
	st_bits = reinterpret_cast<size_t*>(globalMemoryManager->alloc(sizeof(size_t) * numBytes));
#else
	st_bits = new size_t[i_numBits / BITS_PER_BYTE];
#endif

	assert(st_bits);

	ClearAll();
}

BitArray::~BitArray()
{
	delete[] st_bits;
}

void BitArray::SetInfo(size_t i_numBits)
{
	numBytes = i_numBits / BITS_PER_BYTE; //total number of bytes we have (equiv. to array size)
#ifdef USE_MEMORY_MANAGER
	st_bits = reinterpret_cast<size_t*>(globalMemoryManager->alloc(sizeof(size_t) * numBytes));
#else
	st_bits = new size_t[i_numBits / BITS_PER_BYTE];
#endif

	assert(st_bits);

	ClearAll();
}

void BitArray::ClearAll(void)
{
	for (unsigned int i = 0; i < numBytes; i++)
	{
		for (int j = 0; j < BITS_PER_BYTE; j++)
		{
			st_bits[i] &= ~(1UL << j); //clear bit at i's jth index
		}
	}
}

void BitArray::SetAll(void)
{
	for (unsigned int i = 0; i < numBytes; i++)
	{
		for (int j = 0; j < BITS_PER_BYTE; j++)
		{
			st_bits[i] |= 1UL << j; //set bit at i's jth index
		}
	}
}

bool BitArray::AreAllClear(void) const
{
	unsigned long index = 0;

	// quick skip bytes where no bits are set   
	while ((st_bits[index] == HEX_BYTE_MIN_SIZE) && (index < numBytes))
		(index)++;

	if (index == numBytes)
		return true;
	else
		return false;
}

bool BitArray::AreAllSet(void) const
{
	unsigned long index = 0;

	// quick skip bytes where all bits are set   
	while ((st_bits[index] == HEX_BYTE_MAX_SIZE) && (index < numBytes))
		(index)++;

	if (index == numBytes)
		return true;
	else
		return false;
}

bool BitArray::IsBitSet(size_t i_bitNumber) const
{
	unsigned long index = i_bitNumber / BITS_PER_BYTE;
	uint8_t bitLocation = i_bitNumber % BITS_PER_BYTE;

	return ((st_bits[index] >> bitLocation) & 1UL); //checks if set
}

bool BitArray::IsBitClear(size_t i_bitNumber) const
{
	unsigned long index = i_bitNumber / BITS_PER_BYTE;
	uint8_t bitLocation = i_bitNumber % BITS_PER_BYTE;

	return !((st_bits[index] >> bitLocation) & 1UL); //checks if clear
}

void BitArray::SetBit(size_t i_bitNumber)
{
	unsigned long index = i_bitNumber / BITS_PER_BYTE;
	uint8_t bitLocation = i_bitNumber % BITS_PER_BYTE;

	st_bits[index] |= 1UL << bitLocation;
}

void BitArray::ClearBit(size_t i_bitNumber)
{
	unsigned long index = i_bitNumber / BITS_PER_BYTE;
	uint8_t bitLocation = i_bitNumber % BITS_PER_BYTE;

	st_bits[index] &= ~(1UL << bitLocation);
}

bool BitArray::GetFirstClearBit(size_t &o_bitNumber) const
{
	unsigned long index = 0;
	
	// quick skip bytes where no bits are set   
	while ((st_bits[index] == HEX_BYTE_MAX_SIZE) && (index < numBytes))
		(index)++;

	if (index >= numBytes) //if we got through everything without a clear bit
	{
		return false;
	}


	for (int i = 0; i < BITS_PER_BYTE; i++)
	{
		if (!(st_bits[index] >> i & 1UL)) //if cleared
		{
			o_bitNumber = (index * BITS_PER_BYTE) + i;
			return true;
		}
	}

	return false;
}

bool BitArray::GetFirstSetBit(size_t & o_bitNumber) const
{
	unsigned long index = 0;

	// quick skip bytes where no bits are set   
	while ((st_bits[index] == HEX_BYTE_MIN_SIZE) && (index < numBytes))
		(index)++;

	if (index >= numBytes) //if we got through everything without a set bit
	{
		return false;
	}

	for (int i = 0; i < BITS_PER_BYTE; i++)
	{
		if ((st_bits[index] >> i & 1UL)) //if filled
		{
			o_bitNumber = (index * BITS_PER_BYTE) + i;
			return true;
		}
	}


	return false;
}

bool BitArray::operator[](size_t i_index) const
{
	return false;
}

void * BitArray::operator new(const size_t i_size)
{
	return _aligned_malloc(i_size, 4);
}
